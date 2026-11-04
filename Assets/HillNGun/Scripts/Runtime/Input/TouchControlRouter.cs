using System.Collections.Generic;
using UnityEngine;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.Input
{
    /// <summary>
    /// Pure-logic touch router: turns per-pointer down / move / up events into the same input commands the bots and
    /// keyboard use. Every pointer id owns at most one control, so the left thumb can hold the move stick while the
    /// right thumb attacks, rolls, throws or uses the skill. No Unity input or UI calls here (unit-tested with fakes).
    ///
    /// Attack (Brawl-Stars feel): drag from the touch-down point = aim (no shot while dragging), release = one shot in
    /// that direction; a short tap inside the deadzone auto-aims at the nearest enemy; dragging back into the centre
    /// and releasing cancels. Shots respect WeaponData.fireInterval: an early release is queued briefly, never doubled.
    /// </summary>
    public class TouchControlRouter
    {
        public enum Role { None, Move, Attack, Roll, Skill, Grenade }

        private readonly IInputReceiver _rx;
        private readonly ITouchControlContext _ctx;
        private TouchControlsData _d;
        private TouchLayout _L;
        private readonly Dictionary<int, Role> _roles = new Dictionary<int, Role>();

        private const float FireIntervalSlack = 0.02f;

        // ---- view / test state ----
        public TouchLayout Layout => _L;
        public bool MoveActive { get; private set; }
        public Vector2 MoveOrigin { get; private set; }
        public Vector2 MoveKnob { get; private set; }
        public Vector2 MoveDir { get; private set; }
        public float MoveMag { get; private set; }

        public bool AttackActive { get; private set; }
        public bool AttackAiming { get; private set; }
        public Vector2 AttackDelta { get; private set; }
        public Vector2 AimDir { get; private set; } = Vector2.up;

        public bool RollPressed { get; private set; }

        public bool SkillActive { get; private set; }
        public bool SkillAiming { get; private set; }
        public Vector2 SkillTarget { get; private set; }

        public bool GrenadeActive { get; private set; }
        public bool GrenadeAiming { get; private set; }
        public Vector2 GrenadeTarget { get; private set; }
        public Vector2 GrenadeDelta { get; private set; }

        public bool HasPendingShot => _pending;
        /// <summary>1 right after a shot → 0 when the next shot is allowed (revolver cock / fire-interval ring).</summary>
        public float ShotCooldown01 => _ctx.FireInterval > 0f ? Mathf.Clamp01(1f - (Now - _lastShot) / _ctx.FireInterval) : 0f;
        public float LastShotTime => _lastShot;
        public int ActivePointers => _roles.Count;

        // counters (smoke / tests)
        public int ShotsRequested { get; private set; }
        public int ShotsSent { get; private set; }
        public int ShotsQueued { get; private set; }
        public int ShotsDropped { get; private set; }
        public int AutoAimShots { get; private set; }
        public int Rolls { get; private set; }
        public int GrenadesThrown { get; private set; }
        public int SkillPresses { get; private set; }
        public int RepeatShots { get; private set; }

        private int _movePtr = -1, _atkPtr = -1, _rollPtr = -1, _skPtr = -1, _grPtr = -1;
        private int _rowDir; private float _rowHold; private bool _rowSent;
        private Vector2 _atkStart; private float _atkDown; private bool _atkAimedOut; private int _atkRepeats;
        private float _lastShot = -999f;
        private bool _pending; private Vector2 _pendingDir; private float _pendingUntil;
        private Vector2 _skStart; private float _skDown; private bool _skStarted; private Vector2 _skDir; private float _skDist;
        private Vector2 _grStart; private Vector2 _grDir = Vector2.up; private float _grDist;

        public TouchControlRouter(IInputReceiver receiver, ITouchControlContext ctx, TouchControlsData data)
        {
            _rx = receiver;
            _ctx = ctx;
            _d = data;
        }

        public void SetLayout(in TouchLayout layout) { _L = layout; }
        public void SetData(TouchControlsData data) { _d = data; }

        private float Now => _ctx.Now;

        public Role RoleOf(int pointerId) => _roles.TryGetValue(pointerId, out var r) ? r : Role.None;

        // =====================================================================================================
        public void PointerDown(int id, Vector2 pos)
        {
            if (_roles.ContainsKey(id)) PointerUp(id, pos);
            Role role = Role.None;
            if (_d != null && _L.Valid && _ctx.HasCharacter)
            {
                switch (TouchControlLayout.HitTest(_L, pos))
                {
                    case TouchControlLayout.Hit.Grenade: if (_grPtr < 0) role = Role.Grenade; break;
                    case TouchControlLayout.Hit.Skill: if (_skPtr < 0) role = Role.Skill; break;
                    case TouchControlLayout.Hit.Roll: if (_rollPtr < 0) role = Role.Roll; break;
                    case TouchControlLayout.Hit.Attack: if (_atkPtr < 0) role = Role.Attack; break;
                    case TouchControlLayout.Hit.Move: if (_movePtr < 0) role = Role.Move; break;
                }
            }
            _roles[id] = role;
            switch (role)
            {
                case Role.Move: BeginMove(id, pos); break;
                case Role.Attack: BeginAttack(id, pos); break;
                case Role.Roll: BeginRoll(id); break;
                case Role.Skill: BeginSkill(id, pos); break;
                case Role.Grenade: BeginGrenade(id, pos); break;
            }
        }

        public void PointerMove(int id, Vector2 pos)
        {
            if (!_roles.TryGetValue(id, out var role)) return;
            switch (role)
            {
                case Role.Move: UpdateMove(pos); break;
                case Role.Attack: UpdateAttack(pos); break;
                case Role.Skill: UpdateSkill(pos); break;
                case Role.Grenade: UpdateGrenade(pos); break;
            }
        }

        public void PointerUp(int id, Vector2 pos)
        {
            if (!_roles.TryGetValue(id, out var role)) return;
            _roles.Remove(id);
            switch (role)
            {
                case Role.Move: UpdateMove(pos); EndMove(); break;
                case Role.Attack: UpdateAttack(pos); EndAttack(); break;
                case Role.Roll: RollPressed = false; _rollPtr = -1; break;
                case Role.Skill: UpdateSkill(pos); EndSkill(pos); break;
                case Role.Grenade: UpdateGrenade(pos); EndGrenade(); break;
            }
        }

        /// <summary>Releases everything without firing / throwing (pause, receiver change, focus loss).</summary>
        public void CancelAll()
        {
            if (MoveActive) EndMove();
            if (AttackActive) { _rx?.OnAimReleaseCommand(new AimReleaseCommand(AimDir, false, Now)); ResetAttack(); }
            if (_pending) { _pending = false; _rx?.OnAimReleaseCommand(new AimReleaseCommand(_pendingDir, false, Now)); }
            if (GrenadeActive)
            {
                if (_ctx.IsGrenadeAiming) _rx?.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(GrenadeTarget, false, Now));
                GrenadeActive = false; GrenadeAiming = false; _grPtr = -1;
            }
            if (SkillActive)
            {
                if (_ctx.IsUsingSkill) _rx?.OnSkillReleaseCommand(new SkillReleaseCommand(_ctx.CharacterPosition, 0f, Now));
                SkillActive = false; SkillAiming = false; _skPtr = -1;
            }
            RollPressed = false; _rollPtr = -1;
            _roles.Clear();
        }

        /// <summary>Per-frame: queued shots, row-change hold, hold skills, grenade target following the character.</summary>
        public void Tick(float dt)
        {
            float now = Now;
            if (_pending)
            {
                if (ReadyToFire()) { _pending = false; SendShot(_pendingDir); }
                else if (now > _pendingUntil)
                {
                    _pending = false;
                    ShotsDropped++;
                    _rx?.OnAimReleaseCommand(new AimReleaseCommand(_pendingDir, false, now));
                }
            }

            // Hold-to-repeat weapons (Gatling): while the aim is held outside the deadzone, fire at the weapon's interval
            if (AttackActive && AttackAiming && _ctx.HoldToRepeat && !_pending && ReadyToFire())
            {
                ShotsRequested++;
                RepeatShots++;
                _atkRepeats++;
                SendShot(AimDir);
                if (!_ctx.IsChargeWeapon) _rx?.OnAimStartCommand(new AimStartCommand(_atkStart, now)); // keep aiming
            }

            if (MoveActive && _rowDir != 0 && !_rowSent)
            {
                _rowHold += dt;
                if (_rowHold >= _d.rowChangeHoldTime)
                {
                    _rowSent = true; // one row per push; come back under the threshold to go again
                    _rx?.OnRowChangeCommand(new RowChangeCommand(_rowDir, now));
                }
            }

            if (SkillActive && _skStarted && _ctx.HasSkill && _ctx.SkillType == SkillUseType.Hold && _ctx.IsUsingSkill)
                _rx?.OnSkillHoldCommand(new SkillHoldCommand(now - _skDown, AimDir, now));

            if (SkillActive && SkillAiming) SkillTarget = _ctx.ClampToField(_ctx.CharacterPosition + _skDir * _skDist);
            if (GrenadeActive && GrenadeAiming) GrenadeTarget = _ctx.ClampToField(_ctx.CharacterPosition + _grDir * _grDist);
        }

        // ===================================== move =========================================================
        private void BeginMove(int id, Vector2 pos)
        {
            _movePtr = id;
            MoveActive = true;
            MoveOrigin = TouchControlLayout.ClampCircle(pos, _L.MoveRadius, _L.Safe);
            MoveKnob = pos;
            MoveDir = Vector2.zero; MoveMag = 0f;
            _rowDir = 0; _rowHold = 0f; _rowSent = false;
            UpdateMove(pos);
        }

        private void UpdateMove(Vector2 pos)
        {
            if (!MoveActive) return;
            float R = Mathf.Max(1f, _L.MoveRadius);
            Vector2 delta = pos - MoveOrigin;
            if (_d.moveStickFollow && delta.magnitude > R)
            {
                MoveOrigin += delta.normalized * (delta.magnitude - R);
                delta = pos - MoveOrigin;
            }
            MoveKnob = MoveOrigin + Vector2.ClampMagnitude(delta, R);
            float mag = Mathf.Clamp01(delta.magnitude / R);
            if (mag < _d.moveDeadzone)
            {
                if (MoveMag > 0f) _rx?.OnMoveCommand(MoveCommand.Stop(Now));
                MoveDir = Vector2.zero; MoveMag = 0f;
            }
            else
            {
                Vector2 dir = delta.normalized;
                if (MoveMag <= 0f || Vector2.Angle(dir, MoveDir) > 0.5f || Mathf.Abs(mag - MoveMag) > 0.01f)
                {
                    MoveDir = dir; MoveMag = mag;
                    _rx?.OnMoveCommand(new MoveCommand(dir, mag, Now));
                }
            }
            Vector2 stick = MoveDir * MoveMag;
            int rdir = stick.y >= _d.rowChangeThreshold ? 1 : (stick.y <= -_d.rowChangeThreshold ? -1 : 0);
            if (rdir != _rowDir) { _rowDir = rdir; _rowHold = 0f; _rowSent = false; }
        }

        private void EndMove()
        {
            _movePtr = -1;
            MoveActive = false;
            MoveDir = Vector2.zero; MoveMag = 0f;
            _rowDir = 0; _rowHold = 0f; _rowSent = false;
            _rx?.OnMoveCommand(MoveCommand.Stop(Now));
        }

        // ===================================== attack =======================================================
        private void BeginAttack(int id, Vector2 pos)
        {
            if (_pending)
            {
                // a new press flushes the queued shot (fire if legal now, otherwise drop it)
                _pending = false;
                if (ReadyToFire()) SendShot(_pendingDir);
                else { ShotsDropped++; _rx?.OnAimReleaseCommand(new AimReleaseCommand(_pendingDir, false, Now)); }
            }
            _atkPtr = id;
            AttackActive = true;
            AttackAiming = false;
            AttackDelta = Vector2.zero;
            _atkStart = pos;            // aim is measured from where the thumb lands, not the stick centre
            _atkDown = Now;
            _atkAimedOut = false;
            _atkRepeats = 0;
            _rx?.OnAimStartCommand(new AimStartCommand(pos, Now));
        }

        private void UpdateAttack(Vector2 pos)
        {
            if (!AttackActive) return;
            float R = Mathf.Max(1f, _L.AttackRadius);
            Vector2 delta = pos - _atkStart;
            AttackDelta = Vector2.ClampMagnitude(delta, R);
            bool outside = delta.magnitude > _d.attackDeadzone * R;
            if (outside)
            {
                _atkAimedOut = true;
                AimDir = delta.normalized;
            }
            bool changed = outside != AttackAiming;
            AttackAiming = outside;
            if (outside || changed)
            {
                // Charge weapons start charging once the aim leaves the deadzone; auto weapons only aim here (no shot)
                _rx?.OnAimDragCommand(new AimDragCommand(AimDir, delta.magnitude, outside && _ctx.IsChargeWeapon, Now));
            }
        }

        private void EndAttack()
        {
            if (_atkRepeats > 0) _rx?.OnAimReleaseCommand(new AimReleaseCommand(AimDir, false, Now)); // repeat fire stops on release
            else if (AttackAiming) Fire(AimDir, false);
            else if (!_atkAimedOut && _d.tapAutoAim && Now - _atkDown <= _d.tapMaxTime) Fire(AutoAimDirection(), true);
            else _rx?.OnAimReleaseCommand(new AimReleaseCommand(AimDir, false, Now)); // dragged back to centre = cancel
            ResetAttack();
        }

        private void ResetAttack()
        {
            _atkPtr = -1;
            AttackActive = false;
            AttackAiming = false;
            AttackDelta = Vector2.zero;
        }

        /// <summary>Direction to the nearest enemy (in range first), or straight ahead when there is none.</summary>
        public Vector2 AutoAimDirection()
        {
            Vector2 from = _ctx.CharacterPosition;
            Vector2? t = _ctx.NearestEnemy(_ctx.WeaponRange * _d.autoAimRangeMul) ?? _ctx.NearestEnemy(float.MaxValue);
            if (t.HasValue && (t.Value - from).sqrMagnitude > 0.0001f) return (t.Value - from).normalized;
            return Vector2.up;
        }

        private void Fire(Vector2 dir, bool auto)
        {
            ShotsRequested++;
            if (auto) AutoAimShots++;
            AimDir = dir;
            if (ReadyToFire()) { SendShot(dir); return; }
            _pending = true;
            _pendingDir = dir;
            _pendingUntil = Now + _d.fireQueueWindow;
            ShotsQueued++;
        }

        private bool ReadyToFire() => _ctx.CanFireNow && Now - _lastShot >= _ctx.FireInterval - FireIntervalSlack;

        private void SendShot(Vector2 dir)
        {
            float now = Now;
            float R = Mathf.Max(1f, _L.AttackRadius);
            // Re-arm the aim if the press-time AimStart was refused (e.g. pressed mid-roll). Charge weapons keep their
            // original start so the charge time counts from the touch-down.
            if (!_ctx.IsChargeWeapon || !_ctx.IsAiming) _rx?.OnAimStartCommand(new AimStartCommand(_atkStart, now));
            _rx?.OnAimDragCommand(new AimDragCommand(dir, R, true, now));
            _rx?.OnAimReleaseCommand(new AimReleaseCommand(dir, true, now));
            _lastShot = now;
            ShotsSent++;
        }

        // ===================================== roll =========================================================
        private void BeginRoll(int id)
        {
            _rollPtr = id;
            RollPressed = true;
            // current stick direction while moving; zero = the character uses its facing (01 §9)
            Vector2 dir = MoveActive && MoveMag >= _d.moveDeadzone ? MoveDir : Vector2.zero;
            _rx?.OnRollCommand(new RollCommand(dir, Now));
            Rolls++;
        }

        // ===================================== skill ========================================================
        private void BeginSkill(int id, Vector2 pos)
        {
            _skPtr = id;
            SkillActive = true;
            SkillAiming = false;
            _skStart = pos;
            _skDown = Now;
            _skDir = Vector2.up; _skDist = 0f;
            SkillTarget = _ctx.CharacterPosition;
            _skStarted = _ctx.HasSkill;
            if (!_skStarted) return;
            SkillPresses++;
            _rx?.OnSkillStartCommand(new SkillStartCommand(pos, Now));
        }

        private void UpdateSkill(Vector2 pos)
        {
            if (!SkillActive || !_skStarted || _ctx.SkillType != SkillUseType.DragRelease) return;
            Vector2 delta = pos - _skStart;
            SkillAiming = delta.magnitude > _L.GrenadeDeadzonePx;
            if (!SkillAiming) { _skDist = 0f; SkillTarget = _ctx.CharacterPosition; return; }
            _skDir = delta.normalized;
            _skDist = Mathf.Clamp01(delta.magnitude / _L.SkillDragFullPx) * _ctx.SkillRange;
            SkillTarget = _ctx.ClampToField(_ctx.CharacterPosition + _skDir * _skDist);
        }

        private void EndSkill(Vector2 pos)
        {
            // Tap skills already fired on press; only hold / drag skills get a release, and only while the character
            // is really in its skill state (a stray release would reset an aim the other thumb is holding).
            if (_skStarted && _ctx.SkillType != SkillUseType.Tap && _ctx.IsUsingSkill)
            {
                Vector2 target = _ctx.SkillType == SkillUseType.DragRelease ? SkillTarget : _ctx.CharacterPosition;
                _rx?.OnSkillReleaseCommand(new SkillReleaseCommand(target, Now - _skDown, Now));
            }
            _skPtr = -1;
            SkillActive = false;
            SkillAiming = false;
            _skStarted = false;
        }

        // ===================================== grenade ======================================================
        private void BeginGrenade(int id, Vector2 pos)
        {
            _grPtr = id;
            GrenadeActive = true;
            GrenadeAiming = false;
            GrenadeDelta = Vector2.zero;
            _grStart = pos;
            _grDir = Vector2.up;
            _grDist = _ctx.GrenadeMinRange;
            GrenadeTarget = _ctx.ClampToField(_ctx.CharacterPosition + _grDir * _grDist);
            if (_ctx.HasGrenade) _rx?.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(pos, Now));
        }

        private void UpdateGrenade(Vector2 pos)
        {
            if (!GrenadeActive) return;
            Vector2 delta = pos - _grStart;
            float full = _L.GrenadeDragFullPx, dz = _L.GrenadeDeadzonePx;
            GrenadeDelta = Vector2.ClampMagnitude(delta, full);
            GrenadeAiming = delta.magnitude > dz;
            if (!GrenadeAiming) return;
            _grDir = delta.normalized;
            float t = Mathf.Clamp01((delta.magnitude - dz) / Mathf.Max(1f, full - dz));
            _grDist = Mathf.Lerp(_ctx.GrenadeMinRange, _ctx.GrenadeMaxRange, t);
            GrenadeTarget = _ctx.ClampToField(_ctx.CharacterPosition + _grDir * _grDist);
            _rx?.OnGrenadeAimDragCommand(new GrenadeAimDragCommand(_grDir, _grDist, Now));
        }

        private void EndGrenade()
        {
            if (_ctx.IsGrenadeAiming)
            {
                // release inside the deadzone = cancel (01 state table); outside = throw at the shown landing point
                _rx?.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(GrenadeTarget, GrenadeAiming, Now));
                if (GrenadeAiming) GrenadesThrown++;
            }
            _grPtr = -1;
            GrenadeActive = false;
            GrenadeAiming = false;
            GrenadeDelta = Vector2.zero;
        }
    }
}

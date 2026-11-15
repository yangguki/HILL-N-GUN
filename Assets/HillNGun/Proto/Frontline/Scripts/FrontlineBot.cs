using System.Collections.Generic;
using UnityEngine;
using HillNGun.Battlefield;
using HillNGun.Characters;
using HillNGun.Core;
using HillNGun.Cover;
using HillNGun.Input;
using HillNGun.PvE;

namespace HillNGun.Proto.Frontline
{
    /// <summary>
    /// Prefer front (midline-nearest) cover → peek/fire → return. Rolls when hit.
    /// Idle 3–5 s without engagement → switch cover and advance toward the line.
    /// 3v3: each bot claims a distinct cover slot (no shared midline pile-up).
    /// Does not change HP/damage/range/fire interval/round length.
    /// </summary>
    public sealed class FrontlineBot
    {
        enum Phase
        {
            ToCover,
            Hold,
            Peek,
            Return
        }

        readonly CharacterStateMachine _self;
        readonly AllyFreeSteer _steer = new AllyFreeSteer();
        readonly int _slot;
        readonly FrontlineLayout _layout;
        float _grenadeCd;
        float _lastShot = -999f;
        bool _moved;
        Vector2 _lastPos;
        Phase _phase = Phase.ToCover;
        float _phaseUntil;
        Vector2 _coverHold;
        Vector2 _peekPos;
        bool _wantRoll;
        Vector2 _rollDir = Vector2.right;
        float _hpSeen = -1f;
        CoverInstance _cover;
        float _lastEngage;
        float _idleLimit;
        int _coverSalt;

        public CharacterStateMachine Self => _self;
        public CoverInstance ClaimedCover => _cover;

        public FrontlineBot(CharacterStateMachine self, int slot, FrontlineLayout layout)
        {
            _self = self;
            _slot = slot;
            _layout = layout;
            _grenadeCd = 1.4f + slot * 0.35f;
            _phaseUntil = Time.time + 0.15f + slot * 0.1f;
            if (_self != null) _hpSeen = _self.CurrentHp;
            _lastEngage = Time.time;
            _idleLimit = Random.Range(3f, 5f);
            _rollDir = _layout.TowardEnemy(_self != null ? _self.Team : Team.Player);
        }

        public void Tick(
            float dt, float now, List<CharacterStateMachine> foes, IReadOnlyList<CoverInstance> covers,
            HashSet<CoverInstance> claimed)
        {
            if (_self == null || _self.OverallState != CharacterOverallState.Active) return;
            if (_self.MovementState == MovementState.Rolling)
            {
                _lastPos = _self.Position;
                return;
            }

            if (_hpSeen < 0f) _hpSeen = _self.CurrentHp;
            if (_self.CurrentHp < _hpSeen - 0.5f)
            {
                _wantRoll = true;
                MarkEngage(now);
                var foeHit = Nearest(foes);
                if (foeHit != null)
                {
                    Vector2 away = _self.Position - foeHit.Position;
                    _rollDir = away.sqrMagnitude > 1e-4f
                        ? away.normalized
                        : -_layout.TowardEnemy(_self.Team);
                }
            }
            _hpSeen = _self.CurrentHp;

            if (_wantRoll && _self.MovementState != MovementState.Rolling)
            {
                _wantRoll = false;
                _self.OnRollCommand(new RollCommand(_rollDir, now));
                _phase = Phase.ToCover;
                _phaseUntil = now + 0.35f;
                _lastPos = _self.Position;
                return;
            }

            float step = Mathf.Max(0.05f, (_self.Data != null ? _self.Data.moveSpeed : 3f) * dt);
            float progress = _moved ? Mathf.Clamp01((_self.Position - _lastPos).magnitude / step) : 1f;
            _lastPos = _self.Position;
            var foe = Nearest(foes);
            float radius = _self.BodyRadius;
            float range = _self.Data != null && _self.Data.weapon != null ? _self.Data.weapon.range : 8f;

            if (now - _lastEngage >= _idleLimit)
            {
                _coverSalt++;
                _cover = null;
                PickCover(covers, foe, radius, true, claimed);
                _phase = Phase.ToCover;
                _phaseUntil = now + 4f;
                _lastEngage = now;
                _idleLimit = Random.Range(3f, 5f);
            }
            else
                PickCover(covers, foe, radius, false, claimed);

            if (_phase == Phase.ToCover || _phase == Phase.Return)
            {
                if ((_self.Position - _coverHold).sqrMagnitude <= 0.35f * 0.35f || now >= _phaseUntil + 5f)
                {
                    _phase = Phase.Hold;
                    // Shorter hold → more time peeking/shooting (target 45–75 s rounds).
                    _phaseUntil = now + Random.Range(0.25f, 0.55f) + _slot * 0.08f;
                    _self.OnMoveCommand(MoveCommand.Stop(now));
                }
                else
                    MoveToward(_coverHold, dt, progress, radius, now);
            }
            else if (_phase == Phase.Hold)
            {
                _self.OnMoveCommand(MoveCommand.Stop(now));
                if (now >= _phaseUntil)
                {
                    _phase = Phase.Peek;
                    _phaseUntil = now + Random.Range(1.7f, 2.7f);
                }
            }
            else // Peek
            {
                if ((_self.Position - _peekPos).sqrMagnitude > 0.3f * 0.3f)
                    MoveToward(_peekPos, dt, progress, radius, now);
                else
                    _self.OnMoveCommand(MoveCommand.Stop(now));

                if (foe != null)
                {
                    Vector2 aim = foe.Position - _self.Position;
                    if (aim.sqrMagnitude < 1e-4f) aim = _layout.TowardEnemy(_self.Team);
                    aim.Normalize();
                    bool inRange = Vector2.Distance(_self.Position, foe.Position) <= range;
                    if (inRange && Fire(aim, now)) MarkEngage(now);
                    _grenadeCd -= dt;
                    if (_grenadeCd <= 0f && inRange && _self.HasGrenadeReady)
                    {
                        float gap = Vector2.Distance(_self.Position, foe.Position);
                        var grenade = _self.Data != null ? _self.Data.grenade : null;
                        float min = grenade != null ? grenade.minRange : 2f;
                        float max = grenade != null ? grenade.maxRange : 9f;
                        if (gap >= min && gap <= max && _self.BeginAiGrenadeThrow(foe.Position, 0.15f))
                        {
                            MarkEngage(now);
                            _grenadeCd = grenade != null ? grenade.rechargeTime : 12f;
                        }
                    }
                }

                if (now >= _phaseUntil)
                {
                    _phase = Phase.Return;
                    _phaseUntil = now + 1.6f;
                }
            }
        }

        void MarkEngage(float now)
        {
            _lastEngage = now;
            _idleLimit = Random.Range(3f, 5f);
        }

        void MoveToward(Vector2 goal, float dt, float progress, float radius, float now)
        {
            Vector2 delta = goal - _self.Position;
            if (delta.magnitude < 0.25f)
            {
                _self.OnMoveCommand(MoveCommand.Stop(now));
                return;
            }
            Vector2 dir = delta.normalized;
            var world = FreeMoveWorld.Active;
            dir = _steer.Steer(goal, dir, dt, _moved, progress, world, _self.Position, radius);
            _self.OnMoveCommand(new MoveCommand(dir, 1f, now));
            _moved = true;
        }

        void PickCover(
            IReadOnlyList<CoverInstance> covers, CharacterStateMachine foe, float radius, bool advance,
            HashSet<CoverInstance> claimed)
        {
            Vector2 toward = _layout.TowardEnemy(_self.Team);
            Vector2 fallback = FallbackHold();
            CoverInstance best = null;
            float bestScore = float.MaxValue;
            float heldLine = _cover != null ? _layout.DistToLine(_cover.Position) : float.MaxValue;
            if (covers != null)
            {
                for (int i = 0; i < covers.Count; i++)
                {
                    var c = covers[i];
                    if (c == null || c.IsDestroyed) continue;
                    if (c.Team != _self.Team) continue;
                    if (!_layout.OnOwnSide(c.Position, 0.1f, _self.Team)) continue;
                    if (advance && _cover != null && c == _cover) continue;
                    // Unique slot: skip covers already claimed by a living teammate (except our own).
                    if (claimed != null && c != _cover && claimed.Contains(c)) continue;

                    float toLine = _layout.DistToLine(c.Position);
                    float lane = LaneDelta(c.Position);
                    float score = toLine * 3.2f + lane * 0.9f + ((i + _coverSalt) % 3) * 0.05f;
                    // Prefer the lane matching this bot's spawn slot so 3v3 spreads out.
                    score += Mathf.Abs(lane - SlotLane()) * 1.4f;
                    if (foe != null) score += Vector2.Distance(c.Position, foe.Position) * 0.05f;
                    if (advance && toLine >= heldLine - 0.05f) score += 2.5f;
                    if (score < bestScore) { bestScore = score; best = c; }
                }
            }

            if (best == null)
            {
                if (_cover != null && claimed != null) claimed.Remove(_cover);
                _cover = null;
                float hold = advance
                    ? Mathf.Lerp(HoldCoord(fallback), _layout.LineCoord, 0.35f)
                    : HoldCoord(fallback);
                _coverHold = _layout.ClampSide(WithHold(fallback, hold), radius, _self.Team);
                _peekPos = _layout.ClampSide(_coverHold + toward * 1.25f, radius, _self.Team);
                return;
            }

            if (_cover != best && claimed != null)
            {
                if (_cover != null) claimed.Remove(_cover);
                claimed.Add(best);
            }
            else if (claimed != null)
                claimed.Add(best);
            _cover = best;
            Vector2 away = -toward;
            float half = CoverHalfAlongSplit(best);
            _coverHold = _layout.ClampSide(
                best.Position + away * (half + radius + 0.12f),
                radius, _self.Team);
            float sideNudge = (_slot - 1) * 0.18f;
            Vector2 lateral = _layout.IsTopBottom ? new Vector2(sideNudge, 0f) : new Vector2(0f, sideNudge);
            _peekPos = _layout.ClampSide(
                best.Position + toward * (half + radius + 0.7f) + lateral,
                radius, _self.Team);
        }

        Vector2 FallbackHold()
        {
            if (_layout.IsTopBottom)
            {
                float y = _self.Team == Team.Player ? _layout.PlayerHold : _layout.EnemyHold;
                float x = Mathf.Clamp(_self.Position.x, _layout.MinX + 1.1f, _layout.MaxX - 1.1f);
                // Bias fallback toward this slot's lane.
                x = Mathf.Lerp(x, _layout.MidX + SlotLane(), 0.65f);
                return new Vector2(x, y);
            }
            float xL = _self.Team == Team.Player ? _layout.PlayerHold : _layout.EnemyHold;
            float yL = Mathf.Clamp(_self.Position.y, _layout.MinY + 1.1f, _layout.MaxY - 1.1f);
            yL = Mathf.Lerp(yL, _layout.MidY + SlotLane(), 0.65f);
            return new Vector2(xL, yL);
        }

        float SlotLane()
        {
            // slot 0 center, 1 left/west, 2 right/east (or south/north in left-right mode).
            if (_slot == 1) return -3.1f;
            if (_slot == 2) return 3.1f;
            return 0f;
        }

        float LaneDelta(Vector2 p) => _layout.IsTopBottom ? (p.x - _layout.MidX) : (p.y - _layout.MidY);

        float HoldCoord(Vector2 p) => _layout.IsTopBottom ? p.y : p.x;

        Vector2 WithHold(Vector2 p, float hold) =>
            _layout.IsTopBottom ? new Vector2(p.x, hold) : new Vector2(hold, p.y);

        float CoverHalfAlongSplit(CoverInstance c)
        {
            if (c == null) return 0.35f;
            if (_layout.IsTopBottom) return Mathf.Max(0.35f, c.Data != null ? c.Data.depth * 0.5f : 0.35f);
            return Mathf.Max(0.35f, c.Width * 0.5f);
        }

        bool Fire(Vector2 aim, float now)
        {
            float interval = _self.Data != null && _self.Data.weapon != null ? _self.Data.weapon.fireInterval : 0.6f;
            if (now - _lastShot < interval) return false;
            _lastShot = now;
            _self.OnAimStartCommand(new AimStartCommand(Vector2.zero, now));
            _self.OnAimDragCommand(new AimDragCommand(aim, 1f, true, now));
            _self.OnAimReleaseCommand(new AimReleaseCommand(aim, true, now));
            return true;
        }

        CharacterStateMachine Nearest(List<CharacterStateMachine> foes)
        {
            CharacterStateMachine best = null;
            float bestD = float.MaxValue;
            if (foes == null) return null;
            for (int i = 0; i < foes.Count; i++)
            {
                var f = foes[i];
                if (f == null || f.OverallState != CharacterOverallState.Active) continue;
                float d = (f.Position - _self.Position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = f; }
            }
            return best;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxGrenade
    {
        enum Phase { Idle, Flying, Cooldown, ReadyFlash }

        const float ReadyFlashFps = 10f;
        const int ReadyFlashCycles = 3;

        Phase _phase = Phase.Idle;
        float _phaseDoneTime;
        float _flightStartTime;
        Vector2 _startGround;
        Vector2 _aimGround;
        float _cooldownStartTime;
        float _cooldownDoneTime;
        float _readyFlashDoneTime;
        float _grenadeCooldown = Proto_GreyboxTuning.GrenadeCooldown;
        Vector2 _visualGround;
        float _explosionVisUntil;
        bool _justBecameReady;

        public bool IsFlying => _phase == Phase.Flying;
        public bool CanThrow(float time) => _phase == Phase.Idle;
        public bool JustBecameReady => _justBecameReady;
        public Vector2 VisualGround => _visualGround;
        public Vector2 StartGround => _startGround;
        public Vector2 AimGround => _aimGround;
        public float FlightStartTime => _flightStartTime;

        public float FlightProgress01(float time)
        {
            if (_phase != Phase.Flying) return 0f;
            return Mathf.Clamp01((time - _flightStartTime) / Proto_GreyboxTuning.GrenadeFlightTime);
        }

        /// <summary>0 during flight; 0–1 while cooldown fills; 1 during ready flash.</summary>
        public float RechargeProgress01(float time)
        {
            if (_phase == Phase.Flying) return 0f;
            if (_phase == Phase.Cooldown)
            {
                float span = Mathf.Max(_grenadeCooldown, 1e-5f);
                return Mathf.Clamp01((time - _cooldownStartTime) / span);
            }

            if (_phase == Phase.ReadyFlash) return 1f;
            return 0f;
        }

        public bool IsInReadyFlash(float time) => _phase == Phase.ReadyFlash;

        public float ReadyFlashFrame01(float time)
        {
            if (_phase != Phase.ReadyFlash) return 0f;
            float flashStart = _cooldownDoneTime;
            float t = time - flashStart;
            int frame = Mathf.FloorToInt(t * ReadyFlashFps) % 2;
            return frame;
        }

        public Vector2 ArcVisualGround(float time)
        {
            float t = FlightProgress01(time);
            var flat = Vector2.Lerp(_startGround, _aimGround, t);
            float lift = Proto_GreyboxTuning.GrenadeArcVisualHeight * 4f * t * (1f - t);
            return flat + new Vector2(0f, lift);
        }
        public bool ExplosionVisible(float time) => time < _explosionVisUntil;
        public float ExplosionFadeT(float time) =>
            _explosionVisUntil <= time ? 0f :
            1f - (_explosionVisUntil - time) / Proto_GreyboxTuning.ExplosionVisualDuration;
        public float ExplosionRadius => Proto_GreyboxTuning.GrenadeRadius;

        public void ApplyRuntime(Proto_GreyboxRuntimeTuning tuning) => _grenadeCooldown = tuning.GrenadeCooldown;

        public bool TryThrow(float time, Vector2 playerFoot, Vector2 aimGround)
        {
            if (!CanThrow(time)) return false;
            _startGround = playerFoot;
            _aimGround = aimGround;
            _flightStartTime = time;
            _phaseDoneTime = time + Proto_GreyboxTuning.GrenadeFlightTime;
            _phase = Phase.Flying;
            _visualGround = playerFoot;
            _justBecameReady = false;
            return true;
        }

        public void Tick(float time, float dt, IList<Proto_GreyboxTarget> targets, Proto_GreyboxCover cover,
            System.Action<float, float, float> destroyCratePercent = null)
        {
            _justBecameReady = false;
            if (_phase == Phase.Flying)
            {
                float t = Mathf.Clamp01((time - _flightStartTime) / Proto_GreyboxTuning.GrenadeFlightTime);
                _visualGround = Vector2.Lerp(_startGround, _aimGround, t);
                if (time >= _phaseDoneTime)
                {
                    _phase = Phase.Cooldown;
                    _cooldownStartTime = time;
                    _cooldownDoneTime = time + _grenadeCooldown;
                    Detonate(_visualGround, targets, cover, destroyCratePercent, time);
                }
            }
            else if (_phase == Phase.Cooldown && time >= _cooldownDoneTime)
            {
                _phase = Phase.ReadyFlash;
                float flashDur = (2f * ReadyFlashCycles) / ReadyFlashFps;
                _readyFlashDoneTime = time + flashDur;
            }
            else if (_phase == Phase.ReadyFlash && time >= _readyFlashDoneTime)
            {
                _phase = Phase.Idle;
                _justBecameReady = true;
            }
        }

        void Detonate(Vector2 center, IList<Proto_GreyboxTarget> targets, Proto_GreyboxCover cover,
            System.Action<float, float, float> destroyCratePercent, float time)
        {
            _explosionVisUntil = time + Proto_GreyboxTuning.ExplosionVisualDuration;
            float r = Proto_GreyboxTuning.GrenadeRadius;
            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t == null || !t.IsAlive) continue;
                    if ((t.FootPosition - center).magnitude <= r)
                        t.ApplyDamage(Proto_GreyboxTuning.GrenadeDamage);
                }
            }

            TryRemoveCrate(center, cover, 52f, 56.5f, 36f, destroyCratePercent);
            TryRemoveCrate(center, cover, 12f, 21f, 63f, destroyCratePercent);
            TryRemoveCrate(center, cover, 58f, 69f, 82f, destroyCratePercent);
        }

        static void TryRemoveCrate(Vector2 center, Proto_GreyboxCover cover, float x0, float x1, float yBottom,
            System.Action<float, float, float> destroyCratePercent)
        {
            float cx = (Proto_GreyboxLayout.ScreenXPercentToWorldX(x0) +
                        Proto_GreyboxLayout.ScreenXPercentToWorldX(x1)) * 0.5f;
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(yBottom);
            var crateCenter = new Vector2(cx, footY);
            if ((crateCenter - center).magnitude <= Proto_GreyboxTuning.GrenadeRadius)
            {
                cover.RemoveCrateMatching(x0, x1, yBottom);
                destroyCratePercent?.Invoke(x0, x1, yBottom);
            }
        }
    }
}
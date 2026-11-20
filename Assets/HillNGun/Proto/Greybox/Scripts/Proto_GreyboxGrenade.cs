using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxGrenade
    {
        enum Phase { Idle, Flying, Cooldown }

        Phase _phase = Phase.Idle;
        float _phaseDoneTime;
        float _flightStartTime;
        Vector2 _startGround;
        Vector2 _aimGround;
        float _cooldownDoneTime;
        float _grenadeCooldown = Proto_GreyboxTuning.GrenadeCooldown;
        Vector2 _visualGround;
        float _explosionVisUntil;

        public bool IsFlying => _phase == Phase.Flying;
        public bool CanThrow(float time) => _phase == Phase.Idle;
        public Vector2 VisualGround => _visualGround;
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
            return true;
        }

        public void Tick(float time, float dt, IList<Proto_GreyboxTarget> targets, Proto_GreyboxCover cover,
            System.Action<float, float, float> destroyCratePercent = null)
        {
            if (_phase == Phase.Flying)
            {
                float t = Mathf.Clamp01((time - _flightStartTime) / Proto_GreyboxTuning.GrenadeFlightTime);
                _visualGround = Vector2.Lerp(_startGround, _aimGround, t);
                if (time >= _phaseDoneTime)
                {
                    _phase = Phase.Cooldown;
                    _cooldownDoneTime = time + _grenadeCooldown;
                    Detonate(_visualGround, targets, cover, destroyCratePercent, time);
                }
            }
            else if (_phase == Phase.Cooldown && time >= _cooldownDoneTime)
                _phase = Phase.Idle;
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
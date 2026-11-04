using UnityEngine;
using HillNGun.Core;
using HillNGun.Characters;
using HillNGun.Input;

namespace HillNGun.AI
{
    /// <summary>
    /// Pure-logic auto aim/fire driver for a character (PvE ally bots, smoke-test autopilot).
    /// Drives the character through the same input commands a player would send.
    /// Timing comes from the character's WeaponData (fireInterval, chargeMaxTime).
    /// </summary>
    public class AutoGunner
    {
        private readonly CharacterStateMachine _character;
        private float _cooldown;
        private bool _aiming;
        private float _aimStart;
        private Vector2 _lastDir = Vector2.up;

        public CharacterStateMachine Character => _character;
        public int ShotsAttempted { get; private set; }

        public AutoGunner(CharacterStateMachine character, float initialDelay = 0f)
        {
            _character = character;
            _cooldown = initialDelay;
        }

        /// <param name="target">World-logic target position, or null when nothing to shoot.</param>
        public void Tick(float dt, Vector2? target)
        {
            if (_character == null || _character.Data?.weapon == null) return;

            if (_character.OverallState != CharacterOverallState.Active)
            {
                _aiming = false;
                return;
            }

            var weapon = _character.Data.weapon;
            float now = Time.time;

            if (!target.HasValue)
            {
                if (_aiming) Release(now);
                return;
            }

            Vector2 dir = target.Value - _character.Position;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();
            _lastDir = dir;

            if (!_aiming)
            {
                _cooldown -= dt;
                if (_cooldown > 0f) return;

                _character.OnAimStartCommand(new AimStartCommand(Vector2.zero, now));
                _aiming = true;
                _aimStart = now;
                _character.OnAimDragCommand(new AimDragCommand(dir, 100f, true, now));
                ShotsAttempted++;

                if (weapon.fireMode != FireMode.ChargeRelease)
                {
                    Release(now);
                }
            }
            else
            {
                _character.OnAimDragCommand(new AimDragCommand(dir, 100f, true, now));
                float hold = Mathf.Max(weapon.chargeMaxTime, weapon.chargeMinTime, 0.05f);
                if (now - _aimStart >= hold)
                {
                    Release(now);
                }
            }
        }

        private void Release(float now)
        {
            _character.OnAimReleaseCommand(new AimReleaseCommand(_lastDir, true, now));
            _aiming = false;
            _cooldown = Mathf.Max(0.1f, _character.Data.weapon.fireInterval);
        }
    }
}

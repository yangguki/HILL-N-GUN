using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxRoll
    {
        float _distance;
        float _duration;
        float _invulnDuration;
        float _cooldownDuration;

        bool _active;
        float _elapsed;
        float _cooldownRemaining;
        Vector2 _direction;
        Vector2 _lastHorizontal = Vector2.right;

        public bool IsRolling => _active;
        public float CooldownRemaining => _cooldownRemaining;
        public float Elapsed => _elapsed;

        public Proto_GreyboxRoll(
            float distance = Proto_GreyboxTuning.RollDistance,
            float duration = Proto_GreyboxTuning.RollDuration,
            float invulnDuration = Proto_GreyboxTuning.RollInvulnDuration,
            float cooldownDuration = Proto_GreyboxTuning.RollCooldown)
        {
            _distance = distance;
            _duration = duration;
            _invulnDuration = invulnDuration;
            _cooldownDuration = cooldownDuration;
        }

        public void ApplyRuntime(Proto_GreyboxRuntimeTuning tuning)
        {
            _distance = tuning.RollDistance;
            _duration = tuning.RollDuration;
            _invulnDuration = tuning.RollInvulnDuration;
            _cooldownDuration = tuning.RollCooldown;
        }

        public void RememberMoveDirection(Vector2 move)
        {
            if (Mathf.Abs(move.x) > 0.01f)
                _lastHorizontal = move.x >= 0f ? Vector2.right : Vector2.left;
        }

        public bool TryStart(Vector2 moveDirection)
        {
            if (_active || _cooldownRemaining > 0f)
                return false;

            var dir = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : _lastHorizontal;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector2.right;

            _direction = dir.normalized;
            _active = true;
            _elapsed = 0f;
            return true;
        }

        public Vector2 Tick(float dt)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - dt);

            if (!_active)
                return Vector2.zero;

            float prev = _elapsed;
            _elapsed += dt;
            float t0 = prev / _duration;
            float t1 = Mathf.Min(_elapsed, _duration) / _duration;
            var delta = _direction * ((t1 - t0) * _distance);

            if (_elapsed >= _duration)
            {
                _active = false;
                _elapsed = 0f;
                _cooldownRemaining = _cooldownDuration;
            }

            return delta;
        }

        public bool IsInvulnerable => _active && _elapsed <= _invulnDuration;
    }
}
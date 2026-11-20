using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public enum Proto_GreyboxGruntBehavior
    {
        Strafe,
        Popup
    }

    public class Proto_GreyboxGrunt
    {
        public Proto_GreyboxTarget Target { get; }
        public Proto_GreyboxGruntBehavior Behavior { get; }
        public bool IsAtSlot { get; private set; }
        public bool IsHidden => Behavior == Proto_GreyboxGruntBehavior.Popup && _popupHidden;

        readonly Vector2 _spawnPos;
        readonly Vector2 _slotPos;
        readonly float _firePhase;
        readonly float _strafePhase;
        float _fireTimer;
        float _strafeT;
        bool _popupHidden;
        float _popupTimer;

        public Proto_GreyboxGrunt(Vector2 spawnPos, Vector2 slotPos, Proto_GreyboxGruntBehavior behavior,
            int hp, float firePhase, float strafePhase)
        {
            _spawnPos = spawnPos;
            _slotPos = slotPos;
            Behavior = behavior;
            _firePhase = firePhase;
            _strafePhase = strafePhase;
            Target = new Proto_GreyboxTarget(spawnPos, hp);
            _fireTimer = firePhase;
            _strafeT = strafePhase;
        }

        public void Tick(float dt, float walkSpeed, float strafeRange, float fireInterval, float bulletSpeed,
            Vector2 playerFoot)
        {
            if (!Target.IsAlive) return;

            if (!IsAtSlot)
            {
                var delta = _slotPos - Target.FootPosition;
                float step = walkSpeed * dt;
                if (delta.magnitude <= step)
                {
                    Target.FootPosition = _slotPos;
                    IsAtSlot = true;
                }
                else
                    Target.FootPosition += delta.normalized * step;
                return;
            }

            if (Behavior == Proto_GreyboxGruntBehavior.Strafe)
            {
                _strafeT += dt;
                float offset = Mathf.Sin((_strafeT + _strafePhase) * 2.2f) * strafeRange;
                Target.FootPosition = _slotPos + new Vector2(offset, 0f);
            }
            else
            {
                _popupTimer += dt;
                if (_popupHidden && _popupTimer > 1.2f)
                {
                    _popupHidden = false;
                    _popupTimer = 0f;
                }
                else if (!_popupHidden && _popupTimer > 0.9f)
                {
                    _popupHidden = true;
                    _popupTimer = 0f;
                }
            }

            FiredThisTick = false;
            if (!IsHidden)
            {
                _fireTimer += dt;
                if (_fireTimer >= fireInterval)
                {
                    _fireTimer = 0f;
                    if (TryFire(playerFoot, bulletSpeed))
                        FiredThisTick = true;
                }
            }
        }

        public bool FiredThisTick { get; private set; }

        public bool TryFire(Vector2 playerFoot, float bulletSpeed)
        {
            if (!IsAtSlot || IsHidden || !Target.IsAlive) return false;
            LastFireGround = Target.FootPosition;
            LastFireVelocity = ComputeShotVelocity(Target.FootPosition, playerFoot, bulletSpeed);
            return LastFireVelocity.sqrMagnitude > 1e-8f;
        }

        public Vector2 LastFireGround { get; private set; }
        public Vector2 LastFireVelocity { get; private set; }

        public static Vector2 ComputeShotVelocity(Vector2 fromFoot, Vector2 playerFoot, float speed)
        {
            var dir = playerFoot - fromFoot;
            float dist = dir.magnitude;
            return dist > 1e-5f ? dir / dist * speed : Vector2.zero;
        }

        public void ConfigureFireTimer(float interval) => _fireTimer %= interval;
    }
}
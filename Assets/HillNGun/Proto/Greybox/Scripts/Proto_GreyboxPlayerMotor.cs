using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxPlayerMotor
    {
        float _moveSpeed;

        public Vector2 FootPosition { get; private set; }
        public Vector2 Velocity { get; private set; }
        public Vector2 LastMoveDirection { get; private set; } = Vector2.right;

        public Proto_GreyboxPlayerMotor(Vector2 startFoot, float moveSpeed = Proto_GreyboxTuning.MoveSpeed)
        {
            _moveSpeed = moveSpeed;
            FootPosition = Proto_GreyboxMoveZone.Clamp(startFoot);
        }

        public void SetFoot(Vector2 foot) => FootPosition = Proto_GreyboxMoveZone.Clamp(foot);

        public void SetMoveSpeed(float speed) => _moveSpeed = speed;

        public void TickMove(Vector2 analog, float dt)
        {
            if (analog.sqrMagnitude > 1f)
                analog.Normalize();
            if (analog.sqrMagnitude > 0.0001f)
                LastMoveDirection = analog.normalized;

            Velocity = analog * _moveSpeed;
            FootPosition = Proto_GreyboxMoveZone.Clamp(FootPosition + Velocity * dt);
        }

        public void ApplyDelta(Vector2 delta) =>
            FootPosition = Proto_GreyboxMoveZone.Clamp(FootPosition + delta);
    }
}
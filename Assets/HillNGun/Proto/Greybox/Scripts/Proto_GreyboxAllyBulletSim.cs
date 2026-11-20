using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxAllyBulletSim
    {
        readonly float _radius;
        readonly float _speed;
        readonly int _damage;
        readonly Proto_GreyboxCover _cover;
        readonly float _maxRange;

        public Vector2 GroundPosition { get; private set; }
        public Vector2 GroundVelocity { get; private set; }
        public Vector2 StartPosition { get; }
        public bool Alive { get; private set; }

        public Proto_GreyboxAllyBulletSim(Vector2 startGround, Vector2 aimGround, Proto_GreyboxCover cover,
            float maxRange = 25f, float speed = -1f, float diameter = -1f, int damage = -1)
        {
            var dir = aimGround - startGround;
            float dist = dir.magnitude;
            _maxRange = maxRange;
            _speed = speed < 0f ? Proto_GreyboxTuning.AllyBulletSpeed : speed;
            _damage = damage < 0 ? Proto_GreyboxTuning.AllyBulletDamage : damage;
            float diam = diameter < 0f ? Proto_GreyboxTuning.AllyBulletDiameter : diameter;
            var vel = dist > 1e-5f ? dir / dist * _speed : Vector2.zero;
            _radius = diam * 0.5f;
            _cover = cover;
            StartPosition = startGround;
            GroundPosition = startGround;
            GroundVelocity = vel;
            Alive = vel.sqrMagnitude > 1e-8f;
        }

        public float VisualScale => Proto_DepthScale.BulletScaleAt(GroundPosition.y);

        float HitRadius => _radius * VisualScale;

        public Vector2 VisualPositionAtGround() =>
            GroundPosition + new Vector2(0f, _radius * 0.5f * VisualScale);

        public Proto_GreyboxTarget Tick(float dt, IList<Proto_GreyboxTarget> targets)
        {
            if (!Alive) return null;

            var next = GroundPosition + GroundVelocity * dt;
            if ((next - StartPosition).magnitude > _maxRange)
            {
                Alive = false;
                return null;
            }

            if (_cover.SegmentBlocked(GroundPosition, next, Proto_GreyboxTuning.AllyBulletNearCrateIgnore,
                    out _))
            {
                Alive = false;
                return null;
            }

            GroundPosition = next;

            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t == null || !t.IsAlive) continue;
                    if (CircleIntersectsRect(GroundPosition, HitRadius, Proto_GreyboxTarget.GroundFootprint(t.FootPosition)))
                    {
                        bool wasAlive = t.IsAlive;
                        t.ApplyDamage(_damage);
                        Alive = false;
                        return wasAlive ? t : null;
                    }
                }
            }

            return null;
        }

        static bool CircleIntersectsRect(Vector2 c, float r, Rect rect)
        {
            float cx = Mathf.Clamp(c.x, rect.xMin, rect.xMax);
            float cy = Mathf.Clamp(c.y, rect.yMin, rect.yMax);
            var dx = c.x - cx;
            var dy = c.y - cy;
            return dx * dx + dy * dy <= r * r;
        }
    }
}
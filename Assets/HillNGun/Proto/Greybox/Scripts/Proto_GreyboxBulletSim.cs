using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxBulletSim
    {
        const float PlayerFootprintDepthHalf = 0.25f;

        readonly float _radius;
        readonly Proto_GreyboxCover _cover;

        /// <summary>Ground-plane position (foot coordinates); combat resolves here, not on screen-space sprite bounds.</summary>
        public Vector2 GroundPosition { get; private set; }
        public Vector2 GroundVelocity { get; private set; }
        public bool Alive { get; private set; }

        public Proto_GreyboxBulletSim(Vector2 startGround, Vector2 velocityGround, Proto_GreyboxCover cover,
            float diameter = Proto_GreyboxTuning.EnemyBulletDiameter)
        {
            _radius = diameter * 0.5f;
            _cover = cover;
            GroundPosition = startGround;
            GroundVelocity = velocityGround;
            Alive = true;
        }

        public bool Tick(float dt, Rect playerGroundFootprint, bool playerInvulnerable, ref int playerHp,
            float coverDamageReduction01 = 1f, System.Random coverRng = null, int damagePerHit = -1)
        {
            if (!Alive)
                return false;

            int dmg = damagePerHit < 0 ? Proto_GreyboxTuning.PlayerDamagePerHit : damagePerHit;
            var next = GroundPosition + GroundVelocity * dt;
            if (_cover.SegmentBlocked(GroundPosition, next, out _))
            {
                Alive = false;
                if (!playerInvulnerable && coverDamageReduction01 < 1f && coverRng != null &&
                    coverRng.NextDouble() > coverDamageReduction01)
                    playerHp = Mathf.Max(0, playerHp - dmg);
                return false;
            }

            GroundPosition = next;

            if (CircleIntersectsRect(GroundPosition, HitRadius, playerGroundFootprint))
            {
                Alive = false;
                if (!playerInvulnerable)
                    playerHp = Mathf.Max(0, playerHp - dmg);
                return true;
            }

            return false;
        }

        public float VisualScale => Proto_DepthScale.BulletScaleAt(GroundPosition.y);

        public float HitRadius => _radius * VisualScale;

        public float VisualDiameter(float baseDiameter) => baseDiameter * VisualScale;

        public static Rect PlayerGroundFootprint(Vector2 foot)
        {
            float halfW = Proto_GreyboxLayout.WorldWidth * (Proto_GreyboxTuning.PlayerCapsuleWidthPercent / 100f) * 0.5f;
            return new Rect(foot.x - halfW, foot.y - PlayerFootprintDepthHalf, halfW * 2f, PlayerFootprintDepthHalf * 2f);
        }

        public static Vector2 VisualPositionAtGround(Vector2 groundPos)
        {
            float chest = Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.PlayerCapsuleHeightPercent) * 0.35f;
            return groundPos + new Vector2(0f, chest * Proto_DepthScale.BulletScaleAt(groundPos.y));
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
using UnityEngine;
using HillNGun.Battlefield;

namespace HillNGun.Combat
{
    /// <summary>
    /// The cover protection rule as pure maths, using the same tests as the projectile collision pass
    /// (cover rect expanded by bullet radius vs swept circle on the target; nearest contact wins).
    /// Cover height never enters the rule — only the logic footprint (width × depth) — so every scale preset
    /// protects a character tucked behind a cover in the same way.
    /// </summary>
    public static class CoverProtection
    {
        /// <summary>True when a bullet flying shooter → target touches the cover rect before the target circle.</summary>
        public static bool CoverStopsShot(Vector2 shooter, Vector2 target, float bulletRadius, float targetRadius, Rect cover)
        {
            bool hitsTarget = CoordinateHelper.SweptCircleCircleIntersection(shooter, target, bulletRadius, target, targetRadius, out float tTarget);
            bool hitsCover = CoordinateHelper.LineRectIntersection(shooter, target, cover.center, cover.width + bulletRadius * 2f, cover.height + bulletRadius * 2f, out float tCover);
            if (!hitsCover) return false;
            return !hitsTarget || tCover <= tTarget;
        }

        /// <summary>
        /// "Tucked behind" check: target stands on its row straight behind the cover at offset dx; the shot comes
        /// straight from the opposite side at the same x (row-to-row fire).
        /// </summary>
        public static bool ProtectsStraightShot(Rect cover, float coverRowY, float shooterY, float dx, float bulletRadius, float targetRadius)
        {
            float x = cover.center.x + dx;
            return CoverStopsShot(new Vector2(x, shooterY), new Vector2(x, coverRowY), bulletRadius, targetRadius, cover);
        }
    }
}

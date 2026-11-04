using UnityEngine;
using HillNGun.Core;

namespace HillNGun.Battlefield
{
    /// <summary>
    /// View-layer helper: the single logic(tile) -> world mapping used by every actor view
    /// (characters, enemies, boss, covers, projectiles, effects) plus depth sorting.
    /// Logic stays in tile space; only views call this.
    /// </summary>
    public static class FieldView
    {
        // Sorting bands (view constants, not gameplay numbers)
        public const int BackgroundOrder = -100;
        public const int ActorBaseOrder = 100;     // actors/covers: ActorBaseOrder + depth
        public const int ProjectileOrder = 3000;
        public const int OverlayOrder = 3100;      // labels, hp bars, warnings
        /// <summary>Ground decals (soft shadows, team rings): one constant band under every actor so they batch
        /// (same texture, same order) instead of interleaving with bodies by depth.</summary>
        public const int GroundDecalOrder = ActorBaseOrder - 4;

        private static BattlefieldSystem _cached;

        public static BattlefieldSystem Battlefield
        {
            get
            {
                if (_cached == null) _cached = ServiceLocator.Get<BattlefieldSystem>();
                return _cached;
            }
        }

        public static Vector3 ToWorld(Vector2 logicPos)
        {
            var bf = Battlefield;
            if (bf != null && bf.Data != null) return bf.LogicToWorldPosition(logicPos);
            // Fallback: assume default 24x18 field centred on origin
            return new Vector3(logicPos.x - 12f, logicPos.y - 9f, 0f);
        }

        public static float PerspectiveScale(float logicY)
        {
            var bf = Battlefield;
            return bf != null ? bf.GetPerspectiveScale(logicY) : 1f;
        }

        /// <summary>Lower on screen (smaller logic Y = closer to camera) draws on top.</summary>
        public static int DepthOrder(float logicY)
        {
            return ActorBaseOrder + Mathf.Clamp(Mathf.RoundToInt((40f - logicY) * 20f), 0, 2000);
        }

        /// <summary>Places a view transform at the logic position with perspective scale.</summary>
        public static void Place(Transform t, Vector2 logicPos, float baseScale = 1f)
        {
            t.position = ToWorld(logicPos);
            float s = PerspectiveScale(logicPos.y) * baseScale;
            t.localScale = new Vector3(s, s, 1f);
        }

        public static void ApplySorting(Renderer r, float logicY, int offset = 0)
        {
            if (r != null) r.sortingOrder = DepthOrder(logicY) + offset;
        }

        public static Bounds CameraBounds(Camera cam)
        {
            if (cam == null) return new Bounds(Vector3.zero, Vector3.zero);
            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            Vector3 c = cam.transform.position;
            return new Bounds(new Vector3(c.x, c.y, 0f), new Vector3(w, h, 1000f));
        }
    }
}

using UnityEngine;

namespace HillNGun.Proto.TopDown
{
    /// <summary>
    /// Designer pixel swap-in. Assign PNGs (or replace files at the same paths); presentation only.
    /// Facing: down = front, up = back, left/right = side flipped.
    /// </summary>
    [CreateAssetMenu(menuName = "HillNGun/Proto/Pixel Art", fileName = "ProtoPixelArt")]
    public class ProtoPixelArt : ScriptableObject
    {
        [System.Serializable]
        public class PoseSet
        {
            public Sprite front;
            public Sprite back;
            public Sprite side;

            public bool Ready => front != null && back != null && side != null;
        }

        [Header("Background (one plate per scene; swap the file to retarget)")]
        public Sprite background;

        [Tooltip("Dirt/walk UV on the BG plate (0-1, origin bottom-left). Empty (0,0)-(0,0) = full plate.")]
        public Vector2 dirtUvMin = Vector2.zero;
        public Vector2 dirtUvMax = Vector2.zero;

        [Header("Covers by kind")]
        public Sprite brick;
        public Sprite brickVertical;
        public Sprite hay;
        public Sprite hayDamaged;
        public Sprite hayDestroyed;
        public Sprite sandbag;
        public Sprite crate;
        public Sprite crateDamaged;
        public Sprite crateDestroyed;
        public Sprite barrel;

        [Header("Frontline divider (vertical = left/right split)")]
        public Sprite fenceDivider;
        public Sprite fenceCap;

        [Header("Frontline divider (horizontal = top/bottom split)")]
        public Sprite fenceDividerH;
        public Sprite fenceCapH;

        [Header("Hero / grunt poses")]
        public PoseSet buster;
        public PoseSet raven;
        public PoseSet volt;
        public PoseSet grunt;

        [Header("Hero / grunt flipbooks (stage1 idle/run/shoot)")]
        public ProtoFlipbookSet busterFlip;
        public ProtoFlipbookSet ravenFlip;
        public ProtoFlipbookSet voltFlip;
        public ProtoFlipbookSet gruntFlip;

        public bool HasDirtUv =>
            dirtUvMax.x > dirtUvMin.x + 0.01f && dirtUvMax.y > dirtUvMin.y + 0.01f;

        public ProtoFlipbookSet FlipForSlot(int slot)
        {
            if (slot == 1 && ravenFlip != null) return ravenFlip;
            if (slot == 2 && voltFlip != null) return voltFlip;
            return busterFlip;
        }

        public ProtoFlipbookSet EnemyFlip => gruntFlip;

        public Sprite CoverSprite(string kind) => CoverSprite(kind, 1f);

        /// <summary>Hay/crate swap s0→s1→s2 by durability ratio (view only).</summary>
        public Sprite CoverSprite(string kind, float hpRatio)
        {
            switch (kind)
            {
                case "brick": return brick;
                case "hay": return StageSprite(hay, hayDamaged, hayDestroyed, hpRatio) ?? sandbag;
                case "sandbag": return sandbag != null ? sandbag : hay;
                case "barrel": return barrel;
                default: return StageSprite(crate, crateDamaged, crateDestroyed, hpRatio);
            }
        }

        static Sprite StageSprite(Sprite intact, Sprite mid, Sprite low, float hpRatio)
        {
            if (hpRatio <= 0.01f) return low != null ? low : mid != null ? mid : intact;
            if (hpRatio <= 0.45f) return mid != null ? mid : intact;
            return intact;
        }

        public PoseSet HeroForSlot(int slot)
        {
            if (slot == 1 && raven != null && raven.Ready) return raven;
            if (slot == 2 && volt != null && volt.Ready) return volt;
            return buster != null && buster.Ready ? buster : null;
        }

        public PoseSet EnemyPoses => grunt != null && grunt.Ready ? grunt : null;

        public Sprite Pick(PoseSet set, Vector2 face, out bool flipX)
        {
            flipX = false;
            if (set == null || !set.Ready) return null;
            if (Mathf.Abs(face.x) > Mathf.Abs(face.y) * 0.85f)
            {
                flipX = face.x < 0f;
                return set.side;
            }
            return face.y >= 0f ? set.back : set.front;
        }
    }
}

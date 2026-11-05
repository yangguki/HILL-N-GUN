using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>View-only fire VFX contract (08_fire_effects.md). Separate from WeaponData combat numbers.</summary>
    [CreateAssetMenu(fileName = "WeaponVfxData", menuName = "HillNGun/Data/Weapon VFX")]
    public class WeaponVfxData : ScriptableObject
    {
        public string patternId = "heavy_slug";
        public string muzzleId = "muzzle_default";
        public string tracerId = "tracer_default";
        public string impactEnemyId = "impact_stars";
        public string impactCoverId = "impact_wood";
        public float shakeOnFire;
        public float shakeOnHit;
        public string sfxFire;
        public string sfxHit;
        public string sfxSpecial;
        [Tooltip("0 = use VfxPool default for the quality tier")]
        public int particleBudgetOverride;

        [Header("Pattern look (code-built, no external art)")]
        public Color muzzleColor = new Color(1f, 0.75f, 0.3f, 1f);
        public Color tracerColor = new Color(1f, 0.93f, 0.55f, 1f);
        public float tracerWidth = 0.28f;
        public float tracerLength = 1.5f;
        public float headSize = 0.55f;
        public bool isPellet;   // spread_fan: short yellow ovals
        public bool isNeedle;   // charge_beam: thin purple
        public bool isSlug;     // heavy_slug: thick blue-white

        public static WeaponVfxData Builtin(string patternId)
        {
            var v = CreateInstance<WeaponVfxData>();
            v.patternId = patternId;
            switch (patternId)
            {
                case "spread_fan":
                    v.muzzleColor = new Color(1f, 0.55f, 0.15f);
                    v.tracerColor = new Color(1f, 0.9f, 0.25f);
                    v.tracerWidth = 0.18f; v.tracerLength = 0.55f; v.headSize = 0.38f;
                    v.isPellet = true; v.impactEnemyId = "impact_stars"; v.impactCoverId = "impact_wood";
                    break;
                case "charge_beam":
                    v.muzzleColor = new Color(0.75f, 0.45f, 1f);
                    v.tracerColor = new Color(0.85f, 0.55f, 1f);
                    v.tracerWidth = 0.1f; v.tracerLength = 2.2f; v.headSize = 0.28f;
                    v.isNeedle = true; v.impactEnemyId = "impact_flash"; v.impactCoverId = "impact_wood";
                    break;
                default: // heavy_slug
                    v.patternId = "heavy_slug";
                    v.muzzleColor = new Color(0.45f, 0.75f, 1f);
                    v.tracerColor = new Color(0.75f, 0.9f, 1f);
                    v.tracerWidth = 0.36f; v.tracerLength = 1.1f; v.headSize = 0.7f;
                    v.isSlug = true; v.impactEnemyId = "impact_sparks"; v.impactCoverId = "impact_wood";
                    break;
            }
            return v;
        }
    }
}

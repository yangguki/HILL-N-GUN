using System;
using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>What lines the left / right edges of the field (outside the playable trapezoid).</summary>
    public enum ThemeSideKind { TownFacades, CanyonWalls, StationPlatform, Boxcars, OpenDesert }

    /// <summary>Far silhouette on the horizon band.</summary>
    public enum ThemeFarKind { TownSkyline, Mesas, StationYard, SunsetTown }

    /// <summary>Ground marking style inside the field.</summary>
    public enum ThemeRoadKind { MainStreet, CanyonTrail, Railway }

    /// <summary>
    /// View-only cover look: a cover id (logic) is drawn as this prop. The prop always fills the cover's logical
    /// width, so hit boxes / footprints never change, only the art.
    /// </summary>
    public enum CoverVisual { Default, Barrel, Crate, Trough, SwingDoor, Wagon, HayBale, Rock, Fence, CrateStack }

    [Serializable]
    public class CoverVisualRule
    {
        public string coverId;
        public CoverVisual visual = CoverVisual.Default;
        [Tooltip("Optional second look, picked per cover position with this chance (variety without changing the cover).")]
        public CoverVisual altVisual = CoverVisual.Default;
        [Range(0f, 1f)] public float altChance = 0f;
    }

    /// <summary>
    /// Data-driven stage look (background bake, ambient, shadow tone, per-stage cover props). View data only: nothing
    /// here is read by game logic, so rules, balance, rows and cover hit boxes are unaffected by themes.
    /// </summary>
    [CreateAssetMenu(fileName = "theme_", menuName = "HillNGun/Stage Theme")]
    public class StageThemeData : ScriptableObject
    {
        [Header("Identity")]
        public string id = "town_street";
        public string nameKey = "theme.town_street";
        public string nameFallback = "마을 큰길";
        public int seed = 1;

        [Header("Sky")]
        public Color skyTop = new Color(0.42f, 0.62f, 0.86f);
        public Color skyHorizon = new Color(0.98f, 0.86f, 0.62f);
        public Color sunColor = new Color(1f, 0.93f, 0.7f);
        [Tooltip("Sun centre in viewport (0..1); y > 1 = above the visible band")]
        public Vector2 sunViewport = new Vector2(0.82f, 0.97f);
        public float sunRadius = 0.9f;
        public float sunGlow = 0.5f;
        public bool clouds = true;

        [Header("Far horizon")]
        public ThemeFarKind far = ThemeFarKind.TownSkyline;
        public Color farColor = new Color(0.55f, 0.42f, 0.36f);
        public Color farColor2 = new Color(0.70f, 0.55f, 0.45f);
        public Color hazeColor = new Color(0.95f, 0.82f, 0.62f, 0.45f);

        [Header("Ground")]
        public ThemeRoadKind road = ThemeRoadKind.MainStreet;
        public Color sand = new Color(0.86f, 0.70f, 0.48f);
        public Color sandFar = new Color(0.90f, 0.77f, 0.56f);
        public Color dirt = new Color(0.74f, 0.56f, 0.38f);
        public Color rut = new Color(0.58f, 0.42f, 0.28f);
        public Color gravel = new Color(0.62f, 0.52f, 0.42f);
        public Color grass = new Color(0.66f, 0.62f, 0.34f);
        public Color outerGround = new Color(0.76f, 0.60f, 0.40f);
        [Range(0f, 1f)] public float grainStrength = 0.55f;
        [Range(0f, 1f)] public float blotchStrength = 0.5f;
        public int gravelCount = 260;
        public int grassTufts = 40;
        [Tooltip("Faint row guides (gameplay readability of the fixed rows)")]
        [Range(0f, 0.3f)] public float rowGuideAlpha = 0.10f;

        [Header("Sides")]
        public ThemeSideKind leftSide = ThemeSideKind.TownFacades;
        public ThemeSideKind rightSide = ThemeSideKind.TownFacades;
        public Color wallA = new Color(0.62f, 0.36f, 0.24f);
        public Color wallB = new Color(0.72f, 0.58f, 0.40f);
        public Color wallC = new Color(0.50f, 0.42f, 0.36f);
        public Color trim = new Color(0.94f, 0.88f, 0.74f);
        public Color roof = new Color(0.36f, 0.24f, 0.17f);
        public Color windowColor = new Color(0.20f, 0.16f, 0.14f);
        public Color windowLit = new Color(1f, 0.78f, 0.38f);
        [Range(0f, 1f)] public float litWindows = 0f;
        public Color signText = new Color(0.25f, 0.13f, 0.07f);
        public string[] leftSigns = { "SALOON", "HOTEL", "GENERAL STORE" };
        public string[] rightSigns = { "SHERIFF", "BANK", "BARBER" };

        [Header("Decor (outside the playfield)")]
        public int cactusCount = 6;
        public int skullCount = 3;
        public int rockCount = 8;
        public int tumbleweedStatic = 2;
        public Color cactusColor = new Color(0.36f, 0.52f, 0.30f);
        public Color rockColor = new Color(0.66f, 0.50f, 0.38f);
        public Color boneColor = new Color(0.94f, 0.90f, 0.80f);
        public Color fenceColor = new Color(0.50f, 0.34f, 0.20f);

        [Header("Mood")]
        [Tooltip("Ground drop shadows of characters, props and decor (one tone for everything)")]
        public Color groundShadow = new Color(0.20f, 0.10f, 0.06f, 0.36f);
        [Tooltip("Shade band colour of the 3D toon models (same family as groundShadow)")]
        public Color toonShade = new Color(0.80f, 0.70f, 0.78f);
        public Color vignetteColor = new Color(0.16f, 0.08f, 0.04f, 1f);
        [Range(0f, 1f)] public float vignette = 0.42f;
        public Color colorGrade = Color.white;

        [Header("Ambient animation")]
        public int tumbleweeds = 2;
        public float tumbleweedInterval = 7f;
        public int dustMotes = 14;
        public Color dustColor = new Color(0.96f, 0.86f, 0.66f, 0.35f);
        public Vector2 wind = new Vector2(1.6f, 0.15f);

        [Header("Covers (view only)")]
        public CoverVisualRule[] coverVisuals = new CoverVisualRule[0];
        public Color rockTint = new Color(0.74f, 0.52f, 0.38f);

        /// <summary>Visual for a cover id at a logic position (deterministic per position).</summary>
        public CoverVisual ResolveCoverVisual(string coverId, Vector2 logicPos)
        {
            if (coverVisuals != null)
                foreach (var r in coverVisuals)
                {
                    if (r == null || r.coverId != coverId) continue;
                    if (r.altChance > 0f && r.altVisual != CoverVisual.Default)
                    {
                        // stable hash of the position -> [0,1)
                        int h = Mathf.RoundToInt(logicPos.x * 7f) * 73856093 ^ Mathf.RoundToInt(logicPos.y * 7f) * 19349663 ^ seed * 83492791;
                        float u = (h & 0x7fffffff) % 1000 / 1000f;
                        if (u < r.altChance) return r.altVisual;
                    }
                    return r.visual;
                }
            return CoverVisual.Default;
        }
    }
}

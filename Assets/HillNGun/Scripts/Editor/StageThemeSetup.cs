using UnityEditor;
using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Editor
{
    /// <summary>
    /// Creates the Western stage theme assets (data only; art is procedural at runtime) and maps them to stages:
    /// 1-1 town main street, 1-2 canyon pass, 1-3 rail station, boss = sunset showdown, PvP = town, menus = frontier dusk.
    /// </summary>
    public static class StageThemeSetup
    {
        public const string THEME_PATH = "Assets/HillNGun/Data/Themes/";
        public const string LIBRARY_PATH = THEME_PATH + "theme_library.asset";
        private const string STAGE_PATH = "Assets/HillNGun/Data/Stages/";

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static StageThemeData Load(string id)
        {
            string path = THEME_PATH + "theme_" + id + ".asset";
            var t = AssetDatabase.LoadAssetAtPath<StageThemeData>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<StageThemeData>();
                AssetDatabase.CreateAsset(t, path);
            }
            // reset to code defaults so re-running setup is deterministic
            var fresh = ScriptableObject.CreateInstance<StageThemeData>();
            EditorUtility.CopySerialized(fresh, t);
            Object.DestroyImmediate(fresh);
            t.name = "theme_" + id;
            t.id = id;
            t.nameKey = "theme." + id;
            return t;
        }

        private static CoverVisualRule Rule(string id, CoverVisual v, CoverVisual alt = CoverVisual.Default, float chance = 0f)
            => new CoverVisualRule { coverId = id, visual = v, altVisual = alt, altChance = chance };

        public static StageThemeLibrary CreateThemes()
        {
            if (!AssetDatabase.IsValidFolder("Assets/HillNGun/Data/Themes")) AssetDatabase.CreateFolder("Assets/HillNGun/Data", "Themes");

            // ---------------- 1-1 / PvP: town main street (saloon, sheriff's office) ----------------
            var town = Load("town_street");
            town.nameFallback = "마을 큰길"; town.seed = 11;
            town.skyTop = C(0.45f, 0.66f, 0.88f); town.skyHorizon = C(0.98f, 0.88f, 0.66f); town.sunColor = C(1f, 0.95f, 0.75f);
            town.sunViewport = new Vector2(0.84f, 0.985f); town.sunRadius = 0.55f; town.sunGlow = 0.45f; town.clouds = true;
            town.far = ThemeFarKind.TownSkyline; town.farColor = C(0.62f, 0.46f, 0.38f); town.farColor2 = C(0.80f, 0.64f, 0.52f); town.hazeColor = C(0.98f, 0.86f, 0.66f, 0.5f);
            town.road = ThemeRoadKind.MainStreet;
            town.sand = C(0.87f, 0.72f, 0.50f); town.sandFar = C(0.91f, 0.79f, 0.58f); town.dirt = C(0.76f, 0.58f, 0.40f); town.rut = C(0.58f, 0.42f, 0.28f);
            town.gravel = C(0.62f, 0.52f, 0.42f); town.grass = C(0.70f, 0.64f, 0.36f); town.outerGround = C(0.80f, 0.64f, 0.44f);
            town.gravelCount = 260; town.grassTufts = 40; town.rowGuideAlpha = 0.10f;
            town.leftSide = ThemeSideKind.TownFacades; town.rightSide = ThemeSideKind.TownFacades;
            town.wallA = C(0.64f, 0.30f, 0.22f); town.wallB = C(0.76f, 0.60f, 0.42f); town.wallC = C(0.52f, 0.44f, 0.40f);
            town.trim = C(0.95f, 0.89f, 0.74f); town.roof = C(0.38f, 0.25f, 0.18f); town.signText = C(0.30f, 0.14f, 0.08f);
            town.leftSigns = new[] { "SALOON", "HOTEL", "GENERAL STORE", "LIVERY" };
            town.rightSigns = new[] { "SHERIFF", "BANK", "BARBER", "POST OFFICE" };
            town.cactusCount = 6; town.skullCount = 3; town.rockCount = 8; town.tumbleweedStatic = 2;
            town.groundShadow = C(0.24f, 0.12f, 0.07f, 0.36f); town.toonShade = C(0.80f, 0.70f, 0.76f);
            town.vignetteColor = C(0.18f, 0.09f, 0.04f); town.vignette = 0.38f;
            town.tumbleweeds = 2; town.tumbleweedInterval = 7f; town.dustMotes = 14; town.dustColor = C(0.98f, 0.88f, 0.68f, 0.32f); town.wind = new Vector2(1.6f, 0.15f);
            town.coverVisuals = new[]
            {
                Rule("cover_barrel", CoverVisual.Barrel),
                Rule("cover_trough", CoverVisual.Trough),
                Rule("cover_swing_door", CoverVisual.SwingDoor),
                Rule("cover_wagon", CoverVisual.Wagon),
            };
            town.rockTint = C(0.74f, 0.52f, 0.38f);

            // ---------------- 1-2: canyon pass (red rock walls, mesas, winding trail) ----------------
            var canyon = Load("canyon_pass");
            canyon.nameFallback = "협곡 광산길"; canyon.seed = 23;
            canyon.skyTop = C(0.40f, 0.62f, 0.90f); canyon.skyHorizon = C(0.98f, 0.82f, 0.62f); canyon.sunColor = C(1f, 0.94f, 0.72f);
            canyon.sunViewport = new Vector2(0.16f, 0.99f); canyon.sunRadius = 0.5f; canyon.sunGlow = 0.45f; canyon.clouds = true;
            canyon.far = ThemeFarKind.Mesas; canyon.farColor = C(0.66f, 0.36f, 0.26f); canyon.farColor2 = C(0.84f, 0.56f, 0.42f); canyon.hazeColor = C(0.98f, 0.78f, 0.60f, 0.5f);
            canyon.road = ThemeRoadKind.CanyonTrail;
            canyon.sand = C(0.84f, 0.62f, 0.44f); canyon.sandFar = C(0.88f, 0.70f, 0.52f); canyon.dirt = C(0.72f, 0.50f, 0.34f); canyon.rut = C(0.56f, 0.36f, 0.24f);
            canyon.gravel = C(0.58f, 0.42f, 0.34f); canyon.grass = C(0.68f, 0.60f, 0.34f); canyon.outerGround = C(0.70f, 0.44f, 0.30f);
            canyon.gravelCount = 420; canyon.grassTufts = 30; canyon.rowGuideAlpha = 0.10f;
            canyon.leftSide = ThemeSideKind.CanyonWalls; canyon.rightSide = ThemeSideKind.CanyonWalls;
            canyon.wallA = C(0.62f, 0.30f, 0.20f); canyon.wallB = C(0.78f, 0.46f, 0.30f); canyon.wallC = C(0.82f, 0.58f, 0.40f);
            canyon.trim = C(1f, 0.82f, 0.62f); canyon.roof = C(0.40f, 0.22f, 0.15f);
            canyon.cactusCount = 8; canyon.skullCount = 4; canyon.rockCount = 12; canyon.tumbleweedStatic = 3;
            canyon.cactusColor = C(0.38f, 0.52f, 0.30f); canyon.rockColor = C(0.70f, 0.44f, 0.32f);
            canyon.groundShadow = C(0.30f, 0.10f, 0.06f, 0.38f); canyon.toonShade = C(0.84f, 0.66f, 0.66f);
            canyon.vignetteColor = C(0.22f, 0.08f, 0.04f); canyon.vignette = 0.42f;
            canyon.tumbleweeds = 3; canyon.tumbleweedInterval = 5.5f; canyon.dustMotes = 16; canyon.dustColor = C(0.96f, 0.80f, 0.62f, 0.32f); canyon.wind = new Vector2(2.0f, 0.1f);
            canyon.coverVisuals = new[]
            {
                Rule("cover_barrel", CoverVisual.Rock),
                Rule("cover_trough", CoverVisual.Rock),
                Rule("cover_swing_door", CoverVisual.Fence),
                Rule("cover_wagon", CoverVisual.Wagon),
            };
            canyon.rockTint = C(0.68f, 0.44f, 0.30f);

            // ---------------- 1-3: rail station (depot platform, freight yard, tracks across the middle) ----------------
            var station = Load("rail_station");
            station.nameFallback = "기차역"; station.seed = 37;
            station.skyTop = C(0.48f, 0.66f, 0.86f); station.skyHorizon = C(0.96f, 0.86f, 0.70f); station.sunColor = C(1f, 0.94f, 0.76f);
            station.sunViewport = new Vector2(0.70f, 0.99f); station.sunRadius = 0.5f; station.sunGlow = 0.4f; station.clouds = true;
            station.far = ThemeFarKind.StationYard; station.farColor = C(0.50f, 0.42f, 0.40f); station.farColor2 = C(0.72f, 0.60f, 0.54f); station.hazeColor = C(0.96f, 0.86f, 0.70f, 0.5f);
            station.road = ThemeRoadKind.Railway;
            station.sand = C(0.82f, 0.70f, 0.52f); station.sandFar = C(0.88f, 0.78f, 0.60f); station.dirt = C(0.70f, 0.58f, 0.44f); station.rut = C(0.52f, 0.42f, 0.32f);
            station.gravel = C(0.55f, 0.52f, 0.50f); station.grass = C(0.66f, 0.62f, 0.38f); station.outerGround = C(0.74f, 0.62f, 0.46f);
            station.gravelCount = 300; station.grassTufts = 34; station.rowGuideAlpha = 0.10f;
            station.leftSide = ThemeSideKind.StationPlatform; station.rightSide = ThemeSideKind.Boxcars;
            station.wallA = C(0.42f, 0.50f, 0.44f); station.wallB = C(0.74f, 0.62f, 0.46f); station.wallC = C(0.56f, 0.26f, 0.20f);
            station.trim = C(0.94f, 0.90f, 0.78f); station.roof = C(0.30f, 0.24f, 0.20f); station.signText = C(0.20f, 0.14f, 0.10f);
            station.leftSigns = new[] { "TICKETS", "DEPOT", "TELEGRAPH", "HOTEL" };
            station.rightSigns = new[] { "FREIGHT", "STORAGE", "FEED & SEED" };
            station.cactusCount = 4; station.skullCount = 2; station.rockCount = 6; station.tumbleweedStatic = 2;
            station.groundShadow = C(0.18f, 0.12f, 0.10f, 0.36f); station.toonShade = C(0.76f, 0.72f, 0.80f);
            station.vignetteColor = C(0.12f, 0.09f, 0.07f); station.vignette = 0.40f;
            station.tumbleweeds = 2; station.tumbleweedInterval = 8f; station.dustMotes = 12; station.dustColor = C(0.96f, 0.90f, 0.76f, 0.30f); station.wind = new Vector2(1.4f, 0.12f);
            station.coverVisuals = new[]
            {
                Rule("cover_barrel", CoverVisual.Barrel),
                Rule("cover_trough", CoverVisual.CrateStack),
                Rule("cover_swing_door", CoverVisual.Crate),
                Rule("cover_wagon", CoverVisual.Wagon),
            };
            station.rockTint = C(0.66f, 0.58f, 0.52f);

            // ---------------- Boss: sunset showdown with Rattlesnake Rex (red tones) ----------------
            var sunset = Load("sunset_showdown");
            sunset.nameFallback = "석양의 결투"; sunset.seed = 53;
            sunset.skyTop = C(0.36f, 0.10f, 0.18f); sunset.skyHorizon = C(1f, 0.48f, 0.22f); sunset.sunColor = C(1f, 0.78f, 0.42f);
            sunset.sunViewport = new Vector2(0.5f, 0.955f); sunset.sunRadius = 1.7f; sunset.sunGlow = 0.6f; sunset.clouds = false;
            sunset.far = ThemeFarKind.SunsetTown; sunset.farColor = C(0.22f, 0.07f, 0.09f); sunset.farColor2 = C(0.46f, 0.16f, 0.16f); sunset.hazeColor = C(1f, 0.50f, 0.25f, 0.45f);
            sunset.road = ThemeRoadKind.MainStreet;
            sunset.sand = C(0.80f, 0.50f, 0.38f); sunset.sandFar = C(0.90f, 0.58f, 0.40f); sunset.dirt = C(0.66f, 0.38f, 0.28f); sunset.rut = C(0.46f, 0.24f, 0.18f);
            sunset.gravel = C(0.50f, 0.32f, 0.26f); sunset.grass = C(0.62f, 0.44f, 0.28f); sunset.outerGround = C(0.62f, 0.34f, 0.26f);
            sunset.gravelCount = 240; sunset.grassTufts = 34; sunset.rowGuideAlpha = 0.10f;
            sunset.leftSide = ThemeSideKind.TownFacades; sunset.rightSide = ThemeSideKind.TownFacades;
            sunset.wallA = C(0.36f, 0.16f, 0.14f); sunset.wallB = C(0.44f, 0.24f, 0.18f); sunset.wallC = C(0.30f, 0.16f, 0.16f);
            sunset.trim = C(0.70f, 0.46f, 0.34f); sunset.roof = C(0.18f, 0.08f, 0.08f); sunset.windowColor = C(0.12f, 0.05f, 0.05f);
            sunset.windowLit = C(1f, 0.70f, 0.32f); sunset.litWindows = 0.6f; sunset.signText = C(0.24f, 0.08f, 0.06f);
            sunset.leftSigns = new[] { "SALOON", "HOTEL", "GENERAL STORE", "LIVERY" };
            sunset.rightSigns = new[] { "SHERIFF", "BANK", "WANTED", "UNDERTAKER" };
            sunset.cactusCount = 5; sunset.skullCount = 5; sunset.rockCount = 6; sunset.tumbleweedStatic = 2;
            sunset.cactusColor = C(0.28f, 0.30f, 0.20f); sunset.rockColor = C(0.52f, 0.28f, 0.22f); sunset.boneColor = C(0.96f, 0.82f, 0.70f); sunset.fenceColor = C(0.30f, 0.14f, 0.10f);
            sunset.groundShadow = C(0.30f, 0.04f, 0.08f, 0.42f); sunset.toonShade = C(0.88f, 0.56f, 0.58f);
            sunset.vignetteColor = C(0.20f, 0.02f, 0.04f); sunset.vignette = 0.58f;
            sunset.tumbleweeds = 3; sunset.tumbleweedInterval = 6f; sunset.dustMotes = 18; sunset.dustColor = C(1f, 0.66f, 0.42f, 0.30f); sunset.wind = new Vector2(2.2f, 0.1f);
            sunset.coverVisuals = new[]
            {
                Rule("cover_barrel", CoverVisual.Barrel),
                Rule("cover_trough", CoverVisual.HayBale),
                Rule("cover_swing_door", CoverVisual.SwingDoor),
                Rule("cover_wagon", CoverVisual.Wagon),
            };
            sunset.rockTint = C(0.55f, 0.30f, 0.24f);

            // ---------------- Menus: frontier town at dusk ----------------
            var dusk = Load("frontier_dusk");
            dusk.nameFallback = "황혼의 변경"; dusk.seed = 71;
            dusk.skyTop = C(0.28f, 0.20f, 0.36f); dusk.skyHorizon = C(1f, 0.62f, 0.32f); dusk.sunColor = C(1f, 0.85f, 0.5f);
            dusk.sunViewport = new Vector2(0.5f, 0.47f); dusk.sunRadius = 0.9f; dusk.sunGlow = 0.55f; dusk.clouds = true;
            dusk.far = ThemeFarKind.SunsetTown; dusk.farColor = C(0.42f, 0.24f, 0.26f); dusk.farColor2 = C(0.62f, 0.36f, 0.32f); dusk.hazeColor = C(1f, 0.62f, 0.36f, 0.5f);
            dusk.road = ThemeRoadKind.MainStreet;
            dusk.sand = C(0.72f, 0.50f, 0.34f); dusk.sandFar = C(0.86f, 0.62f, 0.40f); dusk.dirt = C(0.60f, 0.40f, 0.28f); dusk.rut = C(0.44f, 0.28f, 0.20f);
            dusk.gravel = C(0.50f, 0.36f, 0.28f); dusk.grass = C(0.62f, 0.50f, 0.30f); dusk.outerGround = C(0.62f, 0.42f, 0.30f);
            dusk.wallA = C(0.46f, 0.22f, 0.18f); dusk.wallB = C(0.56f, 0.38f, 0.28f); dusk.wallC = C(0.40f, 0.30f, 0.30f);
            dusk.trim = C(0.86f, 0.70f, 0.52f); dusk.roof = C(0.24f, 0.12f, 0.10f);
            dusk.windowLit = C(1f, 0.74f, 0.36f); dusk.litWindows = 0.5f;
            dusk.cactusColor = C(0.30f, 0.38f, 0.24f); dusk.rockColor = C(0.55f, 0.34f, 0.26f); dusk.fenceColor = C(0.36f, 0.22f, 0.14f);
            dusk.groundShadow = C(0.24f, 0.08f, 0.10f, 0.38f); dusk.toonShade = C(0.84f, 0.66f, 0.72f);
            dusk.vignetteColor = C(0.12f, 0.04f, 0.06f); dusk.vignette = 0.5f;

            // One look per cover id per stage (variety comes from the stage): identical meshes keep GPU instancing,
            // so themed covers cost no extra draw calls. Alt looks (altVisual/altChance) stay available as data.
            var all = new[] { town, canyon, station, sunset, dusk };
            foreach (var t in all) EditorUtility.SetDirty(t);

            var lib = AssetDatabase.LoadAssetAtPath<StageThemeLibrary>(LIBRARY_PATH);
            if (lib == null) { lib = ScriptableObject.CreateInstance<StageThemeLibrary>(); AssetDatabase.CreateAsset(lib, LIBRARY_PATH); }
            lib.themes = all;
            lib.pvpThemeId = town.id;
            lib.menuThemeId = dusk.id;
            lib.defaultThemeId = town.id;
            lib.maxBakeSize = 2560;
            EditorUtility.SetDirty(lib);

            SetStageTheme("stage_dusty_town_1_1", town.id);
            SetStageTheme("stage_dusty_town_1_2", canyon.id);
            SetStageTheme("stage_dusty_town_1_3", station.id);
            SetStageTheme("stage_dusty_town_boss", sunset.id);
            AssetDatabase.SaveAssets();
            return lib;
        }

        private static void SetStageTheme(string asset, string themeId)
        {
            var s = AssetDatabase.LoadAssetAtPath<StageData>(STAGE_PATH + asset + ".asset");
            if (s == null) { Debug.LogWarning("[StageThemeSetup] missing stage " + asset); return; }
            s.themeId = themeId;
            EditorUtility.SetDirty(s);
        }
    }
}

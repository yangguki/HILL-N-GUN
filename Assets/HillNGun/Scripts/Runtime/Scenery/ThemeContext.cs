using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Scenery
{
    /// <summary>
    /// The stage theme currently on screen (view state only). Views read it for cover prop looks and the shared
    /// shadow tone; logic never does. Version bumps on every change so views can refresh cheaply.
    /// </summary>
    public static class ThemeContext
    {
        public static StageThemeData Current { get; private set; }
        public static int Version { get; private set; }

        /// <summary>Command-line A/B switch (-noscenery): previous flat background, default cover looks, no ambient fx.</summary>
        public static readonly bool ForceOff = System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-noscenery");

        /// <summary>Diagnostic switches for perf A/B: -noambient (no tumbleweed/dust layer), -nothemecovers (default cover looks).</summary>
        public static readonly bool NoAmbient = System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-noambient");
        public static readonly bool NoThemeCovers = System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-nothemecovers");

        public static StageThemeLibrary Library => !ForceOff && RuntimeAssetManager.Instance != null ? RuntimeAssetManager.Instance.StageThemes : null;

        public static StageThemeData ForStage(StageData stage)
        {
            var lib = Library;
            return lib == null ? null : lib.Get(stage != null ? stage.themeId : null);
        }

        public static StageThemeData Pvp => Library?.Get(Library.pvpThemeId);
        public static StageThemeData Menu => Library?.Get(Library.menuThemeId);

        public static void Set(StageThemeData theme)
        {
            if (theme == Current) return;
            Current = theme;
            Version++;
            if (HillNGun.View3D.Actor3DStage.Enabled)
                HillNGun.View3D.Actor3DStage.ApplyGlobals(HillNGun.View3D.Actor3DStage.Data.outlineWidth);
        }

        /// <summary>Ground drop-shadow tone (characters, props, decor all share it).</summary>
        public static Color GroundShadowOr(Color fallback) => Current != null ? Current.groundShadow : fallback;

        /// <summary>Shade band colour for the 3D toon models.</summary>
        public static Color ToonShadeOr(Color fallback) => Current != null ? Current.toonShade : fallback;

        /// <summary>Cover look for a cover id at a logic position under the current theme.</summary>
        public static CoverVisual CoverVisualFor(string coverId, Vector2 logicPos)
            => Current != null && !NoThemeCovers ? Current.ResolveCoverVisual(coverId, logicPos) : CoverVisual.Default;
    }
}

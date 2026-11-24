using System;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxViewModeState
    {
        public const Proto_GreyboxViewMode DefaultMode = Proto_GreyboxViewMode.View2DPerspective;

        static Proto_GreyboxViewMode s_mode = DefaultMode;
        static bool s_loaded;

        public static event Action<Proto_GreyboxViewMode> ModeChanged;

        public static Proto_GreyboxViewMode Current
        {
            get
            {
                EnsureLoaded();
                return s_mode;
            }
        }

        public static bool Is3D => Current == Proto_GreyboxViewMode.View3D;

        /// <summary>2D gameplay with the flat chibi skin (not 3D).</summary>
        public static bool IsChibi => Current == Proto_GreyboxViewMode.View2DChibi;

        /// <summary>Designer perspective 2D street skin (slice K).</summary>
        public static bool IsPersp2D => Current == Proto_GreyboxViewMode.View2DPerspective;

        /// <summary>Toggle order: 2D 원근 → 2D 픽셀 → 3D → 2D 치비 → 2D 원근.</summary>
        public static Proto_GreyboxViewMode Next(Proto_GreyboxViewMode mode)
        {
            switch (mode)
            {
                case Proto_GreyboxViewMode.View2DPerspective: return Proto_GreyboxViewMode.View2D;
                case Proto_GreyboxViewMode.View2D: return Proto_GreyboxViewMode.View3D;
                case Proto_GreyboxViewMode.View3D: return Proto_GreyboxViewMode.View2DChibi;
                default: return Proto_GreyboxViewMode.View2DPerspective;
            }
        }

        public static string Label(Proto_GreyboxViewMode mode, bool hangul)
        {
            switch (mode)
            {
                case Proto_GreyboxViewMode.View3D: return "View:  3D";
                case Proto_GreyboxViewMode.View2DChibi: return hangul ? "View:  2D 치비" : "View:  2D Chibi";
                case Proto_GreyboxViewMode.View2DPerspective: return hangul ? "View:  2D 원근" : "View:  2D Persp";
                case Proto_GreyboxViewMode.View2D: return hangul ? "View:  2D 픽셀" : "View:  2D Pixel";
                default: return hangul ? "View:  2D 픽셀" : "View:  2D Pixel";
            }
        }

        public static UnityEngine.Color PlateColor(Proto_GreyboxViewMode mode)
        {
            switch (mode)
            {
                case Proto_GreyboxViewMode.View3D: return new UnityEngine.Color(0.55f, 0.22f, 0.72f, 0.95f);
                case Proto_GreyboxViewMode.View2DChibi: return new UnityEngine.Color(0.86f, 0.42f, 0.16f, 0.95f);
                case Proto_GreyboxViewMode.View2DPerspective: return new UnityEngine.Color(0.18f, 0.58f, 0.38f, 0.95f);
                default: return new UnityEngine.Color(0.22f, 0.45f, 0.72f, 0.95f);
            }
        }

        public static void EnsureLoaded()
        {
            if (s_loaded) return;
            s_mode = Proto_GreyboxViewModePrefs.LoadOrDefault();
            s_loaded = true;
        }

        public static void SetMode(Proto_GreyboxViewMode mode, bool persist = true)
        {
            EnsureLoaded();
            if (s_mode == mode) return;
            s_mode = mode;
            if (persist)
                Proto_GreyboxViewModePrefs.Save(mode);
            ModeChanged?.Invoke(mode);
        }

        public static void ResetForTests()
        {
            s_loaded = false;
            s_mode = DefaultMode;
        }
    }
}
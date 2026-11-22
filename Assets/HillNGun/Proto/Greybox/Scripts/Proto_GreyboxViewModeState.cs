using System;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxViewModeState
    {
        public const Proto_GreyboxViewMode DefaultMode = Proto_GreyboxViewMode.View2D;

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
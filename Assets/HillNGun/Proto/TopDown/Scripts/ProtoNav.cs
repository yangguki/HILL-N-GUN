using UnityEngine.SceneManagement;

namespace HillNGun.Proto.TopDown
{
    /// <summary>Shared scene names and front-line launch options for the proto menu.</summary>
    public static class ProtoNav
    {
        public const string MenuSceneName = "Proto_Menu";
        public const string TopDownSceneName = "Proto_TopDown";
        public const string FrontlineSceneName = "Proto_Frontline";

        static bool _hasFrontlineMode;
        static bool _frontlineThree;

        public static string SceneNameFor(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return "";
            int slash = assetPath.LastIndexOf('/');
            int dot = assetPath.LastIndexOf('.');
            int start = slash >= 0 ? slash + 1 : 0;
            int end = dot > start ? dot : assetPath.Length;
            return assetPath.Substring(start, end - start);
        }

        public static void OpenMenu() => SceneManager.LoadScene(MenuSceneName);

        public static void OpenTopDown() => SceneManager.LoadScene(TopDownSceneName);

        public static void OpenFrontline(bool threeVsThree)
        {
            SetFrontlineLaunch(threeVsThree);
            SceneManager.LoadScene(FrontlineSceneName);
        }

        public static void SetFrontlineLaunch(bool threeVsThree)
        {
            _hasFrontlineMode = true;
            _frontlineThree = threeVsThree;
        }

        public static bool TryTakeFrontlineThree(out bool threeVsThree)
        {
            if (!_hasFrontlineMode)
            {
                threeVsThree = false;
                return false;
            }
            threeVsThree = _frontlineThree;
            _hasFrontlineMode = false;
            return true;
        }

        public static void ResetLaunch()
        {
            _hasFrontlineMode = false;
            _frontlineThree = false;
        }
    }
}

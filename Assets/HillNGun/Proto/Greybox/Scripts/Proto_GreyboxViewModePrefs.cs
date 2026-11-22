using System;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxViewModePrefs
    {
        public const string Key = "HillNGun.Greybox.ViewMode";

        static bool _inTest;
        static bool _testHas;
        static Proto_GreyboxViewMode _testMode;

        public static Proto_GreyboxViewMode LoadOrDefault()
        {
            if (_inTest && _testHas)
                return _testMode;
            return UnityEngine.PlayerPrefs.GetInt(Key, (int)Proto_GreyboxViewMode.View2D) == (int)Proto_GreyboxViewMode.View3D
                ? Proto_GreyboxViewMode.View3D
                : Proto_GreyboxViewMode.View2D;
        }

        public static void Save(Proto_GreyboxViewMode mode)
        {
            if (_inTest)
            {
                _testMode = mode;
                _testHas = true;
                return;
            }

            UnityEngine.PlayerPrefs.SetInt(Key, (int)mode);
            UnityEngine.PlayerPrefs.Save();
        }

        public static IDisposable EnterTestScope() => new TestScope();

        sealed class TestScope : IDisposable
        {
            public TestScope()
            {
                _inTest = true;
                _testHas = false;
            }

            public void Dispose()
            {
                _inTest = false;
                _testHas = false;
            }
        }
    }
}
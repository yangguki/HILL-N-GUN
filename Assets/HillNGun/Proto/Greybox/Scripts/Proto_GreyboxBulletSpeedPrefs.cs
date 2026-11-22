using System;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxBulletSpeedPrefs
    {
        public const string AllyKey = "HillNGun.Greybox.AllyBulletSpeed";
        public const string EnemyKey = "HillNGun.Greybox.EnemyBulletSpeed";

        static bool _inTest;
        static bool _testHasAlly;
        static bool _testHasEnemy;
        static float _testAlly;
        static float _testEnemy;

        public static void LoadInto(Proto_GreyboxRuntimeTuning tuning)
        {
            if (tuning == null) return;
            if (_inTest)
            {
                if (_testHasAlly) tuning.AllyBulletSpeed = _testAlly;
                if (_testHasEnemy) tuning.EnemyBulletSpeed = _testEnemy;
                return;
            }

            if (UnityEngine.PlayerPrefs.HasKey(AllyKey))
                tuning.AllyBulletSpeed = UnityEngine.PlayerPrefs.GetFloat(AllyKey);
            if (UnityEngine.PlayerPrefs.HasKey(EnemyKey))
                tuning.EnemyBulletSpeed = UnityEngine.PlayerPrefs.GetFloat(EnemyKey);
        }

        public static void SaveFrom(Proto_GreyboxRuntimeTuning tuning)
        {
            if (tuning == null) return;
            Write(tuning.AllyBulletSpeed, tuning.EnemyBulletSpeed);
        }

        public static void SaveAlly(float speed) => Write(speed, null);

        public static void SaveEnemy(float speed) => Write(null, speed);

        static void Write(float? ally, float? enemy)
        {
            if (_inTest)
            {
                if (ally.HasValue)
                {
                    _testAlly = ally.Value;
                    _testHasAlly = true;
                }
                if (enemy.HasValue)
                {
                    _testEnemy = enemy.Value;
                    _testHasEnemy = true;
                }
                return;
            }

            if (ally.HasValue)
                UnityEngine.PlayerPrefs.SetFloat(AllyKey, ally.Value);
            if (enemy.HasValue)
                UnityEngine.PlayerPrefs.SetFloat(EnemyKey, enemy.Value);
            UnityEngine.PlayerPrefs.Save();
        }

        public static IDisposable EnterTestScope() => new TestScope();

        sealed class TestScope : IDisposable
        {
            public TestScope()
            {
                _inTest = true;
                _testHasAlly = false;
                _testHasEnemy = false;
            }

            public void Dispose()
            {
                _inTest = false;
                _testHasAlly = false;
                _testHasEnemy = false;
            }
        }
    }
}

using UnityEngine;

namespace HillNGun.Core
{
    /// <summary>
    /// v14: dedicated gameplay RNG stream (bot decisions, drops, dodge sense). Render/FX/audio keep using
    /// UnityEngine.Random, so visual code (rig, VFX, ambient) can never consume or shift gameplay randomness.
    /// Seeded per session from the clock (variety unchanged); -logic-ab-seed N fixes it for A/B diagnostics.
    /// </summary>
    public static class GameplayRng
    {
        private static uint _s = 0x9E3779B9u;
        public static long Calls { get; private set; }
        public static uint State => _s;

        public static void Seed(int seed)
        {
            _s = (uint)seed ^ 0x9E3779B9u;
            if (_s == 0) _s = 1;
            Calls = 0;
        }

        private static uint Next()
        {
            uint x = _s; x ^= x << 13; x ^= x >> 17; x ^= x << 5; _s = x; Calls++;
            return x;
        }

        /// <summary>[0,1)</summary>
        public static float Value => (Next() >> 8) * (1f / 16777216f);
        public static float Range(float min, float max) => min + (max - min) * Value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void AutoSeed()
        {
            int seed = System.Environment.TickCount;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-logic-ab-seed" && int.TryParse(args[i + 1], out int s)) seed = s;
            Seed(seed);
        }
    }
}

using UnityEngine;

namespace HillNGun.Core
{
    public static class GameTime
    {
        private static float _hitstopEndTime;

        /// <summary>Hitstop clock. Normal play: wall-clock unscaled time (unchanged). Under a fixed-step capture
        /// (Time.captureDeltaTime &gt; 0: recordings, -logic-ab) Unity's unscaledTime still follows the wall clock, which made
        /// hitstop length (in simulated ticks) depend on render cost; there it advances exactly one capture step per frame.</summary>
        private static bool _clockFixed;
        private static float HitstopClock
        {
            get
            {
                bool fixedStep = Time.captureDeltaTime > 0f;
                if (fixedStep != _clockFixed) { _clockFixed = fixedStep; _hitstopEndTime = 0f; } // clock domain switched: drop stale end time
                return fixedStep ? Time.frameCount * Time.captureDeltaTime : Time.unscaledTime;
            }
        }
        private static float _timeScale = 1f;
        private static bool _isPaused;

        public static float DeltaTime
        {
            get
            {
                if (_isPaused) return 0f;
                if (HitstopClock < _hitstopEndTime) return 0f;
                return Time.deltaTime * _timeScale;
            }
        }

        public static float UnscaledDeltaTime => Time.unscaledDeltaTime;

        public static float TimeScale
        {
            get => _timeScale;
            set => _timeScale = Mathf.Clamp(value, 0f, 2f);
        }

        public static bool IsPaused
        {
            get => _isPaused;
            set => _isPaused = value;
        }

        public static bool IsInHitstop => HitstopClock < _hitstopEndTime;

        public static void ApplyHitstop(float duration)
        {
            if (duration <= 0f) return;
            float newEndTime = HitstopClock + duration;
            if (newEndTime > _hitstopEndTime)
            {
                _hitstopEndTime = newEndTime;
            }
        }

        public static void SetSlowMotion(float scale)
        {
            _timeScale = Mathf.Clamp(scale, 0.1f, 2f);
        }

        public static void ResetTimeScale()
        {
            _timeScale = 1f;
        }
    }
}

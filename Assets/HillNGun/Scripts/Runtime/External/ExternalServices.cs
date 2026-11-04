using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.External
{
    /// <summary>
    /// Registry for external-service implementations. Defaults are the offline stubs (STUB-EXTERNAL).
    /// A real build would call Set*(...) with SDK-backed implementations at boot.
    /// </summary>
    public static class ExternalServices
    {
        public static string AnalyticsFileOverride;

        private static IAdService _ads;
        private static IIapService _iap;
        private static IAnalyticsService _analytics;
        private static IMatchmakingService _matchmaking;

        public static ExternalServicesData Config =>
            ServiceLocator.Get<ExternalServicesData>() ?? RuntimeAssetManager.Instance?.ExternalServices;

        public static IAdService Ads => _ads ??= new OfflineAdStub(Config);
        public static IIapService Iap => _iap ??= new OfflineIapStub(Config);
        public static IAnalyticsService Analytics => _analytics ??= new LocalCsvAnalytics(Config);
        public static IMatchmakingService Matchmaking => _matchmaking ??= new BotMatchmakingStub();

        public static void SetAds(IAdService s) => _ads = s;
        public static void SetIap(IIapService s) => _iap = s;
        public static void SetAnalytics(IAnalyticsService s) => _analytics = s;
        public static void SetMatchmaking(IMatchmakingService s) => _matchmaking = s;

        public static void Log(string evt, IDictionary<string, object> fields = null)
        {
            try { Analytics.LogEvent(evt, fields); } catch (Exception e) { Debug.LogWarning($"[Analytics] {e.Message}"); }
        }

        // ---- tiny coroutine host for stubs that need to "take time" (unscaled, works while paused) ----
        private class Runner : MonoBehaviour { }
        private static Runner _runner;

        public static void RunAfterRealtime(float seconds, Action action)
        {
            if (_runner == null)
            {
                var go = new GameObject("[ExternalServicesRunner]");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<Runner>();
            }
            _runner.StartCoroutine(Delay(seconds, action));
        }

        private static IEnumerator Delay(float s, Action a)
        {
            if (s > 0f) yield return new WaitForSecondsRealtime(s);
            a?.Invoke();
        }
    }
}

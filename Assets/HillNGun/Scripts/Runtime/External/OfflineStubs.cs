using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.External
{
    /// <summary>STUB-EXTERNAL: no ad SDK. Simulates a watched ad (toggle) so the continue flow can be tested offline.</summary>
    public class OfflineAdStub : IAdService
    {
        private readonly ExternalServicesData _cfg;
        public OfflineAdStub(ExternalServicesData cfg) { _cfg = cfg; }
        public bool IsRealSdk => false;
        public bool IsRewardedReady(string placement) => _cfg == null || _cfg.rewardedAdsEnabled;

        public void ShowRewarded(string placement, Action<bool> onFinished)
        {
            bool ok = _cfg == null || (_cfg.rewardedAdsEnabled && _cfg.simulateAdSuccess);
            float wait = _cfg != null ? _cfg.simulatedAdSeconds : 1f;
            Debug.Log($"[External][STUB] Rewarded ad '{placement}' simulated ({wait:F1}s) -> {(ok ? "rewarded" : "not available")}");
            ExternalServices.Log("ad_rewarded", new Dictionary<string, object> { { "placement", placement }, { "result", ok ? "simulated_ok" : "unavailable" } });
            ExternalServices.RunAfterRealtime(wait, () => onFinished?.Invoke(ok));
        }
    }

    /// <summary>STUB-EXTERNAL: no store SDK. Lists the planned products; every purchase returns NotAvailableOffline.</summary>
    public class OfflineIapStub : IIapService
    {
        private readonly ExternalServicesData _cfg;
        public OfflineIapStub(ExternalServicesData cfg) { _cfg = cfg; }
        public bool IsRealSdk => false;
        public IReadOnlyList<IapProductDefinition> Products =>
            _cfg != null && _cfg.products != null ? _cfg.products : (IReadOnlyList<IapProductDefinition>)Array.Empty<IapProductDefinition>();

        public void Purchase(string productId, Action<IapResult> onFinished)
        {
            Debug.Log($"[External][STUB] IAP purchase '{productId}' -> NotAvailableOffline (no store SDK in prototype)");
            ExternalServices.Log("iap_attempt", new Dictionary<string, object> { { "product", productId }, { "result", "offline_stub" } });
            onFinished?.Invoke(IapResult.NotAvailableOffline);
        }

        public void RestorePurchases(Action<bool> onFinished) => onFinished?.Invoke(false);
    }

    /// <summary>Offline analytics: appends events to a CSV in persistentDataPath (no network). Remote = STUB-EXTERNAL.</summary>
    public class LocalCsvAnalytics : IAnalyticsService
    {
        private readonly string _path;
        private readonly bool _enabled;
        public bool IsRemote => false;

        public LocalCsvAnalytics(ExternalServicesData cfg)
        {
            _enabled = cfg == null || cfg.analyticsLocalCsv;
            string file = cfg != null && !string.IsNullOrEmpty(cfg.analyticsFileName) ? cfg.analyticsFileName : "analytics_local.csv";
            if (!string.IsNullOrEmpty(ExternalServices.AnalyticsFileOverride)) file = ExternalServices.AnalyticsFileOverride;
            _path = Path.Combine(Application.persistentDataPath, file);
        }

        public void LogEvent(string name, IDictionary<string, object> fields = null)
        {
            if (!_enabled) return;
            try
            {
                var sb = new StringBuilder();
                if (!File.Exists(_path)) sb.Append("utc,event,fields\n");
                sb.Append(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)).Append(',').Append(name).Append(",\"");
                if (fields != null)
                {
                    bool first = true;
                    foreach (var kv in fields)
                    {
                        if (!first) sb.Append(';');
                        first = false;
                        sb.Append(kv.Key).Append('=').Append(Convert.ToString(kv.Value, CultureInfo.InvariantCulture)?.Replace("\"", "'"));
                    }
                }
                sb.Append("\"\n");
                File.AppendAllText(_path, sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] write failed: {e.Message}");
            }
        }
    }

    /// <summary>STUB-EXTERNAL: no game server. Always resolves to an offline bot match (prototype decision: 봇전).</summary>
    public class BotMatchmakingStub : IMatchmakingService
    {
        public bool IsOnline => false;
        public void FindMatch(Role role, Action<MatchmakingResult> onResult)
        {
            Debug.Log($"[External][STUB] Matchmaking role={role} -> offline bot match (no server)");
            ExternalServices.Log("matchmaking", new Dictionary<string, object> { { "role", role }, { "result", "offline_bots" } });
            onResult?.Invoke(new MatchmakingResult { status = MatchmakingStatus.OfflineBots, isOnline = false, opponentDescription = "bots" });
        }
        public void Cancel() { }
    }
}

using System;
using System.Collections.Generic;
using HillNGun.Data;
using HillNGun.Core;

// =====================================================================================
//  EXTERNAL SERVICE INTERFACES  (STUB-EXTERNAL — see implementation_status.md D14)
//  The prototype is offline and ships no third-party SDK (05 §5.4). Game code only talks
//  to these interfaces; Runtime/External/OfflineStubs.cs holds the offline stand-ins.
//  To go live, implement the interface with the real SDK and register it in ExternalServices.
// =====================================================================================
namespace HillNGun.External
{
    /// <summary>Rewarded ads only (FP 518): PvE continue, daily reward x2. No forced ads, no ads in PvP.</summary>
    public interface IAdService
    {
        bool IsRealSdk { get; }
        bool IsRewardedReady(string placement);
        /// <summary>Shows a rewarded ad. Callback(true) only when the reward should be granted.</summary>
        void ShowRewarded(string placement, Action<bool> onFinished);
    }

    public enum IapResult { Success, Cancelled, Failed, NotAvailableOffline }

    /// <summary>Real-money store (FP 511-520: season pass, skins, rank pass). Cosmetic only, no stat items.</summary>
    public interface IIapService
    {
        bool IsRealSdk { get; }
        IReadOnlyList<IapProductDefinition> Products { get; }
        void Purchase(string productId, Action<IapResult> onFinished);
        void RestorePurchases(Action<bool> onFinished);
    }

    /// <summary>Analytics events (02 §5 log spec). Offline: local CSV. Remote backend = external.</summary>
    public interface IAnalyticsService
    {
        bool IsRemote { get; }
        void LogEvent(string name, IDictionary<string, object> fields = null);
    }

    public enum MatchmakingStatus { Searching, Found, OfflineBots, Failed }

    public struct MatchmakingResult
    {
        public MatchmakingStatus status;
        public string opponentDescription;
        public bool isOnline;
    }

    /// <summary>Online 3v3 with MMR / tiers / seasons (FP 270-310). Needs a server; offline = bot match (decision).</summary>
    public interface IMatchmakingService
    {
        bool IsOnline { get; }
        /// <param name="role">Role picked before queueing (FP 276: 입장 전에 역할 선택)</param>
        void FindMatch(Role role, Action<MatchmakingResult> onResult);
        void Cancel();
    }

    /// <summary>Cloud save is another ISaveStorage (Runtime/Progression/SaveStorage.cs); only local file exists.</summary>
    public interface ICloudSaveHint { }
}

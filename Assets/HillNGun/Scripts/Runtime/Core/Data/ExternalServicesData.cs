using System;
using UnityEngine;

namespace HillNGun.Data
{
    public enum IapProductKind { SeasonPass, RankPass, SkinBundle, CurrencyPack }

    [Serializable]
    public class IapProductDefinition
    {
        public string id;
        public string nameKey;
        public string descKey;
        public IapProductKind kind;
        [Tooltip("Store price comes from the store SDK at runtime; label only (가격 미정).")]
        public string priceLabelKey = "shop.price_tbd";
    }

    /// <summary>
    /// Toggles for everything that needs an external SDK / server (05 §5.4: offline prototype, no SDKs).
    /// Each service has an interface in Runtime/External and an offline stub; flip these to test flows.
    /// </summary>
    [CreateAssetMenu(fileName = "external_services", menuName = "HillNGun/External Services")]
    public class ExternalServicesData : ScriptableObject
    {
        [Header("Rewarded ads — STUB-EXTERNAL (FP 518: PvE 이어하기, 일일 보상 2배)")]
        public bool rewardedAdsEnabled = true;
        [Tooltip("Offline stub: pretend the ad was watched (prototype only).")]
        public bool simulateAdSuccess = true;
        public float simulatedAdSeconds = 1.5f;
        [Tooltip("Continue-with-ad allowed per PvE run")]
        public int maxContinuesPerRun = 1;

        [Header("In-app purchases — STUB-EXTERNAL (FP 511-520)")]
        public bool iapEnabled = false;
        public IapProductDefinition[] products = new IapProductDefinition[0];

        [Header("Analytics — local CSV only (offline principle); remote = STUB-EXTERNAL")]
        public bool analyticsLocalCsv = true;
        public string analyticsFileName = "analytics_local.csv";

        [Header("Online PvP / matchmaking — STUB-EXTERNAL (FP 270-310, needs a server)")]
        public bool onlineMatchmakingEnabled = false;

        [Header("Character art — Spine runtime STUB-EXTERNAL (license not purchased, 05 §5.4)")]
        public bool useSpineCharacterViews = false;
    }
}

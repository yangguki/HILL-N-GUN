using System;
using UnityEngine;

namespace HillNGun.Data
{
    public enum SkinUnlockType
    {
        Default,     // owned from the start
        Purchase,    // buy with bounty (현상금)
        BossReward,  // beat the region boss (requirementId = region id)
        Mastery      // character mastery level (masteryLevel)
    }

    [Serializable]
    public class SkinDefinition
    {
        public string id;
        public string characterId;
        public string nameKey;
        [Tooltip("Placeholder skin = body tint (no external art)")]
        public Color tint = Color.white;
        [Tooltip("How strongly the placeholder sprite is recolored toward tint (0 = untouched)")]
        [Range(0f, 1f)] public float recolorStrength = 0.7f;
        public SkinUnlockType unlock = SkinUnlockType.Default;
        public int cost;
        public string requirementId;
        public int masteryLevel;
    }

    [Serializable]
    public class RegionDefinition
    {
        public string id;
        public string nameKey;
        [Tooltip("StageData ids in play order (last = boss)")]
        public string[] stageIds;
        public string bossStageId;
        [Tooltip("Region unlocked by beating this region's boss (FP 528)")]
        public string nextRegionId;
        [Tooltip("Display only: wanted boss of the region and bounty (FP 220-228)")]
        public string bossNameKey;
        public int bossBounty;
    }

    /// <summary>
    /// Offline progression numbers (FP 522-529 성장 구조). No stat growth (03 머리말, decision D3):
    /// bounty (현상금) currency, sheriff badge (계정 레벨), character mastery → titles/skins, region/stage unlocks.
    /// Amounts not in the docs are proposals (decision D19).
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressionData", menuName = "HillNGun/Data/Progression")]
    public class ProgressionData : ScriptableObject
    {
        [Header("Bounty (현상금)")]
        public int startingBounty = 0;
        public int bountyPerStageClear = 100;
        [Tooltip("FP 220: 방울뱀 렉스 현상금 $1,000")]
        public int bountyPerBossDefeat = 1000;
        public int bountyPvPWin = 150;
        public int bountyPvPDraw = 50;
        public int bountyPvPLoss = 0;
        [Tooltip("보스·지역 해금 같은 첫 클리어 보상만 1회 (현상금은 매번)")]
        public bool stageBountyEveryClear = true;

        [Header("Sheriff Badge (계정 레벨) XP")]
        public int xpPerStageClear = 30;
        public int xpBossDefeat = 120;
        public int xpPvEFailedRun = 15;
        public int xpPvPWin = 80;
        public int xpPvPDraw = 50;
        public int xpPvPLoss = 40;
        public int badgeXpBase = 200;
        public int badgeXpPerLevel = 100;
        public int maxBadgeLevel = 50;
        [Tooltip("FP 525: 레벨마다 현상금")]
        public int bountyPerBadgeLevel = 200;

        [Header("Character Mastery (숙련도: 능력치 대신 칭호·스킨)")]
        public int masteryXpPerMatch = 20;
        public int masteryXpWinBonus = 15;
        [Tooltip("누적 XP 기준 (index = level-1)")]
        public int[] masteryLevelXp = new int[] { 0, 100, 250, 500, 900, 1500 };
        public string[] masteryTitleKeys = new string[] {
            "mastery.title.1", "mastery.title.2", "mastery.title.3",
            "mastery.title.4", "mastery.title.5", "mastery.title.6" };

        [Header("Unlocks")]
        public string[] startingCharacterIds = new string[] { "buster", "raven", "volt" };
        public RegionDefinition[] regions = new RegionDefinition[0];
        public SkinDefinition[] skins = new SkinDefinition[0];

        public int XpToNextBadge(int level) => badgeXpBase + badgeXpPerLevel * Mathf.Max(0, level - 1);

        public int MasteryLevelFor(int xp)
        {
            int lvl = 1;
            if (masteryLevelXp == null) return lvl;
            for (int i = 0; i < masteryLevelXp.Length; i++) if (xp >= masteryLevelXp[i]) lvl = i + 1;
            return lvl;
        }
    }
}

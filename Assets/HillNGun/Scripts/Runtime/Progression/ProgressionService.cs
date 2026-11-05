using System;
using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Progression
{
    /// <summary>
    /// Offline progression logic: rewards at match end, bounty wallet, sheriff badge level, character mastery,
    /// stage/region unlocks and skins. Numbers come from ProgressionData; state lives in SaveData (ISaveStorage).
    /// </summary>
    public class ProgressionService
    {
        public const string DefaultSaveFile = "hillngun_save.json";
        /// <summary>Alternate save file (smoke tests use their own slot so a real save is never touched).</summary>
        public static string SaveFileOverride;

        private static ProgressionService _instance;
        public static ProgressionService Instance => _instance ??= new ProgressionService(new FileSaveStorage(SaveFileOverride ?? DefaultSaveFile));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            GameFlowManager.RewardGranter = s => Instance.GrantMatchRewards(s);
            var p = Instance; // load + log once at boot
            Debug.Log($"[Progression] Loaded {p._storage.Location}: bounty=${p.Data.bounty} badge Lv.{p.Data.badgeLevel} ({p.Data.badgeXp}xp) cleared={p.Data.clearedStages.Count} skins={p.Data.ownedSkins.Count}");
        }

        private readonly ISaveStorage _storage;
        public SaveData Data { get; private set; }
        public event Action OnChanged;

        public ProgressionService(ISaveStorage storage)
        {
            _storage = storage;
            Load();
        }

        public ProgressionData Config
        {
            get
            {
                var c = ServiceLocator.Get<ProgressionData>() ?? RuntimeAssetManager.Instance?.Progression;
                if (c == null)
                {
                    _fallback ??= ScriptableObject.CreateInstance<ProgressionData>();
                    c = _fallback;
                }
                return c;
            }
        }
        private ProgressionData _fallback;

        public void Load()
        {
            SaveData d = null;
            if (_storage.TryRead(out string json))
            {
                try { d = JsonUtility.FromJson<SaveData>(json); }
                catch (Exception e) { Debug.LogWarning($"[Progression] Corrupt save, starting fresh: {e.Message}"); }
            }
            Data = d ?? NewSave();
            EnsureDefaults();
        }

        private SaveData NewSave()
        {
            var d = new SaveData();
            d.bounty = Config.startingBounty;
            return d;
        }

        /// <summary>Starting characters, default skins and the first region are always available.</summary>
        private void EnsureDefaults()
        {
            var cfg = Config;
            if (cfg.startingCharacterIds != null)
                foreach (var id in cfg.startingCharacterIds) AddUnique(Data.unlockedCharacters, id);
            if (cfg.regions != null && cfg.regions.Length > 0) AddUnique(Data.unlockedRegions, cfg.regions[0].id);
            if (cfg.skins != null)
                foreach (var s in cfg.skins) if (s != null && s.unlock == SkinUnlockType.Default) AddUnique(Data.ownedSkins, s.id);
            if (Data.ownedWeapons == null) Data.ownedWeapons = new List<string>();
            if (cfg.weaponCatalog != null && cfg.weaponCatalog.offers != null)
                foreach (var o in cfg.weaponCatalog.offers) if (o != null && o.ownedByDefault) AddUnique(Data.ownedWeapons, o.Id);
        }

        public void Save()
        {
            Data.lastSavedUtc = DateTime.UtcNow.ToString("o");
            _storage.Write(JsonUtility.ToJson(Data, true));
            OnChanged?.Invoke();
        }

        public void ResetAll()
        {
            _storage.Delete();
            Data = NewSave();
            EnsureDefaults();
            Save();
        }

        private static bool AddUnique(List<string> list, string id)
        {
            if (string.IsNullOrEmpty(id) || list.Contains(id)) return false;
            list.Add(id);
            return true;
        }

        // ---------------- queries ----------------

        public bool IsStageCleared(string stageId) => Data.clearedStages.Contains(stageId);

        public RegionDefinition GetRegionOfStage(string stageId)
        {
            var regions = Config.regions;
            if (regions == null) return null;
            foreach (var r in regions)
                if (r?.stageIds != null && Array.IndexOf(r.stageIds, stageId) >= 0) return r;
            return null;
        }

        /// <summary>Stage is playable when its region is unlocked and the previous stage of the list is cleared.</summary>
        public bool IsStageUnlocked(IList<StageData> stages, int index)
        {
            if (stages == null || index < 0 || index >= stages.Count || stages[index] == null) return false;
            var region = GetRegionOfStage(stages[index].id);
            if (region != null && !Data.unlockedRegions.Contains(region.id)) return false;
            if (index == 0) return true;
            return stages[index - 1] == null || IsStageCleared(stages[index - 1].id);
        }

        public int HighestUnlockedStageIndex(IList<StageData> stages)
        {
            int best = 0;
            if (stages == null) return 0;
            for (int i = 0; i < stages.Count; i++) if (IsStageUnlocked(stages, i)) best = i;
            return best;
        }

        public bool OwnsSkin(string skinId) => Data.ownedSkins.Contains(skinId);

        public SkinDefinition GetSkin(string skinId)
        {
            if (Config.skins == null) return null;
            foreach (var s in Config.skins) if (s != null && s.id == skinId) return s;
            return null;
        }

        public List<SkinDefinition> GetSkinsFor(string characterId)
        {
            var list = new List<SkinDefinition>();
            if (Config.skins != null)
                foreach (var s in Config.skins) if (s != null && s.characterId == characterId) list.Add(s);
            return list;
        }

        /// <summary>Equipped non-default skin (null = default look).</summary>
        public SkinDefinition GetEquippedSkin(string characterId)
        {
            var c = Data.GetCharacter(characterId, false);
            if (c == null || string.IsNullOrEmpty(c.equippedSkin)) return null;
            var s = GetSkin(c.equippedSkin);
            return s == null || s.unlock == SkinUnlockType.Default ? null : s;
        }

        public string GetEquippedSkinId(string characterId)
        {
            var c = Data.GetCharacter(characterId, false);
            return c != null && !string.IsNullOrEmpty(c.equippedSkin) ? c.equippedSkin : characterId + "_default";
        }

        /// <summary>Equipped skin tint for a character (null = default look).</summary>
        public Color? GetEquippedTint(string characterId)
        {
            var c = Data.GetCharacter(characterId, false);
            if (c == null || string.IsNullOrEmpty(c.equippedSkin)) return null;
            var s = GetSkin(c.equippedSkin);
            if (s == null || s.unlock == SkinUnlockType.Default) return null;
            return s.tint;
        }

        public string GetMasteryTitleKey(string characterId)
        {
            var c = Data.GetCharacter(characterId, false);
            int lvl = c != null ? c.masteryLevel : 1;
            var keys = Config.masteryTitleKeys;
            if (keys == null || keys.Length == 0) return null;
            return keys[Mathf.Clamp(lvl - 1, 0, keys.Length - 1)];
        }

        // ---------------- actions ----------------

        public bool TryPurchaseSkin(string skinId)
        {
            var s = GetSkin(skinId);
            if (s == null || OwnsSkin(skinId) || s.unlock != SkinUnlockType.Purchase) return false;
            if (Data.bounty < s.cost) return false;
            Data.bounty -= s.cost;
            Data.ownedSkins.Add(skinId);
            Save();
            Debug.Log($"[Progression] Bought {skinId} for ${s.cost}, bounty left ${Data.bounty}");
            return true;
        }

        // ---------------- guns (t48u) ----------------

        public WeaponCatalogData WeaponCatalog => Config.weaponCatalog;

        public bool OwnsWeapon(string weaponId) => !string.IsNullOrEmpty(weaponId) && Data.ownedWeapons != null && Data.ownedWeapons.Contains(weaponId);

        public bool TryPurchaseWeapon(string weaponId)
        {
            var o = WeaponCatalog?.Find(weaponId);
            if (o == null || OwnsWeapon(weaponId) || o.ownedByDefault) return false;
            if (Data.bounty < o.cost) return false;
            Data.bounty -= o.cost;
            Data.ownedWeapons.Add(weaponId);
            Save();
            Debug.Log($"[Progression] Bought gun {weaponId} for ${o.cost}, bounty left ${Data.bounty}");
            return true;
        }

        public bool EquipWeapon(string characterId, string weaponId)
        {
            var o = WeaponCatalog?.Find(weaponId);
            if (o == null || !o.AllowedFor(characterId) || !OwnsWeapon(weaponId)) return false;
            Data.GetCharacter(characterId, true).equippedWeapon = o.ownedByDefault ? null : weaponId;
            Save();
            return true;
        }

        /// <summary>Equipped shop gun for a character, or null = keep CharacterData.weapon (starting gun).</summary>
        public WeaponData GetEquippedWeapon(string characterId)
        {
            var c = Data.GetCharacter(characterId, false);
            if (c == null || string.IsNullOrEmpty(c.equippedWeapon)) return null;
            var o = WeaponCatalog?.Find(c.equippedWeapon);
            if (o == null || o.weapon == null || !o.AllowedFor(characterId) || !OwnsWeapon(o.Id)) return null;
            return o.weapon;
        }

        public string GetEquippedWeaponId(string characterId, WeaponData starting)
        {
            var w = GetEquippedWeapon(characterId);
            return w != null ? w.id : starting != null ? starting.id : null;
        }

        public bool EquipSkin(string characterId, string skinId)
        {
            var s = GetSkin(skinId);
            if (s == null || s.characterId != characterId || !OwnsSkin(skinId)) return false;
            Data.GetCharacter(characterId, true).equippedSkin = skinId;
            Save();
            return true;
        }

        public int GetStageStars(string stageId)
        {
            if (Data?.stageStars == null || string.IsNullOrEmpty(stageId)) return 0;
            foreach (var e in Data.stageStars) if (e != null && e.stageId == stageId) return e.stars;
            return 0;
        }

        public void RecordStageStars(string stageId, int stars)
        {
            if (Data == null || string.IsNullOrEmpty(stageId) || stars <= 0) return;
            if (Data.stageStars == null) Data.stageStars = new System.Collections.Generic.List<StageStarSave>();
            foreach (var e in Data.stageStars)
            {
                if (e != null && e.stageId == stageId)
                {
                    if (stars > e.stars) { e.stars = stars; Save(); }
                    return;
                }
            }
            Data.stageStars.Add(new StageStarSave { stageId = stageId, stars = stars });
            Save();
        }

        /// <summary>Grant everything a finished match earns. Called once per match by GameFlowManager.EndMatch.</summary>
        public RewardSummary GrantMatchRewards(GameSession s)
        {
            var cfg = Config;
            var r = new RewardSummary { badgeLevelBefore = Data.badgeLevel };
            bool win = s.result == MatchResult.Victory;

            if (s.mode == GameMode.PvE)
            {
                Data.pveRuns++;
                var stages = RuntimeAssetManager.Instance?.StageDatas;
                foreach (var id in s.clearedStageIds)
                {
                    StageData stData = null;
                    bool isBoss = false;
                    if (stages != null) foreach (var st in stages) if (st != null && st.id == id) { stData = st; isBoss = st.isBossStage; }
                    if (win && stData != null)
                    {
                        int stars = StarRating.ComputeFromSession(stData, s, true);
                        RecordStageStars(id, stars);
                        if (stars > 0) r.lines.Add(new RewardLine("reward.stars", stars.ToString()));
                    }
                    bool first = AddUnique(Data.clearedStages, id);
                    if (first || cfg.stageBountyEveryClear)
                        r.bountyGained += isBoss ? cfg.bountyPerBossDefeat : cfg.bountyPerStageClear;
                    r.xpGained += isBoss ? cfg.xpBossDefeat : cfg.xpPerStageClear;
                    if (isBoss)
                    {
                        Data.bossKills++;
                        if (first) GrantBossFirstClear(id, r);
                    }
                    else if (first && stages != null)
                    {
                        // next stage in the list becomes playable
                        for (int i = 0; i + 1 < stages.Length; i++)
                            if (stages[i] != null && stages[i].id == id && stages[i + 1] != null)
                                r.lines.Add(new RewardLine("reward.stage_unlocked", stages[i + 1].nameKey));
                    }
                }
                if (s.clearedStageIds.Count == 0) r.xpGained += cfg.xpPvEFailedRun;
            }
            else if (s.mode == GameMode.PvP)
            {
                switch (s.result)
                {
                    case MatchResult.Victory: Data.pvpWins++; r.bountyGained += cfg.bountyPvPWin; r.xpGained += cfg.xpPvPWin; break;
                    case MatchResult.Draw: Data.pvpDraws++; r.bountyGained += cfg.bountyPvPDraw; r.xpGained += cfg.xpPvPDraw; break;
                    default: Data.pvpLosses++; r.bountyGained += cfg.bountyPvPLoss; r.xpGained += cfg.xpPvPLoss; break;
                }
            }

            if (s.bonusBounty > 0)
            {
                r.bountyGained += s.bonusBounty;
                r.lines.Add(new RewardLine("reward.coins_picked", "$" + s.bonusBounty));
            }
            Data.bounty += r.bountyGained;
            AddBadgeXp(r.xpGained, r);
            if (s.selectedCharacterIds != null)
                foreach (var id in s.selectedCharacterIds)
                    if (!string.IsNullOrEmpty(id)) AddMastery(id, win, r);

            r.bountyTotal = Data.bounty;
            r.badgeLevelAfter = Data.badgeLevel;
            r.badgeXp = Data.badgeXp;
            r.badgeXpToNext = cfg.XpToNextBadge(Data.badgeLevel);
            Save();
            Debug.Log($"[Progression] {s.mode} {s.result}: +${r.bountyGained} (total ${r.bountyTotal}) +{r.xpGained}xp badge Lv.{r.badgeLevelBefore}->{r.badgeLevelAfter} lines={r.lines.Count}");
            foreach (var l in r.lines) Debug.Log($"[Progression]   {l.key} {l.arg}");
            return r;
        }

        private void GrantBossFirstClear(string bossStageId, RewardSummary r)
        {
            var regions = Config.regions;
            if (regions == null) return;
            foreach (var reg in regions)
            {
                if (reg == null || reg.bossStageId != bossStageId) continue;
                if (!string.IsNullOrEmpty(reg.nextRegionId) && AddUnique(Data.unlockedRegions, reg.nextRegionId))
                {
                    string nextKey = reg.nextRegionId;
                    foreach (var n in regions) if (n != null && n.id == reg.nextRegionId) nextKey = n.nameKey;
                    r.lines.Add(new RewardLine("reward.region_unlocked", nextKey));
                }
                if (Config.skins != null)
                    foreach (var sk in Config.skins)
                        if (sk != null && sk.unlock == SkinUnlockType.BossReward && sk.requirementId == reg.id && AddUnique(Data.ownedSkins, sk.id))
                            r.lines.Add(new RewardLine("reward.skin_unlocked", sk.nameKey));
            }
        }

        private void AddBadgeXp(int xp, RewardSummary r)
        {
            var cfg = Config;
            Data.badgeXp += xp;
            while (Data.badgeLevel < cfg.maxBadgeLevel && Data.badgeXp >= cfg.XpToNextBadge(Data.badgeLevel))
            {
                Data.badgeXp -= cfg.XpToNextBadge(Data.badgeLevel);
                Data.badgeLevel++;
                Data.bounty += cfg.bountyPerBadgeLevel;
                r.bountyGained += cfg.bountyPerBadgeLevel;
                r.lines.Add(new RewardLine("reward.badge_level", Data.badgeLevel.ToString()));
            }
        }

        private void AddMastery(string characterId, bool win, RewardSummary r)
        {
            var cfg = Config;
            var c = Data.GetCharacter(characterId, true);
            c.matches++;
            if (win) c.wins++;
            c.masteryXp += cfg.masteryXpPerMatch + (win ? cfg.masteryXpWinBonus : 0);
            int lvl = cfg.MasteryLevelFor(c.masteryXp);
            while (c.masteryLevel < lvl)
            {
                c.masteryLevel++;
                r.lines.Add(new RewardLine("reward.mastery_level", $"{characterId}|{c.masteryLevel}"));
                if (cfg.skins != null)
                    foreach (var sk in cfg.skins)
                        if (sk != null && sk.unlock == SkinUnlockType.Mastery && sk.characterId == characterId
                            && sk.masteryLevel <= c.masteryLevel && AddUnique(Data.ownedSkins, sk.id))
                            r.lines.Add(new RewardLine("reward.skin_unlocked", sk.nameKey));
            }
        }
    }
}

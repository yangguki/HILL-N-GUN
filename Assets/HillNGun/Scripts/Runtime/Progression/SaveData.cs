using System;
using System.Collections.Generic;

namespace HillNGun.Progression
{
    /// <summary>Offline save (JSON). Only player progress; no gameplay numbers live here.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int bounty;
        public int badgeLevel = 1;
        public int badgeXp;
        public List<string> unlockedCharacters = new List<string>();
        public List<string> clearedStages = new List<string>();
        public List<string> unlockedRegions = new List<string>();
        public List<string> ownedSkins = new List<string>();
        public List<CharacterSave> characters = new List<CharacterSave>();
        public int pveRuns;
        public int bossKills;
        public int pvpWins;
        public int pvpLosses;
        public int pvpDraws;
        public string lastSavedUtc;

        public CharacterSave GetCharacter(string id, bool create)
        {
            foreach (var c in characters) if (c.id == id) return c;
            if (!create) return null;
            var n = new CharacterSave { id = id };
            characters.Add(n);
            return n;
        }
    }

    [Serializable]
    public class CharacterSave
    {
        public string id;
        public int masteryXp;
        public int masteryLevel = 1;
        public int matches;
        public int wins;
        public string equippedSkin;
    }

    /// <summary>One result-screen line: localization key + optional argument.</summary>
    [Serializable]
    public class RewardLine
    {
        public string key;
        public string arg;
        public RewardLine(string key, string arg = null) { this.key = key; this.arg = arg; }
    }

    /// <summary>What a finished match granted (filled by ProgressionService, drawn by ResultUI).</summary>
    public class RewardSummary
    {
        public int bountyGained;
        public int bountyTotal;
        public int xpGained;
        public int badgeLevelBefore;
        public int badgeLevelAfter;
        public int badgeXp;
        public int badgeXpToNext;
        public List<RewardLine> lines = new List<RewardLine>();
    }
}

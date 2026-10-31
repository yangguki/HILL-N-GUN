using System;
using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Core
{
    [Serializable]
    public class GameSession
    {
        public GameMode mode;
        public string[] selectedCharacterIds = new string[3];
        public int currentStageIndex;
        public int currentRound;
        public MatchResult result;

        public int playerKills;
        public int playerDeaths;
        public int damageDealt;
        public int damageTaken;
        public int coversDestroyed;
        public int enemiesDefeated;
        public float matchTime;

        public int playerRoundsWon;
        public int enemyRoundsWon;

        public int pveGauge;
        public int pveGaugeMax;
        public int currentWave;
        public int totalWaves;

        /// <summary>PvE: first stage of this run (stage select; must be unlocked).</summary>
        public int startStageIndex;
        /// <summary>PvE: stage ids cleared during this run (for rewards / unlocks).</summary>
        public List<string> clearedStageIds = new List<string>();
        public bool bossDefeated;
        /// <summary>PvP: per-round results as (int)PvPRoundResult.</summary>
        public List<int> roundHistory = new List<int>();

        /// <summary>Filled once by ProgressionService when the match ends (shown on the result screen).</summary>
        [NonSerialized] public object rewards;
        public bool rewardsGranted;
        /// <summary>PvE: rewarded-ad continues used in this run (reset when a run starts from the lobby).</summary>
        public int continuesUsed;

        public void ResetMatchProgress()
        {
            result = MatchResult.None;
            playerKills = 0;
            playerDeaths = 0;
            damageDealt = 0;
            damageTaken = 0;
            coversDestroyed = 0;
            enemiesDefeated = 0;
            matchTime = 0f;
            playerRoundsWon = 0;
            enemyRoundsWon = 0;
            currentRound = 0;
            pveGauge = 0;
            currentWave = 0;
            clearedStageIds.Clear();
            bossDefeated = false;
            roundHistory.Clear();
            rewards = null;
            rewardsGranted = false;
        }

        public void Reset()
        {
            mode = GameMode.None;
            selectedCharacterIds = new string[3];
            currentStageIndex = 0;
            continuesUsed = 0;
            currentRound = 0;
            result = MatchResult.None;
            playerKills = 0;
            playerDeaths = 0;
            damageDealt = 0;
            damageTaken = 0;
            coversDestroyed = 0;
            enemiesDefeated = 0;
            matchTime = 0f;
            playerRoundsWon = 0;
            enemyRoundsWon = 0;
            pveGauge = 0;
            pveGaugeMax = 0;
            currentWave = 0;
            totalWaves = 0;
            startStageIndex = 0;
            clearedStageIds.Clear();
            bossDefeated = false;
            roundHistory.Clear();
            rewards = null;
            rewardsGranted = false;
        }

        public void SetCharacter(int slot, string characterId)
        {
            if (slot >= 0 && slot < selectedCharacterIds.Length)
            {
                selectedCharacterIds[slot] = characterId;
            }
        }

        public string GetCharacter(int slot)
        {
            if (slot >= 0 && slot < selectedCharacterIds.Length)
            {
                return selectedCharacterIds[slot];
            }
            return null;
        }
    }

    public class MatchStats
    {
        public int kills;
        public int deaths;
        public int damageDealt;
        public int damageTaken;
        public int coversDestroyed;
        public int enemiesDefeated;
        public float time;
        public int roundsWon;
        public int roundsLost;
    }
}

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

        public void Reset()
        {
            mode = GameMode.None;
            selectedCharacterIds = new string[3];
            currentStageIndex = 0;
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

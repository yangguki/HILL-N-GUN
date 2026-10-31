using UnityEngine;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "MatchRulesData", menuName = "HillNGun/Data/MatchRules")]
    public class MatchRulesData : ScriptableObject
    {
        [Header("Friendly Fire")]
        [Tooltip("아군 오사격 활성화 여부")]
        public bool friendlyFire = false;

        [Tooltip("자기 팀 엄폐물이 자기 팀 탄환을 막는지 여부 (false = 아군 엄폐물 너머로 사격 가능)")]
        public bool ownCoverBlocksOwnShots = false;

        [Header("PvE Party Scaling")]
        [Tooltip("PvE 파티 인원별 일반 적 체력 배율. 03 문서: 2인 협동 적 체력 1.5배")]
        public float[] pveEnemyHpMultipliers = new float[] { 1f, 1.5f, 3.4f };

        public float GetPvEEnemyHpMul(int partySize)
        {
            if (pveEnemyHpMultipliers == null || pveEnemyHpMultipliers.Length == 0) return 1f;
            return pveEnemyHpMultipliers[Mathf.Clamp(partySize - 1, 0, pveEnemyHpMultipliers.Length - 1)];
        }

        [Header("Round Settings")]
        [Tooltip("라운드 최대 시간 (초)")]
        public float roundMaxTime = 80f;

        [Tooltip("필요 승리 라운드 수")]
        public int roundsToWin = 2;

        [Tooltip("최대 라운드 수")]
        public int maxRounds = 3;
    }
}

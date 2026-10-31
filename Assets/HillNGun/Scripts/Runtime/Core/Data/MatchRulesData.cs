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

        [Header("Round Settings")]
        [Tooltip("라운드 최대 시간 (초)")]
        public float roundMaxTime = 80f;

        [Tooltip("필요 승리 라운드 수")]
        public int roundsToWin = 2;

        [Tooltip("최대 라운드 수")]
        public int maxRounds = 3;
    }
}

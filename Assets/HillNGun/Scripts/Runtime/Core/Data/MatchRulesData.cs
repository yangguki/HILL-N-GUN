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

        [Tooltip("라운드 시작 전 카운트다운 (초)")]
        public float roundStartDelay = 3f;

        [Tooltip("라운드 끝 결과 표시 시간 (초)")]
        public float roundEndDelay = 3f;

        [Header("PvP Spawn (기획: 양 팀 가운데 줄 x 6·12·18)")]
        public float[] pvpSpawnX = new float[] { 6f, 12f, 18f };
        [Tooltip("시작 줄 (0 = 우리 쪽 뒷줄 / 상대 쪽 앞줄 기준 각 팀 rows 배열 인덱스)")]
        public int pvpSpawnRowIndex = 1;

        [Header("PvP Timeout / Tiebreak (decision D10, D11)")]
        [Tooltip("기절자도 생존으로 셈 (탈락 전까지). 팀 전원이 기절이면 그 팀 패배")]
        public bool downedCountsAsAlive = true;
        [Tooltip("시간 초과 시 생존 인원이 같으면 남은 체력 합으로 판정")]
        public bool timeoutUseHpSum = true;
        [Tooltip("3라운드 뒤 라운드 승수가 같으면 총 피해량으로 판정 (같으면 무승부)")]
        public bool matchTiebreakByDamage = true;

        [Header("PvP Late Round (제안, 04 B-4 미정 → 토글 기본 꺼짐, decision D12)")]
        public bool supplyCrateEnabled = false;
        [Tooltip("라운드 시작 후 보급 상자가 떨어지는 시각 (초)")]
        public float supplyCrateTime = 50f;
        public int supplyCrateHp = 300;
        public float supplyCrateRadius = 0.6f;
        public Vector2 supplyCratePosition = new Vector2(12f, 9f);
        [Tooltip("상자를 연 팀 생존자 수류탄 전부 충전")]
        public bool supplyCrateRefillsGrenades = true;
        [Tooltip("상자를 연 팀 생존자 체력 회복 비율 (0 = 없음)")]
        public float supplyCrateHealRatio = 0f;
        public bool lateCoverDecayEnabled = false;
        [Tooltip("supplyCrateTime 이후 엄폐물이 받는 피해 배율")]
        public float lateCoverDamageMul = 1.5f;
    }
}

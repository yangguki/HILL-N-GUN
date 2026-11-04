using UnityEngine;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "DownedData", menuName = "HillNGun/Data/Downed")]
    public class DownedData : ScriptableObject
    {
        [Header("Downed State")]
        [Tooltip("기절 유지 시간 (초)")]
        public float downedDuration = 10f;

        [Tooltip("기본 소생 시간 (초)")]
        public float baseReviveTime = 3f;

        [Header("Downed HP")]
        [Tooltip("기절 체력 - 기절 중 맞으면 깎임")]
        public int downedHp = 600;

        [Header("Crawl")]
        [Tooltip("기어가기 속도 (타일/초)")]
        public float crawlSpeed = 0.5f;

        [Header("Revive")]
        [Tooltip("소생 범위 (타일)")]
        public float reviveRange = 1.0f;

        [Tooltip("소생 후 체력 비율")]
        [Range(0.1f, 1f)]
        public float reviveHpRatio = 0.3f;

        [Tooltip("소생 진행 중 기절 타이머 정지 (기획 미확정 → 토글, 결정 D16)")]
        public bool pauseDownedTimerWhileReviving = true;

        [Tooltip("소생자가 범위를 벗어나면 진행도 초기화 (false면 유지)")]
        public bool resetProgressWhenInterrupted = true;
    }
}

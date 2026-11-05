using UnityEngine;
using HillNGun.Core;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "CoverData", menuName = "HillNGun/Data/Cover")]
    public class CoverData : ScriptableObject
    {
        [Header("Identification")]
        public string id;
        public string nameKey;

        [Header("Durability")]
        [Tooltip("최대 내구도")]
        public int maxHp = 1500;

        [Header("Size")]
        [Tooltip("폭 (타일)")]
        public float width = 1.0f;

        [Tooltip("깊이 (타일)")]
        public float depth = 0.5f;

        [Header("Destruction")]
        [Tooltip("파괴 단계 수")]
        public int stages = 4;

        [Header("Repair")]
        [Tooltip("수리 가능 여부")]
        public bool repairable = true;

        [Tooltip("보스 바위 등: 피해 무시, 파괴 불가")]
        public bool indestructible = false;

        [Tooltip("라운드마다 재생성 여부")]
        public bool respawnPerRound = true;

        public CoverStage GetStage(int currentHp)
        {
            if (maxHp <= 0) return CoverStage.Destroyed;
            
            float ratio = (float)currentHp / maxHp;
            
            if (ratio <= 0f) return CoverStage.Destroyed;
            if (ratio <= 0.33f) return CoverStage.Damaged;
            if (ratio <= 0.66f) return CoverStage.Cracked;
            return CoverStage.Intact;
        }

        public float GetStageThreshold(CoverStage stage)
        {
            switch (stage)
            {
                case CoverStage.Intact: return 0.67f;
                case CoverStage.Cracked: return 0.34f;
                case CoverStage.Damaged: return 0.01f;
                default: return 0f;
            }
        }
    }
}

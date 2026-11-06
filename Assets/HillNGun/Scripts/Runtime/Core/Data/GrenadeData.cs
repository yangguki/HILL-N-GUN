using UnityEngine;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "GrenadeData", menuName = "HillNGun/Data/Grenade")]
    public class GrenadeData : ScriptableObject
    {
        [Header("Identification")]
        public string id;

        [Header("Damage")]
        [Tooltip("중심부 피해")]
        public int centerDamage = 900;

        [Tooltip("가장자리 피해")]
        public int edgeDamage = 450;

        [Tooltip("폭발 반경 (타일)")]
        public float radius = 1.5f;

        [Tooltip("중심부 반경 - 이 안은 중심 피해 (타일)")]
        public float innerRadius = 0.5f;

        [Header("Knockback")]
        [Tooltip("밀어내기 거리 (타일)")]
        public float knockback = 0f;

        [Header("Range")]
        [Tooltip("최소 투척 거리 (타일)")]
        public float minRange = 2f;

        [Tooltip("최대 투척 거리 (타일)")]
        public float maxRange = 9f;

        [Header("Flight")]
        [Tooltip("비행 시간 기본값 (초)")]
        public float flightTimeBase = 0.6f;

        [Tooltip("거리당 추가 비행 시간 (초/타일)")]
        public float flightTimePerTile = 0.04f;

        [Header("Cover Interaction")]
        [Tooltip("엄폐물 무시 여부")]
        public bool ignoresCover = true;

        [Tooltip("엄폐물에 대한 피해 배율")]
        public float coverDamageMul = 1.5f;

        [Header("Recharge")]
        [Tooltip("1개 충전 시간 (초)")]
        public float rechargeTime = 12f;

        [Header("View / VFX")]
        [Tooltip("Flight spin, trail, cartoon explode. Null = GrenadeVfxData.Builtin")]
        public GrenadeVfxData vfx;

        public float GetFlightTime(float distance)
        {
            return flightTimeBase + flightTimePerTile * distance;
        }

        public int GetDamageAtDistance(float distance)
        {
            if (distance <= innerRadius)
                return centerDamage;

            if (distance >= radius)
                return 0;

            float t = Mathf.InverseLerp(innerRadius, radius, distance);
            return Mathf.RoundToInt(Mathf.Lerp(centerDamage, edgeDamage, t));
        }
    }
}

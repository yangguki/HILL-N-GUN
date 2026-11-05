using UnityEngine;
using HillNGun.Core;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "HillNGun/Data/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identification")]
        public string id;
        [Tooltip("Localized display name key (shop / loadout)")]
        public string nameKey;
        [Tooltip("Localized one-line description key (shop / loadout)")]
        public string descKey;

        [Header("Fire Mode")]
        [Tooltip("발사 모드: Auto=연사, ChargeRelease=충전 후 발사")]
        public FireMode fireMode = FireMode.Auto;

        [Header("Damage")]
        [Tooltip("투사체 당 피해")]
        public int damagePerProjectile = 150;

        [Tooltip("한 번 발사당 투사체 수")]
        public int projectilesPerShot = 1;

        [Tooltip("산탄 퍼짐 각도 (도)")]
        public float spreadDeg = 0f;

        [Header("Projectile")]
        [Tooltip("탄속 (타일/초)")]
        public float projectileSpeed = 18f;

        [Tooltip("투사체 반경 (타일)")]
        public float projectileRadius = 0.3f;

        [Tooltip("사거리 (타일). v2 alias: rangeTiles")]
        public float range = 5f;

        [Tooltip("투사체 수명(초). 0 = range/projectileSpeed 자동")]
        public float projectileLifetime = 0f;

        [Tooltip("산탄: 전탄이 모이는 최대 거리 (타일). 0 = 미사용")]
        public float fullPelletRange = 0f;

        [Tooltip("피해 감쇠 시작 거리 (타일). 0 = 감쇠 없음")]
        public float damageFalloffStart = 0f;

        [Tooltip("사거리 끝에서의 피해 배율 (레이븐 0.7)")]
        [Range(0.1f, 1f)]
        public float damageFalloffEndMul = 1f;

        [Tooltip("조준 레이저 표시 (레이븐)")]
        public bool aimLaser = false;

        [Header("Fire Rate")]
        [Tooltip("발사 간격 (초)")]
        public float fireInterval = 0.6f;

        [Tooltip("Touch: keep firing at fireInterval while the attack stick is held aimed (Gatling). Off = one release, one shot")]
        public bool holdToRepeat = false;
        [Tooltip("View: hammer-cock animation on the attack button during the fire interval (revolver / lever action)")]
        public bool cockAnimation = false;

        [Header("Ammo Gauge")]
        [Tooltip("총 게이지 칸 수")]
        public int gaugeCells = 3;

        [Tooltip("한 칸당 발사 수")]
        public int shotsPerCell = 1;

        [Tooltip("한 칸 충전 시간 (초)")]
        public float rechargePerCell = 1.2f;

        [Header("Charge (Raven)")]
        [Tooltip("최소 충전 시간 (초)")]
        public float chargeMinTime = 0f;

        [Tooltip("최대 충전 시간 (초)")]
        public float chargeMaxTime = 0f;

        [Tooltip("최대 충전 시 피해 배율")]
        public float chargeDamageMul = 1f;

        [Header("Positioning")]
        [Tooltip("총구 전방 오프셋 (타일) - 자기 엄폐물에 막히지 않게")]
        public float muzzleForwardOffset = 1.0f;

        [Header("Cover Damage")]
        [Tooltip("엄폐물에 대한 피해 배율")]
        public float coverDamageMul = 1f;

        /// <summary>Doc alias for <see cref="range"/> (07_weapon_range.md rangeTiles).</summary>
        public float rangeTiles { get => range; set => range = value; }

        public float TimeToReachMaxRange => EffectiveLifetime;

        /// <summary>Lifetime used for travel; 0 field means range/speed.</summary>
        public float EffectiveLifetime =>
            projectileLifetime > 0.0001f ? projectileLifetime
            : (projectileSpeed > 0.0001f ? range / projectileSpeed : 0f);

        /// <summary>Damage multiplier by distance travelled (linear falloff after damageFalloffStart).</summary>
        public float GetDamageMultiplierAtDistance(float distance)
        {
            if (damageFalloffStart <= 0f) return 1f;
            if (distance <= damageFalloffStart) return 1f;
            float end = Mathf.Max(range, damageFalloffStart + 0.001f);
            float t = Mathf.InverseLerp(damageFalloffStart, end, distance);
            return Mathf.Lerp(1f, damageFalloffEndMul, t);
        }

        public int GetDamageAtDistance(int baseDamage, float distance) =>
            Mathf.RoundToInt(baseDamage * GetDamageMultiplierAtDistance(distance));

        public int GetChargeDamage(float chargeTime)
        {
            if (chargeMaxTime <= 0f || chargeMinTime >= chargeMaxTime)
                return damagePerProjectile;

            float t = Mathf.InverseLerp(chargeMinTime, chargeMaxTime, chargeTime);
            float damageMultiplier = Mathf.Lerp(1f, chargeDamageMul, t);
            return Mathf.RoundToInt(damagePerProjectile * damageMultiplier);
        }

        public float[] GetSpreadAngles() => GetSpreadAngles(spreadDeg);

        /// <summary>Fixed, evenly spaced pellet angles across totalSpreadDeg (no randomness).</summary>
        public float[] GetSpreadAngles(float totalSpreadDeg)
        {
            if (projectilesPerShot <= 1 || totalSpreadDeg <= 0f)
                return new float[] { 0f };

            float[] angles = new float[projectilesPerShot];
            float halfSpread = totalSpreadDeg / 2f;
            float step = totalSpreadDeg / (projectilesPerShot - 1);

            for (int i = 0; i < projectilesPerShot; i++)
            {
                angles[i] = -halfSpread + step * i;
            }
            return angles;
        }
    }
}

using UnityEngine;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "FeelData", menuName = "HillNGun/Data/Feel")]
    public class FeelData : ScriptableObject
    {
        [Header("Hit Flash")]
        [Tooltip("피격 시 흰색 번쩍 지속 시간 (초)")]
        public float hitFlashDuration = 0.06f;

        [Header("Hitstop")]
        [Tooltip("약한 명중 (< 300 피해) 히트스톱")]
        public float hitstopLight = 0f;

        [Tooltip("중간 명중 (300-899 피해) 히트스톱")]
        public float hitstopMedium = 0.03f;

        [Tooltip("강한 명중 (≥ 900 피해) 히트스톱")]
        public float hitstopHeavy = 0.06f;

        [Tooltip("수류탄 폭발 히트스톱")]
        public float hitstopGrenade = 0.04f;

        [Tooltip("엄폐물 단계 변화 히트스톱")]
        public float hitstopCoverStage = 0.03f;

        [Header("Screen Shake")]
        [Tooltip("강한 명중 시 흔들림 강도 (타일)")]
        public float shakeHeavyAmplitude = 0.08f;

        [Tooltip("강한 명중 시 흔들림 지속 시간 (초)")]
        public float shakeHeavyDuration = 0.1f;

        [Tooltip("엄폐물 변화 시 흔들림 강도 (타일)")]
        public float shakeCoverAmplitude = 0.08f;

        [Tooltip("엄폐물 변화 시 흔들림 지속 시간 (초)")]
        public float shakeCoverDuration = 0.15f;

        [Tooltip("수류탄 폭발 시 흔들림 강도 (타일)")]
        public float shakeGrenadeAmplitude = 0.12f;

        [Tooltip("수류탄 폭발 시 흔들림 지속 시간 (초)")]
        public float shakeGrenadeDuration = 0.2f;

        [Header("Vibration")]
        [Tooltip("큰 피해 시 진동 시간 (밀리초)")]
        public int vibrateBigDamageMs = 40;

        [Tooltip("큰 피해 임계값")]
        public int bigDamageThreshold = 500;

        [Header("Settings")]
        [Tooltip("화면 흔들림 활성화")]
        public bool shakeEnabled = true;

        [Tooltip("진동 활성화")]
        public bool vibrationEnabled = true;

        [Tooltip("히트스톱 활성화")]
        public bool hitStopEnabled = true;

        [Header("Shot visibility (view only)")]
        [Tooltip("Bullet head size (tiles at the near row)")]
        public float bulletHeadSize = 0.62f;
        [Tooltip("Bullet trail length (tiles), clamped to the distance travelled")]
        public float bulletTrailLength = 1.8f;
        [Tooltip("Bullet trail width (tiles at the near row)")]
        public float bulletTrailWidth = 0.28f;
        [Tooltip("Dark halo behind the bullet head for contrast on bright ground (0 = off)")]
        [Range(0f, 1f)] public float bulletHaloAlpha = 0.5f;
        public Color playerBulletColor = new Color(1f, 0.86f, 0.25f, 1f);
        public Color enemyBulletColor = new Color(1f, 0.36f, 0.2f, 1f);
        [Tooltip("Muzzle flash length (world units) / lifetime (s)")]
        public float muzzleFlashSize = 1.05f;
        public float muzzleFlashTime = 0.1f;
        [Tooltip("Muzzle flash spawns this far along the aim (fraction of WeaponData.muzzleForwardOffset)")]
        public float muzzleFlashForward = 0.55f;

        [Header("Cover destruction effect (view only)")]
        public int debrisSplinters = 16;
        public float debrisLifetime = 0.75f;

        public float hitStopLightDuration => hitstopLight;
        public float hitStopHeavyDuration => hitstopHeavy;

        public float GetHitstopDuration(int damage, bool isGrenade = false)
        {
            if (isGrenade) return hitstopGrenade;
            if (damage >= 900) return hitstopHeavy;
            if (damage >= 300) return hitstopMedium;
            return hitstopLight;
        }

        public bool ShouldVibrate(int damage)
        {
            return vibrationEnabled && damage >= bigDamageThreshold;
        }
    }
}

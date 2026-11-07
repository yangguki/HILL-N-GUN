using System;
using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>Pose / occlusion class only (12_terrain_props §2.2). Never a bullet-height or damage rule.</summary>
    public enum CoverHeightClass { None, Low, Half, Full }

    /// <summary>
    /// 12_terrain_props P0 rule set. EVERY value here is a PROPOSAL default from the planner handoff (2026-11-07),
    /// tunable in this SO — not a user-approved balance. Existing numbers (HP, widths, rows, 700/350/r2, x1.5) are
    /// mirrored here only where the doc fixes them as contract; CoverData / StageData / BattlefieldData stay the
    /// single authority for HP, widths and placements.
    /// </summary>
    [CreateAssetMenu(fileName = "terrain_rules", menuName = "HillNGun/Data/Terrain Rules (P0 proposal)")]
    public class TerrainRulesData : ScriptableObject
    {
        public const string ResourcePath = "Terrain/terrain_rules";

        [Header("Layout (제안 A, layoutVersion 기록)")]
        public string layoutId = "town_street_proposalA";
        public int layoutVersion = 1;
        [Tooltip("제안: 엄폐 중심 = 줄 + 0.60 (기존 오프셋 0.35~0.85 중간)")]
        public float coverCenterOffset = 0.60f;
        [Tooltip("제안 예외: 앞줄 엄폐 중심 오프셋 0.35 → 판정 경계가 중앙 빈 땅 7.6/10.4에 정확히 닿음")]
        public float frontRowCoverOffset = 0.35f;
        [Tooltip("중앙 빈 땅 (엄폐 판정 금지, 보급 상자만 예외)")]
        public float noCoverMinY = 7.6f;
        public float noCoverMaxY = 10.4f;
        [Tooltip("제안: 인접 엄폐 그림 간격 최소 (반경 0.5 + 양옆 0.3)")]
        public float minimumVisualGap = 1.6f;
        [Tooltip("PROPOSAL EXCEPTION (planner PvP town props 2026-11-07): adjacent props of these ids may sit closer than minimumVisualGap (paired wooden barrels, centres 1.3 apart). Still separate objects; must not overlap. Reported as a note, not hidden.")]
        public string[] pairedPropIds = { "cover_barrel" };
        public float pairedPropMinGap = 0.2f;
        [Tooltip("제안: 폭발통 중심 간격 > 4 (자동 연쇄가 기본 답이 되지 않도록)")]
        public float minBarrelSpacing = 4f;

        [Header("Damage states (제안: 연속 경계)")]
        [Tooltip("HP 비율 >= intactMin → Intact")] public float intactMin = 0.67f;
        [Tooltip("HP 비율 >= crackedMin → Cracked, (0, crackedMin) → Damaged, 0 → Destroyed")] public float crackedMin = 0.34f;
        [Tooltip("볼트 수리: 최대 HP 비율 (기존 스킬 파라미터와 동일 40%)")] public float repairPercent = 0.40f;

        [Header("Explosive barrel (06 기존 수치 + 12 제안 규칙)")]
        public int barrelTriggerDamage = 400;
        [Tooltip("제안: Armed → 폭발 경고 시간. 수류탄도 경고를 건너뛰지 않음")] public float barrelWarningSec = 0.4f;
        public int barrelCenterDamage = 700;
        public int barrelEdgeDamage = 350;
        public float barrelRadius = 2f;
        [Tooltip("제안: 이 반경까지 중심 피해")] public float barrelInnerRadius = 0.5f;
        public float barrelCoverDamageMul = 1.5f;
        [Tooltip("제안: 적·아군·소유자 모두 피해 (Rolling 무적은 캐릭터 피해에만)")] public bool barrelFriendlyDamage = true;
        [Tooltip("제안: 범위 내 통을 Armed로 전환 후 각자 경고 (큐, 중복 예약 금지)")] public bool barrelChainEnabled = true;
        public bool barrelGrenadeTriggers = true;
        public float barrelHitRadius = 0.45f;

        [Header("Mode defaults (제안: 기본 꺼짐)")]
        [Tooltip("제안: PvP 엄폐 파괴 랜덤 드롭 비활성 (PvE 드롭은 기존 유지)")] public bool pvpCoverDropsEnabled = false;

        [Header("Height classes (포즈·가림 범주만)")]
        public CoverClassBinding[] classes =
        {
            new CoverClassBinding { coverId = "cover_wagon", heightClass = CoverHeightClass.Full },
            new CoverClassBinding { coverId = "cover_rock", heightClass = CoverHeightClass.Full },
            new CoverClassBinding { coverId = "cover_swing_door", heightClass = CoverHeightClass.Half },
            new CoverClassBinding { coverId = "cover_barrel", heightClass = CoverHeightClass.Half },
            new CoverClassBinding { coverId = "cover_trough", heightClass = CoverHeightClass.Half },
            new CoverClassBinding { coverId = "cover_crate", heightClass = CoverHeightClass.Half },
            new CoverClassBinding { coverId = "cover_hay", heightClass = CoverHeightClass.Low },
        };

        public CoverHeightClass ClassOf(string coverId)
        {
            if (classes != null) foreach (var c in classes) if (c != null && c.coverId == coverId) return c.heightClass;
            return CoverHeightClass.Half;
        }
    }

    [Serializable]
    public class CoverClassBinding
    {
        public string coverId;
        public CoverHeightClass heightClass = CoverHeightClass.Half;
    }
}

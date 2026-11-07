using System;
using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>Cover size class used by the scale presets (sizes are relative to character height H).</summary>
    public enum CoverSizeType { Barrel, Crate, Wagon, SwingDoor, HayBale, Trough, Rock }

    /// <summary>Drawn size of one cover class in units of H (character height).</summary>
    [Serializable]
    public class CoverSizeEntry
    {
        public CoverSizeType type = CoverSizeType.Barrel;
        [Tooltip("그림 높이 (H 배수)")]
        public float heightH = 0.5f;
        [Tooltip("그림/판정 폭 (H 배수)")]
        public float widthH = 0.4f;
    }

    /// <summary>
    /// Drawn cover height as a fraction of the drawn actor height at the same depth (12_terrain_props §3/§4 —
    /// PROPOSAL defaults, not user-confirmed). View only: collision width / HP / protection never read it.
    /// </summary>
    [Serializable]
    public class CoverVisualHeightEntry
    {
        public CoverSizeType type = CoverSizeType.Barrel;
        [Tooltip("그림 높이 = 같은 깊이 캐릭터 표시 키 × 이 값 (제안값)")]
        public float ofActorHeight = 0.6f;
    }

    /// <summary>Which size class a CoverData id belongs to (logic id → size class; theme looks never change it).</summary>
    [Serializable]
    public class CoverTypeBinding
    {
        public string coverId;
        public CoverSizeType type = CoverSizeType.Barrel;
    }

    /// <summary>Cover layout for one stage (StageData.id) or PvP (<see cref="ScalePresetData.pvpLayoutKey"/>). Keeps the 3-row structure.</summary>
    [Serializable]
    public class StageCoverSet
    {
        public string stageKey;
        public CoverPlacement[] placements;
    }

    /// <summary>One A/B option: character height, cover sizes, layouts.</summary>
    [Serializable]
    public class ScalePresetOption
    {
        [Tooltip("표시 번호 (1/2/3)")]
        public int number = 3;
        [Tooltip("게임플레이 기준 키 (화면 세로 비율). 피격 반지름·엄폐 판정 폭·보호 규칙은 이 값만 읽음 — 그림 확대와 분리")]
        public float characterHeightFrac = 0.08f;
        [Tooltip("표시 키 = 화면 세로 비율 (1080p, 원근 1.0 = 가장 앞줄 기준). 0 = characterHeightFrac. 그림·라벨·엄폐 그림 높이만 읽음")]
        public float visualHeightFrac = 0f;

        /// <summary>Drawn character height fraction (falls back to the gameplay fraction).</summary>
        public float VisualHeightFrac => visualHeightFrac > 0f ? visualHeightFrac : characterHeightFrac;
        public CoverSizeEntry[] coverSizes = new CoverSizeEntry[0];
        public StageCoverSet[] layouts = new StageCoverSet[0];

        public CoverSizeEntry GetSize(CoverSizeType t)
        {
            if (coverSizes != null)
                foreach (var s in coverSizes) if (s != null && s.type == t) return s;
            return null;
        }

        public CoverPlacement[] GetLayout(string key)
        {
            if (layouts == null || string.IsNullOrEmpty(key)) return null;
            foreach (var l in layouts)
                if (l != null && l.stageKey == key && l.placements != null && l.placements.Length > 0) return l.placements;
            return null;
        }
    }

    /// <summary>
    /// Character / cover scale presets for the in-game A/B test (기획 비율 1/2/3안). Only drawn sizes and the hit /
    /// collision sizes tied to them change; weapon ranges, bullet speed, row spacing, HP and damage never read this.
    /// Loaded from Resources (<see cref="ResourcePath"/>) so every scene and the APK get it without scene wiring.
    /// </summary>
    [CreateAssetMenu(fileName = "scale_presets", menuName = "HillNGun/Data/Scale Presets")]
    public class ScalePresetData : ScriptableObject
    {
        public const string ResourcePath = "ScalePresets/scale_presets";

        [Header("Default")]
        [Tooltip("저장된 선택이 없을 때 쓰는 안 번호")]
        public int defaultOption = 2;

        [Header("Reference (the build the hit radii / cover widths were tuned for)")]
        [Tooltip("화면 세로 = 월드 유닛 (메인 카메라 orthographicSize × 2)")]
        public float screenHeightWorld = 20f;
        [Tooltip("기존 빌드의 캐릭터 키 (화면 세로 비율). CharacterData/EnemyData/BossData.hitRadius 는 이 크기 기준 값")]
        public float baselineCharacterHeightFrac = 0.126f;
        [Tooltip("기존 hitRadius 기준 캐릭터 피격 반지름 (타일) — 엄폐 보호 최소 폭 계산용")]
        public float referenceHurtRadius = 0.5f;

        [Header("Cover protection rule (same for every option)")]
        [Tooltip("엄폐물 판정 폭 ≥ 캐릭터 피격 지름 × 이 값. 엄폐물 바로 뒤에 붙은 캐릭터는 정면 사격에서 항상 보호됨 (그림 높이와 무관)")]
        public float coverMinWidthVsHurtDiameter = 1.0f;
        [Tooltip("그림 높이 최소값 (모델 Y 배율 하한, 너무 납작해지지 않게)")]
        public float minCoverYScale = 0.25f;
        [Tooltip("엄폐물 그림 폭 배율 (게임플레이 H 배수 폭 위에 곱함). 1.15 = 판정 폭과 같은 그림 폭 (12 §3). 높이는 coverVisualHeights")]
        public float coverVisualMul = 1.15f;
        [Tooltip("엄폐물 그림 높이 = 표시 캐릭터 키 × 비율 (12_terrain_props 제안값). 없는 종류는 heightH × 표시 H × coverVisualMul")]
        public CoverVisualHeightEntry[] coverVisualHeights = new CoverVisualHeightEntry[0];

        public float CoverHeightOfActor(CoverSizeType t)
        {
            if (coverVisualHeights != null)
                foreach (var e in coverVisualHeights) if (e != null && e.type == t && e.ofActorHeight > 0f) return e.ofActorHeight;
            return -1f;
        }
        [Tooltip("엄폐물 판정 폭 배율 (기획 v4: 폭만 1.15). 바닥 면적 깊이/높이는 그대로")]
        public float coverFootprintWidthMul = 1.15f;

        [Header("Cover classes")]
        public CoverTypeBinding[] coverTypes = new CoverTypeBinding[0];
        [Tooltip("안별 배치에서 쓰는 추가 엄폐물 (나무 상자, 건초 더미 등)")]
        public CoverData[] extraCovers = new CoverData[0];

        [Header("Options")]
        public ScalePresetOption[] options = new ScalePresetOption[0];
        [Tooltip("PvP 마을 큰길 배치 키")]
        public string pvpLayoutKey = "pvp_town_street";
        [Tooltip("12_terrain_props P0: 배치 권위는 StageData/BattlefieldData 한 곳. 켜면 (구) 안별 배치가 덮어씀 — 기본 꺼짐")]
        public bool layoutsOverrideStageData = false;

        [Header("HUD")]
        [Tooltip("화면 구석 표시 형식 ({0} = 안 번호)")]
        public string cornerLabelFormat = "비율 {0}";
        public int cornerLabelFontSize = 16;

        public ScalePresetOption GetOption(int number)
        {
            if (options == null) return null;
            foreach (var o in options) if (o != null && o.number == number) return o;
            return null;
        }

        public bool HasOption(int number) => GetOption(number) != null;

        public CoverSizeType TypeOf(string coverId, CoverSizeType fallback = CoverSizeType.Crate)
        {
            if (coverTypes != null)
                foreach (var b in coverTypes) if (b != null && b.coverId == coverId) return b.type;
            return fallback;
        }
    }
}

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
        [Tooltip("캐릭터 키 H = 화면 세로 비율 (1080p 세로 기준, 원근 1.0 = 가장 앞줄)")]
        public float characterHeightFrac = 0.08f;
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
        public int defaultOption = 3;

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

        [Header("Cover classes")]
        public CoverTypeBinding[] coverTypes = new CoverTypeBinding[0];
        [Tooltip("안별 배치에서 쓰는 추가 엄폐물 (나무 상자, 건초 더미 등)")]
        public CoverData[] extraCovers = new CoverData[0];

        [Header("Options")]
        public ScalePresetOption[] options = new ScalePresetOption[0];
        [Tooltip("PvP 마을 큰길 배치 키")]
        public string pvpLayoutKey = "pvp_town_street";

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

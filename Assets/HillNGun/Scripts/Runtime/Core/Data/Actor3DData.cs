using System;
using UnityEngine;

namespace HillNGun.Data
{
    public enum Actor3DHat { None, Cowboy, WitchWide, Bowler, Wide }
    public enum Actor3DHair { None, Short, Long, Curly }
    public enum Actor3DFace { None, Bandana, Mask, Snake }
    public enum Actor3DWeapon { None, Shotgun, Rifle, Pistol, DualPistol, Dynamite }

    /// <summary>
    /// One code-built stylized actor: proportions + palette + parts. Pure data (no meshes are stored);
    /// View3D builds the hierarchical low-poly rig from this at runtime.
    /// </summary>
    [Serializable]
    public class Actor3DModelDef
    {
        public string id = "buster";
        [Header("Proportions (model units, 1 ≈ 1 tile before modelScale)")]
        public float legLength = 0.62f;
        public float torsoHeight = 0.52f;
        public float headRadius = 0.30f;
        public float shoulderWidth = 0.50f;
        public float hipWidth = 0.34f;
        public float bodyDepth = 0.30f;
        public float armLength = 0.50f;
        public float limbRadius = 0.075f;
        [Tooltip("1 = 보통, >1 근육질 (팔·가슴)")]
        public float bulk = 1f;

        [Header("Palette")]
        public Color skin = new Color(0.93f, 0.72f, 0.55f);
        public Color hair = new Color(0.35f, 0.22f, 0.12f);
        public Color hatColor = new Color(0.50f, 0.32f, 0.18f);
        public Color hatBand = new Color(0.78f, 0.16f, 0.12f);
        public Color shirt = new Color(0.86f, 0.80f, 0.66f);
        public Color vest = new Color(0.48f, 0.30f, 0.16f);
        public Color pants = new Color(0.30f, 0.27f, 0.30f);
        public Color boots = new Color(0.30f, 0.18f, 0.10f);
        public Color belt = new Color(0.22f, 0.14f, 0.08f);
        public Color accent = new Color(0.80f, 0.15f, 0.12f);
        public Color metal = new Color(0.42f, 0.44f, 0.48f);
        public Color wood = new Color(0.52f, 0.32f, 0.16f);
        public Color coat = new Color(0.72f, 0.60f, 0.42f);
        public Color eyes = new Color(0.10f, 0.08f, 0.08f);

        [Header("Parts")]
        public Actor3DHat hat = Actor3DHat.Cowboy;
        public Actor3DHair hairStyle = Actor3DHair.Short;
        public Actor3DFace face = Actor3DFace.Bandana;
        public Actor3DWeapon weapon = Actor3DWeapon.Pistol;
        public bool hasVest = true;
        public bool neckerchief;
        public bool bareArms;
        public bool overalls;
        public bool cape;
        public bool longCoat;
        public bool bandolier;
        public bool mustache;
        public bool wrenchOnBack;
        public bool stripedShirt;
        [Tooltip("두 손으로 드는 무기 (샷건/라이플)")]
        public bool twoHanded;

        [Header("Creature extras")]
        public bool tail;
        public int tailSegments = 7;
        public float tailLength = 1.3f;
        public Color tailColor = new Color(0.42f, 0.55f, 0.22f);
        public Color tailPattern = new Color(0.82f, 0.72f, 0.45f);
        public bool mounted;
        public Color horseColor = new Color(0.45f, 0.27f, 0.14f);
        public Color horseMane = new Color(0.18f, 0.10f, 0.06f);
        [Tooltip("스킨 색 변경 시 모자도 바꿀지 (옷은 항상, 피부/금속/눈은 유지)")]
        public bool recolorHat = true;
    }

    /// <summary>
    /// 3D actor view settings (view only: logic stays 2D). useActor3D = false falls back to the sprite view.
    /// </summary>
    [CreateAssetMenu(fileName = "Actor3DData", menuName = "HillNGun/Data/Actor 3D")]
    public class Actor3DData : ScriptableObject
    {
        [Header("Toggle")]
        [Tooltip("켜면 캐릭터/적/보스를 3D 모델로 표시 (끄면 기존 2D 스프라이트)")]
        public bool useActor3D = true;
        [Tooltip("로비 캐릭터 탭에서 회전하는 3D 모델 표시")]
        public bool lobbyPreview3D = true;

        [Header("Camera / staging")]
        [Tooltip("3D 액터 전용 레이어 (오버레이 카메라가 이 레이어만 그림)")]
        public int actorLayer = 8;
        [Tooltip("액터를 카메라 쪽으로 기울이는 각도 — 위에서 내려다보는 느낌 (도)")]
        public float viewTiltDeg = 28f;
        [Tooltip("모델 크기 배율 (기울기로 줄어드는 높이 보정 포함)")]
        public float modelScale = 1.5f;
        public float enemyScaleMul = 1.0f;
        public float bossScaleMul = 0.95f;
        [Tooltip("로직 Y 1타일당 깊이 (뒤에 있는 액터가 가려지도록)")]
        public float depthPerTile = 0.6f;
        public float depthBase = 4f;

        [Header("Facing (yaw, 0 = 화면 위쪽 = 등이 보임)")]
        public float playerTeamYaw = 0f;
        public float enemyTeamYaw = 180f;

        [Header("Toon look")]
        public Vector3 lightDirection = new Vector3(-0.45f, 0.75f, -0.5f);
        [Range(0f, 1f)] public float shadowBrightness = 0.66f;
        public Color shadowColor = new Color(0.78f, 0.74f, 0.92f);
        [Range(0f, 1f)] public float rimStrength = 0.22f;
        [Range(0.001f, 0.3f)] public float bandSoftness = 0.04f;
        [Range(0f, 0.5f)] public float highlight = 0.08f;
        public float outlineWidth = 0.05f;
        [Tooltip("a = 바탕색 섞기 (0 = 단색 외곽선)")]
        public Color outlineColor = new Color(0.10f, 0.07f, 0.06f, 0.35f);
        public Color pvpOpponentTint = new Color(1f, 0.88f, 0.84f);
        public Color defeatedTint = new Color(0.62f, 0.62f, 0.62f);

        [Header("Cover props")]
        [Tooltip("엄폐물을 3D 소품으로 표시 (끄면 2D 스프라이트)")]
        public bool useCover3D = true;
        [Tooltip("엄폐물 3D 크기 배율 (1 = 로직 폭과 같음)")]
        public float coverScale = 1.0f;
        [Tooltip("엄폐물 바닥 위치 (로직 위치 기준 아래로, 타일)")]
        public float coverFootOffset = 0.35f;
        public CoverPalette coverPalette = new CoverPalette();

        [Header("Labels")]
        [Tooltip("이름표/경고 높이 (발 기준, 모델 단위)")]
        public float labelHeight = 2.25f;

        [Header("Lobby preview")]
        public int previewTextureSize = 512;
        public float previewSpinDegPerSec = 40f;
        public float previewTiltDeg = 10f;
        public float previewStartYaw = 200f;
        public float previewOutlineWidth = 0.012f;
        public Color previewBackground = new Color(0f, 0f, 0f, 0f);

        [Header("Models")]
        public Actor3DModelDef[] models;

        public Actor3DModelDef GetModel(string id)
        {
            if (models != null)
                foreach (var m in models) if (m != null && m.id == id) return m;
            return null;
        }

        private static Actor3DData _fallback;
        public static Actor3DData Fallback
        {
            get
            {
                if (_fallback == null) { _fallback = CreateInstance<Actor3DData>(); _fallback.hideFlags = HideFlags.DontSave; _fallback.models = Actor3DDefaults.Models(); }
                return _fallback;
            }
        }
    }

    /// <summary>Cover prop colours (data).</summary>
    [System.Serializable]
    public class CoverPalette
    {
        public Color wood = new Color(0.66f, 0.45f, 0.25f);
        public Color woodDark = new Color(0.43f, 0.27f, 0.15f);
        public Color door = new Color(0.74f, 0.52f, 0.30f);
        public Color metal = new Color(0.38f, 0.38f, 0.42f);
        public Color water = new Color(0.32f, 0.58f, 0.78f);
        public Color sack = new Color(0.86f, 0.78f, 0.60f);
        public Color crack = new Color(0.16f, 0.10f, 0.07f);
        public Color dust = new Color(0.62f, 0.50f, 0.36f);
        public static readonly CoverPalette Default = new CoverPalette();
    }
}

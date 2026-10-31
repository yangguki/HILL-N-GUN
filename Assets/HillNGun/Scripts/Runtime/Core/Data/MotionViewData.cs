using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>
    /// View-only motion feel (bounce / lean / squash / flip / dust / shadow / roll tumble / afterimages).
    /// Nothing here changes gameplay; views read it, logic never does.
    /// </summary>
    [CreateAssetMenu(fileName = "MotionViewData", menuName = "HillNGun/Data/Motion View")]
    public class MotionViewData : ScriptableObject
    {
        [Header("Art facing")]
        [Tooltip("캐릭터 원본 그림이 오른쪽을 보고 있으면 켬 (덱 그림은 왼쪽)")]
        public bool heroArtFacesRight = false;
        [Tooltip("적 원본 그림이 오른쪽을 보고 있으면 켬")]
        public bool enemyArtFacesRight = false;
        [Tooltip("발 위치 (스프라이트 중심 기준, 로컬 단위)")]
        public float heroFootOffset = -0.88f;
        [Tooltip("사격 후 이 시간 동안은 이동 방향 대신 조준 방향을 본다 (초)")]
        public float aimFacingHold = 0.45f;
        [Tooltip("좌우 뒤집기 시간 (초) — 0이면 즉시")]
        public float flipTime = 0.06f;
        [Tooltip("뒤집는 도중 최소 가로 폭 (0이면 종이처럼 사라짐)")]
        [Range(0f, 1f)] public float flipMinWidth = 0.35f;
        [Tooltip("조준/속도로 인한 방향 전환은 이 시간 이상 유지될 때만 (떨림 방지, 초)")]
        public float facingHoldTime = 0.1f;
        [Tooltip("조준 방향으로 돌아보는 최소 가로 성분")]
        public float aimFacingMinX = 0.25f;

        [Header("Walk")]
        [Tooltip("걸음 튐 높이 (로컬 단위)")]
        public float bobHeight = 0.07f;
        [Tooltip("타일당 걸음 수")]
        public float stepsPerTile = 1.25f;
        [Tooltip("걸을 때 세로 늘어남 (걸음에 맞춰)")]
        public float walkStretch = 0.035f;
        [Tooltip("이동 방향으로 기울기 최대 (도)")]
        public float leanMaxDeg = 8f;
        [Tooltip("기울기 따라가는 속도")]
        public float leanSharpness = 14f;

        [Header("Squash & stretch (spring)")]
        [Tooltip("출발 시 늘어남")]
        public float startStretch = 0.10f;
        [Tooltip("정지 시 눌림")]
        public float stopSquash = 0.10f;
        [Tooltip("스프링 강도")]
        public float springStiffness = 260f;
        [Tooltip("스프링 감쇠")]
        public float springDamping = 16f;

        [Header("Row change hop")]
        [Tooltip("줄 이동 중 살짝 뛰는 높이 (로컬 단위)")]
        public float rowHopHeight = 0.18f;

        [Header("Roll")]
        [Tooltip("구르기 회전 각도 (도). 진행 곡선을 따라 감")]
        public float rollSpinDeg = 360f;
        [Tooltip("구르기 시작 눌림")]
        public float rollSquash = 0.16f;
        [Tooltip("착지 눌림")]
        public float landSquash = 0.14f;
        [Tooltip("잔상 간격 (초)")]
        public float afterimageInterval = 0.04f;
        [Tooltip("잔상 수명 (초)")]
        public float afterimageLifetime = 0.2f;
        [Tooltip("잔상 시작 투명도")]
        [Range(0f, 1f)] public float afterimageAlpha = 0.45f;
        public Color afterimageTint = new Color(0.75f, 0.9f, 1f, 1f);

        [Header("Dust")]
        [Tooltip("걸음마다 먼지")]
        public bool dustOnSteps = true;
        [Tooltip("출발/정지/방향 전환 먼지 개수")]
        public int dustBurstCount = 3;
        [Tooltip("구르기 먼지 개수 (시작 + 착지)")]
        public int rollDustCount = 5;
        [Tooltip("구르는 동안 먼지 간격 (초)")]
        public float rollDustInterval = 0.05f;
        public float dustSize = 0.32f;
        public float dustLifetime = 0.38f;
        public Color dustColor = new Color(0.86f, 0.74f, 0.55f, 0.55f);

        [Header("Soft drop shadow")]
        public Color shadowColor = new Color(0.05f, 0.03f, 0.02f, 0.38f);
        [Tooltip("발 위치·그림자 폭을 스프라이트의 불투명 영역에서 자동 측정")]
        public bool autoFootFromSprite = true;
        [Tooltip("그림자 폭 = 불투명 폭 × 이 값 (캐릭터)")]
        public float heroShadowWidth = 0.62f;
        [Tooltip("그림자 폭 = 불투명 폭 × 이 값 (적)")]
        public float enemyShadowWidth = 0.62f;
        [Tooltip("그림자 폭 = 불투명 폭 × 이 값 (보스)")]
        public float bossShadowWidth = 0.55f;
        [Tooltip("그림자 세로/가로 비율")]
        public float shadowAspect = 0.3f;
        [Tooltip("발 위치를 불투명 바닥보다 이만큼 위로 (불투명 높이 비율)")]
        public float footInset = 0.03f;
        [Tooltip("자동 측정 실패 시 그림자 크기 (로컬 단위)")]
        public Vector2 fallbackShadowSize = new Vector2(1.0f, 0.32f);
        [Tooltip("뛰어오를 때 그림자 줄어드는 비율")]
        public float shadowShrinkPerUnit = 0.9f;

        [Header("Enemy / boss motion")]
        public float enemyBobHeight = 0.05f;
        public float enemyLeanMaxDeg = 6f;
        public float bossBreathe = 0.015f;

        private static MotionViewData _fallback;
        public static MotionViewData Fallback
        {
            get
            {
                if (_fallback == null) { _fallback = CreateInstance<MotionViewData>(); _fallback.hideFlags = HideFlags.DontSave; }
                return _fallback;
            }
        }
    }
}

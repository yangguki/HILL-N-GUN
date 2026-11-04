using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>
    /// Procedural animation knobs for the 3D actors (view only). Angles in degrees, distances in model units.
    /// Phase curves take the normalised gait phase (0..1, one full stride = two steps).
    /// </summary>
    [CreateAssetMenu(fileName = "Actor3DAnimData", menuName = "HillNGun/Data/Actor 3D Anim")]
    public class Actor3DAnimData : ScriptableObject
    {
        [Header("Walk — stride synced to speed")]
        [Tooltip("한 보폭 사이클(두 걸음) 동안 이동하는 타일 수")]
        public float strideLength = 1.35f;
        [Tooltip("다리 스윙 최대 각도 (도)")]
        public float maxLegSwingDeg = 34f;
        [Tooltip("다리 스윙 = 보폭으로 계산한 각도 × 이 값")]
        public float legSwingScale = 1f;
        [Tooltip("무릎 굽힘 곡선 (위상 0..1 → 0..1), 0 = 앞다리 착지")]
        public AnimationCurve kneeCurve = new AnimationCurve(
            new Keyframe(0f, 0.08f), new Keyframe(0.12f, 0.18f), new Keyframe(0.3f, 0.05f),
            new Keyframe(0.55f, 0.35f), new Keyframe(0.72f, 1f), new Keyframe(0.88f, 0.45f), new Keyframe(1f, 0.08f));
        public float kneeMaxDeg = 62f;
        [Tooltip("발이 지면과 평행을 유지하는 정도 (0..1)")]
        [Range(0f, 1f)] public float footFlatten = 0.75f;
        [Tooltip("엉덩이 상하 (걸음당 2회)")]
        public float hipBob = 0.055f;
        [Tooltip("엉덩이 좌우 기울기 (도)")]
        public float hipSwayDeg = 4f;
        [Tooltip("엉덩이 비틀림 (도)")]
        public float hipTwistDeg = 7f;
        [Tooltip("어깨 반대 비틀림 (도) — 팔 스윙과 함께")]
        public float shoulderCounterDeg = 6f;
        public float armSwingDeg = 32f;
        public float elbowBaseDeg = 14f;
        public float elbowSwingDeg = 22f;
        [Tooltip("최고 속도에서 상체 앞 기울기 (도)")]
        public float runLeanDeg = 7f;
        [Tooltip("걷기 ↔ 정지 블렌드 속도")]
        public float gaitBlendSharpness = 9f;
        [Tooltip("이 속도(타일/초) 이하이면 정지 자세")]
        public float minWalkSpeed = 0.25f;
        [Tooltip("멈출 때 다리가 모이는 속도")]
        public float settleSharpness = 10f;

        [Header("Facing — legs follow movement, torso follows aim")]
        public float legYawSharpness = 11f;
        public float torsoYawSharpness = 16f;
        [Tooltip("골반 대비 상체 최대 비틀림 (도)")]
        public float torsoTwistMaxDeg = 70f;
        public float headTurnMaxDeg = 45f;
        [Tooltip("이동 방향과 시선 차이가 이 각도보다 크면 뒷걸음")]
        public float backpedalAngleDeg = 115f;
        [Tooltip("조준하지 않을 때 상체가 이동 방향으로 돌아가는 정도 (0 = 항상 기본 방향(등), 1 = 이동 방향)")]
        [Range(0f, 1f)] public float torsoFollowMove = 0.35f;
        [Tooltip("사격 후 조준 방향 유지 시간 (초)")]
        public float aimHoldTime = 0.6f;

        [Header("Idle")]
        public float breathRate = 0.35f;
        public float breathScale = 0.022f;
        public float idleSwayDeg = 1.6f;
        public float idleArmDeg = 6f;

        [Header("Aim / shoot")]
        public float aimArmPitchDeg = -82f;
        public float supportArmPitchDeg = -64f;
        public float supportArmInDeg = 34f;
        public float aimBlendSharpness = 14f;
        public float recoilArmDeg = 22f;
        public float recoilSpineDeg = 7f;
        public float recoilBackDist = 0.06f;
        public float recoilDecay = 9f;

        [Header("Row change hop")]
        public float rowHopHeight = 0.38f;
        public float rowHopTuckDeg = 30f;

        [Header("Roll — tuck + somersault (timing from MovementFeel RollEase)")]
        public float rollSpinDeg = 360f;
        public float rollLift = 0.22f;
        public float rollPivotHeight = 0.25f;
        [Tooltip("웅크림 정도 곡선 (진행 0..1 → 0..1)")]
        public AnimationCurve rollTuckCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.15f, 0.9f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f));
        public float tuckThighDeg = 105f;
        public float tuckKneeDeg = 125f;
        public float tuckSpineDeg = 38f;
        public float tuckArmDeg = -55f;
        public float landSquash = 0.12f;
        public float landSquashTime = 0.14f;

        [Header("Hit / downed / revive")]
        public float flinchDeg = 16f;
        public float flinchDecay = 10f;
        public float downedLieDeg = 82f;
        public float downedDrop = 0.42f;
        public float downedBlendTime = 0.35f;
        public float reviveBlendTime = 0.45f;
        public int dizzyStarCount = 3;
        public float dizzyStarRadius = 0.32f;
        public float dizzyStarSpinDeg = 220f;
        public Color dizzyStarColor = new Color(1f, 0.88f, 0.25f);
        public Color defeatedStarColor = new Color(0.85f, 0.85f, 0.9f);

        [Header("Enemies")]
        public float throwArmDeg = -170f;
        public float throwTime = 0.45f;
        public float warnRaiseDeg = -40f;
        [Tooltip("도망칠 때 속도 배율 (보폭 계산)")]
        public float fleeStrideMul = 1.2f;

        [Header("Horse")]
        public float horseStrideLength = 2.2f;
        public float horseLegSwingDeg = 32f;
        public float horseBob = 0.06f;
        public float horseIdleBreath = 0.015f;

        [Header("Boss")]
        public float tailSwayDeg = 14f;
        public float tailSwayRate = 1.3f;
        public float tailSweepDeg = 120f;
        public float tailSweepTime = 0.55f;
        public float tailWarnCoilDeg = 35f;
        public float fanSpreadDeg = 28f;
        public float fanRearDeg = -8f;
        public float bossBreathScale = 0.03f;

        public float RollTuck(float p) => rollTuckCurve != null && rollTuckCurve.length > 0 ? Mathf.Clamp01(rollTuckCurve.Evaluate(p)) : Mathf.Sin(Mathf.PI * Mathf.Clamp01(p));
        public float Knee(float phase01) => kneeCurve != null && kneeCurve.length > 0 ? kneeCurve.Evaluate(Mathf.Repeat(phase01, 1f)) : Mathf.Max(0f, Mathf.Sin(phase01 * 2f * Mathf.PI));

        private static Actor3DAnimData _fallback;
        public static Actor3DAnimData Fallback
        {
            get
            {
                if (_fallback == null) { _fallback = CreateInstance<Actor3DAnimData>(); _fallback.hideFlags = HideFlags.DontSave; }
                return _fallback;
            }
        }
    }
}

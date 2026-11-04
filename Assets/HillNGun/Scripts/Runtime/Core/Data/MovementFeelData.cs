using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>
    /// Logic-side movement feel (01 §3 이동, §9 구르기). Only shapes HOW the character reaches the
    /// design values: top speeds (CharacterData), row-change time (2.5 tiles / speed×1.5),
    /// roll distance / duration / i-frames / cooldown (RollData) stay untouched.
    /// </summary>
    [CreateAssetMenu(fileName = "MovementFeelData", menuName = "HillNGun/Data/Movement Feel")]
    public class MovementFeelData : ScriptableObject
    {
        [Header("Horizontal acceleration")]
        [Tooltip("정지 → 최고 속도까지 걸리는 시간 (초). 0이면 즉시")]
        public float accelTime = 0.09f;
        [Tooltip("최고 속도 → 정지까지 걸리는 시간 (초). 0이면 즉시")]
        public float decelTime = 0.07f;
        [Tooltip("반대 방향 입력 시 감속 배수 (방향 전환이 미끄러지지 않게)")]
        public float turnDecelMul = 1.8f;
        [Tooltip("가속 곡선 (x = 현재 속도/최고 속도, y = 가속 배율). 출발이 빠르고 끝이 부드럽게")]
        public AnimationCurve accelCurve = new AnimationCurve(new Keyframe(0f, 1.6f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0.6f));

        [Header("Analog input (touch joystick / gamepad / keyboard)")]
        [Tooltip("이 값 아래 입력 크기는 0으로 본다")]
        public float analogDeadzone = 0.12f;
        [Tooltip("이 값 이상 밀면 최고 속도 (AI·키보드는 항상 최고 속도)")]
        public float analogFullSpeedAt = 0.55f;
        [Tooltip("데드존 바로 위에서의 속도 비율 (살살 밀어 걷기)")]
        [Range(0f, 1f)] public float analogMinSpeedFactor = 0.4f;

        [Header("Row change (줄 이동) — duration stays 2.5 / (speed × 1.5)")]
        [Tooltip("0 = 직선, 1 = 부드러운 시작/끝 (SmoothStep). 소요 시간은 그대로")]
        [Range(0f, 1f)] public float rowChangeEase = 1f;

        [Header("Roll (구르기) — distance / duration / i-frames from RollData")]
        [Tooltip("구르기 이동 곡선 지수: 1 = 등속, 클수록 빠른 출발 → 감속")]
        public float rollEasePower = 2.2f;
        [Tooltip("착지 회복 시간 (초). 이 동안 구르기/사격은 버퍼에 쌓였다가 끝나면 실행")]
        public float rollRecoveryTime = 0.08f;
        [Tooltip("착지 회복 중 이동 속도 배율")]
        [Range(0f, 1f)] public float rollRecoveryMoveMul = 0.45f;
        [Tooltip("착지 회복 중 사격 시작 막기 (버퍼로 이어짐)")]
        public bool recoveryBlocksActions = true;
        [Tooltip("구르기 끝에 남기는 좌우 속도 비율 (자연스러운 미끄러짐). 위치 변화는 0.05타일 미만")]
        [Range(0f, 1f)] public float rollExitCarry = 0.35f;
        [Tooltip("구르기 중 대각선 입력은 좌우 구르기로 처리 (줄 밖으로 밀려나지 않음)")]
        public bool horizontalRollOnly = true;

        [Header("Input buffer")]
        [Tooltip("구르기/회복/쿨 끝나기 이 시간 전 안에 누른 구르기·사격·수류탄·줄 이동을 기억했다 실행. <0이면 RollData.inputBuffer 사용")]
        public float inputBufferWindow = -1f;

        public float AnalogSpeedFactor(float magnitude)
        {
            if (magnitude < analogDeadzone) return 0f;
            float span = Mathf.Max(0.001f, analogFullSpeedAt - analogDeadzone);
            float f = Mathf.Clamp01((magnitude - analogDeadzone) / span);
            return Mathf.Lerp(analogMinSpeedFactor, 1f, f);
        }

        public float RollEase(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, Mathf.Max(0.01f, rollEasePower));
        }

        public float RowEase(float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Lerp(t, t * t * (3f - 2f * t), rowChangeEase);
        }

        private static MovementFeelData _fallback;
        /// <summary>Defaults when no asset is registered (tests, sandbox scenes).</summary>
        public static MovementFeelData Fallback
        {
            get
            {
                if (_fallback == null) { _fallback = CreateInstance<MovementFeelData>(); _fallback.hideFlags = HideFlags.DontSave; }
                return _fallback;
            }
        }
    }
}

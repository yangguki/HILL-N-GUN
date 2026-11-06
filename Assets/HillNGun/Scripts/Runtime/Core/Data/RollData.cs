using UnityEngine;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "RollData", menuName = "HillNGun/Data/Roll")]
    public class RollData : ScriptableObject
    {
        [Header("Identification")]
        public string id;

        [Header("Movement")]
        [Tooltip("구르기 거리 (타일)")]
        public float distance = 2.5f;

        [Tooltip("구르기 지속 시간 (초)")]
        public float duration = 0.4f;

        [Header("Invulnerability")]
        [Tooltip("무적 시작 시점 (초)")]
        public float invulnStart = 0f;

        [Tooltip("무적 지속 시간 (초)")]
        public float invulnDuration = 0.35f;

        [Header("Cooldown")]
        [Tooltip("재사용 대기 시간 (초)")]
        public float cooldown = 0f; // 0 = consecutive rolls (i-frames only while Rolling)

        [Header("Behavior Options")]
        [Tooltip("구르기 중 사격 가능 여부")]
        public bool canFireDuring = false;

        [Tooltip("입력 버퍼 시간 (초)")]
        public float inputBuffer = 0.1f;

        [Tooltip("위/아래 구르기로 줄 이동 허용")]
        public bool allowRowRoll = true;

        public float Speed => distance / duration;

        public bool IsInvulnerable(float elapsedTime)
        {
            return elapsedTime >= invulnStart && elapsedTime < (invulnStart + invulnDuration);
        }
    }
}

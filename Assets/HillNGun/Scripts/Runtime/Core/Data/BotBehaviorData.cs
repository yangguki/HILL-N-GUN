using UnityEngine;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "bot_behavior_", menuName = "HillNGun/Bot Behavior Data")]
    public class BotBehaviorData : ScriptableObject
    {
        [Header("Decision Making")]
        [Tooltip("Time between AI decisions")]
        public float decisionInterval = 0.3f;

        [Header("Targeting")]
        [Tooltip("Weight for closer enemies")]
        [Range(0f, 2f)]
        public float distanceWeight = 1f;

        [Tooltip("Weight for lower HP enemies")]
        [Range(0f, 2f)]
        public float lowHpWeight = 0.5f;

        [Header("Aiming")]
        [Tooltip("Time to aim before firing")]
        public float aimDuration = 0.4f;

        [Tooltip("Random aim variation in degrees")]
        [Range(0f, 30f)]
        public float aimJitter = 5f;

        [Header("Movement")]
        [Tooltip("Chance to change rows instead of moving horizontally")]
        [Range(0f, 1f)]
        public float rowChangeChance = 0.3f;

        [Header("Dodging")]
        [Tooltip("HP ratio below which bot considers dodging")]
        [Range(0f, 1f)]
        public float dodgeHpThreshold = 0.5f;

        [Tooltip("Chance to dodge when HP is low")]
        [Range(0f, 1f)]
        public float dodgeChance = 0.4f;

        [Header("Bullet dodge (t48u): react to incoming bullets with a roll, or a side-step when the roll is cooling down")]
        public HillNGun.Combat.DodgeTuning bulletDodge = new HillNGun.Combat.DodgeTuning { chance = 0.25f, delayMin = 0.15f, delayMax = 0.35f, lookahead = 0.6f, margin = 0.2f, cooldown = 2.5f };
        public float sidestepTime = 0.3f;

        [Header("Abilities")]
        [Tooltip("Chance to use grenade when available")]
        [Range(0f, 1f)]
        public float grenadeUseChance = 0.3f;

        [Tooltip("Chance to use skill when available")]
        [Range(0f, 1f)]
        public float skillUseChance = 0.2f;

        [Header("Reviving")]
        [Tooltip("Priority for reviving downed allies")]
        [Range(0f, 1f)]
        public float revivePriority = 0.6f;

        [Tooltip("Distance at which bot will move to revive")]
        public float reviveSeekDistance = 5f;

        [Header("Positioning (PvE)")]
        [Tooltip("사거리 안에 적이 없으면 앞 줄로 전진 (근거리 돌격형이 적에게 닿게 함)")]
        public bool advanceWhenOutOfRange = true;
        [Tooltip("보스 꼬리 휩쓸기 바닥 경고 안에 있으면 옆으로 피함")]
        public bool dodgeTailSweep = true;

        [Header("Abilities (PvE allies / PvP bots)")]
        [Tooltip("스킬을 쓸 수 있을 때 사용")]
        public bool useSkills = true;
        [Tooltip("볼트 수리: 이 비율 아래로 깎인 아군 엄폐물을 고침")]
        public float repairCoverBelowRatio = 0.6f;
        [Tooltip("사거리 안 적에게 수류탄 투척")]
        public bool useGrenades = true;
        [Tooltip("수류탄 투척 최소 간격 (초)")]
        public float grenadeMinInterval = 4f;
        [Tooltip("PvE AI 아군 수류탄 최소 간격 (초). 수류탄 12초 재충전(t49u 수정) 후에도 스테이지 길이(03 §4)를 지키기 위한 값. 0 이하 = grenadeMinInterval 사용")]
        public float pveAllyGrenadeMinInterval = 45f;
        [Tooltip("PvE AI 아군 사격 쿨다운 배율 (1=그대로). 스테이지 목표 시간(06) 맞추기용 페이싱 노브. 무기 fireInterval 자체는 변경하지 않음")]
        [Range(1f, 3f)]
        public float pveAllyShotCooldownMul = 1.25f;
        [Tooltip("기절한 아군에게 가서 소생")]
        public bool reviveAllies = true;
    }
}

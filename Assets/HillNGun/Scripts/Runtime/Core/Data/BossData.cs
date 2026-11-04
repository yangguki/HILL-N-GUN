using UnityEngine;
using System;
using HillNGun.Core;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "boss_", menuName = "HillNGun/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string nameKey;

        [Header("Base Stats")]
        public int maxHp = 24000;
        public float hitRadius = 1.5f;
        public float moveSpeed = 1.5f;

        [Tooltip("살아 있는 파티 인원(1,2,3...)별 체력 배율. 03 문서: 혼자 24,000 / 2인 40,800(×1.7). 3인 AI 분대는 실측으로 조정")]
        public float[] partyHpMultipliers = new float[] { 1f, 1.7f, 4.0f };

        public int GetScaledMaxHp(int partySize)
        {
            if (partyHpMultipliers == null || partyHpMultipliers.Length == 0) return maxHp;
            int i = Mathf.Clamp(partySize - 1, 0, partyHpMultipliers.Length - 1);
            return Mathf.RoundToInt(maxHp * partyHpMultipliers[i]);
        }

        [Header("Phases")]
        public BossPhaseData[] phases;

        [Header("Visual")]
        public Color baseColor = new Color(0.5f, 0.3f, 0.2f);
        public float scale = 2.5f;

        [Header("Entry")]
        public float entryDuration = 3f;
        public Vector2 entryStartPosition = new Vector2(12f, 20f);
        public Vector2 battlePosition = new Vector2(12f, 14f);
    }

    [Serializable]
    public class BossPhaseData
    {
        public BossPhase phase;
        public float hpThresholdPercent = 1f;

        [Header("Primary Attack - Fan Shot")]
        public bool usesFanShot = true;
        public int fanShotCount = 5;
        public float fanShotAngle = 60f;
        public int fanShotDamage = 200;
        public float fanShotInterval = 3f;
        public float fanShotWarning = 0.8f;
        public float fanShotBulletSpeed = 12f;
        public float fanShotBulletRadius = 0.2f;
        [Tooltip("한 번의 예고 뒤 연속 발사 횟수 (3페이즈 난사 3연속)")]
        public int fanShotBurstCount = 1;
        [Tooltip("연속 발사 간격 (초)")]
        public float fanShotBurstInterval = 0.35f;
        [Tooltip("이 페이즈 탄의 엄폐물 피해 배율 (3페이즈 ×2)")]
        public float coverDamageMul = 1f;
        [Tooltip("이 페이즈에서 내려올 전투 위치 y (타일). 음수면 그대로")]
        public float battleY = -1f;

        [Header("Secondary Attack - Tail Sweep")]
        public bool usesTailSweep = true;
        public int tailSweepDamage = 400;
        public float tailSweepInterval = 8f;
        public float tailSweepWarning = 1.2f;
        public float tailSweepWidth = 6f;
        [Tooltip("휩쓸기 판정 영역의 Y 범위 (타일, 평면 좌표). 기본: 우리 진영 전체")]
        public float tailSweepYMin = 0f;
        public float tailSweepYMax = 8.5f;

        [Header("Special Attack - Summon")]
        public bool summonsMooks = false;
        public EnemyData mookToSummon;
        public int mookSummonCount = 2;
        public float summonInterval = 15f;

        [Header("Enrage")]
        public float enrageSpeedMultiplier = 1.2f;
        public float enrageAttackSpeedMultiplier = 1.3f;

        [Header("Movement")]
        public bool movesHorizontally = true;
        public float moveRange = 8f;
        public float moveInterval = 4f;
    }
}

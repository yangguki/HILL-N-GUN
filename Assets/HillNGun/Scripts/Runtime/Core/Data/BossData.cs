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

using UnityEngine;
using System;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "stage_", menuName = "HillNGun/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string nameKey;
        public int stageNumber = 1;
        public int subStageNumber = 1;
        [Tooltip("10 §1 chapter id, e.g. dust_town")]
        public string chapterId = "dust_town";
        [Tooltip("1..10 within chapter")]
        public int stageIndex = 1;
        public StageType stageType = StageType.Exterminate;
        [Tooltip("Optional Task I visual (parallax + timeOfDay). Null = procedural theme only.")]
        public StageVisualData visual;

        [Header("Stage Settings")]
        public int gaugeMax = 20;
        public int waveCount = 4;
        public float stageTimeMin = 60f;
        public float stageTimeMax = 90f;

        [Header("Stars (06 §6) — no stat growth")]
        [Tooltip("★2: clear under this many seconds")]
        public float starTimeUnderSec = 80f;
        [Tooltip("★3: no knockdowns (and optional barrel uses for 1-3)")]
        public bool starRequireNoDowned = true;
        public int starMinBarrelExplosions;

        [Header("Enemy Spawning")]
        public WaveDefinition[] waves;
        public float timeBetweenWaves = 10f;
        public float timeBetweenSpawns = 1f;

        [Header("Map")]
        public BattlefieldData battlefieldOverride;
        [Tooltip("StageThemeData.id (background, ambient, cover props look). View only.")]
        public string themeId = "town_street";
        public Vector2[] enemySpawnPoints;
        public float[] enemyRowsY;
        [Tooltip("Optional cover layout for this stage; null = BattlefieldData.covers")]
        public CoverPlacement[] coverPlacements;
        [Tooltip("Hazards (explosive barrels, etc.)")]
        public HazardPlacement[] hazards;

        [Header("Boss Stage")]
        public bool isBossStage = false;
        public BossData bossData;
    }

    [Serializable]
    public class WaveDefinition
    {
        public int waveNumber;
        public EnemySpawnEntry[] enemies;
        public float delayBeforeWave = 2f;
        [Tooltip("intro / pressure / peak / rest / finale")]
        public string paceTag = "pressure";
        [Tooltip("If true, wait until living enemies are gone before next wave")]
        public bool waitForClear = true;
        [Tooltip("After last spawn (or after clear), delay before next wave")]
        public float nextWaveDelay = 2f;
    }

    [Serializable]
    public class EnemySpawnEntry
    {
        public EnemyData enemyData;
        public int count = 1;
        public int preferredRow = 0;
        public float spawnDelay = 0f;
    }

    public enum HazardType { ExplosiveBarrel, SaloonSign, Tumbleweed, TrainPass }

    [Serializable]
    public class HazardPlacement
    {
        public HazardType type = HazardType.ExplosiveBarrel;
        public float x = 12f;
        public int rowIndex = 1;
        [Tooltip("For train_pass: duration seconds")]
        public float param = 3f;
    }
}

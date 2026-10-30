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

        [Header("Stage Settings")]
        public int gaugeMax = 20;
        public int waveCount = 4;
        public float stageTimeMin = 60f;
        public float stageTimeMax = 90f;

        [Header("Enemy Spawning")]
        public WaveDefinition[] waves;
        public float timeBetweenWaves = 10f;
        public float timeBetweenSpawns = 1f;

        [Header("Map")]
        public BattlefieldData battlefieldOverride;
        public Vector2[] enemySpawnPoints;
        public float[] enemyRowsY;

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
    }

    [Serializable]
    public class EnemySpawnEntry
    {
        public EnemyData enemyData;
        public int count = 1;
        public int preferredRow = 0;
        public float spawnDelay = 0f;
    }
}

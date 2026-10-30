using UnityEngine;
using System.Collections.Generic;
using HillNGun.Data;
using HillNGun.Core;
using HillNGun.Characters;

namespace HillNGun.PvE
{
    public class PvEMatchManager : MonoBehaviour
    {
        [Header("Stage Data")]
        [SerializeField] private StageData[] _stages;
        [SerializeField] private int _currentStageIndex = 0;

        [Header("Settings")]
        [SerializeField] private float _enemySpawnOffscreenY = 20f;
        [SerializeField] private float _maxEnemyY = 25f;

        private StageData _currentStage;
        private List<EnemyInstance> _activeEnemies = new List<EnemyInstance>();
        private BossInstance _activeBoss;

        private int _currentGauge;
        private int _currentWave;
        private float _stageTimer;
        private bool _isMatchActive;
        private bool _isBossStage;

        private float _waveTimer;
        private int _nextSpawnIndex;
        private WaveDefinition _currentWaveData;

        private PvERoundResult _roundResult = PvERoundResult.InProgress;

        public int CurrentGauge => _currentGauge;
        public int GaugeMax => _currentStage?.gaugeMax ?? 20;
        public int CurrentWave => _currentWave;
        public int TotalWaves => _currentStage?.waveCount ?? 4;
        public float StageTimer => _stageTimer;
        public bool IsMatchActive => _isMatchActive;
        public PvERoundResult RoundResult => _roundResult;
        public BossInstance ActiveBoss => _activeBoss;
        public IReadOnlyList<EnemyInstance> ActiveEnemies => _activeEnemies;

        public event System.Action<int> OnGaugeChanged;
        public event System.Action<int> OnWaveChanged;
        public event System.Action<PvERoundResult> OnStageComplete;
        public event System.Action<EnemyInstance> OnEnemySpawned;
        public event System.Action<EnemyInstance> OnEnemyDefeated;
        public event System.Action<BossInstance> OnBossSpawned;
        public event System.Action<BossInstance> OnBossDefeated;

        private void Start()
        {
            if (_stages != null && _stages.Length > 0)
            {
                StartStage(_currentStageIndex);
            }
        }

        public void StartStage(int stageIndex)
        {
            if (_stages == null || stageIndex < 0 || stageIndex >= _stages.Length)
            {
                Debug.LogError("Invalid stage index");
                return;
            }

            _currentStageIndex = stageIndex;
            _currentStage = _stages[stageIndex];
            _isBossStage = _currentStage.isBossStage;

            _currentGauge = 0;
            _currentWave = 0;
            _stageTimer = 0f;
            _roundResult = PvERoundResult.InProgress;
            _isMatchActive = true;

            _activeEnemies.Clear();
            _activeBoss = null;

            if (_isBossStage && _currentStage.bossData != null)
            {
                SpawnBoss();
            }
            else
            {
                StartWave(0);
            }

            var flowManager = GameFlowManager.Instance;
            if (flowManager != null)
            {
                flowManager.SetPvEProgress(_currentGauge, GaugeMax, _currentWave + 1, TotalWaves);
            }
        }

        private void StartWave(int waveIndex)
        {
            if (_currentStage?.waves == null || waveIndex >= _currentStage.waves.Length) return;

            _currentWave = waveIndex;
            _currentWaveData = _currentStage.waves[waveIndex];
            _waveTimer = _currentWaveData.delayBeforeWave;
            _nextSpawnIndex = 0;

            OnWaveChanged?.Invoke(_currentWave + 1);
        }

        private void SpawnBoss()
        {
            if (_currentStage?.bossData == null) return;

            _activeBoss = new BossInstance();
            _activeBoss.Initialize(_currentStage.bossData, _currentStage.bossData.battlePosition);

            _activeBoss.OnDefeated += HandleBossDefeated;
            _activeBoss.OnDamageTaken += HandleBossDamaged;
            _activeBoss.OnFanShot += HandleBossFanShot;
            _activeBoss.OnTailSweep += HandleBossTailSweep;
            _activeBoss.OnSummonMooks += HandleBossSummon;

            OnBossSpawned?.Invoke(_activeBoss);
        }

        private void Update()
        {
            if (!_isMatchActive) return;

            _stageTimer += Time.deltaTime;

            if (_isBossStage)
            {
                UpdateBossFight();
            }
            else
            {
                UpdateWaveMode();
            }

            UpdateEnemies();
            CheckTeamDefeat();
        }

        private void UpdateWaveMode()
        {
            if (_currentWaveData == null) return;

            _waveTimer -= Time.deltaTime;

            if (_waveTimer <= 0f && _nextSpawnIndex < _currentWaveData.enemies.Length)
            {
                SpawnEnemy(_currentWaveData.enemies[_nextSpawnIndex]);
                _nextSpawnIndex++;

                if (_nextSpawnIndex < _currentWaveData.enemies.Length)
                {
                    _waveTimer = _currentStage.timeBetweenSpawns;
                }
            }

            if (_currentGauge >= GaugeMax)
            {
                CompleteStage(PvERoundResult.StageCleared);
            }
        }

        private void UpdateBossFight()
        {
            if (_activeBoss != null)
            {
                _activeBoss.Update(Time.deltaTime);
            }
        }

        private void UpdateEnemies()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];

                if (enemy.IsFleeing)
                {
                    enemy.UpdateFlee(Time.deltaTime);
                    if (enemy.IsOffScreen(_maxEnemyY))
                    {
                        _activeEnemies.RemoveAt(i);
                    }
                }
                else if (enemy.IsAlive)
                {
                    Vector2 playerPos = GetNearestPlayerPosition(enemy.Position);
                    enemy.Update(Time.deltaTime, playerPos);

                    if (_currentStage?.enemyRowsY != null)
                    {
                        enemy.MoveTowardsTargetRow(Time.deltaTime, _currentStage.enemyRowsY);
                    }
                }
            }
        }

        private Vector2 GetNearestPlayerPosition(Vector2 fromPosition)
        {
            return new Vector2(12f, 4.5f);
        }

        private void SpawnEnemy(EnemySpawnEntry entry)
        {
            if (entry?.enemyData == null) return;

            for (int i = 0; i < entry.count; i++)
            {
                float spawnX = UnityEngine.Random.Range(4f, 20f);
                float spawnY = _enemySpawnOffscreenY;
                int row = entry.preferredRow;

                var enemy = new EnemyInstance();
                enemy.Initialize(entry.enemyData, new Vector2(spawnX, spawnY), row);

                enemy.OnDefeated += HandleEnemyDefeated;
                enemy.OnDamageTaken += HandleEnemyDamaged;
                enemy.OnAttack += HandleEnemyAttack;
                enemy.OnExplosiveThrown += HandleEnemyExplosive;

                _activeEnemies.Add(enemy);
                OnEnemySpawned?.Invoke(enemy);
            }
        }

        private void HandleEnemyDefeated(EnemyInstance enemy)
        {
            _currentGauge += enemy.Data.gaugeValue;
            OnGaugeChanged?.Invoke(_currentGauge);
            OnEnemyDefeated?.Invoke(enemy);

            var flowManager = GameFlowManager.Instance;
            flowManager?.IncrementEnemiesDefeated();
            flowManager?.SetPvEProgress(_currentGauge, GaugeMax, _currentWave + 1, TotalWaves);

            if (!_isBossStage && _currentGauge >= GaugeMax)
            {
                CompleteStage(PvERoundResult.StageCleared);
            }
            else if (!_isBossStage)
            {
                CheckWaveProgress();
            }
        }

        private void CheckWaveProgress()
        {
            if (_currentWaveData == null) return;

            bool waveComplete = _nextSpawnIndex >= _currentWaveData.enemies.Length;
            if (!waveComplete) return;

            bool allEnemiesGone = true;
            foreach (var enemy in _activeEnemies)
            {
                if (enemy.IsAlive && !enemy.IsFleeing)
                {
                    allEnemiesGone = false;
                    break;
                }
            }

            if (allEnemiesGone && _currentWave < _currentStage.waveCount - 1)
            {
                StartWave(_currentWave + 1);
            }
        }

        private void HandleEnemyDamaged(EnemyInstance enemy, int damage, Vector2 direction)
        {
        }

        private void HandleEnemyAttack(EnemyInstance enemy, Vector2 direction, int damage)
        {
        }

        private void HandleEnemyExplosive(EnemyInstance enemy, Vector2 direction, int damage)
        {
        }

        private void HandleBossDefeated(BossInstance boss)
        {
            OnBossDefeated?.Invoke(boss);
            CompleteStage(PvERoundResult.BossDefeated);
        }

        private void HandleBossDamaged(BossInstance boss, int damage, Vector2 direction)
        {
        }

        private void HandleBossFanShot(BossInstance boss, Vector2[] directions, int damage)
        {
        }

        private void HandleBossTailSweep(BossInstance boss, float width)
        {
        }

        private void HandleBossSummon(BossInstance boss)
        {
            if (_currentStage?.bossData?.phases == null) return;

            foreach (var phase in _currentStage.bossData.phases)
            {
                if (phase.phase == boss.CurrentPhase && phase.summonsMooks && phase.mookToSummon != null)
                {
                    var entry = new EnemySpawnEntry
                    {
                        enemyData = phase.mookToSummon,
                        count = phase.mookSummonCount,
                        preferredRow = UnityEngine.Random.Range(0, 3)
                    };
                    SpawnEnemy(entry);
                    break;
                }
            }
        }

        private void CheckTeamDefeat()
        {
        }

        private void CompleteStage(PvERoundResult result)
        {
            _roundResult = result;
            _isMatchActive = false;

            OnStageComplete?.Invoke(result);

            var flowManager = GameFlowManager.Instance;
            if (flowManager != null)
            {
                MatchResult matchResult = result == PvERoundResult.StageFailed 
                    ? MatchResult.Defeat 
                    : MatchResult.Victory;

                if (result == PvERoundResult.StageCleared && _currentStageIndex < _stages.Length - 1)
                {
                    StartCoroutine(AdvanceToNextStage());
                }
                else
                {
                    flowManager.EndMatch(matchResult);
                }
            }
        }

        private System.Collections.IEnumerator AdvanceToNextStage()
        {
            yield return new WaitForSeconds(2f);
            StartStage(_currentStageIndex + 1);
        }

        public void DamageEnemy(EnemyInstance enemy, int damage, Vector2 direction)
        {
            enemy?.TakeDamage(damage, direction);
        }

        public void DamageBoss(int damage, Vector2 direction)
        {
            _activeBoss?.TakeDamage(damage, direction);
        }

        public void TeamDefeated()
        {
            CompleteStage(PvERoundResult.StageFailed);
        }
    }
}

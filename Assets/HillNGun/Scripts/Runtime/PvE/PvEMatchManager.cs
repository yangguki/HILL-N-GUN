using UnityEngine;
using System.Collections.Generic;
using HillNGun.Data;
using HillNGun.Core;
using HillNGun.Characters;
using HillNGun.AI;
using HillNGun.Input;
using HillNGun.Combat;
using HillNGun.Cover;
using HillNGun.Feel;

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

        [Header("Player Team")]
        [SerializeField] private CharacterData[] _characterDatas;
        [SerializeField] private BotBehaviorData _botBehavior;
        [SerializeField] private Vector2[] _playerSpawnPoints = new Vector2[]
        {
            new Vector2(6f, 2f),
            new Vector2(12f, 4.5f),
            new Vector2(18f, 2f)
        };

        private StageData _currentStage;
        private List<EnemyInstance> _activeEnemies = new List<EnemyInstance>();
        private BossInstance _activeBoss;
        private List<CharacterStateMachine> _playerTeam = new List<CharacterStateMachine>();
        private List<BotBrain> _allyBots = new List<BotBrain>();
        private int _playerControlledIndex = 0;
        private List<AutoGunner> _allyGunners = new List<AutoGunner>();
        private CombatSystem _combat;

        public int TotalEnemyDamage { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public int PlayerDamageTaken { get; private set; }
        public int PlayerControlledIndex => _playerControlledIndex;

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

        public IReadOnlyList<CharacterStateMachine> PlayerTeam => _playerTeam;

        private void Start()
        {
            LoadDataFromAssetManager();
            _combat = ServiceLocator.Get<CombatSystem>();
            SpawnCovers();
            ProjectileView.Ensure();
            SpawnPlayerTeam();
            if (_stages != null && _stages.Length > 0)
            {
                StartStage(_currentStageIndex);
            }
            else
            {
                Debug.LogWarning("[PvEMatchManager] No stages configured - nothing to spawn");
            }
        }

        private void LoadDataFromAssetManager()
        {
            var assetManager = RuntimeAssetManager.Instance;
            if (assetManager == null)
            {
                Debug.LogWarning("[PvEMatchManager] RuntimeAssetManager not found");
                return;
            }

            if (_stages == null || _stages.Length == 0)
            {
                _stages = assetManager.StageDatas;
            }
            if (_characterDatas == null || _characterDatas.Length == 0)
            {
                _characterDatas = assetManager.CharacterDatas;
            }
            if (_botBehavior == null)
            {
                _botBehavior = assetManager.BotBehavior;
            }
        }

        private void SpawnCovers()
        {
            var am = RuntimeAssetManager.Instance;
            var field = am?.BattlefieldData ?? ServiceLocator.Get<BattlefieldData>();
            if (field == null || !field.spawnCoversInPvE) return;
            CoverFieldSpawner.Spawn(field, am?.CoverDatas, false);
        }

        private void SpawnPlayerTeam()
        {
            _playerTeam.Clear();
            _allyBots.Clear();

            var flowManager = GameFlowManager.Instance;
            string[] charIds = flowManager?.CurrentSession?.selectedCharacterIds ??
                new string[] { "buster", "raven", "volt" };

            for (int i = 0; i < 3; i++)
            {
                string charId = charIds.Length > i ? charIds[i] : "buster";
                CharacterData charData = GetCharacterById(charId);
                if (charData == null && _characterDatas != null && _characterDatas.Length > i)
                {
                    charData = _characterDatas[i];
                }

                if (charData != null)
                {
                    var character = CreatePlayerCharacter(charData, i, _playerSpawnPoints[i], i);
                    _playerTeam.Add(character);

                    if (i != _playerControlledIndex)
                    {
                        var bot = new BotBrain(character, _botBehavior);
                        _allyBots.Add(bot);
                        _allyGunners.Add(new AutoGunner(character, 0.5f + 0.3f * i));
                    }
                }
            }

            CharacterStateMachine[] teamArray = _playerTeam.ToArray();
            foreach (var bot in _allyBots)
            {
                bot.SetAllies(teamArray);
            }

            var inputHandler = FindFirstObjectByType<TouchInputHandler>();
            if (inputHandler != null && _playerTeam.Count > _playerControlledIndex)
            {
                inputHandler.SetReceiver(_playerTeam[_playerControlledIndex]);
            }
        }

        private CharacterStateMachine CreatePlayerCharacter(CharacterData data, int index, Vector2 position, int rowIndex)
        {
            GameObject go = new GameObject($"Player_{data.id}_{index}");
            var character = go.AddComponent<CharacterStateMachine>();

            var viewGo = new GameObject("View");
            viewGo.transform.SetParent(go.transform);
            var view = viewGo.AddComponent<PlaceholderCharacterView>();

            character.Initialize(data, Team.Player, index, position, rowIndex);
            view.Initialize(character);
            character.SetView(view);

            character.OnDowned += HandlePlayerDowned;
            character.OnEliminated += HandlePlayerEliminated;
            character.OnFired += HandlePlayerFired;
            character.OnDamageTaken += HandlePlayerDamaged;

            _combat?.RegisterCharacter(character);

            return character;
        }

        private CharacterData GetCharacterById(string id)
        {
            if (_characterDatas == null) return null;
            foreach (var data in _characterDatas)
            {
                if (data != null && data.id == id)
                    return data;
            }
            return null;
        }

        private void HandlePlayerFired(CharacterStateMachine character, Vector2 direction, int damage)
        {
            EffectsManager.Instance?.SpawnMuzzleFlash(character.Position, direction,
                (character.Data?.weapon?.projectilesPerShot ?? 1) > 1);
        }

        private void HandlePlayerDamaged(CharacterStateMachine character, int damage, Vector2 direction)
        {
            PlayerDamageTaken += damage;
            EffectsManager.Instance?.SpawnDamageNumber(character.Position, damage, damage >= 400);
        }

        private void HandlePlayerDowned(CharacterStateMachine character)
        {
            Debug.Log($"Player {character.Data?.id} downed");
        }

        private void HandlePlayerEliminated(CharacterStateMachine character)
        {
            Debug.Log($"Player {character.Data?.id} eliminated");
            CheckTeamDefeat();
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

            _combat?.ClearCombatTargets();
            _activeEnemies.Clear();
            _activeBoss = null;

            ResetPlayerTeamForStage();

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

        private void ResetPlayerTeamForStage()
        {
            for (int i = 0; i < _playerTeam.Count; i++)
            {
                var character = _playerTeam[i];
                if (character != null)
                {
                    character.ResetForRound(_playerSpawnPoints[i], i);
                }
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
            _combat?.RegisterCombatTarget(_activeBoss);

            _activeBoss.OnDefeated += HandleBossDefeated;
            _activeBoss.OnDamageTaken += HandleBossDamaged;
            _activeBoss.OnFanShot += HandleBossFanShot;
            _activeBoss.OnTailSweep += HandleBossTailSweep;
            _activeBoss.OnSummonMooks += HandleBossSummon;

            CreateBossView(_activeBoss);
            OnBossSpawned?.Invoke(_activeBoss);
        }

        private void CreateBossView(BossInstance boss)
        {
            var go = new GameObject($"Boss_{boss.Data?.id}");
            var view = go.AddComponent<BossView>();
            view.Initialize(boss);
        }

        private void Update()
        {
            if (!_isMatchActive) return;

            _stageTimer += Time.deltaTime;

            UpdateAllyBots();

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

        private void UpdateAllyBots()
        {
            // PvE allies: BotBrain targets characters only, so PvE uses AutoGunner against enemies/boss.
            float dt = Time.deltaTime;
            foreach (var gunner in _allyGunners)
            {
                var c = gunner.Character;
                if (c == null) continue;
                gunner.Tick(dt, FindTargetFor(c.Position, (c.Data?.weapon?.range ?? 7f) * 1.5f));
            }
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

        /// <summary>Nearest hittable enemy/boss position within range, or null.</summary>
        public Vector2? FindTargetFor(Vector2 from, float maxRange)
        {
            Vector2? best = null;
            float bestDist = maxRange;
            foreach (var e in _activeEnemies)
            {
                if (e == null || !e.CanBeHit || e.IsEntering) continue;
                float d = Vector2.Distance(from, e.Position);
                if (d <= bestDist) { bestDist = d; best = e.Position; }
            }
            if (_activeBoss != null && _activeBoss.CanBeHit)
            {
                float d = Vector2.Distance(from, _activeBoss.Position);
                if (d <= bestDist) { best = _activeBoss.Position; }
            }
            return best;
        }

        private Vector2 GetNearestPlayerPosition(Vector2 fromPosition)
        {
            if (_playerTeam == null || _playerTeam.Count == 0)
                return new Vector2(12f, 4.5f);

            Vector2 nearest = _playerTeam[0]?.Position ?? new Vector2(12f, 4.5f);
            float nearestDist = float.MaxValue;

            foreach (var character in _playerTeam)
            {
                if (character == null || character.OverallState != CharacterOverallState.Active)
                    continue;

                float dist = Vector2.Distance(fromPosition, character.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = character.Position;
                }
            }

            return nearest;
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
                var fieldData = ServiceLocator.Get<Battlefield.BattlefieldSystem>()?.Data;
                if (fieldData != null) enemy.SetHittableMaxY(fieldData.size.y);
                _combat?.RegisterCombatTarget(enemy);

                enemy.OnDefeated += HandleEnemyDefeated;
                enemy.OnDamageTaken += HandleEnemyDamaged;
                enemy.OnAttack += HandleEnemyAttack;
                enemy.OnExplosiveThrown += HandleEnemyExplosive;

                _activeEnemies.Add(enemy);
                CreateEnemyView(enemy);
                OnEnemySpawned?.Invoke(enemy);
            }
        }

        private void CreateEnemyView(EnemyInstance enemy)
        {
            var go = new GameObject($"Enemy_{enemy.Data?.id}");
            var view = go.AddComponent<EnemyView>();
            view.Initialize(enemy);
        }

        private void HandleEnemyDefeated(EnemyInstance enemy)
        {
            EnemiesDefeated++;
            _combat?.UnregisterCombatTarget(enemy);
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
            TotalEnemyDamage += damage;
            EffectsManager.Instance?.SpawnDamageNumber(enemy.Position, damage, damage >= 500);
            EffectsManager.Instance?.SpawnImpact(enemy.Position, false);
        }

        private void HandleEnemyAttack(EnemyInstance enemy, Vector2 direction, int damage)
        {
            if (_combat == null || enemy?.Data == null) return;
            var d = enemy.Data;
            int count = Mathf.Max(1, d.projectilesPerShot);
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? Mathf.Lerp(-d.spreadDeg * 0.5f, d.spreadDeg * 0.5f, i / (float)(count - 1)) : 0f;
                Vector2 dir = Battlefield.CoordinateHelper.RotateDirection(direction, angle);
                _combat.SpawnEnemyProjectile(enemy.Position + dir * 0.6f, dir, d.bulletSpeed, d.bulletRadius, damage, d.attackRange, Team.Enemy);
            }
            EffectsManager.Instance?.SpawnMuzzleFlash(enemy.Position, direction, false);
        }

        private void HandleEnemyExplosive(EnemyInstance enemy, Vector2 direction, int damage)
        {
            if (_combat == null || enemy?.Data == null) return;
            var d = enemy.Data;
            float speed = d.attackRange / Mathf.Max(0.1f, d.explosiveFlightTime);
            _combat.SpawnEnemyProjectile(enemy.Position + direction * 0.6f, direction, speed, d.explosiveRadius * 0.25f, damage, d.attackRange, Team.Enemy);
        }

        private void HandleBossDefeated(BossInstance boss)
        {
            OnBossDefeated?.Invoke(boss);
            CompleteStage(PvERoundResult.BossDefeated);
        }

        private void HandleBossDamaged(BossInstance boss, int damage, Vector2 direction)
        {
            TotalEnemyDamage += damage;
            EffectsManager.Instance?.SpawnDamageNumber(boss.Position, damage, damage >= 500);
        }

        private void HandleBossFanShot(BossInstance boss, Vector2[] directions, int damage)
        {
            if (_combat == null || directions == null) return;
            float speed = 12f;
            if (boss.Data?.phases != null)
                foreach (var ph in boss.Data.phases)
                    if (ph.phase == boss.CurrentPhase) { speed = ph.fanShotBulletSpeed; break; }
            foreach (var dir in directions)
                _combat.SpawnEnemyProjectile(boss.Position + dir * 1.2f, dir, speed, 0.25f, damage, 20f, Team.Enemy);
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
            if (_playerTeam == null || _playerTeam.Count == 0) return;

            bool allEliminated = true;
            foreach (var character in _playerTeam)
            {
                if (character != null && character.OverallState != CharacterOverallState.Eliminated)
                {
                    allEliminated = false;
                    break;
                }
            }

            if (allEliminated)
            {
                CompleteStage(PvERoundResult.StageFailed);
            }
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

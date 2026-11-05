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
        private List<AbilityPilot> _allyPilots = new List<AbilityPilot>();

        public event System.Action<CharacterStateMachine, SkillEffect> OnSkillUsed;
        public event System.Action<CharacterStateMachine, CharacterStateMachine> OnAllyRevived;
        public event System.Action<BossInstance, int> OnBossVolley;
        public int SkillsUsed { get; private set; }
        public int GrenadesThrown { get; private set; }
        public int RevivesCompleted { get; private set; }
        public int BossVolleys { get; private set; }
        public int BossPhase3Volleys { get; private set; }

        /// <summary>Squad size used for PvE HP scaling (decision D15).</summary>
        public int PartySize
        {
            get
            {
                int n = 0;
                foreach (var c in _playerTeam)
                    if (c != null && c.OverallState != CharacterOverallState.Eliminated) n++;
                return Mathf.Max(1, n);
            }
        }
        private readonly List<GameObject> _stageViews = new List<GameObject>();
        private CombatSystem _combat;

        public int TotalEnemyDamage { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public int PlayerDamageTaken { get; private set; }
        public int PlayerEliminations { get; private set; }
        public float MatchElapsed { get; private set; }
        public int PlayerControlledIndex => _playerControlledIndex;

        private int _currentGauge;
        private int _currentWave;
        private float _stageTimer;
        private bool _isMatchActive;
        private bool _isBossStage;

        private float _waveTimer;
        private int _nextSpawnIndex;
        private WaveDefinition _currentWaveData;
        private readonly System.Collections.Generic.List<HillNGun.Combat.HazardExplosiveBarrel> _hazards = new System.Collections.Generic.List<HillNGun.Combat.HazardExplosiveBarrel>();
        private float _postWaveTimer = -1f;
        private float _trainBlockTimer;
        public int BarrelExplosions { get; private set; }
        public int TeamDownedCount { get; private set; }
        public bool TrainBlockingSpawns => _trainBlockTimer > 0f;


        private PvERoundResult _roundResult = PvERoundResult.InProgress;

        public int CurrentStageIndex => _currentStageIndex;
        public int StageCount => _stages?.Length ?? 0;
        public string CurrentStageId => _currentStage?.id;
        public StageData CurrentStage => _currentStage;
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
            // Covers spawn in StartStage with stage-specific placements (avoid double-registering battlefield defaults).
            ProjectileView.Ensure();
            CombatFeedback.Ensure();
            SpawnPlayerTeam();
            if (_combat != null)
            {
                _combat.OnCombatTargetHit += HandleCombatTargetHitForStats;
                _combat.OnCoverDestroyed += HandleCoverDestroyedForStats;
            }
            if (_stages != null && _stages.Length > 0)
            {
                // stage select: start where the session asks, only if that stage is unlocked (offline progression)
                var session = GameFlowManager.Instance?.CurrentSession;
                if (session != null && session.startStageIndex > 0)
                {
                    var prog = HillNGun.Progression.ProgressionService.Instance;
                    _currentStageIndex = prog.IsStageUnlocked(_stages, session.startStageIndex)
                        ? session.startStageIndex : prog.HighestUnlockedStageIndex(_stages);
                }
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
            // Destroy previous cover views + unregister so stage layouts do not stack.
            var existing = FindObjectsByType<CoverView>(FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null) Destroy(existing[i].gameObject);
            }
            _combat?.ClearCovers();

            var am = RuntimeAssetManager.Instance;
            var field = am?.BattlefieldData ?? ServiceLocator.Get<BattlefieldData>();
            if (field == null || !field.spawnCoversInPvE) return;
            var overrides = _currentStage != null ? _currentStage.coverPlacements : null;
            CoverFieldSpawner.Spawn(field, am?.CoverDatas, false, overrides);
        }

        private void SpawnHazards()
        {
            ClearHazards();
            BarrelExplosions = 0;
            if (_currentStage?.hazards == null || _combat == null) return;
            var field = Field;
            foreach (var h in _currentStage.hazards)
            {
                if (h == null) continue;
                if (h.type == HazardType.ExplosiveBarrel)
                {
                    float y = field != null ? field.GetOurRowY(h.rowIndex) : 4.5f;
                    var b = new HazardExplosiveBarrel();
                    b.Initialize(new Vector2(h.x, y), _combat);
                    b.OnExploded += _ => { BarrelExplosions++; };
                    _hazards.Add(b);
                }
                else if (h.type == HazardType.TrainPass)
                {
                    // armed by rest wave paceTag; duration from param
                }
            }
        }

        private void ClearHazards()
        {
            foreach (var h in _hazards)
                if (h != null && !h.Exploded) _combat?.UnregisterCombatTarget(h);
            _hazards.Clear();
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
                    Vector2 spawn = GetSpawnPoint(i, charData);
                    var character = CreatePlayerCharacter(charData, i, spawn, GetRowFor(spawn.y));
                    _playerTeam.Add(character);

                    if (i != _playerControlledIndex)
                    {
                        var bot = new BotBrain(character, _botBehavior);
                        _allyBots.Add(bot);
                        float shotMul = _botBehavior != null ? _botBehavior.pveAllyShotCooldownMul : 1f;
                        _allyGunners.Add(new AutoGunner(character, 0.5f + 0.3f * i, shotMul));
                        _allyPilots.Add(new AbilityPilot(character, _botBehavior, 2f + 1.5f * i, _botBehavior != null ? _botBehavior.pveAllyGrenadeMinInterval : 0f));
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
            // On-screen joysticks/buttons (touch devices only; hidden on desktop)
            if (_playerTeam.Count > _playerControlledIndex)
                ServiceLocator.Get<MobileTouchControls>()?.SetReceiver(_playerTeam[_playerControlledIndex]);
            // Field drops (t48u): covers / defeated enemies toss pickups to the player's rows
            HillNGun.Combat.DropDirector.Begin(GameMode.PvE, () => _playerTeam,
                () => _playerTeam.Count > _playerControlledIndex ? _playerTeam[_playerControlledIndex] : null);
        }

        private BattlefieldData Field => ServiceLocator.Get<Battlefield.BattlefieldSystem>()?.Data;

        /// <summary>Spawn point. Assault front-row force is off by default (v2 D1 discarded); toggle BattlefieldData.assaultStartsFrontRow.</summary>
        private Vector2 GetSpawnPoint(int index, CharacterData data)
        {
            Vector2 p = _playerSpawnPoints[Mathf.Clamp(index, 0, _playerSpawnPoints.Length - 1)];
            var field = Field;
            if (field != null && field.assaultStartsFrontRow && data != null && data.role == Role.Assault)
            {
                p.y = field.GetOurRowY(field.OurRowCount - 1);
            }
            return p;
        }

        private int GetRowFor(float y)
        {
            var bf = ServiceLocator.Get<Battlefield.BattlefieldSystem>();
            return bf != null ? bf.GetNearestRowIndex(y, Team.Player) : 0;
        }

        private CharacterStateMachine CreatePlayerCharacter(CharacterData data, int index, Vector2 position, int rowIndex)
        {
            GameObject go = new GameObject($"Player_{data.id}_{index}");
            var character = go.AddComponent<CharacterStateMachine>();

            var viewGo = new GameObject("View");
            viewGo.transform.SetParent(go.transform);
            var view = CharacterViewFactory.Create(viewGo);

            data = HillNGun.Progression.Loadout.Resolve(data); // equipped gun (t48u); starting gun = same data
            character.Initialize(data, Team.Player, index, position, rowIndex);
            view.Initialize(character);
            character.SetView(view);

            character.OnDowned += HandlePlayerDowned;
            character.OnEliminated += HandlePlayerEliminated;
            character.OnFired += HandlePlayerFired;
            character.OnDamageTaken += HandlePlayerDamaged;

            _combat?.RegisterCharacter(character);
            ServiceLocator.Get<ReviveSystem>()?.RegisterCharacter(character);
            character.OnSkillActivated += HandleSkillActivated;
            character.OnRevived += HandlePlayerRevived;

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
                (character.Data?.weapon?.projectilesPerShot ?? 1) > 1, character.Data?.weapon?.muzzleForwardOffset ?? 1f);
        }

        private void HandlePlayerDamaged(CharacterStateMachine character, int damage, Vector2 direction)
        {
            PlayerDamageTaken += damage;
            EffectsManager.Instance?.SpawnDamageNumber(character.Position, damage, damage >= 400);
        }

        public int PlayerKills { get; private set; }
        public int CoversDestroyed { get; private set; }

        private void HandleCombatTargetHitForStats(Projectile p, HillNGun.Combat.ICombatTarget ct, int dmg)
        {
            if (p == null || p.Owner == null || _playerTeam.Count <= _playerControlledIndex) return;
            if (p.Owner != _playerTeam[_playerControlledIndex]) return;
            bool dead = ct is EnemyInstance e ? !e.IsAlive : ct is BossInstance b && !b.IsAlive;
            if (dead) PlayerKills++;
        }

        private void HandleCoverDestroyedForStats(HillNGun.Cover.CoverInstance c)
        {
            if (_isMatchActive) CoversDestroyed++;
        }

        private void OnDestroy()
        {
            if (_combat != null)
            {
                _combat.OnCombatTargetHit -= HandleCombatTargetHitForStats;
                _combat.OnCoverDestroyed -= HandleCoverDestroyedForStats;
            }
        }

        private void HandleSkillActivated(CharacterStateMachine c, SkillEffect effect, Vector2? target)
        {
            SkillsUsed++;
            Debug.Log($"[PvE] Skill {c.Data?.skill?.id} ({effect}) by {c.Data?.id} at {_stageTimer:F1}s");
            OnSkillUsed?.Invoke(c, effect);
        }

        private void HandlePlayerRevived(CharacterStateMachine c)
        {
            RevivesCompleted++;
            Debug.Log($"[PvE] {c.Data?.id} revived at {_stageTimer:F1}s");
            OnAllyRevived?.Invoke(null, c);
        }

        private void HandlePlayerDowned(CharacterStateMachine character)
        {
            Debug.Log($"Player {character.Data?.id} downed");
        }

        private void HandlePlayerEliminated(CharacterStateMachine character)
        {
            Debug.Log($"Player {character.Data?.id} eliminated");
            PlayerEliminations++;
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

            CleanupStageActors(true);
            HillNGun.Combat.DropDirector.Instance?.ClearField();
            ClearHazards();
            TeamDownedCount = 0;
            BarrelExplosions = 0;
            _postWaveTimer = -1f;
            _trainBlockTimer = 0f;

            ResetPlayerTeamForStage();
            SpawnCovers();
            SpawnHazards();

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
                    Vector2 spawn = GetSpawnPoint(i, character.Data);
                    character.ResetForRound(spawn, GetRowFor(spawn.y));
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
            _postWaveTimer = -1f;

            if (_currentWaveData.paceTag == "rest")
            {
                // 1-3 train stub: block top spawns for ~3s (visual bar drawn by TrainPassView)
                float dur = 3f;
                if (_currentStage.hazards != null)
                    foreach (var h in _currentStage.hazards)
                        if (h != null && h.type == HazardType.TrainPass) { dur = h.param > 0f ? h.param : 3f; break; }
                _trainBlockTimer = dur;
                TrainPassView.Ensure().Show(dur);
            }

            OnWaveChanged?.Invoke(_currentWave + 1);
        }

        private void SpawnBoss()
        {
            if (_currentStage?.bossData == null) return;

            _activeBoss = new BossInstance();
            int scaledHp = _currentStage.bossData.GetScaledMaxHp(PartySize);
            _activeBoss.Initialize(_currentStage.bossData, _currentStage.bossData.battlePosition, scaledHp);
            Debug.Log($"[PvE] Boss {_currentStage.bossData.id} hp={scaledHp} (party {PartySize})");
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
            _stageViews.Add(go);
        }

        private void Update()
        {
            if (!_isMatchActive) return;

            _stageTimer += Time.deltaTime;
            MatchElapsed += Time.deltaTime;

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
            for (int i = 0; i < _allyGunners.Count; i++)
            {
                var gunner = _allyGunners[i];
                var c = gunner.Character;
                if (c == null || c.OverallState != CharacterOverallState.Active) continue;
                float range = c.Data?.weapon?.range ?? 7f;
                Vector2? inRange = FindTargetFor(c.Position, range * 1.1f);
                var pilot = i < _allyPilots.Count ? _allyPilots[i] : null;
                bool pilotMoving = pilot != null && TickPilot(pilot, dt, inRange);
                if (!pilotMoving) DriveAllyMovement(c, inRange);
                if (pilot == null || !pilot.IsHolding) gunner.Tick(dt, inRange);
            }
        }

        private void UpdateWaveMode()
        {
            if (_currentWaveData == null) return;

            if (_trainBlockTimer > 0f) _trainBlockTimer -= Time.deltaTime;

            _waveTimer -= Time.deltaTime;
            int enemyEntries = _currentWaveData.enemies != null ? _currentWaveData.enemies.Length : 0;

            if (_waveTimer <= 0f && _nextSpawnIndex < enemyEntries && !TrainBlockingSpawns)
            {
                SpawnEnemy(_currentWaveData.enemies[_nextSpawnIndex]);
                _nextSpawnIndex++;

                if (_nextSpawnIndex < enemyEntries)
                    _waveTimer = _currentStage.timeBetweenSpawns;
            }

            // empty / finished spawn list → wave progress (rest waves advance after delay)
            if (_nextSpawnIndex >= enemyEntries)
                CheckWaveProgress();
            AdvanceWaveIfReady();

            if (_currentGauge >= GaugeMax)
            {
                CompleteStage(PvERoundResult.StageCleared);
            }
        }

        private void UpdateBossFight()
        {
            if (_activeBoss != null)
            {
                if (!_activeBoss.IsWarning)
                {
                    _activeBoss.SweepTargetX = GetNearestPlayerPosition(_activeBoss.Position).x;
                }
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
                    var bfd = Field;
                    enemy.TickDodge(Time.deltaTime, Time.time, _combat?.ProjectilePool?.ActiveProjectiles,
                        (bfd != null ? bfd.boundaryMinX : 0.5f) + 0.5f, (bfd != null ? bfd.boundaryMaxX : 23.5f) - 0.5f);

                    if (_currentStage?.enemyRowsY != null)
                    {
                        enemy.MoveTowardsTargetRow(Time.deltaTime, _currentStage.enemyRowsY);
                    }
                }
            }
        }

        /// <summary>Skill/grenade/revive for an ally (or autopilot). Grenades aim at the nearest target within grenade range.</summary>
        public bool TickPilot(AbilityPilot pilot, float dt, Vector2? inRange)
        {
            var c = pilot.Character;
            Vector2? target = inRange ?? FindTargetFor(c.Position, c.Data?.grenade?.maxRange ?? 9f);
            int before = pilot.GrenadesThrown;
            bool moving = pilot.Tick(dt, target, _playerTeam);
            if (pilot.GrenadesThrown > before) GrenadesThrown++;
            return moving;
        }

        /// <summary>v2 (07 D1): AI advances a row only when every target is outside weapon range. Also dodges tail sweep and strafes on X.</summary>
        public static bool ShouldAdvanceForRange(bool hasTargetInWeaponRange, bool advanceWhenOutOfRangeEnabled)
            => advanceWhenOutOfRangeEnabled && !hasTargetInWeaponRange;

        public void DriveAllyMovement(CharacterStateMachine c, Vector2? inRangeTarget)
        {
            if (c.MovementState == MovementState.RowChanging || c.MovementState == MovementState.Rolling) return;

            bool dodge = _botBehavior == null || _botBehavior.dodgeTailSweep;
            if (dodge && TryGetTailSweepDodge(c.Position, out float dodgeDir))
            {
                c.OnMoveCommand(new MoveCommand(new Vector2(dodgeDir, 0f), 1f, Time.time));
                return;
            }

            if (ShouldAdvanceForRange(inRangeTarget.HasValue, _botBehavior == null || _botBehavior.advanceWhenOutOfRange))
            {
                var bf = ServiceLocator.Get<Battlefield.BattlefieldSystem>();
                if (bf != null && bf.CanMoveToRow(c.CurrentRowIndex, 1, Team.Player))
                {
                    c.OnRowChangeCommand(new RowChangeCommand(1, Time.time));
                    return;
                }
                Vector2? any = FindTargetFor(c.Position, 100f);
                if (any.HasValue && Mathf.Abs(any.Value.x - c.Position.x) > 0.6f)
                {
                    c.OnMoveCommand(new MoveCommand(new Vector2(Mathf.Sign(any.Value.x - c.Position.x), 0f), 0.8f, Time.time));
                    return;
                }
            }
            c.OnMoveCommand(MoveCommand.Stop(Time.time));
        }

        /// <summary>True if pos is inside a telegraphed tail sweep; dodgeDir = -1/+1 toward the nearest edge.</summary>
        public bool TryGetTailSweepDodge(Vector2 pos, out float dodgeDir)
        {
            dodgeDir = 0f;
            if (_activeBoss == null || _activeBoss.CurrentWarning != BossInstance.WarningType.TailSweep) return false;
            Rect a = _activeBoss.TailSweepArea;
            Rect padded = new Rect(a.xMin - 0.6f, a.yMin, a.width + 1.2f, a.height);
            if (!padded.Contains(pos)) return false;
            var field = Field;
            float minX = field != null ? field.boundaryMinX : 0.5f;
            float maxX = field != null ? field.boundaryMaxX : 23.5f;
            bool leftOk = a.xMin - 1f > minX;
            bool rightOk = a.xMax + 1f < maxX;
            if (leftOk && (!rightOk || pos.x - a.xMin < a.xMax - pos.x)) dodgeDir = -1f;
            else dodgeDir = 1f;
            return true;
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
                var rules = ServiceLocator.Get<MatchRulesData>();
                float hpMul = rules != null ? rules.GetPvEEnemyHpMul(PartySize) : 1f;
                enemy.Initialize(entry.enemyData, new Vector2(spawnX, spawnY), row, hpMul);
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
            _stageViews.Add(go);
        }

        private void HandleEnemyDefeated(EnemyInstance enemy)
        {
            EnemiesDefeated++;
            _combat?.UnregisterCombatTarget(enemy);
            _currentGauge += enemy.Data.gaugeValue;
            OnGaugeChanged?.Invoke(_currentGauge);
            OnEnemyDefeated?.Invoke(enemy);
            HillNGun.Combat.DropDirector.Instance?.HandleEnemyDefeated(enemy.Position, enemy.Data.gaugeValue);

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

            int enemyEntries = _currentWaveData.enemies != null ? _currentWaveData.enemies.Length : 0;
            bool waveComplete = _nextSpawnIndex >= enemyEntries;
            if (!waveComplete) return;

            bool waitClear = _currentWaveData.waitForClear;
            if (waitClear && enemyEntries > 0)
            {
                foreach (var enemy in _activeEnemies)
                {
                    if (enemy.IsAlive && !enemy.IsFleeing) return;
                }
            }

            // schedule next wave after nextWaveDelay (rest waves use delayBeforeWave via StartWave)
            if (_postWaveTimer < 0f)
                _postWaveTimer = Mathf.Max(0f, _currentWaveData.nextWaveDelay);
        }

        private void AdvanceWaveIfReady()
        {
            if (_postWaveTimer < 0f) return;
            _postWaveTimer -= Time.deltaTime;
            if (_postWaveTimer > 0f) return;
            _postWaveTimer = -1f;

            int lastWave = Mathf.Min(_currentStage.waveCount, _currentStage.waves?.Length ?? 0) - 1;
            if (_currentWave < lastWave)
            {
                StartWave(_currentWave + 1);
            }
            else if (_currentGauge < GaugeMax && lastWave >= 0)
            {
                Debug.Log($"[PvE] All waves spent with gauge {_currentGauge}/{GaugeMax} - repeating final wave");
                StartWave(lastWave);
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
            EffectsManager.Instance?.SpawnMuzzleFlash(enemy.Position, direction, false, 0.8f);
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
            float radius = boss.CurrentPhaseData?.fanShotBulletRadius ?? 0.2f;
            float coverMul = boss.CurrentPhaseData?.coverDamageMul ?? 1f;
            foreach (var dir in directions)
                _combat.SpawnEnemyProjectile(boss.Position + dir * 1.2f, dir, speed, radius, damage, 24f, Team.Enemy, coverMul);
            BossVolleys++;
            if (boss.CurrentPhase == BossPhase.Phase3) BossPhase3Volleys++;
            OnBossVolley?.Invoke(boss, boss.LastBurstIndex);
        }

        public event System.Action<BossInstance, Rect, int> OnTailSweepResolved;
        public int TailSweepCount { get; private set; }
        public int TailSweepHits { get; private set; }

        /// <summary>Tail sweep: hits every active, non-invulnerable player inside the locked floor area (ignores cover).</summary>
        private void HandleBossTailSweep(BossInstance boss, float width)
        {
            Rect area = boss.TailSweepArea;
            int damage = boss.CurrentPhaseData?.tailSweepDamage ?? 400;
            int hits = 0;
            foreach (var c in _playerTeam)
            {
                if (c == null || c.OverallState != CharacterOverallState.Active || c.IsInvulnerable) continue;
                if (!area.Contains(c.Position)) continue;
                Vector2 dir = new Vector2(Mathf.Sign(c.Position.x - area.center.x), 0f);
                c.TakeDamage(damage, dir);
                hits++;
            }
            TailSweepCount++;
            TailSweepHits += hits;
            EffectsManager.Instance?.SpawnImpact(new Vector2(area.center.x, area.center.y), true);
            FindFirstObjectByType<ScreenShake>()?.ShakeHeavyHit();
            Debug.Log($"[PvE] Tail sweep area={area} damage={damage} hits={hits}");
            OnTailSweepResolved?.Invoke(boss, area, hits);
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

        /// <summary>Removes enemies/boss (logic + views) and in-flight bullets. resetCovers: restore covers for a new stage.</summary>
        public void CleanupStageActors(bool resetCovers)
        {
            foreach (var e in _activeEnemies)
            {
                if (e == null) continue;
                e.OnDefeated -= HandleEnemyDefeated;
                e.OnDamageTaken -= HandleEnemyDamaged;
                e.OnAttack -= HandleEnemyAttack;
                e.OnExplosiveThrown -= HandleEnemyExplosive;
            }
            if (_activeBoss != null)
            {
                _activeBoss.OnDefeated -= HandleBossDefeated;
                _activeBoss.OnDamageTaken -= HandleBossDamaged;
                _activeBoss.OnFanShot -= HandleBossFanShot;
                _activeBoss.OnTailSweep -= HandleBossTailSweep;
                _activeBoss.OnSummonMooks -= HandleBossSummon;
            }
            _activeEnemies.Clear();
            _activeBoss = null;

            foreach (var go in _stageViews)
            {
                if (go != null) Destroy(go);
            }
            _stageViews.Clear();

            if (_combat != null)
            {
                _combat.ClearCombatTargets();
                if (resetCovers) _combat.Clear();
                else _combat.ProjectilePool.Clear();
            }
        }

        private void CompleteStage(PvERoundResult result)
        {
            if (!_isMatchActive) return;
            _roundResult = result;
            _isMatchActive = false;
            Debug.Log($"[PvE] Stage {_currentStageIndex + 1}/{_stages.Length} ({_currentStage?.id}) complete: {result} at {_stageTimer:F1}s");
            CleanupStageActors(false);

            OnStageComplete?.Invoke(result);

            var flowManager = GameFlowManager.Instance;
            if (flowManager?.CurrentSession != null && result != PvERoundResult.StageFailed && _currentStage != null)
            {
                flowManager.CurrentSession.clearedStageIds.Add(_currentStage.id);
                if (result == PvERoundResult.BossDefeated) flowManager.CurrentSession.bossDefeated = true;
                flowManager.CurrentSession.pveDownedCount = TeamDownedCount;
                flowManager.CurrentSession.pveBarrelExplosions = BarrelExplosions;
                flowManager.CurrentSession.pveStageClearTime = _stageTimer;
                int stars = HillNGun.Progression.StarRating.Compute(_currentStage, true, _stageTimer, TeamDownedCount, BarrelExplosions);
                HillNGun.Progression.ProgressionService.Instance?.RecordStageStars(_currentStage.id, stars);
            }
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
                    var session = flowManager.CurrentSession;
                    if (session != null)
                    {
                        session.matchTime = MatchElapsed;
                        session.damageDealt = TotalEnemyDamage;
                        session.damageTaken = PlayerDamageTaken;
                        session.playerDeaths = PlayerEliminations;
                        session.playerKills = PlayerKills;
                        session.coversDestroyed = CoversDestroyed;
                        session.currentStageIndex = _currentStageIndex;
                        session.pveDownedCount = TeamDownedCount;
                        session.pveBarrelExplosions = BarrelExplosions;
                        session.pveStageClearTime = _stageTimer;
                    }
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

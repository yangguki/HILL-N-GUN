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
using HillNGun.Scenery;

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

        /// <summary>Living squad size (human + AI). Used for normal enemy HP scaling only — not bosses.</summary>
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

        /// <summary>Living human (player-controlled) count for boss HP scaling.</summary>
        public int HumanPlayerCount
        {
            get
            {
                if (_playerTeam == null || _playerTeam.Count <= _playerControlledIndex) return 1;
                var c = _playerTeam[_playerControlledIndex];
                if (c != null && c.OverallState != CharacterOverallState.Eliminated) return 1;
                return 1; // scaling baseline stays at least one human seat
            }
        }

        /// <summary>Living AI ally count for boss HP scaling (excludes the player-controlled seat).</summary>
        public int AiAllyCount
        {
            get
            {
                int n = 0;
                if (_playerTeam == null) return 0;
                for (int i = 0; i < _playerTeam.Count; i++)
                {
                    if (i == _playerControlledIndex) continue;
                    var c = _playerTeam[i];
                    if (c != null && c.OverallState != CharacterOverallState.Eliminated) n++;
                }
                return n;
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

        // ---- FreeMove (MatchRulesData.pveMoveMode) ----
        private MoveMode _moveMode = MoveMode.RowLane;
        public MoveMode MoveMode => _moveMode;
        public bool IsFreeMove => _moveMode == MoveMode.FreeMove;
        private Battlefield.FreeMoveWorld _world;
        private EnemySpawnDirector _director;
        private FireTokenScheduler _fire;
        private System.Random _aiRng = new System.Random(1);
        private FreeCtx _ctx;
        private int _enemySerial;
        private readonly List<Vector2> _playerPosBuf = new List<Vector2>();
        public Battlefield.FreeMoveWorld World => _world;
        public EnemySpawnDirector Director => _director;
        public FireTokenScheduler FireTokens => _fire;
        public StreetLayoutData StreetLayout => _world?.Layout;
        /// <summary>Max enemies (incl. the boss) winding up / firing in the same frame this stage.</summary>
        public int PeakConcurrentAttackers { get; private set; }
        public int FramesOverFireCap { get; private set; }
        public event System.Action<EnemyInstance> OnEnemyEntranceComplete;

        private MatchRulesData Rules => ServiceLocator.Get<MatchRulesData>() ?? RuntimeAssetManager.Instance?.MatchRules;

        private sealed class FreeCtx : IEnemyFreeContext
        {
            private readonly PvEMatchManager _m;
            public FreeCtx(PvEMatchManager m) { _m = m; }
            public Vector2 NearestPlayerPos(Vector2 from) => _m.GetNearestPlayerPosition(from);
            public Vector2 MoveEnemy(EnemyInstance e, Vector2 delta) => _m._world != null ? _m._world.Move(e.Position, delta, e.BodyRadius) : e.Position + delta;
            public Vector2 PickPost(EnemyInstance e) => _m.PickPost(e);
            public bool TryAcquireFire(EnemyInstance e) => _m._fire == null || _m._fire.TryAcquire(e, _m._stageTimer);
            public void ReleaseFire(EnemyInstance e) => _m._fire?.Release(e);
        }

        private void SetupMoveMode()
        {
            var rules = Rules;
            _moveMode = rules != null ? rules.pveMoveMode : MoveMode.FreeMove;
            if (!IsFreeMove) return;
            _world = new Battlefield.FreeMoveWorld((StreetLayoutData)null);
            _world.CoverSource = () => _combat != null ? _combat.Covers : null;
            ServiceLocator.Register(_world);
            _ctx = new FreeCtx(this);
            _fire = new FireTokenScheduler(rules != null ? rules.pveMaxConcurrentEnemyFire : 3);
            Debug.Log($"[PvE] MoveMode={_moveMode} (pveMoveMode data flag) fireCap={_fire.Capacity}");
        }

        private void Start()
        {
            LoadDataFromAssetManager();
            _combat = ServiceLocator.Get<CombatSystem>();
            SetupMoveMode();
            // Covers spawn in StartStage with stage-specific placements (avoid double-registering battlefield defaults).
            ProjectileView.Ensure();
            GrenadeView.Ensure();
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
            CoverFieldSpawner.SpawnWithPreset(field, am?.CoverDatas, false, _currentStage != null ? _currentStage.id : null, overrides);
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
                    var pos = h.PositionOn(field);
                    var b = new HazardExplosiveBarrel();
                    b.Initialize(pos, _combat, $"{_currentStage.id}:barrel:{pos.x:0.#},{pos.y:0.#}");
                    // 1-3 star: count UNIQUE exploded barrel ids (a barrel explodes at most once)
                    b.OnExploded += eb => { if (_explodedBarrelIds.Add(eb.InstanceId)) BarrelExplosions = _explodedBarrelIds.Count; };
                    _hazards.Add(b);
                }
                else if (h.type == HazardType.TrainPass)
                {
                    // armed by rest wave paceTag; duration from param
                }
            }
        }

        private readonly System.Collections.Generic.HashSet<string> _explodedBarrelIds = new System.Collections.Generic.HashSet<string>();
        public System.Collections.Generic.IReadOnlyList<HillNGun.Combat.HazardExplosiveBarrel> Hazards => _hazards;

        private void ClearHazards()
        {
            _explodedBarrelIds.Clear();
            _combat?.ClearArmedHazards();
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
                    character.SetMoveMode(_moveMode);
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
            if (_world != null && ServiceLocator.Get<Battlefield.FreeMoveWorld>() == _world) ServiceLocator.Unregister<Battlefield.FreeMoveWorld>();
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
            SetupStageFreeMove();
            HillNGun.Combat.DropDirector.Instance?.ClearField();
            ClearHazards();
            TeamDownedCount = 0;
            BarrelExplosions = 0;
            _postWaveTimer = -1f;
            _trainBlockTimer = 0f;

            ResetPlayerTeamForStage();
            SpawnCovers();
            if (IsFreeMove)
                foreach (var c in _playerTeam) if (c != null) c.SetPosition(c.Position); // depenetrate from the stage covers (no blocked start slot)
            SpawnHazards();
            SpawnVisualProps();
            FindFirstObjectByType<HillNGun.Battlefield.BattlefieldRenderer>()?.RefreshStageVisual();
            StartCoroutine(BeginStagePresentation());

            // Wave/boss start deferred to BeginStagePresentation so intro/WANTED can play first
            // (presentation coroutine kicks combat).

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

        private void SetupStageFreeMove()
        {
            if (!IsFreeMove || _currentStage == null) return;
            var layout = _currentStage.visual != null ? _currentStage.visual.streetLayout : null;
            if (layout == null) Debug.LogWarning($"[PvE] stage {_currentStage.id} has no StreetLayoutData - FreeMove uses the default field polygon and cannot spawn");
            _world.SetLayout(layout);
            EntranceDoors.Setup(layout, _currentStage.visual != null ? _currentStage.visual.timeOfDay : TimeOfDay.Noon); // v13 designer door sprites
            var rules = Rules;
            _director = new EnemySpawnDirector(layout, _currentStage.spawnSeed);
            if (rules != null)
            {
                _director.SafeRadius = rules.pveSpawnSafeRadius;
                _director.RetryInterval = rules.pveSpawnRetryInterval;
                _director.RelaxAfter = rules.pveSpawnRelaxAfter;
            }
            _aiRng = new System.Random(_currentStage.spawnSeed * 7919 + 17);
            _fire?.Clear();
            _fire?.ResetStats();
            PeakConcurrentAttackers = 0;
            FramesOverFireCap = 0;
        }

        private void StartWave(int waveIndex)
        {
            if (_currentStage?.waves == null || waveIndex >= _currentStage.waves.Length) return;

            _director?.BeginWave(waveIndex);
            _currentWave = waveIndex;
            _currentWaveData = _currentStage.waves[waveIndex];
            StagePresentation.Instance?.ShowWave(waveIndex + 1);
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
            int humans = HumanPlayerCount;
            int aiAllies = AiAllyCount;
            int scaledHp = _currentStage.bossData.GetScaledMaxHp(humans, aiAllies);
            _activeBoss.Initialize(_currentStage.bossData, _currentStage.bossData.battlePosition, scaledHp);
            if (IsFreeMove && StreetLayout != null) _activeBoss.SetEntryStart(StreetLayout.bossEntryStart); // walks in through the back gate (no drop)
            Debug.Log($"[PvE] Boss {_currentStage.bossData.id} hp={scaledHp} (humans={humans} aiAllies={aiAllies} aiAllyHpAdd={_currentStage.bossData.aiAllyHpAdd} partyTotal={PartySize})");
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

            if (IsFreeMove)
            {
                if (_director != null && !TrainBlockingSpawns)
                    _director.Tick(_stageTimer, ActivePlayerPositions(), SpawnFromDirector);
                UpdateEnemiesFree(Time.deltaTime);
            }
            else UpdateEnemies();
            CheckTeamDefeat();
        }

        private IList<Vector2> ActivePlayerPositions()
        {
            _playerPosBuf.Clear();
            foreach (var c in _playerTeam)
                if (c != null && c.OverallState != CharacterOverallState.Eliminated) _playerPosBuf.Add(c.Position);
            return _playerPosBuf;
        }

        private void UpdateEnemiesFree(float dt)
        {
            if (_fire != null) _fire.Reserved = _activeBoss != null && _activeBoss.IsAlive ? 1 : 0;
            var projectiles = _combat?.ProjectilePool?.ActiveProjectiles;
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var e = _activeEnemies[i];
                if (e.IsDefeated)
                {
                    e.UpdateDefeated(dt, _ctx);
                    if (e.IsGone) _activeEnemies.RemoveAt(i);
                    continue;
                }
                e.UpdateFree(dt, _ctx);
                e.TickDodge(dt, Time.time, projectiles, -50f, 50f, e.FreeMover);
            }
            int attacking = (_fire != null ? _fire.ActiveCount : 0) + (_activeBoss != null && _activeBoss.IsAlive && _activeBoss.IsAttacking ? 1 : 0);
            if (attacking > PeakConcurrentAttackers) PeakConcurrentAttackers = attacking;
            int cap = Rules != null ? Rules.pveMaxConcurrentEnemyFire : 3;
            if (attacking > cap) FramesOverFireCap++;
        }

        private void SpawnFromDirector(EnemySpawnDirector.Pending p, int pointIndex)
        {
            var layout = StreetLayout;
            if (layout == null || p.data == null) return;
            var sp = layout.spawnPoints[pointIndex];
            var rules = Rules;
            var enemy = new EnemyInstance();
            float hpMul = rules != null ? rules.GetPvEEnemyHpMul(PartySize) : 1f;
            int seed = (_currentStage != null ? _currentStage.spawnSeed : 1) * 92821 + p.serial * 7919;
            enemy.InitializeFree(p.data, sp, pointIndex, hpMul, rules != null ? rules.pveEntranceAttackDelay : 0.8f, new System.Random(seed));
            enemy.Serial = _enemySerial++;
            var feel = ServiceLocator.Get<MovementFeelData>() ?? MovementFeelData.Fallback;
            enemy.BodyRadius = feel.freeMoveEnemyRadius * Mathf.Max(0.5f, p.data.scale);
            var en = enemy;
            enemy.FreeMover = d => _world != null ? _world.Move(en.Position, d, en.BodyRadius) : en.Position + d;
            enemy.OnEntranceComplete += HandleEntranceComplete;
            if (EnemySpawnDirector.IsSniper(p.data)) _director?.SetOccupied(pointIndex, true);
            _combat?.RegisterCombatTarget(enemy);
            enemy.OnDefeated += HandleEnemyDefeated;
            enemy.OnDamageTaken += HandleEnemyDamaged;
            enemy.OnAttack += HandleEnemyAttack;
            enemy.OnExplosiveThrown += HandleEnemyExplosive;
            _activeEnemies.Add(enemy);
            CreateEnemyView(enemy);
            Debug.Log($"[Spawn] {p.data.id} at {sp.id} type={sp.type} side={sp.side} t={_stageTimer:F1}s wave={_currentWave + 1}");
            OnEnemySpawned?.Invoke(enemy);
        }

        private void HandleEntranceComplete(EnemyInstance e)
        {
            if (_world != null && !e.Data.stationary) e.SetPosition(_world.Depenetrate(e.Position, e.BodyRadius));
            OnEnemyEntranceComplete?.Invoke(e);
        }

        /// <summary>FreeMove post: a free spot in the enemy zone at a fair distance from the nearest player, spread from other enemies.</summary>
        public Vector2 PickPost(EnemyInstance e)
        {
            var layout = StreetLayout;
            if (layout == null || _world == null || e?.Data == null) return e != null ? e.Position : Vector2.zero;
            Rect z = layout.enemyPostZone;
            float desired = Mathf.Clamp(e.Data.keepDistance + 2f, 3f, Mathf.Max(3f, e.Data.attackRange * 0.8f));
            Vector2 best = e.Position; float bestScore = float.MinValue;
            for (int k = 0; k < 14; k++)
            {
                float y = z.yMin + (float)_aiRng.NextDouble() * z.height;
                if (!Battlefield.FreeMoveCollision.SpanAtY(_world.Polygon, y, out float x0, out float x1)) continue;
                x0 = Mathf.Max(x0 + layout.enemyPostEdgeMargin, z.xMin); x1 = Mathf.Min(x1 - layout.enemyPostEdgeMargin, z.xMax);
                if (x1 <= x0) continue;
                var c = new Vector2(Mathf.Lerp(x0, x1, (float)_aiRng.NextDouble()), y);
                if (!_world.IsFree(c, e.BodyRadius + 0.2f)) continue;
                Vector2 pl = GetNearestPlayerPosition(c);
                float score = -Mathf.Abs((pl - c).magnitude - desired);
                float spread = 3f;
                foreach (var o in _activeEnemies)
                    if (o != e && o.IsAlive) spread = Mathf.Min(spread, Mathf.Min((o.Post - c).magnitude, (o.Position - c).magnitude));
                score += 0.6f * spread - 0.08f * (c - e.Position).magnitude;
                if (score > bestScore) { bestScore = score; best = c; }
            }
            return best;
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
                Vector2? inRange = FindTargetFor(c.Position, IsFreeMove ? range : range * 1.1f); // FreeMove: true shot range (no row slack)
                var pilot = i < _allyPilots.Count ? _allyPilots[i] : null;
                bool pilotMoving = pilot != null && TickPilot(pilot, dt, inRange);
                if (!pilotMoving) DriveAllyMovement(c, inRange);
                else if (IsFreeMove) { var pn = Nav(c); pn.intent = AllyIntent.Pilot; pn.cmdMag = 0f; pn.lastPos = c.Position; pn.blockedTime = 0f; }
                if (pilot == null || !pilot.IsHolding) gunner.Tick(dt, inRange);
                if (IsFreeMove) Nav(c).shots = gunner.ShotsAttempted;
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
                StagePresentation.Instance?.ShowClear();
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

        private sealed class AllyNav
        {
            public Vector2 lastPos; public float stuck; public float sidestep; public float sideSign = 1f;
            // v12 instrumentation (view/debug only; no gameplay effect)
            public AllyIntent intent; public bool hasTarget, inRange; public float targetDist, cmdMag, progress = 1f, blockedTime;
            public int sidesteps, blockedEpisodes, shots;
            public Vector2 blockedStart;
            public readonly AllyFreeSteer steer = new AllyFreeSteer();
        }

        /// <summary>Why a FreeMove ally is (not) moving this frame.</summary>
        public enum AllyIntent { None, Pilot, Dodge, HoldInRange, BackOff, Approach, Sidestep, NoTarget, Rolling }

        public struct AllyIntentInfo
        {
            public AllyIntent intent; public bool hasTarget, inRange; public float targetDist, cmdMag, progress, blockedTime;
            public int sidesteps, blockedEpisodes, shots;
        }

        public int AllyBlockedEpisodes { get; private set; }

        private AllyNav Nav(CharacterStateMachine c)
        {
            if (!_allyNav.TryGetValue(c, out var nav)) { nav = new AllyNav { lastPos = c.Position, sideSign = (_allyNav.Count % 2 == 0) ? 1f : -1f }; _allyNav[c] = nav; }
            return nav;
        }

        public bool TryGetAllyIntent(CharacterStateMachine c, out AllyIntentInfo info)
        {
            info = default;
            if (c == null || !_allyNav.TryGetValue(c, out var n)) return false;
            info = new AllyIntentInfo { intent = n.intent, hasTarget = n.hasTarget, inRange = n.inRange, targetDist = n.targetDist, cmdMag = n.cmdMag,
                progress = n.progress, blockedTime = n.blockedTime, sidesteps = n.sidesteps, blockedEpisodes = n.blockedEpisodes, shots = n.shots };
            return true;
        }

        /// <summary>Progress bookkeeping: a commanded move making < 25% of its expected distance for > 1 s is a blocked episode (logged).</summary>
        private void TrackAllyProgress(CharacterStateMachine c, AllyNav nav, float dt)
        {
            float expected = nav.cmdMag * (c.Data != null ? c.Data.moveSpeed : 3f) * dt;
            float moved = (c.Position - nav.lastPos).magnitude;
            nav.progress = expected > 1e-4f ? Mathf.Clamp01(moved / expected) : 1f;
            bool blockedNow = expected > 1e-4f && nav.progress < 0.25f && c.MovementState != MovementState.Rolling;
            if (blockedNow)
            {
                if (nav.blockedTime <= 0f) nav.blockedStart = c.Position;
                float before = nav.blockedTime;
                nav.blockedTime += dt;
                if (before < 1f && nav.blockedTime >= 1f)
                {
                    nav.blockedEpisodes++; AllyBlockedEpisodes++;
                    Debug.Log($"[AllyNav] BLOCKED {c.Data?.id} at ({c.Position.x:F2},{c.Position.y:F2}) intent={nav.intent} target={(nav.hasTarget ? nav.targetDist.ToString("F1") : "none")} inRange={nav.inRange} sidesteps={nav.sidesteps} t={_stageTimer:F1}s stage={CurrentStageId}");
                }
            }
            else if (nav.blockedTime > 0f)
            {
                if (nav.blockedTime >= 1f) Debug.Log($"[AllyNav] UNBLOCKED {c.Data?.id} after {nav.blockedTime:F1}s moved {(c.Position - nav.blockedStart).magnitude:F2} t={_stageTimer:F1}s");
                nav.blockedTime = 0f;
            }
        }
        private readonly Dictionary<CharacterStateMachine, AllyNav> _allyNav = new Dictionary<CharacterStateMachine, AllyNav>();

        /// <summary>FreeMove ally/autopilot: walk (2D) toward the nearest target until in weapon range, back off when crowded,
        /// dodge the tail sweep, side-step around covers when stuck. Aiming stays with AutoGunner (2D direction to target).</summary>
        private void DriveAllyFree(CharacterStateMachine c, Vector2? inRangeTarget)
        {
            var nav = Nav(c);
            float dt = Time.deltaTime;
            TrackAllyProgress(c, nav, dt);
            if (c.MovementState == MovementState.Rolling) { nav.intent = AllyIntent.Rolling; nav.cmdMag = 0f; nav.lastPos = c.Position; return; }
            float now = Time.time;
            nav.inRange = inRangeTarget.HasValue;
            bool dodge = _botBehavior == null || _botBehavior.dodgeTailSweep;
            if (dodge && TryGetTailSweepDodge(c.Position, out float dodgeDir))
            {
                nav.intent = AllyIntent.Dodge; nav.cmdMag = 1f; nav.lastPos = c.Position;
                c.OnMoveCommand(new MoveCommand(new Vector2(dodgeDir, -0.3f).normalized, 1f, now));
                return;
            }
            Vector2 dir = Vector2.zero; float mag = 1f;
            Vector2? any = inRangeTarget ?? FindTargetFor(c.Position, 100f);
            nav.hasTarget = any.HasValue;
            nav.targetDist = any.HasValue ? (any.Value - c.Position).magnitude : -1f;
            if (inRangeTarget.HasValue)
            {
                Vector2 away = c.Position - inRangeTarget.Value;
                if (away.magnitude < 2.2f) { dir = away.normalized; mag = 0.8f; nav.intent = AllyIntent.BackOff; }
                else nav.intent = AllyIntent.HoldInRange; // deliberate: in weapon range, AutoGunner fires
            }
            else if (any.HasValue) { dir = (any.Value - c.Position).normalized; nav.intent = AllyIntent.Approach; }
            else nav.intent = AllyIntent.NoTarget; // nothing hittable (all entering / wave gap)
            if (dir == Vector2.zero) { c.OnMoveCommand(MoveCommand.Stop(now)); nav.cmdMag = 0f; nav.steer.Reset(); nav.lastPos = c.Position; return; }
            bool lastActive = nav.cmdMag > 0f;
            Vector2 goal = any.HasValue && nav.intent == AllyIntent.Approach ? any.Value : c.Position + dir * 2f;
            dir = nav.steer.Steer(goal, dir, dt, lastActive, nav.progress, _world, c.Position, c.BodyRadius);
            if (nav.steer.Sidestepping || nav.steer.Routing) nav.intent = AllyIntent.Sidestep;
            nav.sidesteps = nav.steer.sidesteps;
            nav.lastPos = c.Position;
            nav.cmdMag = mag;
            c.OnMoveCommand(new MoveCommand(dir, mag, now));
        }

        public void DriveAllyMovement(CharacterStateMachine c, Vector2? inRangeTarget)
        {
            if (c != null && c == HillNGun.AI.AbilityPilot.DiagHoldActor) { if (c.MovementState == MovementState.Moving) c.OnMoveCommand(new MoveCommand(Vector2.zero, 0f, Time.time)); return; } // diagnostic take only
            if (c.IsFreeMove) { DriveAllyFree(c, inRangeTarget); return; }
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

        public Vector2 GetNearestPlayerPosition(Vector2 fromPosition)
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
            if (IsFreeMove)
            {
                // painted entrances only (no sky drop): the director places it at a safe door / alley / street end / fence / window
                _director?.Enqueue(entry, _stageTimer, Rules != null ? Rules.pveSpawnStagger : 0.3f);
                return;
            }

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
            if (enemy.FreeMove)
            {
                _fire?.Release(enemy);
                if (enemy.Data != null && EnemySpawnDirector.IsSniper(enemy.Data)) _director?.SetOccupied(enemy.SpawnPointIndex, false);
            }
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
            bool waveComplete = _nextSpawnIndex >= enemyEntries && (_director == null || !IsFreeMove || _director.PendingCount == 0);
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
            _combat.SpawnEnemyProjectile(enemy.Position + direction * 0.6f, direction, speed, d.explosiveRadius * 0.25f, damage, d.attackRange, Team.Enemy, 1f, applyGlobalSpeedMul: false); // thrown dynamite: fixed flight time, NOT bullet-speed scaled
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
            _director?.Clear();
            _fire?.Clear();
            _allyNav.Clear();

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


        private void SpawnVisualProps()
        {
            foreach (var s in FindObjectsByType<SaloonSignSway>(FindObjectsSortMode.None))
                if (s != null) Destroy(s.gameObject);
            foreach (var b in FindObjectsByType<ExplosiveBarrelView>(FindObjectsSortMode.None))
                if (b != null) Destroy(b.gameObject);

            if (_hazards != null)
            {
                foreach (var h in _hazards)
                {
                    if (h == null) continue;
                    var go = new GameObject("ExplosiveBarrelView");
                    go.AddComponent<ExplosiveBarrelView>().Bind(h);
                }
            }

            var visual = _currentStage != null ? _currentStage.visual : null;
            bool wantSign = visual == null || visual.saloonSignSway;
            if (wantSign && _currentStage != null && !_isBossStage)
            {
                var go = new GameObject("SaloonSign");
                go.AddComponent<SaloonSignSway>().Place(new Vector2(12f, 14.5f));
            }
        }

        private System.Collections.IEnumerator BeginStagePresentation()
        {
            var pres = StagePresentation.Instance;
            if (pres == null)
            {
                var go = new GameObject("StagePresentation");
                pres = go.AddComponent<StagePresentation>();
            }

            yield return pres.PlayIntroPan(1.5f);

            if (_isBossStage)
            {
                string bossName = "REX";
                int bounty = 1000;
                Sprite portrait = null;
                if (_currentStage?.bossData != null)
                {
                    string key = _currentStage.bossData.nameKey;
                    string fallback = string.IsNullOrEmpty(_currentStage.bossData.id) ? "REX" : _currentStage.bossData.id;
                    if (!string.IsNullOrEmpty(key))
                    {
                        var loc = ServiceLocator.Get<HillNGun.UI.LocalizationManager>();
                        bossName = loc != null && loc.HasString(key) ? loc.GetString(key)
                            : HillNGun.UI.UIKit.T(key, key == "pve.boss.rattlesnake_rex" ? "방울뱀 렉스" : fallback);
                    }
                    else
                        bossName = fallback;
                }
                var assets = RuntimeAssetManager.Instance;
                var lib = assets != null ? assets.SpriteLibrary : ServiceLocator.Get<SpriteLibrary>();
                if (lib?.boss != null)
                    portrait = lib.boss.portrait != null ? lib.boss.portrait : lib.boss.idle;
                if (portrait == null)
                    portrait = HillNGun.UI.LobbyUI.BossPortrait();
                yield return pres.PlayWantedCard(bossName, bounty, portrait, 2f);
                SpawnBoss();
            }
            else
            {
                StartWave(0);
            }
        }

        public void TeamDefeated()
        {
            CompleteStage(PvERoundResult.StageFailed);
        }
    }
}

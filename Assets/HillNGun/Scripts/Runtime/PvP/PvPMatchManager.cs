using UnityEngine;
using System.Collections.Generic;
using HillNGun.Data;
using HillNGun.Core;
using HillNGun.Characters;
using HillNGun.AI;
using HillNGun.Cover;
using HillNGun.Combat;
using HillNGun.Feel;
using HillNGun.Input;

namespace HillNGun.PvP
{
    /// <summary>
    /// PvP 3v3 wipe-out, best of 3 (00, 03 §6). Round flow: countdown → active (max 80s) → result → next round / match end.
    /// Rules come from MatchRulesData (serialized fields are only fallbacks when no asset is loaded).
    /// </summary>
    public class PvPMatchManager : MonoBehaviour
    {
        [Header("Fallback Match Settings (MatchRulesData overrides)")]
        [SerializeField] private int _roundsToWin = 2;
        [SerializeField] private int _maxRounds = 3;
        [SerializeField] private float _roundTimeLimit = 80f;
        [SerializeField] private float _roundStartDelay = 3f;
        [SerializeField] private float _roundEndDelay = 3f;

        [Header("Character Data")]
        [SerializeField] private CharacterData[] _characterDatas;

        [Header("Bot Settings")]
        [SerializeField] private BotBehaviorData _botBehavior;

        private MatchRulesData _rules;
        private readonly List<CharacterStateMachine> _playerTeam = new List<CharacterStateMachine>();
        private readonly List<CharacterStateMachine> _enemyTeam = new List<CharacterStateMachine>();
        private readonly List<BotBrain> _allyBotBrains = new List<BotBrain>();
        private readonly List<BotBrain> _enemyBotBrains = new List<BotBrain>();
        private BotBrain _playerAutoBrain;
        private int _playerControlledIndex = 0;
        private CombatSystem _combat;
        private readonly SupplyCrate _crate = new SupplyCrate();
        private SupplyCrateView _crateView;
        private bool _crateDropped;
        private readonly HashSet<CharacterStateMachine> _pendingDowns = new HashSet<CharacterStateMachine>();

        // match totals
        public int TotalDamageToEnemyTeam { get; private set; }
        public int TotalDamageToPlayerTeam { get; private set; }
        public int PlayerKills { get; private set; }
        public int PlayerDeaths { get; private set; }
        public int CoversDestroyed { get; private set; }
        public float MatchElapsed { get; private set; }
        public int SupplyCratesOpened { get; private set; }

        private int _currentRound = 0;
        private int _playerRoundsWon = 0;
        private int _enemyRoundsWon = 0;
        private float _roundTimer;
        private float _roundElapsed;
        private bool _isMatchActive;
        private bool _isRoundActive;
        private PvPRoundResult _lastRoundResult = PvPRoundResult.InProgress;
        private RoundEndReason _lastRoundReason = RoundEndReason.None;
        private MatchResult _matchResult = MatchResult.None;
        private readonly List<PvPRoundResult> _roundHistory = new List<PvPRoundResult>();

        private float _delayTimer;
        private PvPPhase _currentPhase = PvPPhase.PreMatch;

        public int CurrentRound => _currentRound + 1;
        public int MaxRounds => _rules != null ? _rules.maxRounds : _maxRounds;
        public int RoundsToWin => _rules != null ? _rules.roundsToWin : _roundsToWin;
        public float RoundTimeLimit => _rules != null ? _rules.roundMaxTime : _roundTimeLimit;
        public int PlayerRoundsWon => _playerRoundsWon;
        public int EnemyRoundsWon => _enemyRoundsWon;
        public float RoundTimer => _roundTimer;
        public float RoundElapsed => _roundElapsed;
        public float PhaseTimer => _delayTimer;
        public bool IsMatchActive => _isMatchActive;
        public bool IsRoundActive => _isRoundActive;
        public PvPRoundResult LastRoundResult => _lastRoundResult;
        public RoundEndReason LastRoundReason => _lastRoundReason;
        public MatchResult MatchResult => _matchResult;
        public PvPPhase CurrentPhase => _currentPhase;
        public IReadOnlyList<CharacterStateMachine> PlayerTeam => _playerTeam;
        public IReadOnlyList<CharacterStateMachine> EnemyTeam => _enemyTeam;
        public IReadOnlyList<PvPRoundResult> RoundHistory => _roundHistory;
        public SupplyCrate Crate => _crate;
        public bool IsPlayerAutopilot => _playerAutoBrain != null;

        public event System.Action<int> OnRoundStarted;
        public event System.Action<int> OnRoundFight;
        public event System.Action<PvPRoundResult> OnRoundEnded;
        public event System.Action<MatchResult> OnMatchEnded;
        public event System.Action<CharacterStateMachine> OnCharacterDowned;
        public event System.Action<CharacterStateMachine> OnCharacterEliminated;
        public event System.Action<SupplyCrate> OnSupplyCrateDropped;
        public event System.Action<SupplyCrate, Team> OnSupplyCrateOpened;

        public enum PvPPhase
        {
            PreMatch,
            RoundStart,
            RoundActive,
            RoundEnd,
            MatchEnd
        }

        private void Start()
        {
            LoadDataFromAssetManager();
            _combat = ServiceLocator.Get<CombatSystem>();
            var am = RuntimeAssetManager.Instance;
            var field = am?.BattlefieldData ?? ServiceLocator.Get<BattlefieldData>();
            if (field != null)
            {
                CoverFieldSpawner.Spawn(field, am?.CoverDatas, field.mirrorCoversToEnemySideInPvP);
            }
            ProjectileView.Ensure();
            CombatFeedback.Ensure();
            if (_combat != null)
            {
                _combat.OnCharacterHit += HandleCharacterHit;
                _combat.OnCoverDestroyed += HandleCoverDestroyed;
                _combat.OnCombatTargetHit += HandleCombatTargetHit;
            }
            _crate.OnBroken += HandleCrateBroken;
            _crateView = new GameObject("SupplyCrateView").AddComponent<SupplyCrateView>();
            _crateView.Initialize(_crate);
            StartMatch();
        }

        private void OnDestroy()
        {
            if (_combat != null)
            {
                _combat.OnCharacterHit -= HandleCharacterHit;
                _combat.OnCoverDestroyed -= HandleCoverDestroyed;
                _combat.OnCombatTargetHit -= HandleCombatTargetHit;
                _combat.GlobalCoverDamageMul = 1f;
            }
        }

        private void LoadDataFromAssetManager()
        {
            _rules = ServiceLocator.Get<MatchRulesData>();
            var assetManager = RuntimeAssetManager.Instance;
            if (assetManager == null)
            {
                Debug.LogWarning("[PvPMatchManager] RuntimeAssetManager not found");
                return;
            }
            if (_rules == null) _rules = assetManager.MatchRules;
            if (_characterDatas == null || _characterDatas.Length == 0) _characterDatas = assetManager.CharacterDatas;
            if (_botBehavior == null) _botBehavior = assetManager.BotBehavior;
        }

        public void StartMatch()
        {
            _currentRound = 0;
            _playerRoundsWon = 0;
            _enemyRoundsWon = 0;
            _roundHistory.Clear();
            _isMatchActive = true;
            _matchResult = MatchResult.None;
            MatchElapsed = 0f;

            _allChars = null;
            SpawnTeams();
            // Field drops (t48u): a destroyed cover drops for the team that broke it
            HillNGun.Combat.DropDirector.Begin(GameMode.PvP, () => _allChars ??= BuildAll(), () => _playerTeam.Count > _playerControlledIndex ? _playerTeam[_playerControlledIndex] : null);
            StartRound();
            Debug.Log($"[PvP] Match start: best of {MaxRounds} (first to {RoundsToWin}), round {RoundTimeLimit}s, crate={(_rules != null && _rules.supplyCrateEnabled)}");
        }

        /// <summary>Spawn per doc: each team on its middle row at x 6 / 12 / 18.</summary>
        public Vector2 GetSpawnPoint(Team team, int index)
        {
            float[] xs = _rules?.pvpSpawnX;
            float x = xs != null && xs.Length > 0 ? xs[Mathf.Clamp(index, 0, xs.Length - 1)] : 6f + 6f * index;
            int row = _rules != null ? _rules.pvpSpawnRowIndex : 1;
            var field = ServiceLocator.Get<Battlefield.BattlefieldSystem>()?.Data ?? ServiceLocator.Get<BattlefieldData>();
            float y;
            if (field != null)
                y = team == Team.Player ? field.GetOurRowY(Mathf.Clamp(row, 0, field.OurRowCount - 1))
                                        : field.GetEnemyRowY(Mathf.Clamp(row, 0, field.EnemyRowCount - 1));
            else
                y = team == Team.Player ? 4.5f : 13.5f;
            return new Vector2(x, y);
        }

        private int RowFor(Vector2 p, Team team)
        {
            var bf = ServiceLocator.Get<Battlefield.BattlefieldSystem>();
            return bf != null ? bf.GetNearestRowIndex(p.y, team) : 1;
        }

        private void SpawnTeams()
        {
            _playerTeam.Clear();
            _enemyTeam.Clear();
            _allyBotBrains.Clear();
            _enemyBotBrains.Clear();

            var flowManager = GameFlowManager.Instance;
            string[] playerCharIds = flowManager?.CurrentSession?.selectedCharacterIds ??
                new string[] { "buster", "raven", "volt" };

            for (int i = 0; i < 3; i++)
            {
                string charId = playerCharIds.Length > i && !string.IsNullOrEmpty(playerCharIds[i]) ? playerCharIds[i] : null;
                CharacterData charData = charId != null ? GetCharacterById(charId) : null;
                if (charData == null && _characterDatas != null && _characterDatas.Length > i) charData = _characterDatas[i];
                if (charData == null) continue;

                Vector2 p = GetSpawnPoint(Team.Player, i);
                var playerChar = CreateCharacter(HillNGun.Progression.Loadout.Resolve(charData), Team.Player, i, p, RowFor(p, Team.Player));
                _playerTeam.Add(playerChar);
                if (i != _playerControlledIndex)
                    _allyBotBrains.Add(new BotBrain(playerChar, _botBehavior) { ObjectiveProvider = CrateObjective });
            }

            for (int i = 0; i < 3; i++)
            {
                CharacterData charData = _characterDatas != null && _characterDatas.Length > i ? _characterDatas[i] : null;
                if (charData == null) continue;
                Vector2 p = GetSpawnPoint(Team.Enemy, i);
                var enemyChar = CreateCharacter(charData, Team.Enemy, i, p, RowFor(p, Team.Enemy));
                _enemyTeam.Add(enemyChar);
                _enemyBotBrains.Add(new BotBrain(enemyChar, _botBehavior) { ObjectiveProvider = CrateObjective });
            }

            SetBotAllies();

            var inputHandler = FindFirstObjectByType<TouchInputHandler>();
            if (inputHandler != null && _playerTeam.Count > _playerControlledIndex)
                inputHandler.SetReceiver(_playerTeam[_playerControlledIndex]);
            if (_playerTeam.Count > _playerControlledIndex)
                ServiceLocator.Get<MobileTouchControls>()?.SetReceiver(_playerTeam[_playerControlledIndex]);
        }

        private Vector2? CrateObjective() => _crate.CanBeHit ? _crate.Position : (Vector2?)null;

        /// <summary>Let the AI drive the player's own character (auto-battle / smoke test).</summary>
        public void SetPlayerAutopilot(bool on)
        {
            if (on && _playerAutoBrain == null && _playerTeam.Count > _playerControlledIndex)
            {
                _playerAutoBrain = new BotBrain(_playerTeam[_playerControlledIndex], _botBehavior) { ObjectiveProvider = CrateObjective };
                _playerAutoBrain.SetAllies(_playerTeam.ToArray());
            }
            else if (!on) _playerAutoBrain = null;
        }

        private void SetBotAllies()
        {
            CharacterStateMachine[] playerTeamArray = _playerTeam.ToArray();
            CharacterStateMachine[] enemyTeamArray = _enemyTeam.ToArray();
            foreach (var bot in _allyBotBrains) bot.SetAllies(playerTeamArray);
            foreach (var bot in _enemyBotBrains) bot.SetAllies(enemyTeamArray);
        }

        private CharacterStateMachine CreateCharacter(CharacterData data, Team team, int index, Vector2 position, int rowIndex)
        {
            GameObject go = new GameObject($"{team}_{data.id}_{index}");
            var character = go.AddComponent<CharacterStateMachine>();

            var viewGo = new GameObject("View");
            viewGo.transform.SetParent(go.transform);
            var view = CharacterViewFactory.Create(viewGo);

            character.Initialize(data, team, index, position, rowIndex);
            view.Initialize(character);
            character.SetView(view);

            character.OnDowned += HandleCharacterDowned;
            character.OnEliminated += HandleCharacterEliminated;
            character.OnFired += (c, dir, dmg) =>
                EffectsManager.Instance?.SpawnMuzzleFlashPattern(c.Position, dir, (c.Data?.weapon?.projectilesPerShot ?? 1) > 1, c.Data?.weapon?.muzzleForwardOffset ?? 1f, c.Data?.weapon?.vfx);
            character.OnDamageTaken += (c, dmg, dir) =>
            {
                if (!_isRoundActive) return;
                if (c.Team == Team.Enemy) TotalDamageToEnemyTeam += dmg; else TotalDamageToPlayerTeam += dmg;
                EffectsManager.Instance?.SpawnDamageNumber(c.Position, dmg, dmg >= 500);
            };

            _combat?.RegisterCharacter(character);
            ServiceLocator.Get<ReviveSystem>()?.RegisterCharacter(character);
            return character;
        }

        private CharacterData GetCharacterById(string id)
        {
            if (_characterDatas == null) return null;
            foreach (var data in _characterDatas)
                if (data != null && data.id == id) return data;
            return null;
        }

        private List<CharacterStateMachine> _allChars;
        private List<CharacterStateMachine> BuildAll()
        {
            var l = new List<CharacterStateMachine>(_playerTeam);
            l.AddRange(_enemyTeam);
            return l;
        }

        private void StartRound()
        {
            HillNGun.Combat.DropDirector.Instance?.ClearField();
            _currentPhase = PvPPhase.RoundStart;
            _delayTimer = _rules != null ? _rules.roundStartDelay : _roundStartDelay;
            _lastRoundResult = PvPRoundResult.InProgress;
            _lastRoundReason = RoundEndReason.None;
            _roundTimer = RoundTimeLimit;
            _roundElapsed = 0f;
            _crateDropped = false;
            if (_crate.IsActive) _combat?.UnregisterCombatTarget(_crate);
            _crate.Clear();
            _crateRewardPending = false;
            _pendingDowns.Clear();
            if (_combat != null) _combat.GlobalCoverDamageMul = 1f;

            ResetCharactersForRound();
            ResetCoversForRound(); // FP: 엄폐물은 라운드마다 새로
            _combat?.ProjectilePool?.Clear();

            Debug.Log($"[PvP] Round {_currentRound + 1} countdown (score {_playerRoundsWon}-{_enemyRoundsWon})");
            OnRoundStarted?.Invoke(_currentRound + 1);
        }

        private void ResetCharactersForRound()
        {
            for (int i = 0; i < _playerTeam.Count; i++)
            {
                Vector2 p = GetSpawnPoint(Team.Player, i);
                _playerTeam[i]?.ResetForRound(p, RowFor(p, Team.Player));
            }
            for (int i = 0; i < _enemyTeam.Count; i++)
            {
                Vector2 p = GetSpawnPoint(Team.Enemy, i);
                _enemyTeam[i]?.ResetForRound(p, RowFor(p, Team.Enemy));
            }
        }

        private void ResetCoversForRound()
        {
            if (_combat == null) return;
            foreach (var cover in _combat.Covers) cover.Reset();
        }

        private void Update()
        {
            if (!_isMatchActive) return;
            MatchElapsed += Time.deltaTime;

            switch (_currentPhase)
            {
                case PvPPhase.RoundStart:
                    _delayTimer -= Time.deltaTime;
                    if (_delayTimer <= 0f)
                    {
                        _currentPhase = PvPPhase.RoundActive;
                        _isRoundActive = true;
                        _roundTimer = RoundTimeLimit;
                        Debug.Log($"[PvP] Round {_currentRound + 1} FIGHT");
                        OnRoundFight?.Invoke(_currentRound + 1);
                    }
                    break;

                case PvPPhase.RoundActive:
                    UpdateRound();
                    break;

                case PvPPhase.RoundEnd:
                    _delayTimer -= Time.deltaTime;
                    if (_delayTimer <= 0f) ProcessNextRound();
                    break;
            }
        }

        private void UpdateRound()
        {
            _roundTimer -= Time.deltaTime;
            _roundElapsed += Time.deltaTime;
            if (_crateRewardPending) GrantCrate(_crate);
            UpdateLateRound();
            UpdateBots();
            CheckRoundEndConditions();
        }

        private void UpdateLateRound()
        {
            if (_rules == null || _crateDropped || _roundElapsed < _rules.supplyCrateTime) return;
            _crateDropped = true;
            if (_rules.lateCoverDecayEnabled && _combat != null)
            {
                _combat.GlobalCoverDamageMul = Mathf.Max(1f, _rules.lateCoverDamageMul);
                Debug.Log($"[PvP] Late round: cover damage x{_combat.GlobalCoverDamageMul}");
            }
            if (_rules.supplyCrateEnabled)
            {
                _crate.Drop(_rules.supplyCratePosition, _rules.supplyCrateHp, _rules.supplyCrateRadius);
                _combat?.RegisterCombatTarget(_crate);
                Debug.Log($"[PvP] Supply crate dropped at {_rules.supplyCratePosition}");
                OnSupplyCrateDropped?.Invoke(_crate);
            }
        }

        private Team _lastCrateHitTeam = Team.Player;

        private bool _crateRewardPending;

        private void HandleCrateBroken(SupplyCrate crate)
        {
            _crateRewardPending = true; // opener team is known right after, in OnCombatTargetHit
        }

        private void HandleCombatTargetHit(Projectile p, ICombatTarget ct, int dmg)
        {
            if (ct != _crate) return;
            if (p != null) _lastCrateHitTeam = p.OwnerTeam;
            if (_crateRewardPending) GrantCrate(_crate);
        }

        private void GrantCrate(SupplyCrate crate)
        {
            _crateRewardPending = false;
            _combat?.UnregisterCombatTarget(crate);
            Team team = _lastCrateHitTeam;
            var list = team == Team.Player ? _playerTeam : _enemyTeam;
            foreach (var c in list)
            {
                if (c == null || c.OverallState != CharacterOverallState.Active) continue;
                if (_rules == null || _rules.supplyCrateRefillsGrenades) c.GrenadeSystem?.Refill();
                if (_rules != null && _rules.supplyCrateHealRatio > 0f) c.Heal(_rules.supplyCrateHealRatio);
            }
            SupplyCratesOpened++;
            Debug.Log($"[PvP] Supply crate opened by {team}");
            OnSupplyCrateOpened?.Invoke(crate, team);
        }

        private void UpdateBots()
        {
            CharacterStateMachine[] playerChars = _playerTeam.ToArray();
            CharacterStateMachine[] enemyChars = _enemyTeam.ToArray();
            float dt = Time.deltaTime;
            foreach (var bot in _allyBotBrains) bot.Update(dt, enemyChars);
            _playerAutoBrain?.Update(dt, enemyChars);
            foreach (var bot in _enemyBotBrains) bot.Update(dt, playerChars);
        }

        /// <summary>Team can still fight: at least one Active member (decision D10: all-downed team loses).</summary>
        private static int CountActive(List<CharacterStateMachine> team)
        {
            int n = 0;
            foreach (var c in team) if (c != null && c.OverallState == CharacterOverallState.Active) n++;
            return n;
        }

        /// <summary>Alive for the timeout count: not eliminated (downed counts while the toggle is on).</summary>
        private int CountAlive(List<CharacterStateMachine> team)
        {
            bool downedAlive = _rules == null || _rules.downedCountsAsAlive;
            int n = 0;
            foreach (var c in team)
            {
                if (c == null) continue;
                if (c.OverallState == CharacterOverallState.Active) n++;
                else if (downedAlive && c.OverallState == CharacterOverallState.Downed) n++;
            }
            return n;
        }

        private static int HpSum(List<CharacterStateMachine> team)
        {
            int s = 0;
            foreach (var c in team) if (c != null && c.OverallState == CharacterOverallState.Active) s += c.CurrentHp;
            return s;
        }

        private void CheckRoundEndConditions()
        {
            int playerActive = CountActive(_playerTeam);
            int enemyActive = CountActive(_enemyTeam);

            if (playerActive == 0 && enemyActive == 0) { EndRound(PvPRoundResult.Draw, RoundEndReason.TeamWiped); return; }
            if (playerActive == 0) { EndRound(PvPRoundResult.EnemyWins, RoundEndReason.TeamWiped); return; }
            if (enemyActive == 0) { EndRound(PvPRoundResult.PlayerWins, RoundEndReason.TeamWiped); return; }
            if (_roundTimer > 0f) return;

            // FP 252-258: 살아남은 인원 → 같으면 남은 체력 합 → 같으면 무승부 (D11)
            int pa = CountAlive(_playerTeam), ea = CountAlive(_enemyTeam);
            if (pa != ea) { EndRound(pa > ea ? PvPRoundResult.PlayerWins : PvPRoundResult.EnemyWins, RoundEndReason.TimeoutAlive); return; }
            if (_rules == null || _rules.timeoutUseHpSum)
            {
                int ph = HpSum(_playerTeam), eh = HpSum(_enemyTeam);
                if (ph != eh) { EndRound(ph > eh ? PvPRoundResult.PlayerWins : PvPRoundResult.EnemyWins, RoundEndReason.TimeoutHp); return; }
            }
            EndRound(PvPRoundResult.Draw, RoundEndReason.TimeoutDraw);
        }

        private void EndRound(PvPRoundResult result, RoundEndReason reason)
        {
            _isRoundActive = false;
            _combat?.ProjectilePool?.Clear();
            _lastRoundResult = result;
            _lastRoundReason = reason;
            _roundHistory.Add(result);
            _currentPhase = PvPPhase.RoundEnd;
            _delayTimer = _rules != null ? _rules.roundEndDelay : _roundEndDelay;
            foreach (var c in _playerTeam) c?.OnMoveCommand(MoveCommand.Stop(Time.time));
            foreach (var c in _enemyTeam) c?.OnMoveCommand(MoveCommand.Stop(Time.time));

            if (result == PvPRoundResult.PlayerWins) _playerRoundsWon++;
            else if (result == PvPRoundResult.EnemyWins) _enemyRoundsWon++;

            var flowManager = GameFlowManager.Instance;
            if (result == PvPRoundResult.PlayerWins) flowManager?.IncrementPlayerRounds();
            else if (result == PvPRoundResult.EnemyWins) flowManager?.IncrementEnemyRounds();

            Debug.Log($"[PvP] Round {_currentRound + 1} end: {result} ({reason}) at {_roundElapsed:F1}s alive {CountAlive(_playerTeam)}-{CountAlive(_enemyTeam)} hp {HpSum(_playerTeam)}-{HpSum(_enemyTeam)} score {_playerRoundsWon}-{_enemyRoundsWon}");
            OnRoundEnded?.Invoke(result);
        }

        private void ProcessNextRound()
        {
            int need = RoundsToWin;
            if (_playerRoundsWon >= need) { EndMatch(MatchResult.Victory); return; }
            if (_enemyRoundsWon >= need) { EndMatch(MatchResult.Defeat); return; }
            if (_currentRound + 1 >= MaxRounds)
            {
                // D11: rounds won → total damage dealt → draw
                MatchResult r;
                if (_playerRoundsWon != _enemyRoundsWon) r = _playerRoundsWon > _enemyRoundsWon ? MatchResult.Victory : MatchResult.Defeat;
                else if ((_rules == null || _rules.matchTiebreakByDamage) && TotalDamageToEnemyTeam != TotalDamageToPlayerTeam)
                    r = TotalDamageToEnemyTeam > TotalDamageToPlayerTeam ? MatchResult.Victory : MatchResult.Defeat;
                else r = MatchResult.Draw;
                EndMatch(r);
                return;
            }
            _currentRound++;
            StartRound();
        }

        private void EndMatch(MatchResult result)
        {
            _isMatchActive = false;
            _matchResult = result;
            _currentPhase = PvPPhase.MatchEnd;
            Debug.Log($"[PvP] Match end: {result} rounds {_playerRoundsWon}-{_enemyRoundsWon} dmg {TotalDamageToEnemyTeam}-{TotalDamageToPlayerTeam} time {MatchElapsed:F1}s");
            OnMatchEnded?.Invoke(result);

            var flowManager = GameFlowManager.Instance;
            var session = flowManager?.CurrentSession;
            if (session != null)
            {
                session.matchTime = MatchElapsed;
                session.damageDealt = TotalDamageToEnemyTeam;
                session.damageTaken = TotalDamageToPlayerTeam;
                session.playerKills = PlayerKills;
                session.playerDeaths = PlayerDeaths;
                session.coversDestroyed = CoversDestroyed;
                session.playerRoundsWon = _playerRoundsWon;
                session.enemyRoundsWon = _enemyRoundsWon;
                session.roundHistory.Clear();
                foreach (var r in _roundHistory) session.roundHistory.Add((int)r);
            }
            flowManager?.EndMatch(result);
        }

        private void HandleCharacterHit(Projectile p, CharacterStateMachine target, int dmg)
        {
            if (p != null && target != null && _pendingDowns.Remove(target))
            {
                if (p.Owner != null && p.Owner == GetPlayerCharacter(_playerControlledIndex) && target.Team == Team.Enemy)
                    PlayerKills++;
            }
        }

        private void HandleCoverDestroyed(CoverInstance cover)
        {
            if (_isRoundActive) CoversDestroyed++;
        }

        private void HandleCharacterDowned(CharacterStateMachine character)
        {
            _pendingDowns.Add(character);
            OnCharacterDowned?.Invoke(character);
        }

        private void HandleCharacterEliminated(CharacterStateMachine character)
        {
            if (character == GetPlayerCharacter(_playerControlledIndex)) PlayerDeaths++;
            OnCharacterEliminated?.Invoke(character);
        }

        public CharacterStateMachine GetPlayerCharacter(int index)
        {
            return index >= 0 && index < _playerTeam.Count ? _playerTeam[index] : null;
        }
    }
}

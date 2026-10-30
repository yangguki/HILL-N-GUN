using UnityEngine;
using System.Collections.Generic;
using HillNGun.Data;
using HillNGun.Core;
using HillNGun.Characters;
using HillNGun.AI;
using HillNGun.Cover;

namespace HillNGun.PvP
{
    public class PvPMatchManager : MonoBehaviour
    {
        [Header("Match Settings")]
        [SerializeField] private int _roundsToWin = 2;
        [SerializeField] private int _maxRounds = 3;
        [SerializeField] private float _roundTimeLimit = 80f;

        [Header("Spawn Points")]
        [SerializeField] private Vector2[] _playerSpawnPoints = new Vector2[]
        {
            new Vector2(6f, 2f),
            new Vector2(12f, 4.5f),
            new Vector2(18f, 2f)
        };
        [SerializeField] private Vector2[] _enemySpawnPoints = new Vector2[]
        {
            new Vector2(6f, 16f),
            new Vector2(12f, 13.5f),
            new Vector2(18f, 16f)
        };

        [Header("Character Data")]
        [SerializeField] private CharacterData[] _characterDatas;

        [Header("Bot Settings")]
        [SerializeField] private BotBehaviorData _botBehavior;

        private List<CharacterStateMachine> _playerTeam = new List<CharacterStateMachine>();
        private List<CharacterStateMachine> _enemyTeam = new List<CharacterStateMachine>();
        private List<BotBrain> _allyBotBrains = new List<BotBrain>();
        private List<BotBrain> _enemyBotBrains = new List<BotBrain>();
        private int _playerControlledIndex = 0;

        private int _currentRound = 0;
        private int _playerRoundsWon = 0;
        private int _enemyRoundsWon = 0;
        private float _roundTimer;
        private bool _isMatchActive;
        private bool _isRoundActive;
        private PvPRoundResult _lastRoundResult = PvPRoundResult.InProgress;
        private MatchResult _matchResult = MatchResult.None;

        private float _roundStartDelay = 2f;
        private float _roundEndDelay = 2f;
        private float _delayTimer;
        private PvPPhase _currentPhase = PvPPhase.PreMatch;

        public int CurrentRound => _currentRound + 1;
        public int PlayerRoundsWon => _playerRoundsWon;
        public int EnemyRoundsWon => _enemyRoundsWon;
        public float RoundTimer => _roundTimer;
        public bool IsMatchActive => _isMatchActive;
        public bool IsRoundActive => _isRoundActive;
        public PvPRoundResult LastRoundResult => _lastRoundResult;
        public MatchResult MatchResult => _matchResult;
        public PvPPhase CurrentPhase => _currentPhase;
        public IReadOnlyList<CharacterStateMachine> PlayerTeam => _playerTeam;
        public IReadOnlyList<CharacterStateMachine> EnemyTeam => _enemyTeam;

        public event System.Action<int> OnRoundStarted;
        public event System.Action<PvPRoundResult> OnRoundEnded;
        public event System.Action<MatchResult> OnMatchEnded;
        public event System.Action<CharacterStateMachine> OnCharacterDowned;
        public event System.Action<CharacterStateMachine> OnCharacterEliminated;

        private enum PvPPhase
        {
            PreMatch,
            RoundStart,
            RoundActive,
            RoundEnd,
            MatchEnd
        }

        private void Start()
        {
            StartMatch();
        }

        public void StartMatch()
        {
            _currentRound = 0;
            _playerRoundsWon = 0;
            _enemyRoundsWon = 0;
            _isMatchActive = true;
            _matchResult = MatchResult.None;

            SpawnTeams();
            StartRound();
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
                string charId = playerCharIds.Length > i ? playerCharIds[i] : "buster";
                CharacterData charData = GetCharacterById(charId);
                if (charData == null && _characterDatas != null && _characterDatas.Length > i)
                {
                    charData = _characterDatas[i];
                }

                if (charData != null)
                {
                    var playerChar = CreateCharacter(charData, Team.Player, i, _playerSpawnPoints[i], i);
                    _playerTeam.Add(playerChar);

                    if (i != _playerControlledIndex)
                    {
                        var allyBot = new BotBrain(playerChar, _botBehavior);
                        _allyBotBrains.Add(allyBot);
                    }
                }
            }

            for (int i = 0; i < 3; i++)
            {
                CharacterData charData = _characterDatas != null && _characterDatas.Length > i 
                    ? _characterDatas[i] 
                    : null;

                if (charData != null)
                {
                    var enemyChar = CreateCharacter(charData, Team.Enemy, i, _enemySpawnPoints[i], i);
                    _enemyTeam.Add(enemyChar);

                    var enemyBot = new BotBrain(enemyChar, _botBehavior);
                    _enemyBotBrains.Add(enemyBot);
                }
            }

            SetBotAllies();
        }

        private void SetBotAllies()
        {
            CharacterStateMachine[] playerTeamArray = _playerTeam.ToArray();
            CharacterStateMachine[] enemyTeamArray = _enemyTeam.ToArray();

            foreach (var bot in _allyBotBrains)
            {
                bot.SetAllies(playerTeamArray);
            }
            foreach (var bot in _enemyBotBrains)
            {
                bot.SetAllies(enemyTeamArray);
            }
        }

        private CharacterStateMachine CreateCharacter(CharacterData data, Team team, int index, Vector2 position, int rowIndex)
        {
            GameObject go = new GameObject($"{team}_{data.id}_{index}");
            var character = go.AddComponent<CharacterStateMachine>();

            var viewGo = new GameObject("View");
            viewGo.transform.SetParent(go.transform);
            var view = viewGo.AddComponent<PlaceholderCharacterView>();

            character.Initialize(data, team, index, position, rowIndex);
            view.Initialize(character);
            character.SetView(view);

            character.OnDowned += HandleCharacterDowned;
            character.OnEliminated += HandleCharacterEliminated;

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

        private void StartRound()
        {
            _currentPhase = PvPPhase.RoundStart;
            _delayTimer = _roundStartDelay;
            _lastRoundResult = PvPRoundResult.InProgress;

            ResetCharactersForRound();
            ResetCoversForRound();

            OnRoundStarted?.Invoke(_currentRound + 1);
        }

        private void ResetCharactersForRound()
        {
            for (int i = 0; i < _playerTeam.Count; i++)
            {
                var character = _playerTeam[i];
                if (character != null)
                {
                    character.ResetForRound(_playerSpawnPoints[i], i);
                }
            }

            for (int i = 0; i < _enemyTeam.Count; i++)
            {
                var character = _enemyTeam[i];
                if (character != null)
                {
                    character.ResetForRound(_enemySpawnPoints[i], i);
                }
            }
        }

        private void ResetCoversForRound()
        {
            var covers = FindObjectsByType<CoverInstance>(FindObjectsSortMode.None);
        }

        private void Update()
        {
            if (!_isMatchActive) return;

            switch (_currentPhase)
            {
                case PvPPhase.RoundStart:
                    _delayTimer -= Time.deltaTime;
                    if (_delayTimer <= 0f)
                    {
                        _currentPhase = PvPPhase.RoundActive;
                        _isRoundActive = true;
                        _roundTimer = _roundTimeLimit;
                    }
                    break;

                case PvPPhase.RoundActive:
                    UpdateRound();
                    break;

                case PvPPhase.RoundEnd:
                    _delayTimer -= Time.deltaTime;
                    if (_delayTimer <= 0f)
                    {
                        ProcessNextRound();
                    }
                    break;

                case PvPPhase.MatchEnd:
                    break;
            }
        }

        private void UpdateRound()
        {
            _roundTimer -= Time.deltaTime;

            UpdateBots();

            CheckRoundEndConditions();
        }

        private void UpdateBots()
        {
            CharacterStateMachine[] playerChars = _playerTeam.ToArray();
            CharacterStateMachine[] enemyChars = _enemyTeam.ToArray();

            foreach (var bot in _allyBotBrains)
            {
                bot.Update(Time.deltaTime, enemyChars);
            }

            foreach (var bot in _enemyBotBrains)
            {
                bot.Update(Time.deltaTime, playerChars);
            }
        }

        private void CheckRoundEndConditions()
        {
            int playerAlive = CountAlive(_playerTeam);
            int enemyAlive = CountAlive(_enemyTeam);

            if (playerAlive == 0 && enemyAlive == 0)
            {
                EndRound(PvPRoundResult.Draw);
            }
            else if (playerAlive == 0)
            {
                EndRound(PvPRoundResult.EnemyWins);
            }
            else if (enemyAlive == 0)
            {
                EndRound(PvPRoundResult.PlayerWins);
            }
            else if (_roundTimer <= 0f)
            {
                if (playerAlive > enemyAlive)
                {
                    EndRound(PvPRoundResult.PlayerWins);
                }
                else if (enemyAlive > playerAlive)
                {
                    EndRound(PvPRoundResult.EnemyWins);
                }
                else
                {
                    EndRound(PvPRoundResult.Draw);
                }
            }
        }

        private int CountAlive(List<CharacterStateMachine> team)
        {
            int count = 0;
            foreach (var character in team)
            {
                if (character != null && character.OverallState == CharacterOverallState.Active)
                {
                    count++;
                }
            }
            return count;
        }

        private void EndRound(PvPRoundResult result)
        {
            _isRoundActive = false;
            _lastRoundResult = result;
            _currentPhase = PvPPhase.RoundEnd;
            _delayTimer = _roundEndDelay;

            switch (result)
            {
                case PvPRoundResult.PlayerWins:
                    _playerRoundsWon++;
                    break;
                case PvPRoundResult.EnemyWins:
                    _enemyRoundsWon++;
                    break;
            }

            var flowManager = GameFlowManager.Instance;
            if (result == PvPRoundResult.PlayerWins)
            {
                flowManager?.IncrementPlayerRounds();
            }
            else if (result == PvPRoundResult.EnemyWins)
            {
                flowManager?.IncrementEnemyRounds();
            }

            OnRoundEnded?.Invoke(result);
        }

        private void ProcessNextRound()
        {
            if (_playerRoundsWon >= _roundsToWin)
            {
                EndMatch(MatchResult.Victory);
            }
            else if (_enemyRoundsWon >= _roundsToWin)
            {
                EndMatch(MatchResult.Defeat);
            }
            else if (_currentRound + 1 >= _maxRounds)
            {
                MatchResult result = _playerRoundsWon > _enemyRoundsWon 
                    ? MatchResult.Victory 
                    : MatchResult.Defeat;
                EndMatch(result);
            }
            else
            {
                _currentRound++;
                StartRound();
            }
        }

        private void EndMatch(MatchResult result)
        {
            _isMatchActive = false;
            _matchResult = result;
            _currentPhase = PvPPhase.MatchEnd;

            OnMatchEnded?.Invoke(result);

            var flowManager = GameFlowManager.Instance;
            flowManager?.EndMatch(result);
        }

        private void HandleCharacterDowned(CharacterStateMachine character)
        {
            OnCharacterDowned?.Invoke(character);
        }

        private void HandleCharacterEliminated(CharacterStateMachine character)
        {
            OnCharacterEliminated?.Invoke(character);
        }

        public CharacterStateMachine GetPlayerCharacter(int index)
        {
            if (index >= 0 && index < _playerTeam.Count)
            {
                return _playerTeam[index];
            }
            return null;
        }
    }
}

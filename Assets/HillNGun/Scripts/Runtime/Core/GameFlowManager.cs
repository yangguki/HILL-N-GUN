using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HillNGun.Core
{
    public class GameFlowManager : MonoBehaviour
    {
        private static GameFlowManager _instance;
        public static GameFlowManager Instance => _instance;

        [SerializeField] private float _fadeTime = 0.3f;

        private GameSession _currentSession;
        public GameSession CurrentSession => _currentSession;

        private GamePhase _currentPhase = GamePhase.None;
        public GamePhase CurrentPhase => _currentPhase;

        private bool _isTransitioning;
        private float _fadeAlpha;

        public const string SCENE_TITLE = "Title";
        public const string SCENE_MODE_SELECT = "ModeSelect";
        public const string SCENE_CHARACTER_SELECT = "CharacterSelect";
        public const string SCENE_PVE_MATCH = "PvE_Match";
        public const string SCENE_PVP_MATCH = "PvP_Match";
        public const string SCENE_PRACTICE = "M1_Sandbox";
        public const string SCENE_RESULT = "Result";

        public event Action<GamePhase> OnPhaseChanged;
        public event Action<string> OnSceneLoading;
        public event Action<string> OnSceneLoaded;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _currentSession = new GameSession();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ServiceLocator.Unregister<GameFlowManager>();
                _instance = null;
            }
        }

        public void GoToTitle()
        {
            _currentSession.Reset();
            LoadScene(SCENE_TITLE, GamePhase.Title);
        }

        public void GoToModeSelect()
        {
            LoadScene(SCENE_MODE_SELECT, GamePhase.ModeSelect);
        }

        public void GoToCharacterSelect(GameMode mode)
        {
            _currentSession.mode = mode;
            LoadScene(SCENE_CHARACTER_SELECT, GamePhase.CharacterSelect);
        }

        public void StartMatch()
        {
            if (_currentSession.rewardsGranted || _currentSession.result != MatchResult.None)
                _currentSession.ResetMatchProgress();
            string sceneName = _currentSession.mode switch
            {
                GameMode.PvE => SCENE_PVE_MATCH,
                GameMode.PvP => SCENE_PVP_MATCH,
                GameMode.Practice => SCENE_PRACTICE,
                _ => SCENE_PRACTICE
            };
            HillNGun.External.ExternalServices.Log("match_start", new Dictionary<string, object>
                { { "mode", _currentSession.mode }, { "start_stage", _currentSession.startStageIndex }, { "chars", string.Join("|", _currentSession.selectedCharacterIds ?? new string[0]) } });
            LoadScene(sceneName, GamePhase.Match);
        }

        /// <summary>Hook for granting rewards before the Result scene (set by the progression layer).</summary>
        public static Func<GameSession, object> RewardGranter;

        public void EndMatch(MatchResult result)
        {
            _currentSession.result = result;
            HillNGun.External.ExternalServices.Log("match_end", new Dictionary<string, object>
                { { "mode", _currentSession.mode }, { "result", result }, { "time", _currentSession.matchTime.ToString("F1") }, { "kills", _currentSession.playerKills } });
            if (!_currentSession.rewardsGranted && RewardGranter != null)
            {
                try { _currentSession.rewards = RewardGranter(_currentSession); }
                catch (Exception e) { Debug.LogError($"[GameFlow] Reward grant failed: {e}"); }
                _currentSession.rewardsGranted = true;
            }
            LoadScene(SCENE_RESULT, GamePhase.Result);
        }

        public void RestartMatch()
        {
            _currentSession.ResetMatchProgress();
            StartMatch();
        }

        public void SetCharacterSelection(int slot, string characterId)
        {
            _currentSession.SetCharacter(slot, characterId);
        }

        public void SetAllCharacters(string char1, string char2, string char3)
        {
            _currentSession.selectedCharacterIds[0] = char1;
            _currentSession.selectedCharacterIds[1] = char2;
            _currentSession.selectedCharacterIds[2] = char3;
        }

        private void LoadScene(string sceneName, GamePhase newPhase)
        {
            if (_isTransitioning) return;
            StartCoroutine(LoadSceneRoutine(sceneName, newPhase));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, GamePhase newPhase)
        {
            _isTransitioning = true;
            OnSceneLoading?.Invoke(sceneName);

            float t = 0f;
            while (t < _fadeTime)
            {
                t += Time.unscaledDeltaTime;
                _fadeAlpha = Mathf.Clamp01(t / _fadeTime);
                yield return null;
            }
            _fadeAlpha = 1f;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op != null)
            {
                while (!op.isDone)
                {
                    yield return null;
                }
            }

            _currentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
            OnSceneLoaded?.Invoke(sceneName);

            t = 0f;
            while (t < _fadeTime)
            {
                t += Time.unscaledDeltaTime;
                _fadeAlpha = 1f - Mathf.Clamp01(t / _fadeTime);
                yield return null;
            }
            _fadeAlpha = 0f;

            _isTransitioning = false;
        }

        private void OnGUI()
        {
            if (_fadeAlpha > 0.001f)
            {
                GUI.color = new Color(0f, 0f, 0f, _fadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        public void UpdateSessionStats(int kills, int deaths, int damageDealt, int damageTaken, float time)
        {
            _currentSession.playerKills += kills;
            _currentSession.playerDeaths += deaths;
            _currentSession.damageDealt += damageDealt;
            _currentSession.damageTaken += damageTaken;
            _currentSession.matchTime += time;
        }

        public void IncrementPlayerRounds()
        {
            _currentSession.playerRoundsWon++;
        }

        public void IncrementEnemyRounds()
        {
            _currentSession.enemyRoundsWon++;
        }

        public void SetPvEProgress(int gauge, int gaugeMax, int wave, int totalWaves)
        {
            _currentSession.pveGauge = gauge;
            _currentSession.pveGaugeMax = gaugeMax;
            _currentSession.currentWave = wave;
            _currentSession.totalWaves = totalWaves;
        }

        public void IncrementEnemiesDefeated()
        {
            _currentSession.enemiesDefeated++;
        }

        public void IncrementCoversDestroyed()
        {
            _currentSession.coversDestroyed++;
        }
    }
}

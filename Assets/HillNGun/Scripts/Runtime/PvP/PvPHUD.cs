using UnityEngine;
using HillNGun.Core;
using HillNGun.UI;
using HillNGun.Characters;

namespace HillNGun.PvP
{
    public class PvPHUD : MonoBehaviour
    {
        private PvPMatchManager _matchManager;
        private LocalizationManager _localization;

        private GUIStyle _scoreStyle;
        private GUIStyle _timerStyle;
        private GUIStyle _roundStyle;
        private GUIStyle _teamLabelStyle;
        private GUIStyle _playerNameStyle;
        private bool _stylesInitialized;

        private float _roundMessageTimer;
        private string _roundMessage;

        private void Start()
        {
            _matchManager = FindFirstObjectByType<PvPMatchManager>();
            _localization = ServiceLocator.Get<LocalizationManager>();

            if (_matchManager != null)
            {
                _matchManager.OnRoundStarted += OnRoundStarted;
                _matchManager.OnRoundEnded += OnRoundEnded;
            }
        }

        private void OnDestroy()
        {
            if (_matchManager != null)
            {
                _matchManager.OnRoundStarted -= OnRoundStarted;
                _matchManager.OnRoundEnded -= OnRoundEnded;
            }
        }

        private void OnRoundStarted(int round)
        {
            string roundLabel = GetLocalizedString("pvp.ui.round", "라운드");
            _roundMessage = $"{roundLabel} {round}";
            _roundMessageTimer = 2f;
        }

        private void OnRoundEnded(PvPRoundResult result)
        {
            _roundMessage = result switch
            {
                PvPRoundResult.PlayerWins => GetLocalizedString("pvp.ui.round_win", "라운드 승리"),
                PvPRoundResult.EnemyWins => GetLocalizedString("pvp.ui.round_lose", "라운드 패배"),
                PvPRoundResult.Draw => GetLocalizedString("pvp.ui.round_draw", "무승부"),
                _ => ""
            };
            _roundMessageTimer = 2f;
        }

        private void Update()
        {
            if (_roundMessageTimer > 0f)
            {
                _roundMessageTimer -= Time.deltaTime;
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _scoreStyle = new GUIStyle(GUI.skin.label);
            _scoreStyle.fontSize = 48;
            _scoreStyle.fontStyle = FontStyle.Bold;
            _scoreStyle.alignment = TextAnchor.MiddleCenter;
            _scoreStyle.normal.textColor = Color.white;

            _timerStyle = new GUIStyle(GUI.skin.label);
            _timerStyle.fontSize = 32;
            _timerStyle.fontStyle = FontStyle.Bold;
            _timerStyle.alignment = TextAnchor.MiddleCenter;
            _timerStyle.normal.textColor = new Color(1f, 0.9f, 0.7f);

            _roundStyle = new GUIStyle(GUI.skin.label);
            _roundStyle.fontSize = 36;
            _roundStyle.fontStyle = FontStyle.Bold;
            _roundStyle.alignment = TextAnchor.MiddleCenter;
            _roundStyle.normal.textColor = new Color(1f, 0.85f, 0.5f);

            _teamLabelStyle = new GUIStyle(GUI.skin.label);
            _teamLabelStyle.fontSize = 18;
            _teamLabelStyle.alignment = TextAnchor.MiddleCenter;
            _teamLabelStyle.normal.textColor = new Color(0.7f, 0.65f, 0.55f);

            _playerNameStyle = new GUIStyle(GUI.skin.label);
            _playerNameStyle.fontSize = 14;
            _playerNameStyle.alignment = TextAnchor.MiddleLeft;
            _playerNameStyle.normal.textColor = new Color(0.9f, 0.85f, 0.75f);

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (_matchManager == null) return;

            InitStyles();

            DrawScoreboard();
            DrawTimer();
            DrawTeamStatus();

            if (_roundMessageTimer > 0f)
            {
                DrawRoundMessage();
            }
        }

        private void DrawScoreboard()
        {
            float centerX = Screen.width / 2f;
            float y = 15f;

            Color playerColor = new Color(0.4f, 0.6f, 0.8f);
            Color enemyColor = new Color(0.8f, 0.4f, 0.4f);

            GUIStyle playerStyle = new GUIStyle(_scoreStyle);
            playerStyle.normal.textColor = playerColor;
            GUI.Label(new Rect(centerX - 100f, y, 60f, 60f), _matchManager.PlayerRoundsWon.ToString(), playerStyle);

            GUI.Label(new Rect(centerX - 20f, y, 40f, 60f), "-", _scoreStyle);

            GUIStyle enemyStyle = new GUIStyle(_scoreStyle);
            enemyStyle.normal.textColor = enemyColor;
            GUI.Label(new Rect(centerX + 40f, y, 60f, 60f), _matchManager.EnemyRoundsWon.ToString(), enemyStyle);

            string roundText = $"{GetLocalizedString("pvp.ui.round", "라운드")} {_matchManager.CurrentRound}";
            _teamLabelStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(centerX - 100f, y + 55f, 200f, 25f), roundText, _teamLabelStyle);
        }

        private void DrawTimer()
        {
            float centerX = Screen.width / 2f;
            float y = 100f;

            int seconds = Mathf.CeilToInt(_matchManager.RoundTimer);
            seconds = Mathf.Max(0, seconds);

            Color timerColor = seconds <= 10 
                ? new Color(1f, 0.4f, 0.3f) 
                : new Color(1f, 0.9f, 0.7f);

            GUIStyle style = new GUIStyle(_timerStyle);
            style.normal.textColor = timerColor;

            string timeText = seconds.ToString();
            GUI.Label(new Rect(centerX - 50f, y, 100f, 40f), timeText, style);
        }

        private void DrawTeamStatus()
        {
            DrawTeamPanel(_matchManager.PlayerTeam, true, 20f);
            DrawTeamPanel(_matchManager.EnemyTeam, false, Screen.width - 170f);
        }

        private void DrawTeamPanel(System.Collections.Generic.IReadOnlyList<CharacterStateMachine> team, bool isPlayer, float x)
        {
            float y = 150f;
            float panelWidth = 150f;
            float panelHeight = 120f;

            Color bgColor = isPlayer 
                ? new Color(0.2f, 0.25f, 0.35f, 0.8f)
                : new Color(0.35f, 0.2f, 0.2f, 0.8f);

            GUI.color = bgColor;
            GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string teamLabel = isPlayer ? "아군" : "적군";
            GUI.Label(new Rect(x, y + 5f, panelWidth, 20f), teamLabel, _teamLabelStyle);

            float entryY = y + 30f;
            foreach (var character in team)
            {
                if (character == null) continue;

                string charName = character.Data != null 
                    ? GetLocalizedString(character.Data.nameKey, character.Data.id)
                    : "???";

                bool isAlive = character.OverallState == CharacterOverallState.Active;
                bool isDowned = character.OverallState == CharacterOverallState.Downed ||
                               character.OverallState == CharacterOverallState.BeingRevived;

                GUIStyle style = new GUIStyle(_playerNameStyle);
                if (!isAlive && !isDowned)
                {
                    style.normal.textColor = new Color(0.5f, 0.4f, 0.4f);
                    charName = $"✗ {charName}";
                }
                else if (isDowned)
                {
                    style.normal.textColor = new Color(0.9f, 0.6f, 0.3f);
                    charName = $"★ {charName}";
                }
                else
                {
                    charName = $"● {charName}";
                }

                GUI.Label(new Rect(x + 10f, entryY, panelWidth - 20f, 20f), charName, style);
                entryY += 25f;
            }
        }

        private void DrawRoundMessage()
        {
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            float alpha = Mathf.Clamp01(_roundMessageTimer);
            Color textColor = new Color(1f, 0.9f, 0.6f, alpha);

            GUIStyle style = new GUIStyle(_roundStyle);
            style.normal.textColor = textColor;

            float scale = 1f + (1f - alpha) * 0.5f;
            style.fontSize = Mathf.RoundToInt(36f * scale);

            GUI.Label(new Rect(centerX - 200f, centerY - 50f, 400f, 100f), _roundMessage, style);
        }

        private string GetLocalizedString(string key, string fallback)
        {
            if (_localization != null && _localization.HasString(key))
            {
                return _localization.GetString(key);
            }
            return fallback;
        }
    }
}

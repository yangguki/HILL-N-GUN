using UnityEngine;
using HillNGun.Core;

namespace HillNGun.UI
{
    public class ResultUI : MonoBehaviour
    {
        private GUIStyle _resultTitleStyle;
        private GUIStyle _statsLabelStyle;
        private GUIStyle _statsValueStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;

        private LocalizationManager _localization;
        private GameSession _session;
        private float _animTime;

        private void Start()
        {
            _localization = ServiceLocator.Get<LocalizationManager>();
            _session = GameFlowManager.Instance?.CurrentSession;
        }

        private void Update()
        {
            _animTime += Time.deltaTime;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _resultTitleStyle = new GUIStyle(GUI.skin.label);
            _resultTitleStyle.fontSize = 64;
            _resultTitleStyle.fontStyle = FontStyle.Bold;
            _resultTitleStyle.alignment = TextAnchor.MiddleCenter;

            _statsLabelStyle = new GUIStyle(GUI.skin.label);
            _statsLabelStyle.fontSize = 22;
            _statsLabelStyle.alignment = TextAnchor.MiddleLeft;
            _statsLabelStyle.normal.textColor = new Color(0.8f, 0.75f, 0.65f);

            _statsValueStyle = new GUIStyle(GUI.skin.label);
            _statsValueStyle.fontSize = 22;
            _statsValueStyle.fontStyle = FontStyle.Bold;
            _statsValueStyle.alignment = TextAnchor.MiddleRight;
            _statsValueStyle.normal.textColor = new Color(1f, 0.95f, 0.85f);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 24;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = new Color(1f, 0.9f, 0.6f);
            _buttonStyle.normal.background = MakeTexture(1, 1, new Color(0.4f, 0.32f, 0.22f));
            _buttonStyle.hover.background = MakeTexture(1, 1, new Color(0.55f, 0.45f, 0.32f));
            _buttonStyle.padding = new RectOffset(25, 25, 15, 15);

            _stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            InitStyles();

            DrawBackground();
            DrawResultTitle();
            DrawStats();
            DrawButtons();
        }

        private void DrawBackground()
        {
            Color bgColor = new Color(0.12f, 0.1f, 0.08f);
            GUI.color = bgColor;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawResultTitle()
        {
            if (_session == null) return;

            string titleText;
            Color titleColor;

            if (_session.result == MatchResult.Victory)
            {
                titleText = GetLocalizedString("result.victory", "승리!");
                titleColor = new Color(1f, 0.85f, 0.3f);
            }
            else
            {
                titleText = GetLocalizedString("result.defeat", "패배");
                titleColor = new Color(0.7f, 0.3f, 0.3f);
            }

            _resultTitleStyle.normal.textColor = titleColor;

            float pulse = 1f + Mathf.Sin(_animTime * 2f) * 0.05f;
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(pulse, pulse), new Vector2(Screen.width / 2f, Screen.height * 0.15f));
            GUI.Label(new Rect(0, Screen.height * 0.08f, Screen.width, 100f), titleText, _resultTitleStyle);
            GUI.matrix = matrix;

            string modeText = _session.mode switch
            {
                GameMode.PvE => GetLocalizedString("mode.pve.title", "PvE 스토리"),
                GameMode.PvP => GetLocalizedString("mode.pvp.title", "PvP 대전"),
                GameMode.Practice => GetLocalizedString("mode.practice.title", "연습장"),
                _ => ""
            };

            GUIStyle modeStyle = new GUIStyle(_statsLabelStyle);
            modeStyle.alignment = TextAnchor.MiddleCenter;
            modeStyle.fontSize = 24;
            GUI.Label(new Rect(0, Screen.height * 0.2f, Screen.width, 40f), modeText, modeStyle);
        }

        private void DrawStats()
        {
            if (_session == null) return;

            float boxWidth = 500f;
            float boxHeight = 350f;
            float x = (Screen.width - boxWidth) / 2f;
            float y = Screen.height * 0.28f;

            GUI.color = new Color(0.18f, 0.15f, 0.12f);
            GUI.DrawTexture(new Rect(x, y, boxWidth, boxHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawBorder(x, y, boxWidth, boxHeight, new Color(0.4f, 0.35f, 0.28f), 2f);

            float labelX = x + 30f;
            float valueX = x + boxWidth - 30f;
            float lineY = y + 20f;
            float lineHeight = 38f;
            float labelWidth = boxWidth / 2f - 40f;
            float valueWidth = boxWidth / 2f - 40f;

            string timeText = FormatTime(_session.matchTime);
            DrawStatLine(labelX, lineY, labelWidth, valueX - labelWidth, valueWidth, 
                GetLocalizedString("stats.match_time", "전투 시간"), timeText);
            lineY += lineHeight;

            DrawStatLine(labelX, lineY, labelWidth, valueX - labelWidth, valueWidth,
                GetLocalizedString("stats.kills", "처치"), _session.playerKills.ToString());
            lineY += lineHeight;

            DrawStatLine(labelX, lineY, labelWidth, valueX - labelWidth, valueWidth,
                GetLocalizedString("stats.deaths", "사망"), _session.playerDeaths.ToString());
            lineY += lineHeight;

            DrawStatLine(labelX, lineY, labelWidth, valueX - labelWidth, valueWidth,
                GetLocalizedString("stats.damage_dealt", "가한 피해"), _session.damageDealt.ToString("N0"));
            lineY += lineHeight;

            DrawStatLine(labelX, lineY, labelWidth, valueX - labelWidth, valueWidth,
                GetLocalizedString("stats.damage_taken", "받은 피해"), _session.damageTaken.ToString("N0"));
            lineY += lineHeight;

            if (_session.mode == GameMode.PvE)
            {
                DrawStatLine(labelX, lineY, labelWidth, valueX - labelWidth, valueWidth,
                    GetLocalizedString("stats.enemies_defeated", "처치한 적"), _session.enemiesDefeated.ToString());
                lineY += lineHeight;
            }

            if (_session.mode == GameMode.PvP)
            {
                string roundsText = $"{_session.playerRoundsWon} - {_session.enemyRoundsWon}";
                DrawStatLine(labelX, lineY, labelWidth, valueX - labelWidth, valueWidth,
                    GetLocalizedString("stats.rounds", "라운드"), roundsText);
                lineY += lineHeight;
            }

            DrawStatLine(labelX, lineY, labelWidth, valueX - labelWidth, valueWidth,
                GetLocalizedString("stats.covers_destroyed", "파괴된 엄폐물"), _session.coversDestroyed.ToString());
        }

        private void DrawStatLine(float labelX, float y, float labelWidth, float valueX, float valueWidth, string label, string value)
        {
            GUI.Label(new Rect(labelX, y, labelWidth, 30f), label, _statsLabelStyle);
            GUI.Label(new Rect(valueX, y, valueWidth, 30f), value, _statsValueStyle);
        }

        private void DrawButtons()
        {
            float buttonWidth = 200f;
            float buttonHeight = 50f;
            float spacing = 30f;
            float totalWidth = buttonWidth * 3 + spacing * 2;
            float startX = (Screen.width - totalWidth) / 2f;
            float y = Screen.height - buttonHeight - 60f;

            string retryText = GetLocalizedString("ui.retry", "재도전");
            if (GUI.Button(new Rect(startX, y, buttonWidth, buttonHeight), retryText, _buttonStyle))
            {
                GameFlowManager.Instance?.RestartMatch();
            }

            string selectText = GetLocalizedString("ui.select_mode", "모드 선택");
            if (GUI.Button(new Rect(startX + buttonWidth + spacing, y, buttonWidth, buttonHeight), selectText, _buttonStyle))
            {
                GameFlowManager.Instance?.GoToModeSelect();
            }

            string titleText = GetLocalizedString("ui.to_title", "타이틀");
            if (GUI.Button(new Rect(startX + (buttonWidth + spacing) * 2, y, buttonWidth, buttonHeight), titleText, _buttonStyle))
            {
                GameFlowManager.Instance?.GoToTitle();
            }
        }

        private void DrawBorder(float x, float y, float width, float height, Color color, float thickness)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y + height - thickness, width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y, thickness, height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + width - thickness, y, thickness, height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{mins:00}:{secs:00}";
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

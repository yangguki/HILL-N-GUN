using UnityEngine;
using HillNGun.Core;
using HillNGun.Progression;

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
            DrawRewards();
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
            else if (_session.result == MatchResult.Draw)
            {
                titleText = GetLocalizedString("result.draw", "무승부");
                titleColor = new Color(0.85f, 0.8f, 0.65f);
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

            float boxWidth = 460f;
            float boxHeight = 380f;
            float x = Screen.width / 2f - boxWidth - 15f;
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

                if (_session.roundHistory != null && _session.roundHistory.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < _session.roundHistory.Count; i++)
                    {
                        if (i > 0) sb.Append("  ");
                        var rr = (PvPRoundResult)_session.roundHistory[i];
                        string k = rr == PvPRoundResult.PlayerWins ? GetLocalizedString("result.round_w", "승")
                                 : rr == PvPRoundResult.EnemyWins ? GetLocalizedString("result.round_l", "패")
                                 : GetLocalizedString("result.round_d", "무");
                        sb.Append($"{i + 1}R {k}");
                    }
                    DrawStatLine(labelX, lineY, labelWidth, valueX - labelWidth - 80f, valueWidth + 80f,
                        GetLocalizedString("stats.round_history", "라운드 기록"), sb.ToString());
                    lineY += lineHeight;
                }
            }

            DrawStatLine(labelX, lineY, labelWidth, valueX - labelWidth, valueWidth,
                GetLocalizedString("stats.covers_destroyed", "파괴된 엄폐물"), _session.coversDestroyed.ToString());
        }

        private void DrawRewards()
        {
            if (_session == null) return;
            var r = _session.rewards as RewardSummary;

            float boxWidth = 460f;
            float boxHeight = 380f;
            float x = Screen.width / 2f + 15f;
            float y = Screen.height * 0.28f;

            GUI.color = new Color(0.2f, 0.16f, 0.1f);
            GUI.DrawTexture(new Rect(x, y, boxWidth, boxHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawBorder(x, y, boxWidth, boxHeight, new Color(0.75f, 0.6f, 0.3f), 2f);

            var header = new GUIStyle(_statsValueStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 24 };
            header.normal.textColor = new Color(1f, 0.85f, 0.4f);
            GUI.Label(new Rect(x, y + 10f, boxWidth, 34f), GetLocalizedString("reward.title", "보상"), header);

            float lx = x + 30f, w = boxWidth - 60f, ly = y + 52f, lh = 34f;
            if (r == null)
            {
                GUI.Label(new Rect(lx, ly, w, 30f), GetLocalizedString("reward.none", "보상 없음"), _statsLabelStyle);
                return;
            }

            DrawStatLine(lx, ly, w * 0.55f, lx + w * 0.45f, w * 0.55f, GetLocalizedString("reward.bounty", "현상금"), $"+${r.bountyGained:N0}");
            ly += lh;
            DrawStatLine(lx, ly, w * 0.55f, lx + w * 0.45f, w * 0.55f, GetLocalizedString("reward.xp", "배지 경험치"), $"+{r.xpGained}");
            ly += lh;

            // sheriff badge bar
            string badge = string.Format(GetLocalizedString("reward.badge", "보안관 배지 Lv.{0}"), r.badgeLevelAfter);
            GUI.Label(new Rect(lx, ly, w, 28f), badge, _statsLabelStyle);
            ly += 30f;
            float ratio = r.badgeXpToNext > 0 ? Mathf.Clamp01((float)r.badgeXp / r.badgeXpToNext) : 1f;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(lx, ly, w, 14f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.78f, 0.3f);
            GUI.DrawTexture(new Rect(lx, ly, w * ratio, 14f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var small = new GUIStyle(_statsLabelStyle) { fontSize = 16, alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(lx, ly + 14f, w, 22f), $"{r.badgeXp} / {r.badgeXpToNext}", small);
            ly += 40f;

            var lineStyle = new GUIStyle(_statsLabelStyle) { fontSize = 19 };
            lineStyle.normal.textColor = new Color(0.6f, 1f, 0.7f);
            int shown = 0;
            foreach (var line in r.lines)
            {
                if (shown >= 4) break;
                GUI.Label(new Rect(lx, ly, w, 26f), "★ " + FormatRewardLine(line), lineStyle);
                ly += 27f;
                shown++;
            }
            if (r.lines.Count > shown)
            {
                GUI.Label(new Rect(lx, ly, w, 26f), $"+{r.lines.Count - shown}…", lineStyle);
            }

            DrawStatLine(lx, y + boxHeight - 44f, w * 0.55f, lx + w * 0.45f, w * 0.55f,
                GetLocalizedString("reward.bounty_total", "보유 현상금"), $"${r.bountyTotal:N0}");
        }

        private string FormatRewardLine(RewardLine line)
        {
            string fmt = GetLocalizedString(line.key, line.key + " {0}");
            string arg = line.arg ?? "";
            if (line.key == "reward.mastery_level")
            {
                var parts = arg.Split('|');
                string charName = parts.Length > 0 ? GetLocalizedString($"char.{parts[0]}.name", parts[0]) : "";
                return string.Format(fmt, charName, parts.Length > 1 ? parts[1] : "");
            }
            // args that are localization keys (stage / region / skin names)
            if (_localization != null && _localization.HasString(arg)) arg = _localization.GetString(arg);
            return string.Format(fmt, arg);
        }

        private void DrawStatLine(float labelX, float y, float labelWidth, float valueX, float valueWidth, string label, string value)
        {
            GUI.Label(new Rect(labelX, y, labelWidth, 30f), label, _statsLabelStyle);
            GUI.Label(new Rect(valueX, y, valueWidth, 30f), value, _statsValueStyle);
        }

        private bool _adPending;
        private string _note;

        /// <summary>PvE defeat → rewarded-ad continue (FP 518). Ad SDK is an external stub (ExternalServices.Ads).</summary>
        public bool CanContinueWithAd
        {
            get
            {
                if (_session == null || _session.mode != GameMode.PvE || _session.result != MatchResult.Defeat) return false;
                var cfg = HillNGun.External.ExternalServices.Config;
                int max = cfg != null ? cfg.maxContinuesPerRun : 1;
                return _session.continuesUsed < max && HillNGun.External.ExternalServices.Ads.IsRewardedReady("pve_continue");
            }
        }

        public void ContinueWithAd()
        {
            if (_adPending || !CanContinueWithAd) return;
            _adPending = true;
            _note = GetLocalizedString("result.ad_playing", "광고 재생 중... (오프라인 스텁)");
            HillNGun.External.ExternalServices.Ads.ShowRewarded("pve_continue", ok =>
            {
                _adPending = false;
                if (!ok) { _note = GetLocalizedString("result.ad_unavailable", "지금은 광고를 볼 수 없습니다"); return; }
                _session.continuesUsed++;
                _session.startStageIndex = _session.currentStageIndex; // retry the failed stage; earlier clears were already paid
                HillNGun.External.ExternalServices.Log("pve_continue", new System.Collections.Generic.Dictionary<string, object> { { "stage_index", _session.currentStageIndex } });
                GameFlowManager.Instance?.StartMatch();
            });
        }

        public void GoToLobby(LobbyTab tab)
        {
            LobbyUI.RequestedTab = tab;
            GameFlowManager.Instance?.GoToModeSelect();
        }

        private void DrawButtons()
        {
            bool canContinue = CanContinueWithAd;
            int count = canContinue ? 4 : 3;
            float buttonWidth = 220f;
            float buttonHeight = 54f;
            float spacing = 24f;
            float totalWidth = buttonWidth * count + spacing * (count - 1);
            float startX = (Screen.width - totalWidth) / 2f;
            float y = Screen.height - buttonHeight - 50f;
            int i = 0;

            if (canContinue)
            {
                if (ResultButton(new Rect(startX, y, buttonWidth, buttonHeight), GetLocalizedString("result.continue_ad", "광고 보고 이어하기") , _buttonStyle))
                    ContinueWithAd();
                GUI.color = new Color(0.55f, 0.6f, 0.8f);
                GUI.DrawTexture(new Rect(startX, y - 26f, buttonWidth, 22f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                var tag = new GUIStyle(_statsLabelStyle) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
                tag.normal.textColor = Color.white;
                GUI.Label(new Rect(startX, y - 26f, buttonWidth, 22f), GetLocalizedString("stub.ads", "광고 SDK 없음 · 스텁"), tag);
                i++;
            }

            if (ResultButton(new Rect(startX + (buttonWidth + spacing) * i++, y, buttonWidth, buttonHeight), GetLocalizedString("ui.retry", "재도전"), _buttonStyle))
                GameFlowManager.Instance?.RestartMatch();

            bool pve = _session != null && _session.mode == GameMode.PvE;
            string nextText = pve ? GetLocalizedString("result.to_stages", "스테이지 선택") : GetLocalizedString("result.to_characters", "캐릭터·스킨");
            if (ResultButton(new Rect(startX + (buttonWidth + spacing) * i++, y, buttonWidth, buttonHeight), nextText, _buttonStyle))
                GoToLobby(pve ? LobbyTab.Stages : LobbyTab.Characters);

            if (ResultButton(new Rect(startX + (buttonWidth + spacing) * i++, y, buttonWidth, buttonHeight), GetLocalizedString("ui.to_lobby", "로비"), _buttonStyle))
                GoToLobby(LobbyTab.Home);

            if (!string.IsNullOrEmpty(_note))
            {
                var st = new GUIStyle(_statsLabelStyle) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, y - 60f, Screen.width, 30f), _note, st);
            }
        }

        // Draws an explicit plate behind result buttons (style backgrounds can be lost after domain/scene reloads).
        private bool ResultButton(Rect r, string label, GUIStyle style)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            GUI.color = new Color(0.85f, 0.62f, 0.28f);
            GUI.DrawTexture(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), Texture2D.whiteTexture);
            GUI.color = hover ? new Color(0.62f, 0.45f, 0.24f) : new Color(0.45f, 0.32f, 0.2f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            return GUI.Button(r, label, style);
        }

        private void DrawBorder(float x, float y, float width, float height, Color color, float thickness)
        {
            HillNGun.UI.UIKit.Border(new Rect(x, y, width, height), color, thickness); // one draw call instead of four
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

using UnityEngine;
using HillNGun.Core;
using HillNGun.UI;

namespace HillNGun.PvE
{
    public class PvEHUD : MonoBehaviour
    {
        private PvEMatchManager _matchManager;
        private LocalizationManager _localization;

        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _waveStyle;
        private bool _stylesInitialized;

        private float _gaugeFlashTimer;
        private int _lastGauge;

        private void Start()
        {
            _matchManager = FindFirstObjectByType<PvEMatchManager>();
            _localization = ServiceLocator.Get<LocalizationManager>();

            if (_matchManager != null)
            {
                _matchManager.OnGaugeChanged += OnGaugeChanged;
                _matchManager.OnWaveChanged += OnWaveChanged;
            }
        }

        private void OnDestroy()
        {
            if (_matchManager != null)
            {
                _matchManager.OnGaugeChanged -= OnGaugeChanged;
                _matchManager.OnWaveChanged -= OnWaveChanged;
            }
        }

        private void OnGaugeChanged(int gauge)
        {
            if (gauge > _lastGauge)
            {
                _gaugeFlashTimer = 0.3f;
            }
            _lastGauge = gauge;
        }

        private void OnWaveChanged(int wave)
        {
        }

        private void Update()
        {
            if (_gaugeFlashTimer > 0f)
            {
                _gaugeFlashTimer -= Time.deltaTime;
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 20;
            _labelStyle.alignment = TextAnchor.MiddleLeft;
            _labelStyle.normal.textColor = new Color(0.9f, 0.85f, 0.7f);

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 16;
            _headerStyle.alignment = TextAnchor.MiddleLeft;
            _headerStyle.normal.textColor = new Color(0.7f, 0.65f, 0.55f);

            _waveStyle = new GUIStyle(GUI.skin.label);
            _waveStyle.fontSize = 28;
            _waveStyle.fontStyle = FontStyle.Bold;
            _waveStyle.alignment = TextAnchor.MiddleCenter;
            _waveStyle.normal.textColor = new Color(1f, 0.9f, 0.6f);

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (HillNGun.DebugTools.SmokeTestRunner.NoGui) return;
            if (_matchManager == null) return;

            InitStyles();

            bool bossStage = _matchManager.ActiveBoss != null || _matchManager.GaugeMax <= 0;
            if (!bossStage)
            {
                DrawGaugeBar();
                DrawWaveInfo();
            }
            DrawTimer();

            if (_matchManager.ActiveBoss != null)
            {
                DrawBossHealthBar();
            }
        }

        private void DrawGaugeBar()
        {
            float barWidth = 250f;
            float barHeight = 25f;
            float x = 20f;
            float y = 20f;

            string gaugeLabel = GetLocalizedString("pve.ui.gauge", "적 게이지");
            GUI.Label(new Rect(x, y, 150f, 20f), gaugeLabel, _headerStyle);
            y += 22f;

            GUI.color = new Color(0.2f, 0.18f, 0.15f);
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

            float fillPercent = (float)_matchManager.CurrentGauge / _matchManager.GaugeMax;
            fillPercent = Mathf.Clamp01(fillPercent);

            Color fillColor = _gaugeFlashTimer > 0f 
                ? new Color(1f, 0.9f, 0.5f)
                : new Color(0.8f, 0.5f, 0.2f);
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(x + 2, y + 2, (barWidth - 4) * fillPercent, barHeight - 4), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string gaugeText = $"{_matchManager.CurrentGauge} / {_matchManager.GaugeMax}";
            GUI.Label(new Rect(x, y, barWidth, barHeight), gaugeText, _labelStyle);
        }

        private void DrawWaveInfo()
        {
            float x = Screen.width / 2f;
            float y = 20f;

            string waveLabel = GetLocalizedString("pve.ui.wave", "웨이브");
            string waveText = $"{waveLabel} {_matchManager.CurrentWave + 1} / {_matchManager.TotalWaves}";
            
            float textWidth = 200f;
            GUI.Label(new Rect(x - textWidth / 2f, y, textWidth, 40f), waveText, _waveStyle);
        }

        private void DrawTimer()
        {
            float x = Screen.width - 120f;
            float y = 20f;

            int minutes = Mathf.FloorToInt(_matchManager.StageTimer / 60f);
            int seconds = Mathf.FloorToInt(_matchManager.StageTimer % 60f);
            string timeText = $"{minutes:00}:{seconds:00}";

            GUIStyle timerStyle = new GUIStyle(_labelStyle);
            timerStyle.fontSize = 24;
            timerStyle.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(x, y, 100f, 30f), timeText, timerStyle);
        }

        private void DrawBossHealthBar()
        {
            var boss = _matchManager.ActiveBoss;
            if (boss == null) return;

            float barWidth = 400f;
            float barHeight = 30f;
            float x = (Screen.width - barWidth) / 2f;
            float y = Screen.height - 80f;

            string bossName = GetLocalizedString(boss.Data.nameKey, boss.Data.id);
            GUIStyle nameStyle = new GUIStyle(_waveStyle);
            nameStyle.fontSize = 22;
            GUI.Label(new Rect(x, y - 30f, barWidth, 25f), bossName, nameStyle);

            GUI.color = new Color(0.15f, 0.12f, 0.1f);
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

            float hpPercent = boss.HpRatio;
            hpPercent = Mathf.Clamp01(hpPercent);

            Color hpColor = boss.CurrentPhase switch
            {
                BossPhase.Phase1 => new Color(0.7f, 0.3f, 0.3f),
                BossPhase.Phase2 => new Color(0.8f, 0.5f, 0.2f),
                BossPhase.Phase3 => new Color(0.9f, 0.2f, 0.2f),
                _ => new Color(0.7f, 0.3f, 0.3f)
            };

            GUI.color = hpColor;
            GUI.DrawTexture(new Rect(x + 2, y + 2, (barWidth - 4) * hpPercent, barHeight - 4), Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawBorder(x, y, barWidth, barHeight, new Color(0.4f, 0.3f, 0.2f), 2f);

            string hpText = $"{boss.CurrentHp} / {boss.MaxHp}";
            GUIStyle hpStyle = new GUIStyle(_labelStyle);
            hpStyle.alignment = TextAnchor.MiddleCenter;
            hpStyle.fontSize = 18;
            GUI.Label(new Rect(x, y, barWidth, barHeight), hpText, hpStyle);

            string phaseText = boss.CurrentPhase switch
            {
                BossPhase.Phase1 => "Phase 1",
                BossPhase.Phase2 => "Phase 2",
                BossPhase.Phase3 => "Phase 3",
                _ => ""
            };
            GUI.Label(new Rect(x + barWidth + 10f, y, 80f, barHeight), phaseText, _labelStyle);
        }

        private void DrawBorder(float x, float y, float width, float height, Color color, float thickness)
        {
            HillNGun.UI.UIKit.Border(new Rect(x, y, width, height), color, thickness); // one draw call instead of four
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

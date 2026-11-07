using UnityEngine;
using UnityEngine.InputSystem;
using HillNGun.Core;

namespace HillNGun.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        private bool _isPaused;
        public bool IsPaused => _isPaused;

        private bool _showMenu;

        private LocalizationManager _localization;

        public event System.Action OnPause;
        public event System.Action OnResume;

        private void Start()
        {
            _localization = ServiceLocator.Get<LocalizationManager>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_isPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        public void Pause()
        {
            if (_isPaused) return;
            _isPaused = true;
            _showMenu = true;
            Time.timeScale = 0f;
            OnPause?.Invoke();
        }

        public void Resume()
        {
            if (!_isPaused) return;
            if (_showSettings) { _settingsView.Flush(); _showSettings = false; }
            _isPaused = false;
            _showMenu = false;
            Time.timeScale = 1f;
            OnResume?.Invoke();
        }

        public void RestartMatch()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            _showMenu = false;
            GameFlowManager.Instance?.RestartMatch();
        }

        public void QuitToTitle()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            _showMenu = false;
            GameFlowManager.Instance?.GoToTitle();
        }

        public void QuitToModeSelect()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            _showMenu = false;
            GameFlowManager.Instance?.GoToModeSelect();
        }

        private readonly SettingsView _settingsView = new SettingsView();
        private bool _showSettings;
        public bool SettingsOpen => _showSettings;

        public void OpenSettings()
        {
            if (!_isPaused) Pause();
            _showSettings = true;
        }

        private void OnGUI()
        {
            Vector2 size = UIKit.Begin();
            GUI.depth = -50;
            if (!_showMenu)
            {
                // Pause button for desktop and touch (touch: larger, and reported so the touch controls stay clear of it)
                var mobile = ServiceLocator.Get<HillNGun.Input.MobileTouchControls>();
                bool touch = mobile != null && mobile.IsActive;
                Rect pr = touch ? new Rect(size.x - 92f, 56f, 76f, 64f) : new Rect(size.x - 64f, 56f, 52f, 44f);
                if (touch) HudZones.ReportGui("pause", pr);
                if (UIKit.Button(pr, "II", true, UIKit.ButtonKind.Normal, touch ? 28 : 22))
                    Pause();
                UIKit.End();
                return;
            }

            UIKit.Fill(new Rect(0, 0, size.x, size.y), UIKit.Theme.overlay);
            if (_showSettings)
            {
                Rect area = new Rect(size.x / 2f - 520f, 40f, 1040f, size.y - 80f);
                UIKit.Panel(area);
                UIKit.Header(new Rect(area.x + 20f, area.y + 8f, 400f, 40f), GetLocalizedString("lobby.tab.settings", "설정"));
                _settingsView.Draw(new Rect(area.x + 10f, area.y + 52f, area.width - 20f, area.height - 120f));
                if (Time.unscaledTime < _settingsView.ToastUntil)
                    UIKit.Label(new Rect(area.x, area.yMax - 108f, area.width, 30f), _settingsView.Toast, UIKit.Theme.fontBody, UIKit.Theme.accent, TextAnchor.MiddleCenter, true);
                if (UIKit.Button(new Rect(area.x + area.width / 2f - 120f, area.yMax - 62f, 240f, 48f), GetLocalizedString("ui.back", "뒤로"), true, UIKit.ButtonKind.Primary))
                {
                    _settingsView.Flush();
                    _showSettings = false;
                }
                UIKit.End();
                return;
            }

            bool scaleAb = ScalePresets.Data != null;
            float boxWidth = 420f;
            float boxHeight = (scaleAb ? 490f : 420f) + 140f; // + debug bullet-speed row + cover debug row
            float x = (size.x - boxWidth) / 2f;
            float y = (size.y - boxHeight) / 2f;
            UIKit.Panel(new Rect(x, y, boxWidth, boxHeight));
            UIKit.Title(new Rect(x, y + 14f, boxWidth, 50f), GetLocalizedString("ui.paused", "일시정지"));

            float bw = 300f, bh = 52f, bx = x + (boxWidth - bw) / 2f, by = y + 84f, sp = 64f;
            if (scaleAb)
            {
                // A/B scale test (기획 비율 1/2/3안): pick → current stage restarts with that preset
                DrawScaleToggles(new Rect(bx, by, bw, bh));
                by += 70f;
            }
            // Debug: global projectile speed factor (12 §1). Applies to newly fired bullets; no restart.
            DrawBulletSpeedToggles(new Rect(bx, by, bw, bh));
            by += 70f;
            // Debug (12 §4.3): cover collider / guaranteed-safe width overlay. Also F4 or -cover-debug.
            if (UIKit.Button(new Rect(bx, by, bw, bh), "엄폐 디버그 " + (TerrainRules.CoverDebug ? "ON" : "OFF"), true,
                    TerrainRules.CoverDebug ? UIKit.ButtonKind.Primary : UIKit.ButtonKind.Normal, 20))
                TerrainRules.CoverDebug = !TerrainRules.CoverDebug;
            by += 70f;
            if (UIKit.Button(new Rect(bx, by, bw, bh), GetLocalizedString("ui.resume", "재개"), true, UIKit.ButtonKind.Primary)) Resume();
            if (UIKit.Button(new Rect(bx, by + sp, bw, bh), GetLocalizedString("ui.restart", "재시작"))) RestartMatch();
            if (UIKit.Button(new Rect(bx, by + sp * 2, bw, bh), GetLocalizedString("lobby.tab.settings", "설정"))) _showSettings = true;
            if (UIKit.Button(new Rect(bx, by + sp * 3, bw, bh), GetLocalizedString("ui.quit_to_lobby", "로비로 나가기"))) QuitToModeSelect();
            if (UIKit.Button(new Rect(bx, by + sp * 4, bw, bh), GetLocalizedString("ui.quit_to_title", "타이틀로 나가기"))) QuitToTitle();
            UIKit.End();
        }

        private void DrawScaleToggles(Rect r)
        {
            float gap = 8f, w = (r.width - gap * 2f) / 3f;
            int cur = ScalePresets.Current;
            for (int i = 1; i <= 3; i++)
            {
                var br = new Rect(r.x + (i - 1) * (w + gap), r.y, w, r.height);
                if (UIKit.Button(br, "비율 " + i, ScalePresets.Data.HasOption(i), i == cur ? UIKit.ButtonKind.Primary : UIKit.ButtonKind.Normal, 20) && i != cur)
                {
                    _isPaused = false;
                    _showMenu = false;
                    ScalePresets.SelectAndRestart(i);
                }
            }
        }

        private void DrawBulletSpeedToggles(Rect r)
        {
            var opts = HillNGun.Data.WeaponData.GlobalProjectileSpeedMulOptions;
            float gap = 8f, w = (r.width - gap * (opts.Length - 1)) / opts.Length;
            float cur = HillNGun.Data.WeaponData.GlobalProjectileSpeedMul;
            for (int i = 0; i < opts.Length; i++)
            {
                var br = new Rect(r.x + i * (w + gap), r.y, w, r.height);
                bool active = Mathf.Approximately(cur, opts[i]);
                if (UIKit.Button(br, "탄속 " + opts[i].ToString("0.0"), true, active ? UIKit.ButtonKind.Primary : UIKit.ButtonKind.Normal, 20) && !active)
                {
                    HillNGun.Data.WeaponData.GlobalProjectileSpeedMul = opts[i];
                    Debug.Log($"[BulletSpeed] global projectile speed mul -> {opts[i]:0.0}");
                }
            }
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

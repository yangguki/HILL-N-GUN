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
                // Desktop pause button (touch devices use the on-screen "||" of MobileTouchControls)
                var mobile = ServiceLocator.Get<HillNGun.Input.MobileTouchControls>();
                if ((mobile == null || !mobile.IsActive) && UIKit.Button(new Rect(size.x - 64f, 56f, 52f, 44f), "II", true, UIKit.ButtonKind.Normal, 22))
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

            float boxWidth = 420f;
            float boxHeight = 420f;
            float x = (size.x - boxWidth) / 2f;
            float y = (size.y - boxHeight) / 2f;
            UIKit.Panel(new Rect(x, y, boxWidth, boxHeight));
            UIKit.Title(new Rect(x, y + 14f, boxWidth, 50f), GetLocalizedString("ui.paused", "일시정지"));

            float bw = 300f, bh = 52f, bx = x + (boxWidth - bw) / 2f, by = y + 84f, sp = 64f;
            if (UIKit.Button(new Rect(bx, by, bw, bh), GetLocalizedString("ui.resume", "재개"), true, UIKit.ButtonKind.Primary)) Resume();
            if (UIKit.Button(new Rect(bx, by + sp, bw, bh), GetLocalizedString("ui.restart", "재시작"))) RestartMatch();
            if (UIKit.Button(new Rect(bx, by + sp * 2, bw, bh), GetLocalizedString("lobby.tab.settings", "설정"))) _showSettings = true;
            if (UIKit.Button(new Rect(bx, by + sp * 3, bw, bh), GetLocalizedString("ui.quit_to_lobby", "로비로 나가기"))) QuitToModeSelect();
            if (UIKit.Button(new Rect(bx, by + sp * 4, bw, bh), GetLocalizedString("ui.quit_to_title", "타이틀로 나가기"))) QuitToTitle();
            UIKit.End();
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

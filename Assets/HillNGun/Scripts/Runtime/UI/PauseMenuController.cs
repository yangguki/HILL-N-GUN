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
        private GUIStyle _boxStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized;

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

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTexture(1, 1, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            _boxStyle.alignment = TextAnchor.MiddleCenter;
            _boxStyle.padding = new RectOffset(20, 20, 20, 20);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 24;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = new Color(1f, 0.8f, 0.4f);
            _buttonStyle.normal.background = MakeTexture(1, 1, new Color(0.3f, 0.25f, 0.2f));
            _buttonStyle.hover.background = MakeTexture(1, 1, new Color(0.5f, 0.4f, 0.3f));
            _buttonStyle.padding = new RectOffset(20, 20, 15, 15);

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 32;
            _labelStyle.fontStyle = FontStyle.Bold;
            _labelStyle.normal.textColor = new Color(1f, 0.9f, 0.7f);
            _labelStyle.alignment = TextAnchor.MiddleCenter;

            _stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (!_showMenu) return;

            InitStyles();

            float boxWidth = 400f;
            float boxHeight = 320f;
            float x = (Screen.width - boxWidth) / 2f;
            float y = (Screen.height - boxHeight) / 2f;

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), "", _boxStyle);

            string titleText = GetLocalizedString("ui.paused", "일시정지");
            GUI.Label(new Rect(x, y + 20f, boxWidth, 50f), titleText, _labelStyle);

            float buttonWidth = 280f;
            float buttonHeight = 50f;
            float buttonX = x + (boxWidth - buttonWidth) / 2f;
            float buttonStartY = y + 90f;
            float buttonSpacing = 60f;

            string resumeText = GetLocalizedString("ui.resume", "재개");
            if (GUI.Button(new Rect(buttonX, buttonStartY, buttonWidth, buttonHeight), resumeText, _buttonStyle))
            {
                Resume();
            }

            string restartText = GetLocalizedString("ui.restart", "재시작");
            if (GUI.Button(new Rect(buttonX, buttonStartY + buttonSpacing, buttonWidth, buttonHeight), restartText, _buttonStyle))
            {
                RestartMatch();
            }

            string quitText = GetLocalizedString("ui.quit_to_menu", "메뉴로 나가기");
            if (GUI.Button(new Rect(buttonX, buttonStartY + buttonSpacing * 2, buttonWidth, buttonHeight), quitText, _buttonStyle))
            {
                QuitToModeSelect();
            }

            string titleQuitText = GetLocalizedString("ui.quit_to_title", "타이틀로 나가기");
            if (GUI.Button(new Rect(buttonX, buttonStartY + buttonSpacing * 3, buttonWidth, buttonHeight), titleQuitText, _buttonStyle))
            {
                QuitToTitle();
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

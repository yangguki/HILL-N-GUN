using UnityEngine;
using HillNGun.Core;

namespace HillNGun.UI
{
    public class ModeSelectUI : MonoBehaviour
    {
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _descStyle;
        private GUIStyle _backButtonStyle;
        private bool _stylesInitialized;

        private LocalizationManager _localization;
        private int _hoveredMode = -1;
        private float _animTime;

        private void Start()
        {
            _localization = ServiceLocator.Get<LocalizationManager>();
        }

        private void Update()
        {
            _animTime += Time.deltaTime;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 48;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.normal.textColor = new Color(1f, 0.9f, 0.7f);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 28;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = new Color(1f, 0.9f, 0.6f);
            _buttonStyle.normal.background = MakeTexture(1, 1, new Color(0.4f, 0.3f, 0.2f));
            _buttonStyle.hover.background = MakeTexture(1, 1, new Color(0.6f, 0.45f, 0.3f));
            _buttonStyle.padding = new RectOffset(20, 20, 25, 25);

            _descStyle = new GUIStyle(GUI.skin.label);
            _descStyle.fontSize = 18;
            _descStyle.alignment = TextAnchor.MiddleCenter;
            _descStyle.normal.textColor = new Color(0.8f, 0.7f, 0.6f);
            _descStyle.wordWrap = true;

            _backButtonStyle = new GUIStyle(GUI.skin.button);
            _backButtonStyle.fontSize = 20;
            _backButtonStyle.normal.textColor = new Color(0.9f, 0.8f, 0.7f);
            _backButtonStyle.hover.textColor = Color.white;
            _backButtonStyle.normal.background = MakeTexture(1, 1, new Color(0.3f, 0.25f, 0.2f));
            _backButtonStyle.hover.background = MakeTexture(1, 1, new Color(0.45f, 0.35f, 0.25f));
            _backButtonStyle.padding = new RectOffset(15, 15, 10, 10);

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
            InitStyles();

            DrawBackground();
            DrawTitle();
            DrawModeButtons();
            DrawBackButton();
        }

        private void DrawBackground()
        {
            Color bgColor = new Color(0.18f, 0.14f, 0.1f);
            GUI.color = bgColor;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawTitle()
        {
            string title = GetLocalizedString("ui.select_mode", "모드 선택");
            GUI.Label(new Rect(0, Screen.height * 0.08f, Screen.width, 70f), title, _titleStyle);
        }

        private void DrawModeButtons()
        {
            float cardWidth = 280f;
            float cardHeight = 220f;
            float spacing = 40f;
            float totalWidth = cardWidth * 3f + spacing * 2f;
            float startX = (Screen.width - totalWidth) / 2f;
            float startY = Screen.height * 0.25f;

            ModeCard[] cards = new ModeCard[]
            {
                new ModeCard
                {
                    mode = GameMode.PvE,
                    titleKey = "mode.pve.title",
                    titleFallback = "PvE 스토리",
                    descKey = "mode.pve.desc",
                    descFallback = "먼지 마을을 지켜라!\n3개 스테이지 + 보스전",
                    iconColor = new Color(0.3f, 0.6f, 0.4f)
                },
                new ModeCard
                {
                    mode = GameMode.PvP,
                    titleKey = "mode.pvp.title",
                    titleFallback = "PvP 대전",
                    descKey = "mode.pvp.desc",
                    descFallback = "봇과 3v3 탈락전\n3판 2선승",
                    iconColor = new Color(0.6f, 0.35f, 0.3f)
                },
                new ModeCard
                {
                    mode = GameMode.Practice,
                    titleKey = "mode.practice.title",
                    titleFallback = "연습장",
                    descKey = "mode.practice.desc",
                    descFallback = "조작 연습 및 테스트\n과녁과 엄폐물",
                    iconColor = new Color(0.4f, 0.45f, 0.6f)
                }
            };

            for (int i = 0; i < cards.Length; i++)
            {
                float x = startX + i * (cardWidth + spacing);
                DrawModeCard(x, startY, cardWidth, cardHeight, cards[i], i);
            }
        }

        private struct ModeCard
        {
            public GameMode mode;
            public string titleKey;
            public string titleFallback;
            public string descKey;
            public string descFallback;
            public Color iconColor;
        }

        private void DrawModeCard(float x, float y, float width, float height, ModeCard card, int index)
        {
            bool isHovered = _hoveredMode == index;
            float hoverScale = isHovered ? 1.05f : 1f;
            float scaledWidth = width * hoverScale;
            float scaledHeight = height * hoverScale;
            float scaledX = x - (scaledWidth - width) / 2f;
            float scaledY = y - (scaledHeight - height) / 2f;

            Color bgColor = isHovered ? new Color(0.35f, 0.28f, 0.2f) : new Color(0.25f, 0.2f, 0.15f);
            GUI.color = bgColor;
            GUI.DrawTexture(new Rect(scaledX, scaledY, scaledWidth, scaledHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            Color borderColor = isHovered ? new Color(0.8f, 0.6f, 0.4f) : new Color(0.5f, 0.4f, 0.3f);
            DrawBorder(scaledX, scaledY, scaledWidth, scaledHeight, borderColor, 3f);

            float iconSize = 60f;
            float iconX = scaledX + (scaledWidth - iconSize) / 2f;
            float iconY = scaledY + 25f;
            GUI.color = card.iconColor;
            GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), GetModeIcon(card.mode));
            GUI.color = Color.white;

            string title = GetLocalizedString(card.titleKey, card.titleFallback);
            GUIStyle titleStyle = new GUIStyle(_buttonStyle);
            titleStyle.normal.background = null;
            titleStyle.hover.background = null;
            GUI.Label(new Rect(scaledX, iconY + iconSize + 10f, scaledWidth, 40f), title, titleStyle);

            string desc = GetLocalizedString(card.descKey, card.descFallback);
            GUI.Label(new Rect(scaledX + 15f, iconY + iconSize + 55f, scaledWidth - 30f, 80f), desc, _descStyle);

            if (GUI.Button(new Rect(scaledX, scaledY, scaledWidth, scaledHeight), "", GUIStyle.none))
            {
                OnModeSelected(card.mode);
            }

            Rect cardRect = new Rect(scaledX, scaledY, scaledWidth, scaledHeight);
            if (cardRect.Contains(Event.current.mousePosition))
            {
                _hoveredMode = index;
            }
            else if (_hoveredMode == index)
            {
                _hoveredMode = -1;
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

        private Texture2D _pveIcon;
        private Texture2D _pvpIcon;
        private Texture2D _practiceIcon;

        private Texture2D GetModeIcon(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.PvE:
                    if (_pveIcon == null) _pveIcon = CreatePvEIcon();
                    return _pveIcon;
                case GameMode.PvP:
                    if (_pvpIcon == null) _pvpIcon = CreatePvPIcon();
                    return _pvpIcon;
                case GameMode.Practice:
                    if (_practiceIcon == null) _practiceIcon = CreatePracticeIcon();
                    return _practiceIcon;
                default:
                    return Texture2D.whiteTexture;
            }
        }

        private Texture2D CreatePvEIcon()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < size / 2f - 4 && dist > size / 2f - 10)
                    {
                        pixels[y * size + x] = Color.white;
                    }

                    if (x > size / 2 - 8 && x < size / 2 + 8 && y > 10 && y < size - 10)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D CreatePvPIcon()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx1 = x - 22;
                    float dy1 = y - 42;
                    float dist1 = Mathf.Sqrt(dx1 * dx1 + dy1 * dy1);

                    float dx2 = x - 42;
                    float dy2 = y - 22;
                    float dist2 = Mathf.Sqrt(dx2 * dx2 + dy2 * dy2);

                    if (dist1 < 16 || dist2 < 16)
                    {
                        pixels[y * size + x] = Color.white;
                    }

                    if (x >= 25 && x <= 40 && y >= 25 && y <= 40)
                    {
                        pixels[y * size + x] = new Color(1f, 0.5f, 0.3f);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D CreatePracticeIcon()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            float centerX = size / 2f;
            float centerY = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < 8)
                        pixels[y * size + x] = Color.white;
                    else if (dist >= 12 && dist < 18)
                        pixels[y * size + x] = Color.white;
                    else if (dist >= 22 && dist < 28)
                        pixels[y * size + x] = Color.white;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void DrawBackButton()
        {
            float buttonWidth = 120f;
            float buttonHeight = 40f;
            float x = 30f;
            float y = Screen.height - buttonHeight - 30f;

            string backText = GetLocalizedString("ui.back", "뒤로");
            if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), backText, _backButtonStyle))
            {
                GameFlowManager.Instance?.GoToTitle();
            }
        }

        private void OnModeSelected(GameMode mode)
        {
            GameFlowManager.Instance?.GoToCharacterSelect(mode);
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

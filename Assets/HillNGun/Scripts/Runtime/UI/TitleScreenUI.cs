using UnityEngine;
using HillNGun.Core;

namespace HillNGun.UI
{
    public class TitleScreenUI : MonoBehaviour
    {
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _versionStyle;
        private bool _stylesInitialized;

        private LocalizationManager _localization;
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
            _titleStyle.fontSize = 72;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.normal.textColor = new Color(1f, 0.85f, 0.5f);

            _subtitleStyle = new GUIStyle(GUI.skin.label);
            _subtitleStyle.fontSize = 28;
            _subtitleStyle.alignment = TextAnchor.MiddleCenter;
            _subtitleStyle.normal.textColor = new Color(0.9f, 0.8f, 0.6f);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 32;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = new Color(1f, 0.9f, 0.6f);
            _buttonStyle.normal.background = MakeGradientTexture(256, 64, 
                new Color(0.5f, 0.35f, 0.2f), new Color(0.3f, 0.2f, 0.1f));
            _buttonStyle.hover.background = MakeGradientTexture(256, 64, 
                new Color(0.7f, 0.5f, 0.3f), new Color(0.5f, 0.35f, 0.2f));
            _buttonStyle.padding = new RectOffset(30, 30, 20, 20);

            _versionStyle = new GUIStyle(GUI.skin.label);
            _versionStyle.fontSize = 16;
            _versionStyle.alignment = TextAnchor.LowerRight;
            _versionStyle.normal.textColor = new Color(0.6f, 0.5f, 0.4f);

            _stylesInitialized = true;
        }

        private Texture2D MakeGradientTexture(int width, int height, Color top, Color bottom)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            InitStyles();

            DrawBackground();
            DrawTitle();
            DrawStartButton();
            DrawVersion();
        }

        private void DrawBackground()
        {
            Color bgColor = new Color(0.2f, 0.15f, 0.1f);
            GUI.color = bgColor;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float sunX = Screen.width * 0.8f;
            float sunY = Screen.height * 0.25f;
            float sunRadius = 80f;

            Color sunColor = new Color(1f, 0.7f, 0.3f, 0.8f);
            DrawCircle(sunX, sunY, sunRadius, sunColor);

            for (int i = 0; i < 12; i++)
            {
                float angle = (i * 30f + _animTime * 20f) * Mathf.Deg2Rad;
                float rayLength = 120f + Mathf.Sin(_animTime * 2f + i) * 20f;
                float x1 = sunX + Mathf.Cos(angle) * sunRadius;
                float y1 = sunY + Mathf.Sin(angle) * sunRadius;
                float x2 = sunX + Mathf.Cos(angle) * (sunRadius + rayLength);
                float y2 = sunY + Mathf.Sin(angle) * (sunRadius + rayLength);
                DrawLine(x1, y1, x2, y2, new Color(1f, 0.8f, 0.4f, 0.4f), 3f);
            }

            DrawMountains();
            DrawCacti();
        }

        private void DrawMountains()
        {
            Color mountainColor = new Color(0.3f, 0.2f, 0.15f);
            float baseY = Screen.height * 0.7f;

            float[] peaks = { 0.15f, 0.35f, 0.55f, 0.75f, 0.9f };
            float[] heights = { 0.25f, 0.35f, 0.3f, 0.4f, 0.2f };

            for (int i = 0; i < peaks.Length; i++)
            {
                float peakX = Screen.width * peaks[i];
                float peakY = baseY - Screen.height * heights[i];
                float width = Screen.width * 0.25f;

                DrawTriangle(peakX - width / 2f, baseY, peakX, peakY, peakX + width / 2f, baseY, mountainColor);
            }

            GUI.color = new Color(0.25f, 0.18f, 0.12f);
            GUI.DrawTexture(new Rect(0, baseY, Screen.width, Screen.height - baseY), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawCacti()
        {
            Color cactusColor = new Color(0.2f, 0.35f, 0.15f);
            float groundY = Screen.height * 0.72f;

            DrawCactus(Screen.width * 0.1f, groundY, 60f, cactusColor);
            DrawCactus(Screen.width * 0.25f, groundY, 45f, cactusColor);
            DrawCactus(Screen.width * 0.85f, groundY, 55f, cactusColor);
        }

        private void DrawCactus(float x, float y, float height, Color color)
        {
            float width = height * 0.2f;

            GUI.color = color;
            GUI.DrawTexture(new Rect(x - width / 2f, y - height, width, height), Texture2D.whiteTexture);

            float armY = y - height * 0.6f;
            float armLength = height * 0.3f;
            GUI.DrawTexture(new Rect(x - width / 2f - armLength, armY, armLength, width), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - width / 2f - armLength, armY - armLength, width, armLength), Texture2D.whiteTexture);

            float arm2Y = y - height * 0.4f;
            GUI.DrawTexture(new Rect(x + width / 2f, arm2Y, armLength * 0.8f, width), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + width / 2f + armLength * 0.8f - width, arm2Y - armLength * 0.7f, width, armLength * 0.7f), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        private void DrawTitle()
        {
            float titleY = Screen.height * 0.15f;
            float bounceOffset = Mathf.Sin(_animTime * 2f) * 5f;

            string title = GetLocalizedString("game.title", "힐앤건");
            GUI.Label(new Rect(0, titleY + bounceOffset, Screen.width, 100f), title, _titleStyle);

            string subtitle = GetLocalizedString("game.subtitle", "서부 카우보이 슈팅");
            GUI.Label(new Rect(0, titleY + 80f, Screen.width, 50f), subtitle, _subtitleStyle);
        }

        private void DrawStartButton()
        {
            float buttonWidth = 300f;
            float buttonHeight = 70f;
            float buttonX = (Screen.width - buttonWidth) / 2f;
            float buttonY = Screen.height * 0.55f;

            float pulse = 1f + Mathf.Sin(_animTime * 3f) * 0.05f;
            float scaledWidth = buttonWidth * pulse;
            float scaledHeight = buttonHeight * pulse;
            float scaledX = buttonX - (scaledWidth - buttonWidth) / 2f;
            float scaledY = buttonY - (scaledHeight - buttonHeight) / 2f;

            string startText = GetLocalizedString("ui.start_game", "게임 시작");
            if (GUI.Button(new Rect(scaledX, scaledY, scaledWidth, scaledHeight), startText, _buttonStyle))
            {
                GameFlowManager.Instance?.GoToModeSelect();
            }
        }

        private void DrawVersion()
        {
            string version = "v0.2.0";
            GUI.Label(new Rect(0, 0, Screen.width - 20f, Screen.height - 10f), version, _versionStyle);
        }

        private void DrawCircle(float x, float y, float radius, Color color)
        {
            GUI.color = color;
            Rect rect = new Rect(x - radius, y - radius, radius * 2f, radius * 2f);
            GUI.DrawTexture(rect, GetCircleTexture());
            GUI.color = Color.white;
        }

        private Texture2D _circleTexture;
        private Texture2D GetCircleTexture()
        {
            if (_circleTexture != null) return _circleTexture;

            int size = 64;
            _circleTexture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            float center = size / 2f;
            float radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(1f - (dist / radius) * 0.3f);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            _circleTexture.SetPixels(pixels);
            _circleTexture.Apply();
            return _circleTexture;
        }

        private void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness)
        {
            GUI.color = color;
            float angle = Mathf.Atan2(y2 - y1, x2 - x1) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

            GUIUtility.RotateAroundPivot(angle, new Vector2(x1, y1));
            GUI.DrawTexture(new Rect(x1, y1 - thickness / 2f, length, thickness), Texture2D.whiteTexture);
            GUIUtility.RotateAroundPivot(-angle, new Vector2(x1, y1));
            GUI.color = Color.white;
        }

        private void DrawTriangle(float x1, float y1, float x2, float y2, float x3, float y3, Color color)
        {
            GUI.color = color;
            float minY = Mathf.Min(y1, Mathf.Min(y2, y3));
            float maxY = Mathf.Max(y1, Mathf.Max(y2, y3));
            float minX = Mathf.Min(x1, Mathf.Min(x2, x3));
            float maxX = Mathf.Max(x1, Mathf.Max(x2, x3));

            for (float y = minY; y <= maxY; y += 2f)
            {
                float t = (y - y2) / (y1 - y2);
                float leftX = Mathf.Lerp(x2, x1, Mathf.Clamp01(t));
                float rightX = Mathf.Lerp(x2, x3, Mathf.Clamp01(t));

                if (leftX > rightX)
                {
                    float temp = leftX;
                    leftX = rightX;
                    rightX = temp;
                }

                GUI.DrawTexture(new Rect(leftX, y, rightX - leftX, 2f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
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

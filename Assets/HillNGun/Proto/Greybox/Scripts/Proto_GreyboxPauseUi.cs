using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    public sealed class Proto_GreyboxPauseUi
    {
        public const string RootName = "Greybox_PauseOverlay";
        public const float PauseHitX0 = 92.5f;
        public const float PauseHitX1 = 98.5f;
        public const float PauseHitY0 = 3.2f;
        public const float PauseHitY1 = 9.2f;
        public const float ResumeHitX0 = 38f;
        public const float ResumeHitX1 = 62f;
        public const float ResumeHitY0 = 56f;
        public const float ResumeHitY1 = 62f;
        public const float PauseSettingsHitX0 = 38f;
        public const float PauseSettingsHitX1 = 62f;
        public const float PauseSettingsHitY0 = 64f;
        public const float PauseSettingsHitY1 = 70f;
        public const float PauseRestartHitX0 = 38f;
        public const float PauseRestartHitX1 = 62f;
        public const float PauseRestartHitY0 = 72f;
        public const float PauseRestartHitY1 = 78f;
        public const float ViewToggleHitX0 = 32f;
        public const float ViewToggleHitX1 = 68f;
        public const float ViewToggleHitY0 = 45f;
        public const float ViewToggleHitY1 = 54f;

        GameObject _root;
        Text _viewLabel;
        Image _viewPlate;

        public bool IsBuilt => _root != null;
        public bool IsVisible => _root != null && _root.activeSelf;

        public void Clear()
        {
            if (_root != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_root);
                else
                    Object.DestroyImmediate(_root);
            }

            _root = null;
        }

        public void Ensure(Transform canvasTransform)
        {
            if (_root != null) return;
            if (canvasTransform == null) return;

            var font = Proto_GreyboxHudBuilder.GetUiFont(out bool hangul);

            _root = new GameObject(RootName);
            _root.transform.SetParent(canvasTransform, false);
            var rootRt = _root.AddComponent<RectTransform>();
            StretchFull(rootRt);

            PlaceImage(_root.transform, "Greybox_PauseDim", 0f, 0f, 100f, 100f,
                new Color(0.04f, 0.05f, 0.07f, 0.62f));
            PlaceImage(_root.transform, "Greybox_PauseCard", 28f, 28f, 72f, 82f,
                new Color(16f / 255f, 18f / 255f, 26f / 255f, 0.94f));

            string title = hangul ? "일시정지" : "PAUSED";
            string resume = hangul ? "계속" : "RESUME";
            string settings = hangul ? "설정" : "SETTINGS";
            string restart = hangul ? "다시하기" : "RESTART";

            PlaceText(_root.transform, "Greybox_PauseTitle", 32f, 32f, 68f, 42f, font, 28,
                TextAnchor.MiddleCenter, Color.white, title);
            _viewPlate = PlaceButtonPlate(_root.transform, "Greybox_PauseViewPlate", ViewToggleHitX0, ViewToggleHitY0,
                ViewToggleHitX1, ViewToggleHitY1, new Color(0.55f, 0.22f, 0.72f, 0.95f));
            _viewLabel = PlaceText(_root.transform, "Greybox_PauseViewLabel", ViewToggleHitX0, ViewToggleHitY0,
                ViewToggleHitX1, ViewToggleHitY1, font, 24, TextAnchor.MiddleCenter, Color.white, "2D  /  3D");
            PlaceButtonPlate(_root.transform, "Greybox_PauseResumePlate", ResumeHitX0, ResumeHitY0, ResumeHitX1,
                ResumeHitY1);
            PlaceText(_root.transform, "Greybox_PauseResumeLabel", ResumeHitX0, ResumeHitY0, ResumeHitX1,
                ResumeHitY1, font, 22, TextAnchor.MiddleCenter, Color.white, resume);
            PlaceButtonPlate(_root.transform, "Greybox_PauseSettingsPlate", PauseSettingsHitX0, PauseSettingsHitY0,
                PauseSettingsHitX1, PauseSettingsHitY1);
            PlaceText(_root.transform, "Greybox_PauseSettingsLabel", PauseSettingsHitX0, PauseSettingsHitY0,
                PauseSettingsHitX1, PauseSettingsHitY1, font, 20, TextAnchor.MiddleCenter, Color.white, settings);
            PlaceButtonPlate(_root.transform, "Greybox_PauseRestartPlate", PauseRestartHitX0, PauseRestartHitY0,
                PauseRestartHitX1, PauseRestartHitY1);
            PlaceText(_root.transform, "Greybox_PauseRestartLabel", PauseRestartHitX0, PauseRestartHitY0,
                PauseRestartHitX1, PauseRestartHitY1, font, 20, TextAnchor.MiddleCenter,
                new Color(0.96f, 0.94f, 0.88f, 1f), restart);

            foreach (var t in _root.GetComponentsInChildren<Text>(true))
            {
                t.raycastTarget = false;
                t.supportRichText = false;
            }

            _root.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (_root == null) return;
            _root.SetActive(visible);
            if (visible)
            {
                RefreshViewToggle(Proto_GreyboxViewModeState.Is3D);
                _root.transform.SetAsLastSibling();
            }
        }

        public static bool IsPauseToggleHit(float xPct, float yPct) =>
            Proto_GreyboxViewModeState.IsPersp2D && Proto_GreyboxPersp2DHud.IsApplied
                ? Proto_GreyboxPersp2DHudLayout.PauseHit(xPct, yPct)
                : Proto_Greybox3DHudLayout.Enabled
                    ? Proto_Greybox3DHudLayout.PauseHit(xPct, yPct)
                    : xPct >= PauseHitX0 && xPct <= PauseHitX1 && yPct >= PauseHitY0 && yPct <= PauseHitY1;

        public static bool IsResumeHit(float xPct, float yPct) =>
            xPct >= ResumeHitX0 && xPct <= ResumeHitX1 && yPct >= ResumeHitY0 && yPct <= ResumeHitY1;

        public static bool IsPauseSettingsHit(float xPct, float yPct) =>
            xPct >= PauseSettingsHitX0 && xPct <= PauseSettingsHitX1 && yPct >= PauseSettingsHitY0
            && yPct <= PauseSettingsHitY1;

        public static bool IsPauseRestartHit(float xPct, float yPct) =>
            xPct >= PauseRestartHitX0 && xPct <= PauseRestartHitX1 && yPct >= PauseRestartHitY0
            && yPct <= PauseRestartHitY1;

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void PlaceImage(Transform parent, string name, float x0, float y0, float x1, float y1, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);
            var img = go.AddComponent<Image>();
            img.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            img.color = color;
            img.raycastTarget = false;
        }

        static Image PlaceButtonPlate(Transform parent, string name, float x0, float y0, float x1, float y1,
            Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);
            var img = go.AddComponent<Image>();
            img.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            img.color = color ?? new Color(0.22f, 0.45f, 0.72f, 0.85f);
            img.raycastTarget = false;
            return img;
        }

        public static bool IsViewToggleHit(float xPct, float yPct) =>
            xPct >= ViewToggleHitX0 && xPct <= ViewToggleHitX1 && yPct >= ViewToggleHitY0 && yPct <= ViewToggleHitY1;

        public void RefreshViewToggle(bool is3D)
        {
            RefreshViewToggle(is3D
                ? Proto_GreyboxViewMode.View3D
                : (Proto_GreyboxViewModeState.IsChibi ? Proto_GreyboxViewMode.View2DChibi : Proto_GreyboxViewMode.View2D));
        }

        /// <summary>Three-way style toggle label: 2D pixel / 3D / 2D chibi.</summary>
        public void RefreshViewToggle(Proto_GreyboxViewMode mode)
        {
            if (_viewLabel != null)
            {
                Proto_GreyboxHudBuilder.GetUiFont(out bool hangul);
                _viewLabel.text = Proto_GreyboxViewModeState.Label(mode, hangul);
            }
            if (_viewPlate != null)
                _viewPlate.color = Proto_GreyboxViewModeState.PlateColor(mode);
        }

        static Text PlaceText(Transform parent, string name, float x0, float y0, float x1, float y1, Font font,
            int size, TextAnchor align, Color color, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);
            var label = go.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.alignment = align;
            label.color = color;
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }
    }
}
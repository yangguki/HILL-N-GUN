using System;
using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    public sealed class Proto_GreyboxSettingsUi
    {
        public const string RootName = "Greybox_SettingsOverlay";
        public const float BulletSpeedMin = 2f;
        public const float BulletSpeedMax = 12f;
        public const float BulletSpeedNudge = 0.5f;
        public const float CloseHitX0 = 38f;
        public const float CloseHitX1 = 62f;
        public const float CloseHitY0 = 70f;
        public const float CloseHitY1 = 76f;
        public const float ViewToggleHitX0 = 32f;
        public const float ViewToggleHitX1 = 68f;
        public const float ViewToggleHitY0 = 62f;
        public const float ViewToggleHitY1 = 69f;

        GameObject _root;
        Slider _allySlider;
        Slider _enemySlider;
        Text _allyValue;
        Text _enemyValue;
        Text _viewLabel;
        Image _viewPlate;
        Proto_GreyboxRuntimeTuning _tuning;
        Action _onClose;

        public bool IsBuilt => _root != null;
        public bool IsVisible => _root != null && _root.activeSelf;

        public void Clear()
        {
            if (_root == null) return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_root);
            else
                UnityEngine.Object.DestroyImmediate(_root);
            _root = null;
            _allySlider = null;
            _enemySlider = null;
            _viewLabel = null;
            _viewPlate = null;
        }

        public void Ensure(Transform canvasTransform, Proto_GreyboxRuntimeTuning tuning, Action onClose)
        {
            _tuning = tuning;
            _onClose = onClose;
            if (_root != null)
            {
                SyncSlidersFromTuning();
                return;
            }

            if (canvasTransform == null) return;
            Proto_GreyboxUiEventSystem.Ensure();
            var font = Proto_GreyboxHudBuilder.GetUiFont(out bool hangul);

            _root = new GameObject(RootName);
            _root.transform.SetParent(canvasTransform, false);
            var rootRt = _root.AddComponent<RectTransform>();
            StretchFull(rootRt);
            PlaceImage(_root.transform, "Greybox_SettingsDim", 0f, 0f, 100f, 100f,
                new Color(0.04f, 0.05f, 0.07f, 0.55f));
            PlaceImage(_root.transform, "Greybox_SettingsCard", 24f, 20f, 76f, 80f,
                new Color(16f / 255f, 18f / 255f, 26f / 255f, 0.96f));

            string title = hangul ? "설정" : "SETTINGS";
            string allyLabel = hangul ? "내 총알 속도" : "Ally bullet speed";
            string enemyLabel = hangul ? "적 총알 속도" : "Enemy bullet speed";
            string closeLabel = hangul ? "닫기" : "CLOSE";

            PlaceText(_root.transform, "Greybox_SettingsTitle", 30f, 26f, 70f, 34f, font, 26,
                TextAnchor.MiddleCenter, Color.white, title);
            BuildSliderRow(_root.transform, "AllyRow", 32f, 40f, 68f, 49f, allyLabel, font,
                BulletSpeedMin, BulletSpeedMax, out _allySlider, out _allyValue, v => ApplyAlly(v),
                () => NudgeAlly(BulletSpeedNudge), () => NudgeAlly(-BulletSpeedNudge));
            BuildSliderRow(_root.transform, "EnemyRow", 32f, 52f, 68f, 61f, enemyLabel, font,
                BulletSpeedMin, BulletSpeedMax, out _enemySlider, out _enemyValue, v => ApplyEnemy(v),
                () => NudgeEnemy(BulletSpeedNudge), () => NudgeEnemy(-BulletSpeedNudge));
            _viewPlate = PlaceButtonPlate(_root.transform, "Greybox_SettingsViewPlate", ViewToggleHitX0,
                ViewToggleHitY0, ViewToggleHitX1, ViewToggleHitY1, true,
                new Color(0.55f, 0.22f, 0.72f, 0.95f));
            _viewLabel = PlaceText(_root.transform, "Greybox_SettingsViewLabel", ViewToggleHitX0, ViewToggleHitY0,
                ViewToggleHitX1, ViewToggleHitY1, font, 22, TextAnchor.MiddleCenter, Color.white, "2D  /  3D");
            PlaceButtonPlate(_root.transform, "Greybox_SettingsClosePlate", CloseHitX0, CloseHitY0, CloseHitX1,
                CloseHitY1, true);
            PlaceText(_root.transform, "Greybox_SettingsCloseLabel", CloseHitX0, CloseHitY0, CloseHitX1,
                CloseHitY1, font, 20, TextAnchor.MiddleCenter, Color.white, closeLabel);

            foreach (var t in _root.GetComponentsInChildren<Text>(true))
            {
                t.raycastTarget = false;
                t.supportRichText = false;
            }

            SyncSlidersFromTuning();
            _root.SetActive(false);
        }

        public void NudgeAlly(float delta)
        {
            if (_tuning == null) return;
            ApplyAlly(Mathf.Clamp(_tuning.AllyBulletSpeed + delta, BulletSpeedMin, BulletSpeedMax));
        }

        public void NudgeEnemy(float delta)
        {
            if (_tuning == null) return;
            ApplyEnemy(Mathf.Clamp(_tuning.EnemyBulletSpeed + delta, BulletSpeedMin, BulletSpeedMax));
        }

        public void SetAllyBulletSpeed(float value) => ApplyAlly(Mathf.Clamp(value, BulletSpeedMin, BulletSpeedMax));

        public void SetEnemyBulletSpeed(float value) =>
            ApplyEnemy(Mathf.Clamp(value, BulletSpeedMin, BulletSpeedMax));

        void ApplyAlly(float v)
        {
            v = Mathf.Round(v * 10f) / 10f;
            _tuning.AllyBulletSpeed = v;
            if (_allySlider != null && Mathf.Abs(_allySlider.value - v) > 0.001f)
                _allySlider.SetValueWithoutNotify(v);
            if (_allyValue != null)
                _allyValue.text = v.ToString("0.0");
            Proto_GreyboxBulletSpeedPrefs.SaveAlly(v);
        }

        void ApplyEnemy(float v)
        {
            v = Mathf.Round(v * 10f) / 10f;
            _tuning.EnemyBulletSpeed = v;
            if (_enemySlider != null && Mathf.Abs(_enemySlider.value - v) > 0.001f)
                _enemySlider.SetValueWithoutNotify(v);
            if (_enemyValue != null)
                _enemyValue.text = v.ToString("0.0");
            Proto_GreyboxBulletSpeedPrefs.SaveEnemy(v);
        }

        void SyncSlidersFromTuning()
        {
            if (_tuning == null) return;
            if (_allySlider != null)
            {
                _allySlider.SetValueWithoutNotify(_tuning.AllyBulletSpeed);
                if (_allyValue != null)
                    _allyValue.text = _tuning.AllyBulletSpeed.ToString("0.0");
            }

            if (_enemySlider != null)
            {
                _enemySlider.SetValueWithoutNotify(_tuning.EnemyBulletSpeed);
                if (_enemyValue != null)
                    _enemyValue.text = _tuning.EnemyBulletSpeed.ToString("0.0");
            }
        }

        public void SetVisible(bool visible)
        {
            if (_root == null) return;
            _root.SetActive(visible);
            if (visible)
            {
                Proto_GreyboxUiEventSystem.Ensure();
                SyncSlidersFromTuning();
                RefreshViewToggle(Proto_GreyboxViewModeState.Is3D);
                _root.transform.SetAsLastSibling();
            }
        }

        public static bool IsCloseHit(float xPct, float yPct) =>
            xPct >= CloseHitX0 && xPct <= CloseHitX1 && yPct >= CloseHitY0 && yPct <= CloseHitY1;

        public bool TryCloseFromTap(float xPct, float yPct)
        {
            if (!IsVisible) return false;
            if (!IsCloseHit(xPct, yPct)) return false;
            _onClose?.Invoke();
            return true;
        }

        static void BuildSliderRow(Transform parent, string name, float x0, float y0, float x1, float y1,
            string label, Font font, float min, float max, out Slider slider, out Text valueText,
            Action<float> onChanged, Action onPlus, Action onMinus)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(labelRt, 0f, 55f, 100f, 100f);
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = font;
            labelText.fontSize = 15;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = new Color(0.94f, 0.94f, 0.97f, 1f);
            labelText.text = label;
            labelText.raycastTarget = false;

            var valGo = new GameObject("Value");
            valGo.transform.SetParent(row.transform, false);
            var valRt = valGo.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(valRt, 44f, 55f, 62f, 100f);
            valueText = valGo.AddComponent<Text>();
            valueText.font = font;
            valueText.fontSize = 15;
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.color = new Color(1f, 0.9f, 0.45f, 1f);
            valueText.raycastTarget = false;

            PlaceNudgeButton(row.transform, "Minus", 64f, 8f, 72f, 48f, font, "−", onMinus);
            PlaceNudgeButton(row.transform, "Plus", 74f, 8f, 82f, 48f, font, "+", onPlus);

            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(row.transform, false);
            var sliderRt = sliderGo.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(sliderRt, 0f, 0f, 62f, 52f);
            var sliderBg = sliderGo.AddComponent<Image>();
            sliderBg.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            sliderBg.color = new Color(0.12f, 0.13f, 0.18f, 0.95f);
            sliderBg.raycastTarget = true;
            slider = sliderGo.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.direction = Slider.Direction.LeftToRight;
            slider.interactable = true;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillArea.AddComponent<RectTransform>();
            StretchFull(fillAreaRt);
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            StretchFull(fillRt);
            var fill = fillGo.AddComponent<Image>();
            fill.color = new Color(0.35f, 0.75f, 1f, 0.9f);
            fill.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            fill.raycastTarget = false;
            slider.fillRect = fillRt;

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGo.transform, false);
            var handleAreaRt = handleArea.AddComponent<RectTransform>();
            StretchFull(handleAreaRt);
            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.AddComponent<RectTransform>();
            handleRt.anchorMin = new Vector2(0.5f, 0.5f);
            handleRt.anchorMax = new Vector2(0.5f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(14f, 22f);
            var handleImg = handle.AddComponent<Image>();
            handleImg.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            handleImg.color = Color.white;
            handleImg.raycastTarget = true;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;

            slider.onValueChanged.AddListener(v => onChanged(v));
        }

        static void PlaceNudgeButton(Transform parent, string name, float x0, float y0, float x1, float y1, Font font,
            string glyph, Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);
            var img = go.AddComponent<Image>();
            img.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            img.color = new Color(0.22f, 0.45f, 0.72f, 0.9f);
            img.raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(() => onClick?.Invoke());

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            StretchFull(labelRt);
            var label = labelGo.AddComponent<Text>();
            label.font = font;
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = glyph;
            label.raycastTarget = false;
        }

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
            bool raycast, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);
            var img = go.AddComponent<Image>();
            img.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            img.color = color ?? new Color(0.22f, 0.45f, 0.72f, 0.85f);
            img.raycastTarget = raycast;
            return img;
        }

        public bool IsViewToggleHit(float xPct, float yPct) =>
            xPct >= ViewToggleHitX0 && xPct <= ViewToggleHitX1 && yPct >= ViewToggleHitY0 && yPct <= ViewToggleHitY1;

        public void RefreshViewToggle(bool is3D)
        {
            if (_viewLabel != null)
                _viewLabel.text = is3D ? "View:  3D" : "View:  2D";
            if (_viewPlate != null)
                _viewPlate.color = is3D
                    ? new Color(0.55f, 0.22f, 0.72f, 0.95f)
                    : new Color(0.22f, 0.45f, 0.72f, 0.95f);
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
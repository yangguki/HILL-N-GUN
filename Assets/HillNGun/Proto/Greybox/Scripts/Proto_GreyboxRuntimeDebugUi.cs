using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxRuntimeDebugUi : MonoBehaviour
    {
        Proto_GreyboxRuntimeTuning _tuning;
        Proto_GreyboxPlayerController _controller;
        GameObject _panelRoot;
        Text _header;

        public bool IsOpen { get; private set; }

        public static Proto_GreyboxRuntimeDebugUi Ensure(Canvas canvas, Proto_GreyboxRuntimeTuning tuning,
            Proto_GreyboxPlayerController controller)
        {
            var existing = canvas.GetComponentInChildren<Proto_GreyboxRuntimeDebugUi>(true);
            if (existing != null)
            {
                existing._tuning = tuning;
                existing._controller = controller;
                return existing;
            }

            var go = new GameObject("Greybox_DebugPanel");
            go.transform.SetParent(canvas.transform, false);
            var ui = go.AddComponent<Proto_GreyboxRuntimeDebugUi>();
            ui._tuning = tuning;
            ui._controller = controller;
            ui.Build(canvas.transform);
            ui.SetOpen(false);
            go.SetActive(true);
            return ui;
        }

        void Build(Transform parent)
        {
            _panelRoot = new GameObject("Greybox_DebugPanel_Root");
            _panelRoot.transform.SetParent(parent, false);
            var rt = _panelRoot.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, 1f, 4f, 44f, 96f);
            var bg = _panelRoot.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.88f);
            bg.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            float y = 1f;
            _header = AddLabel(_panelRoot.transform, "TUNING (F1)", 1f, y, 42f, y + 4f, font, 12);
            y += 6f;

            AddSliderRow(_panelRoot.transform, ref y, "Move speed", 1f, 12f, _tuning.MoveSpeed,
                v => _tuning.MoveSpeed = v);
            AddSliderRow(_panelRoot.transform, ref y, "Roll dist", 0.5f, 5f, _tuning.RollDistance,
                v => _tuning.RollDistance = v);
            AddSliderRow(_panelRoot.transform, ref y, "Roll time", 0.1f, 1f, _tuning.RollDuration,
                v => _tuning.RollDuration = v);
            AddSliderRow(_panelRoot.transform, ref y, "Roll invuln", 0.05f, 0.8f, _tuning.RollInvulnDuration,
                v => _tuning.RollInvulnDuration = v);
            AddSliderRow(_panelRoot.transform, ref y, "Roll cd", 0.2f, 5f, _tuning.RollCooldown,
                v => _tuning.RollCooldown = v);
            AddSliderRow(_panelRoot.transform, ref y, "Enemy bullet spd", 1f, 10f, _tuning.EnemyBulletSpeed,
                v => _tuning.EnemyBulletSpeed = v);
            AddSliderRow(_panelRoot.transform, ref y, "Enemy bullet size", 0.2f, 1.2f, _tuning.EnemyBulletDiameter,
                v => _tuning.EnemyBulletDiameter = v);
            AddSliderRow(_panelRoot.transform, ref y, "Ally bullet spd", 1f, 24f, _tuning.AllyBulletSpeed,
                v => _tuning.AllyBulletSpeed = v);
            AddSliderRow(_panelRoot.transform, ref y, "Ally bullet size", 0.05f, 0.4f, _tuning.AllyBulletDiameter,
                v => _tuning.AllyBulletDiameter = v);
            AddSliderRow(_panelRoot.transform, ref y, "Ally bullet dmg", 1f, 10f, _tuning.AllyBulletDamage,
                v => _tuning.AllyBulletDamage = Mathf.RoundToInt(v));
            AddSliderRow(_panelRoot.transform, ref y, "Fire interval", 0.1f, 1f, _tuning.FireInterval,
                v => _tuning.FireInterval = v);
            AddSliderRow(_panelRoot.transform, ref y, "Magazine", 1f, 12f, _tuning.MagazineSize,
                v => _tuning.MagazineSize = Mathf.RoundToInt(v));
            AddSliderRow(_panelRoot.transform, ref y, "Reload time", 0.3f, 4f, _tuning.ReloadDuration,
                v => _tuning.ReloadDuration = v);
            AddSliderRow(_panelRoot.transform, ref y, "Wave1 count", 0f, 12f, _tuning.Wave1Count,
                v => _tuning.Wave1Count = Mathf.RoundToInt(v));
            AddSliderRow(_panelRoot.transform, ref y, "Wave2 count", 0f, 12f, _tuning.Wave2Count,
                v => _tuning.Wave2Count = Mathf.RoundToInt(v));
            AddSliderRow(_panelRoot.transform, ref y, "Wave3 count", 0f, 12f, _tuning.Wave3Count,
                v => _tuning.Wave3Count = Mathf.RoundToInt(v));
            AddSliderRow(_panelRoot.transform, ref y, "Enemy fire int", 0.4f, 4f, _tuning.EnemyFireInterval,
                v => _tuning.EnemyFireInterval = v);
            AddSliderRow(_panelRoot.transform, ref y, "Enemy HP", 1f, 10f, _tuning.EnemyHp,
                v => _tuning.EnemyHp = Mathf.RoundToInt(v));
            AddSliderRow(_panelRoot.transform, ref y, "Player max HP", 1f, 50f, _tuning.PlayerMaxHp,
                v => _tuning.PlayerMaxHp = Mathf.RoundToInt(v));
            AddSliderRow(_panelRoot.transform, ref y, "Grenade cd", 1f, 15f, _tuning.GrenadeCooldown,
                v => _tuning.GrenadeCooldown = v);
            AddSliderRow(_panelRoot.transform, ref y, "Cover dmg reduce%", 0f, 100f, _tuning.CoverDamageReductionPercent,
                v => _tuning.CoverDamageReductionPercent = v);
            AddSliderRow(_panelRoot.transform, ref y, "Depth far scale", 0.5f, 2f, _tuning.DepthFarScale,
                v => _tuning.DepthFarScale = v);
            AddSliderRow(_panelRoot.transform, ref y, "Depth near scale", 1f, 4f, _tuning.DepthNearScale,
                v => _tuning.DepthNearScale = v);
        }

        static Text AddLabel(Transform parent, string text, float x0, float y0, float x1, float y1, Font font, int size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);
            var label = go.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        void AddSliderRow(Transform parent, ref float y, string name, float min, float max, float value,
            System.Action<float> setter)
        {
            var row = new GameObject("Row_" + name);
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, 1f, y, 42f, y + 3.6f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(row.transform, false);
            var nameRt = nameGo.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(nameRt, 0f, 0f, 58f, 42f);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = font;
            nameText.fontSize = 10;
            nameText.text = name;
            nameText.color = new Color(0.92f, 0.92f, 0.95f, 1f);
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameText.verticalOverflow = VerticalWrapMode.Overflow;
            nameText.raycastTarget = false;

            var valGo = new GameObject("Value");
            valGo.transform.SetParent(row.transform, false);
            var valRt = valGo.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(valRt, 58f, 0f, 100f, 42f);
            var valText = valGo.AddComponent<Text>();
            valText.font = font;
            valText.fontSize = 10;
            valText.alignment = TextAnchor.MiddleRight;
            valText.color = new Color(1f, 0.9f, 0.4f, 1f);
            valText.horizontalOverflow = HorizontalWrapMode.Overflow;
            valText.verticalOverflow = VerticalWrapMode.Overflow;
            valText.raycastTarget = false;
            valText.text = value.ToString("0.##");

            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(row.transform, false);
            var sliderRt = sliderGo.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(sliderRt, 0f, 48f, 100f, 100f);
            var sliderBg = sliderGo.AddComponent<Image>();
            sliderBg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
            sliderBg.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.direction = Slider.Direction.LeftToRight;
            var fillArea = new GameObject("Fill");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillRt = fillArea.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(fillRt, 4f, 20f, 96f, 80f);
            var fill = fillArea.AddComponent<Image>();
            fill.color = new Color(0.3f, 0.7f, 1f, 0.9f);
            fill.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            slider.fillRect = fillRt;
            var handle = new GameObject("Handle");
            handle.transform.SetParent(sliderGo.transform, false);
            var handleRt = handle.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(8f, 8f);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            handleImg.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;

            slider.onValueChanged.AddListener(v =>
            {
                setter(v);
                valText.text = v.ToString("0.##");
            });

            y += 3.85f;
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (_panelRoot == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    var hud = GameObject.Find("Greybox_Hud");
                    canvas = hud != null ? hud.GetComponent<Canvas>() : null;
                }

                if (canvas != null)
                    Build(canvas.transform);
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(open);
                if (open)
                {
                    _panelRoot.transform.SetAsLastSibling();
                    var canvas = _panelRoot.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        if (canvas.worldCamera == null)
                            canvas.worldCamera = Camera.main;
                        if (canvas.planeDistance < 0.5f)
                            canvas.planeDistance = 1f;
                    }
                }
            }
        }

        public void Toggle() => SetOpen(!IsOpen);
    }
}
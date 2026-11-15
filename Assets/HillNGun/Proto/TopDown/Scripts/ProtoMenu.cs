using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HillNGun.Proto.TopDown
{
    /// <summary>First proto screen: pick free-move PvE or front-line 1v1 / 3v3.</summary>
    public class ProtoMenu : MonoBehaviour
    {
        RectTransform _safe;
        RectTransform _titleRt, _pveRt, _oneRt, _threeRt;
        Text _title;
        Canvas _canvas;
        Rect _safeSeen;
        Vector2Int _screenSeen;

        void Awake()
        {
#if !UNITY_EDITOR
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
#endif
            if (GetComponent<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            BuildUi();
        }

        void Update()
        {
            if (_safe != null && (Screen.safeArea != _safeSeen || new Vector2Int(Screen.width, Screen.height) != _screenSeen))
                ApplyLayout();
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ProtoSafeLayout.Reference;
            scaler.matchWidthOrHeight = ProtoSafeLayout.Match;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
            }

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_canvas.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.18f, 0.28f, 0.36f, 1f);

            var safeGo = new GameObject("Safe", typeof(RectTransform));
            safeGo.transform.SetParent(_canvas.transform, false);
            _safe = safeGo.GetComponent<RectTransform>();
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;

            _titleRt = MakeText(_safe, "Title", out _title, 54, TextAnchor.MiddleCenter);
            _title.text = "HNG Proto";
            _pveRt = Button(_safe, "자유 이동 PvE", new Color(0.28f, 0.62f, 0.42f), () => ProtoNav.OpenTopDown());
            _oneRt = Button(_safe, "전선 PvP 1v1", new Color(0.78f, 0.42f, 0.22f), () => ProtoNav.OpenFrontline(false));
            _threeRt = Button(_safe, "전선 PvP 3v3", new Color(0.72f, 0.28f, 0.28f), () => ProtoNav.OpenFrontline(true));
            ApplyLayout();
        }

        void ApplyLayout()
        {
            if (_safe == null) return;
            _safeSeen = Screen.safeArea;
            _screenSeen = new Vector2Int(Screen.width, Screen.height);
            float sw = Mathf.Max(1, Screen.width);
            float sh = Mathf.Max(1, Screen.height);
            var safe = _safeSeen;
            if (safe.width < 2f || safe.height < 2f) safe = new Rect(0f, 0f, sw, sh);
            _safe.anchorMin = new Vector2(safe.x / sw, safe.y / sh);
            _safe.anchorMax = new Vector2((safe.x + safe.width) / sw, (safe.y + safe.height) / sh);
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
            float min = ProtoSafeLayout.MinButtonCanvas(sw, sh, Screen.dpi);
            float panelW = safe.width / Mathf.Max(0.01f, _canvas != null && _canvas.scaleFactor > 0.01f
                ? _canvas.scaleFactor
                : ProtoSafeLayout.CanvasScale(sw, sh));
            float btnH = Mathf.Max(min, 110f);
            float btnW = Mathf.Clamp(panelW * 0.55f, min * 3.2f, panelW - 40f);
            float gap = Mathf.Max(14f, btnH * 0.18f);
            float titleH = Mathf.Max(64f, btnH * 0.7f);
            float stack = titleH + gap + btnH * 3f + gap * 2f;
            // Center anchors so the stack stays visually centered regardless of scaleFactor / safe-area timing.
            float top = stack * 0.5f;
            PlaceCenter(_titleRt, top - titleH * 0.5f, btnW, titleH);
            PlaceCenter(_pveRt, top - titleH - gap - btnH * 0.5f, btnW, btnH);
            PlaceCenter(_oneRt, top - titleH - gap - btnH - gap - btnH * 0.5f, btnW, btnH);
            PlaceCenter(_threeRt, top - titleH - gap - (btnH + gap) * 2f - btnH * 0.5f, btnW, btnH);
            int font = Mathf.Clamp(Mathf.RoundToInt(btnH * 0.28f), 28, 48);
            SetFont(_pveRt, font);
            SetFont(_oneRt, font);
            SetFont(_threeRt, font);
            if (_title != null) _title.fontSize = Mathf.Clamp(font + 10, 36, 64);
        }

        static void PlaceCenter(RectTransform rt, float localY, float width, float height)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, localY);
            rt.sizeDelta = new Vector2(width, height);
        }

        static void SetFont(RectTransform rt, int size)
        {
            if (rt == null) return;
            var text = rt.GetComponentInChildren<Text>();
            if (text != null) text.fontSize = size;
        }

        static RectTransform MakeText(Transform parent, string name, out Text text, int size, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return go.GetComponent<RectTransform>();
        }

        static RectTransform Button(Transform parent, string label, Color color, System.Action onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(ProtoControls.HoldButton));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0f, 0f);
            go.GetComponent<Image>().color = color;
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 32;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
            go.GetComponent<ProtoControls.HoldButton>().Pressed = down =>
            {
                if (down) onClick?.Invoke();
            };
            return rt;
        }
    }
}

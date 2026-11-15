using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using HillNGun.Characters;
using HillNGun.Data;
using HillNGun.Input;

namespace HillNGun.Proto.TopDown
{
    /// <summary>
    /// Twin-stick: left stick / WASD moves, mouse or right stick aims. FIRE is one shot along that aim
    /// (buster holdToRepeat is off, fireInterval is respected). No nearest-enemy snap.
    /// </summary>
    public class ProtoControls : MonoBehaviour
    {
        public Vector2 Facing { get; private set; } = Vector2.up;
        public bool GrenadePreview { get; private set; }
        public Vector2 GrenadeTarget { get; private set; }
        public bool Blocked { get; set; }
        public System.Action OnSkinPressed;

        CharacterStateMachine _player;
        GrenadeData _grenade;
        System.Func<Vector2> _pointerLogic;
        float _lastShot = -999f;
        bool _fireHeld, _bombHeld, _rollHeld;
        bool _aimStick;
        Vector2 _moveStick, _aimStickDir;
        Rect _safeSeen;
        Vector2Int _screenSeen;

        Stick _move;
        Stick _aim;
        RectTransform _safe;
        RectTransform _fireRt, _bombRt, _rollRt, _skinRt, _hudRt, _endRt, _resultRt, _restartRt, _menuRt;
        Text _hud, _result;
        Canvas _canvas;

        public void Bind(CharacterStateMachine player, GrenadeData grenade, System.Func<Vector2> pointerLogic)
        {
            _player = player;
            _grenade = grenade;
            _pointerLogic = pointerLogic;
        }

        public void BuildUi(System.Action skin, System.Action restart, bool showSkin = true, string modeLabel = null, System.Action mode = null, System.Action menu = null)
        {
            OnSkinPressed = skin;
            var canvasGo = new GameObject("ProtoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ProtoSafeLayout.Reference;
            scaler.matchWidthOrHeight = ProtoSafeLayout.Match;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
            }

            var safeGo = new GameObject("Safe", typeof(RectTransform));
            safeGo.transform.SetParent(_canvas.transform, false);
            _safe = safeGo.GetComponent<RectTransform>();
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;

            _move = Stick.Create(_safe, false, v => _moveStick = v);
            _aim = Stick.Create(_safe, true, v =>
            {
                _aimStick = v.sqrMagnitude > 0.04f;
                _aimStickDir = v;
            });
            _fireRt = Button(_safe, "사격", new Color(0.86f, 0.28f, 0.26f), down => _fireHeld = down);
            _bombRt = Button(_safe, "폭탄", new Color(0.25f, 0.48f, 0.86f), down => _bombHeld = down);
            _rollRt = Button(_safe, "구르기", new Color(0.75f, 0.58f, 0.28f), down => _rollHeld = down);
            if (mode != null)
                _skinRt = Button(_safe, string.IsNullOrEmpty(modeLabel) ? "모드" : modeLabel, new Color(0.18f, 0.2f, 0.24f), down =>
                {
                    if (down) mode();
                });
            else
                _skinRt = Button(_safe, "스킨", new Color(0.18f, 0.2f, 0.24f), down =>
                {
                    if (down) OnSkinPressed?.Invoke();
                });
            if (!showSkin && mode == null) _skinRt.gameObject.SetActive(false);
            _hudRt = MakeText(_safe, "Hud", out _hud, 36, TextAnchor.MiddleLeft);
            _endRt = MakeOverlay(_safe, restart, menu ?? ProtoNav.OpenMenu, out _resultRt, out _result, out _restartRt, out _menuRt);
            ApplyLayout();
        }

        public void SetHud(int hp, int maxHp, float seconds)
        {
            if (_hud == null) return;
            int t = Mathf.Max(0, Mathf.FloorToInt(seconds));
            _hud.text = "HP " + hp + "/" + maxHp + "    " + (t / 60) + ":" + (t % 60).ToString("00");
        }

        public void SetStatus(string text)
        {
            if (_hud != null) _hud.text = text;
        }

        public void SetModeLabel(string label)
        {
            if (_skinRt == null) return;
            var text = _skinRt.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }

        public void SetEnded(string result)
        {
            if (_endRt == null) return;
            bool show = !string.IsNullOrEmpty(result);
            if (_endRt.gameObject.activeSelf != show) _endRt.gameObject.SetActive(show);
            if (show && _result != null) _result.text = result;
        }

        void Update()
        {
            if (_safe != null && (Screen.safeArea != _safeSeen || new Vector2Int(Screen.width, Screen.height) != _screenSeen))
                ApplyLayout();
            if (_player == null || Blocked) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool touch = AnyTouch();
            Vector2 move = _moveStick;
            if (kb != null)
            {
                Vector2 keys = Vector2.zero;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) keys.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) keys.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) keys.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) keys.y += 1f;
                if (keys.sqrMagnitude > move.sqrMagnitude) move = keys;
            }
            if (move.sqrMagnitude > 1f) move.Normalize();
            float mag = move.magnitude;
            var dir = mag > 0.01f ? move / mag : Vector2.zero;
            _player.OnMoveCommand(new MoveCommand(dir, Mathf.Clamp01(mag), Time.time));

            if (_aimStick) Facing = _aimStickDir.normalized;
            else if (!touch && mouse != null && _pointerLogic != null && !PointerOverUi())
            {
                Vector2 logic = _pointerLogic();
                Vector2 to = logic - _player.Position;
                if (to.sqrMagnitude > 0.04f) Facing = to.normalized;
            }
            else if (mag > 0.2f) Facing = dir;

            bool overUi = PointerOverUi();
            bool fireDown = _fireHeld || (kb != null && kb.jKey.isPressed) || (!touch && !overUi && mouse != null && mouse.leftButton.isPressed);
            bool bombDown = _bombHeld || (kb != null && kb.kKey.isPressed) || (!touch && !overUi && mouse != null && mouse.rightButton.isPressed);
            bool rollDown = _rollHeld || (kb != null && kb.spaceKey.isPressed);

            if (fireDown) TryFire();
            else _fireEdge = false;

            if (bombDown && !_bombEdge) BeginGrenade();
            if (!bombDown && _bombEdge) ReleaseGrenade();
            _bombEdge = bombDown;
            if (GrenadePreview) GrenadeTarget = ClampTarget(_player.Position + Facing * PreferredThrow());

            if (rollDown && !_rollEdge)
                _player.OnRollCommand(new RollCommand(mag > 0.1f ? dir : Facing, Time.time));
            _rollEdge = rollDown;

            if (kb != null && kb.f1Key.wasPressedThisFrame) OnSkinPressed?.Invoke();
        }

        bool _fireEdge, _bombEdge, _rollEdge;

        void TryFire()
        {
            if (_fireEdge) return;
            _fireEdge = true;
            var weapon = _player.Data != null ? _player.Data.weapon : null;
            float interval = weapon != null ? weapon.fireInterval : 0.6f;
            if (Time.time - _lastShot < interval) return;
            _lastShot = Time.time;
            float now = Time.time;
            _player.OnAimStartCommand(new AimStartCommand(Vector2.zero, now));
            _player.OnAimDragCommand(new AimDragCommand(Facing, 1f, true, now));
            _player.OnAimReleaseCommand(new AimReleaseCommand(Facing, true, now));
        }

        void BeginGrenade()
        {
            GrenadePreview = true;
            GrenadeTarget = ClampTarget(_player.Position + Facing * PreferredThrow());
            _player.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, Time.time));
            _player.OnGrenadeAimDragCommand(new GrenadeAimDragCommand(Facing, PreferredThrow(), Time.time));
        }

        void ReleaseGrenade()
        {
            GrenadePreview = false;
            var target = ClampTarget(_player.Position + Facing * PreferredThrow());
            _player.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(target, true, Time.time));
        }

        float PreferredThrow()
        {
            if (_grenade == null) return 5f;
            return Mathf.Lerp(_grenade.minRange, _grenade.maxRange, 0.45f);
        }

        static Vector2 ClampTarget(Vector2 p)
        {
            return new Vector2(Mathf.Clamp(p.x, ProtoArena.MinX + 0.4f, ProtoArena.MaxX - 0.4f),
                Mathf.Clamp(p.y, ProtoArena.MinY + 0.4f, ProtoArena.MaxY - 0.4f));
        }

        static bool PointerOverUi()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            return es.IsPointerOverGameObject();
        }

        static bool AnyTouch()
        {
            var screen = Touchscreen.current;
            if (screen == null) return false;
            for (int i = 0; i < screen.touches.Count; i++)
                if (screen.touches[i].press.isPressed) return true;
            return false;
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
            float scale = _canvas != null && _canvas.scaleFactor > 0.01f
                ? _canvas.scaleFactor
                : ProtoSafeLayout.CanvasScale(sw, sh);
            float min = ProtoSafeLayout.MinButtonCanvas(sw, sh, Screen.dpi);
            var slots = ProtoSafeLayout.Place(safe.width / scale, safe.height / scale, min);
            float panelW = safe.width / scale;
            float panelH = safe.height / scale;
            Place(_move.Root, slots.Move);
            Place(_aim.Root, slots.Aim);
            Place(_fireRt, slots.Fire);
            Place(_bombRt, slots.Bomb);
            Place(_rollRt, slots.Roll);
            // Skin / mode: top-right corner of the safe area (HUD, not mid-dirt).
            PlaceTopRight(_skinRt, slots.Skin, panelW, panelH);
            PlaceTopLeft(_hudRt, slots.Hud, panelH);
            if (_endRt != null)
            {
                _endRt.anchorMin = Vector2.zero;
                _endRt.anchorMax = Vector2.one;
                _endRt.offsetMin = _endRt.offsetMax = Vector2.zero;
            }
            // End-result stack: center anchors so the panel stays visually centered in the safe area.
            PlaceCenter(_resultRt, slots.Result, panelW, panelH);
            PlaceCenter(_restartRt, slots.Restart, panelW, panelH);
            PlaceCenter(_menuRt, slots.Menu, panelW, panelH);
            int font = Mathf.Clamp(Mathf.RoundToInt(slots.Fire.height * 0.22f), 22, 48);
            SetFont(_fireRt, font);
            SetFont(_bombRt, font);
            SetFont(_rollRt, font);
            SetFont(_skinRt, font);
            SetFont(_restartRt, font);
            SetFont(_menuRt, font);
            if (_hud != null) _hud.fontSize = Mathf.Clamp(Mathf.RoundToInt(slots.Hud.height * 0.62f), 28, 56);
            if (_result != null) _result.fontSize = Mathf.Clamp(font + 16, 36, 72);
        }

        static void Place(RectTransform rt, Rect box)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(box.x, box.y);
            rt.sizeDelta = new Vector2(box.width, box.height);
        }

        static void PlaceTopRight(RectTransform rt, Rect box, float panelW, float panelH)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(box.xMax - panelW, box.yMax - panelH);
            rt.sizeDelta = new Vector2(box.width, box.height);
        }

        static void PlaceTopLeft(RectTransform rt, Rect box, float panelH)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(box.x, box.yMax - panelH);
            rt.sizeDelta = new Vector2(box.width, box.height);
        }

        static void PlaceCenter(RectTransform rt, Rect box, float panelW, float panelH)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(box.x + box.width * 0.5f - panelW * 0.5f, box.y + box.height * 0.5f - panelH * 0.5f);
            rt.sizeDelta = new Vector2(box.width, box.height);
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

        static RectTransform MakeOverlay(Transform parent, System.Action restart, System.Action menu, out RectTransform resultRt, out Text result, out RectTransform restartRt, out RectTransform menuRt)
        {
            var go = new GameObject("End", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.62f);
            resultRt = MakeText(go.transform, "Result", out result, 52, TextAnchor.MiddleCenter);
            restartRt = Button(go.transform, "다시하기", new Color(0.9f, 0.55f, 0.2f), down =>
            {
                if (down) restart?.Invoke();
            });
            menuRt = Button(go.transform, "메뉴로", new Color(0.28f, 0.42f, 0.62f), down =>
            {
                if (down) menu?.Invoke();
            });
            go.SetActive(false);
            return go.GetComponent<RectTransform>();
        }

        public sealed class Stick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
        {
            RectTransform _root, _knob;
            System.Action<Vector2> _on;
            Vector2 _value;
            public RectTransform Root => _root;

            public static Stick Create(Transform parent, bool aim, System.Action<Vector2> on)
            {
                var go = new GameObject(aim ? "AimStick" : "MoveStick", typeof(RectTransform), typeof(Image), typeof(Stick));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.pivot = new Vector2(0f, 0f);
                go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
                var knob = new GameObject("Knob", typeof(RectTransform), typeof(Image));
                knob.transform.SetParent(go.transform, false);
                var kr = knob.GetComponent<RectTransform>();
                kr.anchorMin = kr.anchorMax = new Vector2(0.5f, 0.5f);
                kr.sizeDelta = new Vector2(84f, 84f);
                knob.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);
                var stick = go.GetComponent<Stick>();
                stick._root = rt;
                stick._knob = kr;
                stick._on = on;
                return stick;
            }

            public void OnPointerDown(PointerEventData eventData) => Drag(eventData);
            public void OnDrag(PointerEventData eventData) => Drag(eventData);
            public void OnPointerUp(PointerEventData eventData)
            {
                _value = Vector2.zero;
                _knob.anchoredPosition = Vector2.zero;
                _on?.Invoke(Vector2.zero);
            }

            void Drag(PointerEventData eventData)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, eventData.position, eventData.pressEventCamera, out var local);
                var size = _root.rect.size;
                if (size.x < 1f) size = _root.sizeDelta;
                local -= size * 0.5f;
                float r = Mathf.Max(28f, Mathf.Min(size.x, size.y) * 0.34f);
                _knob.sizeDelta = Vector2.one * Mathf.Max(48f, r * 1.15f);
                if (local.magnitude > r) local = local.normalized * r;
                _knob.anchoredPosition = local;
                _value = local / r;
                _on?.Invoke(_value);
            }
        }

        static RectTransform Button(Transform parent, string label, Color color, System.Action<bool> press)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(HoldButton));
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
            text.fontSize = 28;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
            go.GetComponent<HoldButton>().Pressed = press;
            return rt;
        }

        public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
        {
            public System.Action<bool> Pressed;
            public void OnPointerDown(PointerEventData eventData) => Pressed?.Invoke(true);
            public void OnPointerUp(PointerEventData eventData) => Pressed?.Invoke(false);
            public void OnPointerExit(PointerEventData eventData) => Pressed?.Invoke(false);
        }
    }
}

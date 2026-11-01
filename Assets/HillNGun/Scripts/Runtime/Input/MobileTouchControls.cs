using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.UI;
using HillNGun.UI.Touch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace HillNGun.Input
{
    /// <summary>
    /// Touch driver (Brawl-Stars-style on-screen controls). Reads EnhancedTouch every frame and forwards per-finger
    /// down / move / up to <see cref="TouchControlRouter"/> (logic), then lets <see cref="TouchControlsView"/> (uGUI) and
    /// <see cref="TouchAimOverlay"/> (world aim guides) draw the state. Layout follows the screen, Screen.safeArea and
    /// the HUD rects in <see cref="HudZones"/>. Hidden on desktop unless a Touchscreen device appears (or ForceTouch).
    /// </summary>
    public class MobileTouchControls : MonoBehaviour
    {
        [SerializeField] private TouchControlsData _controlsData;

        /// <summary>Smoke / tests: treat the device as touch even on desktop.</summary>
        public static bool ForceTouch;

        private IInputReceiver _receiver;
        private CharacterStateMachine _character;
        private CharacterTouchContext _ctx;
        private TouchControlRouter _router;
        private TouchControlsView _view;
        private TouchAimOverlay _overlay;
        private bool _isTouchDevice;
        private float _redetectTimer;

        private Vector2 _layoutScreen;
        private Rect _layoutSafe;
        private int _layoutHud = -1;
        private TouchControlsData _layoutData;
        private TouchLayout _layout;

        private readonly Dictionary<int, Vector2> _down = new Dictionary<int, Vector2>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _gone = new List<int>();
        private bool _wasBlocked;

        public TouchControlRouter Router => _router;
        public TouchLayout Layout => _layout;
        public TouchAimOverlay Overlay => _overlay;
        public TouchControlsData ControlsData => _controlsData;
        public bool IsActive => (_isTouchDevice || ForceTouch) && _controlsData != null;
        /// <summary>Fingers currently tracked (diagnostics).</summary>
        public int TrackedTouches => _down.Count;

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
            ServiceLocator.Register(this);
            _ctx = new CharacterTouchContext(null);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<MobileTouchControls>();
        }

        private void Start()
        {
            _isTouchDevice = DetectTouch();
        }

        public void SetReceiver(IInputReceiver receiver)
        {
            _router?.CancelAll();
            _down.Clear();
            _receiver = receiver;
            _character = receiver as CharacterStateMachine;
            _ctx.Character = _character;
            _router = receiver != null ? new TouchControlRouter(receiver, _ctx, _controlsData) : null;
            _layoutHud = -1; // force a layout push into the new router
        }

        public void SetControlsData(TouchControlsData data)
        {
            _controlsData = data;
            _router?.SetData(data);
            _layoutHud = -1;
        }

        private static bool DetectTouch()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return Touchscreen.current != null;
#endif
        }

        private void Update()
        {
            if (!_isTouchDevice && (_redetectTimer -= Time.unscaledDeltaTime) <= 0f)
            {
                _redetectTimer = 1f;
                _isTouchDevice = DetectTouch();
            }
            if (!IsActive || _router == null)
            {
                if (_view != null) _view.SetVisible(false);
                return;
            }
            EnsureViews();
            UpdateLayout();

            bool blocked = Time.timeScale <= 0f; // pause menu (PauseMenuController sets 0; hitstop only slows to 0.05)
            if (blocked)
            {
                if (!_wasBlocked) { _router.CancelAll(); _down.Clear(); }
                _wasBlocked = true;
                return;
            }
            if (_wasBlocked)
            {
                // fingers that were down through the pause must lift before they act again
                _wasBlocked = false;
                foreach (var t in ETouch.activeTouches) _ignored.Add(t.touchId);
            }
            ProcessTouches();
            _router.Tick(Time.deltaTime);
        }

        private readonly HashSet<int> _ignored = new HashSet<int>();

        private void ProcessTouches()
        {
            _seen.Clear();
            foreach (var t in ETouch.activeTouches)
            {
                int id = t.touchId;
                Vector2 pos = t.screenPosition;
                var ph = t.phase;
                bool ending = ph == UnityEngine.InputSystem.TouchPhase.Ended || ph == UnityEngine.InputSystem.TouchPhase.Canceled;
                if (_ignored.Contains(id)) { if (ending) _ignored.Remove(id); continue; }
                _seen.Add(id);
                bool known = _down.ContainsKey(id);
                if (!known)
                {
                    if (ph == UnityEngine.InputSystem.TouchPhase.None) continue;
                    // Began, or a touch whose Began we never saw (frame hitch): treat as a press here.
                    _router.PointerDown(id, pos);
                    _down[id] = pos;
                    known = true;
                }
                if (ending)
                {
                    _router.PointerUp(id, pos);
                    _down.Remove(id);
                }
                else if ((_down[id] - pos).sqrMagnitude > 0.01f || ph == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    _router.PointerMove(id, pos);
                    _down[id] = pos;
                }
            }
            // ids that vanished without an Ended (device reset / focus loss): release them where they were
            _gone.Clear();
            foreach (var kv in _down) if (!_seen.Contains(kv.Key)) _gone.Add(kv.Key);
            foreach (var id in _gone)
            {
                _router.PointerUp(id, _down[id]);
                _down.Remove(id);
            }
            if (_ignored.Count > 0)
            {
                _gone.Clear();
                foreach (var id in _ignored) if (!_seen.Contains(id)) { bool alive = false; foreach (var t in ETouch.activeTouches) if (t.touchId == id) { alive = true; break; } if (!alive) _gone.Add(id); }
                foreach (var id in _gone) _ignored.Remove(id);
            }
        }

        private void LateUpdate()
        {
            if (!IsActive || _router == null || _view == null) return;
            _view.Sync(_router, _character, _controlsData);
            _overlay.Sync(_wasBlocked ? null : _router, _character, _controlsData);
        }

        private void EnsureViews()
        {
            if (_view == null) _view = TouchControlsView.Create(transform);
            if (_overlay == null) _overlay = TouchAimOverlay.Create(transform);
        }

        private void UpdateLayout()
        {
            var screen = new Vector2(Screen.width, Screen.height);
            var safe = Screen.safeArea;
            var zones = HudZones.Collect();
            if (screen == _layoutScreen && safe == _layoutSafe && HudZones.Version == _layoutHud && _layoutData == _controlsData) return;
            _layoutScreen = screen; _layoutSafe = safe; _layoutHud = HudZones.Version; _layoutData = _controlsData;
            _layout = TouchControlLayout.Compute(_controlsData, screen, safe, zones);
            _router.SetLayout(_layout);
        }

        private void OnApplicationPause(bool paused) { if (paused) { _router?.CancelAll(); _down.Clear(); } }
        private void OnApplicationFocus(bool focus) { if (!focus) { _router?.CancelAll(); _down.Clear(); } }
    }
}

using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Input
{
    public class MobileTouchControls : MonoBehaviour
    {
        [SerializeField] private TouchControlsData _controlsData;

        private IInputReceiver _receiver;
        private Rect _safeArea;
        private bool _isTouchDevice;

        private Texture2D _circleTexture;
        private Texture2D _filledCircleTexture;
        private Texture2D _buttonTexture;

        private Vector2 _moveJoystickCenter;
        private Vector2 _moveJoystickKnob;
        private int _moveJoystickFinger = -1;
        private bool _moveJoystickActive;

        private Vector2 _aimJoystickCenter;
        private Vector2 _aimJoystickKnob;
        private int _aimJoystickFinger = -1;
        private bool _aimJoystickActive;
        private float _aimStartTime;

        private bool _rollButtonPressed;
        private bool _grenadeButtonPressed;
        private bool _skillButtonPressed;

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
            CreateTextures();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<MobileTouchControls>();
        }

        private void Start()
        {
            _isTouchDevice = IsTouchDevice();
        }

        public void SetReceiver(IInputReceiver receiver)
        {
            _receiver = receiver;
        }

        public void SetControlsData(TouchControlsData data)
        {
            _controlsData = data;
        }

        private bool IsTouchDevice()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return UnityEngine.InputSystem.Touchscreen.current != null;
#endif
        }

        public bool IsActive => _isTouchDevice && _controlsData != null;

        private void Update()
        {
            if (!IsActive || _receiver == null) return;

            UpdateSafeArea();
            ProcessTouches();
        }

        private void UpdateSafeArea()
        {
            _safeArea = Screen.safeArea;
            if (_controlsData != null)
            {
                float pad = _controlsData.safeAreaPadding;
                _safeArea.x += pad;
                _safeArea.y += pad;
                _safeArea.width -= pad * 2f;
                _safeArea.height -= pad * 2f;
            }
        }

        private void ProcessTouches()
        {
            var touches = Touch.activeTouches;

            bool moveActive = false;
            bool aimActive = false;

            foreach (var touch in touches)
            {
                Vector2 pos = touch.screenPosition;
                pos.y = Screen.height - pos.y;

                int fingerId = touch.touchId;

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    HandleTouchBegan(fingerId, pos);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                         touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    HandleTouchMoved(fingerId, pos);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || 
                         touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    HandleTouchEnded(fingerId, pos);
                }

                if (fingerId == _moveJoystickFinger) moveActive = true;
                if (fingerId == _aimJoystickFinger) aimActive = true;
            }

            if (!moveActive && _moveJoystickActive)
            {
                EndMoveJoystick();
            }
            if (!aimActive && _aimJoystickActive)
            {
                EndAimJoystick();
            }
        }

        private void HandleTouchBegan(int fingerId, Vector2 pos)
        {
            Rect moveRect = _controlsData.GetMoveJoystickRect(_safeArea);
            Rect aimRect = _controlsData.GetAimJoystickRect(_safeArea);
            Rect rollRect = _controlsData.GetRollButtonRect(_safeArea);
            Rect grenadeRect = _controlsData.GetGrenadeButtonRect(_safeArea);
            Rect skillRect = _controlsData.GetSkillButtonRect(_safeArea);
            Rect pauseRect = _controlsData.GetPauseButtonRect(_safeArea);

            if (moveRect.Contains(pos) && _moveJoystickFinger == -1)
            {
                _moveJoystickFinger = fingerId;
                _moveJoystickActive = true;
                _moveJoystickCenter = moveRect.center;
                _moveJoystickKnob = pos;
            }
            else if (aimRect.Contains(pos) && _aimJoystickFinger == -1)
            {
                _aimJoystickFinger = fingerId;
                _aimJoystickActive = true;
                _aimJoystickCenter = aimRect.center;
                _aimJoystickKnob = pos;
                _aimStartTime = Time.time;

                _receiver?.OnAimStartCommand(new AimStartCommand(_aimStartTime));
            }
            else if (rollRect.Contains(pos))
            {
                _rollButtonPressed = true;
                Vector2 dir = GetMoveDirection();
                _receiver?.OnRollCommand(new RollCommand(dir, Time.time));
            }
            else if (grenadeRect.Contains(pos))
            {
                _grenadeButtonPressed = true;
                _receiver?.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Time.time));
            }
            else if (skillRect.Contains(pos))
            {
                _skillButtonPressed = true;
                _receiver?.OnSkillStartCommand(new SkillStartCommand(Time.time));
            }
            else if (pauseRect.Contains(pos))
            {
                var flowManager = GameFlowManager.Instance;
                if (flowManager != null)
                {
                    flowManager.TogglePause();
                }
            }
        }

        private void HandleTouchMoved(int fingerId, Vector2 pos)
        {
            if (fingerId == _moveJoystickFinger)
            {
                _moveJoystickKnob = pos;
                Vector2 delta = pos - _moveJoystickCenter;
                float maxDist = _controlsData.moveJoystickSize / 2f - _controlsData.moveKnobSize / 2f;
                
                if (delta.magnitude > maxDist)
                {
                    _moveJoystickKnob = _moveJoystickCenter + delta.normalized * maxDist;
                }

                Vector2 dir = (pos - _moveJoystickCenter) / (maxDist > 0 ? maxDist : 1f);
                float magnitude = Mathf.Clamp01(dir.magnitude);

                if (magnitude < _controlsData.moveDeadzone)
                {
                    dir = Vector2.zero;
                    magnitude = 0f;
                }

                dir.y = -dir.y;
                _receiver?.OnMoveCommand(new MoveCommand(dir.normalized, magnitude, Time.time));
            }
            else if (fingerId == _aimJoystickFinger)
            {
                _aimJoystickKnob = pos;
                Vector2 delta = pos - _aimJoystickCenter;
                float maxDist = _controlsData.aimJoystickSize / 2f - _controlsData.aimKnobSize / 2f;

                if (delta.magnitude > maxDist)
                {
                    _aimJoystickKnob = _aimJoystickCenter + delta.normalized * maxDist;
                }

                Vector2 dir = delta.normalized;
                dir.y = -dir.y;
                bool outsideDeadzone = delta.magnitude > _controlsData.aimDeadzone * _controlsData.aimJoystickSize;

                _receiver?.OnAimDragCommand(new AimDragCommand(dir, outsideDeadzone, Time.time));
            }
        }

        private void HandleTouchEnded(int fingerId, Vector2 pos)
        {
            if (fingerId == _moveJoystickFinger)
            {
                EndMoveJoystick();
            }
            else if (fingerId == _aimJoystickFinger)
            {
                EndAimJoystick();
            }

            _rollButtonPressed = false;
            _grenadeButtonPressed = false;
            _skillButtonPressed = false;
        }

        private void EndMoveJoystick()
        {
            _moveJoystickFinger = -1;
            _moveJoystickActive = false;
            _receiver?.OnMoveCommand(new MoveCommand(Vector2.zero, 0f, Time.time));
        }

        private void EndAimJoystick()
        {
            if (_aimJoystickActive)
            {
                Vector2 delta = _aimJoystickKnob - _aimJoystickCenter;
                Vector2 dir = delta.normalized;
                dir.y = -dir.y;
                bool wasOutside = delta.magnitude > _controlsData.aimDeadzone * _controlsData.aimJoystickSize;

                _receiver?.OnAimReleaseCommand(new AimReleaseCommand(dir, wasOutside, Time.time));
            }
            _aimJoystickFinger = -1;
            _aimJoystickActive = false;
        }

        private Vector2 GetMoveDirection()
        {
            if (_moveJoystickActive)
            {
                Vector2 dir = (_moveJoystickKnob - _moveJoystickCenter).normalized;
                dir.y = -dir.y;
                return dir;
            }
            return Vector2.right;
        }

        private void OnGUI()
        {
            if (!IsActive) return;

            UpdateSafeArea();

            GUI.color = new Color(1f, 1f, 1f, _controlsData.opacity);

            DrawJoystick(_controlsData.GetMoveJoystickRect(_safeArea), 
                _moveJoystickActive ? _moveJoystickKnob : Vector2.zero,
                _moveJoystickActive, _controlsData.moveKnobSize, "이동");

            DrawJoystick(_controlsData.GetAimJoystickRect(_safeArea),
                _aimJoystickActive ? _aimJoystickKnob : Vector2.zero,
                _aimJoystickActive, _controlsData.aimKnobSize, "조준");

            DrawButton(_controlsData.GetRollButtonRect(_safeArea), "구르기", _rollButtonPressed, false);
            DrawButton(_controlsData.GetGrenadeButtonRect(_safeArea), "수류탄", _grenadeButtonPressed, false);
            DrawButton(_controlsData.GetSkillButtonRect(_safeArea), "스킬", _skillButtonPressed, false);
            DrawButton(_controlsData.GetPauseButtonRect(_safeArea), "||", false, false);

            GUI.color = Color.white;
        }

        private void DrawJoystick(Rect rect, Vector2 knobPos, bool active, float knobSize, string label)
        {
            GUI.color = _controlsData.joystickBgColor;
            GUI.DrawTexture(rect, GetCircleTexture());

            Rect knobRect;
            if (active && knobPos != Vector2.zero)
            {
                knobRect = new Rect(knobPos.x - knobSize / 2f, knobPos.y - knobSize / 2f, knobSize, knobSize);
            }
            else
            {
                knobRect = new Rect(rect.center.x - knobSize / 2f, rect.center.y - knobSize / 2f, knobSize, knobSize);
            }

            GUI.color = active ? _controlsData.buttonActiveColor : _controlsData.joystickKnobColor;
            GUI.DrawTexture(knobRect, GetFilledCircleTexture());

            if (!active)
            {
                var style = new GUIStyle { fontSize = 12, alignment = TextAnchor.MiddleCenter };
                style.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
                GUI.Label(rect, label, style);
            }
        }

        private void DrawButton(Rect rect, string label, bool pressed, bool onCooldown)
        {
            Color btnColor = onCooldown ? _controlsData.buttonCooldownColor :
                             pressed ? _controlsData.buttonActiveColor : _controlsData.buttonColor;
            GUI.color = btnColor;
            GUI.DrawTexture(rect, GetFilledCircleTexture());

            var style = new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
            GUI.Label(rect, label, style);
        }

        private void CreateTextures()
        {
            _circleTexture = CreateCircleTexture(64, false);
            _filledCircleTexture = CreateCircleTexture(64, true);
            _buttonTexture = CreateCircleTexture(64, true);
        }

        private Texture2D CreateCircleTexture(int size, bool filled)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = size / 2f - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    if (filled)
                    {
                        float alpha = Mathf.Clamp01((radius - dist + 1f) / 2f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        float ringWidth = 3f;
                        float alpha = 1f - Mathf.Abs(dist - radius) / ringWidth;
                        alpha = Mathf.Clamp01(alpha);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private Texture2D GetCircleTexture()
        {
            if (_circleTexture == null) CreateTextures();
            return _circleTexture;
        }

        private Texture2D GetFilledCircleTexture()
        {
            if (_filledCircleTexture == null) CreateTextures();
            return _filledCircleTexture;
        }
    }
}

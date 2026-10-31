using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using HillNGun.Data;
using HillNGun.Core;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace HillNGun.Input
{
    public class TouchInputHandler : MonoBehaviour
    {
        [SerializeField] private InputData _inputData;

        private IInputReceiver _receiver;
        private bool _useKeyboardMouse = true;

        private Vector2 _joystickCenter;
        private Vector2 _fireButtonCenter;
        private Vector2 _rollButtonCenter;
        private Vector2 _grenadeButtonCenter;
        private Vector2 _skillButtonCenter;

        private bool _joystickActive;
        private bool _fireButtonActive;
        private bool _grenadeButtonActive;

        private int _joystickFingerId = -1;
        private int _fireButtonFingerId = -1;
        private int _grenadeButtonFingerId = -1;

        private Vector2 _joystickStartPos;
        private Vector2 _fireButtonStartPos;
        private Vector2 _grenadeButtonStartPos;
        private Vector2 _lastAimDirection = Vector2.up;
        private bool _wasOutsideDeadzone;
        private float _rowChangeHoldTimer;
        private int _pendingRowDirection;

        private Vector2 _lastMoveInput;
        private const float BUTTON_RADIUS = 60f;

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
            CalculateButtonPositions();
        }

        private void OnDestroy()
        {
            EnhancedTouchSupport.Disable();
        }

        public void SetReceiver(IInputReceiver receiver)
        {
            _receiver = receiver;
        }

        public void SetInputData(InputData data)
        {
            _inputData = data;
            CalculateButtonPositions();
        }

        private void CalculateButtonPositions()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            float scale = _inputData?.CurrentButtonScale ?? 1f;
            float joystickRadius = (_inputData?.joystickRadiusPx ?? 120f) * scale;
            float fireButtonRadius = (_inputData?.fireButtonRadiusPx ?? 140f) * scale;
            float smallButtonRadius = BUTTON_RADIUS * scale;

            float margin = 50f;
            float bottomMargin = 100f;

            _joystickCenter = new Vector2(margin + joystickRadius, bottomMargin + joystickRadius);
            _fireButtonCenter = new Vector2(screenWidth - margin - fireButtonRadius, bottomMargin + fireButtonRadius);

            _rollButtonCenter = new Vector2(margin + joystickRadius * 2 + smallButtonRadius + 30f, bottomMargin + smallButtonRadius);
            _grenadeButtonCenter = new Vector2(screenWidth - margin - fireButtonRadius * 2 - smallButtonRadius - 30f, bottomMargin + fireButtonRadius + smallButtonRadius + 20f);
            _skillButtonCenter = new Vector2(screenWidth - margin - fireButtonRadius - smallButtonRadius - 30f, bottomMargin + fireButtonRadius * 2 + smallButtonRadius);
        }

        private void Update()
        {
            if (_receiver == null) return;

            if (_useKeyboardMouse)
            {
                HandleKeyboardMouseInput();
            }

            // Visible on-screen controls own the touches when present (avoid double commands)
            var mobile = ServiceLocator.Get<MobileTouchControls>();
            if (mobile == null || !mobile.IsActive) HandleTouchInput();
            ProcessRowChangeHold();
        }

        private void HandleKeyboardMouseInput()
        {
            Vector2 moveInput = Vector2.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    moveInput.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    moveInput.x += 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    moveInput.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    moveInput.y -= 1f;

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    _receiver.OnRollCommand(new RollCommand(_lastMoveInput.normalized, Time.time));
                }

                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    Vector2 mouseWorld = GetMouseWorldPosition();
                    _receiver.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Mouse.current.position.ReadValue(), Time.time));
                }

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    _receiver.OnSkillStartCommand(new SkillStartCommand(Mouse.current.position.ReadValue(), Time.time));
                }
                if (Keyboard.current.eKey.wasReleasedThisFrame)
                {
                    Vector2 mouseWorld = GetMouseWorldPosition();
                    _receiver.OnSkillReleaseCommand(new SkillReleaseCommand(mouseWorld, 0f, Time.time));
                }
            }

            if (moveInput != _lastMoveInput)
            {
                _lastMoveInput = moveInput;

                if (moveInput.magnitude > 0.1f)
                {
                    Vector2 normalized = moveInput.normalized;
                    _receiver.OnMoveCommand(new MoveCommand(normalized, moveInput.magnitude, Time.time));

                    if (Mathf.Abs(moveInput.y) > 0.5f && Mathf.Abs(moveInput.x) < 0.3f)
                    {
                        int dir = moveInput.y > 0 ? 1 : -1;
                        if (_pendingRowDirection != dir)
                        {
                            _pendingRowDirection = dir;
                            _rowChangeHoldTimer = 0f;
                        }
                    }
                    else
                    {
                        _pendingRowDirection = 0;
                        _rowChangeHoldTimer = 0f;
                    }
                }
                else
                {
                    _receiver.OnMoveCommand(MoveCommand.Stop(Time.time));
                    _pendingRowDirection = 0;
                    _rowChangeHoldTimer = 0f;
                }
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _fireButtonActive = true;
                    _fireButtonStartPos = Mouse.current.position.ReadValue();
                    _wasOutsideDeadzone = false;
                    _receiver.OnAimStartCommand(new AimStartCommand(_fireButtonStartPos, Time.time));
                }

                if (_fireButtonActive && Mouse.current.leftButton.isPressed)
                {
                    Vector2 currentPos = Mouse.current.position.ReadValue();
                    Vector2 delta = currentPos - _fireButtonStartPos;
                    float distance = delta.magnitude;
                    float deadzoneRadius = _inputData?.DeadzoneRadius ?? 28f;
                    bool outsideDeadzone = distance > deadzoneRadius;

                    if (distance > 0.1f)
                    {
                        _lastAimDirection = delta.normalized;
                    }

                    _wasOutsideDeadzone = _wasOutsideDeadzone || outsideDeadzone;
                    _receiver.OnAimDragCommand(new AimDragCommand(_lastAimDirection, distance, outsideDeadzone, Time.time));
                }

                if (Mouse.current.leftButton.wasReleasedThisFrame && _fireButtonActive)
                {
                    _fireButtonActive = false;
                    _receiver.OnAimReleaseCommand(new AimReleaseCommand(_lastAimDirection, _wasOutsideDeadzone, Time.time));
                }

                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    Vector2 mouseWorld = GetMouseWorldPosition();
                    _receiver.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Mouse.current.position.ReadValue(), Time.time));
                }

                if (Mouse.current.rightButton.isPressed)
                {
                    Vector2 currentPos = Mouse.current.position.ReadValue();
                    Vector2 startPos = _fireButtonStartPos;
                    Vector2 delta = currentPos - startPos;
                    _receiver.OnGrenadeAimDragCommand(new GrenadeAimDragCommand(delta.normalized, delta.magnitude / 100f, Time.time));
                }

                if (Mouse.current.rightButton.wasReleasedThisFrame)
                {
                    Vector2 mouseWorld = GetMouseWorldPosition();
                    _receiver.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(mouseWorld, true, Time.time));
                }
            }
        }

        private Vector2 GetMouseWorldPosition()
        {
            if (Camera.main == null) return Vector2.zero;
            Vector3 mousePos = Mouse.current.position.ReadValue();
            mousePos.z = -Camera.main.transform.position.z;
            return Camera.main.ScreenToWorldPoint(mousePos);
        }

        private void HandleTouchInput()
        {
            foreach (var touch in Touch.activeTouches)
            {
                Vector2 pos = touch.screenPosition;

                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        HandleTouchBegan(touch.touchId, pos);
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Moved:
                    case UnityEngine.InputSystem.TouchPhase.Stationary:
                        HandleTouchMoved(touch.touchId, pos);
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Ended:
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        HandleTouchEnded(touch.touchId, pos);
                        break;
                }
            }
        }

        private void HandleTouchBegan(int fingerId, Vector2 position)
        {
            float joystickRadius = _inputData?.ScaledJoystickRadius ?? 120f;
            float fireButtonRadius = _inputData?.ScaledFireButtonRadius ?? 140f;
            float scale = _inputData?.CurrentButtonScale ?? 1f;
            float smallButtonRadius = BUTTON_RADIUS * scale;

            if (!_joystickActive && Vector2.Distance(position, _joystickCenter) < joystickRadius * 1.5f)
            {
                _joystickActive = true;
                _joystickFingerId = fingerId;
                _joystickStartPos = position;
            }
            else if (!_fireButtonActive && Vector2.Distance(position, _fireButtonCenter) < fireButtonRadius * 1.5f)
            {
                _fireButtonActive = true;
                _fireButtonFingerId = fingerId;
                _fireButtonStartPos = position;
                _wasOutsideDeadzone = false;
                _receiver?.OnAimStartCommand(new AimStartCommand(position, Time.time));
            }
            else if (Vector2.Distance(position, _rollButtonCenter) < smallButtonRadius * 1.5f)
            {
                _receiver?.OnRollCommand(new RollCommand(_lastMoveInput.magnitude > 0.1f ? _lastMoveInput.normalized : Vector2.right, Time.time));
            }
            else if (!_grenadeButtonActive && Vector2.Distance(position, _grenadeButtonCenter) < smallButtonRadius * 1.5f)
            {
                _grenadeButtonActive = true;
                _grenadeButtonFingerId = fingerId;
                _grenadeButtonStartPos = position;
                _receiver?.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(position, Time.time));
            }
            else if (Vector2.Distance(position, _skillButtonCenter) < smallButtonRadius * 1.5f)
            {
                _receiver?.OnSkillStartCommand(new SkillStartCommand(position, Time.time));
            }
        }

        private void HandleTouchMoved(int fingerId, Vector2 position)
        {
            if (fingerId == _joystickFingerId && _joystickActive)
            {
                ProcessJoystickInput(position);
            }
            else if (fingerId == _fireButtonFingerId && _fireButtonActive)
            {
                ProcessFireButtonInput(position);
            }
            else if (fingerId == _grenadeButtonFingerId && _grenadeButtonActive)
            {
                ProcessGrenadeInput(position);
            }
        }

        private void HandleTouchEnded(int fingerId, Vector2 position)
        {
            if (fingerId == _joystickFingerId && _joystickActive)
            {
                _joystickActive = false;
                _joystickFingerId = -1;
                _receiver?.OnMoveCommand(MoveCommand.Stop(Time.time));
                _pendingRowDirection = 0;
                _rowChangeHoldTimer = 0f;
            }
            else if (fingerId == _fireButtonFingerId && _fireButtonActive)
            {
                _fireButtonActive = false;
                _fireButtonFingerId = -1;
                _receiver?.OnAimReleaseCommand(new AimReleaseCommand(_lastAimDirection, _wasOutsideDeadzone, Time.time));
            }
            else if (fingerId == _grenadeButtonFingerId && _grenadeButtonActive)
            {
                _grenadeButtonActive = false;
                _grenadeButtonFingerId = -1;
                Vector2 targetWorld = ScreenToWorld(position);
                _receiver?.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(targetWorld, true, Time.time));
            }
        }

        private void ProcessGrenadeInput(Vector2 position)
        {
            Vector2 delta = position - _grenadeButtonStartPos;
            _receiver?.OnGrenadeAimDragCommand(new GrenadeAimDragCommand(delta.normalized, delta.magnitude / 100f, Time.time));
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            if (Camera.main == null) return Vector2.zero;
            Vector3 pos = screenPos;
            pos.z = -Camera.main.transform.position.z;
            return Camera.main.ScreenToWorldPoint(pos);
        }

        private void ProcessJoystickInput(Vector2 position)
        {
            float joystickRadius = _inputData?.ScaledJoystickRadius ?? 120f;

            Vector2 delta = position - _joystickStartPos;
            float magnitude = Mathf.Clamp01(delta.magnitude / joystickRadius);

            if (magnitude > 0.1f)
            {
                Vector2 direction = delta.normalized;
                _lastMoveInput = direction * magnitude;
                _receiver?.OnMoveCommand(new MoveCommand(direction, magnitude, Time.time));

                float rowThreshold = _inputData?.rowChangeThreshold ?? 0.6f;
                if (Mathf.Abs(direction.y) > rowThreshold && Mathf.Abs(direction.x) < 0.4f)
                {
                    int dir = direction.y > 0 ? 1 : -1;
                    if (_pendingRowDirection != dir)
                    {
                        _pendingRowDirection = dir;
                        _rowChangeHoldTimer = 0f;
                    }
                }
                else
                {
                    _pendingRowDirection = 0;
                    _rowChangeHoldTimer = 0f;
                }
            }
            else
            {
                _lastMoveInput = Vector2.zero;
                _receiver?.OnMoveCommand(MoveCommand.Stop(Time.time));
                _pendingRowDirection = 0;
                _rowChangeHoldTimer = 0f;
            }
        }

        private void ProcessFireButtonInput(Vector2 position)
        {
            float deadzoneRadius = _inputData?.DeadzoneRadius ?? 28f;

            Vector2 delta = position - _fireButtonStartPos;
            float distance = delta.magnitude;
            bool outsideDeadzone = distance > deadzoneRadius;

            if (distance > 0.1f)
            {
                _lastAimDirection = delta.normalized;
            }

            _wasOutsideDeadzone = _wasOutsideDeadzone || outsideDeadzone;
            _receiver?.OnAimDragCommand(new AimDragCommand(_lastAimDirection, distance, outsideDeadzone, Time.time));
        }

        private void ProcessRowChangeHold()
        {
            if (_pendingRowDirection == 0) return;

            float holdTime = _inputData?.rowChangeHoldTime ?? 0.15f;
            _rowChangeHoldTimer += Time.deltaTime;

            if (_rowChangeHoldTimer >= holdTime)
            {
                _receiver?.OnRowChangeCommand(new RowChangeCommand(_pendingRowDirection, Time.time));
                _pendingRowDirection = 0;
                _rowChangeHoldTimer = 0f;
            }
        }

        public Vector2 GetJoystickCenter() => _joystickCenter;
        public Vector2 GetFireButtonCenter() => _fireButtonCenter;
        public Vector2 GetRollButtonCenter() => _rollButtonCenter;
        public Vector2 GetGrenadeButtonCenter() => _grenadeButtonCenter;
        public Vector2 GetSkillButtonCenter() => _skillButtonCenter;
        public bool IsJoystickActive() => _joystickActive;
        public bool IsFireButtonActive() => _fireButtonActive;
        public bool IsGrenadeButtonActive() => _grenadeButtonActive;
        public float GetSmallButtonRadius() => BUTTON_RADIUS * (_inputData?.CurrentButtonScale ?? 1f);

        public bool IsTouchDevice()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return UnityEngine.InputSystem.Touchscreen.current != null;
#endif
        }

        public void InjectMoveCommand(Vector2 direction, float magnitude)
        {
            _receiver?.OnMoveCommand(new MoveCommand(direction.normalized, magnitude, Time.time));
        }

        public void InjectFireCommand(Vector2 aimDirection)
        {
            _receiver?.OnAimStartCommand(new AimStartCommand(Vector2.zero, Time.time));
            _receiver?.OnAimDragCommand(new AimDragCommand(aimDirection.normalized, 100f, true, Time.time));
            _receiver?.OnAimReleaseCommand(new AimReleaseCommand(aimDirection.normalized, true, Time.time));
        }
    }
}

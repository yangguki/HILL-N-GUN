using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public enum Proto_GreyboxAimReleaseIntent
    {
        None,
        AutoAim,
        ManualDirection,
        Cancel
    }

    public class Proto_GreyboxAim
    {
        const float DragThresholdWorld = 0.18f;
        const float DeadZoneWorld = 0.14f;

        Vector2 _stickDrag;
        bool _dragActive;
        float _peakDragMag;
        bool _screenStickActive;
        Vector2 _stickScreenDelta;
        float _peakScreenMag;
        float _screenStickRadiusHeightPercent = Proto_GreyboxPointerMath.AimStickRadiusScreenHeightPercent;
        float _screenStickSmoothTau = Proto_GreyboxPointerMath.AimStickSmoothTime;
        float _screenStickDeadPortion = Proto_GreyboxPointerMath.AimStickDeadZonePortion;
        float _screenStickDragThresholdPortion = Proto_GreyboxPointerMath.AimStickDragThresholdPortion;
        float _screenStickAngularCap = Proto_GreyboxPointerMath.AimAngularSpeedCapDegPerSec;
        float _screenStickFineExponent = Proto_GreyboxPointerMath.AimFineAimExponent;
        float _lastScreenH = 1080f;
        Vector2 _playerFoot;
        IList<Proto_GreyboxTarget> _targets;
        float _maxRange = 25f;

        public Vector2 AimDirection { get; private set; }
        public Vector2 AimLineEnd { get; private set; }
        public Proto_GreyboxTarget SelectedTarget { get; private set; }
        public bool IsDragging => _dragActive;

        public static Vector2 DefaultAimGround =>
            new Vector2(
                Proto_GreyboxLayout.ScreenXPercentToWorldX(50f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.DefaultAimFootPercent));

        public void BeginDrag(Vector2 playerFoot, IList<Proto_GreyboxTarget> targets, float maxRange = 25f)
        {
            _dragActive = true;
            _screenStickActive = false;
            _stickDrag = Vector2.zero;
            _peakDragMag = 0f;
            _stickScreenDelta = Vector2.zero;
            _peakScreenMag = 0f;
            _playerFoot = playerFoot;
            _targets = targets;
            _maxRange = maxRange;
            RefreshNearest();
            UpdateAimGeometry();
        }

        public void ConfigureScreenStick(float radiusHeightPercent, float smoothTauSeconds, float deadZonePortion,
            float dragThresholdPortion, float angularCapDegPerSec = Proto_GreyboxPointerMath.AimAngularSpeedCapDegPerSec,
            float fineAimExponent = Proto_GreyboxPointerMath.AimFineAimExponent)
        {
            _screenStickRadiusHeightPercent = radiusHeightPercent;
            _screenStickSmoothTau = smoothTauSeconds;
            _screenStickDeadPortion = deadZonePortion;
            _screenStickDragThresholdPortion = dragThresholdPortion;
            _screenStickAngularCap = angularCapDegPerSec;
            _screenStickFineExponent = fineAimExponent;
        }

        public void SetAimDirection(Vector2 dragOffsetWorld)
        {
            if (!_dragActive) return;
            _screenStickActive = false;
            _stickDrag = dragOffsetWorld;
            _peakDragMag = Mathf.Max(_peakDragMag, _stickDrag.magnitude);
            UpdateAimGeometry();
        }

        public void SetAimStickScreenDelta(Vector2 screenDelta, float screenW, float screenH, float dt)
        {
            if (!_dragActive) return;
            _screenStickActive = true;
            _lastScreenH = screenH;
            _stickScreenDelta = screenDelta;
            float mag = screenDelta.magnitude;
            _peakScreenMag = Mathf.Max(_peakScreenMag, mag);
            float stickRadiusPx = Proto_GreyboxPointerMath.AimStickRadiusPx(screenH, _screenStickRadiusHeightPercent);
            float deadPx = stickRadiusPx * _screenStickDeadPortion;
            Vector2 targetDir;
            if (mag < deadPx)
                targetDir = IdleAimDirection();
            else
                targetDir = Proto_GreyboxPointerMath.WorldDirectionFromScreenDrag(screenDelta, screenW, screenH);
            AimDirection = Proto_GreyboxPointerMath.SmoothAimDirection(AimDirection, targetDir, dt, _screenStickSmoothTau,
                _screenStickAngularCap, _screenStickFineExponent);
            AimLineEnd = ClipAimEnd(_playerFoot, AimDirection, _maxRange);
        }

        public void PoseInProgressDrag(Vector2 playerFoot, IList<Proto_GreyboxTarget> targets, Vector2 dragOffsetWorld,
            float maxRange = 25f)
        {
            BeginDrag(playerFoot, targets, maxRange);
            SetAimDirection(dragOffsetWorld);
        }

        public Proto_GreyboxAimReleaseIntent EndDrag()
        {
            if (!_dragActive) return Proto_GreyboxAimReleaseIntent.None;
            _dragActive = false;
            if (_screenStickActive)
            {
                float stickRadiusPx =
                    Proto_GreyboxPointerMath.AimStickRadiusPx(_lastScreenH, _screenStickRadiusHeightPercent);
                return Proto_GreyboxPointerMath.AimStickReleaseIntent(_stickScreenDelta.magnitude, _peakScreenMag,
                    stickRadiusPx, _screenStickDeadPortion, _screenStickDragThresholdPortion);
            }

            float mag = _stickDrag.magnitude;
            if (_peakDragMag >= DragThresholdWorld && mag < DeadZoneWorld)
                return Proto_GreyboxAimReleaseIntent.Cancel;
            if (mag < DragThresholdWorld)
                return Proto_GreyboxAimReleaseIntent.AutoAim;
            return Proto_GreyboxAimReleaseIntent.ManualDirection;
        }

        public Vector2 ManualFireAimPoint() => _playerFoot + AimDirection * _maxRange;

        public Vector2 AutoFireAimPoint()
        {
            if (SelectedTarget != null && SelectedTarget.IsAlive)
                return SelectedTarget.BodyCenterGround;
            return DefaultAimGround;
        }

        public void TickIdle(Vector2 playerFoot, IList<Proto_GreyboxTarget> targets, float maxRange = 25f)
        {
            if (_dragActive) return;
            _playerFoot = playerFoot;
            _targets = targets;
            _maxRange = maxRange;
            RefreshNearest();
            var to = SelectedTarget != null && SelectedTarget.IsAlive
                ? SelectedTarget.BodyCenterGround - _playerFoot
                : DefaultAimGround - _playerFoot;
            if (to.sqrMagnitude < 1e-8f)
                to = Vector2.up;
            AimDirection = to.normalized;
            AimLineEnd = ClipAimEnd(_playerFoot, AimDirection, _maxRange);
        }

        public void TickKeyboardDrag(Vector2 playerFoot, IList<Proto_GreyboxTarget> targets, Vector2 pointerWorld,
            Vector2 pressOriginWorld, float maxRange = 25f)
        {
            _dragActive = true;
            _peakDragMag = 0f;
            _playerFoot = playerFoot;
            _targets = targets;
            _maxRange = maxRange;
            _stickDrag = pointerWorld - pressOriginWorld;
            _peakDragMag = Mathf.Max(_peakDragMag, _stickDrag.magnitude);
            RefreshNearest();
            UpdateAimGeometry();
        }

        public Proto_GreyboxAimReleaseIntent EndKeyboardDrag(Vector2 releaseWorld, Vector2 pressOriginWorld,
            float playerDeadRadius = 0.35f)
        {
            _dragActive = false;
            _stickDrag = releaseWorld - pressOriginWorld;
            float mag = _stickDrag.magnitude;
            if ((releaseWorld - pressOriginWorld).sqrMagnitude < DeadZoneWorld * DeadZoneWorld
                && _peakDragMag >= DragThresholdWorld)
                return Proto_GreyboxAimReleaseIntent.Cancel;
            if ((releaseWorld - _playerFoot).sqrMagnitude < playerDeadRadius * playerDeadRadius)
                return Proto_GreyboxAimReleaseIntent.Cancel;
            UpdateAimGeometry();
            if (mag < DragThresholdWorld)
                return Proto_GreyboxAimReleaseIntent.AutoAim;
            return Proto_GreyboxAimReleaseIntent.ManualDirection;
        }

        public void Tick(float dt, Vector2 playerFoot, IList<Proto_GreyboxTarget> targets, bool dragActive,
            Vector2 dragDeltaWorld)
        {
            TickIdle(playerFoot, targets);
        }

        public Vector2 AimGroundPosition()
        {
            if (IsDragging)
                return AimLineEnd;
            if (SelectedTarget != null && SelectedTarget.IsAlive)
                return SelectedTarget.BodyCenterGround;
            return DefaultAimGround;
        }

        void RefreshNearest() => SelectedTarget = Proto_GreyboxTarget.SelectNearest(_playerFoot, _targets);

        Vector2 IdleAimDirection()
        {
            if (SelectedTarget != null && SelectedTarget.IsAlive)
            {
                var d = SelectedTarget.BodyCenterGround - _playerFoot;
                return d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.up;
            }

            var idle = DefaultAimGround - _playerFoot;
            return idle.sqrMagnitude > 1e-8f ? idle.normalized : Vector2.up;
        }

        void UpdateAimGeometry()
        {
            if (_stickDrag.sqrMagnitude > 1e-8f)
                AimDirection = _stickDrag.normalized;
            else if (SelectedTarget != null && SelectedTarget.IsAlive)
            {
                var d = SelectedTarget.BodyCenterGround - _playerFoot;
                AimDirection = d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.up;
            }
            else
            {
                var d = DefaultAimGround - _playerFoot;
                AimDirection = d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.up;
            }

            AimLineEnd = ClipAimEnd(_playerFoot, AimDirection, _maxRange);
        }

        public static Vector2 ClipAimEnd(Vector2 origin, Vector2 dir, float maxRange)
        {
            float minX = Proto_GreyboxLayout.AimVisualMinWorldX;
            float maxX = Proto_GreyboxLayout.AimVisualMaxWorldX;
            float minY = Proto_GreyboxLayout.AimVisualMinWorldY;
            float maxY = Proto_GreyboxLayout.AimVisualMaxWorldY;
            float tEnd = maxRange;
            if (Mathf.Abs(dir.x) > 1e-6f)
            {
                if (dir.x > 0f)
                    tEnd = Mathf.Min(tEnd, (maxX - origin.x) / dir.x);
                else
                    tEnd = Mathf.Min(tEnd, (minX - origin.x) / dir.x);
            }

            if (Mathf.Abs(dir.y) > 1e-6f)
            {
                if (dir.y > 0f)
                    tEnd = Mathf.Min(tEnd, (maxY - origin.y) / dir.y);
                else
                    tEnd = Mathf.Min(tEnd, (minY - origin.y) / dir.y);
            }

            tEnd = Mathf.Clamp(tEnd, 0f, maxRange);
            return origin + dir * tEnd;
        }
    }
}
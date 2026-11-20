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
                Proto_GreyboxLayout.ScreenYPercentToWorldY(38f));

        public void BeginDrag(Vector2 playerFoot, IList<Proto_GreyboxTarget> targets, float maxRange = 25f)
        {
            _dragActive = true;
            _stickDrag = Vector2.zero;
            _peakDragMag = 0f;
            _playerFoot = playerFoot;
            _targets = targets;
            _maxRange = maxRange;
            RefreshNearest();
            UpdateAimGeometry();
        }

        public void SetAimDirection(Vector2 dragOffsetWorld)
        {
            if (!_dragActive) return;
            _stickDrag = dragOffsetWorld;
            _peakDragMag = Mathf.Max(_peakDragMag, _stickDrag.magnitude);
            UpdateAimGeometry();
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
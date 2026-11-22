using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Pure screen↔world pointer math for EditMode tests (pass explicit screen size; no Screen.width).</summary>
    public static class Proto_GreyboxPointerMath
    {
        public const float JoystickAnalogMaxRadiusPercent = 7.5f;
        public const float AimStickRadiusScreenHeightPercent = 18f;
        public const float AimStickDeadZonePortion = 0.15f;
        public const float AimStickDragThresholdPortion = 0.15f;
        public const float AimStickSmoothTime = 0.12f;
        public const float AimAngularSpeedCapDegPerSec = 240f;
        public const float AimFineAimExponent = 1.5f;

        public static float AimStickRadiusPx(float screenH, float radiusHeightPercent) =>
            screenH * (radiusHeightPercent / 100f);

        public static Vector2 WorldDirectionFromScreenDrag(Vector2 screenDelta, float screenW, float screenH)
        {
            if (screenDelta.sqrMagnitude < 1e-8f)
                return Vector2.up;
            var world = ScreenDeltaToWorldDrag(screenDelta, screenW, screenH);
            return world.sqrMagnitude > 1e-10f ? world.normalized : Vector2.up;
        }

        public static Vector2 SmoothAimDirection(Vector2 current, Vector2 target, float dt, float tauSeconds,
            float angularCapDegPerSec = AimAngularSpeedCapDegPerSec, float fineAimExponent = AimFineAimExponent)
        {
            if (target.sqrMagnitude < 1e-8f)
                return current.sqrMagnitude > 1e-8f ? current : Vector2.up;
            target = target.normalized;
            if (current.sqrMagnitude < 1e-8f)
                return target;
            float linearT = tauSeconds <= 1e-6f ? 1f : 1f - Mathf.Exp(-dt / tauSeconds);
            float curDeg = Mathf.Atan2(current.y, current.x) * Mathf.Rad2Deg;
            float tgtDeg = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
            float deltaDeg = Mathf.DeltaAngle(curDeg, tgtDeg);
            float linearStep = deltaDeg * linearT;
            // Exponent applies to small angular errors only (~45° and under) so large flicks stay responsive.
            const float fineRefDeg = 45f;
            float u = Mathf.Clamp01(Mathf.Abs(deltaDeg) / fineRefDeg);
            float scale = fineAimExponent <= 1.001f ? 1f : Mathf.Pow(Mathf.Max(u, 1e-4f), fineAimExponent - 1f);
            float stepDeg = linearStep * scale;
            if (angularCapDegPerSec > 1e-3f && dt > 1e-6f)
            {
                float maxStep = angularCapDegPerSec * dt;
                stepDeg = Mathf.Clamp(stepDeg, -maxStep, maxStep);
            }

            float ang = (curDeg + stepDeg) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        }

        public static Proto_GreyboxAimReleaseIntent AimStickReleaseIntent(float releaseMagPx, float peakMagPx,
            float stickRadiusPx, float deadZonePortion, float dragThresholdPortion)
        {
            float deadPx = stickRadiusPx * deadZonePortion;
            float threshPx = stickRadiusPx * dragThresholdPortion;
            if (peakMagPx >= threshPx && releaseMagPx < deadPx)
                return Proto_GreyboxAimReleaseIntent.Cancel;
            if (releaseMagPx < threshPx)
                return Proto_GreyboxAimReleaseIntent.AutoAim;
            return Proto_GreyboxAimReleaseIntent.ManualDirection;
        }

        public static void PointerPercentFromScreen(Vector2 pos, float screenW, float screenH, out float xPct,
            out float yPct)
        {
            xPct = pos.x / screenW * 100f;
            yPct = (screenH - pos.y) / screenH * 100f;
        }

        public static float WorldWidthForScreen(float screenW, float screenH) =>
            2f * Proto_GreyboxLayout.CameraOrthographicSize * (screenW / screenH);

        public static Vector2 ScreenDeltaToWorldDrag(Vector2 deltaScreen, float screenW, float screenH)
        {
            float worldW = WorldWidthForScreen(screenW, screenH);
            float worldH = Proto_GreyboxLayout.WorldHeight;
            return new Vector2(deltaScreen.x * (worldW / screenW), deltaScreen.y * (worldH / screenH));
        }

        /// <summary>Legacy 16:9 layout-width mapping (regression reference for wide-screen angle tests).</summary>
        public static Vector2 ScreenDeltaToWorldDragFixedLayout16x9(Vector2 deltaScreen, float screenW, float screenH)
        {
            float worldPerPx = Proto_GreyboxLayout.WorldWidth / screenW;
            float worldPerPy = Proto_GreyboxLayout.WorldHeight / screenH;
            return new Vector2(deltaScreen.x * worldPerPx, deltaScreen.y * worldPerPy);
        }

        public static Vector2 ScreenToGroundWorld(Vector2 screenPos, float screenW, float screenH)
        {
            PointerPercentFromScreen(screenPos, screenW, screenH, out float xPct, out float yPct);
            float worldW = WorldWidthForScreen(screenW, screenH);
            return new Vector2(worldW * (xPct / 100f) - worldW * 0.5f,
                Proto_GreyboxLayout.ScreenYPercentToWorldY(yPct));
        }

        public static Vector2 TouchJoystickAnalog(Vector2 fingerScreen, float screenW, float screenH)
        {
            float refW = Proto_GreyboxHudBuilder.RefWidth;
            float refH = Proto_GreyboxHudBuilder.RefHeight;
            float cx = refW * (Proto_GreyboxHudBuilder.JoystickCenterXPercent / 100f);
            float cy = refH * (1f - Proto_GreyboxHudBuilder.JoystickCenterYPercentFromTop / 100f);
            float sx = fingerScreen.x / screenW * refW;
            float sy = fingerScreen.y / screenH * refH;
            var delta = new Vector2(sx - cx, sy - cy);
            float maxR = refW * (JoystickAnalogMaxRadiusPercent / 100f);
            if (delta.magnitude > maxR)
                delta = delta.normalized * maxR;
            return delta / maxR;
        }

        public static Vector2 JoystickVisualCenterScreen(float screenW, float screenH)
        {
            float x = screenW * (Proto_GreyboxHudBuilder.JoystickCenterXPercent / 100f);
            float y = screenH * (1f - Proto_GreyboxHudBuilder.JoystickCenterYPercentFromTop / 100f);
            return new Vector2(x, y);
        }
    }
}
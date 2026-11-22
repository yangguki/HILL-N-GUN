using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Percent HUD layout vs joystick / action cluster at explicit screen sizes (EditMode).</summary>
    public static class Proto_GreyboxHudLayoutMath
    {
        public static bool RectsOverlapPercent(float ax0, float ay0, float ax1, float ay1, float bx0, float by0,
            float bx1, float by1) =>
            Proto_GreyboxCombatInputGate.RectsOverlap(ax0, ay0, ax1, ay1, bx0, by0, bx1, by1);

        public static bool RectOverlapsCircleHitPercent(float rx0, float ry0, float rx1, float ry1, float cx, float cy,
            float placeRadiusPercent, float screenW, float screenH,
            float paddingPx = Proto_GreyboxHudBuilder.ActionClusterHitPaddingPx)
        {
            float hitR = CircleHitRadiusScreenPx(placeRadiusPercent, screenH, paddingPx);
            float ccx = screenW * (cx / 100f);
            float ccy = screenH * (cy / 100f);
            float left = screenW * (rx0 / 100f);
            float right = screenW * (rx1 / 100f);
            float top = screenH * (ry0 / 100f);
            float bottom = screenH * (ry1 / 100f);
            float nearX = Mathf.Clamp(ccx, left, right);
            float nearY = Mathf.Clamp(ccy, top, bottom);
            float dx = ccx - nearX;
            float dy = ccy - nearY;
            return dx * dx + dy * dy <= hitR * hitR;
        }

        public static float CircleHitRadiusScreenPx(float placeRadiusPercent, float screenH, float paddingPx)
        {
            float visual = screenH * (placeRadiusPercent / 100f);
            float pad = paddingPx * (screenH / Proto_GreyboxHudBuilder.RefHeight);
            return visual + pad;
        }

        public static bool HeartsClearHudChromeAt(float screenW, float screenH)
        {
            float hx0 = Proto_GreyboxHudBuilder.HeartsRootX0;
            float hy0 = Proto_GreyboxHudBuilder.HeartsRootY0;
            float hx1 = Proto_GreyboxHudBuilder.HeartsRootX1;
            float hy1 = Proto_GreyboxHudBuilder.HeartsRootY1;

            if (RectsOverlapPercent(hx0, hy0, hx1, hy1, Proto_GreyboxHudBuilder.JoystickHitX0,
                    Proto_GreyboxHudBuilder.JoystickHitY0, Proto_GreyboxHudBuilder.JoystickHitX1,
                    Proto_GreyboxHudBuilder.JoystickHitY1))
                return false;

            if (RectOverlapsCircleHitPercent(hx0, hy0, hx1, hy1, Proto_GreyboxHudBuilder.FireCenterX,
                    Proto_GreyboxHudBuilder.FireCenterY, Proto_GreyboxHudBuilder.FirePlaceRadiusPercent, screenW,
                    screenH))
                return false;
            if (RectOverlapsCircleHitPercent(hx0, hy0, hx1, hy1, Proto_GreyboxHudBuilder.ReloadCenterX,
                    Proto_GreyboxHudBuilder.ReloadCenterY, Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent,
                    screenW, screenH))
                return false;
            if (RectOverlapsCircleHitPercent(hx0, hy0, hx1, hy1, Proto_GreyboxHudBuilder.GrenadeCenterX,
                    Proto_GreyboxHudBuilder.GrenadeCenterY, Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent,
                    screenW, screenH))
                return false;
            if (RectOverlapsCircleHitPercent(hx0, hy0, hx1, hy1, Proto_GreyboxHudBuilder.RollCenterX,
                    Proto_GreyboxHudBuilder.RollCenterY, Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent,
                    screenW, screenH))
                return false;

            return true;
        }
    }
}
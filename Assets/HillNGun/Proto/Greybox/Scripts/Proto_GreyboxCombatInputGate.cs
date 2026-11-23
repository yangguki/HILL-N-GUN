using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Pure input gating for EditMode tests (no Unity Input / timeScale).</summary>
    public static class Proto_GreyboxCombatInputGate
    {
        public static bool RectsOverlap(float ax0, float ay0, float ax1, float ay1, float bx0, float by0, float bx1,
            float by1)
        {
            if (ax1 <= bx0 || bx1 <= ax0) return false;
            if (ay1 <= by0 || by1 <= ay0) return false;
            return true;
        }

        public static bool JoystickCaptureOnBegan(float xPct, float yPct, bool began) =>
            began && (Proto_Greybox3DHudLayout.Enabled
                ? Proto_Greybox3DHudLayout.JoystickHit(xPct, yPct)
                : (xPct >= Proto_GreyboxHudBuilder.JoystickHitX0 && xPct <= Proto_GreyboxHudBuilder.JoystickHitX1
                   && yPct >= Proto_GreyboxHudBuilder.JoystickHitY0 && yPct <= Proto_GreyboxHudBuilder.JoystickHitY1));

        public static bool CircleHitsOverlap(float ax, float ay, float aPlaceRadiusPercent, float bx, float by,
            float bPlaceRadiusPercent, float paddingPx = Proto_GreyboxHudBuilder.ActionClusterHitPaddingPx)
        {
            float aHit = Proto_GreyboxHudBuilder.CircleHitRadiusPx(aPlaceRadiusPercent, paddingPx);
            float bHit = Proto_GreyboxHudBuilder.CircleHitRadiusPx(bPlaceRadiusPercent, paddingPx);
            float dx = (ax - bx) / 100f * Proto_GreyboxHudBuilder.RefWidth;
            float dy = (ay - by) / 100f * Proto_GreyboxHudBuilder.RefHeight;
            return dx * dx + dy * dy < (aHit + bHit) * (aHit + bHit);
        }

        public static bool FireCircleHit(float xPct, float yPct) =>
            Proto_Greybox3DHudLayout.Enabled
                ? Proto_Greybox3DHudLayout.CircleHit(xPct, yPct, Proto_Greybox3DHudLayout.AttackX, Proto_Greybox3DHudLayout.AttackY, Proto_Greybox3DHudLayout.AttackRadius)
                : Proto_GreyboxHudBuilder.CircleHitTest(xPct, yPct, Proto_GreyboxHudBuilder.FireCenterX,
                    Proto_GreyboxHudBuilder.FireCenterY, Proto_GreyboxHudBuilder.FirePlaceRadiusPercent);

        public static bool ReloadCircleHit(float xPct, float yPct) =>
            Proto_Greybox3DHudLayout.Enabled
                ? Proto_Greybox3DHudLayout.CircleHit(xPct, yPct, Proto_Greybox3DHudLayout.ReloadX, Proto_Greybox3DHudLayout.ReloadY, Proto_Greybox3DHudLayout.ReloadRadius)
                : Proto_GreyboxHudBuilder.CircleHitTest(xPct, yPct, Proto_GreyboxHudBuilder.ReloadCenterX,
                    Proto_GreyboxHudBuilder.ReloadCenterY, Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent);

        public static bool GrenadeCircleHit(float xPct, float yPct) =>
            Proto_Greybox3DHudLayout.Enabled
                ? Proto_Greybox3DHudLayout.CircleHit(xPct, yPct, Proto_Greybox3DHudLayout.SkillX, Proto_Greybox3DHudLayout.SkillY, Proto_Greybox3DHudLayout.SkillRadius)
                : Proto_GreyboxHudBuilder.CircleHitTest(xPct, yPct, Proto_GreyboxHudBuilder.GrenadeCenterX,
                    Proto_GreyboxHudBuilder.GrenadeCenterY, Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent);

        public static bool RollCircleHit(float xPct, float yPct) =>
            Proto_Greybox3DHudLayout.Enabled
                ? Proto_Greybox3DHudLayout.CircleHit(xPct, yPct, Proto_Greybox3DHudLayout.RollX, Proto_Greybox3DHudLayout.RollY, Proto_Greybox3DHudLayout.RollRadius)
                : Proto_GreyboxHudBuilder.CircleHitTest(xPct, yPct, Proto_GreyboxHudBuilder.RollCenterX,
                    Proto_GreyboxHudBuilder.RollCenterY, Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent);

        public static Proto_GreyboxInput StripCombatWhenPaused(Proto_GreyboxInput input)
        {
            input.Move = Vector2.zero;
            input.RollPressed = false;
            input.FireQuickAuto = false;
            input.FireStickHeld = false;
            input.FireStickReleased = false;
            input.FireStickDragWorld = Vector2.zero;
            input.MouseAimHeld = false;
            input.MouseAimReleased = false;
            input.ReloadPressed = false;
            input.GrenadePressed = false;
            input.GrenadeStickHeld = false;
            input.GrenadeStickReleased = false;
            input.GrenadeStickDragWorld = Vector2.zero;
            return input;
        }
    }
}
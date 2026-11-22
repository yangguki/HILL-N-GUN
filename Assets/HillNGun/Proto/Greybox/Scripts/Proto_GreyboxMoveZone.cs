using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxMoveZone
    {
        public const float FootXPercentMin = 22f;
        public const float FootXPercentMax = 78f;
        public const float FootYPercentMinSliderMin = 50f;
        public const float FootYPercentMinSliderMax = 74f;
        public const float FenceFootMarginWorld = 0.08f;

        static float s_footYPercentMin;

        static Proto_GreyboxMoveZone() => ResetToDefaults();

        public static float FootYPercentMinCurrent => s_footYPercentMin;

        public static void SetFootYPercentMin(float percentFromTop) =>
            s_footYPercentMin = Mathf.Clamp(percentFromTop, FootYPercentMinSliderMin, FootYPercentMinSliderMax);

        public static void ResetToDefaults() =>
            s_footYPercentMin = Proto_GreyboxPerspectiveDepth.MoveFootMinPercent;

        public static float MinFootWorldY => Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.MoveFootMaxPercent);

        public static float MaxFootWorldY
        {
            get
            {
                float fromPercent = Proto_GreyboxLayout.ScreenYPercentToWorldY(s_footYPercentMin);
                float fenceCap = FenceBottomWorldY - FenceFootMarginWorld;
                return Mathf.Min(fromPercent, fenceCap);
            }
        }

        public static float MinFootWorldX => Proto_GreyboxLayout.ScreenXPercentToWorldX(FootXPercentMin);
        public static float MaxFootWorldX => Proto_GreyboxLayout.ScreenXPercentToWorldX(FootXPercentMax);
        public static float FenceBottomWorldY =>
            Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.FenceBottomPercent);

        public static Vector2 Clamp(Vector2 foot) =>
            new Vector2(
                Mathf.Clamp(foot.x, MinFootWorldX, MaxFootWorldX),
                Mathf.Clamp(foot.y, MinFootWorldY, MaxFootWorldY));
    }
}
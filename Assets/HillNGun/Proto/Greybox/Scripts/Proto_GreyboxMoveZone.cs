using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxMoveZone
    {
        public const float FootYPercentMin = 75f;
        public const float FootYPercentMax = 90f;
        public const float FootXPercentMin = 22f;
        public const float FootXPercentMax = 78f;

        public static float MinFootWorldY => Proto_GreyboxLayout.ScreenYPercentToWorldY(FootYPercentMax);
        public static float MaxFootWorldY => Proto_GreyboxLayout.ScreenYPercentToWorldY(FootYPercentMin);
        public static float MinFootWorldX => Proto_GreyboxLayout.ScreenXPercentToWorldX(FootXPercentMin);
        public static float MaxFootWorldX => Proto_GreyboxLayout.ScreenXPercentToWorldX(FootXPercentMax);
        public static float FenceBottomWorldY => Proto_GreyboxLayout.ScreenYPercentToWorldY(46f);

        public static Vector2 Clamp(Vector2 foot) =>
            new Vector2(
                Mathf.Clamp(foot.x, MinFootWorldX, MaxFootWorldX),
                Mathf.Clamp(foot.y, MinFootWorldY, MaxFootWorldY));
    }
}
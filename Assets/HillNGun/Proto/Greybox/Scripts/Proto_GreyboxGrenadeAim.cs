using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Screen-drag grenade aim: landing point and release intent (EditMode-testable).</summary>
    public static class Proto_GreyboxGrenadeAim
    {
        public const float MaxThrowRange = 7f;

        public static Vector2 LandingFromScreenDrag(Vector2 playerFoot, Vector2 screenDelta, float screenW, float screenH,
            float stickRadiusPx, float maxRange = MaxThrowRange)
        {
            float mag = screenDelta.magnitude;
            if (mag < 1e-6f || stickRadiusPx < 1e-6f)
                return playerFoot;
            float t = Mathf.Clamp01(mag / stickRadiusPx);
            var dir = Proto_GreyboxPointerMath.WorldDirectionFromScreenDrag(screenDelta, screenW, screenH);
            return Proto_GreyboxAim.ClipAimEnd(playerFoot, dir, maxRange * t);
        }

        public static Vector2 AutoThrowLanding(Vector2 playerFoot, IList<Proto_GreyboxTarget> targets)
        {
            var nearest = Proto_GreyboxTarget.SelectNearest(playerFoot, targets);
            if (nearest != null && nearest.IsAlive)
                return nearest.BodyCenterGround;
            return Proto_GreyboxAim.DefaultAimGround;
        }
    }
}
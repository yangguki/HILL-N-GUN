using UnityEngine;
using HillNGun.Proto.Greybox;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>Sim foot (x, worldY) → 3D ground (x, 0, z). Higher sim Y (screen-up / far) → larger +Z.</summary>
    public static class Proto_Greybox3DFootMap
    {
        public const float DepthZNear = 0f;
        public const float DepthZFar = 22f;

        public static float FootWorldYPlayerNear =>
            Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.MoveFootMaxPercent);

        public static float FootWorldYEnemyFar =>
            Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.PerspectiveEnemyFootMinPercent);

        public static float FootWorldYFence =>
            Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.FenceBottomPercent);

        public static float FootWorldYToDepthZ(float footWorldY)
        {
            float t = Mathf.InverseLerp(FootWorldYPlayerNear, FootWorldYEnemyFar, footWorldY);
            return Mathf.Lerp(DepthZNear, DepthZFar, Mathf.Clamp01(t));
        }

        public static Vector3 FootToWorld(Vector2 foot) =>
            new Vector3(foot.x, 0f, FootWorldYToDepthZ(foot.y));

        public static Vector3 GroundPointToWorld(Vector2 ground)
        {
            var pos = FootToWorld(ground);
            pos.y = 0.02f;
            return pos;
        }

        public static float FenceDepthZ => FootWorldYToDepthZ(FootWorldYFence);

        public static float PlayerBandNearZ => FootWorldYToDepthZ(FootWorldYPlayerNear);

        public static float CharacterHeightAtFoot(Vector2 foot, bool player)
        {
            float pct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(foot.y);
            if (player)
            {
                float hPct = Proto_GreyboxPerspectiveDepth.PlayerHeightScreenPercentAtFootPercent(pct);
                return Proto_GreyboxLayout.ScreenPercentToWorldHeight(hPct) * 0.95f;
            }

            float ePct = Proto_GreyboxPerspectiveDepth.EnemyHeightScreenPercentAtFootPercent(pct);
            return Proto_GreyboxLayout.ScreenPercentToWorldHeight(ePct) * 0.92f;
        }
    }
}
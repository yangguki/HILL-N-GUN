using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>1-point rear depth bands (screen % from top). Toggle restores task-15 flat layout.</summary>
    public static class Proto_GreyboxPerspectiveDepth
    {
        public const bool DefaultEnabled = true;

        public const float VanishingPointXPercent = 50f;
        public const float VanishingPointYPercent = 33f;

        public const float PerspectiveEnemyFootMinPercent = 40f;
        public const float PerspectiveEnemyFootMaxPercent = 52f;
        public const float PerspectiveEnemyHeightFarPercent = 6f;
        public const float PerspectiveEnemyHeightNearPercent = 8f;
        public const float PerspectiveFenceTopPercent = 56f;
        public const float PerspectiveFenceBottomPercent = 59f;
        public const float PerspectiveMoveFootMinPercent = 62f;
        public const float PerspectiveMoveFootMaxPercent = 95f;

        public const bool DefaultPlayerPerspectiveEnabled = true;
        public const bool DefaultPlayerDepthMoveEnabled = true;
        public const float DefaultPlayerHeightFarPercent = 9f;
        public const float DefaultPlayerHeightNearPercent = 12f;
        public const float PlayerVerticalMoveSlowAtFar = 0.9f;

        public const float FlatEnemyFootPercent = 37f;
        public const float FlatEnemyHeightPercent = 8f;
        public const float FlatFenceTopPercent = 43f;
        public const float FlatFenceBottomPercent = 46f;
        public const float FlatMoveFootMinPercent = 60f;
        public const float FlatMoveFootMaxPercent = 90f;

        static bool s_enabled = DefaultEnabled;
        static bool s_playerPerspective = DefaultPlayerPerspectiveEnabled;
        static bool s_playerDepthMove = DefaultPlayerDepthMoveEnabled;
        static float s_playerHeightTop = DefaultPlayerHeightFarPercent;
        static float s_playerHeightBottom = DefaultPlayerHeightNearPercent;

        public static bool Enabled
        {
            get => s_enabled;
            set => s_enabled = value;
        }

        public static bool PlayerPerspectiveEnabled
        {
            get => s_playerPerspective;
            set => s_playerPerspective = value;
        }

        public static bool PlayerDepthMoveEnabled
        {
            get => s_playerDepthMove;
            set => s_playerDepthMove = value;
        }

        public static float PlayerHeightTopPercent
        {
            get => s_playerHeightTop;
            set => s_playerHeightTop = Mathf.Clamp(value, 8f, 24f);
        }

        public static float PlayerHeightBottomPercent
        {
            get => s_playerHeightBottom;
            set => s_playerHeightBottom = Mathf.Clamp(value, 12f, 30f);
        }

        public static void ResetToDefault()
        {
            s_enabled = DefaultEnabled;
            s_playerPerspective = DefaultPlayerPerspectiveEnabled;
            s_playerDepthMove = DefaultPlayerDepthMoveEnabled;
            s_playerHeightTop = DefaultPlayerHeightFarPercent;
            s_playerHeightBottom = DefaultPlayerHeightNearPercent;
        }

        public static float FenceTopPercent => Enabled ? PerspectiveFenceTopPercent : FlatFenceTopPercent;
        public static float FenceBottomPercent => Enabled ? PerspectiveFenceBottomPercent : FlatFenceBottomPercent;
        public static float MoveFootMinPercent => Enabled ? PerspectiveMoveFootMinPercent : FlatMoveFootMinPercent;
        public static float MoveFootMaxPercent => Enabled ? PerspectiveMoveFootMaxPercent : FlatMoveFootMaxPercent;
        public static float DefaultEnemyFootPercent => Enabled ? PerspectiveEnemyFootMinPercent : FlatEnemyFootPercent;

        public const float GruntReferenceBodyPx = 120f;
        public const float ReferenceScreenHeightPx = 1080f;

        public static float ClampedFootFraction(float footPercentFromTop) =>
            Mathf.Clamp(footPercentFromTop / 100f, 0.40f, 0.52f);

        public static float EnemyBodyHeightScreenPx(float footPercentFromTop)
        {
            if (!Enabled)
                return ReferenceScreenHeightPx * FlatEnemyHeightPercent / 100f;
            float yf = ClampedFootFraction(footPercentFromTop);
            return ReferenceScreenHeightPx * (0.06f + (yf - 0.40f) / 0.12f * 0.02f);
        }

        public static float EnemyHeightScreenPercentAtFootPercent(float footPercentFromTop)
        {
            if (!Enabled)
                return FlatEnemyHeightPercent;
            return EnemyBodyHeightScreenPx(footPercentFromTop) / ReferenceScreenHeightPx * 100f;
        }

        public static float GruntSpriteUniformScaleAtFootPercent(float footPercentFromTop) =>
            EnemyBodyHeightScreenPx(footPercentFromTop) / GruntReferenceBodyPx;

        public static float EnemyHeightScreenPercentAtFootWorldY(float footWorldY) =>
            EnemyHeightScreenPercentAtFootPercent(Proto_GreyboxLayout.WorldYToScreenPercentFromTop(footWorldY));

        public static float EnemyBodyHeightWorld(float footWorldY) =>
            Proto_GreyboxLayout.ScreenPercentToWorldHeight(EnemyHeightScreenPercentAtFootWorldY(footWorldY));

        /// <summary>Scale relative to near row (~86.4px body at 52%, 8% of 1080).</summary>
        public static float EnemyVisualScaleAtFootPercent(float footPercentFromTop)
        {
            float nearPx = EnemyBodyHeightScreenPx(PerspectiveEnemyFootMaxPercent);
            return EnemyBodyHeightScreenPx(footPercentFromTop) / Mathf.Max(nearPx, 0.001f);
        }

        public static float EnemyVisualScaleAtFootWorldY(float footWorldY) =>
            EnemyVisualScaleAtFootPercent(Proto_GreyboxLayout.WorldYToScreenPercentFromTop(footWorldY));

        public static float EnemyHitScaleAtFootWorldY(Vector2 foot) =>
            Enabled ? EnemyVisualScaleAtFootWorldY(foot.y) : 1f;

        public static float DefaultAimFootPercent =>
            Enabled ? 46f : 38f;

        /// <summary>0 = far/top of move band, 1 = near/bottom. Linear in screen foot % (same style as enemy row lerp).</summary>
        public static float PlayerDepthTAtFootPercent(float footPercentFromTop)
        {
            float min = MoveFootMinPercent;
            float max = MoveFootMaxPercent;
            return Mathf.Clamp01((footPercentFromTop - min) / Mathf.Max(max - min, 0.001f));
        }

        static float PlayerHeightFarPercent =>
            Mathf.Min(PlayerHeightTopPercent, PlayerHeightBottomPercent - 0.01f);

        public static float PlayerHeightScreenPercentAtFootPercent(float footPercentFromTop)
        {
            if (!PlayerPerspectiveEnabled)
                return PlayerHeightBottomPercent;
            float t = PlayerDepthTAtFootPercent(footPercentFromTop);
            return Mathf.Lerp(PlayerHeightFarPercent, PlayerHeightBottomPercent, t);
        }

        public static float PlayerHeightScreenPercentAtFootWorldY(float footWorldY) =>
            PlayerHeightScreenPercentAtFootPercent(Proto_GreyboxLayout.WorldYToScreenPercentFromTop(footWorldY));

        public static float PlayerBodyHeightWorld(float footWorldY) =>
            Proto_GreyboxLayout.ScreenPercentToWorldHeight(PlayerHeightScreenPercentAtFootWorldY(footWorldY));

        /// <summary>Uniform scale relative to bottom/near row height.</summary>
        public static float PlayerVisualScaleAtFootPercent(float footPercentFromTop)
        {
            float nearPct = PlayerHeightScreenPercentAtFootPercent(MoveFootMaxPercent);
            float at = PlayerHeightScreenPercentAtFootPercent(footPercentFromTop);
            return at / Mathf.Max(nearPct, 0.001f);
        }

        public static float PlayerVisualScaleAtFootWorldY(float footWorldY) =>
            PlayerVisualScaleAtFootPercent(Proto_GreyboxLayout.WorldYToScreenPercentFromTop(footWorldY));

        public static float PlayerVerticalMoveSpeedFactorAtFootPercent(float footPercentFromTop)
        {
            if (!PlayerPerspectiveEnabled || !PlayerDepthMoveEnabled)
                return 1f;
            float t = PlayerDepthTAtFootPercent(footPercentFromTop);
            return Mathf.Lerp(PlayerVerticalMoveSlowAtFar, 1f, t);
        }

        public static float PlayerVerticalMoveSpeedFactorAtFootWorldY(float footWorldY) =>
            PlayerVerticalMoveSpeedFactorAtFootPercent(Proto_GreyboxLayout.WorldYToScreenPercentFromTop(footWorldY));

        public static float AllyBulletVisualScaleAtFootWorldY(float footWorldY) =>
            PlayerPerspectiveEnabled
                ? PlayerVisualScaleAtFootWorldY(footWorldY)
                : Proto_DepthScale.BulletScaleAt(footWorldY);
    }
}
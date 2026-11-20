namespace HillNGun.Proto.Greybox
{
    /// <summary>1280×720 / orthographic size 5 — horizontal span from camera aspect (screen % from top-left).</summary>
    public static class Proto_GreyboxLayout
    {
        public const float WorldHeight = 10f;
        public static float WorldWidth => 2f * CameraOrthographicSize * TargetAspect;
        public const float CameraOrthographicSize = 5f;
        public const float CameraPositionY = 5f;
        public const float CameraPositionZ = -10f;
        public const float TargetAspect = 16f / 9f;

        public const float EnemyFarFootWorldY = 6.3f;
        public const float PlayerNearFootWorldY = 1.2f;

        public static float ScreenYPercentToWorldY(float percentFromTop) =>
            WorldHeight * (1f - percentFromTop / 100f);

        public static float ScreenXPercentToWorldX(float percentFromLeft) =>
            WorldWidth * (percentFromLeft / 100f) - WorldWidth * 0.5f;

        public static float WorldYToScreenPercentFromTop(float worldY) =>
            (WorldHeight - worldY) / WorldHeight * 100f;

        public static float WorldHeightToScreenPercent(float worldHeight) =>
            worldHeight / WorldHeight * 100f;

        public static float ScreenPercentToWorldHeight(float screenPercent) =>
            screenPercent / 100f * WorldHeight;

        public static int SortingOrderAtFootY(float footWorldY) => -(int)(footWorldY * 100f);

        /// <summary>Fixed background layers (higher order = drawn in front).</summary>
        public const int SortingSky = -500;
        public const int SortingBuildingBase = -450;
        public const int SortingEnemyMarker = -435;
        public const int SortingCrateFar = -395;
        public const int SortingGround = -400;
        public const int SortingFence = -390;
        public const int SortingCombatOverlay = 200;
        /// <summary>Screen Space — Camera HUD above all world sprites (combat overlay tops ~200+).</summary>
        public const int SortingHudCanvas = 1000;

        /// <summary>탄·폭발 등이 울타리·상자(-390대) 앞에 그려지도록 최소 정렬 보장.</summary>
        public static int CombatDrawSortingOrder(float footWorldY) =>
            UnityEngine.Mathf.Max(SortingOrderAtFootY(footWorldY) + SortingCombatOverlay, SortingFence + 10);

        public static float MinAllyBulletWorldDiameter =>
            ScreenPercentToWorldHeight(0.6f);

        public const float AimVisualTopScreenPercent = 12f;

        /// <summary>Keep aim line end / crosshair below top HUD (~12% from top), minus half reticle height.</summary>
        public static float AimVisualMaxWorldY =>
            ScreenYPercentToWorldY(AimVisualTopScreenPercent) - ScreenPercentToWorldHeight(5f) * 0.5f;

        public static float AimVisualMinWorldY => 0.05f;
        public static float AimVisualMinWorldX => -WorldWidth * 0.5f + 0.05f;
        public static float AimVisualMaxWorldX => WorldWidth * 0.5f - 0.05f;
    }
}
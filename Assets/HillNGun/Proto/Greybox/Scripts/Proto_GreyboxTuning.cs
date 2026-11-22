namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxTuning
    {
        /// <summary>
        /// Brawl Stars "normal" is 720 units/s on a 300-unit tile ≈ 2.4 tiles/s,
        /// and a brawler is about one tile wide, so we target 2.4 hero body-widths
        /// per second. Mid-band visible width (default perspective on) is
        /// WorldWidth * PlayerCapsuleWidthPercent/100 * (mid height / near height).
        /// After the Brawl Stars size pass, near height is 12% and mid is 10.5% (scale 0.875),
        /// width percent is 3.36 (12/20 of the old 5.6). Mid-band width =
        /// (2*5*(16/9)) * 0.0336 * 0.875 = 0.5227 world units.
        /// 2.4 * 0.5227 = 1.254, baked as 1.25. Previously 4.2, then 2.03 at the old 20% height.
        /// Shared by the 2D and 3D views (same motor). Vertical depth factor is unchanged.
        /// </summary>
        public const float MoveSpeedBodyWidthsPerSec = 2.4f;
        public const float MoveSpeed = 1.25f;

        public static float MidBandFootPercent =>
            (Proto_GreyboxPerspectiveDepth.MoveFootMinPercent +
             Proto_GreyboxPerspectiveDepth.MoveFootMaxPercent) * 0.5f;

        public static float HeroVisibleWidthAtMidBand()
        {
            float scale = Proto_GreyboxPerspectiveDepth.PlayerVisualScaleAtFootPercent(MidBandFootPercent);
            return Proto_GreyboxLayout.WorldWidth * (PlayerCapsuleWidthPercent / 100f) * scale;
        }

        /// <summary>Walk-cycle travel as a multiple of mid-band body width (two steps ≈ a little over one width).</summary>
        public const float WalkCycleDistanceInBodyWidths = 1.25f;

        public const float RollDistance = 2.2f;
        public const float RollDuration = 0.28f;
        public const float RollInvulnDuration = 0.22f;
        public const float RollCooldown = 0f;
        public const int PlayerMaxHp = 3;
        public const int PlayerMaxHearts = 3;
        public const float HeartRegenSec = 10f;
        public const float PlayerHitIFrameSec = 0.5f;
        public const int RollChainMax = 3;
        public const float RollChainLockSec = 5f;
        public const float EnemyBulletSpeed = 4.2f;
        public const float EnemyBulletDiameter = 0.17f;
        public const float CoverDamageReduction = 1f;
        public const float PlayerCapsuleHeightPercent = 12f;
        public const float PlayerCapsuleWidthPercent = 3.36f;
        public const int PlayerDamagePerHit = 5;

        public const float AllyBulletSpeed = 4.2f;
        public const int AllyBulletDamage = 3;
        public const float AllyBulletWidthOfHero = 0.25f;
        public const float EnemyBulletWidthOfHero = 0.33f;
        public const float AllyBulletDiameter = 0.13f;
        public const float AllyBulletVisualDiameter = 0.13f;
        public const float AllyBulletVisualTracerLength = 0.32f;
        public const float AllyBulletTracerLength = 0.50f;
        public const float FireInterval = 0.33f;
        public const int MagazineSize = 5;
        public const float ReloadRoundInterval = 0.3f;
        /// <summary>Legacy single-fill duration; gun uses <see cref="ReloadRoundInterval"/> per round.</summary>
        public const float ReloadDuration = 0.5f;
        public const int TargetHp = 3;
        public const int ScorePerKill = 100;
        public const float GrenadeCooldown = 6f;
        public const float GrenadeRadius = 1.2f;
        public const int GrenadeDamage = 3;
        public const float GrenadeFlightTime = 0.6f;
        public const float AimDragMaxX = 1.5f;
        public const float AimDragMaxY = 0.6f;
        public const float AimDragReturnDuration = 0.3f;
        public const float AllyBulletNearCrateIgnore = 0.3f;
        public const float TargetWidthPercent = 3.5f;
        public const float TargetHeightPercent = 8f;
        public const float ExplosionVisualDuration = 0.3f;

        public const float EnemyFireInterval = 1.6f;
        public const int Wave1EnemyCount = 3;
        public const int Wave2EnemyCount = 5;
        public const int Wave3EnemyCount = 8;
        public const float WaveClearDelay = 1.5f;
        public const float GruntWalkSpeed = 2.5f;
        public const float GruntStrafeRange = 0.6f;
        public const float GruntSpawnInterval = 0.35f;
        public const int EnemyBulletDamage = 1;
        public const float DepthFarScale = 1f;
        public const float DepthNearScale = 2.5f;
        public const float BulletDepthScaleCap = 1f;
        public const float EnemyBulletGlowDiameterMul = 1.3f;
        public const float EnemyBulletVisualDiameterMul = 1f;
        public const float EnemyBulletVisualGlowMul = 1.45f;

        public const float PlayerMuzzleFlashHeightPercent = 5.5f;
        public const float PlayerMuzzleFlashDuration = 0.18f;
        public const float GrenadeArcVisualHeight = 1.35f;
        public const float RollDizzyWobbleDeg = 4f;
        public const float RollDizzyStunWobbleDeg = 11f;
        public const float GruntMuzzleFlashHeightPercent = 3.2f;
        public const float GruntMuzzleFlashDuration = 0.1f;
    }
}
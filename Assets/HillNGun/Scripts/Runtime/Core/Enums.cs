namespace HillNGun.Core
{
    public enum Team
    {
        Player,
        Enemy
    }

    public enum Role
    {
        Assault,
        Sniper,
        Support
    }

    public enum FireMode
    {
        Auto,
        ChargeRelease
    }

    public enum SkillUseType
    {
        Tap,
        Hold,
        DragRelease
    }

    /// <summary>What a skill does when activated (logic lives in Combat; numbers in SkillData.parameters).</summary>
    public enum SkillEffect
    {
        None,
        SpreadBoost,     // 버스터 돌파 사격: spreadDeg, coverDamageMul for duration
        PierceNextShot,  // 레이븐 관통탄: next shot pierces pierceCovers, afterPierceMul
        RepairCover      // 볼트 긴급 보수: own cover near target +repairPercent
    }

    public enum PassiveType
    {
        RollShot,
        Weakpoint,
        FastRevive
    }

    public enum MovementState
    {
        Idle,
        Moving,
        RowChanging,
        Rolling
    }

    public enum ActionState
    {
        None,
        Idle,
        Aiming,
        Charging,
        Firing,
        Reloading,
        Skill,
        GrenadeAiming,
        UsingSkill
    }

    public enum CharacterOverallState
    {
        Active,
        Downed,
        BeingRevived,
        Eliminated
    }

    public enum CoverStage
    {
        Intact,
        Cracked,
        Damaged,
        Destroyed
    }

    public enum GameMode
    {
        None,
        Practice,
        PvE,
        PvP
    }

    public enum GamePhase
    {
        None,
        Title,
        ModeSelect,
        CharacterSelect,
        Match,
        Result
    }

    public enum MatchResult
    {
        None,
        Victory,
        Defeat,
        Draw
    }

    /// <summary>Why a PvP round ended (for HUD / result).</summary>
    public enum RoundEndReason
    {
        None,
        TeamWiped,
        TimeoutAlive,
        TimeoutHp,
        TimeoutDraw
    }

    public enum PvERoundResult
    {
        InProgress,
        StageCleared,
        StageFailed,
        BossDefeated
    }

    public enum PvPRoundResult
    {
        InProgress,
        PlayerWins,
        EnemyWins,
        Draw
    }

    public enum EnemyType
    {
        PistolMook,
        DynamiteBandit,
        MountedRaider,
        BossRattlesnakeRex,
        /// <summary>Stationary second-floor window shooter (FreeMove PvE, spawn type BalconySniper).</summary>
        BalconySniper
    }

    /// <summary>How a mode moves its characters (mode data flag: MatchRulesData.pveMoveMode / pvpMoveMode).</summary>
    public enum MoveMode
    {
        /// <summary>Row lanes: horizontal stick movement + discrete row changes (PvP).</summary>
        RowLane,
        /// <summary>360-degree free movement inside the street walk area, cover colliders (PvE).</summary>
        FreeMove
    }

    /// <summary>FreeMove PvE enemy entrance kinds (StageSpawnPoint.type). 0 = any (wave entry wildcard).</summary>
    public enum SpawnPointType
    {
        Any = 0,
        StorefrontDoor = 1,
        AlleyGap = 2,
        StreetEnd = 3,
        FenceClimb = 4,
        BalconySniper = 5
    }

    /// <summary>Which side of the screen a spawn point enters from (waves must use at least two).</summary>
    public enum SpawnSide
    {
        Left,
        Right,
        Bottom,
        Top
    }

    public enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3
    }
}

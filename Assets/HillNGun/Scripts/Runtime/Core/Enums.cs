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
        BossRattlesnakeRex
    }

    public enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3
    }
}

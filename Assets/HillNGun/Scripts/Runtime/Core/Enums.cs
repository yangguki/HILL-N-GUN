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
        Light,
        Medium,
        Heavy
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
        Defeat
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

using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.Progression
{
    /// <summary>06 §6: ★1 clear, ★2 under time, ★3 no downed (+ optional barrels). No stat growth.</summary>
    public static class StarRating
    {
        public static int Compute(StageData stage, bool cleared, float clearTimeSec, int downedCount, int barrelExplosions = 0)
        {
            if (!cleared || stage == null) return 0;
            int stars = 1;
            float limit = stage.starTimeUnderSec > 0f ? stage.starTimeUnderSec : stage.stageTimeMax;
            if (clearTimeSec <= limit) stars = 2;
            bool noDowned = !stage.starRequireNoDowned || downedCount <= 0;
            bool barrels = stage.starMinBarrelExplosions <= 0 || barrelExplosions >= stage.starMinBarrelExplosions;
            if (stars >= 2 && noDowned && barrels) stars = 3;
            return stars;
        }

        public static int ComputeFromSession(StageData stage, GameSession s, bool victory)
        {
            if (s == null) return 0;
            return Compute(stage, victory, s.pveStageClearTime > 0f ? s.pveStageClearTime : s.matchTime,
                s.pveDownedCount, s.pveBarrelExplosions);
        }
    }
}

namespace HillNGun.Proto.Greybox
{
  public class Proto_GreyboxBalanceSimStats
  {
    public int StartHearts;
    public int EnemyShotsFired;
    public int HeroHitsTaken;
    public int HeroShotsFired;
    public int HeroShotsHit;
    public int HeartsLost;
    public bool Knockdown;
    public float ClearTimeSec;
    public int Score;
    public int EnemiesKilled;

    public float HitTakenRate => EnemyShotsFired > 0 ? (float)HeroHitsTaken / EnemyShotsFired : 0f;
    public float HeroHitRate => HeroShotsFired > 0 ? (float)HeroShotsHit / HeroShotsFired : 0f;
  }

  /// <summary>When Active, controller increments counters for deterministic balance sim (start Y only).</summary>
  public static class Proto_GreyboxBalanceSimContext
  {
    public static bool Active;
    public static Proto_GreyboxBalanceSimStats Stats;

    public static void BeginRun(int startHearts)
    {
      Active = true;
      Stats = new Proto_GreyboxBalanceSimStats { StartHearts = startHearts };
    }

    public static void EndRun() => Active = false;

    public static void NotifyEnemyShotFired()
    {
      if (Active) Stats.EnemyShotsFired++;
    }

    public static void NotifyHeroHit()
    {
      if (!Active) return;
      Stats.HeroHitsTaken++;
      Stats.HeartsLost++;
    }

    public static void NotifyHeroShotFired()
    {
      if (Active) Stats.HeroShotsFired++;
    }

    public static void NotifyHeroShotHit()
    {
      if (Active) Stats.HeroShotsHit++;
    }
  }
}
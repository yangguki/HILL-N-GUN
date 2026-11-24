using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
  public static class Proto_GreyboxBalanceSimRunner
  {
    public enum Policy { StandShoot, Strafe }

    const float Dt = 1f / 60f;
    const float MaxStageSec = 180f;

    public static Proto_GreyboxBalanceSimStats RunStage1(
        Proto_GreyboxPlayerController controller, int seed, float startFootWorldY, Policy policy,
        float maxSimSec = MaxStageSec)
    {
      Proto_GreyboxCapturePersp2D.Active = false;
      Proto_GreyboxCapturePersp2D.IgnorePlayerDamage = false;
      Proto_GreyboxViewModeState.SetMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
      controller.RuntimeTuning.WestPixelSkin = false;
      controller.SetWestPixelSkin(false);

      Random.InitState(seed);
      controller.BalanceSimSeedCoverRng(seed);

      Proto_GreyboxPlayerSpawn.StartFootWorldYOverride = startFootWorldY;
      controller.FullRestart();
      Proto_GreyboxPlayerSpawn.StartFootWorldYOverride = null;

      var ctrl = UnityEngine.Object.FindFirstObjectByType<Proto_GreyboxPlayerController>();
      if (ctrl == null) return new Proto_GreyboxBalanceSimStats();

      int startHearts = ctrl.Hp;
      Proto_GreyboxBalanceSimContext.BeginRun(startHearts);

      float t = 0f;
      float strafePhase = 0f;
      while (t < maxSimSec && ctrl.RunResult == Proto_GreyboxRunResult.Playing)
      {
        var input = BuildInput(ctrl, policy, t, ref strafePhase);
        ctrl.Simulate(input, Dt);
        t += Dt;
      }

      var stats = Proto_GreyboxBalanceSimContext.Stats ?? new Proto_GreyboxBalanceSimStats();
      stats.Score = ctrl.Score;
      stats.EnemiesKilled = ctrl.Score / Mathf.Max(1, Proto_GreyboxTuning.ScorePerKill);
      stats.Knockdown = ctrl.RunResult == Proto_GreyboxRunResult.Down;
      if (ctrl.RunResult == Proto_GreyboxRunResult.Clear)
        stats.ClearTimeSec = t;
      else
        stats.ClearTimeSec = maxSimSec;
      Proto_GreyboxBalanceSimContext.EndRun();
      ctrl.ClearAimOverride();
      return stats;
    }

    static Proto_GreyboxInput BuildInput(Proto_GreyboxPlayerController ctrl, Policy policy, float simTime,
        ref float strafePhase)
    {
      var foot = ctrl.Motor.FootPosition;
      var target = FindNearestEnemyFoot(ctrl);
      if (target.HasValue)
        ctrl.SetAimOverride(target.Value);

      Vector2 move = Vector2.zero;
      if (policy == Policy.Strafe)
      {
        strafePhase += 1f / 60f;
        float period = 2.5f;
        float sign = Mathf.FloorToInt(strafePhase / period) % 2 == 0 ? 1f : -1f;
        move = new Vector2(sign * 0.55f, 0f);
      }

      bool reload = ctrl.Gun != null && ctrl.Gun.Ammo <= 0 && !ctrl.Gun.IsReloading;
      return new Proto_GreyboxInput
      {
        Move = move,
        FireQuickAuto = true,
        ReloadPressed = reload
      };
    }

    static Vector2? FindNearestEnemyFoot(Proto_GreyboxPlayerController ctrl)
    {
      var player = ctrl.Motor.FootPosition;
      float best = float.MaxValue;
      Vector2? bestPos = null;
      foreach (var g in UnityEngine.Object.FindObjectsByType<Proto_GreyboxTargetView>(FindObjectsSortMode.None))
      {
        if (g == null || g.Logic == null || !g.Logic.IsAlive) continue;
        var p = g.Logic.FootPosition;
        float d = (p - player).sqrMagnitude;
        if (d >= best) continue;
        best = d;
        bestPos = p;
      }

      return bestPos;
    }
  }
}
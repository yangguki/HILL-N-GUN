using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HillNGun.Proto.Greybox.Editor
{
  public static class Proto_GreyboxBalanceSim
  {
    const string ReportPath = "_tmp_slice_k/balance_r6.txt";

    [MenuItem("HillNGun/Greybox/Balance Sim Persp2D Start Row (Round 6)")]
    public static void RunPersp2DStartRowCompareMenu() => RunPersp2DStartRowCompare();

    public static void RunPersp2DStartRowCompare()
    {
      var sb = new StringBuilder();
      sb.AppendLine("balance_r6 persp2d_start_foot_compare");
      sb.AppendLine($"utc={DateTime.UtcNow:O}");
      sb.AppendLine("A=start_foot_world_y=1.2 (legacy 88% row)");
      sb.AppendLine("B=persp2d_front_row (81.7% / override cleared, view Persp2D)");
      sb.AppendLine();

      float footB = Proto_GreyboxPlayerSpawn.PlayerStartFootWorldY(Proto_GreyboxViewMode.View2DPerspective);
      float footA = Proto_GreyboxLayout.PlayerNearFootWorldY;

      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = UnityEngine.Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      builder?.Build();

      var cases = new List<(string tag, float startFoot)>
      {
        ("A", footA),
        ("B", footB)
      };

      foreach (var policy in new[] { Proto_GreyboxBalanceSimRunner.Policy.StandShoot, Proto_GreyboxBalanceSimRunner.Policy.Strafe })
      {
        sb.AppendLine($"policy={policy}");
        var byTag = new Dictionary<string, List<Proto_GreyboxBalanceSimStats>>();
        foreach (var c in cases) byTag[c.tag] = new List<Proto_GreyboxBalanceSimStats>();

        for (int seed = 1; seed <= 20; seed++)
        {
          foreach (var c in cases)
          {
            var controller = UnityEngine.Object.FindFirstObjectByType<Proto_GreyboxPlayerController>();
            if (controller == null)
            {
              sb.AppendLine($"seed={seed} {c.tag} ERROR=no_player");
              continue;
            }

            var stats = Proto_GreyboxBalanceSimRunner.RunStage1(controller, seed, c.startFoot, policy);
            byTag[c.tag].Add(stats);
            sb.AppendLine(FormatRow(seed, c.tag, stats));
          }
        }

        AppendSummary(sb, "A", byTag["A"]);
        AppendSummary(sb, "B", byTag["B"]);
        AppendDeltas(sb, byTag["A"], byTag["B"]);
        sb.AppendLine();
      }

      var outPath = Path.Combine(Directory.GetCurrentDirectory(), ReportPath);
      Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
      File.WriteAllText(outPath, sb.ToString());
      Debug.Log($"Balance r6 report: {outPath}");
      EditorApplication.Exit(0);
    }

    static string FormatRow(int seed, string tag, Proto_GreyboxBalanceSimStats s) =>
      $"{tag} seed={seed} enemy_shots={s.EnemyShotsFired} hero_hits={s.HeroHitsTaken} hit_rate={s.HitTakenRate:F3} " +
      $"hearts_lost={s.HeartsLost} knockdown={s.Knockdown} clear_s={s.ClearTimeSec:F1} kills={s.EnemiesKilled} " +
      $"hero_shots={s.HeroShotsFired} hero_hits_on_enemy={s.HeroShotsHit} hero_acc={s.HeroHitRate:F3}";

    static void AppendSummary(StringBuilder sb, string tag, List<Proto_GreyboxBalanceSimStats> runs)
    {
      if (runs.Count == 0) return;
      sb.AppendLine($"{tag}_summary n={runs.Count}");
      sb.AppendLine($"  mean_enemy_shots={runs.Average(r => r.EnemyShotsFired):F1}");
      sb.AppendLine($"  mean_hero_hits={runs.Average(r => r.HeroHitsTaken):F2}");
      sb.AppendLine($"  mean_hit_rate={runs.Average(r => r.HitTakenRate):F3}");
      sb.AppendLine($"  mean_hearts_lost={runs.Average(r => r.HeartsLost):F2}");
      sb.AppendLine($"  mean_clear_s={runs.Average(r => r.ClearTimeSec):F1}");
      sb.AppendLine($"  mean_kills={runs.Average(r => r.EnemiesKilled):F2}");
      var clears = runs.Where(r => r.ClearTimeSec < 179.5f).Select(r => r.ClearTimeSec).ToList();
      sb.AppendLine($"  median_clear_s={(clears.Count > 0 ? Median(clears) : float.NaN):F1}");
    }

    static void AppendDeltas(StringBuilder sb, List<Proto_GreyboxBalanceSimStats> a, List<Proto_GreyboxBalanceSimStats> b)
    {
      if (a.Count == 0 || b.Count == 0) return;
      sb.AppendLine("delta_B_minus_A_means");
      sb.AppendLine($"  enemy_shots={b.Average(x => x.EnemyShotsFired) - a.Average(x => x.EnemyShotsFired):F2}");
      sb.AppendLine($"  hero_hits={b.Average(x => x.HeroHitsTaken) - a.Average(x => x.HeroHitsTaken):F2}");
      sb.AppendLine($"  hit_rate={b.Average(x => x.HitTakenRate) - a.Average(x => x.HitTakenRate):F3}");
      sb.AppendLine($"  hearts_lost={b.Average(x => x.HeartsLost) - a.Average(x => x.HeartsLost):F2}");
      sb.AppendLine($"  kills={b.Average(x => x.EnemiesKilled) - a.Average(x => x.EnemiesKilled):F2}");
    }

    static float Median(List<float> values)
    {
      var sorted = values.OrderBy(v => v).ToList();
      int n = sorted.Count;
      return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) * 0.5f;
    }
  }
}
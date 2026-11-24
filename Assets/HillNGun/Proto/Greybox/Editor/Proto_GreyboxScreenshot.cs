using System.IO;
using HillNGun.Proto.Greybox3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox.Editor
{
  public static class Proto_GreyboxScreenshot
  {
    const int Width = 1280;
    const int Height = 720;
    const string DefaultOutput = "v4_greybox_task3.png";

    public static void Capture()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();
      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }
      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      SaveRender(cam, DefaultOutput);
      EditorApplication.Exit(0);
    }

    public static void CaptureMoveSequence()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      const float dt = 0.02f;
      CaptureToFile("v4_task2_seq_0_start.png", cam);

      for (int i = 0; i < 60; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = Vector2.left }, dt);
      CaptureToFile("v4_task2_seq_1_left.png", cam);

      for (int i = 0; i < 80; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = new Vector2(0.85f, -0.2f).normalized }, dt);
      CaptureToFile("v4_task2_seq_2_right_forward.png", cam);

      controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right, RollPressed = true }, dt);
      for (int i = 0; i < 7; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right, RollPressed = true }, dt);
      CaptureToFile("v4_task2_seq_3_midroll.png", cam);

      EditorApplication.Exit(0);
    }

    public static void CaptureCombatSequence()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.RefreshTargets();
      const float dt = 0.02f;

      for (int i = 0; i < 10; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      for (int i = 0; i < 40; i++)
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = true }, dt);
      for (int i = 0; i < 2; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      CaptureToFile("v4_task3_seq_1_bullets.png", cam);

      builder.Build();
      player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName); controller = player.GetComponent<Proto_GreyboxPlayerController>();
      controller.RefreshTargets();
      for (int i = 0; i < 5; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      CaptureToFile("v4_task3_seq_2_crosshair.png", cam);

      builder.Build();
      player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName); controller = player.GetComponent<Proto_GreyboxPlayerController>();
      controller.RefreshTargets();
      var farCx = (Proto_GreyboxLayout.ScreenXPercentToWorldX(52f) +
                   Proto_GreyboxLayout.ScreenXPercentToWorldX(56.5f)) * 0.5f;
      var aim = new Vector2(farCx, Proto_GreyboxLayout.ScreenYPercentToWorldY(36f));
      controller.SetAimOverride(aim);
      for (int i = 0; i < 5; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      controller.Simulate(new Proto_GreyboxInput { GrenadePressed = true }, dt);
      for (int i = 0; i < 32; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      CaptureToFile("v4_task3_seq_3_grenade.png", cam);

      EditorApplication.Exit(0);
    }

    public static void CaptureTask4Sequence()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      const float dt = 0.02f;

      int enemyBullets = 0;
      for (int i = 0; i < 900; i++)
      {
        controller.Simulate(new Proto_GreyboxInput(), dt);
        enemyBullets = 0;
        foreach (var b in controller.EnemyBullets)
          if (b.Alive) enemyBullets++;
        if (i > 150 && enemyBullets >= 2) break;
      }
      Debug.Log($"Task4 shot1 enemy bullets alive={enemyBullets} sim={controller.SimTime:0.00}");
      CaptureToFile("v4_task4_1_wave.png", cam);

      controller.FullRestart(); player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName); controller = player.GetComponent<Proto_GreyboxPlayerController>();
      var aim = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(54f),
        Proto_GreyboxLayout.ScreenYPercentToWorldY(36f));
      controller.SetAimOverride(aim);
      for (int i = 0; i < 30; i++)
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = true }, dt);
      controller.Simulate(new Proto_GreyboxInput { GrenadePressed = true }, dt);
      for (int i = 0; i < 34; i++)
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = true }, dt);
      int allyAlive = 0;
      foreach (var b in controller.AllyBullets)
        if (b.Alive) allyAlive++;
      Debug.Log($"Task4 shot2 ally bullets alive={allyAlive} grenadeVisible={controller.Grenade.ExplosionVisible(controller.SimTime)}");
      CaptureToFile("v4_task4_2_fire.png", cam);

      builder.Build();
      player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      controller = player.GetComponent<Proto_GreyboxPlayerController>();
      controller.SetDebugPanelOpen(true);
      for (int i = 0; i < 4; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      Debug.Log("Task4 shot3 debug panel open");
      CaptureToFile("v4_task4_3_sliders.png", cam);

      controller.FullRestart(); player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName); controller = player.GetComponent<Proto_GreyboxPlayerController>();
      for (int i = 0; i < 80; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      while (controller.RunResult == Proto_GreyboxRunResult.Playing && controller.SimTime < 240f)
      {
        if (controller.WaveDirector.State == Proto_GreyboxWaveDirectorState.Fighting)
          controller.WaveDirector.KillAllGruntsForTest();
        controller.Simulate(new Proto_GreyboxInput(), dt);
      }

      Debug.Log($"Task4 shot4 result={controller.RunResult} score={controller.Score}");
      CaptureToFile("v4_task4_4_result.png", cam);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask5Sequence()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      const float dt = 0.02f;
      var aim = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(54f),
        Proto_GreyboxLayout.ScreenYPercentToWorldY(36f));
      controller.SetAimOverride(aim);

      for (int i = 0; i < 500; i++)
      {
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = i % 4 != 0 }, dt);
        int enemyAlive = 0;
        foreach (var b in controller.EnemyBullets)
          if (b.Alive) enemyAlive++;
        int allyAlive = 0;
        foreach (var b in controller.AllyBullets)
          if (b.Alive) allyAlive++;
        if (i > 60 && enemyAlive >= 1 && allyAlive >= 1
            && controller.WaveDirector.State == Proto_GreyboxWaveDirectorState.Fighting)
          break;
      }

      player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var ppos = player != null ? player.transform.position : Vector3.zero;
      int enemyCount = 0;
      int allyCount = 0;
      foreach (var b in controller.EnemyBullets)
        if (b.Alive) enemyCount++;
      foreach (var b in controller.AllyBullets)
        if (b.Alive) allyCount++;
      Debug.Log(
        $"Task5 combat capture grunts={controller.WaveDirector.Grunts.Count} enemyBullets={enemyCount} allyBullets={allyCount} player=({ppos.x:F2},{ppos.y:F2}) wave={controller.WaveDirector.CurrentWave}");
      CaptureToFile("v4_task5_combat.png", cam);

      controller.FullRestart();
      player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing after restart.");
        EditorApplication.Exit(1);
        return;
      }

      for (int i = 0; i < 40; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      while (controller.RunResult == Proto_GreyboxRunResult.Playing && controller.SimTime < 240f)
      {
        if (controller.WaveDirector.State == Proto_GreyboxWaveDirectorState.Fighting)
          controller.WaveDirector.KillAllGruntsForTest();
        controller.Simulate(new Proto_GreyboxInput(), dt);
      }

      var overlay = GameObject.Find(Proto_GreyboxResultUi.RootName);
      int resultTexts = overlay != null ? overlay.GetComponentsInChildren<UnityEngine.UI.Text>(true).Length : 0;
      ppos = player != null ? player.transform.position : Vector3.zero;
      Debug.Log(
        $"Task5 result capture result={controller.RunResult} score={controller.Score} overlayActive={overlay != null && overlay.activeSelf} resultTexts={resultTexts} player=({ppos.x:F2},{ppos.y:F2})");
      CaptureToFile("v4_task5_result.png", cam);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask6Sequence()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      var aim = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(54f),
        Proto_GreyboxLayout.ScreenYPercentToWorldY(36f));
      controller.SetAimOverride(aim);

      const float dt = 0.02f;
      for (int i = 0; i < 520; i++)
      {
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = i % 5 != 0 }, dt);
        int enemyAlive = 0;
        foreach (var b in controller.EnemyBullets)
          if (b.Alive) enemyAlive++;
        int allyAlive = 0;
        foreach (var b in controller.AllyBullets)
          if (b.Alive) allyAlive++;
        if (i > 80 && enemyAlive >= 1 && allyAlive >= 1
            && controller.WaveDirector.State == Proto_GreyboxWaveDirectorState.Fighting
            && controller.WaveDirector.Grunts.Count >= 1)
          break;
      }

      int ec = 0;
      int ac = 0;
      foreach (var b in controller.EnemyBullets)
        if (b.Alive) ec++;
      foreach (var b in controller.AllyBullets)
        if (b.Alive) ac++;
      Debug.Log(
        $"Task6 combat visual2 grunts={controller.WaveDirector.Grunts.Count} enemyBullets={ec} allyBullets={ac} wave={controller.WaveDirector.CurrentWave}");
      CaptureToFile("v4_task6_combat.png", cam);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask7Sequence()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      var aim = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(54f),
        Proto_GreyboxLayout.ScreenYPercentToWorldY(36f));
      controller.SetAimOverride(aim);

      const float dt = 0.02f;
      for (int i = 0; i < 520; i++)
      {
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = i % 5 != 0 }, dt);
        int enemyAlive = 0;
        foreach (var b in controller.EnemyBullets)
          if (b.Alive) enemyAlive++;
        int allyAlive = 0;
        foreach (var b in controller.AllyBullets)
          if (b.Alive) allyAlive++;
        if (i > 80 && enemyAlive >= 1 && allyAlive >= 1
            && controller.WaveDirector.State == Proto_GreyboxWaveDirectorState.Fighting
            && controller.WaveDirector.Grunts.Count >= 1)
          break;
      }

      int ec = 0;
      int ac = 0;
      foreach (var b in controller.EnemyBullets)
        if (b.Alive) ec++;
      foreach (var b in controller.AllyBullets)
        if (b.Alive) ac++;
      Debug.Log(
        $"Task7 combat visual3 grunts={controller.WaveDirector.Grunts.Count} enemyBullets={ec} allyBullets={ac} wave={controller.WaveDirector.CurrentWave}");
      CaptureToFile("v4_task7_combat.png", cam);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask8Sequence()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      const float dt = 0.02f;
      for (int i = 0; i < 420; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      controller.PoseAimStickDrag(new Vector2(0.35f, 5.5f));
      controller.Simulate(new Proto_GreyboxInput(), dt);
      CaptureToFile("v4_task8_aim.png", cam);

      controller.FullRestart();
      player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing after restart.");
        EditorApplication.Exit(1);
        return;
      }

      for (int i = 0; i < 30; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = true }, dt);
      for (int i = 0; i < 6; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      CaptureToFile("v4_task8_reload.png", cam);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask13Skin()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;
      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller != null)
      {
        controller.SetWestPixelSkin(true);
        const float dt = 0.02f;
        for (int i = 0; i < 30; i++)
          controller.Simulate(new Proto_GreyboxInput(), dt);
      }

      CaptureToFile("v4_greybox/task13_skin.png", cam);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask14Combat()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;
      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      const float dt = 0.02f;
      int totalTicks = Mathf.RoundToInt(5.6f / dt);
      for (int i = 0; i < totalTicks; i++)
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = i % 12 == 0 }, dt);

      Debug.Log(
        $"Task14 combat grunts={controller.WaveDirector.Grunts.Count} sim={controller.SimTime:0.00}s wave={controller.WaveDirector.CurrentWave}");
      CaptureToFile("v4_greybox/task14_combat.png", cam);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask15Combat()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;
      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      const float dt = 0.02f;
      int totalTicks = Mathf.RoundToInt(5.4f / dt);
      for (int i = 0; i < totalTicks; i++)
      {
        var input = new Proto_GreyboxInput
        {
          Move = i < totalTicks * 2 / 3 ? Vector2.up : Vector2.zero,
          FireQuickAuto = i % 14 == 0
        };
        controller.Simulate(input, dt);
      }
      // A couple of recent shots so yellow tracers are still in the air.
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = i == 0 }, dt);
      for (int i = 0; i < 10; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      Debug.Log(
        $"Task15 combat grunts={controller.WaveDirector.Grunts.Count} sim={controller.SimTime:0.00}s footY={controller.Motor.FootPosition.y:F2}");
      CaptureToFile("v4_greybox/task15_combat.png", cam);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask16Depth()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      const float dt = 0.02f;
      // Let wave 1 spawn on 2-3 depth rows; keep the player near default feet (~88%)
      // so the body does not cover the street, and do not wipe the row before the shot.
      int spawnTicks = Mathf.RoundToInt(4.2f / dt);
      for (int i = 0; i < spawnTicks; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      // A short burst so yellow tracers and red enemy shots are mid-flight.
      for (int i = 0; i < 10; i++)
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = i % 3 == 0 }, dt);
      for (int i = 0; i < 6; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      Debug.Log(
        $"Task16 depth grunts={controller.WaveDirector.Grunts.Count} sim={controller.SimTime:0.00}s footY={controller.Motor.FootPosition.y:F2} perspective=ON");
      CaptureToFile("v4_greybox/task16_depth.png", cam, 1920, 1080);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask17Depth()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      const float dt = 0.02f;
      int spawnTicks = Mathf.RoundToInt(4.2f / dt);
      for (int i = 0; i < spawnTicks; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      for (int i = 0; i < 10; i++)
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = i % 3 == 0 }, dt);
      for (int i = 0; i < 6; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      Debug.Log(
        $"Task17 depth grunts={controller.WaveDirector.Grunts.Count} sim={controller.SimTime:0.00}s footY={controller.Motor.FootPosition.y:F2} perspective=ON");
      CaptureToFile("v4_greybox/task17_depth.png", cam, 1920, 1080);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask18BulletFlight()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      const float dt = 0.02f;
      int spawnTicks = Mathf.RoundToInt(3.8f / dt);
      for (int i = 0; i < spawnTicks; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      var aim = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(54f),
        Proto_GreyboxLayout.ScreenYPercentToWorldY(42f));
      controller.SetAimOverride(aim);
      for (int i = 0; i < 6; i++)
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = i == 0 || i == 3 }, dt);

      for (int f = 1; f <= 4; f++)
      {
        for (int i = 0; i < 8; i++)
          controller.Simulate(new Proto_GreyboxInput(), dt);
        int allyAlive = 0;
        var sb = new System.Text.StringBuilder();
        sb.Append($"Task18 f{f} ally alive=");
        foreach (var b in controller.AllyBullets)
        {
          if (!b.Alive) continue;
          allyAlive++;
          sb.Append($" ({b.GroundPosition.x:F2},{b.GroundPosition.y:F2})");
        }

        sb.Append($" count={allyAlive}");
        Debug.Log(sb.ToString());
        CaptureToFile($"v4_greybox/task18_bullet_f{f}.png", cam, 1920, 1080);
      }

      EditorApplication.Exit(0);
    }

    public static void CaptureTask19Fire()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      controller.SetPerspectiveDepth(true);
      controller.RefreshTargets();
      const float dt = 0.02f;
      for (int i = 0; i < 15; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      var aim = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(54f),
        Proto_GreyboxLayout.ScreenYPercentToWorldY(42f));
      controller.SetAimOverride(aim);
      int gap = Mathf.CeilToInt(Proto_GreyboxTuning.FireInterval / dt) + 1;
      controller.Simulate(
        new Proto_GreyboxInput { FireQuickAuto = true, FireStickHeld = true, FireStickDragWorld = new Vector2(0.4f, 2.5f) }, dt);
      for (int i = 0; i < gap; i++)
        controller.Simulate(new Proto_GreyboxInput { FireStickHeld = true, FireStickDragWorld = new Vector2(0.4f, 2.5f) }, dt);
      controller.Simulate(
        new Proto_GreyboxInput { FireQuickAuto = true, FireStickHeld = true, FireStickDragWorld = new Vector2(0.4f, 2.5f) }, dt);
      for (int i = 0; i < 20; i++)
        controller.Simulate(new Proto_GreyboxInput { FireStickHeld = true, FireStickDragWorld = new Vector2(0.4f, 2.5f) }, dt);

      int allyAlive = 0;
      foreach (var b in controller.AllyBullets)
        if (b.Alive) allyAlive++;
      Debug.Log(
        $"Task19 fire ammo={controller.Gun.Ammo}/{controller.Gun.MagazineSize} allyFlying={allyAlive} sim={controller.SimTime:0.00}");
      CaptureToFile("v4_greybox/task19_fire.png", cam, 1920, 1080);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask19Bomb()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      controller.SetPerspectiveDepth(true);
      controller.RefreshTargets();
      const float dt = 0.02f;
      for (int i = 0; i < 20; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      float stickR = Proto_GreyboxPointerMath.AimStickRadiusPx(1080f,
        Proto_GreyboxPointerMath.AimStickRadiusScreenHeightPercent);
      // Mid-range throw so dynamite stays mid-screen (full-stick lands too far / too small).
      var pull = new Vector2(stickR * 0.12f, stickR * 0.42f);
      controller.Simulate(new Proto_GreyboxInput { GrenadeStickHeld = true, GrenadeStickDragWorld = pull }, dt);
      controller.Simulate(new Proto_GreyboxInput { GrenadeStickReleased = true, GrenadeStickDragWorld = pull }, dt);

      int mid = Mathf.CeilToInt(Proto_GreyboxTuning.GrenadeFlightTime / dt * 0.45f);
      for (int i = 0; i < mid; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      Debug.Log(
        $"Task19 bomb flying={controller.Grenade.IsFlying} ammo={controller.Gun.Ammo}/{controller.Gun.MagazineSize} sim={controller.SimTime:0.00}");
      CaptureToFile("v4_greybox/task19_bomb.png", cam, 1920, 1080);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask19Roll()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      controller.SetPerspectiveDepth(true);
      controller.RefreshTargets();
      const float dt = 0.02f;
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right, RollPressed = true }, dt);
      for (int i = 0; i < 6; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right }, dt);

      Debug.Log(
        $"Task19 roll mid rolling={controller.Roll.IsRolling} elapsed={controller.Roll.Elapsed:0.00} dizzy={controller.RollDizzy.Phase} chain={controller.RollDizzy.ChainCount}");
      CaptureToFile("v4_greybox/task19_roll.png", cam, 1920, 1080);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask19Dizzy()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      controller.SetPerspectiveDepth(true);
      controller.RefreshTargets();
      const float dt = 0.02f;
      for (int i = 0; i < 5; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      for (int chain = 0; chain < 5; chain++)
      {
        controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right, RollPressed = true }, dt);
        for (int k = 0; k < Mathf.CeilToInt(Proto_GreyboxTuning.RollDuration / dt) + 2; k++)
          controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right }, dt);
      }

      for (int i = 0; i < 18; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      Debug.Log(
        $"Task19 dizzy phase={controller.RollDizzy.Phase} remaining={controller.RollDizzy.RemainingTime(controller.SimTime):0.00} chain={controller.RollDizzy.ChainCount}");
      CaptureToFile("v4_greybox/task19_dizzy.png", cam, 1920, 1080);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask20Hud()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      controller.SetPerspectiveDepth(true);
      controller.RefreshTargets();
      controller.RuntimeTuning.EnemyFireInterval = 0.75f;
      const float dt = 0.02f;

      for (int i = 0; i < 220; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      controller.Simulate(new Proto_GreyboxInput { GrenadePressed = true }, dt);
      float rechargeWait = Proto_GreyboxTuning.GrenadeFlightTime + Proto_GreyboxTuning.GrenadeCooldown * 0.45f;
      int steps = Mathf.CeilToInt(rechargeWait / dt);
      for (int i = 0; i < steps; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      int enemyFlying = 0;
      foreach (var b in controller.EnemyBullets)
        if (b.Alive) enemyFlying++;
      float gauge = controller.Grenade.RechargeProgress01(controller.SimTime);
      Debug.Log(
        $"Task20 hud gauge={gauge:0.00} enemyFlying={enemyFlying} ammo={controller.Gun.Ammo}/{controller.Gun.MagazineSize} sim={controller.SimTime:0.00}");
      CaptureToFile("v4_greybox/task20_hud.png", cam, 1920, 1080);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask21Hud()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      controller.SetPerspectiveDepth(true);
      controller.RefreshTargets();
      controller.RuntimeTuning.RollCooldown = 0f;
      // PlayerMaxHp is now 3, so warmup fire would down the player before the
      // scripted heart/roll pose. Stretch enemy fire so the capture stays alive.
      controller.RuntimeTuning.EnemyFireInterval = 9999f;
      const float dt = 0.02f;
      for (int i = 0; i < 80; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      controller.ResetHp();
      controller.ApplyTestPlayerHit();
      controller.Simulate(new Proto_GreyboxInput(), Proto_GreyboxTuning.PlayerHitIFrameSec + 0.05f);
      controller.ApplyTestPlayerHit();
      float regenWait = Proto_GreyboxTuning.HeartRegenSec * 0.55f;
      int regenSteps = Mathf.CeilToInt(regenWait / dt);
      for (int i = 0; i < regenSteps; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      for (int chain = 0; chain < 3; chain++)
      {
        controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right, RollPressed = true }, dt);
        for (int k = 0; k < Mathf.CeilToInt(Proto_GreyboxTuning.RollDuration / dt) + 1; k++)
          controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right }, dt);
      }

      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      float rollLock = controller.RollDizzy.ChainLockRemaining(controller.SimTime);
      Debug.Log(
        $"Task21 hud hearts={controller.Hearts.Hearts} regen01={controller.Hearts.Regen01:0.00} rollLock={rollLock:0.00}");
      CaptureToFile("v4_greybox/task21_hud.png", cam, 1920, 1080);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask22Hud()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      controller.SetPerspectiveDepth(true);
      controller.RefreshTargets();
      controller.RuntimeTuning.RollCooldown = 0f;
      controller.RuntimeTuning.EnemyFireInterval = 9999f;
      const float dt = 0.02f;
      for (int i = 0; i < 80; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      controller.ResetHp();
      controller.ApplyTestPlayerHit();
      controller.Simulate(new Proto_GreyboxInput(), Proto_GreyboxTuning.PlayerHitIFrameSec + 0.05f);
      controller.ApplyTestPlayerHit();
      float regenWait = Proto_GreyboxTuning.HeartRegenSec * 0.55f;
      int regenSteps = Mathf.CeilToInt(regenWait / dt);
      for (int i = 0; i < regenSteps; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      for (int chain = 0; chain < 3; chain++)
      {
        controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right, RollPressed = true }, dt);
        for (int k = 0; k < Mathf.CeilToInt(Proto_GreyboxTuning.RollDuration / dt) + 1; k++)
          controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right }, dt);
      }

      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      float rollLock = controller.RollDizzy.ChainLockRemaining(controller.SimTime);
      Debug.Log(
        $"Task22 hud heartBand={Proto_GreyboxHudBuilder.HeartsRootY0:0.0}-{Proto_GreyboxHudBuilder.HeartsRootY1:0.0}% hearts={controller.Hearts.Hearts} regen01={controller.Hearts.Regen01:0.00} rollLock={rollLock:0.00}");
      CaptureToFile("v4_greybox/task22_hud.png", cam, 1920, 1080);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask23Depth()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      controller.SetPerspectiveDepth(true);
      controller.SetPlayerPerspectiveRuntime();
      controller.RuntimeTuning.EnemyFireInterval = 9999f;
      controller.RuntimeTuning.Wave1Count = 0;
      controller.RuntimeTuning.Wave2Count = 0;
      controller.RuntimeTuning.Wave3Count = 0;
      const float dt = 0.02f;
      for (int i = 0; i < 30; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      const int outW = 1920;
      const int halfW = outW / 2;
      const int outH = 1080;
      float footTopPct = Proto_GreyboxPerspectiveDepth.PerspectiveMoveFootMinPercent;
      float footBottomPct = Proto_GreyboxPerspectiveDepth.PerspectiveMoveFootMaxPercent;
      float hTop = Proto_GreyboxPerspectiveDepth.PlayerHeightScreenPercentAtFootPercent(footTopPct);
      float hBottom = Proto_GreyboxPerspectiveDepth.PlayerHeightScreenPercentAtFootPercent(footBottomPct);

      Texture2D FrameAtFoot(float footPct, float xPct)
      {
        float y = Proto_GreyboxLayout.ScreenYPercentToWorldY(footPct);
        float x = Proto_GreyboxLayout.ScreenXPercentToWorldX(xPct);
        controller.Motor.SetFoot(new Vector2(x, y));
        for (int i = 0; i < 8; i++)
          controller.Simulate(new Proto_GreyboxInput(), dt);
        return ReadCameraRender(cam, halfW, outH);
      }

      var texTop = FrameAtFoot(footTopPct, 38f);
      var texBottom = FrameAtFoot(footBottomPct, 62f);
      var combined = new Texture2D(outW, outH, TextureFormat.RGB24, false);
      combined.SetPixels(0, 0, halfW, outH, texTop.GetPixels());
      combined.SetPixels(halfW, 0, halfW, outH, texBottom.GetPixels());
      combined.Apply();
      Object.DestroyImmediate(texTop);
      Object.DestroyImmediate(texBottom);

      var dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds");
      var path = Path.Combine(dir, "v4_greybox/task23_depth.png");
      var outDir = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(outDir))
        Directory.CreateDirectory(outDir);
      File.WriteAllBytes(path, combined.EncodeToPNG());
      Object.DestroyImmediate(combined);
      Debug.Log(
        $"Task23 depth topFoot%={footTopPct:0.0} h%={hTop:0.00} bottomFoot%={footBottomPct:0.0} h%={hBottom:0.00} -> {path}");
      EditorApplication.Exit(0);
    }

  static Texture2D ReadCameraRender(Camera cam, int width, int height)
  {
    Canvas.ForceUpdateCanvases();
    var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
    var prev = cam.targetTexture;
    var prevActive = RenderTexture.active;
    cam.targetTexture = rt;
    cam.Render();
    RenderTexture.active = rt;
    var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    tex.Apply();
    cam.targetTexture = prev;
    RenderTexture.active = prevActive;
    rt.Release();
    Object.DestroyImmediate(rt);
    return tex;
  }

    public static void CaptureTask22Settings()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPerspectiveDepth.Enabled = true;
      Proto_GreyboxMoveZone.ResetToDefaults();
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
      Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
      Proto_GreyboxWestPixelSkinState.Enabled = true;

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      controller.SetWestPixelSkin(true);
      controller.SetPerspectiveDepth(true);
      controller.RefreshTargets();
      controller.RuntimeTuning.EnemyFireInterval = 9999f;
      const float dt = 0.02f;
      for (int i = 0; i < 40; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      controller.ShowSettingsForCapture();
      var root = GameObject.Find(Proto_GreyboxSettingsUi.RootName);
      bool visible = root != null && root.activeSelf;
      Debug.Log(
        $"Task22 settings panelVisible={visible} ally={controller.RuntimeTuning.AllyBulletSpeed:0.0} enemy={controller.RuntimeTuning.EnemyBulletSpeed:0.0}");
      CaptureToFile("v4_greybox/task22_settings.png", cam, 1920, 1080);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask12Hud()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      CaptureToFile("v4_greybox/task12_hud.png", cam);
      EditorApplication.Exit(0);
    }

    public static void CaptureTask9Sequence()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      if (builder != null)
        builder.Build();

      var cam = Camera.main;
      if (cam == null)
      {
        Debug.LogError("Greybox screenshot: no main camera.");
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (controller == null)
      {
        Debug.LogError("Greybox screenshot: player controller missing.");
        EditorApplication.Exit(1);
        return;
      }

      const float dt = 0.02f;
      const float closeBulletMaxDist = 2.8f;
      for (int i = 0; i < 640; i++)
      {
        controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = i % 6 != 0 }, dt);
        var foot = controller.Motor.FootPosition;
        bool closeEnemyBullet = false;
        foreach (var b in controller.EnemyBullets)
        {
          if (!b.Alive) continue;
          if ((b.GroundPosition - foot).sqrMagnitude <= closeBulletMaxDist * closeBulletMaxDist)
          {
            closeEnemyBullet = true;
            break;
          }
        }

        if (i > 120 && closeEnemyBullet
            && controller.WaveDirector.State == Proto_GreyboxWaveDirectorState.Fighting
            && controller.WaveDirector.Grunts.Count >= 1
            && controller.WaveDirector.CurrentWave >= 1)
          break;
      }

      controller.PoseAimStickDrag(new Vector2(0.35f, 5.5f));
      controller.Simulate(new Proto_GreyboxInput(), dt);

      int ec = 0;
      foreach (var b in controller.EnemyBullets)
        if (b.Alive) ec++;
      Debug.Log(
        $"Task9 aim capture wave={controller.WaveDirector.CurrentWave} enemyBullets={ec} player=({controller.Motor.FootPosition.x:F2},{controller.Motor.FootPosition.y:F2})");
      CaptureToFile("v4_task9_aim.png", cam);
      EditorApplication.Exit(0);
    }

    static void CaptureToFile(string fileName, Camera cam, int width = 0, int height = 0)
    {
      Canvas.ForceUpdateCanvases();
      SaveRender(cam, fileName, width, height);
    }

    static void SaveRender(Camera cam, string fileName, int width = 0, int height = 0)
    {
      int w = width > 0 ? width : Width;
      int h = height > 0 ? height : Height;
      var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
      var prev = cam.targetTexture;
      var prevActive = RenderTexture.active;
      cam.targetTexture = rt;
      cam.Render();
      RenderTexture.active = rt;

      var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
      tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
      tex.Apply();

      var png = tex.EncodeToPNG();

      cam.targetTexture = prev;
      RenderTexture.active = prevActive;
      rt.Release();
      Object.DestroyImmediate(rt);
      Object.DestroyImmediate(tex);

      var dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds");
      var path = Path.Combine(dir, fileName);
      var outDir = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(outDir))
        Directory.CreateDirectory(outDir);
      File.WriteAllBytes(path, png);
      Debug.Log($"Greybox screenshot saved: {path}");
    }

    /// <summary>Slice K: perspective 2D captures to _tmp_slice_k/captures_r3c/.</summary>
    public static void Capture2DPerspKAll()
    {
      const string outRoot = "_tmp_slice_k/captures_r3c";
      Proto_GreyboxPersp2DHud.Restore();
      Proto_GreyboxCapturePersp2D.Active = true;
      Proto_GreyboxCapturePersp2D.IgnorePlayerDamage = true;
      Proto_GreyboxCapturePersp2D.HideResultOverlay = true;
      Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;

      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      builder?.Build();

      if (!PerspKTryResolvePlayer(out var cam, out var controller))
      {
        Proto_GreyboxCapturePersp2D.Active = false;
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      const float dt = 0.02f;

      void RestartPersp(ref Proto_GreyboxPlayerController ctrl)
      {
        ctrl.FullRestart();
        if (!PerspKTryResolvePlayer(out _, out ctrl))
        {
          Debug.LogError("Persp K capture: player missing after FullRestart.");
          Proto_GreyboxCapturePersp2D.Active = false;
          EditorApplication.Exit(1);
        }
        ctrl.RuntimeTuning.WestPixelSkin = false;
        ctrl.SetWestPixelSkin(false);
        ctrl.SetViewMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
        ctrl.RuntimeTuning.EnemyFireInterval = 9999f;
        var hud = GameObject.Find("Greybox_Hud");
        Proto_GreyboxPersp2DHud.Restore();
        Proto_GreyboxPersp2DHud.Apply(hud != null ? hud.transform : null);
        Canvas.ForceUpdateCanvases();
      }

      Proto_GreyboxCapturePersp2D.DecorStreetBandits = true;
      RestartPersp(ref controller);
      for (int i = 0; i < 60; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      controller.PreparePerspCaptureDecor();
      var perspMain = controller.PerspSkin;
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_main.png", 1920, 1080);
      SaveRenderToRoot(cam, $"{outRoot}/k_sky_top.png", 1920, 1080);
      for (int i = 0; i < 4; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      var hudPause = GameObject.Find("Greybox_Hud");
      Proto_GreyboxPersp2DHud.Apply(hudPause != null ? hudPause.transform : null);
      Canvas.ForceUpdateCanvases();
      LogPauseHierarchy("pre_k_pause");
      Proto_GreyboxPersp2DHud.SetPausePressedForCapture(true);
      var pausePath = Path.Combine(Directory.GetCurrentDirectory(), outRoot, "k_pause.png");
      SaveRenderToRoot(cam, pausePath, 1920, 1080);
      WritePausePixelCheck(pausePath, Path.Combine(Directory.GetCurrentDirectory(), outRoot, "k_pause_check.txt"));
      Proto_GreyboxPersp2DHud.SetPausePressedForCapture(false);
      SaveRenderToRoot(cam, $"{outRoot}/k_crosshair.png", 1920, 1080);

      Proto_GreyboxCapturePersp2D.DecorStreetBandits = true;
      RestartPersp(ref controller);
      controller.PreparePerspCaptureDecor();
      perspMain = controller.PerspSkin;

      controller.RuntimeTuning.EnemyFireInterval = 9999f;
      float banditX = Proto_GreyboxLayout.ScreenXPercentToWorldX(48f);
      float banditY = Proto_GreyboxLayout.ScreenYPercentToWorldY(56f);
      var playerFoot = controller.Motor.FootPosition;
      var aimStick = new Vector2(banditX - playerFoot.x, banditY - playerFoot.y);
      if (aimStick.sqrMagnitude > 0.01f)
        aimStick.Normalize();
      for (int i = 0; i < 100; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = aimStick * 0.65f }, dt);

      perspMain?.PumpCaptureCombatMoment();
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = aimStick * 0.65f }, dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_combat.png", 1920, 1080);
      var heightLog = new System.Text.StringBuilder();
      perspMain?.WriteDecorBanditHeightReport(cam, heightLog);
      var heightPath = Path.Combine(Directory.GetCurrentDirectory(), outRoot, "k_bandit_heights.txt");
      Directory.CreateDirectory(Path.GetDirectoryName(heightPath)!);
      File.WriteAllText(heightPath, heightLog.ToString());

      Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;
      RestartPersp(ref controller);
      controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right, RollPressed = true }, dt);
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right, RollPressed = true }, dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_roll.png", 1920, 1080);

      Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;
      RestartPersp(ref controller);
      for (int i = 0; i < 12; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      controller.PreparePerspCaptureDecor();
      var persp = controller.PerspSkin;
      persp?.DebugPlaceActorAtScreen2x("bandit", 750f, 1380f, behindPorch: false, buildingId: "saloon_L");
      for (int i = 0; i < 3; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_porch_front.png", 1920, 1080);
      persp?.DebugPlaceActorAtScreen2x("bandit", 580f, 1265f, behindPorch: true, buildingId: "saloon_L");
      for (int i = 0; i < 3; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_porch_behind.png", 1920, 1080);
      var porchLog = new System.Text.StringBuilder();
      persp?.WritePorchBehindCheck(cam, porchLog);
      File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), outRoot, "k_porch_check.txt"),
        porchLog.ToString());
      persp?.ClearPorchDebugActor();
      persp?.SetDepthBandDebug(true);
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_bands.png", 1920, 1080);
      persp?.SetDepthBandDebug(false);

      RestartPersp(ref controller);
      var joyMove = new Vector2(-0.55f, 0.25f);
      for (int i = 0; i < 6; i++)
        controller.Simulate(new Proto_GreyboxInput
        {
          Move = joyMove,
          FireQuickAuto = true,
          FireStickHeld = true,
        }, dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_buttons_pressed.png", 1920, 1080);

      RestartPersp(ref controller);
      for (int i = 0; i < 4; i++)
        controller.Simulate(new Proto_GreyboxInput { RollPressed = true, Move = Vector2.right }, dt);
      for (int i = 0; i < 6; i++)
        controller.Simulate(new Proto_GreyboxInput { GrenadePressed = true }, dt);
      for (int i = 0; i < 10; i++)
        controller.Simulate(new Proto_GreyboxInput { ReloadPressed = true }, dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_buttons_cooldown.png", 1920, 1080);

      RestartPersp(ref controller);
      controller.SetViewMode(Proto_GreyboxViewMode.View2DChibi, persist: false);
      for (int i = 0; i < 35; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_chibi_after_switch.png", 1920, 1080);

      RestartPersp(ref controller);
      controller.ToggleViewMode();
      controller.ToggleViewMode();
      controller.ToggleViewMode();
      for (int i = 0; i < 40; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_chibi_after_cycle.png", 1920, 1080);

      foreach (var (mode, file) in new[]
               {
                 (Proto_GreyboxViewMode.View2D, "k_modes_pixel.png"),
                 (Proto_GreyboxViewMode.View3D, "k_modes_3d.png"),
               })
      {
        Ensure3DFactoryForK();
        if (!PerspKTryResolvePlayer(out cam, out controller))
        {
          Proto_GreyboxCapturePersp2D.Active = false;
          EditorApplication.Exit(1);
          return;
        }
        Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;
        if (mode is Proto_GreyboxViewMode.View2D or Proto_GreyboxViewMode.View2DChibi)
        {
          controller.RuntimeTuning.WestPixelSkin = true;
          Proto_GreyboxWestPixelSkinState.Enabled = true;
        }

        controller.SetViewMode(mode, persist: false);
        cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
        Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
        for (int i = 0; i < 40; i++)
          controller.Simulate(new Proto_GreyboxInput(), dt);
        SaveRenderToRoot(cam, $"{outRoot}/{file}", 1920, 1080);
      }

      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      builder?.Build();
      Ensure3DFactoryForK();
      if (PerspKTryResolvePlayer(out cam, out controller))
      {
        Proto_GreyboxCapturePersp2D.Active = false;
        Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;
        Proto_GreyboxPersp2DHud.Restore();
        controller.RuntimeTuning.WestPixelSkin = true;
        Proto_GreyboxWestPixelSkinState.Enabled = true;
        controller.SetViewMode(Proto_GreyboxViewMode.View2DChibi, persist: false);
        cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
        Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
        for (int i = 0; i < 45; i++)
          controller.Simulate(new Proto_GreyboxInput(), dt);
        SaveRenderToRoot(cam, $"{outRoot}/k_modes_chibi_fresh.png", 1920, 1080);
      }

      Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;
      Proto_GreyboxCapturePersp2D.Active = false;
      EditorApplication.Exit(0);
    }

    /// <summary>Slice K round 4: combat FX subset to _tmp_slice_k/captures_r4/.</summary>
    public static void Capture2DPerspKR4()
    {
      const string outRoot = "_tmp_slice_k/captures_r4";
      Proto_GreyboxPersp2DHud.Restore();
      Proto_GreyboxCapturePersp2D.Active = true;
      Proto_GreyboxCapturePersp2D.IgnorePlayerDamage = true;
      Proto_GreyboxCapturePersp2D.HideResultOverlay = true;
      Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;

      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      builder?.Build();

      if (!PerspKTryResolvePlayer(out var cam, out var controller))
      {
        Proto_GreyboxCapturePersp2D.Active = false;
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      const float dt = 0.02f;

      void RestartPersp(ref Proto_GreyboxPlayerController ctrl)
      {
        ctrl.FullRestart();
        if (!PerspKTryResolvePlayer(out _, out ctrl))
        {
          Debug.LogError("Persp K r4 capture: player missing after FullRestart.");
          Proto_GreyboxCapturePersp2D.Active = false;
          EditorApplication.Exit(1);
        }
        ctrl.RuntimeTuning.WestPixelSkin = false;
        ctrl.SetWestPixelSkin(false);
        ctrl.SetViewMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
        ctrl.RuntimeTuning.EnemyFireInterval = 9999f;
        var hud = GameObject.Find("Greybox_Hud");
        Proto_GreyboxPersp2DHud.Restore();
        Proto_GreyboxPersp2DHud.Apply(hud != null ? hud.transform : null);
        Canvas.ForceUpdateCanvases();
      }

      Proto_GreyboxCapturePersp2D.DecorStreetBandits = true;
      RestartPersp(ref controller);
      for (int i = 0; i < 60; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      controller.PreparePerspCaptureDecor();
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_main.png", 1920, 1080);

      var hudGo = GameObject.Find("Greybox_Hud");
      Proto_GreyboxPersp2DHud.Apply(hudGo != null ? hudGo.transform : null);
      Canvas.ForceUpdateCanvases();
      Proto_GreyboxPersp2DHud.SetPausePressedForCapture(true);
      SaveRenderToRoot(cam, $"{outRoot}/k_pause.png", 1920, 1080);
      Proto_GreyboxPersp2DHud.SetPausePressedForCapture(false);
      SaveRenderToRoot(cam, $"{outRoot}/k_crosshair.png", 1920, 1080);

      RestartPersp(ref controller);
      controller.PreparePerspCaptureDecor();
      var perspMain = controller.PerspSkin;
      controller.RuntimeTuning.EnemyFireInterval = 9999f;
      float banditX = Proto_GreyboxLayout.ScreenXPercentToWorldX(48f);
      float banditY = Proto_GreyboxLayout.ScreenYPercentToWorldY(56f);
      var playerFoot = controller.Motor.FootPosition;
      var aimStick = new Vector2(banditX - playerFoot.x, banditY - playerFoot.y);
      if (aimStick.sqrMagnitude > 0.01f)
        aimStick.Normalize();
      for (int i = 0; i < 100; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = aimStick * 0.65f }, dt);

      perspMain?.PumpCaptureCombatMoment();
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = aimStick * 0.65f }, dt);
      var fxLog = new System.Text.StringBuilder();
      perspMain?.WriteFxCaptureCheck(cam, fxLog);
      var fxPath = Path.Combine(Directory.GetCurrentDirectory(), outRoot, "k_fx_check.txt");
      Directory.CreateDirectory(Path.GetDirectoryName(fxPath)!);
      File.WriteAllText(fxPath, fxLog.ToString());
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_combat.png", 1920, 1080);

      RestartPersp(ref controller);
      controller.PreparePerspCaptureDecor();
      var persp = controller.PerspSkin;
      persp?.DebugPlaceActorAtScreen2x("bandit", 750f, 1380f, behindPorch: false, buildingId: "saloon_L");
      for (int i = 0; i < 3; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_porch_front.png", 1920, 1080);
      persp?.DebugPlaceActorAtScreen2x("bandit", 580f, 1265f, behindPorch: true, buildingId: "saloon_L");
      for (int i = 0; i < 3; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_porch_behind.png", 1920, 1080);
      persp?.ClearPorchDebugActor();

      Proto_GreyboxCapturePersp2D.Active = false;
      EditorApplication.Exit(0);
    }

    /// <summary>Slice K round 4b captures to _tmp_slice_k/captures_r4b/.</summary>
    public static void Capture2DPerspKR4b()
    {
      const string outRoot = "_tmp_slice_k/captures_r4b";
      Proto_GreyboxPersp2DHud.Restore();
      Proto_GreyboxCapturePersp2D.Active = true;
      Proto_GreyboxCapturePersp2D.IgnorePlayerDamage = true;
      Proto_GreyboxCapturePersp2D.HideResultOverlay = true;
      Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;

      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      builder?.Build();

      if (!PerspKTryResolvePlayer(out var cam, out var controller))
      {
        Proto_GreyboxCapturePersp2D.Active = false;
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      const float dt = 0.02f;

      void RestartPersp(ref Proto_GreyboxPlayerController ctrl)
      {
        ctrl.FullRestart();
        if (!PerspKTryResolvePlayer(out _, out ctrl))
        {
          Debug.LogError("Persp K r4b capture: player missing after FullRestart.");
          Proto_GreyboxCapturePersp2D.Active = false;
          EditorApplication.Exit(1);
        }
        ctrl.RuntimeTuning.WestPixelSkin = false;
        ctrl.SetWestPixelSkin(false);
        ctrl.SetViewMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
        ctrl.RuntimeTuning.EnemyFireInterval = 9999f;
        var hud = GameObject.Find("Greybox_Hud");
        Proto_GreyboxPersp2DHud.Restore();
        Proto_GreyboxPersp2DHud.Apply(hud != null ? hud.transform : null);
        Canvas.ForceUpdateCanvases();
      }

      Proto_GreyboxCapturePersp2D.DecorStreetBandits = true;
      RestartPersp(ref controller);
      for (int i = 0; i < 60; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      controller.PreparePerspCaptureDecor();
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_main.png", 1920, 1080);

      var hudGo = GameObject.Find("Greybox_Hud");
      Proto_GreyboxPersp2DHud.Apply(hudGo != null ? hudGo.transform : null);
      Canvas.ForceUpdateCanvases();
      Proto_GreyboxPersp2DHud.SetPausePressedForCapture(true);
      SaveRenderToRoot(cam, $"{outRoot}/k_pause.png", 1920, 1080);
      Proto_GreyboxPersp2DHud.SetPausePressedForCapture(false);
      SaveRenderToRoot(cam, $"{outRoot}/k_crosshair.png", 1920, 1080);

      RestartPersp(ref controller);
      controller.PreparePerspCaptureDecor();
      var perspMain = controller.PerspSkin;
      controller.RuntimeTuning.EnemyFireInterval = 9999f;
      float banditX = Proto_GreyboxLayout.ScreenXPercentToWorldX(48f);
      float banditY = Proto_GreyboxLayout.ScreenYPercentToWorldY(56f);
      var playerFoot = controller.Motor.FootPosition;
      var aimStick = new Vector2(banditX - playerFoot.x, banditY - playerFoot.y);
      if (aimStick.sqrMagnitude > 0.01f)
        aimStick.Normalize();
      for (int i = 0; i < 100; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = aimStick * 0.65f }, dt);

      perspMain?.PumpCaptureCombatMoment();
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = aimStick * 0.65f }, dt);
      var fxLog = new System.Text.StringBuilder();
      perspMain?.WriteFxCaptureCheck(cam, fxLog);
      var fxPath = Path.Combine(Directory.GetCurrentDirectory(), outRoot, "k_fx_check.txt");
      Directory.CreateDirectory(Path.GetDirectoryName(fxPath)!);
      File.WriteAllText(fxPath, fxLog.ToString());
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_combat.png", 1920, 1080);

      RestartPersp(ref controller);
      controller.PreparePerspCaptureDecor();
      var persp = controller.PerspSkin;
      persp?.DebugPlaceActorAtScreen2x("bandit", 750f, 1380f, behindPorch: false, buildingId: "saloon_L");
      for (int i = 0; i < 3; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_porch_front.png", 1920, 1080);
      persp?.DebugPlaceActorAtScreen2x("bandit", 580f, 1265f, behindPorch: true, buildingId: "saloon_L");
      for (int i = 0; i < 3; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_porch_behind.png", 1920, 1080);
      persp?.ClearPorchDebugActor();

      Proto_GreyboxCapturePersp2D.Active = false;
      EditorApplication.Exit(0);
    }

    static bool PerspKTryResolvePlayer(out Camera cam, out Proto_GreyboxPlayerController controller)
    {
      cam = Camera.main;
      var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
      controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
      if (cam != null && controller != null) return true;
      Debug.LogError("Persp K capture: scene not ready (cam or player).");
      return false;
    }

    static void Ensure3DFactoryForK()
    {
      if (!Proto_GreyboxViewPresenterRegistry.HasFactory)
        Proto_GreyboxViewPresenterRegistry.RegisterFactory(() => new Proto_Greybox3DViewHost());
    }

    static void LogPauseHierarchy(string tag)
    {
      var hud = GameObject.Find("Greybox_Hud");
      if (hud == null)
      {
        Debug.Log($"PerspK pause-hierarchy [{tag}]: no hud");
        return;
      }

      var pause = hud.transform.Find(Proto_GreyboxHudBuilder.PauseButtonName);
      if (pause == null)
      {
        Debug.Log($"PerspK pause-hierarchy [{tag}]: no pause node");
        return;
      }

      void Walk(Transform t, int depth)
      {
        var g = t.GetComponent<Graphic>();
        if (g != null && g.enabled)
        {
          var cg = t.GetComponent<CanvasGroup>();
          float cgA = cg != null ? cg.alpha : 1f;
          var sp = g is Image im ? im.sprite : null;
          Debug.Log(
            $"PerspK pause-hierarchy [{tag}] depth={depth} name={t.name} type={g.GetType().Name} sprite={(sp != null ? sp.name : "null")} colorA={g.color.a:F3} canvasGroupA={cgA:F3} enabled={g.enabled}");
        }

        for (int i = 0; i < t.childCount; i++)
          Walk(t.GetChild(i), depth + 1);
      }

      Walk(pause, 0);
      var label = hud.transform.Find("Greybox_Hud_PauseLabel");
      if (label != null)
        Debug.Log(
          $"PerspK pause-hierarchy [{tag}] legacy_label active={label.gameObject.activeSelf}");
    }

    static void WritePausePixelCheck(string pngPath, string txtPath)
    {
      if (!File.Exists(pngPath))
      {
        File.WriteAllText(txtPath, "png_missing\n");
        return;
      }

      var bytes = File.ReadAllBytes(pngPath);
      var tex = new Texture2D(2, 2);
      tex.LoadImage(bytes);
      int w = tex.width;
      int h = tex.height;
      float cxPct = Proto_GreyboxPersp2DHudLayout.PauseX;
      float cyPct = Proto_GreyboxPersp2DHudLayout.PauseY;
      float rPct = Proto_GreyboxPersp2DHudLayout.PauseRadius;
      int cx = Mathf.RoundToInt(cxPct / 100f * w);
      int cy = Mathf.RoundToInt((1f - cyPct / 100f) * h);
      int r = Mathf.Max(4, Mathf.RoundToInt(rPct / 100f * h));
      double sumR = 0, sumG = 0, sumB = 0;
      int n = 0;
      for (int y = cy - r; y <= cy + r; y++)
      {
        for (int x = cx - r; x <= cx + r; x++)
        {
          if (x < 0 || y < 0 || x >= w || y >= h) continue;
          if ((x - cx) * (x - cx) + (y - cy) * (y - cy) > r * r) continue;
          var c = tex.GetPixel(x, y);
          sumR += c.r;
          sumG += c.g;
          sumB += c.b;
          n++;
        }
      }

      Object.DestroyImmediate(tex);
      float meanR = n > 0 ? (float)(sumR / n) * 255f : 255f;
      float meanG = n > 0 ? (float)(sumG / n) * 255f : 255f;
      float meanB = n > 0 ? (float)(sumB / n) * 255f : 255f;
      var sb = new System.Text.StringBuilder();
      sb.AppendLine($"pause_region_samples={n}");
      sb.AppendLine($"mean_rgb=({meanR:F1},{meanG:F1},{meanB:F1})");
      sb.AppendLine($"dark_button={meanR < 110f && meanG < 110f}");
      File.WriteAllText(txtPath, sb.ToString());
    }

    static void SaveRenderToRoot(Camera cam, string relPath, int w, int h)
    {
      Canvas.ForceUpdateCanvases();
      var full = relPath.Contains(Path.DirectorySeparatorChar) || relPath.Contains('/')
        ? relPath
        : Path.Combine(Directory.GetCurrentDirectory(), relPath);
      var dir = Path.GetDirectoryName(full);
      if (!string.IsNullOrEmpty(dir))
        Directory.CreateDirectory(dir);
      var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
      var prev = cam.targetTexture;
      var prevActive = RenderTexture.active;
      cam.targetTexture = rt;
      cam.Render();
      RenderTexture.active = rt;
      var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
      tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
      tex.Apply();
      File.WriteAllBytes(full, tex.EncodeToPNG());
      cam.targetTexture = prev;
      RenderTexture.active = prevActive;
      rt.Release();
      Object.DestroyImmediate(rt);
      Object.DestroyImmediate(tex);
      Debug.Log($"Persp K saved {full}");
    }

    /// <summary>Slice K round 5: full capture set to _tmp_slice_k/captures_r5/.</summary>
    public static void Capture2DPerspKR5()
    {
      const string outRoot = "_tmp_slice_k/captures_r5";
      var heightLog = new System.Text.StringBuilder();
      Proto_GreyboxPersp2DHud.Restore();
      Proto_GreyboxCapturePersp2D.Active = true;
      Proto_GreyboxCapturePersp2D.IgnorePlayerDamage = true;
      Proto_GreyboxCapturePersp2D.HideResultOverlay = true;
      Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;

      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      builder?.Build();

      if (!PerspKTryResolvePlayer(out var cam, out var controller))
      {
        Proto_GreyboxCapturePersp2D.Active = false;
        EditorApplication.Exit(1);
        return;
      }

      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
      const float dt = 0.02f;

      void RestartPersp(ref Proto_GreyboxPlayerController ctrl)
      {
        ctrl.FullRestart();
        if (!PerspKTryResolvePlayer(out _, out ctrl))
        {
          Debug.LogError("Persp K r5 capture: player missing after FullRestart.");
          Proto_GreyboxCapturePersp2D.Active = false;
          EditorApplication.Exit(1);
        }
        ctrl.RuntimeTuning.WestPixelSkin = false;
        ctrl.SetWestPixelSkin(false);
        ctrl.SetViewMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
        ctrl.RuntimeTuning.EnemyFireInterval = 9999f;
        var hud = GameObject.Find("Greybox_Hud");
        Proto_GreyboxPersp2DHud.Restore();
        Proto_GreyboxPersp2DHud.Apply(hud != null ? hud.transform : null);
        Canvas.ForceUpdateCanvases();
      }

      Proto_GreyboxCapturePersp2D.DecorStreetBandits = true;
      RestartPersp(ref controller);
      for (int i = 0; i < 60; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      controller.PreparePerspCaptureDecor();
      var perspMain = controller.PerspSkin;
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_main.png", 1920, 1080);
      heightLog.AppendLine("scene=k_persp_main");
      perspMain?.WriteDecorBanditHeightReport(cam, heightLog, includeBandits: false, heroLabel: "hero");
      SaveRenderToRoot(cam, $"{outRoot}/k_sky_top.png", 1920, 1080);

      var hudGo = GameObject.Find("Greybox_Hud");
      Proto_GreyboxPersp2DHud.Apply(hudGo != null ? hudGo.transform : null);
      Canvas.ForceUpdateCanvases();
      Proto_GreyboxPersp2DHud.SetPausePressedForCapture(true);
      SaveRenderToRoot(cam, $"{outRoot}/k_pause.png", 1920, 1080);
      Proto_GreyboxPersp2DHud.SetPausePressedForCapture(false);
      SaveRenderToRoot(cam, $"{outRoot}/k_crosshair.png", 1920, 1080);

      RestartPersp(ref controller);
      controller.PreparePerspCaptureDecor();
      perspMain = controller.PerspSkin;
      controller.RuntimeTuning.EnemyFireInterval = 9999f;
      float banditX = Proto_GreyboxLayout.ScreenXPercentToWorldX(48f);
      float banditY = Proto_GreyboxLayout.ScreenYPercentToWorldY(56f);
      controller.SetAimOverride(new Vector2(banditX, banditY));
      for (int i = 0; i < 100; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);

      perspMain?.PumpCaptureCombatMoment();
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      controller.ClearAimOverride();
      var fxLog = new System.Text.StringBuilder();
      perspMain?.WriteFxCaptureCheck(cam, fxLog);
      var fxPath = Path.Combine(Directory.GetCurrentDirectory(), outRoot, "k_fx_check.txt");
      Directory.CreateDirectory(Path.GetDirectoryName(fxPath)!);
      File.WriteAllText(fxPath, fxLog.ToString());
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_combat.png", 1920, 1080);
      heightLog.AppendLine();
      heightLog.AppendLine("scene=k_persp_combat");
      perspMain?.WriteDecorBanditHeightReport(cam, heightLog, heroLabel: "hero");
      File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), outRoot, "k_bandit_heights.txt"),
        heightLog.ToString());

      RestartPersp(ref controller);
      controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right, RollPressed = true }, dt);
      for (int i = 0; i < 8; i++)
        controller.Simulate(new Proto_GreyboxInput { Move = Vector2.right, RollPressed = true }, dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_persp_roll.png", 1920, 1080);

      RestartPersp(ref controller);
      controller.PreparePerspCaptureDecor();
      var persp = controller.PerspSkin;
      persp?.DebugPlaceActorAtScreen2x("bandit", 750f, 1380f, behindPorch: false, buildingId: "saloon_L");
      for (int i = 0; i < 3; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_porch_front.png", 1920, 1080);
      persp?.DebugPlaceActorAtScreen2x("bandit", 580f, 1265f, behindPorch: true, buildingId: "saloon_L");
      for (int i = 0; i < 3; i++)
        controller.Simulate(new Proto_GreyboxInput(), dt);
      SaveRenderToRoot(cam, $"{outRoot}/k_porch_behind.png", 1920, 1080);
      persp?.ClearPorchDebugActor();

      foreach (var (mode, file) in new[]
               {
                 (Proto_GreyboxViewMode.View2D, "k_modes_pixel.png"),
                 (Proto_GreyboxViewMode.View3D, "k_modes_3d.png"),
                 (Proto_GreyboxViewMode.View2DChibi, "k_modes_chibi.png"),
               })
      {
        Ensure3DFactoryForK();
        if (!PerspKTryResolvePlayer(out cam, out controller))
        {
          Proto_GreyboxCapturePersp2D.Active = false;
          EditorApplication.Exit(1);
          return;
        }
        Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;
        if (mode is Proto_GreyboxViewMode.View2D or Proto_GreyboxViewMode.View2DChibi)
        {
          controller.RuntimeTuning.WestPixelSkin = true;
          Proto_GreyboxWestPixelSkinState.Enabled = true;
        }
        controller.SetViewMode(mode, persist: false);
        cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
        Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
        for (int i = 0; i < 40; i++)
          controller.Simulate(new Proto_GreyboxInput(), dt);
        SaveRenderToRoot(cam, $"{outRoot}/{file}", 1920, 1080);
      }

      Proto_GreyboxCapturePersp2D.Active = false;
      EditorApplication.Exit(0);
    }
  }
}
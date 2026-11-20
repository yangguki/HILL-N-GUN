using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

    static void CaptureToFile(string fileName, Camera cam)
    {
      Canvas.ForceUpdateCanvases();
      SaveRender(cam, fileName);
    }

    static void SaveRender(Camera cam, string fileName)
    {
      var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
      var prev = cam.targetTexture;
      var prevActive = RenderTexture.active;
      cam.targetTexture = rt;
      cam.Render();
      RenderTexture.active = rt;

      var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
      tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
      tex.Apply();

      var png = tex.EncodeToPNG();

      cam.targetTexture = prev;
      RenderTexture.active = prevActive;
      rt.Release();
      Object.DestroyImmediate(rt);
      Object.DestroyImmediate(tex);

      var dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds");
      Directory.CreateDirectory(dir);
      var path = Path.Combine(dir, fileName);
      File.WriteAllBytes(path, png);
      Debug.Log($"Greybox screenshot saved: {path}");
    }
  }
}
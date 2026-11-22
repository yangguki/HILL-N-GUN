using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
  [TestFixture]
  public class Proto_GreyboxStageTests
  {
    const float Tol = 2f;

    Scene _previous;

    [SetUp]
    public void SetUp()
    {
      _previous = SceneManager.GetActiveScene();
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxMoveZone.ResetToDefaults();
    }

    [Test]
    public void SceneOpens_WithOrthographicCamera16x9()
    {
      var scene = EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      Assert.IsTrue(scene.IsValid() && scene.isLoaded);

      var cam = Camera.main;
      Assert.NotNull(cam);
      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Assert.IsTrue(cam.orthographic);
      Assert.AreEqual(Proto_GreyboxLayout.CameraOrthographicSize, cam.orthographicSize, 0.001f);
      Assert.AreEqual(Proto_GreyboxLayout.TargetAspect, cam.aspect, 0.02f);

      LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void Build_MatchesLayoutRatios()
    {
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      var cam = Camera.main;
      cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
      Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);

      var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
      Assert.NotNull(builder);
      builder.Build();

      AssertObjectRatios(cam, Proto_GreyboxStageBuilder.PlayerName, 76f, 88f, 12f, Tol);
      AssertObjectRatios(cam, Proto_GreyboxStageBuilder.FenceName,
        Proto_GreyboxPerspectiveDepth.FenceTopPercent,
        Proto_GreyboxPerspectiveDepth.FenceBottomPercent,
        Proto_GreyboxPerspectiveDepth.FenceBottomPercent - Proto_GreyboxPerspectiveDepth.FenceTopPercent, Tol);
      Assert.AreEqual(8f, Proto_GreyboxTuning.TargetHeightPercent, Tol, "grunt height percent");

      AssertObjectHeight(cam, Proto_GreyboxStageBuilder.CrateFarName, 3.6f, Tol);
      AssertObjectHeight(cam, Proto_GreyboxStageBuilder.CrateMidName, 6.6f, Tol);
      AssertObjectHeight(cam, Proto_GreyboxStageBuilder.CrateNearName, 12f, Tol);

      AssertCrateX(cam, Proto_GreyboxStageBuilder.CrateFarName, 52f, 56.5f, Tol);
      AssertCrateX(cam, Proto_GreyboxStageBuilder.CrateMidName, 12f, 21f, Tol);
      AssertCrateX(cam, Proto_GreyboxStageBuilder.CrateNearName, 58f, 69f, Tol);

      var sky = Proto_GreyboxScreenMetrics.FindRenderer(Proto_GreyboxStageBuilder.SkyName);
      var ground = Proto_GreyboxScreenMetrics.FindRenderer(Proto_GreyboxStageBuilder.GroundName);
      var fence = Proto_GreyboxScreenMetrics.FindRenderer(Proto_GreyboxStageBuilder.FenceName);
      var crateFar = Proto_GreyboxScreenMetrics.FindRenderer(Proto_GreyboxStageBuilder.CrateFarName);
      Assert.NotNull(sky, Proto_GreyboxStageBuilder.SkyName);
      Assert.NotNull(fence, Proto_GreyboxStageBuilder.FenceName);
      Assert.NotNull(ground, Proto_GreyboxStageBuilder.GroundName);
      Assert.NotNull(crateFar, Proto_GreyboxStageBuilder.CrateFarName);
      Assert.Greater(fence.sortingOrder, ground.sortingOrder, "fence should draw above ground");
      Assert.Greater(Proto_GreyboxLayout.SortingEnemyMarker, Proto_GreyboxLayout.SortingBuildingBase,
        "enemy markers should draw in front of buildings");
      Assert.Greater(crateFar.sortingOrder, Proto_GreyboxLayout.SortingBuildingBase,
        "far crate should draw in front of buildings");
      Assert.Greater(crateFar.sortingOrder, ground.sortingOrder, "far crate should draw in front of ground");

      Proto_GreyboxScreenMetrics.FromBoundsViaCamera(cam, sky.bounds, out var skyTop, out var skyBottom, out _, out var skyLeft,
        out var skyRight, out _);
      Assert.AreEqual(0f, skyTop, Tol, "sky top");
      Assert.AreEqual(16f, skyBottom, Tol, "sky bottom");
      Assert.LessOrEqual(skyLeft, 0f + Tol, "sky left edge should reach screen left");
      Assert.GreaterOrEqual(skyRight, 100f - Tol, "sky right edge should reach screen right");

      Proto_GreyboxScreenMetrics.FromBoundsViaCamera(cam, ground.bounds, out _, out _, out _, out var groundLeft,
        out var groundRight, out _);
      Assert.LessOrEqual(groundLeft, 0f + Tol, "ground left edge should reach screen left");
      Assert.GreaterOrEqual(groundRight, 100f - Tol, "ground right edge should reach screen right");

      Assert.AreEqual(1f, Proto_DepthScale.ScaleAt(6.3f), 0.001f);
      Assert.AreEqual(2.5f, Proto_DepthScale.ScaleAt(1.2f), 0.001f);
      float prev = Proto_DepthScale.ScaleAt(6.3f);
      for (float y = 6.2f; y >= 1.2f; y -= 0.3f)
      {
        float s = Proto_DepthScale.ScaleAt(y);
        Assert.GreaterOrEqual(s, prev - 0.0001f);
        prev = s;
      }

      LogAssert.NoUnexpectedReceived();
    }

    [TearDown]
    public void TearDown()
    {
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxMoveZone.ResetToDefaults();
      if (_previous.IsValid() && _previous.path != Proto_GreyboxStageBuilder.ScenePath)
        EditorSceneManager.OpenScene(_previous.path, OpenSceneMode.Single);
      else
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
    }

    static void AssertObjectRatios(Camera cam, string name, float head, float foot, float height, float tol)
    {
      var r = Proto_GreyboxScreenMetrics.FindRenderer(name);
      Assert.NotNull(r, name);
      Proto_GreyboxScreenMetrics.FromBoundsViaCamera(cam, r.bounds, out var top, out var bottom, out var h, out _, out _, out _);
      Assert.AreEqual(head, top, tol, name + " top");
      Assert.AreEqual(foot, bottom, tol, name + " foot");
      Assert.AreEqual(height, h, tol, name + " height");
    }

    static void AssertObjectHeight(Camera cam, string name, float height, float tol)
    {
      var r = Proto_GreyboxScreenMetrics.FindRenderer(name);
      Assert.NotNull(r, name);
      Proto_GreyboxScreenMetrics.FromBoundsViaCamera(cam, r.bounds, out _, out _, out var h, out _, out _, out _);
      Assert.AreEqual(height, h, tol, name);
    }

    static void AssertCrateX(Camera cam, string name, float x0, float x1, float tol)
    {
      var r = Proto_GreyboxScreenMetrics.FindRenderer(name);
      Assert.NotNull(r, name);
      Proto_GreyboxScreenMetrics.FromBoundsViaCamera(cam, r.bounds, out _, out _, out _, out var left, out var right, out _);
      Assert.AreEqual(x0, left, tol, name + " left");
      Assert.AreEqual(x1, right, tol, name + " right");
    }
  }
}
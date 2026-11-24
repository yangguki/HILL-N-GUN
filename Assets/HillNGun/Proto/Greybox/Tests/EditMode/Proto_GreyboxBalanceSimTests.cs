using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
  [TestFixture]
  public class Proto_GreyboxBalanceSimTests
  {
    [SetUp]
    public void SetUp()
    {
      Proto_GreyboxViewModeState.ResetForTests();
      Proto_GreyboxPerspectiveDepth.ResetToDefault();
      Proto_GreyboxPlayerSpawn.StartFootWorldYOverride = null;
      Proto_GreyboxBalanceSimContext.EndRun();
    }

    [TearDown]
    public void TearDown()
    {
      Proto_GreyboxPlayerSpawn.StartFootWorldYOverride = null;
      Proto_GreyboxBalanceSimContext.EndRun();
    }

    [Test]
    public void BalanceSimHarness_Seed1_Runs10sWithoutThrow()
    {
      Proto_GreyboxCapturePersp2D.Active = false;
      EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
      Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>()?.Build();
      var controller = Object.FindFirstObjectByType<Proto_GreyboxPlayerController>();
      Assert.NotNull(controller);

      float foot = Proto_GreyboxPlayerSpawn.PlayerStartFootWorldY(Proto_GreyboxViewMode.View2DPerspective);
      var stats = Proto_GreyboxBalanceSimRunner.RunStage1(controller, 1, foot,
          Proto_GreyboxBalanceSimRunner.Policy.StandShoot, maxSimSec: 10f);

      Assert.NotNull(stats);
      Assert.GreaterOrEqual(stats.EnemyShotsFired, 0);
    }
  }
}
using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    /// <summary>Slice J: third view style (flat 2D chibi) on the same 2D gameplay.</summary>
    [TestFixture]
    public class Proto_GreyboxChibiTests
    {
        [SetUp]
        public void SetUp()
        {
            Proto_GreyboxViewModeState.ResetForTests();
            Proto_GreyboxPerspectiveDepth.ResetToDefault();
            Proto_GreyboxMoveZone.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            Proto_GreyboxChibiHud.Restore();
            Proto_GreyboxViewModeState.ResetForTests();
            Proto_GreyboxWestPixelSkinState.ResetToDefault();
            Proto_GreyboxPerspectiveDepth.ResetToDefault();
            Proto_GreyboxMoveZone.ResetToDefaults();
        }

        [Test]
        public void ViewToggle_Cycles_Pixel_3D_Chibi()
        {
            Assert.AreEqual(Proto_GreyboxViewMode.View2D,
                Proto_GreyboxViewModeState.Next(Proto_GreyboxViewMode.View2DPerspective));
            Assert.AreEqual(Proto_GreyboxViewMode.View3D, Proto_GreyboxViewModeState.Next(Proto_GreyboxViewMode.View2D));
            Assert.AreEqual(Proto_GreyboxViewMode.View2DChibi, Proto_GreyboxViewModeState.Next(Proto_GreyboxViewMode.View3D));
            Assert.AreEqual(Proto_GreyboxViewMode.View2DPerspective,
                Proto_GreyboxViewModeState.Next(Proto_GreyboxViewMode.View2DChibi));
        }

        [Test]
        public void ChibiMode_PersistsAndIsNot3D()
        {
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                Proto_GreyboxViewModeState.ResetForTests();
                Proto_GreyboxViewModeState.SetMode(Proto_GreyboxViewMode.View2DChibi, persist: true);
                Assert.IsTrue(Proto_GreyboxViewModeState.IsChibi);
                Assert.IsFalse(Proto_GreyboxViewModeState.Is3D);
                Assert.AreEqual(Proto_GreyboxViewMode.View2DChibi, Proto_GreyboxViewModePrefs.LoadOrDefault());
                Assert.AreEqual(Proto_GreyboxViewMode.View2DPerspective, Proto_GreyboxViewModePrefs.FromInt(99));
            }
        }

        [Test]
        public void ToggleLabels_NameAllThreeStyles()
        {
            Assert.AreEqual("View:  2D 치비", Proto_GreyboxViewModeState.Label(Proto_GreyboxViewMode.View2DChibi, true));
            Assert.AreEqual("View:  2D Chibi", Proto_GreyboxViewModeState.Label(Proto_GreyboxViewMode.View2DChibi, false));
            Assert.AreEqual("View:  3D", Proto_GreyboxViewModeState.Label(Proto_GreyboxViewMode.View3D, true));
            StringAssert.StartsWith("View:  2D", Proto_GreyboxViewModeState.Label(Proto_GreyboxViewMode.View2D, false));
        }

        [Test]
        public void World2D_HiddenFor3D_ComesBackFor2DStyles()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var grunts = new GameObject("Greybox_Grunts");
            var cross = new GameObject("Greybox_Crosshair");
            Proto_GreyboxWorld2DVisibility.SetWorldVisible(false);
            Assert.IsFalse(grunts.activeSelf);
            Proto_GreyboxWorld2DVisibility.SetWorldVisible(false); // 3D calls this every frame
            Proto_GreyboxWorld2DVisibility.SetWorldVisible(true);
            Assert.IsTrue(grunts.activeSelf, "grunts must reappear after leaving 3D");
            Assert.IsTrue(cross.activeSelf, "crosshair must reappear after leaving 3D");
        }

        [Test]
        public void ChibiCatalog_IsSeparateAndComplete()
        {
            var chibi = Proto_GreyboxWestPixelCatalog.LoadChibiRuntime();
            var pixel = Proto_GreyboxWestPixelCatalog.LoadRuntime();
            Assert.NotNull(chibi, "Resources/Proto_GreyboxChibiCatalog missing or not gameplay-ready");
            Assert.IsTrue(chibi.isChibi);
            Assert.NotNull(chibi.bgFullScene);
            Assert.NotNull(chibi.heartFull);
            Assert.NotNull(chibi.heartEmpty);
            Assert.NotNull(chibi.scorePlate);
            Assert.NotNull(chibi.hpPlate);
            Assert.NotNull(chibi.perspFence);
            Assert.GreaterOrEqual(chibi.playerRoll.Length, 8);
            Assert.AreEqual(3, chibi.gruntDown.Length);
            // Same canvas sizes as the pixel set so the shared skin layout math holds.
            Assert.AreEqual(306f, chibi.playerIdle[0].rect.width);
            Assert.AreEqual(213f, chibi.playerIdle[0].rect.height);
            Assert.AreEqual(168f, chibi.gruntIdle[0].rect.width);
            Assert.AreEqual(147f, chibi.gruntIdle[0].rect.height);
            if (pixel != null)
            {
                Assert.IsFalse(pixel.isChibi);
                Assert.AreNotSame(pixel, chibi);
            }
        }

        [Test]
        public void SwitchToChibi_SwapsStageArt_AndBackToPixel()
        {
            if (Proto_GreyboxWestPixelCatalog.LoadChibiRuntime() == null || Proto_GreyboxWestPixelCatalog.LoadRuntime() == null)
                Assert.Ignore("catalog assets missing");
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
                var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
                builder.Build();
                var controller = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName)
                    .GetComponent<Proto_GreyboxPlayerController>();
                controller.RuntimeTuning.EnemyFireInterval = 9999f;
                controller.SetWestPixelSkin(true);
                controller.SetViewMode(Proto_GreyboxViewMode.View2D, persist: false);
                for (int i = 0; i < 10; i++) controller.Simulate(new Proto_GreyboxInput(), 0.02f);
                Assert.IsNull(GameObject.Find("WP_FullScene"));
                int hearts = controller.Hearts.Hearts;

                controller.SetViewMode(Proto_GreyboxViewMode.View2DChibi, persist: false);
                for (int i = 0; i < 10; i++) controller.Simulate(new Proto_GreyboxInput(), 0.02f);
                Assert.NotNull(GameObject.Find("WP_FullScene"), "chibi plate not built");
                Assert.NotNull(GameObject.Find(Proto_GreyboxChibiHud.OverlayName));
                var sr = controller.GetComponent<SpriteRenderer>();
                StringAssert.Contains("player_back_", sr.sprite.name);
                Assert.AreEqual(hearts, controller.Hearts.Hearts, "style swap must not touch game logic (3 hearts)");
                var fill = GameObject.Find("HeartFill_0");
                if (fill != null)
                    Assert.AreEqual(Proto_GreyboxWestPixelCatalog.LoadChibiRuntime().heartFull, fill.GetComponent<Image>().sprite);

                controller.SetViewMode(Proto_GreyboxViewMode.View2D, persist: false);
                controller.Simulate(new Proto_GreyboxInput(), 0.02f);
                Assert.IsNull(GameObject.Find("WP_FullScene"), "pixel stage should be back");
                Assert.IsNull(GameObject.Find(Proto_GreyboxChibiHud.OverlayName));
            }
        }
    }
}

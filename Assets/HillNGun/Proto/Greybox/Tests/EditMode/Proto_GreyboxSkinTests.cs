using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxSkinTests
    {
        [SetUp]
        public void SetUp()
        {
            Proto_GreyboxPerspectiveDepth.ResetToDefault();
            Proto_GreyboxMoveZone.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            Proto_GreyboxPerspectiveDepth.ResetToDefault();
            Proto_GreyboxMoveZone.ResetToDefaults();
        }

        [Test]
        public void WestPixelSkinToggle_DoesNotChangeMoveZoneOrLayoutPercents()
        {
            float minY = Proto_GreyboxMoveZone.MinFootWorldY;
            float maxY = Proto_GreyboxMoveZone.MaxFootWorldY;
            float playerFoot = Proto_GreyboxLayout.ScreenYPercentToWorldY(88f);
            float fenceTop = Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.FenceTopPercent);

            Proto_GreyboxWestPixelSkinState.Enabled = false;
            Assert.AreEqual(minY, Proto_GreyboxMoveZone.MinFootWorldY);
            Assert.AreEqual(maxY, Proto_GreyboxMoveZone.MaxFootWorldY);
            Assert.AreEqual(playerFoot, Proto_GreyboxLayout.ScreenYPercentToWorldY(88f));
            Assert.AreEqual(fenceTop, Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.FenceTopPercent));

            Proto_GreyboxWestPixelSkinState.Enabled = true;
            Assert.AreEqual(minY, Proto_GreyboxMoveZone.MinFootWorldY);
            Assert.AreEqual(playerFoot, Proto_GreyboxLayout.ScreenYPercentToWorldY(88f));
            Proto_GreyboxWestPixelSkinState.ResetToDefault();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void GruntSorting_DrawsInFrontOfTownAndGround_WithWestSkin()
        {
            int townOrder = Proto_GreyboxLayout.SortingBuildingBase;
            int order = Proto_GreyboxLayout.WorldSpriteSortingOrder(Proto_GreyboxLayout.EnemyFarFootWorldY);
            Assert.Greater(order, townOrder);
            Assert.Greater(order, Proto_GreyboxLayout.SortingGround);
            Assert.Less(order, Proto_GreyboxLayout.SortingFence);

            var catalog = Proto_GreyboxWestPixelCatalog.LoadRuntime();
            if (catalog == null || !catalog.IsGameplayReady)
            {
                Proto_GreyboxWestPixelSkinState.ResetToDefault();
                LogAssert.NoUnexpectedReceived();
                return;
            }

            EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
            Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
            var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            Assert.NotNull(builder);
            builder.Build();

            Proto_GreyboxWestPixelSkinState.Enabled = true;
            var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
            var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
            Assert.NotNull(controller);
            controller.SetWestPixelSkin(true);
            const float dt = 0.02f;
            for (int i = 0; i < 200; i++)
                controller.Simulate(new Proto_GreyboxInput(), dt);

            Assert.Greater(controller.WaveDirector.Grunts.Count, 0, "grunts spawned");
            var views = Object.FindObjectsByType<Proto_GreyboxTargetView>(FindObjectsSortMode.None);
            Assert.Greater(views.Length, 0, "grunt views in scene");
            foreach (var view in views)
            {
                if (view.Logic == null || !view.Logic.IsAlive) continue;
                var sr = view.GetComponent<SpriteRenderer>();
                Assert.NotNull(sr);
                Assert.IsTrue(sr.enabled);
                Assert.NotNull(sr.sprite);
                Assert.Greater(sr.sortingOrder, townOrder);
                Assert.Greater(sr.sortingOrder, Proto_GreyboxLayout.SortingGround);
                Assert.Less(sr.sortingOrder, Proto_GreyboxLayout.SortingFence);
                break;
            }

            Proto_GreyboxWestPixelSkinState.ResetToDefault();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CombatDrawSortingOrder_AboveGroundBuildingsAndFence()
        {
            float playerFootY = Proto_GreyboxLayout.ScreenYPercentToWorldY(88f);
            float enemyFootY = Proto_GreyboxLayout.ScreenYPercentToWorldY(45f);
            foreach (float y in new[] { playerFootY, enemyFootY })
            {
                int order = Proto_GreyboxLayout.CombatDrawSortingOrder(y);
                Assert.Greater(order, Proto_GreyboxLayout.SortingGround);
                Assert.Greater(order, Proto_GreyboxLayout.SortingBuildingBase);
                Assert.Greater(order, Proto_GreyboxLayout.SortingFence);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void WestSkin_AllyBullets_CreateVisibleViewsWithTracer()
        {
            var catalog = Proto_GreyboxWestPixelCatalog.LoadRuntime();
            if (catalog == null || !catalog.IsGameplayReady || catalog.bulletAlly == null)
            {
                Assert.Inconclusive("West pixel catalog not ready in this editor run.");
                return;
            }

            EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
            Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
            var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            Assert.NotNull(builder);
            builder.Build();

            Proto_GreyboxWestPixelSkinState.Enabled = true;
            var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
            var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
            Assert.NotNull(controller);
            controller.SetWestPixelSkin(true);
            const float dt = 0.02f;
            for (int i = 0; i < 120; i++)
                controller.Simulate(new Proto_GreyboxInput(), dt);
            controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = true }, dt);
            controller.Simulate(new Proto_GreyboxInput(), dt);

            int alive = 0;
            foreach (var b in controller.AllyBullets)
                if (b.Alive) alive++;
            Assert.Greater(alive, 0, "expected at least one live ally bullet");

            SpriteRenderer enabledCore = null;
            foreach (var sr in controller.AllyBulletViewRenderers)
            {
                if (sr == null || !sr.gameObject.activeSelf) continue;
                enabledCore = sr;
                break;
            }

            Assert.NotNull(enabledCore, "west skin must allocate ally bullet SpriteRenderers");
            Assert.Greater(enabledCore.sortingOrder, Proto_GreyboxLayout.SortingGround);
            Assert.Greater(enabledCore.sortingOrder, Proto_GreyboxLayout.SortingBuildingBase);
            Assert.Greater(enabledCore.sortingOrder, Proto_GreyboxLayout.SortingFence);

            bool tracerOn = false;
            foreach (var tr in controller.AllyTracerViewRenderers)
            {
                if (tr != null && tr.gameObject.activeSelf)
                {
                    tracerOn = true;
                    break;
                }
            }

            Assert.IsTrue(tracerOn, "ally tracer must stay enabled under west skin");

            Proto_GreyboxWestPixelSkinState.ResetToDefault();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Build_StillUsesProceduralSkyForEditModeLayoutTest()
        {
            EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
            Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
            var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            Assert.NotNull(builder);
            builder.Build();
            var sky = Proto_GreyboxScreenMetrics.FindRenderer(Proto_GreyboxStageBuilder.SkyName);
            Assert.NotNull(sky);
            Assert.IsTrue(sky.gameObject.activeSelf, "EditMode Build keeps procedural sky active until runtime skin bind");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D.Tests.EditMode
{
    public sealed class Proto_Greybox3DCatalogTests
    {
        [TearDown]
        public void TearDown()
        {
            var root = GameObject.Find("Greybox3D_ViewRoot");
            if (root != null)
                Object.DestroyImmediate(root);
            Proto_GreyboxViewPresenterRegistry.ResetForTests();
            Proto_Greybox3DFault.ResetForTests();
        }

        [Test]
        public void Catalog_ReferencesCustomShaders_SoPlayerBuildKeepsThem()
        {
            var cat = Resources.Load<Proto_Greybox3DCatalog>(Proto_Greybox3DCatalog.ResourcesPath);
            Assert.NotNull(cat, "Resources catalog missing");
            Assert.NotNull(cat.CelShader, "Cel shader must be referenced by the catalog or IL2CPP/player builds strip it");
            Assert.AreEqual("HillNGun/Proto/Greybox3D/Cel", cat.CelShader.name);
            Assert.NotNull(cat.GlowShader);
            Assert.AreEqual("HillNGun/Proto/Greybox3D/Glow", cat.GlowShader.name);
            Assert.NotNull(cat.BlobShader);
            Assert.AreEqual("HillNGun/Proto/Greybox3D/BlobShadow", cat.BlobShader.name);
            Assert.NotNull(cat.SkyShader);
            Assert.AreEqual("HillNGun/Proto/Greybox3D/Sky", cat.SkyShader.name);
        }

        [Test]
        public void Materials_Cel_DoesNotThrow_WithCatalogShaders()
        {
            var cat = Resources.Load<Proto_Greybox3DCatalog>(Proto_Greybox3DCatalog.ResourcesPath);
            Assert.NotNull(cat);
            Proto_Greybox3DMaterials.BindFromCatalog(cat);
            Assert.DoesNotThrow(() =>
            {
                var m = Proto_Greybox3DMaterials.Cel(Color.white);
                Assert.NotNull(m);
            });
        }

        [Test]
        public void RegistryEnsure_DoesNotBuild3DStage_UntilActivated()
        {
            Proto_GreyboxViewPresenterRegistry.ResetForTests();
            Proto_GreyboxViewPresenterRegistry.RegisterFactory(() => new Proto_Greybox3DViewHost());
            var presenter = Proto_GreyboxViewPresenterRegistry.Ensure();
            Assert.NotNull(presenter);
            Assert.IsNull(GameObject.Find("Greybox3D_ViewRoot"),
                "2D default launch must not build the 3D stage (a throw there freezes Awake/input).");
            presenter.SetActive(false, null);
            Assert.IsNull(GameObject.Find("Greybox3D_ViewRoot"));
        }

        [Test]
        public void Catalog_HasCowboyModelAndAnimationClips()
        {
            var cat = Resources.Load<Proto_Greybox3DCatalog>(Proto_Greybox3DCatalog.ResourcesPath);
            Assert.NotNull(cat, "Resources catalog missing — 3D models would be absent from the player build");
            Assert.IsTrue(cat.HasCowboy, "Cowboy FBX must be referenced by the Resources catalog");
            Assert.NotNull(cat.CowboyIdle, "Idle clip missing from catalog");
            Assert.NotNull(cat.CowboyWalk, "Walk clip missing from catalog");
            Assert.NotNull(cat.CowboyShoot, "Shoot clip missing from catalog");
        }

        [Test]
        public void Bootstrap_Register_ThenSetActive_CreatesViewRootAndCamera()
        {
            Proto_GreyboxViewPresenterRegistry.ClearFactoryForTests();
            Proto_Greybox3DViewBootstrap.Register();
            Assert.IsTrue(Proto_GreyboxViewPresenterRegistry.HasFactory, "Bootstrap.Register must install the 3D factory");
            var presenter = Proto_GreyboxViewPresenterRegistry.Ensure();
            Assert.NotNull(presenter);
            presenter.SetActive(true, null);
            var root = GameObject.Find("Greybox3D_ViewRoot");
            Assert.NotNull(root, "toggle/startup SetActive(true) must create the 3D stage");
            Assert.NotNull(presenter.WorldCamera, "3D camera must exist after SetActive(true)");
            presenter.SetActive(false, null);
            if (root != null)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyViewMode_WithoutFactory_Stays2D_AndShowsFault()
        {
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                Proto_GreyboxViewModeState.ResetForTests();
                Proto_GreyboxViewPresenterRegistry.ClearFactoryForTests();
                Proto_GreyboxRuntimeDebugUi.SetFaultLine(null);

                EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
                var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
                Assert.NotNull(builder);
                builder.Build();
                var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
                var controller = player.GetComponent<Proto_GreyboxPlayerController>();
                controller.SetViewMode(Proto_GreyboxViewMode.View3D, persist: false);

                Assert.IsFalse(Proto_GreyboxViewModeState.Is3D, "missing factory must revert to 2D");
                Assert.IsNull(GameObject.Find("Greybox3D_ViewRoot"));
                var cam2d = Proto_GreyboxWorld2DVisibility.Find2DWorldCamera();
                Assert.NotNull(cam2d);
                Assert.IsTrue(cam2d.enabled, "2D camera must stay on if 3D cannot start");
                Assert.IsFalse(string.IsNullOrEmpty(Proto_GreyboxRuntimeDebugUi.LastFaultLine),
                    "3D failure must show on-screen error text");
            }
        }

        [Test]
        public void Prefs3D_OnBuildWithFactory_CreatesViewRoot()
        {
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                Proto_GreyboxViewPresenterRegistry.ClearFactoryForTests();
                Proto_Greybox3DViewBootstrap.Register();
                Proto_GreyboxViewModePrefs.Save(Proto_GreyboxViewMode.View3D);
                Proto_GreyboxViewModeState.ResetForTests();

                EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
                var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
                Assert.NotNull(builder);
                builder.Build();

                Assert.IsTrue(Proto_GreyboxViewModeState.Is3D);
                var root = GameObject.Find("Greybox3D_ViewRoot");
                Assert.NotNull(root, "startup must create 3D when prefs say 3D");
                var presenter = Proto_GreyboxViewPresenterRegistry.Ensure();
                Assert.NotNull(presenter);
                Assert.NotNull(presenter.WorldCamera);
                var cam2d = Proto_GreyboxWorld2DVisibility.Find2DWorldCamera();
                if (cam2d != null)
                    Assert.IsFalse(cam2d.enabled, "2D camera should be disabled in 3D");
                presenter.SetActive(false, null);
                if (root != null)
                    Object.DestroyImmediate(root);
            }
        }
    }
}

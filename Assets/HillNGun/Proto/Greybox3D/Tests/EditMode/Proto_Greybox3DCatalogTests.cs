using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D.Tests.EditMode
{
    public sealed class Proto_Greybox3DCatalogTests
    {
        [TearDown]
        public void TearDown()
        {
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
    }
}

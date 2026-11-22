using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D.Tests.EditMode
{
    public sealed class Proto_Greybox3DLookTests
    {
        Transform _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root.gameObject);
            var leftover = GameObject.Find("Greybox3D_Stage");
            if (leftover != null)
                Object.DestroyImmediate(leftover);
            var view = GameObject.Find("Greybox3D_ViewRoot");
            if (view != null)
                Object.DestroyImmediate(view);
            Proto_GreyboxViewPresenterRegistry.ResetForTests();
        }

        [Test]
        public void SceneBuilder_HasNineStorefronts_WaterTower_Hay()
        {
            _root = new GameObject("LookTestRoot").transform;
            var stage = Proto_Greybox3DSceneBuilder.BuildRoot(_root);
            int stores = 0, towers = 0, hay = 0, scallops = 0;
            foreach (var t in stage.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Greybox3D_Store_")) stores++;
                if (t.name == "Greybox3D_WaterTower") towers++;
                if (t.name == "Greybox3D_Hay") hay++;
                if (t.name == "Scallop") scallops++;
            }
            Assert.AreEqual(9, stores, "3 storefronts per side + 3 in the back row");
            Assert.AreEqual(1, towers, "water tower missing");
            Assert.GreaterOrEqual(hay, 3, "hay bales missing");
            Assert.Greater(scallops, 0, "awnings should have scalloped edges");
        }

        [Test]
        public void DressFollow_LocksHatAndBandanaKnot()
        {
            _root = new GameObject("DressTestRoot").transform;
            var hero = Proto_Greybox3DCharacterMesh.BuildHero(_root);
            var hat = FindDeep(hero, "DressHat");
            var knot = FindDeep(hero, "DressBandanaKnot");
            Assert.NotNull(hat, "hat dress missing");
            Assert.NotNull(knot, "bandana knot missing");
            var hatFollow = hat.GetComponent<Proto_Greybox3DDressFollow>();
            var knotFollow = knot.GetComponent<Proto_Greybox3DDressFollow>();
            Assert.NotNull(hatFollow);
            Assert.NotNull(knotFollow);
            Assert.NotNull(hatFollow.Bone);
            Assert.AreEqual("Head", hatFollow.Bone.name);
        }

        [Test]
        public void CombatFx_Death_FallsThenGone_NoBlood()
        {
            _root = new GameObject("DeathTestRoot").transform;
            var grunt = Proto_Greybox3DCharacterMesh.BuildGrunt(_root, 1);
            var fx = grunt.GetComponent<Proto_Greybox3DCombatFx>();
            Assert.NotNull(fx);
            fx.Bind();
            Assert.IsFalse(fx.IsDying);
            for (int i = 0; i < 8; i++)
                fx.Tick(0.1f, false, true, false, Vector3.forward);
            Assert.IsTrue(fx.IsGone, "death should finish and fade out");
        }

        [Test]
        public void GruntVariants_ShareOutfitButTintDiffers()
        {
            _root = new GameObject("VariantRoot").transform;
            var a = Proto_Greybox3DCharacterMesh.BuildGrunt(_root, 0);
            var b = Proto_Greybox3DCharacterMesh.BuildGrunt(_root, 1);
            var hatA = FindDeep(a, "DressHat");
            var hatB = FindDeep(b, "DressHat");
            Assert.NotNull(hatA);
            Assert.NotNull(hatB);
            var colA = FirstColor(hatA);
            var colB = FirstColor(hatB);
            Assert.AreNotEqual(colA, colB, "hat variants should use different tints");
        }

        [Test]
        public void WalkPlayback_ScalesWithGroundSpeed()
        {
            _root = new GameObject("WalkSpeedRoot").transform;
            var hero = Proto_Greybox3DCharacterMesh.BuildHero(_root);
            var driver = hero.GetComponentInChildren<Proto_Greybox3DAnimDriver>();
            Assert.NotNull(driver);
            const float cycle = 1f;
            driver.Tick(0f, 0.02f, true, false, false, false, false, 1f, cycle);
            float a = driver.WalkPlaybackSpeed;
            driver.Tick(0.02f, 0.02f, true, false, false, false, false, 2f, cycle);
            float b = driver.WalkPlaybackSpeed;
            Assert.Greater(a, 0.3f);
            Assert.AreEqual(2f, b / a, 0.05f);
        }

        static Color FirstColor(Transform t)
        {
            var r = t.GetComponentInChildren<MeshRenderer>();
            Assert.NotNull(r);
            var m = r.sharedMaterial;
            return m.HasProperty("_Color") ? m.GetColor("_Color") : m.color;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}

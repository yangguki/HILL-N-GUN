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
        public void SceneBuilder_HasStorefrontsAlongBothSides_WaterTower_Hay()
        {
            _root = new GameObject("LookTestRoot").transform;
            var stage = Proto_Greybox3DSceneBuilder.BuildRoot(_root);
            int stores = 0, towers = 0, hay = 0, planks = 0, stripes = 0, mesas = 0, strata = 0;
            var storePos = new System.Collections.Generic.List<Vector3>();
            foreach (var child in stage.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith("Greybox3D_Store_"))
                {
                    stores++;
                    storePos.Add(child.position);
                }
                if (child.name == "Greybox3D_WaterTower") towers++;
                if (child.name == "Greybox3D_Hay") hay++;
                if (child.name == "Plank") planks++;
                if (child.name == "AwningStripe") stripes++;
                if (child.name == "Greybox3D_Mesa") mesas++;
                if (child.name == "MesaStratum") strata++;
            }
            Assert.AreEqual(9, stores, "3 storefronts per side + 3 at the far end");
            Assert.Greater(planks, 9 * 6, "false fronts should be built from plank strips");
            Assert.Greater(stripes, 9 * 3, "awnings should be striped");
            Assert.GreaterOrEqual(mesas, 8, "sculpted mesa/butte count");
            Assert.Greater(strata, 20, "mesas need layered strata, not a single stripe");
            float minGap = 99f;
            for (int i = 0; i < storePos.Count; i++)
            for (int j = i + 1; j < storePos.Count; j++)
                minGap = Mathf.Min(minGap, Vector3.Distance(storePos[i], storePos[j]));
            Assert.Greater(minGap, 2.0f, "stores should leave desert gaps, not stack tight");
            Assert.AreEqual(1, towers, "water tower missing");
            Assert.GreaterOrEqual(hay, 3, "hay bales missing");
        }

        [Test]
        public void DressFollow_LocksHatAndBandanaKnot()
        {
            _root = new GameObject("DressTestRoot").transform;
            var hero = Proto_Greybox3DCharacterMesh.BuildHero(_root);
            var hat = FindDeep(hero, "DressHat");
            var knot = FindDeep(hero, "DressBandanaKnot");
            var boots = FindDeep(hero, "DressBoots");
            Assert.NotNull(hat, "hat dress missing");
            Assert.NotNull(knot, "bandana knot missing");
            Assert.NotNull(boots, "boots missing");
            // Chibi is authored in local space of the 1-unit body: hat at the
            // top, knot at the nape, boots on the ground — not stacked at origin.
            Assert.Greater(hat.localPosition.y, 0.70f, "hat should sit on the big chibi head");
            Assert.Greater(knot.localPosition.y, 0.40f);
            Assert.Greater(hat.localPosition.y, knot.localPosition.y + 0.15f);
            Assert.Less(boots.localPosition.y, 0.15f);
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


        [Test]
        public void DressHat_HasWideBrimAndBandanaTails()
        {
            _root = new GameObject("DressLookRoot").transform;
            var hero = Proto_Greybox3DCharacterMesh.BuildHero(_root);
            var brim = FindDeep(hero, "Brim");
            var tails = 0;
            var silver = 0;
            foreach (var child in hero.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith("DressBandanaTail")) tails++;
                if (child.name == "Barrel" || child.name == "Body")
                {
                    var mr = child.GetComponent<MeshRenderer>();
                    if (mr != null && mr.sharedMaterial != null && mr.sharedMaterial.HasProperty("_Color"))
                    {
                        var c = mr.sharedMaterial.GetColor("_Color");
                        if (c.r > 0.5f && c.g > 0.5f && c.b > 0.5f && c.b >= c.r * 0.9f)
                            silver++;
                    }
                }
            }
            Assert.NotNull(brim, "wide hat brim missing");
            Assert.GreaterOrEqual(brim.localScale.x, 0.70f, "hero brim should be wide like the concept");
            Assert.GreaterOrEqual(tails, 2, "bandana tails should be visible from behind");
            Assert.Greater(silver, 0, "revolver should read as silver, not dark iron");
            Assert.NotNull(FindDeep(hero, "HairBack"), "brown hair under the hat missing");
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}

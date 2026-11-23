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
        public void MiniHero_HasArmatureGunAndBlobShadow()
        {
            _root = new GameObject("MiniHeroRoot").transform;
            var hero = Proto_Greybox3DCharacterMesh.BuildHero(_root);
            Assert.NotNull(FindDeep(hero, "arm-right") ?? FindDeepContains(hero, "arm-right"),
                "Kenney mini armature arm-right missing");
            Assert.NotNull(FindDeep(hero, "head") ?? FindDeepContains(hero, "head"), "head bone missing");
            Assert.NotNull(FindDeep(hero, "MiniGun"), "gun prop missing");
            Assert.NotNull(FindDeep(hero, "BlobShadow"), "blob shadow missing");
            Assert.Greater(hero.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, 0,
                "hero skinned mesh should be visible");
            Assert.NotNull(hero.GetComponentInChildren<Proto_Greybox3DAnimDriver>(), "anim driver missing");
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
            var texA = FirstTexName(a);
            var texB = FirstTexName(b);
            Assert.IsNotEmpty(texA);
            Assert.IsNotEmpty(texB);
            Assert.AreNotEqual(texA, texB, "grunt variants should use different palette remaps");
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

        static string FirstTexName(Transform t)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.sharedMaterial == null) continue;
                if (r.name == "BlobShadow") continue;
                if (r.sharedMaterial.HasProperty("_MainTex"))
                {
                    var tex = r.sharedMaterial.GetTexture("_MainTex");
                    if (tex != null) return tex.name;
                }
            }
            return "";
        }

        [Test]
        public void MiniHeroAndGrunt_AreDifferentModels()
        {
            _root = new GameObject("MiniPairRoot").transform;
            var hero = Proto_Greybox3DCharacterMesh.BuildHero(_root);
            var grunt = Proto_Greybox3DCharacterMesh.BuildGrunt(_root, 0);
            var heroModel = FindDeep(hero, "Model");
            var gruntModel = FindDeep(grunt, "Model");
            Assert.NotNull(heroModel);
            Assert.NotNull(gruntModel);
            // Instantiated names stay "Model"; distinguish by source mesh / palette.
            var heroTex = FirstTexName(hero);
            var gruntTex = FirstTexName(grunt);
            Assert.IsNotEmpty(heroTex);
            Assert.IsNotEmpty(gruntTex);
            Assert.AreNotEqual(heroTex, gruntTex, "hero and grunt should use different remapped colormaps");
            Assert.NotNull(FindDeep(grunt, "MiniGun"), "grunt should also hold a gun");
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        static Transform FindDeepContains(Transform root, string name)
        {
            string n = name.ToLowerInvariant();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.ToLowerInvariant().Contains(n)) return t;
            return null;
        }
        [Test]
        public void SceneBuilder_HasStreetProps()
        {
            _root = new GameObject("PropRoot").transform;
            var stage = Proto_Greybox3DSceneBuilder.BuildRoot(_root);
            Assert.Greater(CountByName(stage, "Greybox3D_Cart"), 0, "cart prop missing");
            Assert.Greater(CountByName(stage, "Greybox3D_Stall"), 0, "stall prop missing");
            Assert.Greater(CountByName(stage, "Greybox3D_LampPost"), 0, "lamp post missing");
        }

        [Test]
        public void DizzyStars_AppearDuringLightDizzy()
        {
            _root = new GameObject("DizzyRoot").transform;
            var hero = Proto_Greybox3DCharacterMesh.BuildHero(_root);
            var stars = Proto_Greybox3DDizzyStars.Ensure(hero);
            Assert.NotNull(stars);
            var dizzy = new Proto_GreyboxRollDizzy();
            // 3 chained rolls trigger Light dizzy.
            float t = 0f;
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(dizzy.TryAcceptRollStart(t));
                dizzy.NotifyRollStarted(t);
                t += 0.2f;
                dizzy.NotifyRollEnded(t);
                t += 0.05f;
            }
            Assert.AreEqual(Proto_GreyboxRollDizzyPhase.Light, dizzy.Phase);
            stars.Tick(0.02f, dizzy, null);
            bool any = false;
            foreach (var tr in hero.GetComponentsInChildren<Transform>(true))
            {
                if (tr.name.StartsWith("DizzyStar") || tr.name == "DizzyStarRing")
                {
                    any = tr.gameObject.activeSelf || any;
                }
            }
            Assert.IsTrue(any, "dizzy stars/ring should be visible during 3rd-roll dizzy");
        }

        static int CountByName(Transform root, string name)
        {
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) n++;
            return n;
        }

    }
}

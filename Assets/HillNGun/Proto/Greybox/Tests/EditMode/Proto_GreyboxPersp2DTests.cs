using HillNGun.Proto.Greybox3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxPersp2DTests
    {
        [SetUp]
        public void SetUp()
        {
            Proto_GreyboxViewModeState.ResetForTests();
            Proto_GreyboxPerspectiveDepth.ResetToDefault();
            Proto_Greybox3DHudLayout.Enabled = false;
            Proto_GreyboxPersp2DHud.Restore();
        }

        [TearDown]
        public void TearDown()
        {
            Proto_Greybox3DHudLayout.Enabled = false;
            Proto_GreyboxPersp2DHud.Restore();
        }

        [Test]
        public void DefaultViewMode_IsPersp2D()
        {
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                Proto_GreyboxViewModeState.ResetForTests();
                Assert.AreEqual(Proto_GreyboxViewMode.View2DPerspective, Proto_GreyboxViewModeState.Current);
                Assert.AreEqual(Proto_GreyboxViewMode.View2DPerspective, Proto_GreyboxViewModeState.DefaultMode);
            }
        }

        [Test]
        public void ViewToggle_Cycles_FourStyles()
        {
            Assert.AreEqual(Proto_GreyboxViewMode.View2D,
                Proto_GreyboxViewModeState.Next(Proto_GreyboxViewMode.View2DPerspective));
            Assert.AreEqual(Proto_GreyboxViewMode.View3D, Proto_GreyboxViewModeState.Next(Proto_GreyboxViewMode.View2D));
            Assert.AreEqual(Proto_GreyboxViewMode.View2DChibi, Proto_GreyboxViewModeState.Next(Proto_GreyboxViewMode.View3D));
            Assert.AreEqual(Proto_GreyboxViewMode.View2DPerspective,
                Proto_GreyboxViewModeState.Next(Proto_GreyboxViewMode.View2DChibi));
        }

        [Test]
        public void PerspLabels_AndUnknownPref()
        {
            Assert.AreEqual("View:  2D 원근", Proto_GreyboxViewModeState.Label(Proto_GreyboxViewMode.View2DPerspective, true));
            Assert.AreEqual("View:  2D Persp", Proto_GreyboxViewModeState.Label(Proto_GreyboxViewMode.View2DPerspective, false));
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                Assert.AreEqual(Proto_GreyboxViewMode.View2DPerspective, Proto_GreyboxViewModePrefs.FromInt(99));
            }
        }

        [Test]
        public void EngineScale_HeroRow_IsAbout152()
        {
            float s = Proto_GreyboxPersp2DMath.EngineScaleAtFeetY2x(1764f);
            Assert.AreEqual(1.52f, s, 0.02f);
        }

        [Test]
        public void PlayerStartFoot_Persp2D_Is817Percent()
        {
            Proto_GreyboxPlayerSpawn.StartFootWorldYOverride = null;
            float y = Proto_GreyboxPlayerSpawn.PlayerStartFootWorldY(Proto_GreyboxViewMode.View2DPerspective);
            float pct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(y);
            Assert.AreEqual(Proto_GreyboxPersp2DStageLayout.HeroStartFootPercent, pct, 0.5f);
        }

        [Test]
        public void PlayerStartFoot_OtherModes_UseLegacyNearRow12World()
        {
            Proto_GreyboxPlayerSpawn.StartFootWorldYOverride = null;
            foreach (var mode in new[]
                     {
                         Proto_GreyboxViewMode.View2D, Proto_GreyboxViewMode.View3D,
                         Proto_GreyboxViewMode.View2DChibi
                     })
            {
                float y = Proto_GreyboxPlayerSpawn.PlayerStartFootWorldY(mode);
                Assert.AreEqual(Proto_GreyboxLayout.PlayerNearFootWorldY, y, 0.001f, mode.ToString());
            }
        }

        [Test]
        public void PlayerSpawn_FirstInit_MatchesViewMode_WithoutFullRestart()
        {
            const float legacyFootPct = 88f;
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                Proto_GreyboxPlayerSpawn.StartFootWorldYOverride = null;
                Proto_GreyboxViewModeState.SetMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
                EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
                UnityEngine.Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>()?.Build();
                var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
                Assert.NotNull(player);
                var controller = player.GetComponent<Proto_GreyboxPlayerController>();
                Assert.NotNull(controller);
                player.transform.position = new Vector3(
                    Proto_GreyboxLayout.ScreenXPercentToWorldX(49f),
                    Proto_GreyboxLayout.ScreenYPercentToWorldY(legacyFootPct), 0f);
                controller.Simulate(new Proto_GreyboxInput(), 1f / 60f);
                float pctPersp = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(controller.Motor.FootPosition.y);
                Assert.AreEqual(Proto_GreyboxPersp2DStageLayout.HeroStartFootPercent, pctPersp, 0.5f);

                Proto_GreyboxViewModeState.SetMode(Proto_GreyboxViewMode.View2DChibi, persist: false);
                UnityEngine.Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>()?.Build();
                player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
                controller = player.GetComponent<Proto_GreyboxPlayerController>();
                Assert.NotNull(controller);
                player.transform.position = new Vector3(
                    Proto_GreyboxLayout.ScreenXPercentToWorldX(49f),
                    Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPersp2DStageLayout.HeroStartFootPercent), 0f);
                controller.Simulate(new Proto_GreyboxInput(), 1f / 60f);
                float pctChibi = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(controller.Motor.FootPosition.y);
                Assert.AreEqual(legacyFootPct, pctChibi, 0.5f);
            }
        }

        [Test]
        public void EngineScale_ClampsAtMinimum()
        {
            Assert.AreEqual(Proto_GreyboxPersp2DMath.MinEngineScale,
                Proto_GreyboxPersp2DMath.EngineScaleAtFeetY2x(900f));
        }

        [Test]
        public void PorchSort_BehindVsStreet()
        {
            const float porchX = -5.2f;
            const float nearZ = 15f;
            Assert.IsTrue(Proto_GreyboxPersp2DMath.PorchFrontDrawsOverActor(-6f, 16f, porchX, nearZ));
            Assert.IsFalse(Proto_GreyboxPersp2DMath.PorchFrontDrawsOverActor(-3f, 16f, porchX, nearZ));
        }

        [Test]
        public void PorchCaptureCoords_FrontStreet_BehindDeck()
        {
            const float porchX = -5.2f;
            const float nearZ = 15f;
            Proto_GreyboxPersp2DMath.ActorWorldFromFootScreen2x(750f, 1380f, out var xf, out var zf);
            Proto_GreyboxPersp2DMath.ActorWorldFromFootScreen2x(580f, 1265f, out var xb, out var zb);
            Assert.IsFalse(Proto_GreyboxPersp2DMath.PorchFrontDrawsOverActor(xf, zf, porchX, nearZ));
            Assert.IsTrue(Proto_GreyboxPersp2DMath.PorchFrontDrawsOverActor(xb, zb, porchX, nearZ));
        }

        [Test]
        public void Crosshair_DecorBanditChest_NotDefaultSaloon()
        {
            Proto_GreyboxCapturePersp2D.DecorStreetBandits = true;
            try
            {
                EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
                Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>()?.Build();
                var controller = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName)
                    ?.GetComponent<Proto_GreyboxPlayerController>();
                Assert.NotNull(controller);
                controller.SetViewMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
                var cam = Camera.main;
                cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
                Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
                controller.PreparePerspCaptureDecor();
                for (int i = 0; i < 8; i++)
                    controller.Simulate(new Proto_GreyboxInput(), dt: 0.02f);
                Assert.IsTrue(controller.TryGetPersp2DCrosshairScreen(out var x, out var y));
                Assert.Greater(x, 40f);
                Assert.Less(x, 65f);
                Assert.Greater(y, 48f);
                Assert.Less(y, 60f);
            }
            finally
            {
                Proto_GreyboxCapturePersp2D.DecorStreetBandits = false;
            }
        }

        [Test]
        public void Direction_MirrorMapping()
        {
            Proto_GreyboxPersp2DDirection.Resolve("hero", "W", out var dir, out var flip);
            Assert.AreEqual("E", dir);
            Assert.IsTrue(flip);
            Assert.AreEqual("SE", Proto_GreyboxPersp2DDirection.PickFromVector(new Vector2(1f, 1f).normalized));
        }

        [Test]
        public void Direction_BanditMap_FromJson()
        {
            var cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
            if (cat == null)
            {
                Assert.Ignore("Proto_GreyboxPersp2DCatalog asset missing");
                return;
            }

            Proto_GreyboxPersp2DDirection.Resolve("bandit", "SW", out var dir, out var flip);
            Assert.AreEqual("S", dir);
            Assert.IsFalse(flip);
            Proto_GreyboxPersp2DDirection.Resolve("bandit", "W", out dir, out flip);
            Assert.AreEqual("E", dir);
            Assert.IsTrue(flip);
        }

        [Test]
        public void Persp2DCatalog_FrameCountsMatchJson()
        {
            var cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
            if (cat == null)
            {
                Assert.Ignore("Proto_GreyboxPersp2DCatalog asset missing");
                return;
            }

            Assert.IsFalse(cat.ReferencesKenney());
            Assert.AreEqual(4, cat.FrameCount("hero", "idle"));
            Assert.AreEqual(6, cat.FrameCount("hero", "run"));
            Assert.AreEqual(3, cat.FrameCount("hero", "shoot"));
            Assert.AreEqual(13, cat.FrameCount("hero", "roll"));
            Assert.AreEqual(2, cat.FrameCount("hero", "hit"));
            Assert.AreEqual(4, cat.FrameCount("hero", "down"));
            Assert.AreEqual(4, cat.FrameCount("bandit", "idle"));
            Assert.AreEqual(6, cat.FrameCount("bandit", "run"));
            Assert.AreEqual(3, cat.FrameCount("bandit", "shoot"));
            Assert.AreEqual(2, cat.FrameCount("bandit", "hit"));
            Assert.AreEqual(4, cat.FrameCount("bandit", "down"));
            foreach (var anim in new[] { "idle", "run", "shoot", "roll", "hit", "down" })
            {
                var hero = cat.FramesFor("hero", anim, "S");
                Assert.NotNull(hero, anim);
                Assert.Greater(hero.Length, 0);
                Assert.NotNull(hero[0]);
            }
        }

        [Test]
        public void Persp2DDepthBands_ActiveInMode()
        {
            Proto_GreyboxViewModeState.SetMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
            Assert.AreEqual(47f, Proto_GreyboxPerspectiveDepth.EnemyFootMinPercent, 0.01f);
            Assert.AreEqual(58f, Proto_GreyboxPerspectiveDepth.EnemyFootMaxPercent, 0.01f);
            Assert.AreEqual(62f, Proto_GreyboxPerspectiveDepth.MoveFootMinPercent, 0.01f);
        }

        [Test]
        public void StageLayout_RoadTopLeft_MatchesHorizonPercent()
        {
            float yPct = Proto_GreyboxPersp2DStageLayout.ScreenPercentYFromRefY2x(
                Proto_GreyboxPersp2DStageLayout.RoadTopLeftY2x);
            Assert.AreEqual(42f, yPct, 0.15f);
            var w = Proto_GreyboxPersp2DStageLayout.RefPivotToWorld(0f, Proto_GreyboxPersp2DStageLayout.RoadTopLeftY2x);
            float pct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(w.y);
            Assert.AreEqual(42f, pct, 0.15f);
        }

        [Test]
        public void StageLayout_SaloonPivot_RefPx()
        {
            var lay = Proto_GreyboxPersp2DStageLayout.GetBuilding("saloon_L");
            Assert.AreEqual(995.6f, lay.pivotX, 0.5f);
            Assert.AreEqual(1333.7f, lay.pivotY, 0.5f);
            var w = Proto_GreyboxPersp2DStageLayout.RefPivotToWorld(lay.pivotX, lay.pivotY);
            float xPct = (w.x / Proto_GreyboxLayout.WorldWidth + 0.5f) * 100f;
            float yPct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(w.y);
            Assert.AreEqual(lay.pivotX / Proto_GreyboxPersp2DStageLayout.RefW * 100f, xPct, 0.12f);
            Assert.AreEqual(lay.pivotY / Proto_GreyboxPersp2DStageLayout.RefH * 100f, yPct, 0.12f);
        }

        [Test]
        public void HeroRenderedHeight_AtFeet1764_Is26Percent()
        {
            float frac = Proto_GreyboxPersp2DStageLayout.HeroScreenHeightFractionAtFeetY2x(1764f);
            Assert.AreEqual(0.26f, frac, 0.01f);
        }

        [Test]
        public void BakedLayout_StoreSignOffset_MatchesJson()
        {
            var cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
            if (cat == null)
            {
                Assert.Ignore("Proto_GreyboxPersp2DCatalog asset missing");
                return;
            }

            var store = Proto_GreyboxPersp2DEnvLayout.FindBuilding(cat, "store_L");
            Assert.AreEqual("store_L", store.id);
            Assert.AreEqual(-255.5f, store.signOffsetX, 0.2f);
            Assert.AreEqual(-252.3f, store.signOffsetY, 0.2f);
            Assert.AreEqual(1570.9f, store.pivotX, 0.5f);
            Assert.AreEqual(-4.0f, store.porchEdgeX, 0.1f);
            Assert.AreEqual(29f, store.nearPorchZ, 0.1f);
        }

        const string ArtRoot = "Assets/HillNGun/Proto/Greybox/Art/Persp2D/Environment";

        static void AssertImporterPivot(string rel, float expectedX, float expectedY)
        {
            var path = $"{ArtRoot}/{rel}";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.NotNull(importer, path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.AreEqual((int)SpriteAlignment.Custom, settings.spriteAlignment, path);
            Assert.AreEqual(expectedX, importer.spritePivot.x, 0.001f, path);
            Assert.AreEqual(expectedY, importer.spritePivot.y, 0.001f, path);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.NotNull(sprite, path);
            float px = sprite.pivot.x;
            float py = sprite.pivot.y;
            Assert.AreEqual(expectedX, px / sprite.rect.width, 0.002f, path + " sprite px pivot x");
            Assert.AreEqual(expectedY, py / sprite.rect.height, 0.002f, path + " sprite px pivot y");
        }

        [Test]
        public void EnvImporter_PivotNorm_MatchesJson()
        {
            AssertImporterPivot("Buildings/hng_env_bld_saloon_L_back.png", 0.77591f, 0.04095f);
            AssertImporterPivot("Buildings/hng_env_bld_hotel_R_front.png", 0.21467f, 0.04064f);
            AssertImporterPivot("Buildings/hng_env_bld_sheriff_R_back.png", 0.21957f, 0.08401f);
            AssertImporterPivot("Buildings/hng_env_sign_saloon.png", 0.5f, 0.03636f);
            AssertImporterPivot("Props/hng_env_prop_barrel.png", 0.50074f, 0.13178f);
        }

        [Test]
        public void BuildingPlacement_TopLeftRefPx_SaloonAndHotel()
        {
            var saloon = AssetDatabase.LoadAssetAtPath<Sprite>(
                $"{ArtRoot}/Buildings/hng_env_bld_saloon_L_back.png");
            var hotel = AssetDatabase.LoadAssetAtPath<Sprite>(
                $"{ArtRoot}/Buildings/hng_env_bld_hotel_R_back.png");
            if (saloon == null || hotel == null)
            {
                Assert.Ignore("env sprites missing");
                return;
            }

            var saloonLay = Proto_GreyboxPersp2DStageLayout.GetBuilding("saloon_L");
            var hotelLay = Proto_GreyboxPersp2DStageLayout.GetBuilding("hotel_R");
            var saloonBake = Proto_GreyboxPersp2DEnvLayout.FindBuilding(Proto_GreyboxPersp2DCatalog.LoadRuntime(), "saloon_L");
            var hotelBake = Proto_GreyboxPersp2DEnvLayout.FindBuilding(Proto_GreyboxPersp2DCatalog.LoadRuntime(), "hotel_R");

            var saloonImp = AssetImporter.GetAtPath($"{ArtRoot}/Buildings/hng_env_bld_saloon_L_back.png") as TextureImporter;
            var hotelImp = AssetImporter.GetAtPath($"{ArtRoot}/Buildings/hng_env_bld_hotel_R_back.png") as TextureImporter;
            var saloonTopLeft = Proto_GreyboxPersp2DStageLayout.RefTopLeftFromPivotNorm(
                saloonLay.pivotX, saloonLay.pivotY, saloonImp.spritePivot,
                saloonBake.nativeW, saloonBake.nativeH);
            var hotelTopLeft = Proto_GreyboxPersp2DStageLayout.RefTopLeftFromPivotNorm(
                hotelLay.pivotX, hotelLay.pivotY, hotelImp.spritePivot,
                hotelBake.nativeW, hotelBake.nativeH);

            Assert.AreEqual(-557f, saloonTopLeft.x, 2f);
            Assert.AreEqual(226f, saloonTopLeft.y, 2f);
            Assert.AreEqual(2420f, hotelTopLeft.x, 2f);
            Assert.AreEqual(217f, hotelTopLeft.y, 2f);
        }

        [Test]
        public void RoadTopLeftPlacement_SpansRefWidth()
        {
            var cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
            if (cat?.road == null)
            {
                Assert.Ignore("Proto_GreyboxPersp2DCatalog asset missing");
                return;
            }

            var plane = cat.roadPlane;
            float nativeW = plane.nativeW > 0f ? plane.nativeW : 3840f;
            float nativeH = plane.nativeH > 0f ? plane.nativeH : 1253f;
            float nx = cat.road.pivot.x / Mathf.Max(cat.road.rect.width, 1f);
            var piv = Proto_GreyboxPersp2DStageLayout.RefPivotFromTopLeft(
                plane.topLeftX, plane.topLeftY, cat.road, nativeW, nativeH);
            Proto_GreyboxPersp2DStageLayout.RoadRefBounds(piv.x, piv.y, nativeW, nativeH, new Vector2(nx, 1f),
                out float left, out float right);
            Assert.AreEqual(0f, left, 2f);
            Assert.AreEqual(3840f, right, 2f);
        }

        [Test]
        public void Persp2DHud_UsesConceptSprites()
        {
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                Proto_GreyboxViewModeState.SetMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
                var camGo = new GameObject("TestCam");
                var cam = camGo.AddComponent<Camera>();
                var canvas = Proto_GreyboxHudBuilder.Build(cam, null);
                var cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
                if (cat?.uiHeartFull == null || cat.uiIconRoll == null)
                {
                    Object.DestroyImmediate(camGo);
                    Assert.Ignore("Persp2D UI catalog incomplete");
                    return;
                }

                Proto_GreyboxPersp2DHud.Apply(canvas.transform);
                var overlay = canvas.transform.Find(Proto_GreyboxPersp2DHud.OverlayName);
                Assert.IsNotNull(overlay);
                for (int i = 0; i < 3; i++)
                {
                    var heart = overlay.Find("Heart" + i)?.GetComponent<UnityEngine.UI.Image>();
                    Assert.IsNotNull(heart?.sprite, "heart sprite");
                }

                Assert.IsFalse(canvas.transform.Find(Proto_GreyboxHudBuilder.ScoreTextName).gameObject.activeSelf);
                var rollIcon = canvas.transform.Find(Proto_GreyboxHudBuilder.RollButtonName + "/P2D_Icon")
                    ?.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(rollIcon?.sprite);
                StringAssert.Contains("roll", rollIcon.sprite.name.ToLowerInvariant());
                StringAssert.Contains("roll", cat.uiIconRoll.name.ToLowerInvariant());
                foreach (var btn in new[]
                         {
                             Proto_GreyboxHudBuilder.FireButtonName, Proto_GreyboxHudBuilder.RollButtonName,
                             Proto_GreyboxHudBuilder.GrenadeButtonName, Proto_GreyboxHudBuilder.ReloadButtonName
                         })
                    Assert.IsNotNull(canvas.transform.Find(btn + "/P2D_Icon")?.GetComponent<UnityEngine.UI.Image>()
                        ?.sprite);

                foreach (var label in new[] { "P2D_Label_Attack", "P2D_Label_Roll", "P2D_Label_Skill", "P2D_Label_Reload" })
                    Assert.IsNotNull(overlay.Find(label)?.GetComponent<UnityEngine.UI.Image>()?.sprite, label);

                Object.DestroyImmediate(camGo);
            }
        }

        [Test]
        public void SkyGradient_RefBoundsCoverDesignerBand()
        {
            Proto_GreyboxPersp2DSkyGradient.GetRefBoundsAfterScale(out var refW, out var refH);
            Assert.AreEqual(Proto_GreyboxPersp2DStageLayout.RefW, refW, 4f);
            Assert.AreEqual(Proto_GreyboxPersp2DStageLayout.SkyGradientBottomY2x, refH, 6f);
        }

        [Test]
        public void Persp2DHudLayout_ButtonRowMatchesConcept()
        {
            Assert.AreEqual(Proto_GreyboxPersp2DHudLayout.AttackX, 68f, 0.1f);
            Assert.AreEqual(Proto_GreyboxPersp2DHudLayout.ReloadX, 94f, 0.1f);
            Assert.Less(Proto_GreyboxPersp2DHudLayout.RollX, Proto_GreyboxPersp2DHudLayout.SkillX);
            Assert.Less(Proto_GreyboxPersp2DHudLayout.SkillX, Proto_GreyboxPersp2DHudLayout.ReloadX);
            Assert.AreEqual(Proto_GreyboxPersp2DHudLayout.BtnRowY, 84f, 0.1f);
            Assert.Greater(Proto_GreyboxPersp2DHudLayout.LabelRowY, Proto_GreyboxPersp2DHudLayout.BtnRowY);
        }

        [Test]
        public void Persp2DCatalog_AllHudUiSpritesNonNull()
        {
            var cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
            if (cat == null)
            {
                Assert.Ignore("Proto_GreyboxPersp2DCatalog asset missing");
                return;
            }

            Assert.IsTrue(cat.HudGameplayReady(out var missing), "missing ui fields: " + missing);
            Assert.IsNotNull(cat.uiHeartFull);
            Assert.IsNotNull(cat.uiIconRoll);
        }

        [Test]
        public void Persp2DUiTextures_ImportedAsSprites()
        {
            const string uiRoot = "Assets/HillNGun/Proto/Greybox/Art/Persp2D/UI";
            if (!AssetDatabase.IsValidFolder(uiRoot))
            {
                Assert.Ignore("Persp2D UI folder missing");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { uiRoot });
            Assert.Greater(guids.Length, 30);
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.NotNull(imp, path);
                Assert.AreEqual(TextureImporterType.Sprite, imp.textureType, path);
                Assert.AreEqual(SpriteImportMode.Single, imp.spriteImportMode, path);
            }
        }

        [Test]
        public void ViewMode_PerspToChibi_WestFullSceneAndPerspLayer()
        {
            EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
            Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
            var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            Assert.NotNull(builder);
            builder.Build();
            var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
            var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
            Assert.NotNull(controller);
            const float dt = 0.02f;
            controller.SetViewMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
            for (int i = 0; i < 8; i++)
                controller.Simulate(new Proto_GreyboxInput(), dt);
            var perspGo = GameObject.Find(Proto_GreyboxPerspective2DSkin.LayerRootName);
            Assert.IsNotNull(perspGo, "persp layer should bind in 2D 원근");

            controller.SetViewMode(Proto_GreyboxViewMode.View2DChibi, persist: false);
            for (int i = 0; i < 12; i++)
                controller.Simulate(new Proto_GreyboxInput(), dt);

            perspGo = GameObject.Find(Proto_GreyboxPerspective2DSkin.LayerRootName);
            Assert.IsTrue(perspGo == null || !perspGo.activeInHierarchy);
            var layer = GameObject.Find("Greybox_WestPixel_Layer");
            Assert.NotNull(layer);
            var full = layer.transform.Find("WP_FullScene");
            Assert.NotNull(full);
            var sr = full.GetComponent<SpriteRenderer>();
            Assert.IsTrue(sr.enabled);
            Assert.NotNull(sr.sprite);
        }

        static void AssertChibiCameraMatchesFresh(Camera cam, Proto_GreyboxPlayerController controller)
        {
            controller.RuntimeTuning.WestPixelSkin = true;
            Proto_GreyboxWestPixelSkinState.Enabled = true;
            controller.SetViewMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
            for (int i = 0; i < 4; i++)
                controller.Simulate(new Proto_GreyboxInput(), 0.02f);
            controller.SetViewMode(Proto_GreyboxViewMode.View2DChibi, persist: false);
            for (int i = 0; i < 16; i++)
                controller.Simulate(new Proto_GreyboxInput(), 0.02f);
            Assert.GreaterOrEqual(cam.orthographicSize, Proto_GreyboxLayout.CameraOrthographicSize * 0.98f);
            Assert.NotNull(GameObject.Find("Greybox_WestPixel_Layer")?.transform.Find("WP_FullScene"));
        }

        [Test]
        public void ViewMode_PerspToChibi_CameraMatchesFresh()
        {
            EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
            Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
            Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>()?.Build();
            var controller = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName)
                ?.GetComponent<Proto_GreyboxPlayerController>();
            Assert.NotNull(controller);
            AssertChibiCameraMatchesFresh(cam, controller);
        }

        [Test]
        public void ViewMode_ToggleCycleToChibi_CameraMatchesFresh()
        {
            Proto_GreyboxViewPresenterRegistry.RegisterFactory(() => new Proto_Greybox3DViewHost());
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
                var cam = Camera.main;
                cam.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
                Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
                Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>()?.Build();
                var controller = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName)
                    ?.GetComponent<Proto_GreyboxPlayerController>();
                Assert.NotNull(controller);
                controller.RuntimeTuning.WestPixelSkin = true;
                Proto_GreyboxWestPixelSkinState.Enabled = true;
                controller.SetViewMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
                controller.Simulate(new Proto_GreyboxInput(), 0.02f);
                controller.ToggleViewMode();
                controller.ToggleViewMode();
                controller.ToggleViewMode();
                Assert.AreEqual(Proto_GreyboxViewMode.View2DChibi, Proto_GreyboxViewModeState.Current);
                for (int i = 0; i < 24; i++)
                    controller.Simulate(new Proto_GreyboxInput(), 0.02f);
                var layer = GameObject.Find("Greybox_WestPixel_Layer");
                Assert.NotNull(layer);
                var full = layer.transform.Find("WP_FullScene");
                Assert.NotNull(full);
                Assert.IsTrue(full.GetComponent<SpriteRenderer>().enabled);
                Assert.GreaterOrEqual(cam.orthographicSize, Proto_GreyboxLayout.CameraOrthographicSize * 0.98f);
            }
        }

        [Test]
        public void ViewMode_PerspToPixel_WestStageBound()
        {
            EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
            var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            builder?.Build();
            var controller = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName)
                ?.GetComponent<Proto_GreyboxPlayerController>();
            Assert.NotNull(controller);
            controller.SetViewMode(Proto_GreyboxViewMode.View2DPerspective, persist: false);
            controller.Simulate(new Proto_GreyboxInput(), 0.02f);
            controller.SetViewMode(Proto_GreyboxViewMode.View2D, persist: false);
            for (int i = 0; i < 8; i++)
                controller.Simulate(new Proto_GreyboxInput(), 0.02f);
            Assert.NotNull(GameObject.Find("Greybox_WestPixel_Layer"));
        }
    }
}
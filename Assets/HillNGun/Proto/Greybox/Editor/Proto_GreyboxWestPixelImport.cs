using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HillNGun.Proto.Greybox.Editor
{
    public class Proto_GreyboxWestPixelImport : AssetPostprocessor
    {
        const string FolderToken = "Art/WestPixel";

        static readonly HashSet<string> SkipNames = new HashSet<string>
        {
            "preview.png", "target_look.png", "manifest.md", "compare.png"
        };

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains(FolderToken)) return;
            var file = Path.GetFileName(assetPath).ToLowerInvariant();
            if (SkipNames.Contains(file) || file.EndsWith("_sheet.png")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Proto_GreyboxWestPixelCatalog.ImportPpu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            bool inPersp = assetPath.Contains("perspective_v1");
            bool inFxV2_1 = assetPath.Contains("fx_v2_1");
            bool inFxV2 = assetPath.Contains("fx_v2") && !inFxV2_1;
            var pivot = ResolvePivot(file, inPersp, inFxV2, inFxV2_1);
            importer.spritePivot = pivot;
            bool custom = Mathf.Abs(pivot.x - 0.5f) > 0.001f || Mathf.Abs(pivot.y - 0.5f) > 0.001f;
            bool bgOrGround = !inPersp && (file.StartsWith("bg_") || file.StartsWith("ground") || file == "fence.png");
            if (inPersp && (file == "bg_sky_far.png" || file == "ground.png" || file.StartsWith("bld_")))
                bgOrGround = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)((custom && !bgOrGround) ? SpriteAlignment.Custom : SpriteAlignment.Center);
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
        }

        static Vector2 ResolvePivot(string file, bool inPersp, bool inFxV2, bool inFxV2_1)
        {
            if (inFxV2_1)
            {
                if (file.StartsWith("bullet_enemy_"))
                    return new Vector2(0.65f, 0.5f);
                return new Vector2(0.5f, 0.5f);
            }

            if (inFxV2)
                return ResolveFxV2Pivot(file);

            if (inPersp)
            {
                if (file == "bg_sky_far.png" || file == "ground.png" || file.StartsWith("bld_"))
                    return new Vector2(0.5f, 0.5f);
                if (file == "fence.png")
                    return new Vector2(0f, 0f);
                if (file == "fence_post.png" || file.StartsWith("crate_d"))
                    return new Vector2(0.5f, 0f);
            }

            if (file.StartsWith("bg_") || file.StartsWith("ground"))
                return new Vector2(0f, 1f);
            if (file == "fence.png")
                return new Vector2(0f, 0f);
            if (file == "fence_post.png" || file.StartsWith("crate_"))
                return new Vector2(0.5f, 0f);
            if (file.StartsWith("player_back_"))
                return new Vector2(0.5f, 0.028f);
            if (file.StartsWith("grunt_front_"))
                return new Vector2(0.5f, 0.041f);
            if (file.StartsWith("bullet_"))
                return new Vector2(0.75f, 0.5f);
            if (file.StartsWith("hp_"))
                return new Vector2(0f, 0.5f);
            return new Vector2(0.5f, 0.5f);
        }

        static Vector2 ResolveFxV2Pivot(string file)
        {
            if (file.StartsWith("roll_") && (file.Contains("_afterimage") || file.Contains("_00") || file.Contains("_01")
                    || file.Contains("_02") || file.Contains("_03") || file.Contains("_04") || file.Contains("_05")
                    || file.Contains("_06") || file.Contains("_07") || file.Contains("_08") || file.Contains("_09")
                    || file.Contains("_10")))
            {
                if (file.StartsWith("roll_dust_"))
                    return new Vector2(0.5f, 0.21f);
                return new Vector2(0.5f, 0.028f);
            }

            if (file.StartsWith("dizzy_light_body_") || file.StartsWith("dizzy_spin_in_body_")
                || file.StartsWith("dizzy_spin_body_") || file.StartsWith("dizzy_spin_out_body_"))
                return new Vector2(0.5f, 0.028f);
            if (file.StartsWith("dizzy_light_fx_") || file.StartsWith("dizzy_light_in_fx_")
                || file.StartsWith("dizzy_spin_in_fx_") || file.StartsWith("dizzy_spin_fx_")
                || file.StartsWith("dizzy_spin_out_fx_"))
                return new Vector2(0.5f, 0.021f);
            if (file == "bullet_ally_tracer.png")
                return new Vector2(0.92f, 0.5f);
            if (file == "bullet_ally.png")
                return new Vector2(0.5f, 0.5f);
            if (file == "bullet_enemy.png")
                return new Vector2(0.75f, 0.5f);
            if (file.StartsWith("reload_casings_"))
                return new Vector2(0.5f, 0.909f);
            if (file.StartsWith("explosion_"))
                return new Vector2(0.5f, 0.42f);
            if (file == "aim_band.png")
                return new Vector2(0.5f, 0f);
            return new Vector2(0.5f, 0.5f);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (path.Contains(FolderToken) && path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
                    if (path.Contains("fx_v2"))
                        Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
                    return;
                }
            }
        }
    }

    [InitializeOnLoad]
    public static class Proto_GreyboxWestPixelCatalogRebuild
    {
        const string CatalogAssetPath = "Assets/HillNGun/Proto/Greybox/Resources/Proto_GreyboxWestPixelCatalog.asset";

        static Proto_GreyboxWestPixelCatalogRebuild()
        {
            EditorApplication.delayCall += () =>
            {
                RebuildCatalog();
                Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();
            };
        }

        public static void RebuildCatalog()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var dir = Path.GetDirectoryName(CatalogAssetPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder("Assets/HillNGun/Proto/Greybox/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/HillNGun/Proto/Greybox"))
                    return;
                AssetDatabase.CreateFolder("Assets/HillNGun/Proto/Greybox", "Resources");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<Proto_GreyboxWestPixelCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<Proto_GreyboxWestPixelCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            catalog.bgFar = LoadSprite("bg_far.png");
            catalog.bgTown = LoadSprite("bg_town.png");
            catalog.ground = LoadSprite("ground.png");
            catalog.groundTile = LoadSprite("ground_tile.png");
            catalog.fence = LoadSprite("fence.png");
            catalog.fencePost = LoadSprite("fence_post.png");
            catalog.crateNear = LoadSprite("crate_near.png");
            catalog.crateFar = LoadSprite("crate_far.png");

            catalog.playerIdle = LoadSeries("player_back_idle_", 2);
            catalog.playerShoot = LoadSeries("player_back_shoot_", 2);
            catalog.playerRun = LoadSeries("player_back_run_", 4);
            catalog.playerRoll = LoadSeries("player_back_roll_", 4);
            catalog.playerHit = LoadSeries("player_back_hit_", 1);

            catalog.gruntIdle = LoadSeries("grunt_front_idle_", 2);
            catalog.gruntShoot = LoadSeries("grunt_front_shoot_", 2);
            catalog.gruntHit = LoadSeries("grunt_front_hit_", 1);
            catalog.gruntDown = LoadSeries("grunt_front_down_", 3);

            catalog.bulletEnemy = LoadSprite("bullet_enemy.png");
            catalog.bulletAlly = LoadSprite("bullet_ally.png");
            catalog.muzzleFlash = LoadSeries("muzzle_flash_", 2);
            catalog.hitSpark = LoadSeries("hit_spark_", 3);
            catalog.crosshair = LoadSprite("crosshair.png");

            catalog.btnFire = LoadSprite("btn_fire.png");
            catalog.btnReload = LoadSprite("btn_reload.png");
            catalog.btnGrenade = LoadSprite("btn_grenade.png");
            catalog.btnRoll = LoadSprite("btn_roll.png");
            catalog.btnPause = LoadSprite("btn_pause.png");
            catalog.joystickBase = LoadSprite("joystick_base.png");
            catalog.joystickKnob = LoadSprite("joystick_knob.png");
            catalog.hpFrame = LoadSprite("hp_frame.png");
            catalog.hpFill = LoadSprite("hp_fill.png");

            catalog.bgSkyFar = LoadPerspectiveSprite("bg_sky_far.png");
            catalog.bldLeft = LoadPerspectiveSprite("bld_left.png");
            catalog.bldRight = LoadPerspectiveSprite("bld_right.png");
            catalog.perspGround = LoadPerspectiveSprite("ground.png");
            catalog.perspFence = LoadPerspectiveSprite("fence.png");
            catalog.perspFencePost = LoadPerspectiveSprite("fence_post.png");
            catalog.crateD40 = LoadPerspectiveSprite("crate_d40.png");
            catalog.crateD46 = LoadPerspectiveSprite("crate_d46.png");
            catalog.crateD52 = LoadPerspectiveSprite("crate_d52.png");

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        static Sprite LoadSprite(string fileName)
        {
            var path = $"{Proto_GreyboxWestPixelCatalog.ArtFolder}/{fileName}";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite LoadPerspectiveSprite(string fileName)
        {
            var path =
                $"{Proto_GreyboxWestPixelCatalog.ArtFolder}/{Proto_GreyboxWestPixelCatalog.PerspectiveArtSubfolder}/{fileName}";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite[] LoadSeries(string prefix, int count)
        {
            var list = new List<Sprite>(count);
            for (int i = 0; i < count; i++)
            {
                var s = LoadSprite($"{prefix}{i}.png");
                if (s != null)
                    list.Add(s);
            }

            return list.ToArray();
        }
    }

    public static class Proto_GreyboxFxV2CatalogRebuild
    {
        public static void RebuildCatalog()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var folder = Proto_GreyboxFxV2Catalog.ArtFolder;
            if (!AssetDatabase.IsValidFolder("Assets/HillNGun/Proto/Greybox/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/HillNGun/Proto/Greybox"))
                    return;
                AssetDatabase.CreateFolder("Assets/HillNGun/Proto/Greybox", "Resources");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<Proto_GreyboxFxV2Catalog>(Proto_GreyboxFxV2Catalog.EditorAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<Proto_GreyboxFxV2Catalog>();
                AssetDatabase.CreateAsset(catalog, Proto_GreyboxFxV2Catalog.EditorAssetPath);
            }

            catalog.rollLeft = LoadSeries(folder, "roll_left_", 11);
            catalog.rollRight = LoadSeries(folder, "roll_right_", 11);
            catalog.rollForward = LoadSeries(folder, "roll_forward_", 11);
            catalog.rollBack = LoadSeries(folder, "roll_back_", 11);
            catalog.rollLeftAfterimage = LoadOne(folder, "roll_left_afterimage.png");
            catalog.rollRightAfterimage = LoadOne(folder, "roll_right_afterimage.png");
            catalog.rollForwardAfterimage = LoadOne(folder, "roll_forward_afterimage.png");
            catalog.rollBackAfterimage = LoadOne(folder, "roll_back_afterimage.png");
            catalog.rollDustTakeoff = LoadSeries(folder, "roll_dust_takeoff_", 4);
            catalog.rollDustLand = LoadSeries(folder, "roll_dust_land_", 4);

            catalog.btnFireCyl = new Sprite[6];
            for (int i = 0; i < 6; i++)
                catalog.btnFireCyl[i] = LoadOne(folder, $"btn_fire_cyl_{i}.png");
            catalog.btnFireRefill0to1 = LoadSeries(folder, "btn_fire_refill_0to1_", 5);
            catalog.btnFireRefill1to2 = LoadSeries(folder, "btn_fire_refill_1to2_", 5);
            catalog.btnFireRefill2to3 = LoadSeries(folder, "btn_fire_refill_2to3_", 5);
            catalog.btnFireRefill3to4 = LoadSeries(folder, "btn_fire_refill_3to4_", 5);
            catalog.btnFireRefill4to5 = LoadSeries(folder, "btn_fire_refill_4to5_", 5);
            catalog.btnFireEmptyBlink = LoadSeries(folder, "btn_fire_empty_blink_", 2);

            catalog.reloadCasings = new Sprite[5][];
            for (int k = 1; k <= 5; k++)
                catalog.reloadCasings[k - 1] = LoadSeries(folder, $"reload_casings_{k}_", 12);
            catalog.casing = LoadOne(folder, "casing.png");

            catalog.dizzyLightBody = LoadSeries(folder, "dizzy_light_body_", 4);
            catalog.dizzyLightFx = LoadSeries(folder, "dizzy_light_fx_", 4);
            catalog.dizzyLightInFx = LoadSeries(folder, "dizzy_light_in_fx_", 2);
            catalog.dizzySpinInBody = LoadSeries(folder, "dizzy_spin_in_body_", 2);
            catalog.dizzySpinInFx = LoadSeries(folder, "dizzy_spin_in_fx_", 2);
            catalog.dizzySpinBody = LoadSeries(folder, "dizzy_spin_body_", 12);
            catalog.dizzySpinFx = LoadSeries(folder, "dizzy_spin_fx_", 12);
            catalog.dizzySpinOutBody = LoadSeries(folder, "dizzy_spin_out_body_", 3);
            catalog.dizzySpinOutFx = LoadSeries(folder, "dizzy_spin_out_fx_", 3);
            catalog.dizzyStar = LoadOne(folder, "dizzy_star.png");
            catalog.dizzyBird = LoadOne(folder, "dizzy_bird.png");

            catalog.bulletAlly = LoadOne(folder, "bullet_ally.png");
            catalog.bulletAllyTracer = LoadOne(folder, "bullet_ally_tracer.png");
            catalog.bulletEnemy = LoadOne(folder, "bullet_enemy.png");
            catalog.muzzleFlash = LoadSeries(folder, "muzzle_flash_", 3);
            catalog.hitSparks = LoadSeries(folder, "hit_spark_", 3);
            catalog.dustGround = LoadSeries(folder, "dust_ground_", 3);
            catalog.dustCrate = LoadSeries(folder, "dust_crate_", 3);
            catalog.dynamiteSpin = LoadSeries(folder, "dynamite_spin_", 4);
            catalog.smokePuffs = new[]
            {
                LoadOne(folder, "smoke_puff_0.png"),
                LoadOne(folder, "smoke_puff_1.png"),
                LoadOne(folder, "smoke_puff_2.png")
            };
            catalog.bombShadow = LoadOne(folder, "bomb_shadow.png");
            catalog.explosion = LoadSeries(folder, "explosion_", 5);
            catalog.aimBand = LoadOne(folder, "aim_band.png");
            catalog.aimTip = LoadOne(folder, "aim_tip.png");
            catalog.aimBombDot = LoadOne(folder, "aim_bomb_dot.png");
            catalog.aimBombLand = LoadOne(folder, "aim_bomb_land.png");

            var folderV21 = Proto_GreyboxFxV2Catalog.ArtFolderV2_1;
            catalog.bulletEnemyFlipbook = new Sprite[3];
            for (int i = 0; i < 3; i++)
                catalog.bulletEnemyFlipbook[i] = LoadOne(folderV21, $"bullet_enemy_{i}.png");
            if (catalog.bulletEnemyFlipbook[0] != null)
                catalog.bulletEnemy = catalog.bulletEnemyFlipbook[0];
            catalog.btnGrenadeGaugeBg = LoadOne(folderV21, "btn_grenade_gauge_bg.png");
            catalog.btnGrenadeGaugeFill = LoadOne(folderV21, "btn_grenade_gauge_fill.png");
            catalog.btnGrenadeGaugeFull = new[]
            {
                LoadOne(folderV21, "btn_grenade_gauge_full_0.png"),
                LoadOne(folderV21, "btn_grenade_gauge_full_1.png")
            };
            catalog.btnGrenadeGaugeBaked = new[]
            {
                LoadOne(folderV21, "btn_grenade_gauge_000.png"),
                LoadOne(folderV21, "btn_grenade_gauge_025.png"),
                LoadOne(folderV21, "btn_grenade_gauge_050.png"),
                LoadOne(folderV21, "btn_grenade_gauge_075.png"),
                LoadOne(folderV21, "btn_grenade_gauge_100.png")
            };

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        static Sprite LoadOne(string folder, string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{fileName}");
        }

        static Sprite[] LoadSeries(string folder, string prefix, int count)
        {
            var list = new List<Sprite>(count);
            for (int i = 0; i < count; i++)
            {
                var s = LoadOne(folder, $"{prefix}{i:D2}.png");
                if (s != null)
                    list.Add(s);
            }

            return list.ToArray();
        }
    }
}
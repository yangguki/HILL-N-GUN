using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HillNGun.Proto.Greybox.Editor
{
    /// <summary>Slice J: import settings + catalog for the self-made flat 2D chibi sprites (Art/Chibi).</summary>
    public class Proto_GreyboxChibiImport : AssetPostprocessor
    {
        const string FolderToken = "Greybox/Art/Chibi";

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains(FolderToken)) return;
            var file = Path.GetFileName(assetPath).ToLowerInvariant();
            if (file.StartsWith("_") || file.Contains("preview")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Proto_GreyboxWestPixelCatalog.ImportPpu;
            importer.filterMode = FilterMode.Bilinear; // vector-style art, not pixel art
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            if (file == "bg_full_scene.png" || file == "bg_far.png" || file == "ground.png" || file == "bg_town.png")
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var pivot = ResolvePivot(file);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool custom = Mathf.Abs(pivot.x - 0.5f) > 0.001f || Mathf.Abs(pivot.y - 0.5f) > 0.001f;
            settings.spriteAlignment = (int)(custom ? SpriteAlignment.Custom : SpriteAlignment.Center);
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.spritePivot = pivot;
        }

        /// <summary>Same pivots as the pixel set so the shared skin layout code places both identically.</summary>
        public static Vector2 ResolvePivot(string file)
        {
            if (file == "fence.png") return new Vector2(0f, 0f);
            if (file == "fence_post.png" || file.StartsWith("crate_")) return new Vector2(0.5f, 0f);
            if (file.StartsWith("player_back_")) return new Vector2(0.5f, 0.028f);
            if (file.StartsWith("grunt_front_")) return new Vector2(0.5f, 0.041f);
            if (file.StartsWith("bullet_enemy")) return new Vector2(0.75f, 0.5f);
            if (file.StartsWith("bullet_")) return new Vector2(0.75f, 0.5f);
            if (file.StartsWith("hp_frame") || file.StartsWith("hp_fill")) return new Vector2(0f, 0.5f);
            if (file.StartsWith("bg_far") || file.StartsWith("bg_town") || file.StartsWith("ground"))
                return new Vector2(0f, 1f);
            return new Vector2(0.5f, 0.5f);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (path.Contains(FolderToken) && path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    EditorApplication.delayCall += Proto_GreyboxChibiCatalogRebuild.RebuildCatalog;
                    return;
                }
            }
        }
    }

    [InitializeOnLoad]
    public static class Proto_GreyboxChibiCatalogRebuild
    {
        const string FontPath = "Assets/HillNGun/Proto/Greybox3D/Art/Fonts/LilitaOne-Regular.ttf";

        static Proto_GreyboxChibiCatalogRebuild()
        {
            EditorApplication.delayCall += RebuildCatalog;
        }

        [MenuItem("HillNGun/Greybox/Rebuild 2D Chibi Catalog")]
        public static void RebuildCatalog()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!AssetDatabase.IsValidFolder(Proto_GreyboxWestPixelCatalog.ChibiArtFolder)) return;
            if (!AssetDatabase.IsValidFolder("Assets/HillNGun/Proto/Greybox/Resources"))
                AssetDatabase.CreateFolder("Assets/HillNGun/Proto/Greybox", "Resources");

            var path = Proto_GreyboxWestPixelCatalog.ChibiEditorCatalogAssetPath;
            var c = AssetDatabase.LoadAssetAtPath<Proto_GreyboxWestPixelCatalog>(path);
            if (c == null)
            {
                c = ScriptableObject.CreateInstance<Proto_GreyboxWestPixelCatalog>();
                AssetDatabase.CreateAsset(c, path);
            }

            c.isChibi = true;
            c.bgFullScene = L("bg_full_scene.png");
            c.bgFar = L("bg_far.png");
            c.bgTown = L("bg_town.png");
            c.ground = L("ground.png");
            c.groundTile = null;
            c.fence = L("fence.png");
            c.fencePost = L("fence_post.png");
            c.crateNear = L("crate_near.png");
            c.crateFar = L("crate_far.png");

            // Perspective: only fence + crates (the full scene replaces sky / ground / buildings).
            c.bgSkyFar = null;
            c.bldLeft = null;
            c.bldRight = null;
            c.perspGround = null;
            c.perspFence = c.fence;
            c.perspFencePost = c.fencePost;
            c.crateD40 = L("crate_d40.png");
            c.crateD46 = L("crate_d46.png");
            c.crateD52 = L("crate_d52.png");

            c.playerIdle = Series("player_back_idle_", 2, false);
            c.playerShoot = Series("player_back_shoot_", 2, false);
            c.playerRun = Series("player_back_run_", 4, false);
            c.playerRoll = Series("player_back_roll_", 11, true);
            c.playerHit = Series("player_back_hit_", 1, false);
            c.gruntIdle = Series("grunt_front_idle_", 2, false);
            c.gruntShoot = Series("grunt_front_shoot_", 2, false);
            c.gruntHit = Series("grunt_front_hit_", 1, false);
            c.gruntDown = Series("grunt_front_down_", 3, false);

            c.bulletEnemy = L("bullet_enemy.png");
            c.bulletAlly = L("bullet_ally.png");
            c.muzzleFlash = Series("muzzle_flash_", 2, false);
            c.hitSpark = Series("hit_spark_", 3, false);
            c.crosshair = L("crosshair.png");

            c.btnFire = L("btn_fire.png");
            c.btnReload = L("btn_reload.png");
            c.btnGrenade = L("btn_grenade.png");
            c.btnRoll = L("btn_roll.png");
            c.btnPause = L("btn_pause.png");
            c.joystickBase = L("joystick_base.png");
            c.joystickKnob = L("joystick_knob.png");
            c.hpFrame = L("hp_frame.png");
            c.hpFill = L("hp_fill.png");

            c.scorePlate = L("score_plate.png");
            c.stagePlate = L("stage_plate.png");
            c.hpPlate = L("hp_plate.png");
            c.heartFull = L("heart_full.png");
            c.heartEmpty = L("heart_empty.png");
            c.uiFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
        }

        static Sprite L(string file) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{Proto_GreyboxWestPixelCatalog.ChibiArtFolder}/{file}");

        static Sprite[] Series(string prefix, int count, bool twoDigits)
        {
            var list = new List<Sprite>(count);
            for (int i = 0; i < count; i++)
            {
                var s = L(twoDigits ? $"{prefix}{i:D2}.png" : $"{prefix}{i}.png");
                if (s != null) list.Add(s);
            }
            return list.ToArray();
        }
    }
}

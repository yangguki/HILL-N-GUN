using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace HillNGun.Proto.Greybox.Editor
{
    /// <summary>Slice K: import + catalog + sprite atlases for Persp2D art.</summary>
    public class Proto_GreyboxPersp2DImport : AssetPostprocessor
    {
        const string FolderToken = "Greybox/Art/Persp2D";
        static readonly Vector2 CharPivot = new(0.5f, 0.21875f);

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains(FolderToken)) return;
            var file = Path.GetFileName(assetPath).ToLowerInvariant();
            if (file.StartsWith("_") || file.Contains("preview")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            bool isUi = assetPath.Contains("/UI/", StringComparison.OrdinalIgnoreCase);
            bool isChar = assetPath.Contains("/Characters/") && file.EndsWith(".png");
            bool isEnvBig = file.Contains("road") || file.Contains("bld_") || file.Contains("sky_");
            importer.mipmapEnabled = false;
            importer.maxTextureSize = isUi ? 1024 : isChar ? 512 : isEnvBig ? 4096 : 2048;
            if (isChar)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var androidChar = importer.GetPlatformTextureSettings("Android");
                androidChar.overridden = true;
                androidChar.maxTextureSize = 512;
                androidChar.format = TextureImporterFormat.RGBA32;
                androidChar.compressionQuality = 100;
                importer.SetPlatformTextureSettings(androidChar);
            }
            else
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                var android = importer.GetPlatformTextureSettings("Android");
                android.overridden = true;
                android.maxTextureSize = importer.maxTextureSize;
                android.format = isUi ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
                android.compressionQuality = 50;
                importer.SetPlatformTextureSettings(android);
            }
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            standalone.overridden = true;
            standalone.maxTextureSize = importer.maxTextureSize;
            importer.SetPlatformTextureSettings(standalone);

            importer.spritePixelsPerUnit = isChar
                ? Proto_GreyboxPersp2DCatalog.ImportPpu
                : Proto_GreyboxWestPixelCatalog.ImportPpu;

            var pivot = ResolvePivot(file, assetPath);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.spritePivot = pivot;
        }

        static Vector2 ResolvePivot(string file, string path)
        {
            if (path.Contains("/Characters/") && file.StartsWith("hng_char_"))
                return CharPivot;
            if (path.Contains(FolderToken) && path.Contains("/Environment/"))
                return Proto_GreyboxPersp2DEnvPivotResolver.Resolve(file);
            return new Vector2(0.5f, 0.5f);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (Proto_GreyboxPersp2DCatalogRebuild.IsBusy) return;
            foreach (var path in importedAssets)
            {
                if (path.Contains(FolderToken) && (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
                {
                    if (path.Contains("hng_env_layout_2d.json"))
                        Proto_GreyboxPersp2DEnvPivotResolver.InvalidateCache();
                    Proto_GreyboxPersp2DCatalogRebuild.ScheduleRebuildCatalogAndAtlases();
                    return;
                }
            }
        }
    }

    public static class Proto_GreyboxPersp2DCatalogRebuild
    {
        static bool s_busy;
        static bool s_pending;

        internal static bool IsBusy => s_busy;

        static readonly string[] HeroDirs = { "S", "SE", "E", "NE", "N" };
        static readonly string[] BanditDirs = { "S", "E", "N" };
        static readonly string[] HeroAnims = { "idle", "run", "shoot", "roll", "hit", "down" };
        static readonly string[] BanditAnims = { "idle", "run", "shoot", "hit", "down" };

        [InitializeOnLoadMethod]
        static void Init() => ScheduleRebuildCatalogAndAtlases();

        public static void ScheduleRebuildCatalogAndAtlases()
        {
            if (s_busy || s_pending) return;
            s_pending = true;
            EditorApplication.delayCall += DelayedRebuildCatalogAndAtlases;
        }

        static void DelayedRebuildCatalogAndAtlases()
        {
            s_pending = false;
            if (s_busy) return;
            RebuildCatalogAndAtlases();
        }

        [MenuItem("HillNGun/Greybox/Rebuild 2D Persp2D Catalog + Atlases")]
        public static void RebuildAll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!AssetDatabase.IsValidFolder(Proto_GreyboxPersp2DCatalog.ArtFolder)) return;
            s_busy = true;
            try
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    Proto_GreyboxPersp2DEnvPivotResolver.InvalidateCache();
                    ForceReimportCharTextures();
                    ForceReimportEnvTextures();
                    ForceReimportUiTextures();
                    ForceReimportFxTextures();
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                RebuildCatalogAndAtlases();
            }
            finally
            {
                s_busy = false;
            }
        }

        public static void RebuildCatalogAndAtlases()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!AssetDatabase.IsValidFolder(Proto_GreyboxPersp2DCatalog.ArtFolder)) return;
            Proto_GreyboxPersp2DEnvPivotResolver.InvalidateCache();
            EnsureAtlases();
            RebuildCatalog();
        }

        public static void ForceReimportUiTextures()
        {
            var uiRoot = $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/UI";
            if (!AssetDatabase.IsValidFolder(uiRoot)) return;
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { uiRoot });
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            }
        }

        public static void ForceReimportFxTextures()
        {
            var fxRoot = $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/FX";
            if (!AssetDatabase.IsValidFolder(fxRoot)) return;
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { fxRoot });
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            }
        }

        public static void ForceReimportCharTextures()
        {
            foreach (var who in new[] { "hero", "bandit" })
            {
                var folder = $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/{who}";
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var g in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        public static void ForceReimportEnvTextures()
        {
            var envRoot = $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Environment";
            if (!AssetDatabase.IsValidFolder(envRoot)) return;
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { envRoot });
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            }
        }

        static void EnsureAtlases()
        {
            EnsureAtlas("Persp2D_CharHeroAtlas", $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/hero");
            EnsureAtlas("Persp2D_CharBanditAtlas", $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/bandit");
        }

        static void EnsureAtlas(string name, string folder)
        {
            var path = $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Atlases/{name}.spriteatlasv2";
            if (!AssetDatabase.IsValidFolder($"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Atlases"))
                AssetDatabase.CreateFolder(Proto_GreyboxPersp2DCatalog.ArtFolder, "Atlases");

            if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path) != null)
                AssetDatabase.DeleteAsset(path);
            else if (File.Exists(Path.GetFullPath(path)))
                AssetDatabase.DeleteAsset(path);

            var objs = CollectAtlasPackables(folder);
            var saa = new SpriteAtlasAsset();
            var pack = saa.GetPackingSettings();
            pack.enableRotation = false;
            pack.enableTightPacking = true;
            pack.padding = 4;
            saa.SetPackingSettings(pack);
            var tex = saa.GetTextureSettings();
            tex.generateMipMaps = false;
            saa.SetTextureSettings(tex);
            var plat = saa.GetPlatformSettings("Android");
            plat.overridden = true;
            plat.format = TextureImporterFormat.ASTC_6x6;
            plat.maxTextureSize = 4096;
            plat.compressionQuality = 50;
            saa.SetPlatformSettings(plat);
            saa.SetIncludeInBuild(true);
            if (objs.Count > 0)
                saa.Add(objs.ToArray());
            SpriteAtlasAsset.Save(saa, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas != null)
            {
                var packSettings = atlas.GetPackingSettings();
                packSettings.enableRotation = false;
                packSettings.enableTightPacking = true;
                packSettings.padding = 4;
                atlas.SetPackingSettings(packSettings);
                var texSettings = atlas.GetTextureSettings();
                texSettings.generateMipMaps = false;
                atlas.SetTextureSettings(texSettings);
                var androidPlat = atlas.GetPlatformSettings("Android");
                androidPlat.overridden = true;
                androidPlat.format = TextureImporterFormat.ASTC_6x6;
                androidPlat.maxTextureSize = 4096;
                androidPlat.compressionQuality = 50;
                atlas.SetPlatformSettings(androidPlat);
                atlas.SetIncludeInBuild(true);
                EditorUtility.SetDirty(atlas);
                SpriteAtlasUtility.PackAtlases(new[] { atlas }, BuildTarget.Android);
            }
            else
                SpriteAtlasUtility.PackAllAtlases(BuildTarget.Android);
            AssetDatabase.SaveAssets();
        }

        static List<UnityEngine.Object> CollectAtlasPackables(string folder)
        {
            var objs = new List<UnityEngine.Object>();
            if (!AssetDatabase.IsValidFolder(folder)) return objs;
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.Contains("/sheets/", StringComparison.OrdinalIgnoreCase)) continue;
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (sp != null) objs.Add(sp);
            }

            if (objs.Count == 0)
            {
                guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var g in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (p.Contains("/sheets/", StringComparison.OrdinalIgnoreCase)) continue;
                    objs.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p));
                }
            }

            return objs;
        }

        public static void RebuildCatalog()
        {
            if (!AssetDatabase.IsValidFolder("Assets/HillNGun/Proto/Greybox/Resources"))
                AssetDatabase.CreateFolder("Assets/HillNGun/Proto/Greybox", "Resources");

            var path = Proto_GreyboxPersp2DCatalog.EditorCatalogAssetPath;
            var c = AssetDatabase.LoadAssetAtPath<Proto_GreyboxPersp2DCatalog>(path);
            if (c == null)
            {
                c = ScriptableObject.CreateInstance<Proto_GreyboxPersp2DCatalog>();
                AssetDatabase.CreateAsset(c, path);
            }

            c.heroAnimJson = LoadText("Characters/hng_char_hero_anim.json");
            c.banditAnimJson = LoadText("Characters/hng_char_bandit_anim.json");
            c.charShadow = L("Characters/hng_char_shadow_ellipse.png");
            c.sky = L("Environment/Sky/hng_env_sky_sunset_2d.png");
            c.road = L("Environment/Road/hng_env_road_perspective.png");
            c.propShadow = L("Environment/Props/hng_env_prop_shadow_ellipse.png");

            var heroJson = c.heroAnimJson != null ? c.heroAnimJson.text : null;
            var banditJson = c.banditAnimJson != null ? c.banditAnimJson.text : null;
            c.hero = BuildChar("hero", HeroDirs, HeroAnims, heroJson);
            c.bandit = BuildChar("bandit", BanditDirs, BanditAnims, banditJson);
            c.EnsureAnimJsonWarmed();
            c.sceneBuildings = new[]
            {
                Bld("sheriff_R"),
                Bld("store_L"),
                Bld("hotel_R"),
                Bld("saloon_L"),
            };
            c.props = new[]
            {
                L("Environment/Props/hng_env_prop_crate.png"),
                L("Environment/Props/hng_env_prop_barrel.png"),
                L("Environment/Props/hng_env_prop_crate_small.png"),
            };

            c.fxTracerEnemyHead = Fx("hng_fx_tracer_enemy_normal.png");
            c.fxTracerEnemyTrail = Fx("hng_fx_tracer_enemy_trail.png");
            c.fxTracerPlayerHead = Fx("hng_fx_tracer_player_normal.png");
            c.fxTracerPlayerTrail = Fx("hng_fx_tracer_player_trail.png");
            c.fxMuzzleFlash = new[]
            {
                Fx("hng_fx_muzzle_flash_f00.png"), Fx("hng_fx_muzzle_flash_f01.png"),
                Fx("hng_fx_muzzle_flash_f02.png"), Fx("hng_fx_muzzle_flash_f03.png")
            };
            c.fxGroundImpact = new[]
            {
                Fx("hng_fx_ground_impact_f00.png"), Fx("hng_fx_ground_impact_f01.png"),
                Fx("hng_fx_ground_impact_f02.png"), Fx("hng_fx_ground_impact_f03.png"),
                Fx("hng_fx_ground_impact_f04.png"), Fx("hng_fx_ground_impact_f05.png")
            };

            c.uiJoystickBase = Ui("hng_ui_joystick_base_normal.png");
            c.uiJoystickKnob = Ui("hng_ui_joystick_knob_normal.png");
            c.uiJoystickBasePressed = Ui("hng_ui_joystick_base_pressed.png") ?? c.uiJoystickBase;
            c.uiJoystickKnobPressed = Ui("hng_ui_joystick_knob_pressed.png") ?? c.uiJoystickKnob;
            c.uiBtnAttack = Ui("hng_ui_btn_attack_normal.png");
            c.uiBtnReload = Ui("hng_ui_btn_reload_normal.png");
            c.uiBtnSkill = Ui("hng_ui_btn_skill_normal.png");
            c.uiBtnPause = Ui("hng_ui_btn_pause_normal.png");
            c.uiBtnRoll = Ui("hng_ui_btn_roll_normal.png") ?? Ui("hng_ui_btn_aim_normal.png");
            c.uiTextScore = Ui("hng_ui_text_score_normal.png");
            c.uiTextStage = Ui("hng_ui_text_stage_normal.png");
            c.uiHeartFull = Ui("hng_ui_hp_heart_full.png");
            c.uiHeartEmpty = Ui("hng_ui_hp_heart_empty.png");
            c.uiCrosshair = Ui("hng_ui_crosshair_normal.png");
            c.uiBtnCooldownOverlay = Ui("hng_ui_btn_cooldown_overlay.png");
            c.uiBtnAttackPressed = Ui("hng_ui_btn_attack_pressed.png");
            c.uiBtnRollPressed = Ui("hng_ui_btn_roll_pressed.png") ?? Ui("hng_ui_btn_roll_normal.png");
            c.uiBtnReloadPressed = Ui("hng_ui_btn_reload_pressed.png");
            c.uiBtnSkillPressed = Ui("hng_ui_btn_skill_pressed.png");
            c.uiBtnPausePressed = Ui("hng_ui_btn_pause_pressed.png");
            c.uiIconAttack = Ui("hng_ui_icon_attack_normal.png");
            c.uiIconRoll = Ui("hng_ui_icon_roll_normal.png");
            c.uiIconReload = Ui("hng_ui_icon_reload_normal.png");
            c.uiIconSkill = Ui("hng_ui_icon_skill_normal.png");
            c.uiIconPause = Ui("hng_ui_icon_pause_normal.png");
            c.uiLabelAttack = Ui("hng_ui_label_attack_normal.png");
            c.uiLabelRoll = Ui("hng_ui_label_roll_normal.png");
            c.uiLabelReload = Ui("hng_ui_label_reload_normal.png");
            c.uiLabelSkill = Ui("hng_ui_label_skill_normal.png");

            Proto_GreyboxPersp2DEnvLayoutBake.BakeIntoCatalog(c);

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
        }

        static Proto_GreyboxPersp2DCatalog.BuildingSet Bld(string id) => new()
        {
            id = id,
            back = L($"Environment/Buildings/hng_env_bld_{id}_back.png"),
            front = L($"Environment/Buildings/hng_env_bld_{id}_front.png"),
            sign = L($"Environment/Buildings/hng_env_sign_{id.Split('_')[0]}.png"),
        };

        static Sprite Ui(string file)
        {
            var persp = AssetDatabase.LoadAssetAtPath<Sprite>($"{Proto_GreyboxPersp2DCatalog.ArtFolder}/UI/{file}");
            if (persp != null) return persp;
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/HillNGun/Proto/Greybox3D/Art/UI/{file}");
        }

        static Sprite Fx(string file)
        {
            var persp = AssetDatabase.LoadAssetAtPath<Sprite>($"{Proto_GreyboxPersp2DCatalog.ArtFolder}/FX/{file}");
            if (persp != null) return persp;
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/HillNGun/Proto/Greybox3D/Art/FX/{file}");
        }

        static Proto_GreyboxPersp2DCatalog.CharacterAnims BuildChar(string who, string[] dirs, string[] anims,
            string animJson)
        {
            var result = new Proto_GreyboxPersp2DCatalog.CharacterAnims();
            foreach (var anim in anims)
            {
                int fcount = Proto_GreyboxPersp2DAnimJson.AnimFrameCount(animJson, anim);
                if (fcount <= 0)
                {
                    fcount = anim switch
                    {
                        "idle" => 4,
                        "run" => 6,
                        "shoot" => 3,
                        "roll" => 13,
                        "hit" => 2,
                        "down" => 4,
                        _ => 1
                    };
                }

                var list = new List<Proto_GreyboxPersp2DCatalog.DirFrames>();
                foreach (var dir in dirs)
                {
                    var frames = new List<Sprite>();
                    for (int i = 0; i < fcount; i++)
                    {
                        var s = L($"Characters/{who}/{anim}/hng_char_{who}_{anim}_{dir}_{i:D2}.png");
                        if (s != null) frames.Add(s);
                    }
                    list.Add(new Proto_GreyboxPersp2DCatalog.DirFrames { dir = dir, frames = frames.ToArray() });
                }
                switch (anim)
                {
                    case "idle": result.idle = list.ToArray(); break;
                    case "run": result.run = list.ToArray(); break;
                    case "shoot": result.shoot = list.ToArray(); break;
                    case "roll": result.roll = list.ToArray(); break;
                    case "hit": result.hit = list.ToArray(); break;
                    case "down": result.down = list.ToArray(); break;
                }
            }
            return result;
        }

        static Sprite L(string rel) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{Proto_GreyboxPersp2DCatalog.ArtFolder}/{rel}");

        static TextAsset LoadText(string rel) =>
            AssetDatabase.LoadAssetAtPath<TextAsset>($"{Proto_GreyboxPersp2DCatalog.ArtFolder}/{rel}");
    }
}
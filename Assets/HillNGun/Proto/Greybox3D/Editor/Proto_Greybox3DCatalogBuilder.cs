using System.IO;
using UnityEditor;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D.Editor
{
    public static class Proto_Greybox3DCatalogBuilder
    {
        const string CatalogAsset = "Assets/HillNGun/Proto/Greybox3D/Resources/HillNGunGreybox3D/Catalog.asset";
        const string CowboyPath =
            "Assets/HillNGun/Proto/Greybox3D/ThirdParty/Quaternius/UltimateAnimatedCharacterPack/Cowboy_Male.fbx";
        const string MiniDir =
            "Assets/HillNGun/Proto/Greybox3D/ThirdParty/KenneyMiniCharacters/Models/FBX format/";
        static string Mini(string file) => MiniDir + file;

        [MenuItem("HillNGun/Proto/Greybox3D/Rebuild Catalog")]
        public static void Build()
        {
            ConfigureTextures();
            ConfigureDesignerArt();
            ConfigureCowboyImport();
            ConfigureMiniImport(Mini("character-male-g.fbx"));
            ConfigureMiniImport(Mini("character-raider.fbx"));
            ConfigureMiniImport(Mini("weapon-shooter.fbx"), animations: false);
            AssetDatabase.Refresh();

            var dir = Path.GetDirectoryName(CatalogAsset);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var catalog = AssetDatabase.LoadAssetAtPath<Proto_Greybox3DCatalog>(CatalogAsset);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<Proto_Greybox3DCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAsset);
            }

            catalog.CowboyMale = LoadGo(CowboyPath);
            AssignCowboyClips(catalog);
            catalog.MiniHero = LoadGo(Mini("character-male-g.fbx"));
            catalog.MiniGrunt = LoadGo(Mini("character-raider.fbx"));
            catalog.MiniGun = LoadGo(Mini("weapon-shooter.fbx"));
            catalog.MiniColormap = LoadTex(Mini("Textures/colormap.png"));
            catalog.MiniColormapHero = LoadTex(Mini("Textures/colormap_hero.png"));
            catalog.MiniColormapGrunt = LoadTex(Mini("Textures/colormap_grunt.png"));
            catalog.MiniColormapGruntB = LoadTex(Mini("Textures/colormap_grunt_b.png"));
            catalog.MiniColormapGruntC = LoadTex(Mini("Textures/colormap_grunt_c.png"));
            AssignMiniClips(catalog);
            AssignDesignerArt(catalog);

            catalog.CactusShort = LoadGo(Nature("cactus_short.fbx"));
            catalog.CactusTall = LoadGo(Nature("cactus_tall.fbx"));
            catalog.Grass = LoadGo(Nature("grass.fbx"));
            catalog.GrassLarge = LoadGo(Nature("grass_large.fbx"));
            catalog.GrassLeafs = LoadGo(Nature("grass_leafs.fbx"));
            catalog.RockSmallA = LoadGo(Nature("rock_smallA.fbx"));
            catalog.RockSmallB = LoadGo(Nature("rock_smallB.fbx"));
            catalog.RockSmallC = LoadGo(Nature("rock_smallC.fbx"));
            catalog.RockTallA = LoadGo(Nature("rock_tallA.fbx"));
            catalog.RockTallB = LoadGo(Nature("rock_tallB.fbx"));
            catalog.RockLargeA = LoadGo(Nature("rock_largeA.fbx"));
            catalog.CliffBlock = LoadGo(Nature("cliff_block_rock.fbx"));
            catalog.CliffLarge = LoadGo(Nature("cliff_large_rock.fbx"));
            catalog.CliffTop = LoadGo(Nature("cliff_top_rock.fbx"));

            catalog.Barrel = LoadGo(Survival("barrel.fbx"));
            catalog.Crate = LoadGo(Survival("box.fbx"));
            catalog.CrateLarge = LoadGo(Survival("box-large.fbx"));
            catalog.Fence = LoadGo(Survival("fence.fbx"));
            catalog.FenceDoorway = LoadGo(Survival("fence-doorway.fbx"));
            catalog.PatchGrass = LoadGo(Survival("patch-grass.fbx"));
            catalog.RockSandA = LoadGo(Survival("rock-sand-a.fbx"));
            catalog.RockSandB = LoadGo(Survival("rock-sand-b.fbx"));

            catalog.WesternObjects = LoadGo(
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/LowPolyWesternObjects.fbx");
            catalog.WoodTex = LoadTex(
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/Wood-Texture.png");
            catalog.DirtTex = LoadTex(
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/DirtSandStone-Texture.png");
            catalog.FabricTex = LoadTex(
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/Fabric_Texture.png");
            catalog.SkyboxDay = LoadTex(
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/Kenney/Skyboxes/skybox-day.png");

            catalog.CelShader = Shader.Find("HillNGun/Proto/Greybox3D/Cel");
            catalog.GlowShader = Shader.Find("HillNGun/Proto/Greybox3D/Glow");
            catalog.BlobShader = Shader.Find("HillNGun/Proto/Greybox3D/BlobShadow");
            catalog.SkyShader = Shader.Find("HillNGun/Proto/Greybox3D/Sky");
            catalog.VignetteShader = Shader.Find("HillNGun/Proto/Greybox3D/Vignette");

            var prefab = EnsureCowboyPrefab(catalog.CowboyMale);
            if (prefab != null)
                catalog.CowboyMale = prefab;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Proto_Greybox3DCatalog.Inject(catalog);
            Debug.Log(
                $"Greybox3D catalog rebuilt. miniHero={(catalog.MiniHero != null)} miniGrunt={(catalog.MiniGrunt != null)} miniIdle={(catalog.MiniIdle != null)} miniWalk={(catalog.MiniWalk != null)} miniShoot={(catalog.MiniShoot != null)} miniDie={(catalog.MiniDie != null)} cowboy={(catalog.CowboyMale != null)} cactus={(catalog.CactusTall != null)} sky={(catalog.SkyboxDay != null)} celShader={(catalog.CelShader != null)}");
        }

        public static void BuildAndExit()
        {
            Build();
            EditorApplication.Exit(0);
        }

        static string Nature(string file) =>
            "Assets/HillNGun/Proto/Greybox3D/ThirdParty/Kenney/NatureKit/" + file;

        static string Survival(string file) =>
            "Assets/HillNGun/Proto/Greybox3D/ThirdParty/Kenney/SurvivalKit/" + file;

        static GameObject LoadGo(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        static Texture2D LoadTex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        static void AssignCowboyClips(Proto_Greybox3DCatalog catalog)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(CowboyPath))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__"))
                    continue;
                string n = clip.name.ToLowerInvariant();
                if (n.Contains("idle") && catalog.CowboyIdle == null) catalog.CowboyIdle = clip;
                else if (n.Contains("walk") && !n.Contains("carry") && catalog.CowboyWalk == null)
                    catalog.CowboyWalk = clip;
                else if (n.Contains("shoot") && catalog.CowboyShoot == null) catalog.CowboyShoot = clip;
                else if ((n.Contains("recievehit") || n.Contains("receivehit") || n.Contains("hit")) &&
                         catalog.CowboyHit == null)
                    catalog.CowboyHit = clip;
                else if (n.Contains("punch") && catalog.CowboyPunch == null) catalog.CowboyPunch = clip;
            }
        }

        static void ConfigureTextures()
        {
            ConfigureTex("Assets/HillNGun/Proto/Greybox3D/ThirdParty/Kenney/Skyboxes/skybox-day.png",
                tex =>
                {
                    tex.textureType = TextureImporterType.Default;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.sRGBTexture = true;
                    tex.mipmapEnabled = false;
                    tex.maxTextureSize = 2048;
                });
            string[] texes =
            {
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/Wood-Texture.png",
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/DirtSandStone-Texture.png",
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/Fabric_Texture.png",
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/Kenney/SurvivalKit/Textures/colormap.png"
            };
            foreach (var p in texes)
            {
                ConfigureTex(p, tex =>
                {
                    tex.textureType = TextureImporterType.Default;
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.sRGBTexture = true;
                    tex.maxTextureSize = 512;
                });
            }

            string[] miniPalettes =
            {
                Mini("Textures/colormap.png"),
                Mini("Textures/colormap_hero.png"),
                Mini("Textures/colormap_grunt.png"),
                Mini("Textures/colormap_grunt_b.png"),
                Mini("Textures/colormap_grunt_c.png")
            };
            foreach (var p in miniPalettes)
            {
                ConfigureTex(p, tex =>
                {
                    tex.textureType = TextureImporterType.Default;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Point;
                    tex.mipmapEnabled = false;
                    tex.sRGBTexture = true;
                    tex.maxTextureSize = 512;
                });
            }
        }

        static void ConfigureTex(string path, System.Action<TextureImporter> cfg)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            cfg(imp);
            imp.SaveAndReimport();
        }

        const string CowboyPrefabPath =
            "Assets/HillNGun/Proto/Greybox3D/Resources/HillNGunGreybox3D/CowboyMale.prefab";

        static GameObject EnsureCowboyPrefab(GameObject source)
        {
            if (source == null) return null;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CowboyPrefabPath);
            if (existing != null) return existing;
            var tmp = Object.Instantiate(source);
            tmp.name = "CowboyMale";
            var prefab = PrefabUtility.SaveAsPrefabAsset(tmp, CowboyPrefabPath);
            Object.DestroyImmediate(tmp);
            return prefab != null ? prefab : AssetDatabase.LoadAssetAtPath<GameObject>(CowboyPrefabPath);
        }

        static void ConfigureCowboyImport()
        {
            var imp = AssetImporter.GetAtPath(CowboyPath) as ModelImporter;
            if (imp == null) return;
            bool dirty = imp.animationType != ModelImporterAnimationType.Generic
                         || imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                         || !imp.importAnimation;
            if (!dirty) return;
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
            imp.SaveAndReimport();
        }

        static void AssignMiniClips(Proto_Greybox3DCatalog catalog)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(Mini("character-male-g.fbx")))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__"))
                    continue;
                string leaf = ClipLeaf(clip.name);
                if (leaf.Contains("wheelchair"))
                    continue;
                if (leaf == "idle" && catalog.MiniIdle == null) catalog.MiniIdle = clip;
                else if (leaf == "walk" && catalog.MiniWalk == null) catalog.MiniWalk = clip;
                else if (leaf == "sprint" && catalog.MiniSprint == null) catalog.MiniSprint = clip;
                else if (leaf == "holding-right-shoot" && catalog.MiniShoot == null) catalog.MiniShoot = clip;
                else if (leaf == "fall" && catalog.MiniHit == null) catalog.MiniHit = clip;
                else if (leaf == "die" && catalog.MiniDie == null) catalog.MiniDie = clip;
                else if (leaf == "jump" && catalog.MiniJump == null) catalog.MiniJump = clip;
            }
            // Fallbacks if Unity names the takes slightly differently.
            if (catalog.MiniShoot == null || catalog.MiniHit == null || catalog.MiniIdle == null)
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(Mini("character-male-g.fbx")))
                {
                    if (obj is not AnimationClip clip || clip.name.StartsWith("__"))
                        continue;
                    string leaf = ClipLeaf(clip.name);
                    if (leaf.Contains("wheelchair")) continue;
                    if (catalog.MiniIdle == null && leaf.Contains("idle")) catalog.MiniIdle = clip;
                    if (catalog.MiniWalk == null && leaf == "walk") catalog.MiniWalk = clip;
                    if (catalog.MiniShoot == null && leaf.Contains("shoot")) catalog.MiniShoot = clip;
                    if (catalog.MiniHit == null && leaf.Contains("fall")) catalog.MiniHit = clip;
                    if (catalog.MiniDie == null && leaf.Contains("die")) catalog.MiniDie = clip;
                }
            }
        }

        static string ClipLeaf(string name)
        {
            string n = name.ToLowerInvariant();
            int at = n.LastIndexOf('@');
            if (at >= 0) n = n.Substring(at + 1);
            int pipe = n.LastIndexOf('|');
            if (pipe >= 0) n = n.Substring(pipe + 1);
            const string prefix = "character-male-g_";
            if (n.StartsWith(prefix)) n = n.Substring(prefix.Length);
            return n.Trim();
        }

        static void ConfigureMiniImport(string path, bool animations = true)
        {
            if (AssetImporter.GetAtPath(path) == null)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null)
            {
                Debug.LogWarning("[Greybox3D] Mini FBX missing at " + path);
                return;
            }

            bool dirty = imp.animationType != ModelImporterAnimationType.Generic
                         || (animations && !imp.importAnimation)
                         || imp.materialImportMode != ModelImporterMaterialImportMode.None;
            if (animations)
            {
                imp.animationType = ModelImporterAnimationType.Generic;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation = true;
                imp.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
                var clips = imp.defaultClipAnimations;
                if (clips != null)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        string n = clips[i].name.ToLowerInvariant();
                        clips[i].loopTime = n.Contains("idle") || n.Contains("walk") || n.Contains("sprint") ||
                                            n == "static";
                    }
                    imp.clipAnimations = clips;
                    dirty = true;
                }
            }
            else
            {
                imp.importAnimation = false;
            }

            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            if (dirty)
                imp.SaveAndReimport();
        }

        const string ArtRoot = "Assets/HillNGun/Proto/Greybox3D/Art/";
        static string Art(string rel) => ArtRoot + rel;

        static void ConfigureDesignerArt()
        {
            ConfigureCharTex(Art("Characters/hng_char_player_colormap.png"));
            ConfigureCharTex(Art("Characters/hng_char_enemy_colormap.png"));
            ConfigureCharTex(Art("Characters/hng_char_enemyfast_colormap.png"));
            ConfigureCharTex(Art("Characters/hng_char_enemystrong_colormap.png"));
            ConfigureCharTex(Art("Characters/hng_char_enemybomb_colormap.png"));
            ConfigureCharTex(Art("Characters/hng_char_enemysniper_colormap.png"));
            ConfigureCharTex(Art("Characters/hng_char_playermaleg_colormap.png"));

            string[] tiles =
            {
                "Environment/hng_env_wall_wood_normal.png",
                "Environment/hng_env_deck_wood_normal.png",
                "Environment/hng_env_roof_shingle_normal.png",
                "Environment/hng_env_awning_blue_normal.png",
                "Environment/hng_env_awning_red_normal.png",
                "Environment/hng_env_awning_yellow_normal.png",
                "Environment/hng_env_awning_green_normal.png",
                "Environment/hng_env_ground_dirt_normal.png",
                "Environment/hng_env_rock_mesa_normal.png"
            };
            foreach (var t in tiles)
                ConfigureEnvTile(Art(t), repeat: true);
            ConfigureEnvTile(Art("Environment/hng_env_atlas_main.png"), repeat: false);

            ConfigureUiSprite(Art("UI/hng_ui_btn_aim_normal.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_aim_pressed.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_aim_disabled.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_attack_normal.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_attack_pressed.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_attack_disabled.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_skill_normal.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_skill_pressed.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_skill_disabled.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_reload_normal.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_reload_pressed.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_reload_disabled.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_pause_normal.png"));
            ConfigureUiSprite(Art("UI/hng_ui_btn_pause_pressed.png"));
            ConfigureUiSprite(Art("UI/hng_ui_joystick_base_normal.png"));
            ConfigureUiSprite(Art("UI/hng_ui_joystick_knob_normal.png"));
            ConfigureUiSprite(Art("UI/hng_ui_joystick_knob_pressed.png"));
            ConfigureUiSprite(Art("UI/hng_ui_hp_frame_normal.png"), border: new Vector4(38, 38, 38, 38));
            ConfigureUiSprite(Art("UI/hng_ui_hp_track_normal.png"), border: new Vector4(20, 20, 20, 20));
            ConfigureUiSprite(Art("UI/hng_ui_hp_fill_normal.png"), border: new Vector4(20, 20, 20, 20));
            ConfigureUiSprite(Art("UI/hng_ui_hp_fill_low.png"), border: new Vector4(20, 20, 20, 20));
            ConfigureUiSprite(Art("UI/hng_ui_hp_fill_full.png"));
            ConfigureUiSprite(Art("UI/hng_ui_panel_text_normal.png"), border: new Vector4(36, 36, 36, 36));
            ConfigureUiSprite(Art("UI/hng_ui_crosshair_normal.png"));
            ConfigureUiSprite(Art("UI/hng_ui_crosshair_hit.png"));
            ConfigureUiSprite(Art("UI/hng_ui_text_score_normal.png"), pivot: new Vector2(0f, 0.5f));
            ConfigureUiSprite(Art("UI/hng_ui_text_stage_normal.png"));
            ConfigureUiSprite(Art("UI/hng_ui_text_hp_normal.png"));

            ConfigureFxSprite(Art("FX/hng_fx_bullet_player_normal.png"));
            ConfigureFxTrail(Art("FX/hng_fx_bullet_player_trail.png"));
            ConfigureFxSprite(Art("FX/hng_fx_bullet_player_streak.png"), pivot: new Vector2(0.875f, 0.5f));
            ConfigureFxSprite(Art("FX/hng_fx_bullet_enemy_normal.png"));
            ConfigureFxTrail(Art("FX/hng_fx_bullet_enemy_trail.png"));
            ConfigureFxSprite(Art("FX/hng_fx_bullet_enemy_streak.png"), pivot: new Vector2(0.8125f, 0.5f));
            ConfigureFxSheet(Art("FX/hng_fx_muzzle_flash_sheet.png"), 4, 160, 160, new Vector2(0.25f, 0.5f));
            ConfigureFxSheet(Art("FX/hng_fx_hit_spark_sheet.png"), 5, 128, 128, new Vector2(0.5f, 0.5f));
            ConfigureFxSheet(Art("FX/hng_fx_explosion_sheet.png"), 6, 256, 256, new Vector2(0.5f, 0.5f));
            ConfigureFxSheet(Art("FX/hng_fx_smoke_sheet.png"), 6, 192, 192, new Vector2(0.5f, 0.5f));
            ConfigureFxSheet(Art("FX/hng_fx_dust_sheet.png"), 6, 160, 160, new Vector2(0.5f, 0.2f));
            ConfigureFxSheet(Art("FX/hng_fx_coin_spin_sheet.png"), 6, 96, 96, new Vector2(0.5f, 0.5f));
            ConfigureFxSprite(Art("FX/hng_fx_coin_normal.png"));
            ConfigureCharTex(Art("Characters/hng_char_turret_colormap.png"));
            ConfigureEnvTile(Art("Environment/hng_env_awning_stall_normal.png"), repeat: true);
            ConfigureCutout(Art("Environment/hng_env_awning_stall_edge.png"), wrapU: TextureWrapMode.Repeat, wrapV: TextureWrapMode.Clamp);
            ConfigureCutout(Art("Environment/hng_env_fence_wood_normal.png"), wrapU: TextureWrapMode.Repeat, wrapV: TextureWrapMode.Clamp);
            ConfigureCutout(Art("Environment/hng_env_prop_wheel_normal.png"), wrapU: TextureWrapMode.Clamp, wrapV: TextureWrapMode.Clamp);
            ConfigureCutout(Art("Environment/hng_env_prop_lamp_normal.png"), wrapU: TextureWrapMode.Clamp, wrapV: TextureWrapMode.Clamp);
            ConfigureCutout(Art("Environment/hng_env_prop_lampglow_normal.png"), wrapU: TextureWrapMode.Clamp, wrapV: TextureWrapMode.Clamp);
            ConfigureCutout(Art("Environment/hng_env_prop_rockpile_normal.png"), wrapU: TextureWrapMode.Clamp, wrapV: TextureWrapMode.Clamp);
            ConfigureEnvTile(Art("Environment/hng_env_atlas_main_v2.png"), repeat: false);
            ConfigureFxSheet(Art("FX/hng_fx_dizzy_stars_sheet.png"), 8, 256, 128, new Vector2(0.5f, 0.5f));
            ConfigureFxSheet(Art("FX/hng_fx_roll_dust_puff_sheet.png"), 4, 128, 128, new Vector2(0.5f, 0.15f));
            ConfigureFxTrail(Art("FX/hng_fx_roll_dust_trail.png"));
            ConfigureFxSheet(Art("FX/hng_fx_shield_bubble_sheet.png"), 8, 256, 256, new Vector2(0.5f, 0.5f));
            ConfigureFxSheet(Art("FX/hng_fx_shield_pop_sheet.png"), 4, 256, 256, new Vector2(0.5f, 0.5f));
            ConfigureEnvTile(Art("FX/hng_fx_shield_sphere_add.png"), repeat: true);
            ConfigureEnvTile(Art("FX/hng_fx_shield_sphere_alpha.png"), repeat: true);
        }

        static void ConfigureCutout(string path, TextureWrapMode wrapU, TextureWrapMode wrapV)
        {
            ConfigureTex(path, imp =>
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true;
                imp.filterMode = FilterMode.Bilinear;
                imp.mipmapEnabled = true;
                imp.alphaIsTransparency = true;
                imp.mipMapsPreserveCoverage = true;
                imp.alphaTestReferenceValue = 0.5f;
                imp.wrapModeU = wrapU;
                imp.wrapModeV = wrapV;
                imp.textureCompression = TextureImporterCompression.Compressed;
                imp.maxTextureSize = 1024;
            });
        }

        static void AssignDesignerArt(Proto_Greybox3DCatalog catalog)
        {
            catalog.CharColormapPlayer = LoadTex(Art("Characters/hng_char_player_colormap.png"));
            catalog.CharColormapEnemy = LoadTex(Art("Characters/hng_char_enemy_colormap.png"));
            catalog.CharColormapEnemyFast = LoadTex(Art("Characters/hng_char_enemyfast_colormap.png"));
            catalog.CharColormapEnemyStrong = LoadTex(Art("Characters/hng_char_enemystrong_colormap.png"));
            catalog.CharColormapEnemyBomb = LoadTex(Art("Characters/hng_char_enemybomb_colormap.png"));
            catalog.CharColormapEnemySniper = LoadTex(Art("Characters/hng_char_enemysniper_colormap.png"));
            catalog.CharColormapPlayerMaleG = LoadTex(Art("Characters/hng_char_playermaleg_colormap.png"));
            catalog.EnvWallWood = LoadTex(Art("Environment/hng_env_wall_wood_normal.png"));
            catalog.EnvDeckWood = LoadTex(Art("Environment/hng_env_deck_wood_normal.png"));
            catalog.EnvRoofShingle = LoadTex(Art("Environment/hng_env_roof_shingle_normal.png"));
            catalog.EnvAwningBlue = LoadTex(Art("Environment/hng_env_awning_blue_normal.png"));
            catalog.EnvAwningRed = LoadTex(Art("Environment/hng_env_awning_red_normal.png"));
            catalog.EnvAwningYellow = LoadTex(Art("Environment/hng_env_awning_yellow_normal.png"));
            catalog.EnvAwningGreen = LoadTex(Art("Environment/hng_env_awning_green_normal.png"));
            catalog.EnvGroundDirt = LoadTex(Art("Environment/hng_env_ground_dirt_normal.png"));
            catalog.EnvRockMesa = LoadTex(Art("Environment/hng_env_rock_mesa_normal.png"));
            catalog.EnvAtlas = LoadTex(Art("Environment/hng_env_atlas_main.png"));
            catalog.LilitaOne = AssetDatabase.LoadAssetAtPath<Font>(Art("Fonts/LilitaOne-Regular.ttf"));
            catalog.UiJoystickBase = LoadSprite(Art("UI/hng_ui_joystick_base_normal.png"));
            catalog.UiJoystickKnob = LoadSprite(Art("UI/hng_ui_joystick_knob_normal.png"));
            catalog.UiJoystickKnobPressed = LoadSprite(Art("UI/hng_ui_joystick_knob_pressed.png"));
            catalog.UiBtnAttackNormal = LoadSprite(Art("UI/hng_ui_btn_attack_normal.png"));
            catalog.UiBtnAttackPressed = LoadSprite(Art("UI/hng_ui_btn_attack_pressed.png"));
            catalog.UiBtnSkillNormal = LoadSprite(Art("UI/hng_ui_btn_skill_normal.png"));
            catalog.UiBtnSkillPressed = LoadSprite(Art("UI/hng_ui_btn_skill_pressed.png"));
            catalog.UiBtnReloadNormal = LoadSprite(Art("UI/hng_ui_btn_reload_normal.png"));
            catalog.UiBtnReloadPressed = LoadSprite(Art("UI/hng_ui_btn_reload_pressed.png"));
            catalog.UiBtnPauseNormal = LoadSprite(Art("UI/hng_ui_btn_pause_normal.png"));
            catalog.UiBtnPausePressed = LoadSprite(Art("UI/hng_ui_btn_pause_pressed.png"));
            catalog.UiHpFrame = LoadSprite(Art("UI/hng_ui_hp_frame_normal.png"));
            catalog.UiHpTrack = LoadSprite(Art("UI/hng_ui_hp_track_normal.png"));
            catalog.UiHpFill = LoadSprite(Art("UI/hng_ui_hp_fill_normal.png"));
            catalog.UiHpFillLow = LoadSprite(Art("UI/hng_ui_hp_fill_low.png"));
            catalog.UiHpFillFull = LoadSprite(Art("UI/hng_ui_hp_fill_full.png"));
            catalog.UiCrosshair = LoadSprite(Art("UI/hng_ui_crosshair_normal.png"));
            catalog.UiCrosshairHit = LoadSprite(Art("UI/hng_ui_crosshair_hit.png"));
            catalog.UiTextScore = LoadSprite(Art("UI/hng_ui_text_score_normal.png"));
            catalog.UiTextStage = LoadSprite(Art("UI/hng_ui_text_stage_normal.png"));
            catalog.UiTextHp = LoadSprite(Art("UI/hng_ui_text_hp_normal.png"));
            catalog.FxBulletPlayer = LoadTex(Art("FX/hng_fx_bullet_player_normal.png"));
            catalog.FxBulletPlayerTrail = LoadTex(Art("FX/hng_fx_bullet_player_trail.png"));
            catalog.FxBulletPlayerStreak = LoadTex(Art("FX/hng_fx_bullet_player_streak.png"));
            catalog.FxBulletEnemy = LoadTex(Art("FX/hng_fx_bullet_enemy_normal.png"));
            catalog.FxBulletEnemyTrail = LoadTex(Art("FX/hng_fx_bullet_enemy_trail.png"));
            catalog.FxBulletEnemyStreak = LoadTex(Art("FX/hng_fx_bullet_enemy_streak.png"));
            catalog.FxMuzzle = LoadSheet(Art("FX/hng_fx_muzzle_flash_sheet.png"));
            catalog.FxHitSpark = LoadSheet(Art("FX/hng_fx_hit_spark_sheet.png"));
            catalog.FxExplosion = LoadSheet(Art("FX/hng_fx_explosion_sheet.png"));
            catalog.FxSmoke = LoadSheet(Art("FX/hng_fx_smoke_sheet.png"));
            catalog.FxDust = LoadSheet(Art("FX/hng_fx_dust_sheet.png"));
            catalog.FxCoinSpin = LoadSheet(Art("FX/hng_fx_coin_spin_sheet.png"));
            catalog.FxCoin = LoadSprite(Art("FX/hng_fx_coin_normal.png"));
            catalog.SpriteShader = Shader.Find("HillNGun/Proto/Greybox3D/Sprite");
            catalog.CutoutShader = Shader.Find("HillNGun/Proto/Greybox3D/Cutout");
            catalog.CharColormapTurret = LoadTex(Art("Characters/hng_char_turret_colormap.png"));
            catalog.EnvAwningStall = LoadTex(Art("Environment/hng_env_awning_stall_normal.png"));
            catalog.EnvAwningStallEdge = LoadTex(Art("Environment/hng_env_awning_stall_edge.png"));
            catalog.EnvFenceWood = LoadTex(Art("Environment/hng_env_fence_wood_normal.png"));
            catalog.EnvPropWheel = LoadTex(Art("Environment/hng_env_prop_wheel_normal.png"));
            catalog.EnvPropLamp = LoadTex(Art("Environment/hng_env_prop_lamp_normal.png"));
            catalog.EnvPropLampGlow = LoadTex(Art("Environment/hng_env_prop_lampglow_normal.png"));
            catalog.EnvPropRockPile = LoadTex(Art("Environment/hng_env_prop_rockpile_normal.png"));
            catalog.EnvAtlasV2 = LoadTex(Art("Environment/hng_env_atlas_main_v2.png"));
            catalog.FxDizzyStars = LoadSheet(Art("FX/hng_fx_dizzy_stars_sheet.png"));
            catalog.FxRollDustPuff = LoadSheet(Art("FX/hng_fx_roll_dust_puff_sheet.png"));
            catalog.FxRollDustTrail = LoadTex(Art("FX/hng_fx_roll_dust_trail.png"));
            catalog.FxShieldBubble = LoadSheet(Art("FX/hng_fx_shield_bubble_sheet.png"));
            catalog.FxShieldPop = LoadSheet(Art("FX/hng_fx_shield_pop_sheet.png"));
            catalog.FxShieldSphereAdd = LoadTex(Art("FX/hng_fx_shield_sphere_add.png"));
            catalog.FxShieldSphereAlpha = LoadTex(Art("FX/hng_fx_shield_sphere_alpha.png"));
        }

        static Sprite LoadSprite(string path)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all != null)
            {
                foreach (var a in all)
                {
                    var s = a as Sprite;
                    if (s != null) return s;
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite[] LoadSheet(string path)
        {
            var list = new System.Collections.Generic.List<Sprite>();
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all != null)
            {
                foreach (var a in all)
                {
                    var s = a as Sprite;
                    if (s != null) list.Add(s);
                }
            }
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list.ToArray();
        }

        static void ConfigureCharTex(string path)
        {
            ConfigureTex(path, imp =>
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true;
                imp.filterMode = FilterMode.Point;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.maxTextureSize = 512;
            });
        }

        static void ConfigureEnvTile(string path, bool repeat)
        {
            ConfigureTex(path, imp =>
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true;
                imp.filterMode = FilterMode.Bilinear;
                imp.mipmapEnabled = true;
                imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                imp.textureCompression = TextureImporterCompression.Compressed;
                imp.maxTextureSize = 1024;
            });
        }

        static void ConfigureUiSprite(string path, Vector4 border = default, Vector2? pivot = null)
        {
            ConfigureSprite(path, 100f, SpriteImportMode.Single, 1, 0, 0, pivot ?? new Vector2(0.5f, 0.5f), border);
        }

        static void ConfigureFxSprite(string path, Vector2? pivot = null)
        {
            ConfigureSprite(path, 100f, SpriteImportMode.Single, 1, 0, 0, pivot ?? new Vector2(0.5f, 0.5f), Vector4.zero);
        }

        static void ConfigureFxTrail(string path)
        {
            ConfigureTex(path, imp =>
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true;
                imp.filterMode = FilterMode.Bilinear;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
            });
        }

        static void ConfigureFxSheet(string path, int frames, int cellW, int cellH, Vector2 pivot)
        {
            ConfigureSprite(path, 100f, SpriteImportMode.Multiple, frames, cellW, cellH, pivot, Vector4.zero);
        }

        static void ConfigureSprite(string path, float ppu, SpriteImportMode mode, int frames, int cellW, int cellH, Vector2 pivot, Vector4 border)
        {
            if (!File.Exists(Path.GetFullPath(path)))
            {
                Debug.LogWarning("[Greybox3D] Art missing at " + path);
                return;
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = mode;
            imp.spritePixelsPerUnit = ppu;
            imp.sRGBTexture = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMode = (int)mode;
            settings.spritePixelsPerUnit = ppu;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            settings.sRGBTexture = true;
            settings.alphaIsTransparency = true;
            settings.filterMode = FilterMode.Bilinear;
            settings.mipmapEnabled = false;
            imp.SetTextureSettings(settings);
            if (mode == SpriteImportMode.Multiple && frames > 0)
            {
                var metas = new SpriteMetaData[frames];
                for (int i = 0; i < frames; i++)
                {
                    metas[i] = new SpriteMetaData
                    {
                        name = Path.GetFileNameWithoutExtension(path) + "_" + i,
                        rect = new Rect(i * cellW, 0, cellW, cellH),
                        pivot = pivot,
                        alignment = (int)SpriteAlignment.Custom,
                        border = border
                    };
                }
                imp.spritesheet = metas;
            }
            imp.SaveAndReimport();
        }

    }
}

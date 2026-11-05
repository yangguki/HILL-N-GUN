using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using HillNGun.Data;
using HillNGun.Core;
using HillNGun.Combat;
using HillNGun.Battlefield;
using HillNGun.Input;
using HillNGun.Logging;
using HillNGun.DebugTools;
using HillNGun.Feel;
using HillNGun.UI;
using HillNGun.Characters;
using HillNGun.PvE;
using HillNGun.PvP;
using System.IO;

namespace HillNGun.Editor
{
    public static class GameSetup
    {
        private const string ASSET_ROOT = "Assets/HillNGun/";
        private const string DATA_PATH = ASSET_ROOT + "Data/";
        private const string SCENE_PATH = ASSET_ROOT + "Scenes/";

        [MenuItem("HillNGun/Setup All Scenes and Data", false, 1)]
        public static void SetupAll()
        {
            if (!EditorUtility.DisplayDialog("전체 게임 설정",
                "이 작업은 다음을 수행합니다:\n\n" +
                "1. 모든 ScriptableObject 데이터 에셋 생성\n" +
                "   - 캐릭터, 무기, 수류탄, 스킬 등\n" +
                "   - 적 타입 및 스테이지 데이터\n" +
                "   - 보스 데이터\n\n" +
                "2. 플레이스홀더 아트 및 오디오 생성\n\n" +
                "3. 모든 게임 씬 생성\n" +
                "   - Title, ModeSelect, CharacterSelect\n" +
                "   - PvE_Match, PvP_Match, M1_Sandbox\n" +
                "   - Result\n\n" +
                "4. Build Settings 업데이트\n\n" +
                "기존 에셋이 있으면 덮어씁니다. 계속하시겠습니까?",
                "계속", "취소"))
            {
                return;
            }

            SetupAllHeadless();

            EditorUtility.DisplayDialog("완료", 
                "전체 게임 설정이 완료되었습니다.\n\n" +
                "Title 씬에서 게임을 시작하거나,\n" +
                "M1_Sandbox 씬에서 조작을 테스트할 수 있습니다.\n\n" +
                "빌드: HillNGun → Build → Build All Platforms",
                "확인");
        }

        public static void SetupAllHeadless()
        {
            CreateDirectories();
            
            ArtGenerator.GenerateAllArt();
            AudioGenerator.GenerateAllAudio();
            
            CreateAllDataAssets();
            CreateSpriteLibrary();
            CreateAudioLibrary();
            CreateAllScenes();
            BuildHelper.UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[GameSetup] Setup completed successfully!");
        }

        private static void CreateSpriteLibrary()
        {
            string path = DATA_PATH + "SpriteLibrary.asset";
            var library = ScriptableObject.CreateInstance<SpriteLibrary>();
            
            library.characters = new CharacterSprites[]
            {
                LoadCharacterSprites("buster"),
                LoadCharacterSprites("raven"),
                LoadCharacterSprites("volt")
            };

            library.enemies = new EnemySprites[]
            {
                new EnemySprites { enemyId = "pistol_mook", idle = LoadSprite("Enemies/pistol_mook") },
                new EnemySprites { enemyId = "dynamite_bandit", idle = LoadSprite("Enemies/dynamite_bandit") },
                new EnemySprites { enemyId = "mounted_raider", idle = LoadSprite("Enemies/mounted_raider") }
            };

            library.boss = new BossSprites
            {
                bossId = "rattlesnake_rex",
                idle = LoadSprite("Boss/rattlesnake_rex"),
                warningIndicator = LoadSprite("Boss/warning_indicator"),
                portrait = LoadDeckSprite("Portraits/rattlesnake_rex_portrait")
            };

            // Deck boss portraits (HILLNGUN_기획서 hng_g3_boss1-5) for the lobby stage select
            var regionArt = new System.Collections.Generic.List<RegionBossArt>();
            foreach (var rid in new[] { "dusty_town", "railroad_canyon", "silver_mine", "border_fort", "ghost_town" })
            {
                var sp = LoadDeckSprite($"Portraits/region_{rid}");
                if (sp != null) regionArt.Add(new RegionBossArt { regionId = rid, portrait = sp });
            }
            library.regionBosses = regionArt.ToArray();

            // Keyed by CoverData.id (generated art: barrel / crate / wagon)
            // Painted cover art (기획 handoff, ArtSource/covers → Art/Covers): intact only; damage stages tint the same
            // sprite (SpriteLibrary.coverStageTints) until dedicated damage art arrives.
            library.covers = new CoverSprites[]
            {
                LoadCoverArt("cover_barrel", "cover_barrel"),
                LoadCoverArt("cover_crate", "cover_crate"),
                LoadCoverArt("cover_wagon", "cover_wagon_small"),
                LoadCoverArt("cover_swing_door", "cover_door"),
                LoadCoverArt("cover_hay", "cover_hay"),
                LoadCoverArt("cover_trough", "cover_trough"),
                LoadCoverArt("cover_rock", "cover_rock")
            };

            library.bullet = LoadSprite("Projectiles/bullet");
            library.bulletHeavy = LoadSprite("Projectiles/bullet_heavy");
            library.grenade = LoadSprite("Projectiles/grenade");

            library.muzzleFlash = LoadSprite("Effects/muzzle_flash");
            library.hitSpark = LoadSprite("Effects/hit_spark");
            library.dustPuff = LoadSprite("Effects/dust_puff");
            library.explosion = LoadSprite("Effects/explosion");

            library.skyGradient = LoadSprite("Background/sky_gradient");
            library.mesas = LoadSprite("Background/mesas");
            library.saloonFacades = LoadSprite("Background/saloon_facades");
            library.ground = LoadSprite("Background/ground");

            library.hpBarBg = LoadSprite("UI/hp_bar_bg");
            library.hpBarFill = LoadSprite("UI/hp_bar_fill");
            library.ammoCell = LoadSprite("UI/ammo_cell");
            library.ammoCellEmpty = LoadSprite("UI/ammo_cell_empty");
            library.buttonBg = LoadSprite("UI/button_bg");
            library.joystickBg = LoadSprite("UI/joystick_bg");
            library.joystickKnob = LoadSprite("UI/joystick_knob");

            CreateOrReplaceAsset(library, path);
        }

        private static CharacterSprites LoadCharacterSprites(string id)
        {
            return new CharacterSprites
            {
                characterId = id,
                idle = LoadSprite($"Characters/{id}_idle"),
                shoot = LoadSprite($"Characters/{id}_shoot"),
                roll = LoadSprite($"Characters/{id}_roll"),
                downed = LoadSprite($"Characters/{id}_downed"),
                portrait = LoadDeckSprite($"Portraits/{id}_portrait")
            };
        }

        private static CoverSprites LoadCoverArt(string coverId, string file)
        {
            return new CoverSprites { coverId = coverId, intact = LoadSpriteAt($"Assets/HillNGun/Art/Covers/{file}.png") };
        }

        private static CoverSprites LoadCoverSprites(string coverId, string id)
        {
            return new CoverSprites
            {
                coverId = coverId,
                intact = LoadSprite($"Covers/{id}_intact"),
                cracked = LoadSprite($"Covers/{id}_cracked"),
                damaged = LoadSprite($"Covers/{id}_damaged"),
                destroyed = LoadSprite($"Covers/{id}_destroyed")
            };
        }

        // Character art from the design deck (Assets/HillNGun/Art/Deck, committed source art) wins over the procedural
        // placeholders in Art/Generated (ArtGenerator only writes Generated/, so setup never overwrites deck art).
        // Set false to go back to the procedural set. Deck sprites keep the old world size: 256px canvas at 128 PPU = 2 units
        // (same as the 64px@32 placeholders), enemies/boss likewise (x4 resolution, x4 PPU).
        public static bool UseDeckArt = true;
        private const string DECK_ART_PATH = "Assets/HillNGun/Art/Deck/";
        private const float DECK_PPU = 128f;
        private const float DECK_HERO_PPU = 108f;
        private const float DECK_PORTRAIT_PPU = 100f;

        private static Sprite LoadDeckSprite(string relativePath)
        {
            if (!UseDeckArt) return null;
            string path = $"{DECK_ART_PATH}{relativePath}.png";
            if (!System.IO.File.Exists(path)) return null;
            bool portrait = relativePath.StartsWith("Portraits/");
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                // heroes drawn ~18% larger than the old placeholders so the painted deck art stays readable at field scale
                float ppu = portrait ? DECK_PORTRAIT_PPU : relativePath.StartsWith("Characters/") ? DECK_HERO_PPU : DECK_PPU;
                bool dirty = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                    || !importer.isReadable || !Mathf.Approximately(importer.spritePixelsPerUnit, ppu) || importer.mipmapEnabled == portrait
                    || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.maxTextureSize < 1024;
                if (dirty)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.isReadable = true; // SkinRecolor + CharacterSelect portraits read pixels
                    importer.spritePixelsPerUnit = ppu;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.mipmapEnabled = !portrait; // field sprites are drawn ~4x smaller than source
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 1024;
                    importer.SaveAndReimport();
                }
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning($"[GameSetup] Deck sprite failed to load: {path}");
            return sprite;
        }

        private static Sprite LoadSprite(string relativePath)
        {
            var deck = LoadDeckSprite(relativePath);
            if (deck != null) return deck;
            string path = $"Assets/HillNGun/Art/Generated/{relativePath}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (obj is Sprite s) { sprite = s; break; }
                }
            }
            if (sprite == null) Debug.LogWarning($"[GameSetup] Sprite not found: {path}");
            return sprite;
        }

        private static void CreateAudioLibrary()
        {
            string path = DATA_PATH + "AudioLibrary.asset";
            var library = ScriptableObject.CreateInstance<AudioLibrary>();
            
            library.masterVolume = 0.7f;
            library.sfxVolume = 0.8f;
            library.bgmVolume = 0.5f;

            library.shotLight = LoadAudioClip("SFX/shot_light");
            library.shotHeavy = LoadAudioClip("SFX/shot_heavy");
            library.shotShotgun = LoadAudioClip("SFX/shot_shotgun");
            library.chargeLoop = LoadAudioClip("SFX/charge_loop");
            library.chargeReady = LoadAudioClip("SFX/charge_ready");

            library.reloadClick = LoadAudioClip("SFX/reload_click");
            library.reloadFull = LoadAudioClip("SFX/reload_full");

            library.roll = LoadAudioClip("SFX/roll");
            library.footstep = LoadAudioClip("SFX/footstep");

            library.grenadeThrow = LoadAudioClip("SFX/grenade_throw");
            library.grenadeExplode = LoadAudioClip("SFX/grenade_explode");

            library.coverHit = LoadAudioClip("SFX/cover_hit");
            library.coverBreak = LoadAudioClip("SFX/cover_break");

            library.hitLight = LoadAudioClip("SFX/hit_light");
            library.hitHeavy = LoadAudioClip("SFX/hit_heavy");
            library.downed = LoadAudioClip("SFX/downed");
            library.revive = LoadAudioClip("SFX/revive");
            library.skillActivate = LoadAudioClip("SFX/skill_activate");

            library.bossRoar = LoadAudioClip("SFX/boss_roar");
            library.bossWarning = LoadAudioClip("SFX/boss_warning");
            library.bossPhaseChange = LoadAudioClip("SFX/boss_phase_change");

            library.uiClick = LoadAudioClip("SFX/ui_click");
            library.uiHover = LoadAudioClip("SFX/ui_hover");
            library.uiBack = LoadAudioClip("SFX/ui_back");

            library.victoryFanfare = LoadAudioClip("SFX/victory_fanfare");
            library.defeatSting = LoadAudioClip("SFX/defeat_sting");

            library.bgmMenu = LoadAudioClip("BGM/bgm_menu");
            library.bgmBattle = LoadAudioClip("BGM/bgm_battle");
            library.bgmBoss = LoadAudioClip("BGM/bgm_boss");

            CreateOrReplaceAsset(library, path);
        }

        private static AudioClip LoadAudioClip(string relativePath)
        {
            string path = $"Assets/HillNGun/Audio/Generated/{relativePath}.wav";
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void CreateDirectories()
        {
            string[] dirs = {
                ASSET_ROOT,
                DATA_PATH,
                DATA_PATH + "Characters",
                DATA_PATH + "Weapons",
                DATA_PATH + "Grenades",
                DATA_PATH + "Rolls",
                DATA_PATH + "Skills",
                DATA_PATH + "Passives",
                DATA_PATH + "Cover",
                DATA_PATH + "Battlefield",
                DATA_PATH + "Input",
                DATA_PATH + "Downed",
                DATA_PATH + "Feel",
                DATA_PATH + "Targets",
                DATA_PATH + "Match",
                DATA_PATH + "Enemies",
                DATA_PATH + "Stages",
                DATA_PATH + "Bosses",
                DATA_PATH + "AI",
                DATA_PATH + "Progression",
                DATA_PATH + "UI",
                DATA_PATH + "External",
                DATA_PATH + "Tutorial",
                SCENE_PATH
            };

            foreach (string dir in dirs)
            {
                CreateFolderRecursive(dir);
            }
        }

        private static void CreateFolderRecursive(string path)
        {
            path = path.TrimEnd('/');
            
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(parent) || parent == "Assets")
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    string folderName = Path.GetFileName(path);
                    string parentPath = string.IsNullOrEmpty(parent) ? "Assets" : parent;
                    AssetDatabase.CreateFolder(parentPath, folderName);
                }
                return;
            }

            CreateFolderRecursive(parent);

            if (!AssetDatabase.IsValidFolder(path))
            {
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void CreateAllDataAssets()
        {
            M1SandboxSetup.SetupDataOnly();
            CreateEnemyAssets();
            CreateStageAssets();
            CreateChapterAndVisualAssets();
            CreateBossAsset();
            StageThemeSetup.CreateThemes();
            CreateBotBehaviorAsset();
            CreateProgressionAsset();
            CreateUIThemeAsset();
            CreateExternalServicesAsset();
            CreateTutorialAsset();
            CreateMotionFeelAssets();
            CreateActor3DAssets();
            CreateTouchControlsAsset();
            CreateDropTableAsset();
        }

        /// <summary>Field drops (t48u). Code defaults in DropTableData are the tuned numbers (see implementation_status.md).</summary>
        private static void CreateDropTableAsset()
        {
            CreateFolderRecursive(DATA_PATH + "Drops");
            var t = CreateOrLoad<DropTableData>(DATA_PATH + "Drops/drop_table.asset");
            var defaults = ScriptableObject.CreateInstance<DropTableData>();
            EditorUtility.CopySerialized(defaults, t);
            Object.DestroyImmediate(defaults);
            t.name = "drop_table";
            EditorUtility.SetDirty(t);
        }

        /// <summary>
        /// Brawl-Stars-style touch layout (t47u). Field defaults in TouchControlsData are the tuned values; the asset is
        /// reset to them here so a regenerated project always matches the code (layout math in TouchControlLayout).
        /// </summary>
        private static void CreateTouchControlsAsset()
        {
            var touchControls = CreateOrLoad<TouchControlsData>(DATA_PATH + "Input/touch_controls.asset");
            var defaults = ScriptableObject.CreateInstance<TouchControlsData>();
            EditorUtility.CopySerialized(defaults, touchControls);
            Object.DestroyImmediate(defaults);
            touchControls.name = "touch_controls";
            EditorUtility.SetDirty(touchControls);
        }

        private static void CreateEnemyAssets()
        {
            var pistolMook = CreateOrLoad<EnemyData>(DATA_PATH + "Enemies/enemy_pistol_mook.asset");
            pistolMook.id = "pistol_mook";
            pistolMook.nameKey = "pve.enemy.pistol_mook";
            pistolMook.enemyType = EnemyType.PistolMook;
            pistolMook.maxHp = 600;
            pistolMook.hitRadius = 0.5f;
            pistolMook.moveSpeed = 2f;
            pistolMook.warningTime = 0.5f;
            pistolMook.attackInterval = 1.5f;
            pistolMook.attackDamage = 120;
            pistolMook.isRanged = true;
            pistolMook.attackRange = 10f;
            pistolMook.bulletSpeed = 8f;
            pistolMook.gaugeValue = 1;
            pistolMook.scoreValue = 100;
            pistolMook.baseColor = new Color(0.6f, 0.4f, 0.3f);
            pistolMook.fleesOnDefeat = true;
            pistolMook.fleeSpeed = 5f;
            // t48u bullet dodge (proposal): sees a bullet coming, 25% side-step after 0.15-0.3 s, then 3 s cooldown
            pistolMook.bulletDodge = new DodgeTuning { chance = 0.25f, delayMin = 0.15f, delayMax = 0.3f, lookahead = 0.55f, margin = 0.15f, cooldown = 3f };
            pistolMook.dodgeStepDistance = 1.2f; pistolMook.dodgeStepSpeed = 7f;
            EditorUtility.SetDirty(pistolMook);

            var dynamiteBandit = CreateOrLoad<EnemyData>(DATA_PATH + "Enemies/enemy_dynamite_bandit.asset");
            dynamiteBandit.id = "dynamite_bandit";
            dynamiteBandit.nameKey = "pve.enemy.dynamite_bandit";
            dynamiteBandit.enemyType = EnemyType.DynamiteBandit;
            dynamiteBandit.maxHp = 900;
            dynamiteBandit.hitRadius = 0.55f;
            dynamiteBandit.moveSpeed = 1.8f;
            dynamiteBandit.warningTime = 1.0f;
            dynamiteBandit.attackInterval = 4f;
            dynamiteBandit.attackDamage = 450;
            dynamiteBandit.isRanged = false;
            dynamiteBandit.throwsExplosive = true;
            dynamiteBandit.explosiveRadius = 2f;
            dynamiteBandit.explosiveDamage = 450;
            dynamiteBandit.explosiveFlightTime = 0.8f;
            dynamiteBandit.gaugeValue = 2;
            dynamiteBandit.scoreValue = 200;
            dynamiteBandit.baseColor = new Color(0.5f, 0.35f, 0.25f);
            dynamiteBandit.fleesOnDefeat = true;
            dynamiteBandit.fleeSpeed = 4f;
            dynamiteBandit.bulletDodge = new DodgeTuning { chance = 0.15f, delayMin = 0.2f, delayMax = 0.35f, lookahead = 0.55f, margin = 0.15f, cooldown = 3.5f };
            dynamiteBandit.dodgeStepDistance = 1.0f; dynamiteBandit.dodgeStepSpeed = 6f;
            EditorUtility.SetDirty(dynamiteBandit);

            var mountedRaider = CreateOrLoad<EnemyData>(DATA_PATH + "Enemies/enemy_mounted_raider.asset");
            mountedRaider.id = "mounted_raider";
            mountedRaider.nameKey = "pve.enemy.mounted_raider";
            mountedRaider.enemyType = EnemyType.MountedRaider;
            mountedRaider.maxHp = 1400;
            mountedRaider.hitRadius = 0.7f;
            mountedRaider.moveSpeed = 3f;
            mountedRaider.warningTime = 1.0f;
            mountedRaider.attackInterval = 2f;
            mountedRaider.attackDamage = 100;
            mountedRaider.isRanged = true;
            mountedRaider.attackRange = 12f;
            mountedRaider.bulletSpeed = 10f;
            mountedRaider.projectilesPerShot = 3;
            mountedRaider.spreadDeg = 15f;
            mountedRaider.movesBetweenRows = true;
            mountedRaider.rowChangeInterval = 5f;
            mountedRaider.gaugeValue = 3;
            mountedRaider.scoreValue = 300;
            mountedRaider.baseColor = new Color(0.4f, 0.3f, 0.25f);
            mountedRaider.scale = 1.3f;
            mountedRaider.fleesOnDefeat = true;
            mountedRaider.fleeSpeed = 6f;
            mountedRaider.bulletDodge = new DodgeTuning { chance = 0.35f, delayMin = 0.12f, delayMax = 0.25f, lookahead = 0.6f, margin = 0.2f, cooldown = 2.5f };
            mountedRaider.dodgeStepDistance = 1.6f; mountedRaider.dodgeStepSpeed = 9f;
            EditorUtility.SetDirty(mountedRaider);
        }

        private static void CreateStageAssets()
        {
            var pistolMook = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA_PATH + "Enemies/enemy_pistol_mook.asset");
            var dynamiteBandit = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA_PATH + "Enemies/enemy_dynamite_bandit.asset");
            var mountedRaider = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA_PATH + "Enemies/enemy_mounted_raider.asset");

            // --- 1-1 (06 §7-1) target 70–90s, gauge 20, stars ★2≤80s ★3 no downed ---
            var stage1_1 = CreateOrLoad<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_1.asset");
            stage1_1.id = "dusty_town_1_1";
            stage1_1.nameKey = "pve.stage.dusty_town_1";
            stage1_1.stageNumber = 1; stage1_1.subStageNumber = 1;
            stage1_1.chapterId = "dust_town"; stage1_1.stageIndex = 1;
            stage1_1.stageType = StageType.Exterminate;
            stage1_1.gaugeMax = 20; stage1_1.waveCount = 5;
            stage1_1.stageTimeMin = 70f; stage1_1.stageTimeMax = 90f;
            stage1_1.starTimeUnderSec = 80f; stage1_1.starRequireNoDowned = true;
            stage1_1.timeBetweenSpawns = 0.85f;
            stage1_1.themeId = "town_street";
            stage1_1.enemyRowsY = new float[] { 11f, 13.5f, 16f };
            // covers: back 2 / mid 3 / front 3, wagon front-center
            stage1_1.coverPlacements = new CoverPlacement[] {
                new CoverPlacement { coverId = "cover_swing_door", rowIndex = 0, xPosition = 6f },
                new CoverPlacement { coverId = "cover_swing_door", rowIndex = 0, xPosition = 18f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 1, xPosition = 5f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 1, xPosition = 12f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 1, xPosition = 19f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 2, xPosition = 4f },
                new CoverPlacement { coverId = "cover_wagon", rowIndex = 2, xPosition = 12f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 2, xPosition = 20f },
            };
            stage1_1.hazards = new HazardPlacement[] {
                new HazardPlacement { type = HazardType.ExplosiveBarrel, x = 8f, rowIndex = 1 },
            };
            stage1_1.waves = new WaveDefinition[] {
                new WaveDefinition { waveNumber = 0, paceTag = "intro", delayBeforeWave = 1f, nextWaveDelay = 5.0f, waitForClear = true,
                    enemies = new[] { new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 0 } } },
                new WaveDefinition { waveNumber = 1, paceTag = "pressure", delayBeforeWave = 0.5f, nextWaveDelay = 5.0f, waitForClear = true,
                    enemies = new[] { new EnemySpawnEntry { enemyData = pistolMook, count = 4, preferredRow = 0 } } },
                new WaveDefinition { waveNumber = 2, paceTag = "pressure", delayBeforeWave = 0.5f, nextWaveDelay = 4.5f, waitForClear = true,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 0 },
                        new EnemySpawnEntry { enemyData = dynamiteBandit, count = 1, preferredRow = 1 } } },
                new WaveDefinition { waveNumber = 3, paceTag = "peak", delayBeforeWave = 0.5f, nextWaveDelay = 4.0f, waitForClear = true,
                    enemies = new[] { new EnemySpawnEntry { enemyData = pistolMook, count = 5, preferredRow = 0 } } },
                new WaveDefinition { waveNumber = 4, paceTag = "finale", delayBeforeWave = 0.5f, nextWaveDelay = 2.5f, waitForClear = true,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = pistolMook, count = 2, preferredRow = 0 },
                        new EnemySpawnEntry { enemyData = dynamiteBandit, count = 1, preferredRow = 1 } } },
            };
            EditorUtility.SetDirty(stage1_1);

            // --- 1-2 (06 §7-2) gauge 30, 80–105s ---
            var stage1_2 = CreateOrLoad<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_2.asset");
            stage1_2.id = "dusty_town_1_2";
            stage1_2.nameKey = "pve.stage.dusty_town_2";
            stage1_2.stageNumber = 1; stage1_2.subStageNumber = 2;
            stage1_2.chapterId = "dust_town"; stage1_2.stageIndex = 2;
            stage1_2.stageType = StageType.Exterminate;
            stage1_2.gaugeMax = 30; stage1_2.waveCount = 6;
            stage1_2.stageTimeMin = 80f; stage1_2.stageTimeMax = 105f;
            stage1_2.starTimeUnderSec = 95f; stage1_2.starRequireNoDowned = true;
            stage1_2.timeBetweenSpawns = 0.75f;
            stage1_2.themeId = "canyon_pass";
            stage1_2.enemyRowsY = new float[] { 11f, 13.5f, 16f };
            stage1_2.coverPlacements = new CoverPlacement[] {
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 0, xPosition = 4f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 0, xPosition = 12f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 0, xPosition = 20f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 1, xPosition = 5f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 1, xPosition = 10f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 1, xPosition = 14f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 1, xPosition = 19f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 2, xPosition = 6f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 2, xPosition = 12f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 2, xPosition = 18f },
            };
            stage1_2.hazards = new HazardPlacement[] {
                new HazardPlacement { type = HazardType.ExplosiveBarrel, x = 7f, rowIndex = 1 },
                new HazardPlacement { type = HazardType.ExplosiveBarrel, x = 17f, rowIndex = 2 },
            };
            stage1_2.waves = new WaveDefinition[] {
                new WaveDefinition { waveNumber = 0, paceTag = "intro", delayBeforeWave = 1f, nextWaveDelay = 4f, waitForClear = true,
                    enemies = new[] { new EnemySpawnEntry { enemyData = pistolMook, count = 4, preferredRow = 0 } } },
                new WaveDefinition { waveNumber = 1, paceTag = "pressure", delayBeforeWave = 0.5f, nextWaveDelay = 4f, waitForClear = true,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 0 },
                        new EnemySpawnEntry { enemyData = dynamiteBandit, count = 1, preferredRow = 1 } } },
                new WaveDefinition { waveNumber = 2, paceTag = "peak", delayBeforeWave = 0.5f, nextWaveDelay = 4f, waitForClear = true,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = mountedRaider, count = 1, preferredRow = 1 },
                        new EnemySpawnEntry { enemyData = pistolMook, count = 2, preferredRow = 0 } } },
                new WaveDefinition { waveNumber = 3, paceTag = "rest", delayBeforeWave = 6f, nextWaveDelay = 1.5f, waitForClear = true,
                    enemies = new[] { new EnemySpawnEntry { enemyData = pistolMook, count = 2, preferredRow = 0 } } },
                new WaveDefinition { waveNumber = 4, paceTag = "pressure", delayBeforeWave = 0.5f, nextWaveDelay = 4f, waitForClear = false,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = pistolMook, count = 4, preferredRow = 0 },
                        new EnemySpawnEntry { enemyData = dynamiteBandit, count = 1, preferredRow = 1 } } },
                new WaveDefinition { waveNumber = 5, paceTag = "finale", delayBeforeWave = 0.5f, nextWaveDelay = 2.5f, waitForClear = true,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 0 },
                        new EnemySpawnEntry { enemyData = dynamiteBandit, count = 1, preferredRow = 1 },
                        new EnemySpawnEntry { enemyData = mountedRaider, count = 1, preferredRow = 1 } } },
            };
            EditorUtility.SetDirty(stage1_2);

            // --- 1-3 (06 §7-3) gauge 40, train rest stub ---
            var stage1_3 = CreateOrLoad<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_3.asset");
            stage1_3.id = "dusty_town_1_3";
            stage1_3.nameKey = "pve.stage.dusty_town_3";
            stage1_3.stageNumber = 1; stage1_3.subStageNumber = 3;
            stage1_3.chapterId = "dust_town"; stage1_3.stageIndex = 3;
            stage1_3.stageType = StageType.Exterminate;
            stage1_3.gaugeMax = 40; stage1_3.waveCount = 7;
            stage1_3.stageTimeMin = 95f; stage1_3.stageTimeMax = 120f;
            stage1_3.starTimeUnderSec = 110f; stage1_3.starRequireNoDowned = true; stage1_3.starMinBarrelExplosions = 2;
            stage1_3.timeBetweenSpawns = 0.7f;
            stage1_3.themeId = "rail_station";
            stage1_3.enemyRowsY = new float[] { 11f, 13.5f, 16f };
            stage1_3.coverPlacements = new CoverPlacement[] {
                new CoverPlacement { coverId = "cover_swing_door", rowIndex = 0, xPosition = 5f },
                new CoverPlacement { coverId = "cover_swing_door", rowIndex = 0, xPosition = 19f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 1, xPosition = 6f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 1, xPosition = 12f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 1, xPosition = 18f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 2, xPosition = 4f },
                new CoverPlacement { coverId = "cover_wagon", rowIndex = 2, xPosition = 12f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 2, xPosition = 16f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 2, xPosition = 20f },
            };
            stage1_3.hazards = new HazardPlacement[] {
                new HazardPlacement { type = HazardType.ExplosiveBarrel, x = 7f, rowIndex = 1 },
                new HazardPlacement { type = HazardType.ExplosiveBarrel, x = 15f, rowIndex = 2 },
                new HazardPlacement { type = HazardType.ExplosiveBarrel, x = 10f, rowIndex = 2 },
                new HazardPlacement { type = HazardType.TrainPass, x = 12f, rowIndex = 2, param = 3f },
            };
            stage1_3.waves = new WaveDefinition[] {
                new WaveDefinition { waveNumber = 0, paceTag = "intro", delayBeforeWave = 1f, nextWaveDelay = 4f, waitForClear = true,
                    enemies = new[] { new EnemySpawnEntry { enemyData = pistolMook, count = 4, preferredRow = 0 } } },
                new WaveDefinition { waveNumber = 1, paceTag = "pressure", delayBeforeWave = 0.5f, nextWaveDelay = 4f, waitForClear = true,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 0 },
                        new EnemySpawnEntry { enemyData = dynamiteBandit, count = 2, preferredRow = 1 } } },
                new WaveDefinition { waveNumber = 2, paceTag = "peak", delayBeforeWave = 0.5f, nextWaveDelay = 4f, waitForClear = true,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = mountedRaider, count = 1, preferredRow = 1 },
                        new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 0 } } },
                new WaveDefinition { waveNumber = 3, paceTag = "rest", delayBeforeWave = 0.2f, nextWaveDelay = 0.5f, waitForClear = true,
                    enemies = new EnemySpawnEntry[0] }, // train pass, spawn gap
                new WaveDefinition { waveNumber = 4, paceTag = "pressure", delayBeforeWave = 0.5f, nextWaveDelay = 4f, waitForClear = false,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = pistolMook, count = 4, preferredRow = 0 },
                        new EnemySpawnEntry { enemyData = dynamiteBandit, count = 1, preferredRow = 1 } } },
                new WaveDefinition { waveNumber = 5, paceTag = "peak", delayBeforeWave = 0.5f, nextWaveDelay = 4f, waitForClear = true,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = mountedRaider, count = 1, preferredRow = 1 },
                        new EnemySpawnEntry { enemyData = pistolMook, count = 2, preferredRow = 0 } } },
                new WaveDefinition { waveNumber = 6, paceTag = "finale", delayBeforeWave = 0.5f, nextWaveDelay = 2.5f, waitForClear = true,
                    enemies = new[] {
                        new EnemySpawnEntry { enemyData = dynamiteBandit, count = 2, preferredRow = 0 },
                        new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 1 } } },
            };
            EditorUtility.SetDirty(stage1_3);
        }


        private static void CreateChapterAndVisualAssets()
        {
            CreateFolderRecursive(DATA_PATH + "Chapters");
            CreateFolderRecursive(DATA_PATH + "Visuals");
            string art = "Assets/HillNGun/Art/StageVisual/Dust/";
            var noon = LoadSpriteAt(art + "stage_visual_dust_1_1_noon.png");
            var sunset = LoadSpriteAt(art + "stage_visual_dust_1_3_sunset.png");
            var night = LoadSpriteAt(art + "stage_visual_rex_night.png");

            var v11 = CreateOrLoad<StageVisualData>(DATA_PATH + "Visuals/visual_dust_1_1_noon.asset");
            StageVisualData.EnsureFiveLayers(v11);
            v11.id = "visual_dust_1_1_noon"; v11.timeOfDay = TimeOfDay.Noon;
            v11.gradeTint = Color.white; v11.gradeExposure = 1f;
            v11.dustMotes = 12; v11.dustMotesLowTier = 4; v11.tumbleweeds = 1; v11.saloonSignSway = true;
            v11.conceptCoverDim = 0.4f;
            // Temp: full concept PNG on L2 (town); L3 empty combat lane
            v11.layers[0].sprite = noon; v11.layers[0].tint = new Color(0.85f, 0.92f, 1f, 0.55f);
            v11.layers[2].sprite = noon; v11.layers[2].tint = Color.white;
            v11.layers[3].sprite = null; // combat lane clear
            EditorUtility.SetDirty(v11);

            var v12 = CreateOrLoad<StageVisualData>(DATA_PATH + "Visuals/visual_dust_1_2_afternoon.asset");
            StageVisualData.EnsureFiveLayers(v12);
            v12.id = "visual_dust_1_2_afternoon"; v12.timeOfDay = TimeOfDay.Afternoon;
            v12.gradeTint = new Color(1f, 0.92f, 0.75f); v12.gradeExposure = 1.05f;
            v12.dustMotes = 14; v12.dustMotesLowTier = 5; v12.tumbleweeds = 1; v12.saloonSignSway = true;
            v12.conceptCoverDim = 0.4f;
            v12.layers[0].sprite = noon; v12.layers[0].tint = new Color(1f, 0.9f, 0.7f, 0.5f);
            v12.layers[2].sprite = noon; v12.layers[2].tint = new Color(1f, 0.93f, 0.8f, 1f);
            v12.layers[3].sprite = null;
            EditorUtility.SetDirty(v12);

            var v13 = CreateOrLoad<StageVisualData>(DATA_PATH + "Visuals/visual_dust_1_3_sunset.asset");
            StageVisualData.EnsureFiveLayers(v13);
            v13.id = "visual_dust_1_3_sunset"; v13.timeOfDay = TimeOfDay.Sunset;
            // Sunset empty plate is already graded: no extra sunset tint / overlay (would double-warm it)
            v13.gradeTint = Color.white; v13.gradeExposure = 1f;
            v13.dustMotes = 10; v13.dustMotesLowTier = 3; v13.tumbleweeds = 1; v13.saloonSignSway = true;
            v13.conceptCoverDim = 0.45f;
            v13.lanternPositions = new[] { new Vector2(5f, 10f), new Vector2(19f, 10f) };
            // bg_street_dust_sunset_empty (ArtSource/street_bg/empty) lives in stage_visual_dust_1_3_sunset.png
            v13.layers[0].sprite = sunset; v13.layers[0].tint = new Color(1f, 1f, 1f, 0.5f);
            v13.layers[2].sprite = sunset; v13.layers[2].tint = Color.white;
            v13.layers[3].sprite = null;
            EditorUtility.SetDirty(v13);

            var vBoss = CreateOrLoad<StageVisualData>(DATA_PATH + "Visuals/visual_rex_night.asset");
            StageVisualData.EnsureFiveLayers(vBoss);
            vBoss.id = "visual_rex_night"; vBoss.timeOfDay = TimeOfDay.Night;
            vBoss.gradeTint = new Color(0.55f, 0.65f, 1f); vBoss.gradeExposure = 0.85f;
            vBoss.dustMotes = 8; vBoss.dustMotesLowTier = 2; vBoss.tumbleweeds = 0; vBoss.saloonSignSway = false;
            vBoss.conceptCoverDim = 0.35f;
            vBoss.lanternPositions = new[] { new Vector2(4f, 9f), new Vector2(12f, 11f), new Vector2(20f, 9f) };
            vBoss.layers[0].sprite = night; vBoss.layers[0].tint = new Color(0.6f, 0.7f, 1f, 0.55f);
            vBoss.layers[2].sprite = night; vBoss.layers[2].tint = Color.white;
            vBoss.layers[3].sprite = null;
            EditorUtility.SetDirty(vBoss);

            var s11 = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_1.asset");
            var s12 = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_2.asset");
            var s13 = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_3.asset");
            var sb = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_boss.asset");
            if (s11) { s11.visual = v11; EditorUtility.SetDirty(s11); }
            if (s12) { s12.visual = v12; EditorUtility.SetDirty(s12); }
            if (s13) { s13.visual = v13; EditorUtility.SetDirty(s13); }
            if (sb) { sb.visual = vBoss; EditorUtility.SetDirty(sb); }

            var chapter = CreateOrLoad<ChapterData>(DATA_PATH + "Chapters/chapter_dust_town.asset");
            chapter.chapterId = "dust_town";
            chapter.index = 1;
            chapter.displayNameKey = "region.dusty_town";
            chapter.bossId = "boss_rex";
            chapter.bountyReward = 1000;
            chapter.paletteId = "sand_saloon";
            chapter.signatureHazards = new[] { "explosive_barrel", "saloon_sign", "tumbleweed" };
            chapter.stages = new[] { s11, s12, s13, sb };
            EditorUtility.SetDirty(chapter);
            Debug.Log("[GameSetup] Chapter dust_town + StageVisualData linked");
        }

        private static Sprite LoadSpriteAt(string path)
        {
            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null) return spr;
            // Import as sprite if texture exists without sprite type
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogWarning("[GameSetup] Missing art: " + path);
                return null;
            }
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return spr;
        }

        private static void CreateBossAsset()
        {
            var bossRex = CreateOrLoad<BossData>(DATA_PATH + "Bosses/boss_rattlesnake_rex.asset");
            bossRex.id = "rattlesnake_rex";
            bossRex.nameKey = "pve.boss.rattlesnake_rex";
            bossRex.maxHp = 24000; // 03_balance_data.md: 렉스 HP 24,000
            bossRex.hitRadius = 1.5f;
            bossRex.moveSpeed = 1.5f;
            bossRex.baseColor = new Color(0.5f, 0.3f, 0.2f);
            bossRex.scale = 2.5f;
            bossRex.entryDuration = 3f;
            bossRex.entryStartPosition = new Vector2(12f, 20f);
            bossRex.battlePosition = new Vector2(12f, 14f);
            // HP scale: human players use partyHpMultipliers; each AI ally adds aiAllyHpAdd.
            // Default PvE (1 human + 2 AI): 1.0 + 2*1.0 = ×3.0 → 72,000 (tuned for 90~120s autoplay) (target clear 90~120s).
            bossRex.partyHpMultipliers = new float[] { 1f, 1.8f, 2.6f };
            bossRex.aiAllyHpAdd = 1.0f;

            var pistolMook = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA_PATH + "Enemies/enemy_pistol_mook.asset");

            bossRex.phases = new BossPhaseData[]
            {
                new BossPhaseData
                {
                    phase = BossPhase.Phase1,
                    hpThresholdPercent = 1f,
                    usesFanShot = true,
                    fanShotCount = 5,
                    fanShotAngle = 60f,
                    fanShotDamage = 150,
                    fanShotInterval = 3f,
                    fanShotWarning = 0.8f,
                    fanShotBulletSpeed = 9f,
                    fanShotBulletRadius = 0.2f,
                    usesTailSweep = false, // 1페이즈: 부채꼴만
                    summonsMooks = false,
                    enrageSpeedMultiplier = 1f,
                    enrageAttackSpeedMultiplier = 1f,
                    movesHorizontally = true,
                    moveRange = 8f,
                    moveInterval = 4f
                },
                new BossPhaseData
                {
                    phase = BossPhase.Phase2,
                    hpThresholdPercent = 0.6f,
                    usesFanShot = true,
                    fanShotCount = 7,
                    fanShotAngle = 80f,
                    fanShotDamage = 150,
                    fanShotInterval = 2.5f,
                    fanShotWarning = 0.6f,
                    fanShotBulletSpeed = 9f,
                    fanShotBulletRadius = 0.2f,
                    usesTailSweep = true,
                    tailSweepDamage = 400,   // 2페이즈: 꼬리 휩쓸기 400, 바닥 경고 1.0s
                    tailSweepInterval = 6f,
                    tailSweepWarning = 1.0f,
                    tailSweepWidth = 6f,
                    tailSweepYMin = 0f,
                    tailSweepYMax = 8.5f,
                    summonsMooks = true,
                    mookToSummon = pistolMook,
                    mookSummonCount = 2,
                    summonInterval = 15f,
                    enrageSpeedMultiplier = 1.2f,
                    enrageAttackSpeedMultiplier = 1.2f,
                    movesHorizontally = true,
                    moveRange = 10f,
                    moveInterval = 3f
                },
                new BossPhaseData
                {
                    phase = BossPhase.Phase3,
                    hpThresholdPercent = 0.25f,
                    usesFanShot = true,
                    fanShotCount = 7,
                    fanShotAngle = 100f,
                    fanShotDamage = 150,
                    fanShotInterval = 3f,          // 3연속 난사로 대체 → 경고 간격은 1페이즈와 같게 (decision D18)
                    fanShotWarning = 0.5f,
                    fanShotBulletSpeed = 9f,
                    fanShotBulletRadius = 0.2f,
                    usesTailSweep = false, // 3페이즈: 지면 부채꼴 난사 (꼬리 휩쓸기 없음)
                    fanShotBurstCount = 3,      // 난사: 경고 1회당 3연속 부채꼴 (decision D9)
                    fanShotBurstInterval = 0.35f,
                    coverDamageMul = 2f,        // 3페이즈 엄폐물 피해 x2
                    battleY = 12.5f,            // 3페이즈: 앞으로 내려와 전장 압박
                    summonsMooks = false,
                    enrageSpeedMultiplier = 1.5f,
                    enrageAttackSpeedMultiplier = 1f, // 문서에 공격 가속 없음 (D18)
                    movesHorizontally = true,
                    moveRange = 12f,
                    moveInterval = 2f
                }
            };
            EditorUtility.SetDirty(bossRex);

            var bossStage = CreateOrLoad<StageData>(DATA_PATH + "Stages/stage_dusty_town_boss.asset");
            bossStage.id = "dusty_town_boss";
            bossStage.chapterId = "dust_town"; bossStage.stageIndex = 10;
            bossStage.stageType = StageType.Boss;
            bossStage.nameKey = "pve.boss.rattlesnake_rex";
            bossStage.stageNumber = 1;
            bossStage.subStageNumber = 4;
            bossStage.isBossStage = true;
            bossStage.bossData = bossRex;
            bossStage.gaugeMax = 0;
            bossStage.waveCount = 0;
            bossStage.stageTimeMin = 90f;
            bossStage.stageTimeMax = 120f;
            bossStage.starTimeUnderSec = 110f;
            bossStage.starRequireNoDowned = true;
            bossStage.themeId = "sunset_showdown";
            bossStage.enemyRowsY = new float[] { 11f, 13.5f, 16f };
            // 06 §7-4: wagon+troughs front, barrels mid, doors back, rocks L/R fixed
            bossStage.coverPlacements = new CoverPlacement[] {
                new CoverPlacement { coverId = "cover_swing_door", rowIndex = 0, xPosition = 5f },
                new CoverPlacement { coverId = "cover_swing_door", rowIndex = 0, xPosition = 19f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 1, xPosition = 7f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 1, xPosition = 17f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 2, xPosition = 6f },
                new CoverPlacement { coverId = "cover_wagon", rowIndex = 2, xPosition = 12f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 2, xPosition = 18f },
                new CoverPlacement { coverId = "cover_rock", rowIndex = 2, xPosition = 2f },
                new CoverPlacement { coverId = "cover_rock", rowIndex = 2, xPosition = 22f },
            };
            bossStage.hazards = new HazardPlacement[] {
                new HazardPlacement { type = HazardType.ExplosiveBarrel, x = 9f, rowIndex = 2 },
                new HazardPlacement { type = HazardType.ExplosiveBarrel, x = 15f, rowIndex = 2 },
            };
            EditorUtility.SetDirty(bossStage);
        }

        /// <summary>Offline progression numbers (FP 522-529). Amounts not in the docs are proposals (decision D19).</summary>
        private static WeaponData ShopGun(string id, System.Action<WeaponData> set)
        {
            var w = CreateOrLoad<WeaponData>(DATA_PATH + "Weapons/" + id + ".asset");
            w.id = id;
            w.nameKey = "weapon." + id;
            w.descKey = "weapon." + id + ".desc";
            w.fireMode = FireMode.Auto;
            w.projectilesPerShot = 1; w.spreadDeg = 0f;
            w.chargeMinTime = 0f; w.chargeMaxTime = 0f; w.chargeDamageMul = 1f;
            w.muzzleForwardOffset = 1.0f; w.coverDamageMul = 1f;
            w.holdToRepeat = false; w.cockAnimation = false;
            set(w);
            EditorUtility.SetDirty(w);
            return w;
        }

        /// <summary>
        /// Gun catalog (t48u). Starting guns = 03 numbers (unchanged). Shop guns are proposals from the design's gun list
        /// (리볼버·산탄총·레버액션 소총·개틀링건; 다이너마이트 = the existing grenade), kept inside each role's weapon family
        /// and priced against the bounty economy: a full PvE run ≈ $1,300-1,500, a PvP win $150, a skin $1,500.
        /// </summary>
        private static WeaponCatalogData CreateWeaponCatalog()
        {
            var wB = AssetDatabase.LoadAssetAtPath<WeaponData>(DATA_PATH + "Weapons/wpn_buster.asset");
            var wR = AssetDatabase.LoadAssetAtPath<WeaponData>(DATA_PATH + "Weapons/wpn_raven.asset");
            var wV = AssetDatabase.LoadAssetAtPath<WeaponData>(DATA_PATH + "Weapons/wpn_volt.asset");

            var peacemaker = ShopGun("wpn_peacemaker", w =>
            {   // 6-shot revolver: smaller rounds, more of them, quicker cock
                w.damagePerProjectile = 380; w.projectileSpeed = 22f; w.projectileRadius = 0.25f; w.range = 7.5f;
                w.fireInterval = 0.45f; w.gaugeCells = 6; w.shotsPerCell = 1; w.rechargePerCell = 0.9f;
                w.cockAnimation = true;
            });
            var coach = ShopGun("wpn_coachgun", w =>
            {   // coach gun (double barrel): one more pellet, a little more reach
                w.damagePerProjectile = 140; w.projectilesPerShot = 6; w.spreadDeg = 32f; w.projectileSpeed = 18f;
                w.projectileRadius = 0.3f; w.range = 5.5f; w.fireInterval = 0.6f; w.gaugeCells = 3; w.shotsPerCell = 1;
                w.rechargePerCell = 1.1f;
            });
            var lever = ShopGun("wpn_lever_action", w =>
            {   // lever-action rifle: no charge, quick follow-up shots, 4 rounds
                w.damagePerProjectile = 560; w.projectileSpeed = 30f; w.projectileRadius = 0.14f; w.range = 11f;
                w.fireInterval = 0.6f; w.gaugeCells = 4; w.shotsPerCell = 1; w.rechargePerCell = 1.3f;
                w.cockAnimation = true;
            });
            var gatling = ShopGun("wpn_gatling", w =>
            {   // Gatling: hold to keep firing (touch: hold the aim), 18 rounds, low damage per round
                w.damagePerProjectile = 120; w.projectileSpeed = 20f; w.projectileRadius = 0.2f; w.range = 6f;
                w.fireInterval = 0.1f; w.gaugeCells = 3; w.shotsPerCell = 6; w.rechargePerCell = 1.4f;
                w.holdToRepeat = true;
            });

            var cat = CreateOrLoad<WeaponCatalogData>(DATA_PATH + "Progression/weapon_catalog.asset");
            cat.offers = new[]
            {
                new WeaponOffer { weapon = wB, displayName = "더블배럴 산탄총", description = "근거리 산탄 5발 (기본)", ownedByDefault = true, characterIds = new[] { "buster" } },
                new WeaponOffer { weapon = wR, displayName = "사냥용 라이플", description = "충전 저격 (기본)", ownedByDefault = true, characterIds = new[] { "raven" } },
                new WeaponOffer { weapon = wV, displayName = "대형 리볼버", description = "한 발씩 정확하게 (기본)", ownedByDefault = true, characterIds = new[] { "volt" } },
                new WeaponOffer { weapon = peacemaker, displayName = "피스메이커 리볼버", description = "6연발 · 빠른 격발 · 한 발 380", cost = 1800, characterIds = new[] { "volt" } },
                new WeaponOffer { weapon = coach, displayName = "코치건", description = "산탄 6발 · 사거리 5.5", cost = 2000, characterIds = new[] { "buster" } },
                new WeaponOffer { weapon = lever, displayName = "레버액션 소총", description = "충전 없이 4연발 · 사거리 11", cost = 2400, characterIds = new[] { "raven" } },
                new WeaponOffer { weapon = gatling, displayName = "개틀링건", description = "누르고 있으면 연사 · 18발", cost = 3500, characterIds = new[] { "buster" } },
            };
            EditorUtility.SetDirty(cat);
            return cat;
        }

        private static void CreateProgressionAsset()
        {
            var p = CreateOrLoad<ProgressionData>(DATA_PATH + "Progression/progression_default.asset");
            p.weaponCatalog = CreateWeaponCatalog();
            p.startingBounty = 0;
            p.bountyPerStageClear = 100;
            p.bountyPerBossDefeat = 1000; // FP 220: 렉스 현상금 $1,000
            p.bountyPvPWin = 150;
            p.bountyPvPDraw = 50;
            p.bountyPvPLoss = 0;          // FP 526: PvP는 승리 시 현상금
            p.stageBountyEveryClear = true;
            p.xpPerStageClear = 30;
            p.xpBossDefeat = 120;
            p.xpPvEFailedRun = 15;
            p.xpPvPWin = 80;
            p.xpPvPDraw = 50;
            p.xpPvPLoss = 40;             // FP 525: 판마다 경험치
            p.badgeXpBase = 200;
            p.badgeXpPerLevel = 100;
            p.maxBadgeLevel = 50;
            p.bountyPerBadgeLevel = 200;  // FP 525: 레벨마다 현상금
            p.masteryXpPerMatch = 20;
            p.masteryXpWinBonus = 15;
            p.masteryLevelXp = new int[] { 0, 100, 250, 500, 900, 1500 };
            p.masteryTitleKeys = new string[] { "mastery.title.1", "mastery.title.2", "mastery.title.3", "mastery.title.4", "mastery.title.5", "mastery.title.6" };
            p.startingCharacterIds = new string[] { "buster", "raven", "volt" };
            p.regions = new RegionDefinition[]
            {
                new RegionDefinition { id = "dusty_town", nameKey = "region.dusty_town",
                    stageIds = new[] { "dusty_town_1_1", "dusty_town_1_2", "dusty_town_1_3", "dusty_town_boss" },
                    bossStageId = "dusty_town_boss", nextRegionId = "railroad_canyon", bossNameKey = "pve.boss.rattlesnake_rex", bossBounty = 1000 },
                // FP 220-228 지역·보스·현상금 표 (철도 협곡부터는 스테이지가 프로토타입 범위 밖, 해금 표시만)
                new RegionDefinition { id = "railroad_canyon", nameKey = "region.railroad_canyon", stageIds = new string[0], nextRegionId = "silver_mine", bossNameKey = "boss.baron", bossBounty = 2500 },
                new RegionDefinition { id = "silver_mine", nameKey = "region.silver_mine", stageIds = new string[0], nextRegionId = "border_fort", bossNameKey = "boss.may", bossBounty = 4000 },
                new RegionDefinition { id = "border_fort", nameKey = "region.border_fort", stageIds = new string[0], nextRegionId = "ghost_town", bossNameKey = "boss.grimshaw", bossBounty = 6000 },
                new RegionDefinition { id = "ghost_town", nameKey = "region.ghost_town", stageIds = new string[0], bossNameKey = "boss.nameless", bossBounty = 10000 },
            };
            var skins = new System.Collections.Generic.List<SkinDefinition>();
            foreach (var c in new[] { "buster", "raven", "volt" })
            {
                skins.Add(new SkinDefinition { id = c + "_default", characterId = c, nameKey = "char." + c + ".name", unlock = SkinUnlockType.Default, tint = Color.white });
                // FP 528: 보스를 잡으면 지역 스킨 해금
                skins.Add(new SkinDefinition { id = c + "_dusty", characterId = c, nameKey = "skin." + c + ".dusty", unlock = SkinUnlockType.BossReward, requirementId = "dusty_town", tint = new Color(0.86f, 0.72f, 0.52f) });
                // FP 526: 일부 스킨은 현상금으로 구매
                skins.Add(new SkinDefinition { id = c + "_gold", characterId = c, nameKey = "skin." + c + ".gold", unlock = SkinUnlockType.Purchase, cost = 1500, tint = new Color(1f, 0.85f, 0.35f), recolorStrength = 0.8f });
                // FP 527: 숙련도 보상은 능력치 대신 스킨·칭호
                skins.Add(new SkinDefinition { id = c + "_veteran", characterId = c, nameKey = "skin." + c + ".veteran", unlock = SkinUnlockType.Mastery, masteryLevel = 3, tint = new Color(0.6f, 0.75f, 0.95f) });
            }
            p.skins = skins.ToArray();
            EditorUtility.SetDirty(p);
        }

        /// <summary>Menu theme tokens (05 §5.1).</summary>
        private static void CreateUIThemeAsset()
        {
            var t = CreateOrLoad<UIThemeData>(DATA_PATH + "UI/ui_theme_default.asset");
            t.referenceHeight = 720f;
            t.mobileRenderHeight = 720;
            EditorUtility.SetDirty(t);
        }

        /// <summary>External-service toggles; everything here is an offline stub (D14).</summary>
        private static void CreateExternalServicesAsset()
        {
            var e = CreateOrLoad<ExternalServicesData>(DATA_PATH + "External/external_services.asset");
            e.rewardedAdsEnabled = true;
            e.simulateAdSuccess = true;   // prototype: lets the PvE continue flow be tested without an ad SDK
            e.simulatedAdSeconds = 1.5f;
            e.maxContinuesPerRun = 1;
            e.iapEnabled = false;
            e.products = new IapProductDefinition[]
            {
                // FP 513-517 수익 모델 (결정). 가격은 스토어 SDK가 줌 → 미정
                new IapProductDefinition { id = "season_pass_premium", nameKey = "shop.product.season_pass", descKey = "shop.product.season_pass.desc", kind = IapProductKind.SeasonPass },
                new IapProductDefinition { id = "skin_bundle_launch", nameKey = "shop.product.skin_bundle", descKey = "shop.product.skin_bundle.desc", kind = IapProductKind.SkinBundle },
                new IapProductDefinition { id = "rank_pass", nameKey = "shop.product.rank_pass", descKey = "shop.product.rank_pass.desc", kind = IapProductKind.RankPass },
            };
            e.analyticsLocalCsv = true;
            e.analyticsFileName = "analytics_local.csv";
            e.onlineMatchmakingEnabled = false;
            e.useSpineCharacterViews = false;
            EditorUtility.SetDirty(e);
        }

        /// <summary>First PvE onboarding steps (FP 202, D13). Each step waits for the action or times out.</summary>
        private static void CreateTutorialAsset()
        {
            var t = CreateOrLoad<TutorialData>(DATA_PATH + "Tutorial/tutorial_default.asset");
            t.enabled = true;
            t.stageIndex = 0;
            t.startDelay = 1.0f;
            TutorialStep S(string key, TutorialTrigger trig, float min, float max, bool pc = true) =>
                new TutorialStep { textKey = "tutorial." + key, textKeyDesktop = pc ? "tutorial." + key + ".pc" : null, trigger = trig, minSeconds = min, maxSeconds = max };
            t.steps = new[]
            {
                S("move", TutorialTrigger.Move, 1.5f, 10f),
                S("row", TutorialTrigger.RowChange, 1.5f, 10f),
                S("shoot", TutorialTrigger.Shoot, 1.5f, 12f),
                S("gauge", TutorialTrigger.Timer, 4f, 0f, false),
                S("roll", TutorialTrigger.Roll, 1.5f, 10f),
                S("grenade", TutorialTrigger.Grenade, 1.5f, 12f),
                S("skill", TutorialTrigger.Skill, 1.5f, 12f),
                S("revive", TutorialTrigger.Timer, 5f, 0f, false),
            };
            EditorUtility.SetDirty(t);
        }

        /// <summary>Movement / roll feel (logic) + motion view feel (view). Design values (speeds, row time, roll distance/i-frames/cooldown) stay in CharacterData/RollData.</summary>
        private static void CreateMotionFeelAssets()
        {
            var m = CreateOrLoad<MovementFeelData>(DATA_PATH + "Feel/movement_feel.asset");
            m.accelTime = 0.09f;
            m.decelTime = 0.07f;
            m.turnDecelMul = 1.8f;
            m.accelCurve = new AnimationCurve(new Keyframe(0f, 1.6f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0.6f));
            m.analogDeadzone = 0.12f;
            m.analogFullSpeedAt = 0.55f;
            m.analogMinSpeedFactor = 0.4f;
            m.rowChangeEase = 1f;
            m.rollEasePower = 2.2f;
            m.rollRecoveryTime = 0.08f;
            m.rollRecoveryMoveMul = 0.45f;
            m.recoveryBlocksActions = true;
            m.rollExitCarry = 0.35f;
            m.horizontalRollOnly = true;
            m.inputBufferWindow = -1f; // use RollData.inputBuffer (0.1s, 01 §9)
            EditorUtility.SetDirty(m);

            var v = CreateOrLoad<MotionViewData>(DATA_PATH + "Feel/motion_view.asset");
            v.heroArtFacesRight = false; // deck hero art faces left
            v.enemyArtFacesRight = false;
            EditorUtility.SetDirty(v);
        }

        /// <summary>3D actor view (view only): toggle/look/models + procedural animation knobs + the toon material in Resources (so the shader ships).</summary>
        private static void CreateActor3DAssets()
        {
            var a = CreateOrLoad<Actor3DData>(DATA_PATH + "Feel/actor3d.asset");
            var fresh = ScriptableObject.CreateInstance<Actor3DData>();
            EditorUtility.CopySerialized(fresh, a);
            Object.DestroyImmediate(fresh);
            a.models = Actor3DDefaults.Models();
            EditorUtility.SetDirty(a);

            var an = CreateOrLoad<Actor3DAnimData>(DATA_PATH + "Feel/actor3d_anim.asset");
            var freshAn = ScriptableObject.CreateInstance<Actor3DAnimData>();
            EditorUtility.CopySerialized(freshAn, an);
            Object.DestroyImmediate(freshAn);
            EditorUtility.SetDirty(an);

            const string resDir = "Assets/HillNGun/Resources/Actor3D";
            if (!AssetDatabase.IsValidFolder("Assets/HillNGun/Resources")) AssetDatabase.CreateFolder("Assets/HillNGun", "Resources");
            if (!AssetDatabase.IsValidFolder(resDir)) AssetDatabase.CreateFolder("Assets/HillNGun/Resources", "Actor3D");
            var shader = Shader.Find("HillNGun/Toon");
            if (shader == null) { Debug.LogWarning("[GameSetup] HillNGun/Toon shader not found; 3D actors fall back to sprites"); return; }
            string matPath = resDir + "/hng_toon.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = shader;
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
        }

        private static void CreateBotBehaviorAsset()
        {
            var botBehavior = CreateOrLoad<BotBehaviorData>(DATA_PATH + "AI/bot_behavior_default.asset");
            botBehavior.decisionInterval = 0.3f;
            botBehavior.distanceWeight = 1f;
            botBehavior.lowHpWeight = 0.5f;
            botBehavior.aimDuration = 0.4f;
            botBehavior.aimJitter = 5f;
            botBehavior.rowChangeChance = 0.3f;
            botBehavior.dodgeHpThreshold = 0.5f;
            botBehavior.dodgeChance = 0.4f;
            botBehavior.grenadeUseChance = 0.3f;
            botBehavior.skillUseChance = 0.2f;
            botBehavior.revivePriority = 0.6f;
            botBehavior.reviveSeekDistance = 8f;
            botBehavior.useSkills = true;
            botBehavior.repairCoverBelowRatio = 0.6f;
            botBehavior.useGrenades = true;
            botBehavior.grenadeMinInterval = 4f;
            botBehavior.pveAllyGrenadeMinInterval = 45f;
            botBehavior.pveAllyShotCooldownMul = 1.25f;
            botBehavior.reviveAllies = true;
            EditorUtility.SetDirty(botBehavior);
        }

        private static void CreateAllScenes()
        {
            CreateTitleScene();
            CreateModeSelectScene();
            CreateCharacterSelectScene();
            CreatePvEMatchScene();
            CreatePvPMatchScene();
            CreateResultScene();
        }

        private static void CreateTitleScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateBasicSceneObjects();

            var gameFlowGO = new GameObject("GameFlowManager");
            gameFlowGO.AddComponent<GameFlowManager>();

            var titleUIGO = new GameObject("TitleScreenUI");
            titleUIGO.AddComponent<TitleScreenUI>();

            var localizationGO = new GameObject("LocalizationManager");
            AssignStringsFile(localizationGO.AddComponent<LocalizationManager>());

            EditorSceneManager.SaveScene(scene, SCENE_PATH + "Title.unity");
        }

        private static void CreateModeSelectScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateBasicSceneObjects();

            // Lobby / home hub (홈·스테이지·캐릭터·상점·설정). Scene keeps the ModeSelect name for the flow manager.
            var lobbyGO = new GameObject("LobbyUI");
            lobbyGO.AddComponent<LobbyUI>();

            var localizationGO = new GameObject("LocalizationManager");
            AssignStringsFile(localizationGO.AddComponent<LocalizationManager>());

            EditorSceneManager.SaveScene(scene, SCENE_PATH + "ModeSelect.unity");
        }

        private static void CreateCharacterSelectScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateBasicSceneObjects();

            var charSelectGO = new GameObject("CharacterSelectUI");
            var charSelectUI = charSelectGO.AddComponent<CharacterSelectUI>();

            var charBuster = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_buster.asset");
            var charRaven = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_raven.asset");
            var charVolt = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_volt.asset");

            var field = typeof(CharacterSelectUI).GetField("_availableCharacters", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(charSelectUI, new CharacterData[] { charBuster, charRaven, charVolt });
            }

            var localizationGO = new GameObject("LocalizationManager");
            AssignStringsFile(localizationGO.AddComponent<LocalizationManager>());

            EditorSceneManager.SaveScene(scene, SCENE_PATH + "CharacterSelect.unity");
        }

        private static void CreatePvEMatchScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateBasicSceneObjects();
            CreateBattlefieldAndSystems();

            var pveManagerGO = new GameObject("PvEMatchManager");
            var pveManager = pveManagerGO.AddComponent<PvEMatchManager>();

            var stage1 = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_1.asset");
            var stage2 = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_2.asset");
            var stage3 = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_3.asset");
            var bossStage = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_boss.asset");

            var charBuster = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_buster.asset");
            var charRaven = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_raven.asset");
            var charVolt = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_volt.asset");
            var botBehavior = AssetDatabase.LoadAssetAtPath<BotBehaviorData>(DATA_PATH + "AI/bot_behavior_default.asset");

            SetPrivateField(pveManager, "_stages", new StageData[] { stage1, stage2, stage3, bossStage });
            SetPrivateField(pveManager, "_characterDatas", new CharacterData[] { charBuster, charRaven, charVolt });
            SetPrivateField(pveManager, "_botBehavior", botBehavior);

            var pveHUDGO = new GameObject("PvEHUD");
            pveHUDGO.AddComponent<PvEHUD>();

            var tutorialGO = new GameObject("TutorialOverlay");
            tutorialGO.AddComponent<TutorialOverlay>();

            CreateMobileTouchControls();

            var pauseGO = new GameObject("PauseMenuController");
            pauseGO.AddComponent<PauseMenuController>();

            EditorSceneManager.SaveScene(scene, SCENE_PATH + "PvE_Match.unity");
        }

        private static void CreatePvPMatchScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateBasicSceneObjects();
            CreateBattlefieldAndSystems();

            var pvpManagerGO = new GameObject("PvPMatchManager");
            var pvpManager = pvpManagerGO.AddComponent<PvPMatchManager>();

            var charBuster = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_buster.asset");
            var charRaven = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_raven.asset");
            var charVolt = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_volt.asset");
            var botBehavior = AssetDatabase.LoadAssetAtPath<BotBehaviorData>(DATA_PATH + "AI/bot_behavior_default.asset");

            SetPrivateField(pvpManager, "_characterDatas", new CharacterData[] { charBuster, charRaven, charVolt });
            SetPrivateField(pvpManager, "_botBehavior", botBehavior);

            var pvpHUDGO = new GameObject("PvPHUD");
            pvpHUDGO.AddComponent<PvPHUD>();

            CreateMobileTouchControls();

            var pauseGO = new GameObject("PauseMenuController");
            pauseGO.AddComponent<PauseMenuController>();

            EditorSceneManager.SaveScene(scene, SCENE_PATH + "PvP_Match.unity");
        }

        /// <summary>On-screen joysticks / buttons for touch devices (hidden on desktop). Receiver is set by the match manager.</summary>
        private static void CreateMobileTouchControls()
        {
            var touchControls = AssetDatabase.LoadAssetAtPath<TouchControlsData>(DATA_PATH + "Input/touch_controls.asset");
            var go = new GameObject("MobileTouchControls");
            var mtc = go.AddComponent<MobileTouchControls>();
            if (touchControls != null) SetPrivateField(mtc, "_controlsData", touchControls);
            else Debug.LogWarning("[GameSetup] touch_controls.asset not found - on-screen controls disabled");
        }

        private static void CreateResultScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateBasicSceneObjects();

            var resultUIGO = new GameObject("ResultUI");
            resultUIGO.AddComponent<ResultUI>();

            var localizationGO = new GameObject("LocalizationManager");
            AssignStringsFile(localizationGO.AddComponent<LocalizationManager>());

            EditorSceneManager.SaveScene(scene, SCENE_PATH + "Result.unity");
        }

        private static void CreateBasicSceneObjects()
        {
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.2f, 0.15f, 0.1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraGO.tag = "MainCamera";
            cameraGO.AddComponent<AudioListener>();
            
            CreateRuntimeAssetManager();
        }

        private static void CreateRuntimeAssetManager()
        {
            var spriteLibrary = AssetDatabase.LoadAssetAtPath<SpriteLibrary>(DATA_PATH + "SpriteLibrary.asset");
            var audioLibrary = AssetDatabase.LoadAssetAtPath<AudioLibrary>(DATA_PATH + "AudioLibrary.asset");
            
            var charBuster = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_buster.asset");
            var charRaven = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_raven.asset");
            var charVolt = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_volt.asset");
            
            var enemyPistol = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA_PATH + "Enemies/enemy_pistol_mook.asset");
            var enemyDynamite = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA_PATH + "Enemies/enemy_dynamite_bandit.asset");
            var enemyMounted = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA_PATH + "Enemies/enemy_mounted_raider.asset");
            
            var bossData = AssetDatabase.LoadAssetAtPath<BossData>(DATA_PATH + "Bosses/boss_rattlesnake_rex.asset");
            
            var stage1 = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_1.asset");
            var stage2 = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_2.asset");
            var stage3 = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_3.asset");
            var bossStage = AssetDatabase.LoadAssetAtPath<StageData>(DATA_PATH + "Stages/stage_dusty_town_boss.asset");
            
            var coverBarrel = AssetDatabase.LoadAssetAtPath<CoverData>(DATA_PATH + "Cover/cover_barrel.asset");
            var coverTrough = AssetDatabase.LoadAssetAtPath<CoverData>(DATA_PATH + "Cover/cover_trough.asset");
            var coverSwingDoor = AssetDatabase.LoadAssetAtPath<CoverData>(DATA_PATH + "Cover/cover_swing_door.asset");
            var coverWagon = AssetDatabase.LoadAssetAtPath<CoverData>(DATA_PATH + "Cover/cover_wagon.asset");
            var coverRock = AssetDatabase.LoadAssetAtPath<CoverData>(DATA_PATH + "Cover/cover_rock.asset");
            
            var botBehavior = AssetDatabase.LoadAssetAtPath<BotBehaviorData>(DATA_PATH + "AI/bot_behavior_default.asset");
            var battlefieldData = AssetDatabase.LoadAssetAtPath<BattlefieldData>(DATA_PATH + "Battlefield/battlefield_main.asset");
            var downedData = AssetDatabase.LoadAssetAtPath<DownedData>(DATA_PATH + "Downed/downed_default.asset");
            var feelData = AssetDatabase.LoadAssetAtPath<FeelData>(DATA_PATH + "Feel/feel_default.asset");
            var matchRules = AssetDatabase.LoadAssetAtPath<MatchRulesData>(DATA_PATH + "Match/match_rules_default.asset");
            var inputData = AssetDatabase.LoadAssetAtPath<InputData>(DATA_PATH + "Input/input_default.asset");
            var touchControls = AssetDatabase.LoadAssetAtPath<TouchControlsData>(DATA_PATH + "Input/touch_controls.asset");
            var progression = AssetDatabase.LoadAssetAtPath<ProgressionData>(DATA_PATH + "Progression/progression_default.asset");
            var uiTheme = AssetDatabase.LoadAssetAtPath<UIThemeData>(DATA_PATH + "UI/ui_theme_default.asset");
            var externalServices = AssetDatabase.LoadAssetAtPath<ExternalServicesData>(DATA_PATH + "External/external_services.asset");
            var tutorial = AssetDatabase.LoadAssetAtPath<TutorialData>(DATA_PATH + "Tutorial/tutorial_default.asset");
            var movementFeel = AssetDatabase.LoadAssetAtPath<MovementFeelData>(DATA_PATH + "Feel/movement_feel.asset");
            var motionView = AssetDatabase.LoadAssetAtPath<MotionViewData>(DATA_PATH + "Feel/motion_view.asset");
            var actor3D = AssetDatabase.LoadAssetAtPath<Actor3DData>(DATA_PATH + "Feel/actor3d.asset");
            var actor3DAnim = AssetDatabase.LoadAssetAtPath<Actor3DAnimData>(DATA_PATH + "Feel/actor3d_anim.asset");
            
            var assetManagerGO = new GameObject("RuntimeAssetManager");
            var assetManager = assetManagerGO.AddComponent<RuntimeAssetManager>();
            
            SetPrivateField(assetManager, "_spriteLibrary", spriteLibrary);
            SetPrivateField(assetManager, "_audioLibrary", audioLibrary);
            SetPrivateField(assetManager, "_characterDatas", new CharacterData[] { charBuster, charRaven, charVolt });
            SetPrivateField(assetManager, "_enemyDatas", new EnemyData[] { enemyPistol, enemyDynamite, enemyMounted });
            SetPrivateField(assetManager, "_bossDatas", bossData != null ? new BossData[] { bossData } : new BossData[0]);
            SetPrivateField(assetManager, "_stageDatas", new StageData[] { stage1, stage2, stage3, bossStage });
            SetPrivateField(assetManager, "_coverDatas", new CoverData[] { coverBarrel, coverTrough, coverSwingDoor, coverWagon, coverRock });
            SetPrivateField(assetManager, "_botBehavior", botBehavior);
            SetPrivateField(assetManager, "_battlefieldData", battlefieldData);
            SetPrivateField(assetManager, "_downedData", downedData);
            SetPrivateField(assetManager, "_feelData", feelData);
            SetPrivateField(assetManager, "_matchRules", matchRules);
            SetPrivateField(assetManager, "_inputData", inputData);
            SetPrivateField(assetManager, "_touchControlsData", touchControls);
            SetPrivateField(assetManager, "_progression", progression);
            SetPrivateField(assetManager, "_uiTheme", uiTheme);
            SetPrivateField(assetManager, "_externalServices", externalServices);
            SetPrivateField(assetManager, "_tutorial", tutorial);
            SetPrivateField(assetManager, "_movementFeel", movementFeel);
            SetPrivateField(assetManager, "_motionView", motionView);
            SetPrivateField(assetManager, "_actor3D", actor3D);
            SetPrivateField(assetManager, "_actor3DAnim", actor3DAnim);
            SetPrivateField(assetManager, "_stageThemes", AssetDatabase.LoadAssetAtPath<StageThemeLibrary>(StageThemeSetup.LIBRARY_PATH));
            SetPrivateField(assetManager, "_dropTable", AssetDatabase.LoadAssetAtPath<DropTableData>(DATA_PATH + "Drops/drop_table.asset"));
        }

        private static void CreateBattlefieldAndSystems()
        {
            var battlefieldData = AssetDatabase.LoadAssetAtPath<BattlefieldData>(DATA_PATH + "Battlefield/battlefield_main.asset");
            var feelData = AssetDatabase.LoadAssetAtPath<FeelData>(DATA_PATH + "Feel/feel_default.asset");
            var matchRules = AssetDatabase.LoadAssetAtPath<MatchRulesData>(DATA_PATH + "Match/match_rules_default.asset");
            var inputData = AssetDatabase.LoadAssetAtPath<InputData>(DATA_PATH + "Input/input_default.asset");
            var downedData = AssetDatabase.LoadAssetAtPath<DownedData>(DATA_PATH + "Downed/downed_default.asset");

            var battlefieldGO = new GameObject("BattlefieldSystem");
            var battlefieldSys = battlefieldGO.AddComponent<BattlefieldSystem>();
            SetPrivateField(battlefieldSys, "_data", battlefieldData);

            var rendererGO = new GameObject("BattlefieldRenderer");
            rendererGO.AddComponent<BattlefieldRenderer>();

            var combatGO = new GameObject("CombatSystem");
            var combatSys = combatGO.AddComponent<CombatSystem>();
            SetPrivateField(combatSys, "_feelData", feelData);
            SetPrivateField(combatSys, "_matchRules", matchRules);

            var inputGO = new GameObject("InputHandler");
            var inputHandler = inputGO.AddComponent<TouchInputHandler>();
            SetPrivateField(inputHandler, "_inputData", inputData);

            var loggerGO = new GameObject("GameLogger");
            loggerGO.AddComponent<GameLogger>();

            var shakeGO = new GameObject("ScreenShake");
            var screenShake = shakeGO.AddComponent<ScreenShake>();
            SetPrivateField(screenShake, "_feelData", feelData);

            var effectsGO = new GameObject("EffectsManager");
            effectsGO.AddComponent<EffectsManager>();
            var vfxGO = new GameObject("VfxPool");
            vfxGO.AddComponent<HillNGun.Feel.VfxPool>();

            var localizationGO = new GameObject("LocalizationManager");
            AssignStringsFile(localizationGO.AddComponent<LocalizationManager>());

            var audioGO = new GameObject("PlaceholderAudio");
            audioGO.AddComponent<PlaceholderAudio>();

            var reviveSystemGO = new GameObject("ReviveSystem");
            var reviveSystem = reviveSystemGO.AddComponent<ReviveSystem>();
            SetPrivateField(reviveSystem, "_downedData", downedData);
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static void CreateOrReplaceAsset(Object asset, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                CreateFolderRecursive(directory);
            }

            Object existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Points the LocalizationManager at Assets/HillNGun/Localization/strings_ko.csv (otherwise only built-in defaults load).</summary>
        private static void AssignStringsFile(LocalizationManager loc)
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/HillNGun/Localization/strings_ko.csv");
            if (loc != null && csv != null) SetPrivateField(loc, "_defaultStringsFile", csv);
            var en = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/HillNGun/Localization/strings_en.csv");
            if (loc != null && en != null) SetPrivateField(loc, "_languageFiles", new TextAsset[] { en });
            else Debug.LogWarning("[GameSetup] strings_ko.csv not found - localization uses built-in defaults");
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}

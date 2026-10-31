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
                warningIndicator = LoadSprite("Boss/warning_indicator")
            };

            // Keyed by CoverData.id (generated art: barrel / crate / wagon)
            library.covers = new CoverSprites[]
            {
                LoadCoverSprites("cover_barrel", "barrel"),
                LoadCoverSprites("cover_trough", "crate"),
                LoadCoverSprites("cover_swing_door", "crate"),
                LoadCoverSprites("cover_wagon", "wagon")
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
                downed = LoadSprite($"Characters/{id}_downed")
            };
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

        private static Sprite LoadSprite(string relativePath)
        {
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
            CreateBossAsset();
            CreateBotBehaviorAsset();
            CreateTouchControlsAsset();
        }

        private static void CreateTouchControlsAsset()
        {
            var touchControls = CreateOrLoad<TouchControlsData>(DATA_PATH + "Input/touch_controls.asset");
            touchControls.moveJoystickAnchor = new Vector2(0.15f, 0.25f);
            touchControls.moveJoystickSize = 150f;
            touchControls.moveKnobSize = 60f;
            touchControls.moveDeadzone = 0.1f;
            touchControls.aimJoystickAnchor = new Vector2(0.85f, 0.25f);
            touchControls.aimJoystickSize = 150f;
            touchControls.aimKnobSize = 60f;
            touchControls.aimDeadzone = 0.15f;
            touchControls.rollButtonAnchor = new Vector2(0.25f, 0.55f);
            touchControls.rollButtonSize = 70f;
            touchControls.grenadeButtonAnchor = new Vector2(0.35f, 0.45f);
            touchControls.grenadeButtonSize = 70f;
            touchControls.skillButtonAnchor = new Vector2(0.75f, 0.55f);
            touchControls.skillButtonSize = 80f;
            touchControls.pauseButtonAnchor = new Vector2(0.5f, 0.95f);
            touchControls.pauseButtonSize = 50f;
            touchControls.opacity = 0.6f;
            touchControls.safeAreaPadding = 20f;
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
            EditorUtility.SetDirty(mountedRaider);
        }

        private static void CreateStageAssets()
        {
            var pistolMook = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA_PATH + "Enemies/enemy_pistol_mook.asset");
            var dynamiteBandit = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA_PATH + "Enemies/enemy_dynamite_bandit.asset");
            var mountedRaider = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA_PATH + "Enemies/enemy_mounted_raider.asset");

            var stage1_1 = CreateOrLoad<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_1.asset");
            stage1_1.id = "dusty_town_1_1";
            stage1_1.nameKey = "pve.stage.dusty_town_1";
            stage1_1.stageNumber = 1;
            stage1_1.subStageNumber = 1;
            stage1_1.gaugeMax = 20;
            stage1_1.waveCount = 4;
            stage1_1.stageTimeMin = 60f;
            stage1_1.stageTimeMax = 90f;
            stage1_1.timeBetweenWaves = 10f;
            stage1_1.enemyRowsY = new float[] { 11f, 13.5f, 16f };
            stage1_1.waves = new WaveDefinition[]
            {
                new WaveDefinition { waveNumber = 1, delayBeforeWave = 2f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 0 }
                }},
                new WaveDefinition { waveNumber = 2, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 1 },
                    new EnemySpawnEntry { enemyData = pistolMook, count = 2, preferredRow = 0 }
                }},
                new WaveDefinition { waveNumber = 3, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = pistolMook, count = 2, preferredRow = 0 },
                    new EnemySpawnEntry { enemyData = dynamiteBandit, count = 1, preferredRow = 1 }
                }},
                new WaveDefinition { waveNumber = 4, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 0 },
                    new EnemySpawnEntry { enemyData = dynamiteBandit, count = 2, preferredRow = 1 }
                }}
            };
            EditorUtility.SetDirty(stage1_1);

            var stage1_2 = CreateOrLoad<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_2.asset");
            stage1_2.id = "dusty_town_1_2";
            stage1_2.nameKey = "pve.stage.dusty_town_2";
            stage1_2.stageNumber = 1;
            stage1_2.subStageNumber = 2;
            stage1_2.gaugeMax = 30;
            stage1_2.waveCount = 5;
            stage1_2.stageTimeMin = 75f;
            stage1_2.stageTimeMax = 105f;
            stage1_2.timeBetweenWaves = 8f;
            stage1_2.enemyRowsY = new float[] { 11f, 13.5f, 16f };
            stage1_2.waves = new WaveDefinition[]
            {
                new WaveDefinition { waveNumber = 1, delayBeforeWave = 2f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = pistolMook, count = 4, preferredRow = 0 }
                }},
                new WaveDefinition { waveNumber = 2, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 1 },
                    new EnemySpawnEntry { enemyData = dynamiteBandit, count = 2, preferredRow = 0 }
                }},
                new WaveDefinition { waveNumber = 3, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = mountedRaider, count = 1, preferredRow = 1 },
                    new EnemySpawnEntry { enemyData = pistolMook, count = 2, preferredRow = 0 }
                }},
                new WaveDefinition { waveNumber = 4, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = dynamiteBandit, count = 2, preferredRow = 0 },
                    new EnemySpawnEntry { enemyData = dynamiteBandit, count = 2, preferredRow = 1 }
                }},
                new WaveDefinition { waveNumber = 5, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = mountedRaider, count = 2, preferredRow = 1 },
                    new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 0 }
                }}
            };
            EditorUtility.SetDirty(stage1_2);

            var stage1_3 = CreateOrLoad<StageData>(DATA_PATH + "Stages/stage_dusty_town_1_3.asset");
            stage1_3.id = "dusty_town_1_3";
            stage1_3.nameKey = "pve.stage.dusty_town_3";
            stage1_3.stageNumber = 1;
            stage1_3.subStageNumber = 3;
            stage1_3.gaugeMax = 40;
            stage1_3.waveCount = 6;
            stage1_3.stageTimeMin = 90f;
            stage1_3.stageTimeMax = 120f;
            stage1_3.timeBetweenWaves = 8f;
            stage1_3.enemyRowsY = new float[] { 11f, 13.5f, 16f };
            stage1_3.waves = new WaveDefinition[]
            {
                new WaveDefinition { waveNumber = 1, delayBeforeWave = 2f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = pistolMook, count = 4, preferredRow = 0 },
                    new EnemySpawnEntry { enemyData = dynamiteBandit, count = 1, preferredRow = 1 }
                }},
                new WaveDefinition { waveNumber = 2, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = mountedRaider, count = 1, preferredRow = 1 },
                    new EnemySpawnEntry { enemyData = pistolMook, count = 3, preferredRow = 0 }
                }},
                new WaveDefinition { waveNumber = 3, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = dynamiteBandit, count = 3, preferredRow = 0 },
                    new EnemySpawnEntry { enemyData = dynamiteBandit, count = 2, preferredRow = 1 }
                }},
                new WaveDefinition { waveNumber = 4, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = mountedRaider, count = 2, preferredRow = 1 },
                    new EnemySpawnEntry { enemyData = pistolMook, count = 4, preferredRow = 0 }
                }},
                new WaveDefinition { waveNumber = 5, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = dynamiteBandit, count = 2, preferredRow = 0 },
                    new EnemySpawnEntry { enemyData = mountedRaider, count = 2, preferredRow = 1 }
                }},
                new WaveDefinition { waveNumber = 6, delayBeforeWave = 3f, enemies = new EnemySpawnEntry[] {
                    new EnemySpawnEntry { enemyData = mountedRaider, count = 3, preferredRow = 1 },
                    new EnemySpawnEntry { enemyData = dynamiteBandit, count = 3, preferredRow = 0 },
                    new EnemySpawnEntry { enemyData = pistolMook, count = 4, preferredRow = 2 }
                }}
            };
            EditorUtility.SetDirty(stage1_3);
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
                    fanShotInterval = 2f,
                    fanShotWarning = 0.5f,
                    fanShotBulletSpeed = 9f,
                    fanShotBulletRadius = 0.2f,
                    usesTailSweep = false, // 3페이즈: 지면 부채꼴 난사 (꼬리 휩쓸기 없음)
                    summonsMooks = false,
                    enrageSpeedMultiplier = 1.5f,
                    enrageAttackSpeedMultiplier = 1.5f,
                    movesHorizontally = true,
                    moveRange = 12f,
                    moveInterval = 2f
                }
            };
            EditorUtility.SetDirty(bossRex);

            var bossStage = CreateOrLoad<StageData>(DATA_PATH + "Stages/stage_dusty_town_boss.asset");
            bossStage.id = "dusty_town_boss";
            bossStage.nameKey = "pve.boss.rattlesnake_rex";
            bossStage.stageNumber = 1;
            bossStage.subStageNumber = 4;
            bossStage.isBossStage = true;
            bossStage.bossData = bossRex;
            bossStage.gaugeMax = 0;
            bossStage.waveCount = 0;
            bossStage.stageTimeMin = 180f;
            bossStage.stageTimeMax = 300f;
            bossStage.enemyRowsY = new float[] { 11f, 13.5f, 16f };
            EditorUtility.SetDirty(bossStage);
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
            botBehavior.reviveSeekDistance = 5f;
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

            var modeSelectGO = new GameObject("ModeSelectUI");
            modeSelectGO.AddComponent<ModeSelectUI>();

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

            var pauseGO = new GameObject("PauseMenuController");
            pauseGO.AddComponent<PauseMenuController>();

            EditorSceneManager.SaveScene(scene, SCENE_PATH + "PvP_Match.unity");
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
            
            var botBehavior = AssetDatabase.LoadAssetAtPath<BotBehaviorData>(DATA_PATH + "AI/bot_behavior_default.asset");
            var battlefieldData = AssetDatabase.LoadAssetAtPath<BattlefieldData>(DATA_PATH + "Battlefield/battlefield_main.asset");
            var downedData = AssetDatabase.LoadAssetAtPath<DownedData>(DATA_PATH + "Downed/downed_default.asset");
            var feelData = AssetDatabase.LoadAssetAtPath<FeelData>(DATA_PATH + "Feel/feel_default.asset");
            var matchRules = AssetDatabase.LoadAssetAtPath<MatchRulesData>(DATA_PATH + "Match/match_rules_default.asset");
            var inputData = AssetDatabase.LoadAssetAtPath<InputData>(DATA_PATH + "Input/input_default.asset");
            var touchControls = AssetDatabase.LoadAssetAtPath<TouchControlsData>(DATA_PATH + "Input/touch_controls.asset");
            
            var assetManagerGO = new GameObject("RuntimeAssetManager");
            var assetManager = assetManagerGO.AddComponent<RuntimeAssetManager>();
            
            SetPrivateField(assetManager, "_spriteLibrary", spriteLibrary);
            SetPrivateField(assetManager, "_audioLibrary", audioLibrary);
            SetPrivateField(assetManager, "_characterDatas", new CharacterData[] { charBuster, charRaven, charVolt });
            SetPrivateField(assetManager, "_enemyDatas", new EnemyData[] { enemyPistol, enemyDynamite, enemyMounted });
            SetPrivateField(assetManager, "_bossDatas", bossData != null ? new BossData[] { bossData } : new BossData[0]);
            SetPrivateField(assetManager, "_stageDatas", new StageData[] { stage1, stage2, stage3, bossStage });
            SetPrivateField(assetManager, "_coverDatas", new CoverData[] { coverBarrel, coverTrough, coverSwingDoor, coverWagon });
            SetPrivateField(assetManager, "_botBehavior", botBehavior);
            SetPrivateField(assetManager, "_battlefieldData", battlefieldData);
            SetPrivateField(assetManager, "_downedData", downedData);
            SetPrivateField(assetManager, "_feelData", feelData);
            SetPrivateField(assetManager, "_matchRules", matchRules);
            SetPrivateField(assetManager, "_inputData", inputData);
            SetPrivateField(assetManager, "_touchControlsData", touchControls);
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

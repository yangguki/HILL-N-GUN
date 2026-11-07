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
using System.IO;

namespace HillNGun.Editor
{
    public static class M1SandboxSetup
    {
        private const string ASSET_ROOT = "Assets/HillNGun/";
        private const string DATA_PATH = ASSET_ROOT + "Data/";
        private const string SCENE_PATH = ASSET_ROOT + "Scenes/M1_Sandbox.unity";

        [MenuItem("HillNGun/Setup M1 Sandbox", false, 0)]
        public static void SetupM1Sandbox()
        {
            if (!EditorUtility.DisplayDialog("M1 Sandbox Setup",
                "이 작업은 다음을 수행합니다:\n" +
                "1. 기준안 수치로 ScriptableObject 데이터 에셋 생성\n" +
                "2. M1_Sandbox 씬 생성 및 구성\n\n" +
                "기존 에셋이 있으면 덮어씁니다. 계속하시겠습니까?",
                "계속", "취소"))
            {
                return;
            }

            CreateDirectories();
            CreateDataAssets();
            CreateScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("완료", 
                "M1 Sandbox 설정이 완료되었습니다.\n\n" +
                "조작법:\n" +
                "- WASD/방향키: 이동\n" +
                "- 마우스 좌클릭 드래그: 조준/사격\n" +
                "- 마우스 우클릭: 수류탄\n" +
                "- Space: 구르기\n" +
                "- E: 스킬\n" +
                "- 1/2/3: 캐릭터 전환 (버스터/레이븐/볼트)\n" +
                "- R: 과녁/엄폐물/더미아군 리셋\n" +
                "- Tab: 설정 패널\n" +
                "- F1-F8: 디버그 옵션",
                "확인");
        }

        public static void SetupDataOnly()
        {
            CreateDirectories();
            CreateDataAssets();
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
                ASSET_ROOT + "Scenes"
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

        private static void CreateDataAssets()
        {
            CreateWeaponAssets();
            CreateWeaponVfxAssets();
            CreateGrenadeAssets();
            CreateRollAssets();
            CreateSkillAssets();
            CreatePassiveAssets();
            CreateCoverAssets();
            CreateCharacterAssets();
            CreateBattlefieldAsset();
            CreateInputAsset();
            CreateDownedAsset();
            CreateFeelAsset();
            CreateTargetAssets();
            CreateMatchRulesAsset();
        }

        private static void CreateWeaponAssets()
        {
            var wpnBuster = CreateOrLoad<WeaponData>(DATA_PATH + "Weapons/wpn_buster.asset");
            wpnBuster.id = "wpn_buster";
            wpnBuster.fireMode = FireMode.Auto;
            wpnBuster.damagePerProjectile = 150;
            wpnBuster.projectilesPerShot = 5;
            wpnBuster.spreadDeg = 30f;
            wpnBuster.projectileSpeed = 18f;
            wpnBuster.projectileRadius = 0.3f;
            wpnBuster.range = 8f; // v2 (07): mid-row → enemy front (6.5) reachable
            wpnBuster.projectileLifetime = 0.44f;
            wpnBuster.fullPelletRange = 4f;
            wpnBuster.damageFalloffStart = 0f;
            wpnBuster.damageFalloffEndMul = 1f;
            wpnBuster.aimLaser = false;
            wpnBuster.fireInterval = 0.6f;
            wpnBuster.gaugeCells = 3;
            wpnBuster.shotsPerCell = 1;
            wpnBuster.rechargePerCell = 1.2f;
            wpnBuster.muzzleForwardOffset = 1.0f;
            wpnBuster.nameKey = "weapon.wpn_buster"; wpnBuster.descKey = "weapon.wpn_buster.desc";
            wpnBuster.holdToRepeat = false; wpnBuster.cockAnimation = false;
            EditorUtility.SetDirty(wpnBuster);

            var wpnRaven = CreateOrLoad<WeaponData>(DATA_PATH + "Weapons/wpn_raven.asset");
            wpnRaven.id = "wpn_raven";
            wpnRaven.fireMode = FireMode.ChargeRelease;
            wpnRaven.damagePerProjectile = 900;
            wpnRaven.projectilesPerShot = 1;
            wpnRaven.spreadDeg = 0f;
            wpnRaven.projectileSpeed = 32f;
            wpnRaven.projectileRadius = 0.12f;
            wpnRaven.range = 16f; // v2: back-row → enemy back (14) reachable
            wpnRaven.projectileLifetime = 0.50f;
            wpnRaven.fullPelletRange = 0f;
            wpnRaven.damageFalloffStart = 12f;
            wpnRaven.damageFalloffEndMul = 0.7f;
            wpnRaven.aimLaser = true;
            wpnRaven.fireInterval = 1.2f;
            wpnRaven.gaugeCells = 2;
            wpnRaven.shotsPerCell = 1;
            wpnRaven.rechargePerCell = 2.0f;
            wpnRaven.chargeMinTime = 0.3f;
            wpnRaven.chargeMaxTime = 1.0f;
            wpnRaven.chargeDamageMul = 1.6f;
            wpnRaven.muzzleForwardOffset = 1.0f;
            wpnRaven.nameKey = "weapon.wpn_raven"; wpnRaven.descKey = "weapon.wpn_raven.desc";
            wpnRaven.holdToRepeat = false; wpnRaven.cockAnimation = true;
            EditorUtility.SetDirty(wpnRaven);

            var wpnVolt = CreateOrLoad<WeaponData>(DATA_PATH + "Weapons/wpn_volt.asset");
            wpnVolt.id = "wpn_volt";
            wpnVolt.fireMode = FireMode.Auto;
            wpnVolt.damagePerProjectile = 520;
            wpnVolt.projectilesPerShot = 1;
            wpnVolt.spreadDeg = 0f;
            wpnVolt.projectileSpeed = 22f;
            wpnVolt.projectileRadius = 0.25f;
            wpnVolt.range = 10f; // v2: mid → enemy mid (9)
            wpnVolt.projectileLifetime = 0.45f;
            wpnVolt.fullPelletRange = 0f;
            wpnVolt.damageFalloffStart = 0f;
            wpnVolt.damageFalloffEndMul = 1f;
            wpnVolt.aimLaser = false;
            wpnVolt.fireInterval = 0.7f;
            wpnVolt.gaugeCells = 3;
            wpnVolt.shotsPerCell = 1;
            wpnVolt.rechargePerCell = 1.5f;
            wpnVolt.muzzleForwardOffset = 1.0f;
            wpnVolt.nameKey = "weapon.wpn_volt"; wpnVolt.descKey = "weapon.wpn_volt.desc";
            wpnVolt.holdToRepeat = false; wpnVolt.cockAnimation = true; // big revolver: one tap, one shot, hammer cock
            EditorUtility.SetDirty(wpnVolt);
        }


        private static void CreateWeaponVfxAssets()
        {
            CreateFolderRecursive(DATA_PATH + "Vfx");
            var fan = CreateOrLoad<WeaponVfxData>(DATA_PATH + "Vfx/vfx_spread_fan.asset");
            var tmp = WeaponVfxData.Builtin("spread_fan");
            EditorUtility.CopySerialized(tmp, fan); Object.DestroyImmediate(tmp); fan.name = "vfx_spread_fan";
            EditorUtility.SetDirty(fan);
            var beam = CreateOrLoad<WeaponVfxData>(DATA_PATH + "Vfx/vfx_charge_beam.asset");
            tmp = WeaponVfxData.Builtin("charge_beam");
            EditorUtility.CopySerialized(tmp, beam); Object.DestroyImmediate(tmp); beam.name = "vfx_charge_beam";
            EditorUtility.SetDirty(beam);
            var slug = CreateOrLoad<WeaponVfxData>(DATA_PATH + "Vfx/vfx_heavy_slug.asset");
            tmp = WeaponVfxData.Builtin("heavy_slug");
            EditorUtility.CopySerialized(tmp, slug); Object.DestroyImmediate(tmp); slug.name = "vfx_heavy_slug";
            EditorUtility.SetDirty(slug);

            var wpnBuster = AssetDatabase.LoadAssetAtPath<WeaponData>(DATA_PATH + "Weapons/wpn_buster.asset");
            var wpnRaven = AssetDatabase.LoadAssetAtPath<WeaponData>(DATA_PATH + "Weapons/wpn_raven.asset");
            var wpnVolt = AssetDatabase.LoadAssetAtPath<WeaponData>(DATA_PATH + "Weapons/wpn_volt.asset");
            if (wpnBuster != null) { wpnBuster.vfx = fan; EditorUtility.SetDirty(wpnBuster); }
            if (wpnRaven != null) { wpnRaven.vfx = beam; EditorUtility.SetDirty(wpnRaven); }
            if (wpnVolt != null) { wpnVolt.vfx = slug; EditorUtility.SetDirty(wpnVolt); }
        }

        private static void CreateGrenadeAssets()
        {
            var grenStd = CreateOrLoad<GrenadeData>(DATA_PATH + "Grenades/gren_std.asset");
            grenStd.id = "gren_std";
            grenStd.centerDamage = 900;
            grenStd.edgeDamage = 450;
            grenStd.radius = 1.5f;
            grenStd.innerRadius = 0.5f;
            grenStd.knockback = 0f;
            grenStd.minRange = 2f;
            grenStd.maxRange = 9f;
            grenStd.flightTimeBase = 0.6f;
            grenStd.flightTimePerTile = 0.04f;
            grenStd.ignoresCover = true;
            grenStd.coverDamageMul = 1.5f;
            grenStd.rechargeTime = 12f;
            EditorUtility.SetDirty(grenStd);

            var grenVolt = CreateOrLoad<GrenadeData>(DATA_PATH + "Grenades/gren_volt.asset");
            grenVolt.id = "gren_volt";
            grenVolt.centerDamage = 700;
            grenVolt.edgeDamage = 350;
            grenVolt.radius = 2.0f;
            grenVolt.innerRadius = 0.5f;
            grenVolt.knockback = 1.0f;
            grenVolt.minRange = 2f;
            grenVolt.maxRange = 9f;
            grenVolt.flightTimeBase = 0.6f;
            grenVolt.flightTimePerTile = 0.04f;
            grenVolt.ignoresCover = true;
            grenVolt.coverDamageMul = 1.5f;
            grenVolt.rechargeTime = 12f;
            EditorUtility.SetDirty(grenVolt);
        }

        private static void CreateRollAssets()
        {
            var rollBuster = CreateOrLoad<RollData>(DATA_PATH + "Rolls/roll_buster.asset");
            rollBuster.id = "roll_buster";
            rollBuster.distance = 2.5f;
            rollBuster.duration = 0.4f;
            rollBuster.invulnStart = 0f;
            rollBuster.invulnDuration = 0.35f;
            rollBuster.cooldown = 4f;
            rollBuster.canFireDuring = false;
            rollBuster.inputBuffer = 0.1f;
            rollBuster.allowRowRoll = true;
            EditorUtility.SetDirty(rollBuster);

            var rollRaven = CreateOrLoad<RollData>(DATA_PATH + "Rolls/roll_raven.asset");
            rollRaven.id = "roll_raven";
            rollRaven.distance = 2.5f;
            rollRaven.duration = 0.4f;
            rollRaven.invulnStart = 0f;
            rollRaven.invulnDuration = 0.35f;
            rollRaven.cooldown = 6f;
            rollRaven.canFireDuring = false;
            rollRaven.inputBuffer = 0.1f;
            rollRaven.allowRowRoll = true;
            EditorUtility.SetDirty(rollRaven);

            var rollVolt = CreateOrLoad<RollData>(DATA_PATH + "Rolls/roll_volt.asset");
            rollVolt.id = "roll_volt";
            rollVolt.distance = 2.5f;
            rollVolt.duration = 0.4f;
            rollVolt.invulnStart = 0f;
            rollVolt.invulnDuration = 0.35f;
            rollVolt.cooldown = 5f;
            rollVolt.canFireDuring = false;
            rollVolt.inputBuffer = 0.1f;
            rollVolt.allowRowRoll = true;
            EditorUtility.SetDirty(rollVolt);
        }

        private static void CreateSkillAssets()
        {
            var skillBreach = CreateOrLoad<SkillData>(DATA_PATH + "Skills/skill_breach.asset");
            skillBreach.id = "skill_breach";
            skillBreach.nameKey = "skill.breach.name";
            skillBreach.useType = SkillUseType.Tap;
            skillBreach.effect = SkillEffect.SpreadBoost;
            skillBreach.cooldown = 12f;
            skillBreach.duration = 3f;
            skillBreach.parameters = new SkillParameter[] {
                new SkillParameter { key = "spreadDeg", value = 45f },
                new SkillParameter { key = "coverDamageMul", value = 2f }
            };
            EditorUtility.SetDirty(skillBreach);

            var skillPierce = CreateOrLoad<SkillData>(DATA_PATH + "Skills/skill_pierce.asset");
            skillPierce.id = "skill_pierce";
            skillPierce.nameKey = "skill.pierce.name";
            skillPierce.useType = SkillUseType.Hold;
            skillPierce.effect = SkillEffect.PierceNextShot;
            skillPierce.cooldown = 18f;
            skillPierce.holdTime = 1.0f;
            skillPierce.duration = 0f;
            skillPierce.parameters = new SkillParameter[] {
                new SkillParameter { key = "pierceCovers", value = 1f },
                new SkillParameter { key = "afterPierceMul", value = 0.8f }
            };
            EditorUtility.SetDirty(skillPierce);

            var skillRepair = CreateOrLoad<SkillData>(DATA_PATH + "Skills/skill_repair.asset");
            skillRepair.id = "skill_repair";
            skillRepair.nameKey = "skill.repair.name";
            skillRepair.useType = SkillUseType.DragRelease;
            skillRepair.effect = SkillEffect.RepairCover;
            skillRepair.cooldown = 15f;
            skillRepair.targetRange = 6f;
            skillRepair.duration = 0f;
            skillRepair.parameters = new SkillParameter[] {
                new SkillParameter { key = "repairPercent", value = 0.4f }
            };
            EditorUtility.SetDirty(skillRepair);
        }

        private static void CreatePassiveAssets()
        {
            var psvRollShot = CreateOrLoad<PassiveData>(DATA_PATH + "Passives/psv_roll_shot.asset");
            psvRollShot.id = "psv_roll_shot";
            psvRollShot.nameKey = "passive.roll_shot.name";
            psvRollShot.passiveType = PassiveType.RollShot;
            psvRollShot.parameters = new PassiveParameter[] {
                new PassiveParameter { key = "firstShotMul", value = 1.3f },
                new PassiveParameter { key = "window", value = 1.0f }
            };
            EditorUtility.SetDirty(psvRollShot);

            var psvWeakpoint = CreateOrLoad<PassiveData>(DATA_PATH + "Passives/psv_weakpoint.asset");
            psvWeakpoint.id = "psv_weakpoint";
            psvWeakpoint.nameKey = "passive.weakpoint.name";
            psvWeakpoint.passiveType = PassiveType.Weakpoint;
            psvWeakpoint.parameters = new PassiveParameter[] {
                new PassiveParameter { key = "aimTime", value = 2.0f },
                new PassiveParameter { key = "weakpointMul", value = 1.25f },
                new PassiveParameter { key = "weakpointRadius", value = 0.2f } // decision D5: centre radius on any target
            };
            EditorUtility.SetDirty(psvWeakpoint);

            var psvFastRevive = CreateOrLoad<PassiveData>(DATA_PATH + "Passives/psv_fast_revive.asset");
            psvFastRevive.id = "psv_fast_revive";
            psvFastRevive.nameKey = "passive.fast_revive.name";
            psvFastRevive.passiveType = PassiveType.FastRevive;
            psvFastRevive.parameters = new PassiveParameter[] {
                new PassiveParameter { key = "reviveTime", value = 2.0f }
            };
            EditorUtility.SetDirty(psvFastRevive);
        }

        private static void CreateCoverAssets()
        {
            var coverSwingDoor = CreateOrLoad<CoverData>(DATA_PATH + "Cover/cover_swing_door.asset");
            coverSwingDoor.id = "cover_swing_door";
            coverSwingDoor.nameKey = "cover.swing_door.name";
            coverSwingDoor.maxHp = 1000;
            coverSwingDoor.width = 1.5f;
            coverSwingDoor.depth = 0.5f;
            coverSwingDoor.stages = 4;
            coverSwingDoor.repairable = true;
            coverSwingDoor.respawnPerRound = true;
            EditorUtility.SetDirty(coverSwingDoor);

            var coverBarrel = CreateOrLoad<CoverData>(DATA_PATH + "Cover/cover_barrel.asset");
            coverBarrel.id = "cover_barrel";
            coverBarrel.nameKey = "cover.barrel.name";
            coverBarrel.maxHp = 1500;
            coverBarrel.width = 1.0f;
            coverBarrel.depth = 0.5f;
            coverBarrel.stages = 4;
            coverBarrel.repairable = true;
            coverBarrel.respawnPerRound = true;
            EditorUtility.SetDirty(coverBarrel);

            var coverTrough = CreateOrLoad<CoverData>(DATA_PATH + "Cover/cover_trough.asset");
            coverTrough.id = "cover_trough";
            coverTrough.nameKey = "cover.trough.name";
            coverTrough.maxHp = 2500;
            coverTrough.width = 2.0f;
            coverTrough.depth = 0.5f;
            coverTrough.stages = 4;
            coverTrough.repairable = true;
            coverTrough.respawnPerRound = true;
            EditorUtility.SetDirty(coverTrough);

            var coverWagon = CreateOrLoad<CoverData>(DATA_PATH + "Cover/cover_wagon.asset");
            coverWagon.id = "cover_wagon";
            coverWagon.nameKey = "cover.wagon.name";
            coverWagon.maxHp = 4000;
            coverWagon.width = 3.0f;
            coverWagon.depth = 0.5f;
            coverWagon.stages = 4;
            coverWagon.repairable = true;
            coverWagon.respawnPerRound = true;
            EditorUtility.SetDirty(coverWagon);

            var coverRock = CreateOrLoad<CoverData>(DATA_PATH + "Cover/cover_rock.asset");
            coverRock.id = "cover_rock";
            coverRock.nameKey = "cover.rock";
            coverRock.maxHp = 9999;
            coverRock.width = 2.0f;
            coverRock.depth = 0.6f;
            coverRock.stages = 4;
            coverRock.repairable = false;
            coverRock.respawnPerRound = true;
            coverRock.indestructible = true;
            EditorUtility.SetDirty(coverRock);
        }

        private static void CreateCharacterAssets()
        {
            var wpnBuster = AssetDatabase.LoadAssetAtPath<WeaponData>(DATA_PATH + "Weapons/wpn_buster.asset");
            var wpnRaven = AssetDatabase.LoadAssetAtPath<WeaponData>(DATA_PATH + "Weapons/wpn_raven.asset");
            var wpnVolt = AssetDatabase.LoadAssetAtPath<WeaponData>(DATA_PATH + "Weapons/wpn_volt.asset");

            var grenStd = AssetDatabase.LoadAssetAtPath<GrenadeData>(DATA_PATH + "Grenades/gren_std.asset");
            var grenVolt = AssetDatabase.LoadAssetAtPath<GrenadeData>(DATA_PATH + "Grenades/gren_volt.asset");

            var rollBuster = AssetDatabase.LoadAssetAtPath<RollData>(DATA_PATH + "Rolls/roll_buster.asset");
            var rollRaven = AssetDatabase.LoadAssetAtPath<RollData>(DATA_PATH + "Rolls/roll_raven.asset");
            var rollVolt = AssetDatabase.LoadAssetAtPath<RollData>(DATA_PATH + "Rolls/roll_volt.asset");

            var skillBreach = AssetDatabase.LoadAssetAtPath<SkillData>(DATA_PATH + "Skills/skill_breach.asset");
            var skillPierce = AssetDatabase.LoadAssetAtPath<SkillData>(DATA_PATH + "Skills/skill_pierce.asset");
            var skillRepair = AssetDatabase.LoadAssetAtPath<SkillData>(DATA_PATH + "Skills/skill_repair.asset");

            var psvRollShot = AssetDatabase.LoadAssetAtPath<PassiveData>(DATA_PATH + "Passives/psv_roll_shot.asset");
            var psvWeakpoint = AssetDatabase.LoadAssetAtPath<PassiveData>(DATA_PATH + "Passives/psv_weakpoint.asset");
            var psvFastRevive = AssetDatabase.LoadAssetAtPath<PassiveData>(DATA_PATH + "Passives/psv_fast_revive.asset");

            var buster = CreateOrLoad<CharacterData>(DATA_PATH + "Characters/char_buster.asset");
            buster.id = "buster";
            buster.nameKey = "char.buster.name";
            buster.role = Role.Assault;
            buster.maxHp = 3200;
            buster.moveSpeed = 3.0f;
            buster.chargingMoveSpeed = 3.0f;
            buster.rowChangeSpeedMul = 1.5f;
            buster.hitRadius = 0.5f;
            buster.weakpointRadius = 0f;
            buster.grenadeCount = 2;
            buster.weapon = wpnBuster;
            buster.grenade = grenStd;
            buster.roll = rollBuster;
            buster.skill = skillBreach;
            buster.passive = psvRollShot;
            EditorUtility.SetDirty(buster);

            var raven = CreateOrLoad<CharacterData>(DATA_PATH + "Characters/char_raven.asset");
            raven.id = "raven";
            raven.nameKey = "char.raven.name";
            raven.role = Role.Sniper;
            raven.maxHp = 2200;
            raven.moveSpeed = 2.6f;
            raven.chargingMoveSpeed = 1.6f;
            raven.rowChangeSpeedMul = 1.5f;
            raven.hitRadius = 0.5f;
            raven.weakpointRadius = 0.2f;
            raven.grenadeCount = 1;
            raven.weapon = wpnRaven;
            raven.grenade = grenStd;
            raven.roll = rollRaven;
            raven.skill = skillPierce;
            raven.passive = psvWeakpoint;
            EditorUtility.SetDirty(raven);

            var volt = CreateOrLoad<CharacterData>(DATA_PATH + "Characters/char_volt.asset");
            volt.id = "volt";
            volt.nameKey = "char.volt.name";
            volt.role = Role.Support;
            volt.maxHp = 2600;
            volt.moveSpeed = 2.8f;
            volt.chargingMoveSpeed = 2.8f;
            volt.rowChangeSpeedMul = 1.5f;
            volt.hitRadius = 0.5f;
            volt.weakpointRadius = 0f;
            volt.grenadeCount = 2;
            volt.weapon = wpnVolt;
            volt.grenade = grenVolt;
            volt.roll = rollVolt;
            volt.skill = skillRepair;
            volt.passive = psvFastRevive;
            EditorUtility.SetDirty(volt);
        }

        private static void CreateBattlefieldAsset()
        {
            var battlefield = CreateOrLoad<BattlefieldData>(DATA_PATH + "Battlefield/battlefield_main.asset");
            battlefield.size = new Vector2(24f, 18f);
            battlefield.ourRowsY = new float[] { 2f, 4.5f, 7f };
            battlefield.enemyRowsY = new float[] { 11f, 13.5f, 16f };
            battlefield.coverOffsetMin = 0.35f;
            battlefield.coverOffsetMax = 0.85f;
            battlefield.startRowIndex = 1;
            battlefield.assaultStartsFrontRow = false; // v2 D1 discarded: assault starts mid-row
            battlefield.startX = new float[] { 6f, 12f, 18f };
            battlefield.referenceResolution = new Vector2Int(1920, 1080);
            battlefield.pxPerTileX = 72f;
            battlefield.pxPerTileY = 50f;
            battlefield.perspectiveScaleBottom = 1.0f;
            battlefield.perspectiveScaleTop = 0.8f;
            battlefield.hudTopPx = 100;
            battlefield.boundaryMinX = 0.5f;
            battlefield.boundaryMaxX = 23.5f;

            // PvP town props (planner provisional, 2026-11-07): 한 팀; 상대 = (24-x, 18-y) 회전 복제. 실제 권위는 battlefield_main 에셋
            battlefield.covers = new CoverPlacement[] {
                new CoverPlacement { coverId = "cover_wagon", rowIndex = 0, xPosition = 3f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 0, xPosition = 8f },
                new CoverPlacement { coverId = "cover_trough", rowIndex = 0, xPosition = 16f },
                new CoverPlacement { coverId = "cover_wagon", rowIndex = 0, xPosition = 21f },
                new CoverPlacement { coverId = "cover_swing_door", rowIndex = 1, xPosition = 3f },
                new CoverPlacement { coverId = "cover_wagon", rowIndex = 1, xPosition = 8f },
                new CoverPlacement { coverId = "cover_wagon", rowIndex = 1, xPosition = 16f },
                new CoverPlacement { coverId = "cover_swing_door", rowIndex = 1, xPosition = 21f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 2, xPosition = 2.5f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 2, xPosition = 3.8f },
                new CoverPlacement { coverId = "cover_hay", rowIndex = 2, xPosition = 8f },
                new CoverPlacement { coverId = "cover_hay", rowIndex = 2, xPosition = 16f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 2, xPosition = 20.2f },
                new CoverPlacement { coverId = "cover_barrel", rowIndex = 2, xPosition = 21.5f },
            };

            EditorUtility.SetDirty(battlefield);
        }

        private static void CreateInputAsset()
        {
            var input = CreateOrLoad<InputData>(DATA_PATH + "Input/input_default.asset");
            input.fireButtonRadiusPx = 140f;
            input.deadzoneRatio = 0.2f;
            input.aimClampDeg = 170f;
            input.aimMinDistance = 2f;
            input.joystickRadiusPx = 120f;
            input.rowChangeThreshold = 0.6f;
            input.rowChangeHoldTime = 0.15f;
            input.buttonScales = new float[] { 0.85f, 1.0f, 1.15f };
            input.currentButtonScaleIndex = 1;
            EditorUtility.SetDirty(input);
        }

        private static void CreateDownedAsset()
        {
            var downed = CreateOrLoad<DownedData>(DATA_PATH + "Downed/downed_default.asset");
            downed.downedDuration = 10f;
            downed.baseReviveTime = 3f;
            downed.downedHp = 600;
            downed.crawlSpeed = 0.5f;
            downed.reviveRange = 1.0f;
            downed.reviveHpRatio = 0.3f;
            EditorUtility.SetDirty(downed);
        }

        private static void CreateFeelAsset()
        {
            var feel = CreateOrLoad<FeelData>(DATA_PATH + "Feel/feel_default.asset");
            feel.hitFlashDuration = 0.06f;
            feel.hitstopLight = 0f;
            feel.hitstopMedium = 0.03f;
            feel.hitstopHeavy = 0.06f;
            feel.hitstopGrenade = 0.04f;
            feel.hitstopCoverStage = 0.03f;
            feel.shakeHeavyAmplitude = 0.08f;
            feel.shakeHeavyDuration = 0.1f;
            feel.shakeCoverAmplitude = 0.08f;
            feel.shakeCoverDuration = 0.15f;
            feel.shakeGrenadeAmplitude = 0.12f;
            feel.shakeGrenadeDuration = 0.2f;
            feel.vibrateBigDamageMs = 40;
            feel.bigDamageThreshold = 500;
            feel.shakeEnabled = true;
            feel.vibrationEnabled = true;
            // shot visibility (view only, t48u): readable bullets against the sand
            feel.bulletHeadSize = 0.62f;
            feel.bulletTrailLength = 1.8f;
            feel.bulletTrailWidth = 0.28f;
            feel.bulletHaloAlpha = 0.5f;
            feel.playerBulletColor = new Color(1f, 0.86f, 0.25f, 1f);
            feel.enemyBulletColor = new Color(1f, 0.36f, 0.2f, 1f);
            feel.muzzleFlashSize = 1.05f;
            feel.muzzleFlashTime = 0.1f;
            EditorUtility.SetDirty(feel);
        }

        private static void CreateTargetAssets()
        {
            var targetStatic = CreateOrLoad<TargetData>(DATA_PATH + "Targets/target_static.asset");
            targetStatic.id = "target_static";
            targetStatic.hp = 3000;
            targetStatic.hitRadius = 0.5f;
            targetStatic.moves = false;
            targetStatic.shoots = false;
            EditorUtility.SetDirty(targetStatic);

            var targetMover = CreateOrLoad<TargetData>(DATA_PATH + "Targets/target_mover.asset");
            targetMover.id = "target_mover";
            targetMover.hp = 3000;
            targetMover.hitRadius = 0.5f;
            targetMover.moves = true;
            targetMover.moveSpeed = 2.0f;
            targetMover.moveRange = 3f;
            targetMover.shoots = false;
            EditorUtility.SetDirty(targetMover);

            var targetShooter = CreateOrLoad<TargetData>(DATA_PATH + "Targets/target_shooter.asset");
            targetShooter.id = "target_shooter";
            targetShooter.hp = 3000;
            targetShooter.hitRadius = 0.5f;
            targetShooter.moves = false;
            targetShooter.shoots = true;
            targetShooter.shootDamage = 120;
            targetShooter.bulletSpeed = 8f;
            targetShooter.fireInterval = 1.5f;
            targetShooter.warningTime = 0.5f;
            EditorUtility.SetDirty(targetShooter);
        }

        private static void CreateMatchRulesAsset()
        {
            var rules = CreateOrLoad<MatchRulesData>(DATA_PATH + "Match/match_rules_default.asset");
            rules.friendlyFire = false;
            rules.roundMaxTime = 80f;
            rules.roundsToWin = 2;
            rules.maxRounds = 3;
            rules.roundStartDelay = 3f;
            rules.roundEndDelay = 3f;
            rules.pvpSpawnX = new float[] { 6f, 12f, 18f }; // 기획: 양 팀 가운데 줄 x 6·12·18
            rules.pvpSpawnRowIndex = 1;
            rules.downedCountsAsAlive = true;   // D10
            rules.timeoutUseHpSum = true;       // FP 256
            rules.matchTiebreakByDamage = true; // D11
            rules.supplyCrateEnabled = false;   // D12: 04 B-4 미정 → 기본 꺼짐
            rules.supplyCrateTime = 50f;
            rules.supplyCrateHp = 300;
            rules.supplyCrateRadius = 0.6f;
            rules.supplyCratePosition = new Vector2(12f, 9f);
            rules.supplyCrateRefillsGrenades = true;
            rules.supplyCrateHealRatio = 0f;
            rules.lateCoverDecayEnabled = false;
            rules.lateCoverDamageMul = 1.5f;
            // PvE enemy HP by squad size (03 doc: 2인 협동 적 HP x1.5; 3-person squad x3.4 tuned toward 60~120s stages, decision D15)
            rules.pveEnemyHpMultipliers = new float[] { 1f, 1.5f, 3.4f };
            EditorUtility.SetDirty(rules);
        }

        private static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.25f, 0.2f, 0.15f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraGO.tag = "MainCamera";
            cameraGO.AddComponent<AudioListener>();

            var battlefieldGO = new GameObject("BattlefieldSystem");
            var battlefieldSys = battlefieldGO.AddComponent<BattlefieldSystem>();
            var battlefieldData = AssetDatabase.LoadAssetAtPath<BattlefieldData>(DATA_PATH + "Battlefield/battlefield_main.asset");
            SetPrivateField(battlefieldSys, "_data", battlefieldData);

            var rendererGO = new GameObject("BattlefieldRenderer");
            rendererGO.AddComponent<BattlefieldRenderer>();

            var hudGO = new GameObject("PlaceholderHUD");
            var placeholderHUD = hudGO.AddComponent<PlaceholderHUD>();

            var combatGO = new GameObject("CombatSystem");
            var combatSys = combatGO.AddComponent<CombatSystem>();
            var feelData = AssetDatabase.LoadAssetAtPath<FeelData>(DATA_PATH + "Feel/feel_default.asset");
            var matchRules = AssetDatabase.LoadAssetAtPath<MatchRulesData>(DATA_PATH + "Match/match_rules_default.asset");
            SetPrivateField(combatSys, "_feelData", feelData);
            SetPrivateField(combatSys, "_matchRules", matchRules);

            var inputGO = new GameObject("InputHandler");
            var inputHandler = inputGO.AddComponent<TouchInputHandler>();
            var inputData = AssetDatabase.LoadAssetAtPath<InputData>(DATA_PATH + "Input/input_default.asset");
            SetPrivateField(inputHandler, "_inputData", inputData);

            var loggerGO = new GameObject("GameLogger");
            loggerGO.AddComponent<GameLogger>();

            var debugGO = new GameObject("DebugOverlay");
            debugGO.AddComponent<DebugOverlay>();

            var shakeGO = new GameObject("ScreenShake");
            var screenShake = shakeGO.AddComponent<ScreenShake>();
            SetPrivateField(screenShake, "_feelData", feelData);

            var effectsGO = new GameObject("EffectsManager");
            effectsGO.AddComponent<EffectsManager>();

            var localizationGO = new GameObject("LocalizationManager");
            localizationGO.AddComponent<LocalizationManager>();

            var audioGO = new GameObject("PlaceholderAudio");
            var placeholderAudio = audioGO.AddComponent<PlaceholderAudio>();

            var downedData = AssetDatabase.LoadAssetAtPath<DownedData>(DATA_PATH + "Downed/downed_default.asset");

            var reviveSystemGO = new GameObject("ReviveSystem");
            var reviveSystem = reviveSystemGO.AddComponent<ReviveSystem>();
            SetPrivateField(reviveSystem, "_downedData", downedData);

            var gameManagerGO = new GameObject("GameManager");
            var gameManager = gameManagerGO.AddComponent<GameManager>();
            var charBuster = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_buster.asset");
            var charRaven = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_raven.asset");
            var charVolt = AssetDatabase.LoadAssetAtPath<CharacterData>(DATA_PATH + "Characters/char_volt.asset");

            var coverSwingDoor = AssetDatabase.LoadAssetAtPath<CoverData>(DATA_PATH + "Cover/cover_swing_door.asset");
            var coverBarrel = AssetDatabase.LoadAssetAtPath<CoverData>(DATA_PATH + "Cover/cover_barrel.asset");
            var coverTrough = AssetDatabase.LoadAssetAtPath<CoverData>(DATA_PATH + "Cover/cover_trough.asset");
            var coverWagon = AssetDatabase.LoadAssetAtPath<CoverData>(DATA_PATH + "Cover/cover_wagon.asset");

            var targetStatic = AssetDatabase.LoadAssetAtPath<TargetData>(DATA_PATH + "Targets/target_static.asset");
            var targetMover = AssetDatabase.LoadAssetAtPath<TargetData>(DATA_PATH + "Targets/target_mover.asset");
            var targetShooter = AssetDatabase.LoadAssetAtPath<TargetData>(DATA_PATH + "Targets/target_shooter.asset");

            SetPrivateField(gameManager, "_battlefieldData", battlefieldData);
            SetPrivateField(gameManager, "_inputData", inputData);
            SetPrivateField(gameManager, "_downedData", downedData);
            SetPrivateField(gameManager, "_feelData", feelData);
            SetPrivateField(gameManager, "_matchRules", matchRules);
            SetPrivateField(gameManager, "_characterDatas", new CharacterData[] { charBuster, charRaven, charVolt });
            SetPrivateField(gameManager, "_coverDatas", new CoverData[] { coverSwingDoor, coverBarrel, coverTrough, coverWagon });
            SetPrivateField(gameManager, "_targetDatas", new TargetData[] { targetStatic, targetMover, targetShooter });
            SetPrivateField(gameManager, "_battlefieldSystem", battlefieldSys);
            SetPrivateField(gameManager, "_combatSystem", combatSys);
            SetPrivateField(gameManager, "_inputHandler", inputHandler);
            SetPrivateField(gameManager, "_logger", loggerGO.GetComponent<GameLogger>());
            SetPrivateField(gameManager, "_debugOverlay", debugGO.GetComponent<DebugOverlay>());
            SetPrivateField(gameManager, "_screenShake", screenShake);
            SetPrivateField(gameManager, "_localization", localizationGO.GetComponent<LocalizationManager>());
            SetPrivateField(gameManager, "_audio", placeholderAudio);
            SetPrivateField(gameManager, "_reviveSystem", reviveSystem);
            SetPrivateField(gameManager, "_placeholderHUD", placeholderHUD);

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            EditorSceneManager.OpenScene(SCENE_PATH);
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

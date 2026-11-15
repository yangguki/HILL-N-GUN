using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Editor
{
    /// <summary>
    /// v11 FreeMove PvE authority (called last from GameSetup.CreateAllDataAssets so a regenerated project keeps it):
    /// street layout (walk polygon + painted spawn points) shared by the four dust-town plates, balcony sniper role,
    /// wave spawn assignment (types / explicit ids / seeds), mode flags, and the v11 motion feel numbers.
    /// Batch: -executeMethod HillNGun.Editor.FreeMoveSetup.ApplyFromCommandLine
    /// </summary>
    public static class FreeMoveSetup
    {
        private const string DATA = "Assets/HillNGun/Data/";
        public const string LayoutPath = DATA + "Layout/street_layout_dust.asset";
        public const string SniperPath = DATA + "Enemies/enemy_balcony_sniper.asset";

        [MenuItem("HillNGun/Apply FreeMove PvE (v11)", false, 30)]
        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(DATA + "Layout")) AssetDatabase.CreateFolder("Assets/HillNGun/Data", "Layout");
            var layout = CreateLayout();
            foreach (var v in new[] { "visual_dust_1_1_noon", "visual_dust_1_2_afternoon", "visual_dust_1_3_sunset", "visual_rex_night" })
            {
                var vis = AssetDatabase.LoadAssetAtPath<StageVisualData>(DATA + "Visuals/" + v + ".asset");
                if (vis == null) { Debug.LogWarning("[FreeMoveSetup] missing visual " + v); continue; }
                vis.streetLayout = layout;
                EditorUtility.SetDirty(vis);
            }
            var sniper = CreateSniper();
            AssignStageSpawns(sniper);
            ApplyRules();
            ApplyFeel();
            AssetDatabase.SaveAssets();
            Debug.Log("[FreeMoveSetup] applied: layout=" + LayoutPath + " sniper=" + SniperPath);
        }

        public static void ApplyFromCommandLine()
        {
            Apply();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        /// <summary>
        /// Measured on the 1280x720 style-4 plates (same composition for noon/afternoon/sunset/night): painted foot line
        /// px → logic (K = 35.29 px/world, s(y) = 1 - 0.2y/18), then +0.8 to actor centres. Top = back fence/gate base,
        /// bottom = front fence rail, sides = boardwalk edges (wider at the bottom, narrower at the gate).
        /// </summary>
        public static StreetLayoutData CreateLayout()
        {
            var l = CreateOrLoad<StreetLayoutData>(LayoutPath);
            l.id = "street_layout_dust";
            l.footOffset = 0.8f;
            l.walkPolygon = new[]
            {
                new Vector2(-3.6f, 1.7f), new Vector2(28.4f, 1.7f), new Vector2(24.95f, 8.1f), new Vector2(21.86f, 11.93f),
                new Vector2(21.45f, 13.9f), new Vector2(1.72f, 13.9f), new Vector2(1.35f, 11.78f), new Vector2(-1.6f, 7.39f),
                new Vector2(-2.38f, 6.54f), new Vector2(-3.65f, 2.01f),
            };
            l.enemyPostZone = new Rect(1f, 8.6f, 22f, 5.0f);
            l.enemyPostEdgeMargin = 0.8f;
            l.bossEntryStart = new Vector2(11.6f, 19.5f); // behind the back gate (street end), walks down to battlePosition
            l.spawnPoints = new[]
            {
                // 1 storefront doors (both sides): 0.8 s '!' + shake at the door, 0.5 s walk out
                P("door_L", SpawnPointType.StorefrontDoor, SpawnSide.Left, new Vector2(0.5f, 11.6f), new Vector2(2.7f, 11.3f), new Vector2(1f, -0.2f), 0.8f, 0.5f),
                P("door_R", SpawnPointType.StorefrontDoor, SpawnSide.Right, new Vector2(23.9f, 11.5f), new Vector2(21.2f, 11.3f), new Vector2(-1f, -0.2f), 0.8f, 0.5f),
                // 2 alley gaps between buildings
                P("alley_L", SpawnPointType.AlleyGap, SpawnSide.Left, new Vector2(-1.8f, 9.1f), new Vector2(0.9f, 8.9f), new Vector2(1f, 0f), 0.35f, 0.7f),
                P("alley_R", SpawnPointType.AlleyGap, SpawnSide.Right, new Vector2(25.9f, 10.2f), new Vector2(22.5f, 9.9f), new Vector2(-1f, 0f), 0.35f, 0.7f),
                // 3 street ends (foreground left / right corners), walking in from off-screen
                P("street_L", SpawnPointType.StreetEnd, SpawnSide.Left, new Vector2(-8.5f, 3.2f), new Vector2(-1.6f, 3.4f), new Vector2(1f, 0f), 0f, 1.5f),
                P("street_R", SpawnPointType.StreetEnd, SpawnSide.Right, new Vector2(32.5f, 3.2f), new Vector2(25.4f, 3.4f), new Vector2(-1f, 0f), 0f, 1.5f),
                // 4 bottom fence climbing in
                P("fence_1", SpawnPointType.FenceClimb, SpawnSide.Bottom, new Vector2(2.5f, 0.6f), new Vector2(2.5f, 2.6f), Vector2.up, 0.4f, 0.7f),
                P("fence_2", SpawnPointType.FenceClimb, SpawnSide.Bottom, new Vector2(9.0f, 0.6f), new Vector2(9.0f, 2.6f), Vector2.up, 0.4f, 0.7f),
                P("fence_3", SpawnPointType.FenceClimb, SpawnSide.Bottom, new Vector2(15.0f, 0.6f), new Vector2(15.0f, 2.6f), Vector2.up, 0.4f, 0.7f),
                P("fence_4", SpawnPointType.FenceClimb, SpawnSide.Bottom, new Vector2(21.5f, 0.6f), new Vector2(21.5f, 2.6f), Vector2.up, 0.4f, 0.7f),
                // 5 second-floor windows (balcony sniper, stationary)
                P("window_L", SpawnPointType.BalconySniper, SpawnSide.Left, new Vector2(-2.6f, 14.9f), new Vector2(-2.6f, 15.5f), Vector2.down, 0.6f, 0.4f, new Vector2(-2.6f, 15.5f)),
                P("window_R", SpawnPointType.BalconySniper, SpawnSide.Right, new Vector2(24.66f, 14.9f), new Vector2(24.66f, 15.5f), Vector2.down, 0.6f, 0.4f, new Vector2(24.66f, 15.5f)),
            };
            EditorUtility.SetDirty(l);
            return l;
        }

        private static StageSpawnPoint P(string id, SpawnPointType t, SpawnSide side, Vector2 start, Vector2 end, Vector2 dir, float tele, float dur, Vector2 telePos = default)
            => new StageSpawnPoint { id = id, type = t, side = side, entranceStart = start, position = end, direction = dir.normalized, telegraphTime = tele, entranceTime = dur, telegraphPosition = telePos };

        public static EnemyData CreateSniper()
        {
            var s = CreateOrLoad<EnemyData>(SniperPath);
            s.id = "balcony_sniper";
            s.nameKey = "pve.enemy.balcony_sniper";
            s.enemyType = EnemyType.BalconySniper;
            s.artId = "pistol_mook";                    // existing art, distinct role tint + sight line
            s.roleTint = new Color(0.72f, 0.82f, 1f);
            s.maxHp = 500;
            s.hitRadius = 0.45f;
            s.moveSpeed = 0f;
            s.stationary = true;
            s.warningTime = 1.0f;                       // long, visible sight-line wind-up
            s.sightLineTelegraph = true;
            s.attackInterval = 3.2f;
            s.attackDamage = 140;
            s.isRanged = true;
            s.attackRange = 20f;
            s.bulletSpeed = 13f;
            s.bulletRadius = 0.12f;
            s.projectilesPerShot = 1;
            s.gaugeValue = 2;
            s.scoreValue = 200;
            s.baseColor = new Color(0.45f, 0.5f, 0.65f);
            s.scale = 1f;
            s.fleesOnDefeat = false;                    // goes down / disappears in the window (no fall, no gore)
            s.fleeSpeed = 0f;
            s.bulletDodge = new HillNGun.Combat.DodgeTuning { chance = 0f, delayMin = 0.15f, delayMax = 0.3f, lookahead = 0.55f, margin = 0.15f, cooldown = 3f };
            s.keepDistance = 0f;
            EditorUtility.SetDirty(s);
            return s;
        }

        private struct E { public string enemy; public int count; public SpawnPointType type; public string point; }
        private static E W(string enemy, int count, SpawnPointType type = SpawnPointType.Any, string point = null) => new E { enemy = enemy, count = count, type = type, point = point };

        /// <summary>Per-wave entrances. Counts are the pre-v11 counts; only snipers are new (+1 in late waves).</summary>
        private static readonly Dictionary<string, E[][]> Waves = new Dictionary<string, E[][]>
        {
            ["dusty_town_1_1"] = new[]
            {
                new[] { W("pistol_mook", 3, SpawnPointType.StorefrontDoor) },
                new[] { W("pistol_mook", 4) },
                new[] { W("pistol_mook", 3, SpawnPointType.AlleyGap), W("dynamite_bandit", 1, SpawnPointType.StreetEnd) },
                new[] { W("pistol_mook", 5) },
                new[] { W("pistol_mook", 2, SpawnPointType.FenceClimb), W("dynamite_bandit", 1, SpawnPointType.StorefrontDoor), W("balcony_sniper", 1, SpawnPointType.BalconySniper, "window_R") },
            },
            ["dusty_town_1_2"] = new[]
            {
                new[] { W("pistol_mook", 4, SpawnPointType.StorefrontDoor) },
                new[] { W("pistol_mook", 3), W("dynamite_bandit", 1, SpawnPointType.StreetEnd) },
                new[] { W("mounted_raider", 1, SpawnPointType.StreetEnd), W("pistol_mook", 2, SpawnPointType.AlleyGap), W("balcony_sniper", 1, SpawnPointType.BalconySniper, "window_L") },
                new[] { W("pistol_mook", 1, SpawnPointType.FenceClimb), W("pistol_mook", 1, SpawnPointType.StreetEnd) }, // >= 2 directions
                new[] { W("pistol_mook", 4), W("dynamite_bandit", 1, SpawnPointType.StorefrontDoor) },
                new[] { W("pistol_mook", 3), W("dynamite_bandit", 1, SpawnPointType.AlleyGap), W("mounted_raider", 1, SpawnPointType.StreetEnd), W("balcony_sniper", 1, SpawnPointType.BalconySniper, "window_R") },
            },
            ["dusty_town_1_3"] = new[]
            {
                new[] { W("pistol_mook", 4) },
                new[] { W("pistol_mook", 3, SpawnPointType.StorefrontDoor), W("dynamite_bandit", 2, SpawnPointType.FenceClimb) },
                new[] { W("mounted_raider", 1, SpawnPointType.StreetEnd), W("pistol_mook", 3), W("balcony_sniper", 1, SpawnPointType.BalconySniper, "window_L") },
                new E[0], // train rest
                new[] { W("pistol_mook", 4), W("dynamite_bandit", 1, SpawnPointType.StreetEnd) },
                new[] { W("mounted_raider", 1, SpawnPointType.StreetEnd), W("pistol_mook", 2, SpawnPointType.AlleyGap), W("balcony_sniper", 1, SpawnPointType.BalconySniper, "window_R") },
                new[] { W("dynamite_bandit", 2, SpawnPointType.StorefrontDoor), W("pistol_mook", 3, SpawnPointType.FenceClimb) },
            },
        };

        private static readonly Dictionary<string, int> Seeds = new Dictionary<string, int>
        {
            ["dusty_town_1_1"] = 1101, ["dusty_town_1_2"] = 1202, ["dusty_town_1_3"] = 1303, ["dusty_town_boss"] = 1999,
        };

        public static void AssignStageSpawns(EnemyData sniper)
        {
            var byId = new Dictionary<string, EnemyData>
            {
                ["pistol_mook"] = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA + "Enemies/enemy_pistol_mook.asset"),
                ["dynamite_bandit"] = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA + "Enemies/enemy_dynamite_bandit.asset"),
                ["mounted_raider"] = AssetDatabase.LoadAssetAtPath<EnemyData>(DATA + "Enemies/enemy_mounted_raider.asset"),
                ["balcony_sniper"] = sniper,
            };
            foreach (var guid in AssetDatabase.FindAssets("t:StageData", new[] { DATA + "Stages" }))
            {
                var st = AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(guid));
                if (st == null) continue;
                if (Seeds.TryGetValue(st.id ?? "", out int seed)) st.spawnSeed = seed;
                if (st.waves != null && Waves.TryGetValue(st.id ?? "", out var table))
                {
                    for (int w = 0; w < st.waves.Length && w < table.Length; w++)
                    {
                        var list = new List<EnemySpawnEntry>();
                        foreach (var e in table[w])
                        {
                            if (!byId.TryGetValue(e.enemy, out var data) || data == null) { Debug.LogWarning("[FreeMoveSetup] missing enemy " + e.enemy); continue; }
                            list.Add(new EnemySpawnEntry { enemyData = data, count = e.count, preferredRow = 0, spawnType = e.type, spawnPointId = e.point });
                        }
                        st.waves[w].enemies = list.ToArray();
                    }
                }
                EditorUtility.SetDirty(st);
            }
        }

        private static void ApplyRules()
        {
            var r = AssetDatabase.LoadAssetAtPath<MatchRulesData>(DATA + "Match/match_rules_default.asset");
            if (r == null) return;
            r.pveMoveMode = MoveMode.FreeMove;
            r.pvpMoveMode = MoveMode.RowLane;
            r.pveMaxConcurrentEnemyFire = 3;
            r.pveSpawnSafeRadius = 3f;
            r.pveEntranceAttackDelay = 0.8f;
            r.pveSpawnStagger = 0.3f;
            r.pveSpawnRetryInterval = 0.25f;
            r.pveSpawnRelaxAfter = 2f;
            EditorUtility.SetDirty(r);
        }

        private static void ApplyFeel()
        {
            var m = AssetDatabase.LoadAssetAtPath<MovementFeelData>(DATA + "Feel/movement_feel.asset");
            if (m != null)
            {
                m.accelTime = 0.08f;   // was 0.09 (both modes, same top speed)
                m.decelTime = 0.06f;   // was 0.07
                m.freeMoveBodyRadius = 0.3f;
                m.freeMoveEnemyRadius = 0.3f;
                EditorUtility.SetDirty(m);
            }
            var v = AssetDatabase.LoadAssetAtPath<MotionViewData>(DATA + "Feel/motion_view.asset");
            if (v != null)
            {
                v.bobHeight = 0.12f;       // was 0.07
                v.enemyBobHeight = 0.12f;  // was 0.05
                v.leanMaxDeg = 6f;         // was 8
                v.enemyLeanMaxDeg = 6f;    // was 6
                v.landSquash = 0.05f;      // was 0.14
                v.flipTime = 0.08f;        // was 0.07
                v.idleBreathAmp = 0.02f;
                v.idleBreathPeriod = 1.6f;
                v.shootRecoil = 0.06f;
                v.hitRecoil = 0.1f;
                v.recoilReturnTime = 0.08f;
                v.maxActiveDust = 16;
                v.dustGroundBand = true; // v12 batching
                v.rowViewEaseTime = 0.10f; // v12: 0.15 -> 0.10 (PvP view only)
                v.rowViewSnapDistance = 3f;
                EditorUtility.SetDirty(v);
            }
        }
    }
}

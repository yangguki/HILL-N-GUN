using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Cover;
using HillNGun.Combat;
using HillNGun.Characters;
using HillNGun.Input;
using HillNGun.Progression;

namespace HillNGun.Tests.EditMode
{
    /// <summary>12_terrain_props §8 P0 checklist. Values under test are the PROPOSED (tunable) TerrainRules defaults.</summary>
    [TestFixture]
    public class TerrainP0Tests
    {
        private const string StageDir = "Assets/HillNGun/Data/Stages/";
        private const string CoverDir = "Assets/HillNGun/Data/Cover/";
        private readonly List<Object> _trash = new List<Object>();
        private readonly List<GameObject> _gos = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Unregister<CombatSystem>();
            foreach (var g in _gos) if (g != null) Object.DestroyImmediate(g);
            foreach (var o in _trash) if (o != null) Object.DestroyImmediate(o);
            _gos.Clear(); _trash.Clear();
        }

        private CoverData Cover(int hp, float width = 2f, bool rock = false)
        {
            var d = ScriptableObject.CreateInstance<CoverData>();
            d.id = rock ? "cover_rock" : "cover_test"; d.maxHp = hp; d.width = width; d.depth = 0.5f; d.stages = 4;
            d.repairable = !rock; d.indestructible = rock;
            _trash.Add(d);
            return d;
        }

        private CombatSystem Combat()
        {
            var go = new GameObject("cs"); _gos.Add(go);
            var cs = go.AddComponent<CombatSystem>();
            typeof(CombatSystem).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(cs, null);
            return cs;
        }

        private CharacterStateMachine Actor(Vector2 pos, Team team)
        {
            var roll = ScriptableObject.CreateInstance<RollData>();
            roll.distance = 2.5f; roll.duration = 0.4f; roll.invulnStart = 0f; roll.invulnDuration = 0.35f; roll.cooldown = 0f; roll.inputBuffer = 0.15f;
            var wd = ScriptableObject.CreateInstance<WeaponData>();
            var gd = ScriptableObject.CreateInstance<GrenadeData>();
            var cd = ScriptableObject.CreateInstance<CharacterData>();
            cd.maxHp = 3000; cd.moveSpeed = 5f; cd.weapon = wd; cd.grenade = gd; cd.grenadeCount = 2; cd.roll = roll; cd.hitRadius = 0.5f;
            _trash.Add(roll); _trash.Add(wd); _trash.Add(gd); _trash.Add(cd);
            var go = new GameObject("actor"); _gos.Add(go);
            var c = go.AddComponent<CharacterStateMachine>();
            c.Initialize(cd, team, 0, pos, 1);
            return c;
        }

        private static Dictionary<string, CoverData> Lib()
        {
            var lib = new Dictionary<string, CoverData>();
            foreach (var guid in AssetDatabase.FindAssets("t:CoverData", new[] { "Assets/HillNGun/Data/Cover" }))
            {
                var d = AssetDatabase.LoadAssetAtPath<CoverData>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null) lib[d.id] = d;
            }
            return lib;
        }

        private static BattlefieldData Field() => AssetDatabase.LoadAssetAtPath<BattlefieldData>("Assets/HillNGun/Data/Battlefield/battlefield_main.asset");
        private static StageData Stage(string id) => AssetDatabase.LoadAssetAtPath<StageData>(StageDir + id + ".asset");

        // ------------------------------------------------------------------ durability (§2.2)
        [Test]
        public void StageThresholds_67_34_0()
        {
            var d = Cover(1000);
            Assert.AreEqual(CoverStage.Intact, d.GetStage(1000));
            Assert.AreEqual(CoverStage.Intact, d.GetStage(670));
            Assert.AreEqual(CoverStage.Cracked, d.GetStage(665));
            Assert.AreEqual(CoverStage.Cracked, d.GetStage(340));
            Assert.AreEqual(CoverStage.Damaged, d.GetStage(335));
            Assert.AreEqual(CoverStage.Damaged, d.GetStage(1));
            Assert.AreEqual(CoverStage.Destroyed, d.GetStage(0));
        }

        [Test]
        public void Destroy_FiresOnce_EvenWithOverkillAndRepeatedHits()
        {
            var c = new CoverInstance(); c.Initialize(Cover(1000), new Vector2(5f, 5.1f), 1, Team.Player);
            int fired = 0; c.OnDestroyed += _ => fired++;
            c.TakeDamage(600); c.TakeDamage(600); c.TakeDamage(5000); c.TakeDamage(1, 1f, "barrel");
            Assert.IsTrue(c.IsDestroyed);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, c.DestroyedEvents);
            Assert.AreEqual(0, c.CurrentHp);
        }

        [Test]
        public void Repair_40Percent_LivingOnly_ClampedToMax()
        {
            float pct = TerrainRules.Data.repairPercent;
            Assert.AreEqual(0.40f, pct, 1e-4f);
            var c = new CoverInstance(); c.Initialize(Cover(1000), Vector2.zero, 0, Team.Player);
            c.TakeDamage(700); c.Repair(pct);
            Assert.AreEqual(700, c.CurrentHp);
            c.Repair(pct);
            Assert.AreEqual(1000, c.CurrentHp, "clamped to max");
            c.TakeDamage(1000); c.Repair(pct);
            Assert.IsTrue(c.IsDestroyed, "destroyed cover cannot be repaired back");
            Assert.AreEqual(0, c.CurrentHp);
            c.Reset();
            Assert.AreEqual(1000, c.CurrentHp); Assert.AreEqual(0, c.DestroyedEvents); Assert.AreEqual(CoverStage.Intact, c.Stage);
        }

        [Test]
        public void Rock_Indestructible_NotRepairable_StillBlocks()
        {
            var cs = Combat();
            var rock = new CoverInstance(); rock.Initialize(Cover(9999, 2f, true), new Vector2(10f, 5.1f), 1, Team.Player);
            cs.RegisterCover(rock);
            rock.TakeDamage(50000, 1.5f, "barrel");
            Assert.AreEqual(9999, rock.CurrentHp); Assert.IsFalse(rock.IsDestroyed);
            rock.Repair(0.4f);
            Assert.AreEqual(9999, rock.CurrentHp);
            Assert.IsTrue(cs.CoverBlocksSegment(new Vector2(10f, 12f), new Vector2(10f, 4.5f), Team.Enemy));
            var real = Lib()["cover_rock"];
            Assert.IsTrue(real.indestructible); Assert.IsFalse(real.repairable);
            Assert.AreEqual(CoverHeightClass.Full, TerrainRules.Data.ClassOf("cover_rock"));
            Assert.AreEqual(CoverHeightClass.Full, TerrainRules.Data.ClassOf("cover_wagon"));
            Assert.AreEqual(CoverHeightClass.Half, TerrainRules.Data.ClassOf("cover_trough"));
        }

        // ------------------------------------------------------------------ layout (§5 / §7)
        private static readonly (string id, int[] rows, int barrels)[] StageSpecs =
        {
            ("stage_dusty_town_1_1", new[] { 2, 3, 3 }, 1),
            ("stage_dusty_town_1_2", new[] { 3, 4, 3 }, 2),
            ("stage_dusty_town_1_3", new[] { 2, 3, 4 }, 3),
            ("stage_dusty_town_boss", new[] { 2, 4, 3 }, 2),
        };

        [Test]
        public void ProposalA_StageLayouts_PassValidator()
        {
            var lib = Lib(); var field = Field();
            Assert.IsNotNull(field);
            foreach (var (id, rows, barrels) in StageSpecs)
            {
                var s = Stage(id);
                Assert.IsNotNull(s, id);
                var issues = TerrainLayout.Validate(id, field, s.coverPlacements, s.hazards, k => lib.TryGetValue(k, out var d) ? d : null, d => d.width, false, barrels, rows);
                Assert.IsEmpty(issues, string.Join("\n", issues));
                // runtime (preset-scaled v8) widths must also not overlap / enter the centre
                var issuesRt = TerrainLayout.Validate(id + "@runtime", field, s.coverPlacements, s.hazards, k => lib.TryGetValue(k, out var d) ? d : null, d => ScalePresets.CoverSizeOf(d).logicWidth, false, barrels, rows);
                Assert.IsEmpty(issuesRt.FindAll(x => !x.Contains("gap")), string.Join("\n", issuesRt));
            }
        }

        [Test]
        public void ProposalA_CountsHpWidthsPreserved()
        {
            var lib = Lib();
            Assert.AreEqual(1000, lib["cover_swing_door"].maxHp); Assert.AreEqual(1.5f, lib["cover_swing_door"].width, 1e-4f);
            Assert.AreEqual(1500, lib["cover_barrel"].maxHp); Assert.AreEqual(1.0f, lib["cover_barrel"].width, 1e-4f);
            Assert.AreEqual(2500, lib["cover_trough"].maxHp); Assert.AreEqual(2.0f, lib["cover_trough"].width, 1e-4f);
            Assert.AreEqual(4000, lib["cover_wagon"].maxHp); Assert.AreEqual(3.0f, lib["cover_wagon"].width, 1e-4f);
            int[] totals = { 8, 10, 9, 9 };
            for (int i = 0; i < StageSpecs.Length; i++) Assert.AreEqual(totals[i], Stage(StageSpecs[i].id).coverPlacements.Length, StageSpecs[i].id);
            var s13 = Stage("stage_dusty_town_1_3");
            Assert.IsTrue(System.Array.Exists(s13.hazards, h => h.type == HazardType.TrainPass), "1-3 train hazard kept");
        }

        [Test]
        public void Coverage_DataWidths_19_20_20_of_72()
        {
            var lib = Lib(); var field = Field();
            float[] want = { 19f, 20f, 20f };
            for (int i = 0; i < 3; i++)
            {
                var placed = TerrainLayout.Place(field, Stage(StageSpecs[i].id).coverPlacements, k => lib[k], d => d.width, false);
                float frac = TerrainLayout.Coverage(placed, Team.Player, 3, 24f, out float sum);
                Assert.AreEqual(want[i], sum, 1e-3f, StageSpecs[i].id);
                Assert.AreEqual(want[i] / 72f, frac, 1e-4f);
            }
        }

        [Test]
        public void PvP_TownProps_Layout_CountsSymmetryAndPairedBarrelException()
        {
            var lib = Lib(); var field = Field();
            // planner provisional PvP town props (2026-11-07): back 4 / middle 4 / front 6
            var issues = TerrainLayout.Validate("pvp", field, field.covers, null, k => lib.TryGetValue(k, out var d) ? d : null, d => d.width, true, -1, new[] { 4, 4, 6 });
            Assert.IsEmpty(issues, string.Join("\n", issues));
            // the paired wooden barrels (centres 1.3 apart) are a declared exception to minimumVisualGap 1.6 — reported, not spread
            var notes = new List<string>(TerrainLayout.LastNotes);
            Assert.AreEqual(4, notes.Count, string.Join("\n", notes)); // 2 pairs per side
            Assert.IsTrue(notes.TrueForAll(n => n.Contains("PAIRED-PROP EXCEPTION") && n.Contains("gap 0.30")), string.Join("\n", notes));

            var placed = TerrainLayout.Place(field, field.covers, k => lib[k], d => d.width, true);
            Assert.AreEqual(28, placed.Count);
            TerrainLayout.Coverage(placed, Team.Player, 3, 24f, out float sp); TerrainLayout.Coverage(placed, Team.Enemy, 3, 24f, out float se);
            Assert.AreEqual(sp, se, 1e-4f, "fair: equal total cover per team");
            Assert.AreEqual(26f, sp, 1e-3f, "10 + 9 + 7 data width (was 30 = 10/10/10)");
            int Count(int row) { int n = 0; foreach (var p in field.covers) if (p.rowIndex == row) n++; return n; }
            Assert.AreEqual(4, Count(0)); Assert.AreEqual(4, Count(1)); Assert.AreEqual(6, Count(2));
            Assert.AreEqual(1000, lib["cover_hay"].maxHp); Assert.AreEqual(1.5f, lib["cover_hay"].width, 1e-4f); Assert.AreEqual(0.5f, lib["cover_hay"].depth, 1e-4f);
            Assert.AreEqual(CoverHeightClass.Low, TerrainRules.Data.ClassOf("cover_hay"));
            Assert.AreEqual(1500, lib["cover_barrel"].maxHp, "paired barrels keep 1500 each, separate slots");

            // every scale preset: runtime widths never overlap, never enter the centre band, stay rotationally symmetric
            var data = ScalePresets.Data;
            Assert.IsNotNull(data);
            for (int n = 1; n <= 3; n++)
            {
                if (!data.HasOption(n)) continue;
                var opt = data.GetOption(n);
                var iss = TerrainLayout.Validate($"pvp@preset{n}", field, field.covers, null, k => lib.TryGetValue(k, out var d) ? d : null, d => ScalePresets.CoverSizeFor(data, opt, d).logicWidth, true, -1, new[] { 4, 4, 6 });
                Assert.IsEmpty(iss.FindAll(x => !x.Contains(" gap ")), string.Join("\n", iss));
                float bw = ScalePresets.CoverSizeFor(data, opt, lib["cover_barrel"]).logicWidth;
                Assert.Greater(1.3f - bw, 0f, $"preset {n}: paired barrels must not overlap (w {bw})");
                Assert.Less(TerrainLayout.SafeHalfWidth(bw, 0.5f * ScalePresets.CharacterMulFor(data, opt), 0.15f), 0f, $"preset {n}: a single barrel has no guaranteed slot (pair is not fused)");
                Debug.Log($"[TerrainP0Tests] pvp preset {n}: barrel w={bw:F2} pairGap={1.3f - bw:F2} hay w={ScalePresets.CoverSizeFor(data, opt, lib["cover_hay"]).logicWidth:F2} gapNotes={TerrainLayout.LastNotes.Count} otherGapIssues={iss.FindAll(x => x.Contains(" gap ")).Count}");
            }
        }

        [Test]
        public void FrontRowException_KeepsCentreBandClear()
        {
            var field = Field(); var r = TerrainRules.Data;
            float y = CoverFieldSpawner.CoverY(field, 2, out Team t, out _);
            Assert.AreEqual(Team.Player, t);
            Assert.AreEqual(7.35f, y, 1e-4f);
            Assert.LessOrEqual(y + 0.25f, r.noCoverMinY + 1e-4f);
            float ey = CoverFieldSpawner.CoverY(field, 3, out Team et, out int er);
            Assert.AreEqual(Team.Enemy, et); Assert.AreEqual(0, er);
            Assert.AreEqual(10.65f, ey, 1e-4f);
            Assert.GreaterOrEqual(ey - 0.25f, r.noCoverMaxY - 1e-4f);
            Assert.AreEqual(5.1f, CoverFieldSpawner.CoverY(field, 1, out _, out _), 1e-4f);
        }

        // ------------------------------------------------------------------ safe width / own cover (§4)
        [Test]
        public void SafeWidth_SeparatesCrouchFromGuaranteedProtection()
        {
            // v8 preset 2 runtime numbers: hurt r 0.35 (0.5 x CharacterMul), mook bullet 0.15
            Assert.AreEqual(0.535f, TerrainLayout.SafeHalfWidth(2.07f, 0.35f, 0.15f), 1e-3f);
            Assert.Less(TerrainLayout.SafeHalfWidth(0.93f, 0.35f, 0.15f), 0f, "barrel has no guaranteed slot");
            Assert.AreEqual(1.26f, TerrainLayout.SafeHalfWidth(3.52f, 0.35f, 0.15f), 1e-3f);
            // actor inside the cover x-range but outside the safe interval: hidden, not guaranteed
            Assert.IsTrue(TerrainLayout.InCoverXRange(10f, 2.07f, 10.9f));
            Assert.IsFalse(TerrainLayout.GuaranteedSafe(10f, 2.07f, 10.9f, 0.35f, 0.15f));
            Assert.IsTrue(TerrainLayout.GuaranteedSafe(10f, 2.07f, 10.5f, 0.35f, 0.15f));

            // straight enemy shots at the safe-centre edge are stopped; just outside the hurt circle edge leaks
            var cs = Combat();
            var cover = new CoverInstance(); cover.Initialize(Cover(2500, 2.07f), new Vector2(10f, 5.1f), 1, Team.Player);
            cs.RegisterCover(cover);
            float edge = 10f + 0.535f + 0.35f; // actor at safe edge: its far hurt edge
            Assert.IsTrue(cs.CoverBlocksSegment(new Vector2(edge, 12f), new Vector2(edge, 4.5f), Team.Enemy));
        }

        [Test]
        public void OwnCover_NeverBlocksOwnShots_OwnRowCoverIsBehindMuzzle()
        {
            var cs = Combat(); var field = Field();
            var mid = new CoverInstance(); mid.Initialize(Cover(2500), new Vector2(12f, CoverFieldSpawner.CoverY(field, 1, out _, out _)), 1, Team.Player);
            var front = new CoverInstance(); front.Initialize(Cover(2500), new Vector2(12f, CoverFieldSpawner.CoverY(field, 2, out _, out _)), 2, Team.Player);
            cs.RegisterCover(mid); cs.RegisterCover(front);
            const float muzzle = 1.0f;
            // own-row: the muzzle (row y + 1.0) is already past the own-row cover's top edge
            Assert.Greater(field.GetOurRowY(1) + muzzle, mid.GetBounds().yMax);
            Assert.Greater(field.GetOurRowY(2) + muzzle, front.GetBounds().yMax);
            // other-row (mid shooter, front cover in path): current rule ownCoverBlocksOwnShots=0 -> passes (§4.5 stricter rule NOT adopted in P0)
            Assert.IsFalse(cs.CoverBlocksSegment(new Vector2(12f, field.GetOurRowY(1) + muzzle), new Vector2(12f, 16f), Team.Player));
            var rules = AssetDatabase.LoadAssetAtPath<MatchRulesData>("Assets/HillNGun/Data/Match/match_rules_default.asset");
            Assert.IsFalse(rules.ownCoverBlocksOwnShots);
        }

        // ------------------------------------------------------------------ danger barrel (§3.2 / §6.1)
        [Test]
        public void Barrel_400Arms_04Warning_NoDuplicateReservation()
        {
            var cs = Combat();
            var b = new HazardExplosiveBarrel(); b.Initialize(new Vector2(3f, 12f), cs, "t:b1");
            b.ReceiveHit(399, Vector2.up, Team.Player);
            Assert.AreEqual(BarrelState.Idle, b.State);
            b.ReceiveHit(1, Vector2.up, Team.Player);
            Assert.IsTrue(b.Armed);
            Assert.AreEqual(0.4f, b.WarningRemaining, 1e-4f);
            Assert.IsFalse(b.Arm(Team.Enemy, "grenade"), "second trigger while armed is ignored");
            b.ReceiveHit(500, Vector2.up, Team.Enemy);
            b.TriggerByGrenade(Team.Enemy);
            Assert.AreEqual(1, cs.ArmedHazards.Count);
            cs.TickHazards(0.39f);
            Assert.IsFalse(b.Exploded);
            cs.TickHazards(0.02f);
            Assert.IsTrue(b.Exploded);
            Assert.AreEqual(1, b.ExplodeCount);
            Assert.AreEqual(0, cs.ArmedHazards.Count);
            cs.TickHazards(1f); b.ReceiveHit(999, Vector2.up); b.TriggerByGrenade();
            Assert.AreEqual(1, b.ExplodeCount, "explodes once");
            Assert.IsFalse(b.CanBeHit);
        }

        [Test]
        public void Barrel_Chain_IsQueued_WithEachBarrelsOwnWarning()
        {
            var cs = Combat();
            var a = new HazardExplosiveBarrel(); a.Initialize(new Vector2(3f, 12f), cs, "a");
            var b = new HazardExplosiveBarrel(); b.Initialize(new Vector2(4.5f, 12f), cs, "b");
            int explA = 0, explB = 0; a.OnExploded += _ => explA++; b.OnExploded += _ => explB++;
            a.TriggerByGrenade(Team.Player);
            cs.TickHazards(0.41f);
            Assert.IsTrue(a.Exploded);
            Assert.IsTrue(b.Armed, "neighbour armed, not exploded in the same frame");
            Assert.IsFalse(b.Exploded);
            Assert.AreEqual(0.4f, b.WarningRemaining, 1e-4f);
            Assert.AreEqual("chain", b.TriggerSource);
            Assert.AreEqual(Team.Player, b.TriggerTeam, "attribution carries through the chain");
            cs.TickHazards(0.41f);
            Assert.IsTrue(b.Exploded);
            Assert.AreEqual(1, explA); Assert.AreEqual(1, explB);
        }

        [Test]
        public void Barrel_Damage_700Centre_350Edge_Inner05_CoverX15()
        {
            var r = TerrainRules.Data;
            Assert.AreEqual(700, HazardExplosiveBarrel.DamageAt(0f, r.barrelRadius, r.barrelInnerRadius, r.barrelCenterDamage, r.barrelEdgeDamage));
            Assert.AreEqual(700, HazardExplosiveBarrel.DamageAt(0.5f, r.barrelRadius, r.barrelInnerRadius, r.barrelCenterDamage, r.barrelEdgeDamage));
            Assert.AreEqual(350, HazardExplosiveBarrel.DamageAt(2f, r.barrelRadius, r.barrelInnerRadius, r.barrelCenterDamage, r.barrelEdgeDamage));
            Assert.AreEqual(0, HazardExplosiveBarrel.DamageAt(2.01f, r.barrelRadius, r.barrelInnerRadius, r.barrelCenterDamage, r.barrelEdgeDamage));
            var cs = Combat();
            var cover = new CoverInstance(); cover.Initialize(Cover(4000, 2f), new Vector2(3f, 12f), 4, Team.Enemy);
            cs.RegisterCover(cover);
            var b = new HazardExplosiveBarrel(); b.Initialize(new Vector2(3f, 12f), cs, "c");
            b.TriggerByGrenade(); cs.TickHazards(0.5f);
            Assert.AreEqual(4000 - 1050, cover.CurrentHp, "700 x 1.5 to covers");
        }

        [Test]
        public void Barrel_HitsBothTeams_RollingActorIgnores_CoverNotInvulnerable()
        {
            var cs = Combat();
            var standing = Actor(new Vector2(3f, 11.6f), Team.Player);
            var enemy = Actor(new Vector2(3.4f, 12.2f), Team.Enemy);
            var roller = Actor(new Vector2(2.6f, 12f), Team.Player);
            cs.RegisterCharacter(standing); cs.RegisterCharacter(enemy); cs.RegisterCharacter(roller);
            roller.OnRollCommand(new RollCommand(Vector2.right, 0f));
            roller.TickSimulation(0.05f);
            Assert.IsTrue(roller.IsInvulnerable);
            var cover = new CoverInstance(); cover.Initialize(Cover(1000, 1f), new Vector2(3f, 12.5f), 4, Team.Enemy);
            cs.RegisterCover(cover);
            var b = new HazardExplosiveBarrel(); b.Initialize(new Vector2(3f, 12f), cs, "d");
            b.TriggerByGrenade(Team.Player); cs.TickHazards(0.5f);
            Assert.AreEqual(3000 - 700, standing.CurrentHp, "friendly blast (proposal: barrels hurt everyone)");
            Assert.AreEqual(3000 - 700, enemy.CurrentHp);
            Assert.AreEqual(3000, roller.CurrentHp, "Rolling i-frames ignore the blast");
            Assert.Less(cover.CurrentHp, 1000, "covers have no i-frames");
            // after the roll ends the actor is vulnerable again (Rolling-only i-frames)
            roller.TickSimulation(0.5f);
            Assert.IsFalse(roller.IsInvulnerable);
        }

        [Test]
        public void Stage13_TwoUniqueExplosions_GiveBarrelStar()
        {
            var s = Stage("stage_dusty_town_1_3");
            Assert.AreEqual(2, s.starMinBarrelExplosions);
            Assert.AreEqual(3, StarRating.Compute(s, true, 60f, 0, 2));
            Assert.AreEqual(2, StarRating.Compute(s, true, 60f, 0, 1));
            // the same barrel can never count twice
            var cs = Combat();
            var ids = new HashSet<string>();
            var b1 = new HazardExplosiveBarrel(); b1.Initialize(new Vector2(3f, 12f), cs, "1-3:barrel:3,12");
            var b2 = new HazardExplosiveBarrel(); b2.Initialize(new Vector2(21f, 12f), cs, "1-3:barrel:21,12");
            b1.OnExploded += x => ids.Add(x.InstanceId); b2.OnExploded += x => ids.Add(x.InstanceId);
            b1.TriggerByGrenade(); cs.TickHazards(0.5f); b1.TriggerByGrenade(); cs.TickHazards(0.5f);
            Assert.AreEqual(1, ids.Count);
            b2.ReceiveHit(400, Vector2.up); cs.TickHazards(0.5f);
            Assert.AreEqual(2, ids.Count);
        }

        // ------------------------------------------------------------------ optional systems OFF + v8 regression
        [Test]
        public void OptionalSystems_DefaultOff()
        {
            var rules = AssetDatabase.LoadAssetAtPath<MatchRulesData>("Assets/HillNGun/Data/Match/match_rules_default.asset");
            Assert.IsNotNull(rules);
            Assert.IsFalse(rules.supplyCrateEnabled);
            Assert.IsFalse(rules.lateCoverDecayEnabled);
            Assert.IsFalse(rules.friendlyFire);
            Assert.IsFalse(TerrainRules.Data.pvpCoverDropsEnabled);
            Assert.IsFalse(ScalePresets.Data != null && ScalePresets.Data.layoutsOverrideStageData, "single placement authority: StageData / BattlefieldData");
            var asset = Resources.Load<TerrainRulesData>(TerrainRulesData.ResourcePath);
            Assert.IsNotNull(asset, "terrain_rules asset present");
        }

        [Test]
        public void V8_Regression_SpeedFactor_RollCooldown0()
        {
            Assert.AreEqual(0.8f, WeaponData.DefaultGlobalProjectileSpeedMul, 1e-4f);
            foreach (var id in new[] { "roll_buster", "roll_raven", "roll_volt" })
            {
                var r = AssetDatabase.LoadAssetAtPath<RollData>("Assets/HillNGun/Data/Rolls/" + id + ".asset");
                Assert.IsNotNull(r, id);
                Assert.AreEqual(0f, r.cooldown, 1e-5f, id);
                Assert.IsTrue(r.IsInvulnerable(0.1f)); Assert.IsFalse(r.IsInvulnerable(0.36f));
            }
            var a = Actor(new Vector2(12f, 4.5f), Team.Player);
            Assert.IsFalse(a.IsInvulnerable, "idle actor has no i-frames");
        }
    }
}

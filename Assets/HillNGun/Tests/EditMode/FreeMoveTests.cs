using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.Input;
using HillNGun.Battlefield;
using HillNGun.PvE;
using HillNGun.Cover;
using HillNGun.Combat;

namespace HillNGun.Tests.EditMode
{
    /// <summary>
    /// v11 PvE FreeMove: mode isolation (PvP RowLane untouched), 360 last-aim input, speed parity, swept collision +
    /// sliding, 360 roll, spawn director safety / deferral / directions, entrance immunity + attack delay, fire-token cap,
    /// layout data consistency (reachable snipers, connected walk area, no sky drop).
    /// </summary>
    [TestFixture]
    public class FreeMoveTests
    {
        private const string DataDir = "Assets/HillNGun/Data/";
        private static readonly string[] StageIds = { "stage_dusty_town_1_1", "stage_dusty_town_1_2", "stage_dusty_town_1_3", "stage_dusty_town_boss" };

        private GameObject _go, _bfGo;
        private CharacterStateMachine _c;
        private CharacterData _cd;
        private RollData _roll;
        private WeaponData _wd;
        private GrenadeData _gd;
        private bool _registeredWorld, _registeredBf;

        [SetUp]
        public void SetUp()
        {
            _roll = ScriptableObject.CreateInstance<RollData>();
            _roll.distance = 2.5f; _roll.duration = 0.4f; _roll.invulnStart = 0f; _roll.invulnDuration = 0.35f;
            _roll.cooldown = 0f; _roll.inputBuffer = 0.15f; _roll.allowRowRoll = false;
            _wd = ScriptableObject.CreateInstance<WeaponData>();
            _gd = ScriptableObject.CreateInstance<GrenadeData>();
            _cd = ScriptableObject.CreateInstance<CharacterData>();
            _cd.maxHp = 3000; _cd.moveSpeed = 5f; _cd.weapon = _wd; _cd.grenade = _gd; _cd.grenadeCount = 2; _cd.roll = _roll; _cd.hitRadius = 0.5f;
            _go = new GameObject("freemove_test");
            _c = _go.AddComponent<CharacterStateMachine>();
            _c.Initialize(_cd, Team.Player, 0, new Vector2(12f, 4.5f), 1);
        }

        [TearDown]
        public void TearDown()
        {
            if (_registeredWorld) ServiceLocator.Unregister<FreeMoveWorld>();
            if (_registeredBf) ServiceLocator.Unregister<BattlefieldSystem>();
            _registeredWorld = _registeredBf = false;
            if (_go != null) Object.DestroyImmediate(_go);
            if (_bfGo != null) Object.DestroyImmediate(_bfGo);
            Object.DestroyImmediate(_cd); Object.DestroyImmediate(_roll); Object.DestroyImmediate(_wd); Object.DestroyImmediate(_gd);
        }

        // ------------------------------------------------------------------ helpers
        private static Vector2[] Box(float x0, float y0, float x1, float y1) => new[] { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) };

        private FreeMoveWorld MakeFree(Vector2 pos, Vector2[] poly = null, params Rect[] rects)
        {
            var w = new FreeMoveWorld(poly ?? Box(0f, 0f, 24f, 18f));
            if (rects != null) w.StaticObstacles.AddRange(rects);
            ServiceLocator.Register(w); _registeredWorld = true;
            _c.SetMoveMode(MoveMode.FreeMove);
            _c.SetPosition(pos);
            return w;
        }

        private void Run(float seconds, Vector2 dir, float mag = 1f)
        {
            _c.OnMoveCommand(new MoveCommand(dir.normalized, mag, 0f));
            int n = Mathf.RoundToInt(seconds * 60f);
            for (int i = 0; i < n; i++) _c.TickSimulation(1f / 60f);
        }

        private static StreetLayoutData Layout() => AssetDatabase.LoadAssetAtPath<StreetLayoutData>(DataDir + "Layout/street_layout_dust.asset");
        private static StageData Stage(string id) => AssetDatabase.LoadAssetAtPath<StageData>(DataDir + "Stages/" + id + ".asset");
        private static BattlefieldData Field() => AssetDatabase.LoadAssetAtPath<BattlefieldData>(DataDir + "Battlefield/battlefield_main.asset");

        private static Dictionary<string, CoverData> CoverLib()
        {
            var lib = new Dictionary<string, CoverData>();
            foreach (var guid in AssetDatabase.FindAssets("t:CoverData", new[] { DataDir + "Cover" }))
            {
                var d = AssetDatabase.LoadAssetAtPath<CoverData>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null) lib[d.id] = d;
            }
            return lib;
        }

        /// <summary>Runtime cover rects of a stage (preset layout + preset widths, the same as CoverFieldSpawner).</summary>
        private static List<Rect> StageRects(string id)
        {
            var lib = CoverLib(); var s = Stage(id);
            // same source as CoverFieldSpawner: StageData unless the preset override flag is on (v12 fix: v11 read preset sets)
            bool presetOverride = ScalePresets.Data != null && ScalePresets.Data.layoutsOverrideStageData;
            var placements = presetOverride ? ScalePresets.LayoutFor(s.id, s.coverPlacements) : s.coverPlacements;
            var placed = TerrainLayout.Place(Field(), placements, k => lib.TryGetValue(k, out var d) ? d : null, d => ScalePresets.CoverSizeOf(d).logicWidth, false);
            return placed.Select(p => new Rect(p.XMin, p.YMin, p.width, p.depth)).ToList();
        }

        // ------------------------------------------------------------------ A. mode data / isolation
        [Test]
        public void ModeData_PvEFreeMove_PvPRowLane()
        {
            var r = AssetDatabase.LoadAssetAtPath<MatchRulesData>(DataDir + "Match/match_rules_default.asset");
            Assert.IsNotNull(r);
            Assert.AreEqual(MoveMode.FreeMove, r.pveMoveMode);
            Assert.AreEqual(MoveMode.RowLane, r.pvpMoveMode);
            Assert.AreEqual(3, r.pveMaxConcurrentEnemyFire);
            Assert.AreEqual(3f, r.pveSpawnSafeRadius, 1e-4f);
            Assert.AreEqual(0.8f, r.pveEntranceAttackDelay, 1e-4f);
            var feel = AssetDatabase.LoadAssetAtPath<MovementFeelData>(DataDir + "Feel/movement_feel.asset");
            Assert.AreEqual(0.08f, feel.accelTime, 1e-4f); Assert.AreEqual(0.06f, feel.decelTime, 1e-4f);
            var mv = AssetDatabase.LoadAssetAtPath<MotionViewData>(DataDir + "Feel/motion_view.asset");
            Assert.AreEqual(0.12f, mv.bobHeight, 1e-4f); Assert.AreEqual(0.05f, mv.landSquash, 1e-4f); Assert.AreEqual(6f, mv.leanMaxDeg, 1e-4f);
            Assert.AreEqual(0.02f, mv.idleBreathAmp, 1e-4f); Assert.AreEqual(1.6f, mv.idleBreathPeriod, 1e-4f);
            Assert.AreEqual(0.08f, mv.flipTime, 1e-4f); Assert.AreEqual(0.10f, mv.rowViewEaseTime, 1e-4f); Assert.AreEqual(16, mv.maxActiveDust);
        }

        [Test]
        public void FreeMove_IgnoresRowCommands_AndMovesIn2D()
        {
            MakeFree(new Vector2(12f, 5f));
            float hit = _c.HitRadius;
            _c.OnRowChangeCommand(new RowChangeCommand(1, 0f));
            Assert.AreNotEqual(MovementState.RowChanging, _c.MovementState);
            Run(0.5f, Vector2.up);
            Assert.Greater(_c.Position.y, 5f + 2f, "free vertical movement");
            Assert.AreEqual(12f, _c.Position.x, 1e-3f);
            Assert.AreEqual(hit, _c.HitRadius, 1e-6f, "hit radius unchanged by mode");
        }

        [Test]
        public void RowLane_Regression_VerticalStickDoesNotMoveY_RowChangeStillWorks()
        {
            var field = Field();
            _bfGo = new GameObject("bf");
            var bf = _bfGo.AddComponent<BattlefieldSystem>();
            typeof(BattlefieldSystem).GetField("_data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(bf, field);
            ServiceLocator.Register(bf); _registeredBf = true;
            float y1 = bf.GetRowY(1, Team.Player);
            _c.Initialize(_cd, Team.Player, 1, new Vector2(12f, y1), 1);
            Assert.AreEqual(MoveMode.RowLane, _c.MoveMode);
            Run(0.4f, new Vector2(0.3f, 0.95f));
            Assert.AreEqual(y1, _c.Position.y, 1e-5f, "RowLane: stick never moves y");
            Assert.Greater(_c.Position.x, 12.2f);
            Run(0.1f, Vector2.zero, 0f);
            _c.OnRowChangeCommand(new RowChangeCommand(1, 0f));
            for (int i = 0; i < 90; i++) _c.TickSimulation(1f / 60f);
            Assert.AreEqual(2, _c.CurrentRowIndex, "row change still works in RowLane");
            Assert.AreEqual(bf.GetRowY(2, Team.Player), _c.Position.y, 1e-3f);
            // RowLane roll stays horizontal
            float y2 = _c.Position.y;
            _c.OnRollCommand(new RollCommand(new Vector2(-1f, 1f).normalized, 0f));
            for (int i = 0; i < 30; i++) _c.TickSimulation(1f / 60f);
            Assert.AreEqual(y2, _c.Position.y, 1e-4f);
        }

        // ------------------------------------------------------------------ speed parity / accel / decel
        [Test]
        public void TopSpeed_SameInBothModes_AccelDecelTimes()
        {
            Run(1f, Vector2.right);
            float rowSpeed = Mathf.Abs(_c.Velocity.x);
            Assert.AreEqual(_cd.moveSpeed, rowSpeed, 0.05f);
            MakeFree(new Vector2(6f, 4f));
            var diag = new Vector2(1f, 1f).normalized;
            Run(1f, diag);
            Assert.AreEqual(_cd.moveSpeed, _c.FreeVelocity.magnitude, 0.05f, "diagonal top speed == data moveSpeed");
            Assert.Greater(Vector2.Dot(_c.FreeVelocity.normalized, diag), 0.999f);
            // accel from rest
            Run(0.5f, Vector2.zero, 0f);
            Assert.Less(_c.FreeVelocity.magnitude, 0.01f, "decel to rest");
            Run(0.1f, Vector2.left);
            Assert.GreaterOrEqual(_c.FreeVelocity.magnitude, 0.95f * _cd.moveSpeed, "~0.08s accel");
            Run(0.0833f, Vector2.zero, 0f);
            Assert.LessOrEqual(_c.FreeVelocity.magnitude, 0.05f * _cd.moveSpeed, "~0.06s decel");
        }

        // ------------------------------------------------------------------ collision
        [Test]
        public void Collision_SlidesAlongCover_NeverOverlaps()
        {
            var cover = new Rect(10f, 4f, 4f, 0.5f);
            var w = MakeFree(new Vector2(12f, 3.4f), null, cover);
            Run(1f, new Vector2(1f, 1f));
            Assert.IsTrue(w.IsFree(_c.Position, _c.BodyRadius));
            Assert.Greater(_c.Position.x, 14.3f, "slid along the cover face and around it");
        }

        [Test]
        public void Collision_NoTunneling_OnHugeStep()
        {
            var poly = Box(0f, 0f, 24f, 18f);
            var rects = new List<Rect> { new Rect(0f, 5f, 24f, 0.1f) };
            var p = FreeMoveCollision.Move(new Vector2(5f, 3f), new Vector2(0.3f, 12f), 0.3f, poly, rects);
            Assert.LessOrEqual(p.y, 5f - 0.3f + 1e-3f, "thin wall not tunnelled");
            var q = FreeMoveCollision.Move(new Vector2(5f, 3f), new Vector2(-40f, 0f), 0.3f, poly, rects);
            Assert.GreaterOrEqual(q.x, 0.3f - 1e-3f, "polygon edge not tunnelled");
        }

        [Test]
        public void Collision_GapBetweenWallAndCover_Passable_CornerNoLock()
        {
            var poly = Box(0f, 0f, 24f, 18f);
            var rects = new List<Rect> { new Rect(1f, 5f, 9f, 0.5f) }; // 1.0 gap to the left wall (body 0.6)
            Vector2 p = new Vector2(0.5f, 3f);
            for (int i = 0; i < 120; i++) p = FreeMoveCollision.Move(p, new Vector2(0f, 5f / 60f), 0.3f, poly, rects);
            Assert.Greater(p.y, 7f, "passes through a gap wider than the body");
            // concave corner: push into the wall/cover corner, then leave along the cover
            Vector2 c = new Vector2(2f, 4f);
            for (int i = 0; i < 60; i++) c = FreeMoveCollision.Move(c, new Vector2(-1f, 1f).normalized * (5f / 60f), 0.3f, poly, rects);
            Assert.IsTrue(FreeMoveCollision.IsFree(c, 0.3f, poly, rects));
            Vector2 before = c;
            for (int i = 0; i < 60; i++) c = FreeMoveCollision.Move(c, new Vector2(1f, 0.3f).normalized * (5f / 60f), 0.3f, poly, rects);
            Assert.Greater(c.x - before.x, 3f, "no corner lock");
        }

        [Test]
        public void Collision_Fuzz_StageCovers_AlwaysFree()
        {
            var layout = Layout();
            foreach (var id in StageIds)
            {
                var rects = StageRects(id);
                var rng = new System.Random(7);
                Vector2 p = FreeMoveCollision.Depenetrate(new Vector2(12f, 3f), 0.3f, layout.walkPolygon, rects);
                Assert.IsTrue(FreeMoveCollision.IsFree(p, 0.3f, layout.walkPolygon, rects), id);
                for (int i = 0; i < 3000; i++)
                {
                    float a = (float)(rng.NextDouble() * Mathf.PI * 2f);
                    float len = (float)rng.NextDouble() * (i % 50 == 0 ? 6f : 0.4f); // includes roll-sized jumps
                    p = FreeMoveCollision.Move(p, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * len, 0.3f, layout.walkPolygon, rects);
                    Assert.IsTrue(FreeMoveCollision.IsFree(p, 0.3f, layout.walkPolygon, rects), $"{id} step {i} {p}");
                }
            }
        }

        [Test]
        public void Depenetrate_SpawnInsideCover_IsPushedOutNearby()
        {
            var poly = Box(0f, 0f, 24f, 18f);
            var rects = new List<Rect> { new Rect(10f, 4f, 3f, 0.5f), new Rect(13f, 4f, 1f, 0.5f) };
            var p = FreeMoveCollision.Depenetrate(new Vector2(12.9f, 4.2f), 0.3f, poly, rects);
            Assert.IsTrue(FreeMoveCollision.IsFree(p, 0.3f, poly, rects));
            Assert.Less((p - new Vector2(12.9f, 4.2f)).magnitude, 1.2f);
        }

        // ------------------------------------------------------------------ roll
        [Test]
        public void Roll_FreeMove_360_InMoveDirection_AndBlockedByCover()
        {
            MakeFree(new Vector2(12f, 6f));
            var dir = new Vector2(-1f, 1f).normalized;
            _c.OnRollCommand(new RollCommand(dir, 0f));
            for (int i = 0; i < 30; i++) _c.TickSimulation(1f / 60f);
            var d = _c.Position - new Vector2(12f, 6f);
            Assert.Greater(Vector2.Dot(d.normalized, dir), 0.999f, "roll follows the 360 direction");
            Assert.AreEqual(_roll.distance, d.magnitude, 0.15f);
            // into a cover
            Object.DestroyImmediate(_go);
            ServiceLocator.Unregister<FreeMoveWorld>();
            _go = new GameObject("freemove_test2"); _c = _go.AddComponent<CharacterStateMachine>();
            _c.Initialize(_cd, Team.Player, 0, new Vector2(12f, 4.5f), 1);
            var w = MakeFree(new Vector2(12f, 3.5f), null, new Rect(11f, 4.2f, 2f, 0.4f));
            _c.OnRollCommand(new RollCommand(Vector2.up, 0f));
            for (int i = 0; i < 30; i++) { _c.TickSimulation(1f / 60f); Assert.IsTrue(w.IsFree(_c.Position, _c.BodyRadius)); }
            Assert.Less(_c.Position.y, 4.2f);
        }

        // ------------------------------------------------------------------ input
        private class FakeCtx : ITouchControlContext
        {
            public float now; public bool free = true;
            public bool FreeMove => free;
            public bool HasCharacter => true; public float Now => now;
            public Vector2 pos = new Vector2(12f, 4.5f); public Vector2 CharacterPosition => pos;
            public bool IsChargeWeapon => false; public float FireInterval => 0.3f; public bool HoldToRepeat => false;
            public float WeaponRange => 8f; public bool CanFireNow => true; public bool aiming; public bool IsAiming => aiming;
            public bool HasSkill => false; public SkillUseType SkillType => SkillUseType.Tap; public float SkillRange => 6f; public bool IsUsingSkill => false;
            public bool HasGrenade => false; public bool IsGrenadeAiming => false; public float GrenadeMinRange => 2f; public float GrenadeMaxRange => 9f; public float GrenadeRadius => 1.5f;
            public Vector2? enemy = new Vector2(15f, 4.5f);
            public Vector2? NearestEnemy(float maxRange) => enemy;
            public Vector2 ClampToField(Vector2 p) => p;
        }

        private class Rx : IInputReceiver
        {
            public readonly FakeCtx ctx; public Rx(FakeCtx c) { ctx = c; }
            public readonly List<Vector2> shots = new List<Vector2>(); public int rows;
            public void OnMoveCommand(MoveCommand c) { }
            public void OnRowChangeCommand(RowChangeCommand c) { rows++; }
            public void OnAimStartCommand(AimStartCommand c) { ctx.aiming = true; }
            public void OnAimDragCommand(AimDragCommand c) { }
            public void OnAimReleaseCommand(AimReleaseCommand c) { ctx.aiming = false; if (c.WasOutsideDeadzone) shots.Add(c.Direction); }
            public void OnRollCommand(RollCommand c) { }
            public void OnGrenadeAimStartCommand(GrenadeAimStartCommand c) { }
            public void OnGrenadeAimDragCommand(GrenadeAimDragCommand c) { }
            public void OnGrenadeReleaseCommand(GrenadeReleaseCommand c) { }
            public void OnSkillStartCommand(SkillStartCommand c) { }
            public void OnSkillHoldCommand(SkillHoldCommand c) { }
            public void OnSkillReleaseCommand(SkillReleaseCommand c) { }
        }

        [Test]
        public void Input_FreeMove_Aim360_TapFiresLastAim_NoAutoAim_NoRows()
        {
            var d = ScriptableObject.CreateInstance<TouchControlsData>();
            try
            {
                var ctx = new FakeCtx(); var rx = new Rx(ctx);
                var r = new TouchControlRouter(rx, ctx, d);
                var L = TouchControlLayout.Compute(d, new Vector2(2400, 1080), new Rect(0, 0, 2400, 1080));
                r.SetLayout(L);
                void Step() { ctx.now += 1f / 60f; r.Tick(1f / 60f); }
                foreach (float deg in new[] { 200f, 315f, 95f })
                {
                    var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
                    r.PointerDown(1, L.Attack);
                    for (int i = 1; i <= 10; i++) { Step(); r.PointerMove(1, L.Attack + dir * (L.AttackRadius * i / 10f)); }
                    r.PointerUp(1, L.Attack + dir * L.AttackRadius);
                    for (int i = 0; i < 30; i++) Step();
                    int n = rx.shots.Count;
                    Assert.Greater(Vector2.Dot(rx.shots[n - 1], dir), 0.999f, $"drag aim {deg}");
                    r.PointerDown(2, L.Attack); Step(); r.PointerUp(2, L.Attack);
                    for (int i = 0; i < 30; i++) Step();
                    Assert.AreEqual(n + 1, rx.shots.Count, "tap fires");
                    Assert.Greater(Vector2.Dot(rx.shots[n], dir), 0.999f, $"tap = last aim {deg}, not the enemy");
                }
                Assert.AreEqual(0, r.AutoAimShots, "no auto-aim in FreeMove");
                // vertical move-stick hold never sends row commands in FreeMove
                r.PointerDown(3, L.MoveRest);
                for (int i = 1; i <= 40; i++) { Step(); r.PointerMove(3, L.MoveRest + new Vector2(0f, L.MoveRadius * Mathf.Min(1f, i / 5f))); }
                r.PointerUp(3, L.MoveRest);
                Assert.AreEqual(0, rx.rows);
                // RowLane context keeps auto-aim
                var ctx2 = new FakeCtx { free = false }; var rx2 = new Rx(ctx2);
                var r2 = new TouchControlRouter(rx2, ctx2, d); r2.SetLayout(L);
                r2.PointerDown(1, L.Attack); ctx2.now += 1f / 60f; r2.Tick(1f / 60f); r2.PointerUp(1, L.Attack);
                Assert.AreEqual(1, r2.AutoAimShots);
                Assert.Greater(rx2.shots[0].x, 0.99f);
            }
            finally { Object.DestroyImmediate(d); }
        }

        // ------------------------------------------------------------------ projectiles 2D: speed once, range lifetime
        [Test]
        public void Projectile_2D_GlobalSpeedOnce_RangeLifetime()
        {
            WeaponData.ResetGlobalProjectileSpeedMul();
            var p = new Projectile();
            var dir = new Vector2(Mathf.Cos(37f * Mathf.Deg2Rad), Mathf.Sin(37f * Mathf.Deg2Rad));
            p.InitializeSimple(new Vector2(12f, 4f), dir, 10f, 0.15f, 100, 8f, Team.Player);
            Assert.AreEqual(8f, p.Speed, 1e-4f, "10 x 0.8 applied once");
            Assert.AreEqual(1f, p.Lifetime, 1e-4f);
            int frames = 0;
            while (p.IsActive && frames < 600) { p.Update(1f / 60f); frames++; }
            Assert.AreEqual(60, frames, 2);
            var travelled = p.Position - new Vector2(12f, 4f);
            Assert.Greater(Vector2.Dot(travelled.normalized, dir), 0.9999f, "2D straight flight");
        }

        // ------------------------------------------------------------------ spawn director
        private static EnemyData Mook(bool sniper = false)
        {
            var d = ScriptableObject.CreateInstance<EnemyData>();
            d.maxHp = 300; d.moveSpeed = 3f; d.attackRange = 10f; d.attackInterval = 0.5f; d.warningTime = 0.4f; d.hitRadius = 0.4f;
            d.stationary = sniper; if (sniper) d.enemyType = EnemyType.BalconySniper;
            return d;
        }

        [Test]
        public void Spawn_SafeRadius_ExcludesNearPoints_DefersThenRelaxes()
        {
            var layout = Layout(); var mook = Mook();
            try
            {
                var dl = layout.Find("door_L"); var dr = layout.Find("door_R");
                for (int seed = 1; seed <= 20; seed++)
                {
                    var dir = new EnemySpawnDirector(layout, seed) { SafeRadius = 3f };
                    dir.BeginWave(0);
                    var p = new EnemySpawnDirector.Pending { data = mook, type = SpawnPointType.StorefrontDoor, firstTry = 0f };
                    int idx = dir.Choose(p, new List<Vector2> { dl.position }, 0f, false);
                    Assert.AreEqual("door_R", layout.spawnPoints[idx].id, $"seed {seed}");
                }
                var players = new List<Vector2> { dl.position, dr.position };
                var d2 = new EnemySpawnDirector(layout, 3) { SafeRadius = 3f, RetryInterval = 0.25f, RelaxAfter = 2f };
                d2.BeginWave(0);
                d2.Enqueue(new EnemySpawnEntry { enemyData = mook, count = 1, spawnType = SpawnPointType.StorefrontDoor }, 0f, 0.3f);
                var got = new List<int>();
                float t = 0f;
                for (; t < 1.9f; t += 0.05f) d2.Tick(t, players, (pp, i) => got.Add(i));
                Assert.IsEmpty(got, "no unsafe door spawn");
                Assert.Greater(d2.Deferrals, 0);
                for (; t < 3f; t += 0.05f) d2.Tick(t, players, (pp, i) => got.Add(i));
                Assert.AreEqual(1, got.Count, "relaxed to another safe point (no infinite stall)");
                var sp = layout.spawnPoints[got[0]];
                Assert.IsTrue(d2.IsSafe(sp, players));
                Assert.IsFalse(sp.IsSniperPost);
                Assert.AreEqual(0, d2.PendingCount);
            }
            finally { Object.DestroyImmediate(mook); }
        }

        [Test]
        public void Spawn_AllStages_WavesUseTwoDirections_Deterministic_NoSkyDrop()
        {
            var layout = Layout();
            var players = new List<Vector2> { new Vector2(6f, 2.5f), new Vector2(12f, 2.5f), new Vector2(18f, 2.5f) };
            foreach (var id in StageIds)
            {
                var s = Stage(id);
                Assert.IsNotNull(s, id);
                Assert.IsNotNull(s.visual != null ? s.visual.streetLayout : null, id + " visual has a street layout");
                string Run1()
                {
                    var log = new System.Text.StringBuilder();
                    var dir = new EnemySpawnDirector(layout, s.spawnSeed);
                    for (int w = 0; w < s.waves.Length; w++)
                    {
                        dir.BeginWave(w);
                        int total = 0;
                        foreach (var e in s.waves[w].enemies)
                        {
                            Assert.IsNotNull(e.enemyData, $"{id} w{w}");
                            if (!string.IsNullOrEmpty(e.spawnPointId)) Assert.GreaterOrEqual(layout.IndexOf(e.spawnPointId), 0, e.spawnPointId);
                            dir.Enqueue(e, 0f, 0.3f); total += Mathf.Max(1, e.count);
                        }
                        var sides = new HashSet<SpawnSide>();
                        int spawned = 0;
                        for (float t = 0f; t < 40f && dir.PendingCount > 0; t += 0.05f)
                            dir.Tick(t, players, (p, i) =>
                            {
                                var sp = layout.spawnPoints[i];
                                sides.Add(sp.side); spawned++;
                                Assert.LessOrEqual(sp.entranceStart.y, 16f, "no sky drop");
                                Assert.IsTrue(dir.IsSafe(sp, players), $"{id} unsafe spawn {sp.id}");
                                log.Append(sp.id).Append(',');
                            });
                        Assert.AreEqual(0, dir.PendingCount, $"{id} w{w} stalled");
                        Assert.AreEqual(total, spawned, $"{id} w{w} all spawned");
                        if (total >= 2) Assert.GreaterOrEqual(sides.Count, 2, $"{id} wave {w} uses >= 2 directions");
                        log.Append('|');
                    }
                    return log.ToString();
                }
                Assert.AreEqual(Run1(), Run1(), id + " seeded deterministic");
            }
        }

        // ------------------------------------------------------------------ entrance immunity + attack delay
        private class Ctx : IEnemyFreeContext
        {
            public Vector2 player; public FireTokenScheduler fire = new FireTokenScheduler(3); public float now;
            public Vector2 NearestPlayerPos(Vector2 from) => player;
            public Vector2 MoveEnemy(EnemyInstance e, Vector2 delta) => e.Position + delta;
            public Vector2 PickPost(EnemyInstance e) => e.Position;
            public bool TryAcquireFire(EnemyInstance e) => fire.TryAcquire(e, now);
            public void ReleaseFire(EnemyInstance e) => fire.Release(e);
        }

        [Test]
        public void Entrance_IgnoresHits_ThenNoAttackFor08s()
        {
            var layout = Layout(); var mook = Mook();
            try
            {
                var sp = layout.Find("door_L");
                var e = new EnemyInstance();
                e.InitializeFree(mook, sp, layout.IndexOf("door_L"), 1f, 0.8f, new System.Random(1));
                var ctx = new Ctx { player = sp.position + new Vector2(2f, -4f) };
                float entrance = sp.telegraphTime + sp.entranceTime;
                float t = 0f; float firstWind = -1f; bool everHitDuring = false;
                for (int i = 0; i < 240; i++)
                {
                    e.UpdateFree(1f / 60f, ctx); t += 1f / 60f; ctx.now = t;
                    if (t < entrance - 0.02f && e.CanBeHit) everHitDuring = true;
                    if (firstWind < 0f && e.IsWindingUp) firstWind = t;
                }
                Assert.IsFalse(everHitDuring, "entrance ignores hits");
                Assert.IsTrue(e.CanBeHit);
                Assert.Greater(firstWind, entrance + 0.8f - 0.02f, "no attack for 0.8s after entering");
                Assert.Less(firstWind, entrance + 0.8f + 0.6f, "attacks soon after the delay");
            }
            finally { Object.DestroyImmediate(mook); }
        }

        // ------------------------------------------------------------------ fire cap
        [Test]
        public void FireCap_AtMostThree_FairFifo_BossReserve()
        {
            var layout = Layout(); var mook = Mook();
            try
            {
                foreach (int reserved in new[] { 0, 1 })
                {
                    var ctx = new Ctx { player = new Vector2(12f, 6f) };
                    mook.attackRange = 40f; // everyone in range: pure scheduler pressure
                    ctx.fire.Reserved = reserved;
                    var list = new List<EnemyInstance>();
                    for (int k = 0; k < 9; k++)
                    {
                        var e = new EnemyInstance();
                        var sp = layout.spawnPoints[k % layout.spawnPoints.Length];
                        e.InitializeFree(mook, sp, k, 1f, 0f, new System.Random(k + 1));
                        list.Add(e);
                    }
                    var attacks = new int[list.Count]; var was = new bool[list.Count];
                    int peak = 0;
                    for (int f = 0; f < 60 * 20; f++)
                    {
                        ctx.now = f / 60f;
                        int active = 0;
                        for (int k = 0; k < list.Count; k++)
                        {
                            list[k].UpdateFree(1f / 60f, ctx);
                            if (list[k].IsWindingUp) { active++; if (!was[k]) attacks[k]++; }
                            was[k] = list[k].IsWindingUp;
                        }
                        peak = Mathf.Max(peak, active);
                        Assert.LessOrEqual(active, 3 - reserved, $"cap frame {f}");
                    }
                    Assert.AreEqual(3 - reserved, peak, "cap reached under pressure");
                    int min = attacks.Min(), max = attacks.Max();
                    Assert.Greater(min, 3, "everyone gets turns");
                    Assert.LessOrEqual(max - min, Mathf.Max(3, max / 3), $"fair rotation min={min} max={max}");
                }
            }
            finally { Object.DestroyImmediate(mook); }
        }

        // ------------------------------------------------------------------ layout data
        [Test]
        public void Layout_PolygonValid_SpawnEndsFree_StreetEndsOffscreen()
        {
            var layout = Layout();
            Assert.IsNotNull(layout);
            var poly = layout.walkPolygon;
            Assert.Greater(FreeMoveCollision.SignedArea(poly), 0f, "CCW");
            int n = poly.Length;
            for (int i = 0; i < n; i++)
                for (int j = i + 2; j < n; j++)
                {
                    if (i == 0 && j == n - 1) continue;
                    Assert.IsFalse(SegCross(poly[i], poly[(i + 1) % n], poly[j], poly[(j + 1) % n]), $"self-intersection {i}/{j}");
                }
            var types = new HashSet<SpawnPointType>(layout.spawnPoints.Select(p => p.type));
            foreach (var t in new[] { SpawnPointType.StorefrontDoor, SpawnPointType.AlleyGap, SpawnPointType.StreetEnd, SpawnPointType.FenceClimb, SpawnPointType.BalconySniper })
                Assert.IsTrue(types.Contains(t), t.ToString());
            foreach (var id in StageIds)
            {
                var rects = StageRects(id);
                foreach (var sp in layout.spawnPoints)
                {
                    if (sp.IsSniperPost) continue;
                    Assert.IsTrue(FreeMoveCollision.IsFree(sp.position, 0.3f, poly, rects), $"{id} {sp.id} end point blocked");
                }
            }
            foreach (var sp in layout.spawnPoints.Where(p => p.type == SpawnPointType.StreetEnd))
            {
                float s = 1f - 0.2f * sp.entranceStart.y / 18f;
                float wx = (sp.entranceStart.x - 12f) * s;
                Assert.Greater(Mathf.Abs(wx), 17.78f + 0.3f, sp.id + " walks in from off-screen");
            }
        }

        private static bool SegCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float Cr(Vector2 o, Vector2 p, Vector2 q) => (p.x - o.x) * (q.y - o.y) - (p.y - o.y) * (q.x - o.x);
            float d1 = Cr(c, d, a), d2 = Cr(c, d, b), d3 = Cr(a, b, c), d4 = Cr(a, b, d);
            return d1 * d2 < 0f && d3 * d4 < 0f;
        }

        [Test]
        public void Sniper_TargetableInRangeOfEveryWeapon_AndRoleData()
        {
            var layout = Layout(); var poly = layout.walkPolygon;
            var sniper = AssetDatabase.LoadAssetAtPath<EnemyData>(DataDir + "Enemies/enemy_balcony_sniper.asset");
            Assert.IsNotNull(sniper);
            Assert.IsTrue(sniper.stationary);
            Assert.AreEqual(EnemyType.BalconySniper, sniper.enemyType);
            float hit = sniper.hitRadius * ScalePresets.CharacterMul;
            var weapons = AssetDatabase.FindAssets("t:WeaponData", new[] { DataDir + "Weapons" })
                .Select(g => AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(g))).Where(w => w != null && w.range > 0f).ToList();
            Assert.IsNotEmpty(weapons);
            foreach (var id in StageIds)
            {
                var rects = StageRects(id);
                foreach (var sp in layout.spawnPoints.Where(p => p.IsSniperPost))
                {
                    float best = float.MaxValue;
                    for (float x = 0f; x <= 24f; x += 0.2f)
                        for (float y = 0f; y <= 15f; y += 0.2f)
                        {
                            var q = new Vector2(x, y);
                            if (!FreeMoveCollision.IsFree(q, 0.3f, poly, rects)) continue;
                            best = Mathf.Min(best, (q - sp.position).magnitude);
                        }
                    foreach (var w in weapons)
                        Assert.LessOrEqual(best - hit, w.range, $"{id} {sp.id} unreachable for {w.name} ({best:0.00} vs {w.range})");
                    Assert.GreaterOrEqual(sniper.attackRange, 12f, "sniper can reach the street");
                }
            }
        }

        [Test]
        public void Stage_WalkAreaConnected_PlayerSpawnsAndEntrancesReachable()
        {
            var layout = Layout(); var poly = layout.walkPolygon;
            const float cell = 0.2f;
            foreach (var id in StageIds)
            {
                var rects = StageRects(id);
                int nx = Mathf.CeilToInt(30f / cell), ny = Mathf.CeilToInt(16f / cell);
                float ox = -4f, oy = 0f;
                var free = new bool[nx, ny]; int freeCount = 0;
                for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++)
                    { free[i, j] = FreeMoveCollision.IsFree(new Vector2(ox + i * cell, oy + j * cell), 0.3f, poly, rects); if (free[i, j]) freeCount++; }
                (int, int) Cell(Vector2 p) => (Mathf.Clamp(Mathf.RoundToInt((p.x - ox) / cell), 0, nx - 1), Mathf.Clamp(Mathf.RoundToInt((p.y - oy) / cell), 0, ny - 1));
                (int, int) Nearest(Vector2 p)
                {
                    var c = Cell(p); var bestC = c; float bd = float.MaxValue;
                    for (int i = Mathf.Max(0, c.Item1 - 8); i <= Mathf.Min(nx - 1, c.Item1 + 8); i++)
                        for (int j = Mathf.Max(0, c.Item2 - 8); j <= Mathf.Min(ny - 1, c.Item2 + 8); j++)
                            if (free[i, j]) { float dd = (new Vector2(ox + i * cell, oy + j * cell) - p).sqrMagnitude; if (dd < bd) { bd = dd; bestC = (i, j); } }
                    return bestC;
                }
                var start = Nearest(FreeMoveCollision.Depenetrate(new Vector2(12f, 2.5f), 0.3f, poly, rects));
                Assert.IsTrue(free[start.Item1, start.Item2], id);
                var seen = new bool[nx, ny]; var q = new Queue<(int, int)>(); q.Enqueue(start); seen[start.Item1, start.Item2] = true; int reach = 0;
                while (q.Count > 0)
                {
                    var (i, j) = q.Dequeue(); reach++;
                    foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        int a = i + di, b = j + dj;
                        if (a < 0 || b < 0 || a >= nx || b >= ny || seen[a, b] || !free[a, b]) continue;
                        seen[a, b] = true; q.Enqueue((a, b));
                    }
                }
                Assert.GreaterOrEqual(reach / (float)freeCount, 0.97f, $"{id} walk area connected ({reach}/{freeCount})");
                foreach (float px in new[] { 6f, 12f, 18f })
                {
                    var c = Nearest(FreeMoveCollision.Depenetrate(new Vector2(px, 2.5f), 0.3f, poly, rects));
                    Assert.IsTrue(seen[c.Item1, c.Item2], $"{id} player start {px} reachable");
                }
                foreach (var sp in layout.spawnPoints.Where(p => !p.IsSniperPost))
                {
                    var c = Nearest(sp.position);
                    Assert.IsTrue(seen[c.Item1, c.Item2], $"{id} {sp.id} reachable");
                }
            }
        }

        // ------------------------------------------------------------------ v12 ally steering (cover route / narrow gap)
        /// <summary>Drives the real CharacterStateMachine toward goal with the same AllyFreeSteer + progress formula as
        /// PvEMatchManager.DriveAllyFree (steer=false: plain greedy baseline).</summary>
        private bool SteerTo(FreeMoveWorld w, Vector2 goal, float maxT, out float t, out int sidesteps, bool steer = true)
        {
            var s = new AllyFreeSteer(); const float dt = 1f / 60f; float lastMag = 0f; Vector2 lastPos = _c.Position;
            for (t = 0f; t < maxT; t += dt)
            {
                Vector2 pos = _c.Position;
                Assert.IsTrue(w.IsFree(pos, _c.BodyRadius), $"inside collider at {pos}");
                if ((goal - pos).magnitude < 0.5f) { sidesteps = s.sidesteps + 100 * s.plans; return true; }
                float expected = lastMag * _cd.moveSpeed * dt;
                float progress = expected > 1e-4f ? Mathf.Clamp01((pos - lastPos).magnitude / expected) : 1f;
                Vector2 dir = (goal - pos).normalized;
                if (steer) dir = s.Steer(goal, dir, dt, lastMag > 0f, progress, w, pos, _c.BodyRadius);
                lastPos = pos; lastMag = 1f;
                _c.OnMoveCommand(new MoveCommand(dir, 1f, t));
                _c.TickSimulation(dt);
            }
            sidesteps = s.sidesteps + 100 * s.plans; return false;
        }

        [Test]
        public void AllySteer_CoverRoute_GoesAroundWideCover()
        {
            var w = MakeFree(new Vector2(12f, 4f), null, new Rect(8f, 6f, 8f, 0.6f));
            bool ok = SteerTo(w, new Vector2(12.3f, 9f), 6f, out float t, out int ss);
            Assert.IsTrue(ok, $"ally pinned behind cover (t={t:F1}s sidesteps={ss})");
            // baseline greedy (v11 15% stall band would also miss the slow slide): proves the steering is what gets it out
            Object.DestroyImmediate(_go); ServiceLocator.Unregister<FreeMoveWorld>();
            _go = new GameObject("freemove_test2"); _c = _go.AddComponent<CharacterStateMachine>(); _c.Initialize(_cd, Team.Player, 0, new Vector2(12f, 4f), 1);
            var w2 = MakeFree(new Vector2(12f, 4f), null, new Rect(8f, 6f, 8f, 0.6f));
            Assert.IsFalse(SteerTo(w2, new Vector2(12.3f, 9f), 6f, out _, out _, steer: false), "greedy baseline should be pinned (test meaningful)");
        }

        [Test]
        public void AllySteer_NarrowGap_PassesAndEscapesCorner()
        {
            // two covers with a 1.2 gap; goal straight through the gap but start offset under the left cover's corner
            var w = MakeFree(new Vector2(10.6f, 4f), null, new Rect(4f, 6f, 7f, 0.6f), new Rect(12.2f, 6f, 7f, 0.6f));
            Assert.IsTrue(SteerTo(w, new Vector2(11.6f, 9f), 5f, out float t, out int ss), $"no gap pass t={t:F1} ss={ss}");
            // pocket corner: L-shaped cover pair, ally pushed into the inner corner, goal behind
            Object.DestroyImmediate(_go); ServiceLocator.Unregister<FreeMoveWorld>();
            _go = new GameObject("freemove_test3"); _c = _go.AddComponent<CharacterStateMachine>(); _c.Initialize(_cd, Team.Player, 0, new Vector2(9f, 5f), 1);
            var w2 = MakeFree(new Vector2(9f, 5.3f), null, new Rect(8f, 6f, 5f, 0.6f), new Rect(8f, 3.5f, 0.6f, 3.1f));
            Assert.IsTrue(SteerTo(w2, new Vector2(7f, 9f), 6f, out t, out ss), $"corner pocket not escaped t={t:F1} ss={ss}");
        }

        [Test]
        public void AllySteer_StageCovers_EveryCoverRoutable_BothWays()
        {
            var layout = Layout(); int cases = 0, greedyFails = 0;
            foreach (var id in StageIds)
            {
                var rects = StageRects(id);
                foreach (var r in rects)
                {
                    foreach (bool up in new[] { true, false })
                    {
                        Vector2 a = new Vector2(r.center.x + 0.2f, up ? r.yMin - 0.8f : r.yMax + 0.8f);
                        Vector2 b = new Vector2(r.center.x - 0.2f, up ? r.yMax + 0.8f : r.yMin - 0.8f);
                        if (!FreeMoveCollision.IsFree(a, 0.3f, layout.walkPolygon, rects) || !FreeMoveCollision.IsFree(b, 0.3f, layout.walkPolygon, rects)) continue;
                        cases++;
                        var w = MakeFree(a, layout.walkPolygon, rects.ToArray());
                        if (!SteerTo(w, b, 1.0f + 0.0f, out _, out _, steer: false)) greedyFails++;
                        _c.SetPosition(a); _c.OnMoveCommand(MoveCommand.Stop(0f)); _c.TickSimulation(1f / 60f); _c.SetPosition(a);
                        Assert.IsTrue(SteerTo(w, b, 7f, out float t, out int ss), $"{id} cover {r} {(up ? "up" : "down")}: not routed t={t:F1}s ss={ss}");
                        ServiceLocator.Unregister<FreeMoveWorld>();
                    }
                }
            }
            Debug.Log($"[AllySteer] stage cover routes={cases} (greedy-only pinned in 1s: {greedyFails})");
            Assert.Greater(cases, 20);
        }

        // ------------------------------------------------------------------ v13 stage 1-2 mixed cover layout
        private static float RectGap(Rect a, Rect b)
        {
            float dx = Mathf.Max(0f, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static float RectToPolygon(Rect r, Vector2[] poly)
        {
            float best = float.MaxValue;
            var corners = new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
                // horizontal sidewalk / back edges are not corridors: the 0.65 slot under row-0 cover is the pre-existing
                // behind-cover standing strip (identical for the previous all-trough layout); side/diagonal walls are checked
                if (Mathf.Abs(a.y - b.y) < 1e-3f) continue;
                foreach (var c in corners)
                {
                    Vector2 ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(c - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                    best = Mathf.Min(best, (a + ab * t - c).magnitude);
                }
            }
            return best;
        }

        [Test]
        public void Stage12_MixedCover_Corridors12_AllPresets_CentreFree_RocksIndestructible()
        {
            var s = Stage("stage_dusty_town_1_2"); var lib = CoverLib(); var layout = Layout();
            var ids = s.coverPlacements.Select(p => p.coverId).Distinct().ToList();
            foreach (var need in new[] { "cover_wagon", "cover_barrel", "cover_hay", "cover_rock" }) CollectionAssert.Contains(ids, need, "mixed cover types");
            Assert.AreEqual(10, s.coverPlacements.Length, "count preserved");
            Assert.AreEqual(20f, s.coverPlacements.Sum(p => lib[p.coverId].width), 1e-3f, "width sum preserved");
            foreach (var p in s.coverPlacements) if (p.coverId == "cover_rock") Assert.IsTrue(lib[p.coverId].indestructible, "rocks indestructible");
            int destructibleHp = s.coverPlacements.Where(p => !lib[p.coverId].indestructible).Sum(p => lib[p.coverId].maxHp);
            Assert.AreEqual(19500, destructibleHp, "documented HP total (was 25,000 all troughs)");
            var data = ScalePresets.Data; Assert.IsNotNull(data);
            int presets = 0;
            foreach (var o in data.options)
            {
                if (o == null) continue; presets++;
                var placed = TerrainLayout.Place(Field(), s.coverPlacements, k => lib.TryGetValue(k, out var d) ? d : null, d => ScalePresets.CoverSizeFor(data, o, d).logicWidth, false);
                var rects = placed.Select(p => new Rect(p.XMin, p.YMin, p.width, p.depth)).ToList();
                for (int a = 0; a < rects.Count; a++)
                {
                    for (int b = a + 1; b < rects.Count; b++)
                        Assert.GreaterOrEqual(RectGap(rects[a], rects[b]), 1.2f, $"preset {o.number}: corridor between {rects[a]} and {rects[b]}");
                    Assert.GreaterOrEqual(RectToPolygon(rects[a], layout.walkPolygon), 1.2f, $"preset {o.number}: corridor to the walk edge {rects[a]}");
                    Assert.IsTrue(FreeMoveCollision.PointInPolygon(rects[a].center, layout.walkPolygon), $"preset {o.number}: cover inside the play area");
                }
                // centre of the field (row-1 band) stays open: a 2.4-wide lane around x=12 crosses every row gap
                var centre = new Rect(10.8f, 0f, 2.4f, 0f);
                float y1 = placed.Where(p => p.row == 1).Select(p => p.YMin + p.depth * 0.5f).DefaultIfEmpty(5f).First();
                centre = new Rect(10.8f, y1 - 0.6f, 2.4f, 1.2f);
                foreach (var r in rects) Assert.IsFalse(r.Overlaps(centre), $"preset {o.number}: centre of row 1 kept free ({r})");
            }
            Assert.GreaterOrEqual(presets, 1);
        }
    }
}

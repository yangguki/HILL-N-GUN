using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Input;

namespace HillNGun.Tests.EditMode
{
    /// <summary>
    /// Touch router regression tests (t47u). Root cause of "bullets do not fire on a real touch": the old IMGUI touch
    /// code fired on every drag frame from the stick centre and left the grenade / skill states open (all later shots
    /// refused). These tests pin the fixed behaviour with a recording receiver + fake character context.
    /// </summary>
    [TestFixture]
    public class TouchControlRouterTests
    {
        private class FakeCtx : ITouchControlContext
        {
            public float now;
            public bool HasCharacter => true;
            public float Now => now;
            public Vector2 pos = new Vector2(12f, 4.5f);
            public Vector2 CharacterPosition => pos;
            public bool charge;
            public bool IsChargeWeapon => charge;
            public float interval = 0.45f;
            public float FireInterval => interval;
            public bool repeat;
            public bool HoldToRepeat => repeat;
            public float WeaponRange => 7f;
            public bool canFire = true;
            public bool CanFireNow => canFire;
            public bool aiming;
            public bool IsAiming => aiming;
            public bool HasSkill => true;
            public SkillUseType skillType = SkillUseType.Tap;
            public SkillUseType SkillType => skillType;
            public float SkillRange => 6f;
            public bool usingSkill;
            public bool IsUsingSkill => usingSkill;
            public bool hasGrenade = true;
            public bool HasGrenade => hasGrenade;
            public bool grenadeAiming;
            public bool IsGrenadeAiming => grenadeAiming;
            public float GrenadeMinRange => 2f;
            public float GrenadeMaxRange => 9f;
            public float GrenadeRadius => 1.5f;
            public Vector2? enemy;
            public Vector2? NearestEnemy(float maxRange) => enemy.HasValue && Vector2.Distance(enemy.Value, pos) <= maxRange ? enemy : null;
            public Vector2 ClampToField(Vector2 p) => p;
        }

        private class Rx : IInputReceiver
        {
            public readonly FakeCtx ctx;
            public Rx(FakeCtx c) { ctx = c; }
            public readonly List<string> log = new List<string>();
            public readonly List<Vector2> shots = new List<Vector2>();
            public readonly List<float> shotTimes = new List<float>();
            public MoveCommand lastMove;
            public int moves, stops;
            public RollCommand? lastRoll;
            public GrenadeReleaseCommand? lastGrenade;
            public int skillReleases;
            public void OnMoveCommand(MoveCommand c) { lastMove = c; moves++; if (c.Magnitude <= 0f) stops++; log.Add("move"); }
            public void OnRowChangeCommand(RowChangeCommand c) { log.Add("row"); }
            public void OnAimStartCommand(AimStartCommand c) { ctx.aiming = true; log.Add("aimStart"); }
            public void OnAimDragCommand(AimDragCommand c) { log.Add("aimDrag"); }
            public void OnAimReleaseCommand(AimReleaseCommand c)
            {
                ctx.aiming = false;
                log.Add(c.WasOutsideDeadzone ? "FIRE" : "cancel");
                if (c.WasOutsideDeadzone) { shots.Add(c.Direction); shotTimes.Add(ctx.now); }
            }
            public void OnRollCommand(RollCommand c) { lastRoll = c; log.Add("roll"); }
            public void OnGrenadeAimStartCommand(GrenadeAimStartCommand c) { ctx.grenadeAiming = true; log.Add("gStart"); }
            public void OnGrenadeAimDragCommand(GrenadeAimDragCommand c) { log.Add("gDrag"); }
            public void OnGrenadeReleaseCommand(GrenadeReleaseCommand c) { ctx.grenadeAiming = false; lastGrenade = c; log.Add(c.WasOutsideDeadzone ? "THROW" : "gCancel"); }
            public void OnSkillStartCommand(SkillStartCommand c) { if (ctx.skillType != SkillUseType.Tap) ctx.usingSkill = true; log.Add("skill"); }
            public void OnSkillHoldCommand(SkillHoldCommand c) { log.Add("skillHold"); }
            public void OnSkillReleaseCommand(SkillReleaseCommand c) { ctx.usingSkill = false; skillReleases++; log.Add("skillRelease"); }
        }

        private TouchControlsData _d;
        private FakeCtx _ctx;
        private Rx _rx;
        private TouchControlRouter _r;
        private TouchLayout _L;

        [SetUp]
        public void SetUp()
        {
            _d = ScriptableObject.CreateInstance<TouchControlsData>();
            _ctx = new FakeCtx();
            _rx = new Rx(_ctx);
            _r = new TouchControlRouter(_rx, _ctx, _d);
            _L = TouchControlLayout.Compute(_d, new Vector2(2400, 1080), new Rect(0, 0, 2400, 1080));
            _r.SetLayout(_L);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(_d); }

        private void Step(float dt = 1f / 60f) { _ctx.now += dt; _r.Tick(dt); }

        private void DragAttack(int id, Vector2 to, int frames = 10)
        {
            for (int i = 1; i <= frames; i++) { Step(); _r.PointerMove(id, Vector2.Lerp(_L.Attack, to, i / (float)frames)); }
        }

        [Test]
        public void Drag_AimsWithoutFiring_ReleaseFiresExactlyOnce()
        {
            _r.PointerDown(1, _L.Attack);
            Vector2 to = _L.Attack + new Vector2(0f, _L.AttackRadius);
            DragAttack(1, to, 20);
            for (int i = 0; i < 30; i++) Step();
            Assert.AreEqual(0, _rx.shots.Count, "no shot while the aim is held (old code fired every drag frame)");
            Assert.IsTrue(_r.AttackAiming);
            _r.PointerUp(1, to);
            Assert.AreEqual(1, _rx.shots.Count);
            Assert.AreEqual(1f, _rx.shots[0].y, 0.01f, "fires along the drag from the touch-down point");
        }

        [Test]
        public void Aim_IsMeasuredFromTouchDownPoint_NotStickCentre()
        {
            // thumb lands off-centre (left of the stick centre) and drags straight up: must fire up, not to the right
            Vector2 down = _L.Attack + new Vector2(-_L.AttackRadius * 0.8f, 0f);
            _r.PointerDown(1, down);
            for (int i = 1; i <= 10; i++) { Step(); _r.PointerMove(1, down + new Vector2(0f, _L.AttackRadius * i / 10f)); }
            _r.PointerUp(1, down + new Vector2(0f, _L.AttackRadius));
            Assert.AreEqual(1, _rx.shots.Count);
            Assert.AreEqual(0f, _rx.shots[0].x, 0.01f);
            Assert.Greater(_rx.shots[0].y, 0.99f);
        }

        [Test]
        public void Tap_AutoAimsAtNearestEnemy()
        {
            _ctx.enemy = _ctx.pos + new Vector2(3f, 4f);
            _r.PointerDown(1, _L.Attack);
            Step();
            _r.PointerUp(1, _L.Attack);
            Assert.AreEqual(1, _rx.shots.Count);
            Assert.AreEqual(0.6f, _rx.shots[0].x, 0.01f);
            Assert.AreEqual(0.8f, _rx.shots[0].y, 0.01f);
            Assert.AreEqual(1, _r.AutoAimShots);
        }

        [Test]
        public void Tap_WithoutEnemy_FiresForward()
        {
            _r.PointerDown(1, _L.Attack);
            _r.PointerUp(1, _L.Attack);
            Assert.AreEqual(1, _rx.shots.Count);
            Assert.AreEqual(Vector2.up, _rx.shots[0]);
        }

        [Test]
        public void FastTaps_RespectFireInterval_QueueNotDouble()
        {
            _ctx.enemy = _ctx.pos + Vector2.up * 5f;
            _r.PointerDown(1, _L.Attack); _r.PointerUp(1, _L.Attack);
            Step(0.2f);
            _r.PointerDown(2, _L.Attack); _r.PointerUp(2, _L.Attack);
            Assert.AreEqual(1, _rx.shots.Count, "second tap inside the fire interval is queued, not doubled");
            Assert.IsTrue(_r.HasPendingShot);
            for (int i = 0; i < 30; i++) Step(0.02f);
            Assert.AreEqual(2, _rx.shots.Count, "queued tap fires as soon as the hammer is back");
            Assert.GreaterOrEqual(_rx.shotTimes[1] - _rx.shotTimes[0], _ctx.interval - 0.021f);
            // a tap way too early (more than fireQueueWindow before ready) is dropped, never fired late
            Step(0.5f);
            _r.PointerDown(3, _L.Attack); _r.PointerUp(3, _L.Attack);
            Assert.AreEqual(3, _rx.shots.Count);
            Step(0.05f);
            _r.PointerDown(4, _L.Attack); _r.PointerUp(4, _L.Attack);
            for (int i = 0; i < 30; i++) Step(0.02f);
            Assert.AreEqual(3, _rx.shots.Count);
            Assert.AreEqual(1, _r.ShotsDropped);
        }

        [Test]
        public void DragBackToCentre_Cancels()
        {
            _r.PointerDown(1, _L.Attack);
            DragAttack(1, _L.Attack + new Vector2(_L.AttackRadius, 0f));
            DragAttack(1, _L.Attack);
            _r.PointerMove(1, _L.Attack);
            _r.PointerUp(1, _L.Attack);
            Assert.AreEqual(0, _rx.shots.Count);
            Assert.Contains("cancel", _rx.log);
        }

        [Test]
        public void Multitouch_MoveKeepsGoingWhileAttackingAndThrowing()
        {
            Vector2 m0 = _L.MoveRest;
            _r.PointerDown(1, m0);
            _r.PointerMove(1, m0 + new Vector2(_L.MoveRadius, 0f));
            int stops = _rx.stops;
            Assert.Greater(_rx.lastMove.Magnitude, 0.9f);
            _r.PointerDown(2, _L.Attack); _r.PointerUp(2, _L.Attack);
            _r.PointerDown(3, _L.Grenade);
            _r.PointerMove(3, _L.Grenade + new Vector2(0f, _L.GrenadeDragFullPx));
            Assert.AreEqual(2, _r.ActivePointers);
            _r.PointerUp(3, _L.Grenade + new Vector2(0f, _L.GrenadeDragFullPx));
            Assert.AreEqual(stops, _rx.stops, "attack / grenade never stop the move stick");
            Assert.IsTrue(_r.MoveActive);
            Assert.AreEqual(1, _rx.shots.Count);
            Assert.Contains("THROW", _rx.log);
        }

        [Test]
        public void Roll_UsesMoveDirection_OrFacingWhenIdle()
        {
            _r.PointerDown(5, _L.Roll); _r.PointerUp(5, _L.Roll);
            Assert.IsTrue(_rx.lastRoll.HasValue);
            Assert.AreEqual(Vector2.zero, _rx.lastRoll.Value.Direction, "no stick input: character uses its facing");
            _r.PointerDown(1, _L.MoveRest);
            _r.PointerMove(1, _L.MoveRest + new Vector2(-_L.MoveRadius, 0f));
            _r.PointerDown(5, _L.Roll); _r.PointerUp(5, _L.Roll);
            Assert.AreEqual(-1f, _rx.lastRoll.Value.Direction.x, 0.01f);
        }

        [Test]
        public void Grenade_DragSetsDistance_ReleaseThrows_DeadzoneCancels()
        {
            _r.PointerDown(3, _L.Grenade);
            Vector2 full = _L.Grenade + new Vector2(0f, _L.GrenadeDragFullPx);
            _r.PointerMove(3, full);
            Assert.AreEqual(_ctx.pos.y + 9f, _r.GrenadeTarget.y, 0.01f, "full drag = max range");
            _r.PointerUp(3, full);
            Assert.IsTrue(_rx.lastGrenade.Value.WasOutsideDeadzone);
            Assert.IsFalse(_ctx.grenadeAiming, "grenade state always closed (old bug: stuck in GrenadeAiming)");

            _r.PointerDown(3, _L.Grenade);
            _r.PointerUp(3, _L.Grenade + new Vector2(2f, 2f));
            Assert.IsFalse(_rx.lastGrenade.Value.WasOutsideDeadzone);
            Assert.IsFalse(_ctx.grenadeAiming);
        }

        [Test]
        public void HoldSkill_GetsHoldAndRelease()
        {
            _ctx.skillType = SkillUseType.Hold;
            _r.PointerDown(4, _L.Skill);
            for (int i = 0; i < 10; i++) Step();
            Assert.Contains("skillHold", _rx.log);
            _r.PointerUp(4, _L.Skill);
            Assert.AreEqual(1, _rx.skillReleases);
            Assert.IsFalse(_ctx.usingSkill, "skill state closed so the next shot is accepted");
        }

        [Test]
        public void HoldToRepeat_FiresAtFireInterval()
        {
            _ctx.repeat = true; _ctx.interval = 0.1f;
            _r.PointerDown(1, _L.Attack);
            DragAttack(1, _L.Attack + new Vector2(0f, _L.AttackRadius), 3);
            for (int i = 0; i < 50; i++) Step(0.02f); // 1 s
            int held = _rx.shots.Count;
            Assert.That(held, Is.InRange(8, 11));
            _r.PointerUp(1, _L.Attack + new Vector2(0f, _L.AttackRadius));
            Assert.AreEqual(held, _rx.shots.Count, "release stops a repeat weapon (no extra shot)");
        }

        [Test]
        public void CancelAll_ReleasesEverything_WithoutFiring()
        {
            _r.PointerDown(1, _L.MoveRest);
            _r.PointerMove(1, _L.MoveRest + new Vector2(_L.MoveRadius, 0f));
            _r.PointerDown(2, _L.Attack);
            DragAttack(2, _L.Attack + new Vector2(0f, _L.AttackRadius));
            _r.PointerDown(3, _L.Grenade);
            _r.CancelAll();
            Assert.AreEqual(0, _rx.shots.Count);
            Assert.AreEqual(0, _r.ActivePointers);
            Assert.IsFalse(_ctx.grenadeAiming);
            Assert.AreEqual(0f, _rx.lastMove.Magnitude);
        }

        [Test]
        public void TouchOnNothing_DoesNotStealControls()
        {
            Vector2 top = new Vector2(2300f, 1050f); // top-right corner (HUD / pause area)
            _r.PointerDown(9, top);
            Assert.AreEqual(TouchControlRouter.Role.None, _r.RoleOf(9));
            _r.PointerDown(1, _L.Attack); _r.PointerUp(1, _L.Attack);
            Assert.AreEqual(1, _rx.shots.Count);
        }

        [Test]
        public void SecondFingerOnAttack_WhileFirstHolds_IsIgnored()
        {
            _r.PointerDown(1, _L.Attack);
            _r.PointerDown(2, _L.Attack);
            Assert.AreEqual(TouchControlRouter.Role.None, _r.RoleOf(2));
            _r.PointerUp(2, _L.Attack);
            Assert.AreEqual(0, _rx.shots.Count);
        }
    }

    [TestFixture]
    public class TouchControlLayoutTests
    {
        private static readonly Vector2[] Screens = { new Vector2(2560, 1600), new Vector2(2400, 1080), new Vector2(1920, 1080), new Vector2(2732, 2048), new Vector2(1280, 720) };

        [Test]
        public void Buttons_InsideSafeArea_NoOverlap_OnTabletAndPhone()
        {
            var d = ScriptableObject.CreateInstance<TouchControlsData>();
            foreach (var s in Screens)
            {
                foreach (var safe in new[] { new Rect(0, 0, s.x, s.y), new Rect(s.x * 0.04f, s.y * 0.03f, s.x * 0.92f, s.y * 0.97f) })
                {
                    var L = TouchControlLayout.Compute(d, s, safe);
                    Assert.IsTrue(L.Valid);
                    var circles = new (Vector2 c, float r)[] { (L.Attack, L.AttackRadius), (L.Roll, L.RollRadius), (L.Skill, L.SkillRadius), (L.Grenade, L.GrenadeRadius) };
                    foreach (var (c, r) in circles)
                        Assert.IsTrue(safe.Contains(c - Vector2.one * r) && safe.Contains(c + Vector2.one * r), $"{s} {safe}: control at {c} r{r} leaves the safe area");
                    for (int i = 0; i < circles.Length; i++)
                        for (int j = i + 1; j < circles.Length; j++)
                            Assert.Greater(Vector2.Distance(circles[i].c, circles[j].c), circles[i].r + circles[j].r, $"{s}: controls {i} and {j} overlap");
                    // thumb reach: the whole right cluster sits in the lower-right part of the screen
                    var b = L.BoundsOfRightCluster();
                    Assert.Greater(b.xMin, s.x * 0.55f, $"{s}: right cluster too far left");
                    Assert.Less(b.yMax, s.y * 0.72f, $"{s}: right cluster too high");
                    Assert.Less(L.MoveRest.x, s.x * 0.35f);
                }
            }
            Object.DestroyImmediate(d);
        }

        [Test]
        public void TabletUsesSmallerScaleThanPhone_SameShortSide()
        {
            var d = ScriptableObject.CreateInstance<TouchControlsData>();
            float tablet = TouchControlLayout.ComputeScale(d, new Vector2(1728, 1080), out bool isTablet);
            float phone = TouchControlLayout.ComputeScale(d, new Vector2(2400, 1080), out bool isPhoneTablet);
            Assert.IsTrue(isTablet); Assert.IsFalse(isPhoneTablet);
            Assert.Less(tablet, phone);
            Object.DestroyImmediate(d);
        }

        [Test]
        public void Cluster_MovesClearOfHudRects()
        {
            var d = ScriptableObject.CreateInstance<TouchControlsData>();
            var s = new Vector2(2400, 1080);
            var L0 = TouchControlLayout.Compute(d, s, new Rect(0, 0, s.x, s.y));
            var hud = L0.BoundsOfRightCluster();
            hud = new Rect(hud.x, 0f, hud.width, hud.yMin + hud.height * 0.3f); // a bar under the cluster
            var L = TouchControlLayout.Compute(d, s, new Rect(0, 0, s.x, s.y), new[] { hud });
            Assert.IsFalse(L.BoundsOfRightCluster().Overlaps(hud));
            Object.DestroyImmediate(d);
        }

        [Test]
        public void HitTest_RoutesToTheRightControl()
        {
            var d = ScriptableObject.CreateInstance<TouchControlsData>();
            var s = new Vector2(2560, 1600);
            var L = TouchControlLayout.Compute(d, s, new Rect(0, 0, s.x, s.y));
            Assert.AreEqual(TouchControlLayout.Hit.Attack, TouchControlLayout.HitTest(L, L.Attack));
            Assert.AreEqual(TouchControlLayout.Hit.Roll, TouchControlLayout.HitTest(L, L.Roll));
            Assert.AreEqual(TouchControlLayout.Hit.Skill, TouchControlLayout.HitTest(L, L.Skill));
            Assert.AreEqual(TouchControlLayout.Hit.Grenade, TouchControlLayout.HitTest(L, L.Grenade));
            Assert.AreEqual(TouchControlLayout.Hit.Move, TouchControlLayout.HitTest(L, new Vector2(400, 300)));
            // generous attack hit area: an off-centre thumb still lands on attack (old 150 px rect missed on tablets)
            Assert.AreEqual(TouchControlLayout.Hit.Attack, TouchControlLayout.HitTest(L, L.Attack + new Vector2(L.AttackRadius * 1.2f, -L.AttackRadius * 0.3f)));
            Object.DestroyImmediate(d);
        }
    }
}

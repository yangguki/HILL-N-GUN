using NUnit.Framework;
using UnityEngine;
using HillNGun.Proto.TopDown;

namespace HillNGun.Tests.EditMode
{
    [TestFixture]
    public class ProtoFacingTests
    {
        static ProtoFacingSelector.Result Select(
            Vector2 move, Vector2 aim, bool aiming, bool shooting,
            ProtoFacingDir facing = ProtoFacingDir.Down, bool flip = false,
            float since = 1f, float dt = 1f / 60f, float speed = 0f, float max = 5f)
        {
            return ProtoFacingSelector.Select(new ProtoFacingSelector.Input
            {
                MoveVec = move,
                AimVec = aim,
                IsAiming = aiming,
                IsShooting = shooting,
                CurrentFacing = facing,
                CurrentFlipX = flip,
                TimeSinceFacingChange = since,
                Dt = dt,
                ActualSpeed = speed,
                MaxSpeed = max
            });
        }

        [Test]
        public void Facing_ByAngle_SideUpDown_AndFlip()
        {
            // |elev from horizontal| <= 45 → side; above → up; below → down
            Assert.AreEqual(ProtoFacingDir.Side, ProtoFacingSelector.FacingFromDir(Ang(0f), ProtoFacingDir.Side, false));
            Assert.AreEqual(ProtoFacingDir.Side, ProtoFacingSelector.FacingFromDir(Ang(30f), ProtoFacingDir.Side, false));
            Assert.AreEqual(ProtoFacingDir.Up, ProtoFacingSelector.FacingFromDir(Ang(60f), ProtoFacingDir.Side, false));
            Assert.AreEqual(ProtoFacingDir.Up, ProtoFacingSelector.FacingFromDir(Ang(90f), ProtoFacingDir.Side, false));
            // 135° is up-left: |elev from horizontal| = 45° (boundary) → side, same as 45° NE
            Assert.AreEqual(ProtoFacingDir.Side, ProtoFacingSelector.FacingFromDir(Ang(135f), ProtoFacingDir.Side, false));
            Assert.AreEqual(ProtoFacingDir.Side, ProtoFacingSelector.FacingFromDir(Ang(180f), ProtoFacingDir.Side, false));
            Assert.AreEqual(ProtoFacingDir.Down, ProtoFacingSelector.FacingFromDir(Ang(-60f), ProtoFacingDir.Side, false));
            Assert.AreEqual(ProtoFacingDir.Down, ProtoFacingSelector.FacingFromDir(Ang(-90f), ProtoFacingDir.Side, false));

            var right = Select(Vector2.zero, Vector2.right, true, false, ProtoFacingDir.Side, false);
            Assert.AreEqual(ProtoFacingDir.Side, right.Facing);
            Assert.IsFalse(right.FlipX);

            var left = Select(Vector2.zero, Vector2.left, true, false, ProtoFacingDir.Side, true, since: 1f);
            Assert.AreEqual(ProtoFacingDir.Side, left.Facing);
            Assert.IsTrue(left.FlipX);
        }

        [Test]
        public void Facing_Hysteresis_Around45()
        {
            // From side: 50° stays side, 56° → up
            Assert.AreEqual(ProtoFacingDir.Side,
                ProtoFacingSelector.FacingFromElevation(50f, ProtoFacingDir.Side, true));
            Assert.AreEqual(ProtoFacingDir.Up,
                ProtoFacingSelector.FacingFromElevation(56f, ProtoFacingDir.Side, true));

            // From up: 40° stays up, 34° → side
            Assert.AreEqual(ProtoFacingDir.Up,
                ProtoFacingSelector.FacingFromElevation(40f, ProtoFacingDir.Up, true));
            Assert.AreEqual(ProtoFacingDir.Side,
                ProtoFacingSelector.FacingFromElevation(34f, ProtoFacingDir.Up, true));

            // From down: can jump straight to up when elev is steep
            Assert.AreEqual(ProtoFacingDir.Up,
                ProtoFacingSelector.FacingFromElevation(60f, ProtoFacingDir.Down, true));
        }

        [Test]
        public void Facing_MinHold_BlocksRapidChange()
        {
            // Want up, but held side for only 0.05s → stay side
            var held = Select(Vector2.zero, Vector2.up, true, false, ProtoFacingDir.Side, false, since: 0.05f, dt: 0.02f);
            Assert.AreEqual(ProtoFacingDir.Side, held.Facing);
            Assert.Less(held.TimeSinceFacingChange, ProtoFacingSelector.MinHoldSec);

            var released = Select(Vector2.zero, Vector2.up, true, false, ProtoFacingDir.Side, false, since: 0.16f, dt: 0.02f);
            Assert.AreEqual(ProtoFacingDir.Up, released.Facing);
            Assert.AreEqual(0f, released.TimeSinceFacingChange, 1e-5f);
        }

        [Test]
        public void AimOverMove_BackpedalShoot()
        {
            // Move down, aim up while shooting → facing up, anim shoot (or run if not shooting)
            var run = Select(Vector2.down, Vector2.up, true, false, ProtoFacingDir.Down, false, speed: 3f, max: 5f);
            Assert.AreEqual(ProtoFacingDir.Up, run.Facing);
            Assert.AreEqual(ProtoAnimKind.Run, run.Anim);

            var shoot = Select(Vector2.down, Vector2.up, true, true, ProtoFacingDir.Up, false, speed: 3f, max: 5f);
            Assert.AreEqual(ProtoFacingDir.Up, shoot.Facing);
            Assert.AreEqual(ProtoAnimKind.Shoot, shoot.Anim);
        }

        [Test]
        public void IdleRun_BySpeed_AndRunRateClamp()
        {
            var idle = Select(Vector2.zero, Vector2.down, false, false, speed: 0f);
            Assert.AreEqual(ProtoAnimKind.Idle, idle.Anim);

            var slow = Select(Vector2.right, Vector2.right, false, false, ProtoFacingDir.Side, false, speed: 1f, max: 5f);
            Assert.AreEqual(ProtoAnimKind.Run, slow.Anim);
            Assert.AreEqual(ProtoFacingSelector.RunBaseFps * ProtoFacingSelector.RunRateMin, slow.PlaybackFps, 1e-4f);

            var mid = Select(Vector2.right, Vector2.right, false, false, ProtoFacingDir.Side, false, speed: 5f, max: 5f);
            Assert.AreEqual(ProtoFacingSelector.RunBaseFps * 1f, mid.PlaybackFps, 1e-4f);

            var fast = Select(Vector2.right, Vector2.right, false, false, ProtoFacingDir.Side, false, speed: 10f, max: 5f);
            Assert.AreEqual(ProtoFacingSelector.RunBaseFps * ProtoFacingSelector.RunRateMax, fast.PlaybackFps, 1e-4f);
        }

        [Test]
        public void AccelDecel_DirectionSnap_AndTimeConstant()
        {
            // ~63% of target in one time constant
            var v = ProtoFacingSelector.SmoothVelocity(Vector2.zero, Vector2.right * 5f, 0.08f, 0.08f);
            Assert.AreEqual(5f * (1f - Mathf.Exp(-1f)), v.magnitude, 0.05f);
            Assert.Greater(Vector2.Dot(v.normalized, Vector2.right), 0.999f);

            // Instant direction snap: was moving right, wish left — direction flips, mag eases
            var cur = Vector2.right * 5f;
            var next = ProtoFacingSelector.SmoothVelocity(cur, Vector2.left * 5f, 0.02f, 0.08f);
            Assert.Greater(Vector2.Dot(next.normalized, Vector2.left), 0.999f);
            Assert.Greater(next.magnitude, 4f);
        }

        [Test]
        public void MetaParse_IdleRunShootFrameCounts()
        {
            const string json = @"{
 ""character"": ""buster"",
 ""stage"": 1,
 ""anims"": {
  ""down/idle"": {
   ""frames"": [""frames/down/idle/a_00.png"",""frames/down/idle/a_01.png"",""frames/down/idle/a_02.png"",""frames/down/idle/a_03.png""],
   ""fps"": 6, ""loop"": true, ""pivot"": [64,124], ""flip_x_for_left"": false
  },
  ""down/run"": {
   ""frames"": [""frames/down/run/a_00.png"",""frames/down/run/a_01.png"",""frames/down/run/a_02.png"",""frames/down/run/a_03.png"",""frames/down/run/a_04.png"",""frames/down/run/a_05.png"",""frames/down/run/a_06.png"",""frames/down/run/a_07.png""],
   ""fps"": 12, ""loop"": true, ""pivot"": [64,124], ""flip_x_for_left"": false
  },
  ""down/shoot"": {
   ""frames"": [""frames/down/shoot/a_00.png"",""frames/down/shoot/a_01.png"",""frames/down/shoot/a_02.png""],
   ""fps"": 12, ""loop"": false, ""pivot"": [64,124], ""flip_x_for_left"": false
  },
  ""side/idle"": {
   ""frames"": [""frames/side/idle/a_00.png"",""frames/side/idle/a_01.png"",""frames/side/idle/a_02.png"",""frames/side/idle/a_03.png""],
   ""fps"": 6, ""loop"": true, ""pivot"": [64,124], ""flip_x_for_left"": true
  },
  ""side/run"": {
   ""frames"": [""frames/side/run/a_00.png"",""frames/side/run/a_01.png"",""frames/side/run/a_02.png"",""frames/side/run/a_03.png"",""frames/side/run/a_04.png"",""frames/side/run/a_05.png"",""frames/side/run/a_06.png"",""frames/side/run/a_07.png""],
   ""fps"": 12, ""loop"": true, ""pivot"": [64,124], ""flip_x_for_left"": true
  },
  ""side/shoot"": {
   ""frames"": [""frames/side/shoot/a_00.png"",""frames/side/shoot/a_01.png"",""frames/side/shoot/a_02.png""],
   ""fps"": 12, ""loop"": false, ""pivot"": [64,124], ""flip_x_for_left"": true
  },
  ""up/idle"": {
   ""frames"": [""frames/up/idle/a_00.png"",""frames/up/idle/a_01.png"",""frames/up/idle/a_02.png"",""frames/up/idle/a_03.png""],
   ""fps"": 6, ""loop"": true, ""pivot"": [64,124], ""flip_x_for_left"": false
  },
  ""up/run"": {
   ""frames"": [""frames/up/run/a_00.png"",""frames/up/run/a_01.png"",""frames/up/run/a_02.png"",""frames/up/run/a_03.png"",""frames/up/run/a_04.png"",""frames/up/run/a_05.png"",""frames/up/run/a_06.png"",""frames/up/run/a_07.png""],
   ""fps"": 12, ""loop"": true, ""pivot"": [64,124], ""flip_x_for_left"": false
  },
  ""up/shoot"": {
   ""frames"": [""frames/up/shoot/a_00.png"",""frames/up/shoot/a_01.png"",""frames/up/shoot/a_02.png""],
   ""fps"": 12, ""loop"": false, ""pivot"": [64,124], ""flip_x_for_left"": false
  }
 }
}";
            Assert.IsTrue(ProtoFlipbookMeta.TryParse(json, out var stage));
            Assert.AreEqual("buster", stage.Character);
            Assert.AreEqual(9, stage.Anims.Count);
            foreach (var a in stage.Anims)
            {
                Assert.AreEqual(ProtoFlipbookMeta.ExpectedFrameCount(a.Anim), a.Frames.Length, a.Key);
                if (a.Facing == ProtoFacingDir.Side) Assert.IsTrue(a.FlipXForLeft);
                else Assert.IsFalse(a.FlipXForLeft);
                Assert.AreEqual(64f, a.PivotPx.x, 0.01f);
                Assert.AreEqual(124f, a.PivotPx.y, 0.01f);
            }
            Assert.AreEqual(0.03125f, ProtoFlipbookMeta.PivotY, 1e-6f);
        }

        static Vector2 Ang(float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }
    }
}

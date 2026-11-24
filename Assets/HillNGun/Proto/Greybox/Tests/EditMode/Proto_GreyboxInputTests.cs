using System.Collections.Generic;
using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxInputTests
    {
        const float Dt = 0.02f;
        const float Epsilon = 0.02f;

        [SetUp]
        public void SetUp()
        {
            Proto_Greybox3DHudLayout.Enabled = false;
            Proto_GreyboxPersp2DHud.Restore();
        }

        [Test]
        public void Drag_RightDrag_WorldDeltaPositiveX()
        {
            var drag = Proto_GreyboxPointerMath.ScreenDeltaToWorldDrag(new Vector2(120f, 0f), 1920f, 1080f);
            Assert.Greater(drag.x, 0f);
            Assert.Less(Mathf.Abs(drag.y), Epsilon);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Drag_UpDrag_WorldDeltaPositiveY_AimTowardEnemies()
        {
            var drag = Proto_GreyboxPointerMath.ScreenDeltaToWorldDrag(new Vector2(0f, 200f), 1920f, 1080f);
            Assert.Greater(drag.y, 0f, "upward screen drag must map to +world Y (enemy line)");
            Assert.Less(Mathf.Abs(drag.x), Epsilon);

            var foot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            var aim = new Proto_GreyboxAim();
            aim.BeginDrag(foot, new List<Proto_GreyboxTarget>());
            aim.SetAimDirection(drag);
            Assert.Greater(aim.AimDirection.y, 0f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Joystick_CenterTouch_AnalogNearZero()
        {
            AssertJoystickCenter(1920f, 1080f);
            AssertJoystickCenter(2400f, 1080f);
            LogAssert.NoUnexpectedReceived();
        }

        static void AssertJoystickCenter(float w, float h)
        {
            var center = Proto_GreyboxPointerMath.JoystickVisualCenterScreen(w, h);
            var analog = Proto_GreyboxPointerMath.TouchJoystickAnalog(center, w, h);
            Assert.Less(analog.magnitude, 0.08f, $"center touch at {w}x{h} must not bias +Y");
        }

        [Test]
        public void Joystick_RightDrag_PositiveX_UpDrag_PositiveY_DownDrag_NegativeY()
        {
            const float w = 1920f;
            const float h = 1080f;
            var center = Proto_GreyboxPointerMath.JoystickVisualCenterScreen(w, h);
            float knob = w * 0.06f;

            var right = Proto_GreyboxPointerMath.TouchJoystickAnalog(center + new Vector2(knob, 0f), w, h);
            Assert.Greater(right.x, 0.5f);
            Assert.Less(Mathf.Abs(right.y), 0.2f);

            var up = Proto_GreyboxPointerMath.TouchJoystickAnalog(center + new Vector2(0f, knob), w, h);
            Assert.Greater(up.y, 0.5f);

            var down = Proto_GreyboxPointerMath.TouchJoystickAnalog(center + new Vector2(0f, -knob), w, h);
            Assert.Less(down.y, -0.5f);

            // Start mid-zone: Vector2.zero / min Y is already clamped, so down cannot move further.
            var midY = (Proto_GreyboxMoveZone.MinFootWorldY + Proto_GreyboxMoveZone.MaxFootWorldY) * 0.5f;
            var motor = new Proto_GreyboxPlayerMotor(new Vector2(0f, midY));
            var y0 = motor.FootPosition.y;
            motor.TickMove(down, Dt * 30f);
            Assert.Less(motor.FootPosition.y, y0);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Aim_TapNearZeroDrag_AutoAimNearestLiving()
        {
            const float w = 1920f;
            const float h = 1080f;
            var footY = Proto_GreyboxLayout.EnemyFarFootWorldY;
            var playerFoot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            var near = new Proto_GreyboxTarget(new Vector2(0.2f, footY));
            var far = new Proto_GreyboxTarget(new Vector2(4f, footY));
            var list = new List<Proto_GreyboxTarget> { far, near };

            var aim = new Proto_GreyboxAim();
            aim.BeginDrag(playerFoot, list);
            aim.SetAimStickScreenDelta(new Vector2(2f, 1f), w, h, Dt);
            var intent = aim.EndDrag();
            Assert.AreEqual(Proto_GreyboxAimReleaseIntent.AutoAim, intent);
            Assert.AreSame(near, aim.SelectedTarget);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Aim_DragOutThenReturnToCenter_Cancel()
        {
            const float w = 1920f;
            const float h = 1080f;
            var playerFoot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            float stickR = Proto_GreyboxPointerMath.AimStickRadiusPx(h,
                Proto_GreyboxPointerMath.AimStickRadiusScreenHeightPercent);
            var pull = new Vector2(0f, stickR * 0.55f);
            var tiny = new Vector2(0f, stickR * 0.02f);

            var aim = new Proto_GreyboxAim();
            aim.BeginDrag(playerFoot, new List<Proto_GreyboxTarget>());
            aim.SetAimStickScreenDelta(pull, w, h, Dt);
            aim.SetAimStickScreenDelta(tiny, w, h, Dt);
            var intent = aim.EndDrag();
            Assert.AreEqual(Proto_GreyboxAimReleaseIntent.Cancel, intent);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Drag_SameScreenPercent_WorldXScalesWithAspect()
        {
            // Equal screen-percent drags are NOT equal visual angles on different aspects.
            // World Y (fixed height) stays the same; world X scales with aspect.
            // Pixel-equal drags (45°) are covered by Drag_100PxDiagonal_Approx45Degrees_On1920And2400.
            const float pctX = 0.08f;
            const float pctY = 0.12f;
            var d1920 = PercentDragWorldDelta(1920f, 1080f, pctX, pctY);
            var d2400 = PercentDragWorldDelta(2400f, 1080f, pctX, pctY);

            Assert.Greater(d1920.x, 0f);
            Assert.Greater(d1920.y, 0f, "upward percent drag must stay +Y (no flip)");
            Assert.Greater(d2400.x, 0f);
            Assert.AreEqual(d1920.y, d2400.y, 0.001f);
            float aspectRatio = (2400f / 1080f) / (1920f / 1080f);
            Assert.AreEqual(d1920.x * aspectRatio, d2400.x, 0.001f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Drag_100PxDiagonal_Approx45Degrees_On1920And2400()
        {
            const float delta = 100f;
            var dragScreen = new Vector2(delta, delta);
            var d1920 = Proto_GreyboxPointerMath.ScreenDeltaToWorldDrag(dragScreen, 1920f, 1080f);
            var d2400 = Proto_GreyboxPointerMath.ScreenDeltaToWorldDrag(dragScreen, 2400f, 1080f);
            float angle1920 = Mathf.Atan2(d1920.y, d1920.x) * Mathf.Rad2Deg;
            float angle2400 = Mathf.Atan2(d2400.y, d2400.x) * Mathf.Rad2Deg;
            Assert.AreEqual(45f, angle1920, 0.5f);
            Assert.AreEqual(45f, angle2400, 0.5f);
            Assert.Less(Mathf.Abs(angle1920 - angle2400), 0.1f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Drag_100PxDiagonal_WideScreenDiffersFromLegacy16x9LayoutWidth()
        {
            var dragScreen = new Vector2(100f, 100f);
            var modern = Proto_GreyboxPointerMath.ScreenDeltaToWorldDrag(dragScreen, 2400f, 1080f);
            var legacy = Proto_GreyboxPointerMath.ScreenDeltaToWorldDragFixedLayout16x9(dragScreen, 2400f, 1080f);
            float modernAngle = Mathf.Atan2(modern.y, modern.x) * Mathf.Rad2Deg;
            float legacyAngle = Mathf.Atan2(legacy.y, legacy.x) * Mathf.Rad2Deg;
            Assert.Greater(Mathf.Abs(modernAngle - legacyAngle), 1f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void HitCircles_Cluster_Disjoint_AndPositionsRelativeToFire()
        {
            Assert.IsFalse(Proto_GreyboxCombatInputGate.CircleHitsOverlap(
                Proto_GreyboxHudBuilder.ReloadCenterX, Proto_GreyboxHudBuilder.ReloadCenterY,
                Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent,
                Proto_GreyboxHudBuilder.RollCenterX, Proto_GreyboxHudBuilder.RollCenterY,
                Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent));
            Assert.IsFalse(Proto_GreyboxCombatInputGate.CircleHitsOverlap(
                Proto_GreyboxHudBuilder.ReloadCenterX, Proto_GreyboxHudBuilder.ReloadCenterY,
                Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent,
                Proto_GreyboxHudBuilder.FireCenterX, Proto_GreyboxHudBuilder.FireCenterY,
                Proto_GreyboxHudBuilder.FirePlaceRadiusPercent));
            Assert.IsFalse(Proto_GreyboxCombatInputGate.CircleHitsOverlap(
                Proto_GreyboxHudBuilder.GrenadeCenterX, Proto_GreyboxHudBuilder.GrenadeCenterY,
                Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent,
                Proto_GreyboxHudBuilder.FireCenterX, Proto_GreyboxHudBuilder.FireCenterY,
                Proto_GreyboxHudBuilder.FirePlaceRadiusPercent));
            Assert.IsFalse(Proto_GreyboxCombatInputGate.CircleHitsOverlap(
                Proto_GreyboxHudBuilder.RollCenterX, Proto_GreyboxHudBuilder.RollCenterY,
                Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent,
                Proto_GreyboxHudBuilder.FireCenterX, Proto_GreyboxHudBuilder.FireCenterY,
                Proto_GreyboxHudBuilder.FirePlaceRadiusPercent));

            float expect = Proto_GreyboxHudBuilder.ExpectedClusterCenterDistancePx(
                Proto_GreyboxHudBuilder.FirePlaceRadiusPercent,
                Proto_GreyboxHudBuilder.SmallActionPlaceRadiusPercent);
            float reloadGap = Proto_GreyboxHudBuilder.ClusterCenterDistancePx(Proto_GreyboxHudBuilder.FireCenterX,
                Proto_GreyboxHudBuilder.FireCenterY, Proto_GreyboxHudBuilder.ReloadCenterX,
                Proto_GreyboxHudBuilder.ReloadCenterY);
            float grenadeGap = Proto_GreyboxHudBuilder.ClusterCenterDistancePx(Proto_GreyboxHudBuilder.FireCenterX,
                Proto_GreyboxHudBuilder.FireCenterY, Proto_GreyboxHudBuilder.GrenadeCenterX,
                Proto_GreyboxHudBuilder.GrenadeCenterY);
            float rollGap = Proto_GreyboxHudBuilder.ClusterCenterDistancePx(Proto_GreyboxHudBuilder.FireCenterX,
                Proto_GreyboxHudBuilder.FireCenterY, Proto_GreyboxHudBuilder.RollCenterX,
                Proto_GreyboxHudBuilder.RollCenterY);
            Assert.AreEqual(expect, reloadGap, 4f);
            Assert.AreEqual(expect, grenadeGap, 4f);
            Assert.AreEqual(expect, rollGap, 4f);

            Assert.Less(Proto_GreyboxHudBuilder.ReloadCenterX, Proto_GreyboxHudBuilder.FireCenterX);
            Assert.Less(Proto_GreyboxHudBuilder.ReloadCenterY, Proto_GreyboxHudBuilder.FireCenterY);
            Assert.Less(Proto_GreyboxHudBuilder.GrenadeCenterX, Proto_GreyboxHudBuilder.FireCenterX);
            Assert.AreEqual(Proto_GreyboxHudBuilder.FireCenterY, Proto_GreyboxHudBuilder.GrenadeCenterY, 0.01f);
            Assert.AreEqual(Proto_GreyboxHudBuilder.FireCenterX, Proto_GreyboxHudBuilder.RollCenterX, 0.01f);
            Assert.Less(Proto_GreyboxHudBuilder.RollCenterY, Proto_GreyboxHudBuilder.FireCenterY);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AimStick_AngleMatchesDrag_IndependentOfPullDistance()
        {
            const float w = 1920f;
            const float h = 1080f;
            var playerFoot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            float stickR = Proto_GreyboxPointerMath.AimStickRadiusPx(h,
                Proto_GreyboxPointerMath.AimStickRadiusScreenHeightPercent);
            var shortDrag = new Vector2(stickR * 0.25f, stickR * 0.25f);
            var longDrag = new Vector2(stickR * 0.85f, stickR * 0.85f);
            var aimShort = new Proto_GreyboxAim();
            var aimLong = new Proto_GreyboxAim();
            aimShort.BeginDrag(playerFoot, new List<Proto_GreyboxTarget>());
            aimLong.BeginDrag(playerFoot, new List<Proto_GreyboxTarget>());
            for (int i = 0; i < 60; i++)
            {
                aimShort.SetAimStickScreenDelta(shortDrag, w, h, Dt);
                aimLong.SetAimStickScreenDelta(longDrag, w, h, Dt);
            }

            float angShort = Mathf.Atan2(aimShort.AimDirection.y, aimShort.AimDirection.x) * Mathf.Rad2Deg;
            float angLong = Mathf.Atan2(aimLong.AimDirection.y, aimLong.AimDirection.x) * Mathf.Rad2Deg;
            Assert.AreEqual(angShort, angLong, 1.5f);
            var refDir = Proto_GreyboxPointerMath.WorldDirectionFromScreenDrag(longDrag, w, h);
            float refAng = Mathf.Atan2(refDir.y, refDir.x) * Mathf.Rad2Deg;
            Assert.AreEqual(refAng, angLong, 2f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AimStick_SmoothingConvergesTowardTargetAngle()
        {
            const float w = 1920f;
            const float h = 1080f;
            var playerFoot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            float stickR = Proto_GreyboxPointerMath.AimStickRadiusPx(h,
                Proto_GreyboxPointerMath.AimStickRadiusScreenHeightPercent);
            var initial = new Vector2(0f, stickR * 0.4f);
            var drag = new Vector2(stickR * 0.6f, 0f);
            var aim = new Proto_GreyboxAim();
            aim.BeginDrag(playerFoot, new List<Proto_GreyboxTarget>());
            for (int i = 0; i < 5; i++)
                aim.SetAimStickScreenDelta(initial, w, h, Dt);
            float elapsed = 0f;
            while (elapsed < 0.95f)
            {
                aim.SetAimStickScreenDelta(drag, w, h, Dt);
                elapsed += Dt;
            }

            var refDir = Proto_GreyboxPointerMath.WorldDirectionFromScreenDrag(drag, w, h);
            float err = Vector2.Angle(aim.AimDirection, refDir);
            Assert.Less(err, 3.5f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Joystick_OnlyBeganInsideZone_Captures_NotSlipIn()
        {
            float inX = (Proto_GreyboxHudBuilder.JoystickHitX0 + Proto_GreyboxHudBuilder.JoystickHitX1) * 0.5f;
            float inY = (Proto_GreyboxHudBuilder.JoystickHitY0 + Proto_GreyboxHudBuilder.JoystickHitY1) * 0.5f;
            float outX = Proto_GreyboxHudBuilder.JoystickHitX1 + 5f;
            Assert.IsTrue(Proto_GreyboxCombatInputGate.JoystickCaptureOnBegan(inX, inY, true));
            Assert.IsFalse(Proto_GreyboxCombatInputGate.JoystickCaptureOnBegan(inX, inY, false));
            Assert.IsFalse(Proto_GreyboxCombatInputGate.JoystickCaptureOnBegan(outX, inY, true));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Pause_StripsFireStickAndQuickAuto()
        {
            var input = new Proto_GreyboxInput
            {
                FireQuickAuto = true,
                FireStickReleased = true,
                FireStickDragWorld = new Vector2(1f, 2f),
                GrenadeStickHeld = true,
                GrenadeStickReleased = true,
                GrenadeStickDragWorld = new Vector2(3f, 4f),
                Move = Vector2.right,
                RollPressed = true,
                ReloadPressed = true
            };
            var stripped = Proto_GreyboxPlayerController.FilterCombatInputWhenPaused(input, true);
            Assert.IsFalse(stripped.FireQuickAuto);
            Assert.IsFalse(stripped.FireStickReleased);
            Assert.AreEqual(Vector2.zero, stripped.FireStickDragWorld);
            Assert.IsFalse(stripped.GrenadeStickHeld);
            Assert.IsFalse(stripped.GrenadeStickReleased);
            Assert.AreEqual(Vector2.zero, stripped.GrenadeStickDragWorld);
            Assert.AreEqual(Vector2.zero, stripped.Move);
            Assert.IsFalse(stripped.RollPressed);
            Assert.IsFalse(stripped.ReloadPressed);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Aim_AngularSpeed_OverSmallDt_NeverExceedsCap()
        {
            const float w = 1920f;
            const float h = 1080f;
            var cur = Vector2.up;
            var target = Vector2.right;
            float cap = Proto_GreyboxPointerMath.AimAngularSpeedCapDegPerSec;
            for (int i = 0; i < 30; i++)
            {
                float prev = Mathf.Atan2(cur.y, cur.x) * Mathf.Rad2Deg;
                cur = Proto_GreyboxPointerMath.SmoothAimDirection(cur, target, Dt,
                    Proto_GreyboxPointerMath.AimStickSmoothTime, cap,
                    Proto_GreyboxPointerMath.AimFineAimExponent);
                float now = Mathf.Atan2(cur.y, cur.x) * Mathf.Rad2Deg;
                float step = Mathf.Abs(Mathf.DeltaAngle(prev, now));
                Assert.LessOrEqual(step, cap * Dt + 0.25f);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Aim_FineAimExponent_SmallErrorAdvancesLessThanLinear()
        {
            float startDeg = 90f;
            float targetDeg = 78f; // ~12° error, inside the fine-aim band
            var start = new Vector2(Mathf.Cos(startDeg * Mathf.Deg2Rad), Mathf.Sin(startDeg * Mathf.Deg2Rad));
            var target = new Vector2(Mathf.Cos(targetDeg * Mathf.Deg2Rad), Mathf.Sin(targetDeg * Mathf.Deg2Rad));
            const float tau = 0.12f;
            var fine = Proto_GreyboxPointerMath.SmoothAimDirection(start, target, Dt, tau, 480f, 1.5f);
            var linear = Proto_GreyboxPointerMath.SmoothAimDirection(start, target, Dt, tau, 480f, 1f);
            float fineErr = Vector2.Angle(fine, target);
            float linearErr = Vector2.Angle(linear, target);
            Assert.Greater(fineErr, linearErr * 0.92f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Aim_LargeFlickStillConvergesTowardTarget()
        {
            const float w = 1920f;
            const float h = 1080f;
            var playerFoot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            float stickR = Proto_GreyboxPointerMath.AimStickRadiusPx(h,
                Proto_GreyboxPointerMath.AimStickRadiusScreenHeightPercent);
            var drag = new Vector2(stickR * 0.85f, stickR * 0.55f);
            var aim = new Proto_GreyboxAim();
            aim.ConfigureScreenStick(Proto_GreyboxPointerMath.AimStickRadiusScreenHeightPercent,
                Proto_GreyboxPointerMath.AimStickSmoothTime,
                Proto_GreyboxPointerMath.AimStickDeadZonePortion,
                Proto_GreyboxPointerMath.AimStickDragThresholdPortion,
                Proto_GreyboxPointerMath.AimAngularSpeedCapDegPerSec,
                Proto_GreyboxPointerMath.AimFineAimExponent);
            aim.BeginDrag(playerFoot, new List<Proto_GreyboxTarget>());
            float elapsed = 0f;
            while (elapsed < 0.65f)
            {
                aim.SetAimStickScreenDelta(drag, w, h, Dt);
                elapsed += Dt;
            }

            var refDir = Proto_GreyboxPointerMath.WorldDirectionFromScreenDrag(drag, w, h);
            Assert.Less(Vector2.Angle(aim.AimDirection, refDir), 4f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Grenade_ShortTapAutoAimsNearestEnemy()
        {
            var foot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            float yFar = Proto_GreyboxLayout.EnemyFarFootWorldY;
            var near = new Proto_GreyboxTarget(new Vector2(0.5f, yFar));
            var far = new Proto_GreyboxTarget(new Vector2(3f, yFar));
            var list = new List<Proto_GreyboxTarget> { far, near };
            var landing = Proto_GreyboxGrenadeAim.AutoThrowLanding(foot, list);
            Assert.AreEqual(near.BodyCenterGround.x, landing.x, 0.05f);
            Assert.AreEqual(near.BodyCenterGround.y, landing.y, 0.05f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Grenade_DragDirectionAndRange_MonotonicWithLength()
        {
            const float w = 1920f;
            const float h = 1080f;
            var foot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            float stickR = Proto_GreyboxPointerMath.AimStickRadiusPx(h,
                Proto_GreyboxPointerMath.AimStickRadiusScreenHeightPercent);
            var shortDrag = new Vector2(0f, stickR * 0.35f);
            var longDrag = new Vector2(0f, stickR * 0.92f);
            var landShort = Proto_GreyboxGrenadeAim.LandingFromScreenDrag(foot, shortDrag, w, h, stickR);
            var landLong = Proto_GreyboxGrenadeAim.LandingFromScreenDrag(foot, longDrag, w, h, stickR);
            Assert.Greater(landLong.y, landShort.y);
            Assert.Greater((landLong - foot).magnitude, (landShort - foot).magnitude);
            Assert.Less(Mathf.Abs(landShort.x - foot.x), 0.15f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Grenade_DragBackToCenter_ReleaseIsCancel()
        {
            const float h = 1080f;
            float stickR = Proto_GreyboxPointerMath.AimStickRadiusPx(h,
                Proto_GreyboxPointerMath.AimStickRadiusScreenHeightPercent);
            var pull = new Vector2(0f, stickR * 0.55f);
            var tiny = new Vector2(0f, stickR * 0.02f);
            var intent = Proto_GreyboxPointerMath.AimStickReleaseIntent(tiny.magnitude, pull.magnitude, stickR,
                Proto_GreyboxPointerMath.AimStickDeadZonePortion,
                Proto_GreyboxPointerMath.AimStickDragThresholdPortion);
            Assert.AreEqual(Proto_GreyboxAimReleaseIntent.Cancel, intent);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Result_RestartHit_OnlyInsideButtonRect()
        {
            float cx = (Proto_GreyboxResultUi.ResultRestartHitX0 + Proto_GreyboxResultUi.ResultRestartHitX1) * 0.5f;
            float cy = (Proto_GreyboxResultUi.ResultRestartHitY0 + Proto_GreyboxResultUi.ResultRestartHitY1) * 0.5f;
            Assert.IsTrue(Proto_GreyboxResultUi.IsRestartHit(cx, cy));
            Assert.IsFalse(Proto_GreyboxResultUi.IsRestartHit(10f, 10f));
            LogAssert.NoUnexpectedReceived();
        }

        static Vector2 PercentDragWorldDelta(float screenW, float screenH, float pctX, float pctY)
        {
            var deltaScreen = new Vector2(screenW * pctX, screenH * pctY);
            return Proto_GreyboxPointerMath.ScreenDeltaToWorldDrag(deltaScreen, screenW, screenH);
        }

        static Vector2 AimDirectionFromDrag(Vector2 dragWorld, Vector2 foot)
        {
            var aim = new Proto_GreyboxAim();
            aim.BeginDrag(foot, new List<Proto_GreyboxTarget>());
            aim.SetAimDirection(dragWorld);
            return aim.AimDirection;
        }
    }
}
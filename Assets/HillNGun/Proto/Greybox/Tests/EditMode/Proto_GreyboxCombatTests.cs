using System.Collections.Generic;
using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxCombatTests
    {
        const float Dt = 0.02f;

        [SetUp]
        public void SetUp() => Proto_GreyboxMoveZone.ResetToDefaults();

        [Test]
        public void Tuning_DefaultBulletSpeedsEqual()
        {
            Assert.AreEqual(4.2f, Proto_GreyboxTuning.AllyBulletSpeed, 0.001f);
            Assert.AreEqual(4.2f, Proto_GreyboxTuning.EnemyBulletSpeed, 0.001f);
            var rt = new Proto_GreyboxRuntimeTuning();
            Assert.AreEqual(Proto_GreyboxTuning.AllyBulletSpeed, rt.AllyBulletSpeed, 0.001f);
            Assert.AreEqual(Proto_GreyboxTuning.EnemyBulletSpeed, rt.EnemyBulletSpeed, 0.001f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Tuning_OneAllyBulletKillsDefaultHpTarget()
        {
            Assert.GreaterOrEqual(Proto_GreyboxTuning.AllyBulletDamage, Proto_GreyboxTuning.TargetHp);
            var t = new Proto_GreyboxTarget(Vector2.zero, Proto_GreyboxTuning.TargetHp);
            t.ApplyDamage(Proto_GreyboxTuning.AllyBulletDamage);
            Assert.IsFalse(t.IsAlive);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Aim_SelectsNearestAliveTarget()
        {
            var footY = Proto_GreyboxLayout.EnemyFarFootWorldY;
            var playerFoot = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(49f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            var t0 = new Proto_GreyboxTarget(new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(30f), footY));
            var t1 = new Proto_GreyboxTarget(new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(50f), footY));
            var t2 = new Proto_GreyboxTarget(new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(70f), footY));
            var list = new List<Proto_GreyboxTarget> { t0, t1, t2 };
            var aim = new Proto_GreyboxAim();

            aim.TickIdle(playerFoot, list);
            Assert.AreSame(t1, aim.SelectedTarget);

            aim.TickIdle(new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(72f),
                    Proto_GreyboxLayout.ScreenYPercentToWorldY(84f)), list);
            Assert.AreSame(t2, aim.SelectedTarget);

            t2.ApplyDamage(Proto_GreyboxTuning.TargetHp);
            aim.TickIdle(new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(72f),
                    Proto_GreyboxLayout.ScreenYPercentToWorldY(84f)), list);
            Assert.AreSame(t1, aim.SelectedTarget);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Aim_TapAutoAim_SelectsNearestLiving()
        {
            var footY = Proto_GreyboxLayout.EnemyFarFootWorldY;
            var playerFoot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            var near = new Proto_GreyboxTarget(new Vector2(0.2f, footY));
            var far = new Proto_GreyboxTarget(new Vector2(4f, footY));
            var list = new List<Proto_GreyboxTarget> { far, near };
            var aim = new Proto_GreyboxAim();
            aim.BeginDrag(playerFoot, list);
            var intent = aim.EndDrag();
            Assert.AreEqual(Proto_GreyboxAimReleaseIntent.AutoAim, intent);
            Assert.AreSame(near, aim.SelectedTarget);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Aim_ManualDirection_ParallelBulletVelocity()
        {
            var cover = Proto_GreyboxCover.FromStageCrates();
            var playerFoot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            var drag = new Vector2(0.5f, 6f);
            var aim = new Proto_GreyboxAim();
            aim.BeginDrag(playerFoot, new List<Proto_GreyboxTarget>());
            aim.SetAimDirection(drag);
            var intent = aim.EndDrag();
            Assert.AreEqual(Proto_GreyboxAimReleaseIntent.ManualDirection, intent);

            var bullet = new Proto_GreyboxAllyBulletSim(playerFoot, aim.ManualFireAimPoint(), cover);
            var expected = drag.normalized;
            var actual = bullet.GroundVelocity.normalized;
            Assert.Less(Vector2.Angle(expected, actual), 0.5f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllyBullet_MovesScalesAndDamagesTarget()
        {
            float yNear = Proto_GreyboxLayout.ScreenYPercentToWorldY(84f);
            float yFar = Proto_GreyboxLayout.ScreenYPercentToWorldY(37f);
            var scaleNear = Proto_DepthScale.ScaleAt(yNear);
            var scaleFar = Proto_DepthScale.ScaleAt(yFar);
            Assert.Greater(scaleNear, scaleFar, "larger ground y should yield smaller visual scale");

            var cover = Proto_GreyboxCover.FromStageCrates();
            var start = new Vector2(0f, yNear);
            var aim = new Vector2(0f, yFar);
            var bullet = new Proto_GreyboxAllyBulletSim(start, aim, cover);
            float traveled = 0f;
            float t = 0f;
            float prevScale = bullet.VisualScale;
            while (bullet.Alive && t < 0.5f)
            {
                var before = bullet.GroundPosition;
                bullet.Tick(Dt, null);
                traveled += (bullet.GroundPosition - before).magnitude;
                t += Dt;
                if (bullet.Alive)
                {
                    Assert.LessOrEqual(bullet.VisualScale, prevScale + 0.0001f);
                    prevScale = bullet.VisualScale;
                }
            }

            Assert.AreEqual(Proto_GreyboxTuning.AllyBulletSpeed * t, traveled, 0.25f);

            var target = new Proto_GreyboxTarget(new Vector2(0f, yFar));
            bullet = new Proto_GreyboxAllyBulletSim(start, aim, cover);
            while (bullet.Alive)
                bullet.Tick(Dt, new List<Proto_GreyboxTarget> { target });
            Assert.IsFalse(target.IsAlive);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Gun_ReloadMagazineAndManual()
        {
            var gun = new Proto_GreyboxGun();
            float time = 0f;
            float interval = Proto_GreyboxTuning.FireInterval;
            float ri = Proto_GreyboxTuning.ReloadRoundInterval;
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(gun.TryFire(time));
                time += interval;
            }

            Assert.AreEqual(0, gun.Ammo);
            gun.StartReload(time);
            Assert.AreEqual(5, gun.ReloadCasingCount);
            for (int i = 1; i < 5; i++)
            {
                gun.StartReload(time);
                Assert.AreEqual(0, gun.ReloadCasingCount, "queued tap has no casings");
            }
            gun.StartReload(time);
            gun.Tick(time + ri);
            Assert.AreEqual(1, gun.Ammo);
            Assert.IsTrue(gun.IsReloading);
            time += ri;
            for (int r = 1; r < 5; r++)
            {
                time += ri;
                gun.Tick(time);
            }
            Assert.AreEqual(5, gun.Ammo);
            Assert.IsFalse(gun.IsReloading);
            Assert.IsTrue(gun.TryFire(time + interval));

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Gun_RollCancelsReload()
        {
            var gun = new Proto_GreyboxGun();
            float time = 0f;
            float interval = Proto_GreyboxTuning.FireInterval;
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(gun.TryFire(time));
                time += interval;
            }

            Assert.AreEqual(0, gun.Ammo);
            gun.StartReload(time);
            Assert.IsTrue(gun.IsReloading);
            gun.Tick(time + Proto_GreyboxTuning.ReloadRoundInterval);
            Assert.AreEqual(1, gun.Ammo);
            gun.CancelReload();
            Assert.IsFalse(gun.IsReloading);
            Assert.AreEqual(0, gun.ReloadQueue);
            Assert.AreEqual(1, gun.Ammo);
            // last shot was at time-interval; still inside fire interval shortly after it.
            Assert.IsFalse(gun.TryFire(time - interval + 0.01f));
            Assert.AreEqual(1, gun.Ammo);

            float t2 = time + 0.05f;
            float ri2 = Proto_GreyboxTuning.ReloadRoundInterval;
            for (int i = 0; i < 4; i++)
                gun.StartReload(t2);
            for (int r = 0; r < 4; r++)
            {
                t2 += ri2;
                gun.Tick(t2);
            }
            Assert.AreEqual(5, gun.Ammo);
            Assert.IsTrue(gun.TryFire(time + 1f));

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Gun_FiveShotsEmptySixthFails()
        {
            var gun = new Proto_GreyboxGun();
            float time = 0f;
            float interval = Proto_GreyboxTuning.FireInterval;
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(gun.TryFire(time), $"shot {i}");
                time += interval;
            }

            Assert.AreEqual(0, gun.Ammo);
            Assert.IsFalse(gun.TryFire(time));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Gun_IncrementalReloadFromThreeFifths()
        {
            var gun = new Proto_GreyboxGun();
            float time = 0f;
            float interval = Proto_GreyboxTuning.FireInterval;
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(gun.TryFire(time));
                time += interval;
            }

            Assert.AreEqual(3, gun.Ammo);
            gun.StartReload(time);
            Assert.AreEqual(2, gun.ReloadCasingCount);
            gun.StartReload(time);
            Assert.AreEqual(0, gun.ReloadCasingCount);
            gun.Tick(time + Proto_GreyboxTuning.ReloadRoundInterval);
            Assert.AreEqual(4, gun.Ammo);
            Assert.IsTrue(gun.IsReloading);
            gun.Tick(time + Proto_GreyboxTuning.ReloadRoundInterval * 2f);
            Assert.AreEqual(5, gun.Ammo);
            Assert.IsFalse(gun.IsReloading);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Gun_MidReloadFireInterrupts()
        {
            var gun = new Proto_GreyboxGun();
            float time = 0f;
            float interval = Proto_GreyboxTuning.FireInterval;
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(gun.TryFire(time));
                time += interval;
            }

            Assert.AreEqual(2, gun.Ammo);
            gun.StartReload(time);
            Assert.IsTrue(gun.IsReloading);
            gun.Tick(time + Proto_GreyboxTuning.ReloadRoundInterval * 0.5f);
            Assert.AreEqual(2, gun.Ammo, "insert not finished yet");
            Assert.IsTrue(gun.IsReloading);
            float fireTime = time + interval;
            Assert.IsTrue(gun.TryFire(fireTime));
            Assert.IsFalse(gun.IsReloading);
            Assert.AreEqual(1, gun.Ammo);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Gun_StartReloadCasingCountMatchesSpent()
        {
            var gun = new Proto_GreyboxGun();
            float t = Proto_GreyboxTuning.FireInterval;
            Assert.IsTrue(gun.TryFire(0f));
            Assert.IsTrue(gun.TryFire(t));
            Assert.AreEqual(2, gun.SpentSinceFull);
            gun.StartReload(t * 2f);
            Assert.AreEqual(2, gun.ReloadCasingCount);
            gun.StartReload(t * 2f);
            Assert.AreEqual(0, gun.ReloadCasingCount);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Gun_OneReloadTapThenInterval_DoesNotAutoContinue()
        {
            var gun = new Proto_GreyboxGun();
            gun.TryFire(0f);
            float t = 0.1f;
            gun.StartReload(t);
            gun.Tick(t + Proto_GreyboxTuning.ReloadRoundInterval);
            Assert.AreEqual(5, gun.Ammo);
            Assert.IsFalse(gun.IsReloading);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Gun_FiveQueuedTapsFillMag_SixthIgnored()
        {
            var gun = new Proto_GreyboxGun();
            float t = 0f;
            float interval = Proto_GreyboxTuning.FireInterval;
            for (int i = 0; i < 5; i++)
            {
                gun.TryFire(t);
                t += interval;
            }
            for (int i = 0; i < 5; i++)
                gun.StartReload(t);
            gun.StartReload(t);
            float ri = Proto_GreyboxTuning.ReloadRoundInterval;
            for (int r = 0; r < 5; r++)
            {
                t += ri;
                gun.Tick(t);
            }
            Assert.AreEqual(5, gun.Ammo);
            int ammoBefore = gun.Ammo;
            gun.StartReload(t);
            gun.Tick(t + ri);
            Assert.AreEqual(ammoBefore, gun.Ammo);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Gun_MidQueueFireClearsReload_KeepsAmmo()
        {
            var gun = new Proto_GreyboxGun();
            float t = 0f;
            gun.TryFire(0f);
            gun.TryFire(Proto_GreyboxTuning.FireInterval);
            gun.TryFire(Proto_GreyboxTuning.FireInterval * 2f);
            gun.StartReload(t + 0.5f);
            gun.StartReload(t + 0.5f);
            gun.Tick(t + 0.5f + Proto_GreyboxTuning.ReloadRoundInterval);
            Assert.AreEqual(3, gun.Ammo);
            Assert.IsTrue(gun.TryFire(t + 1f));
            Assert.IsFalse(gun.IsReloading);
            Assert.AreEqual(0, gun.ReloadQueue);
            Assert.AreEqual(2, gun.Ammo);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Tuning_RuntimeBulletSpeedsIndependent()
        {
            var rt = new Proto_GreyboxRuntimeTuning();
            rt.AllyBulletSpeed = 6f;
            rt.EnemyBulletSpeed = 3f;
            Assert.AreEqual(6f, rt.AllyBulletSpeed, 0.001f);
            Assert.AreEqual(3f, rt.EnemyBulletSpeed, 0.001f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BulletSpeedPrefs_SaveLoadRoundTrip()
        {
            using (Proto_GreyboxBulletSpeedPrefs.EnterTestScope())
            {
                var rt = new Proto_GreyboxRuntimeTuning();
                rt.AllyBulletSpeed = 7.3f;
                rt.EnemyBulletSpeed = 5.1f;
                Proto_GreyboxBulletSpeedPrefs.SaveFrom(rt);
                var load = new Proto_GreyboxRuntimeTuning();
                Proto_GreyboxBulletSpeedPrefs.LoadInto(load);
                Assert.AreEqual(7.3f, load.AllyBulletSpeed, 0.01f);
                Assert.AreEqual(5.1f, load.EnemyBulletSpeed, 0.01f);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Grenade_RechargeProgressAndFlash()
        {
            var g = new Proto_GreyboxGrenade();
            float t = 0f;
            var foot = Vector2.zero;
            var aim = new Vector2(2f, 0f);
            Assert.IsTrue(g.TryThrow(t, foot, aim));
            Assert.AreEqual(0f, g.RechargeProgress01(t), 0.001f);
            t += Proto_GreyboxTuning.GrenadeFlightTime + 0.1f;
            g.Tick(t, 0.02f, null, null);
            float mid = t + Proto_GreyboxTuning.GrenadeCooldown * 0.5f;
            g.Tick(mid, 0.02f, null, null);
            Assert.Greater(g.RechargeProgress01(mid), 0.45f);
            Assert.Less(g.RechargeProgress01(mid), 0.55f);
            float end = t + Proto_GreyboxTuning.GrenadeCooldown + 0.01f;
            g.Tick(end, 0.02f, null, null);
            Assert.IsFalse(g.CanThrow(end));
            Assert.AreEqual(1f, g.RechargeProgress01(end), 0.001f);
            float ready = end + 0.7f;
            g.Tick(ready, 0.02f, null, null);
            Assert.IsTrue(g.CanThrow(ready));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Grenade_StickDragBackToCenter_DoesNotStartFlight()
        {
            EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
            var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            Assert.NotNull(builder);
            builder.Build();
            var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
            var controller = player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
            Assert.NotNull(controller);
            for (int i = 0; i < 8; i++)
                controller.Simulate(new Proto_GreyboxInput(), Dt);

            float stickR = Proto_GreyboxPointerMath.AimStickRadiusPx(1080f,
                Proto_GreyboxPointerMath.AimStickRadiusScreenHeightPercent);
            var pull = new Vector2(0f, stickR * 0.55f);
            var tiny = new Vector2(0f, stickR * 0.02f);
            controller.Simulate(new Proto_GreyboxInput { GrenadeStickHeld = true, GrenadeStickDragWorld = pull }, Dt);
            controller.Simulate(new Proto_GreyboxInput { GrenadeStickHeld = true, GrenadeStickDragWorld = pull }, Dt);
            controller.Simulate(
                new Proto_GreyboxInput { GrenadeStickReleased = true, GrenadeStickDragWorld = tiny }, Dt);
            Assert.IsFalse(controller.Grenade.IsFlying);
            Assert.IsTrue(controller.Grenade.CanThrow(controller.SimTime));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Grenade_DestroysCrateAndDamagesInRadius()
        {
            var cover = Proto_GreyboxCover.FromStageCrates();
            var grenade = new Proto_GreyboxGrenade();
            float yFar = Proto_GreyboxLayout.EnemyFarFootWorldY;
            float midCx = (Proto_GreyboxLayout.ScreenXPercentToWorldX(12f) +
                           Proto_GreyboxLayout.ScreenXPercentToWorldX(21f)) * 0.5f;
            float midFootY = Proto_GreyboxLayout.ScreenYPercentToWorldY(63f);
            var near = new Proto_GreyboxTarget(new Vector2(midCx, midFootY));
            var far = new Proto_GreyboxTarget(new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(75f), yFar));
            var targets = new List<Proto_GreyboxTarget> { near, far };

            var aim = new Vector2(midCx, Proto_GreyboxLayout.ScreenYPercentToWorldY(63f));
            var playerFoot = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(49f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));

            float time = 0f;
            Assert.IsTrue(grenade.TryThrow(time, playerFoot, aim));
            Assert.IsFalse(grenade.TryThrow(time, playerFoot, aim), "cooldown should block immediate rethrow");

            while (grenade.IsFlying)
            {
                time += Dt;
                grenade.Tick(time, Dt, targets, cover, null);
            }

            Assert.Less(near.Hp, Proto_GreyboxTuning.TargetHp);
            Assert.AreEqual(Proto_GreyboxTuning.TargetHp, far.Hp);
            Assert.IsFalse(grenade.CanThrow(time), "grenade should stay on cooldown after detonation");

            var spawn = new Vector2(playerFoot.x, Proto_GreyboxLayout.ScreenYPercentToWorldY(30f));
            Assert.IsFalse(cover.SegmentBlocked(spawn, playerFoot, out _), "mid crate should be removed from cover");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void EnemyBullet_PassingThroughGrunts_DoesNotDamageThem()
        {
            var tuning = new Proto_GreyboxRuntimeTuning();
            var footY = Proto_GreyboxLayout.EnemyFarFootWorldY;
            var posA = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(25f), footY);
            var posB = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(42f), footY);
            var gruntA = new Proto_GreyboxGrunt(posA, posA, Proto_GreyboxGruntBehavior.Strafe, tuning.EnemyHp, 999f, 0f);
            var gruntB = new Proto_GreyboxGrunt(posB, posB, Proto_GreyboxGruntBehavior.Strafe, tuning.EnemyHp, 999f, 0f);
            var playerFoot = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(58f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            var vel = Proto_GreyboxGrunt.ComputeShotVelocity(posA, playerFoot, Proto_GreyboxTuning.EnemyBulletSpeed);
            var cover = Proto_GreyboxCover.FromStageCrates();
            var bullet = new Proto_GreyboxBulletSim(posA, vel, cover);
            int hpBeforeA = gruntA.Target.Hp;
            int hpBeforeB = gruntB.Target.Hp;

            int playerHp = tuning.PlayerMaxHp;
            while (bullet.Alive)
                bullet.Tick(Dt, Proto_GreyboxBulletSim.PlayerGroundFootprint(playerFoot), true, ref playerHp);

            Assert.AreEqual(hpBeforeA, gruntA.Target.Hp);
            Assert.AreEqual(hpBeforeB, gruntB.Target.Hp);
            Assert.IsTrue(gruntA.Target.IsAlive);
            Assert.IsTrue(gruntB.Target.IsAlive);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Grunts_StayAliveWithoutAllyDamageOrGrenades()
        {
            var tuning = new Proto_GreyboxRuntimeTuning();
            var director = new Proto_GreyboxWaveDirector(77);
            director.StartRun(tuning);
            var playerFoot = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(84f));
            var cover = Proto_GreyboxCover.FromStageCrates();
            var enemyBullets = new System.Collections.Generic.List<Proto_GreyboxBulletSim>();
            int playerHp = tuning.PlayerMaxHp;

            for (int i = 0; i < 450; i++)
            {
                director.Tick(Dt, tuning);
                foreach (var grunt in director.Grunts)
                {
                    if (!grunt.Target.IsAlive) continue;
                    grunt.Tick(Dt, Proto_GreyboxTuning.GruntWalkSpeed, Proto_GreyboxTuning.GruntStrafeRange,
                        tuning.EnemyFireInterval, tuning.EnemyBulletSpeed, playerFoot);
                    if (!grunt.FiredThisTick) continue;
                    enemyBullets.Add(new Proto_GreyboxBulletSim(grunt.LastFireGround, grunt.LastFireVelocity, cover,
                        tuning.EnemyBulletDiameter));
                }

                var footprint = Proto_GreyboxBulletSim.PlayerGroundFootprint(playerFoot);
                for (int b = enemyBullets.Count - 1; b >= 0; b--)
                {
                    if (!enemyBullets[b].Alive)
                    {
                        enemyBullets.RemoveAt(b);
                        continue;
                    }

                    enemyBullets[b].Tick(Dt, footprint, true, ref playerHp, tuning.CoverDamageReduction01);
                    if (!enemyBullets[b].Alive)
                        enemyBullets.RemoveAt(b);
                }
            }

            foreach (var grunt in director.Grunts)
            {
                Assert.IsTrue(grunt.Target.IsAlive, "grunt died without ally damage");
                Assert.AreEqual(tuning.EnemyHp, grunt.Target.Hp);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void GruntTarget_SpawnsWithPositiveHp()
        {
            var tuning = new Proto_GreyboxRuntimeTuning();
            var t = new Proto_GreyboxTarget(Vector2.zero, tuning.EnemyHp);
            Assert.IsTrue(t.IsAlive);
            Assert.AreEqual(tuning.EnemyHp, t.Hp);
            Assert.Greater(new Proto_GreyboxTarget(Vector2.zero).Hp, 0);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
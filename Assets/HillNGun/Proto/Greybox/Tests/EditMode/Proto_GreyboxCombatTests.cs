using System.Collections.Generic;
using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxCombatTests
    {
        const float Dt = 0.02f;

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
            Assert.IsTrue(gun.TryFire(time));
            Assert.AreEqual(0, gun.Ammo);
            Assert.IsFalse(gun.IsReloading);
            Assert.IsFalse(gun.TryFire(time + 0.01f));

            gun.StartReload(time);
            Assert.IsTrue(gun.IsReloading);
            gun.Tick(time + Proto_GreyboxTuning.ReloadDuration);
            Assert.AreEqual(1, gun.Ammo);
            Assert.IsFalse(gun.IsReloading);
            Assert.IsTrue(gun.TryFire(time + Proto_GreyboxTuning.ReloadDuration));

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Gun_RollCancelsReload()
        {
            var gun = new Proto_GreyboxGun();
            float time = 0f;
            Assert.IsTrue(gun.TryFire(time));
            gun.StartReload(time + 0.05f);
            Assert.IsTrue(gun.IsReloading);
            gun.CancelReload();
            Assert.IsFalse(gun.IsReloading);
            Assert.AreEqual(0, gun.Ammo);
            Assert.IsFalse(gun.TryFire(time + 0.1f));

            gun.StartReload(time + 0.2f);
            gun.Tick(time + 0.2f + Proto_GreyboxTuning.ReloadDuration);
            Assert.IsTrue(gun.TryFire(time + 0.9f));

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
    }
}
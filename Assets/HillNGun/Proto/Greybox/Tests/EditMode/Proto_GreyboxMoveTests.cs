using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxMoveTests
    {
        const float Dt = 0.02f;

        [SetUp]
        public void SetUp() => Proto_GreyboxMoveZone.ResetToDefaults();

        [Test]
        public void MoveClamp_KeepsFootInsideMoveZone()
        {
            var motor = new Proto_GreyboxPlayerMotor(Vector2.zero);
            var input = new Vector2(1f, 1f);
            for (int i = 0; i < 400; i++)
                motor.TickMove(input, Dt);

            var foot = motor.FootPosition;
            Assert.GreaterOrEqual(foot.x, Proto_GreyboxMoveZone.MinFootWorldX - 0.001f);
            Assert.LessOrEqual(foot.x, Proto_GreyboxMoveZone.MaxFootWorldX + 0.001f);
            Assert.GreaterOrEqual(foot.y, Proto_GreyboxMoveZone.MinFootWorldY - 0.001f);
            Assert.LessOrEqual(foot.y, Proto_GreyboxMoveZone.MaxFootWorldY + 0.001f);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Move_CannotPassFenceZone()
        {
            var motor = new Proto_GreyboxPlayerMotor(new Vector2(0f, Proto_GreyboxMoveZone.MaxFootWorldY));
            var roll = new Proto_GreyboxRoll();
            var up = new Vector2(0f, 1f);

            for (int i = 0; i < 600; i++)
            {
                if (!roll.IsRolling)
                    roll.TryStart(up);
                if (roll.IsRolling)
                    motor.ApplyDelta(roll.Tick(Dt));
                else
                {
                    roll.Tick(Dt);
                    motor.TickMove(up, Dt);
                }

                Assert.LessOrEqual(motor.FootPosition.y, Proto_GreyboxMoveZone.MaxFootWorldY + 0.001f);
                Assert.Less(motor.FootPosition.y, Proto_GreyboxMoveZone.FenceBottomWorldY);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Roll_InvulnCooldownAndDistance()
        {
            var start = new Vector2(0f, Proto_GreyboxLayout.ScreenYPercentToWorldY(82.5f));
            var motor = new Proto_GreyboxPlayerMotor(start);
            var roll = new Proto_GreyboxRoll();
            roll.TryStart(Vector2.right);

            roll.Tick(0.21f);
            Assert.IsTrue(roll.IsInvulnerable, "invuln at 0.21s");

            roll = new Proto_GreyboxRoll();
            roll.TryStart(Vector2.right);
            roll.Tick(0.23f);
            Assert.IsFalse(roll.IsInvulnerable, "no invuln at 0.23s");

            motor = new Proto_GreyboxPlayerMotor(start);
            roll = new Proto_GreyboxRoll();
            roll.TryStart(Vector2.right);
            float dist = 0f;
            while (roll.IsRolling)
            {
                var d = roll.Tick(Dt);
                motor.ApplyDelta(d);
                dist += d.magnitude;
            }

            Assert.AreEqual(Proto_GreyboxTuning.RollDistance, dist, 0.05f);
            Assert.IsTrue(roll.TryStart(Vector2.right), "zero cooldown allows immediate restart after roll ends");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Roll_ChainsImmediatelyAfterPreviousEnds()
        {
            var roll = new Proto_GreyboxRoll();
            Assert.IsTrue(roll.TryStart(Vector2.right));
            while (roll.IsRolling)
                roll.Tick(Dt);
            Assert.IsFalse(roll.IsRolling);
            Assert.IsTrue(roll.TryStart(Vector2.right), "should chain when previous roll finished");
            Assert.IsFalse(roll.TryStart(Vector2.left), "cannot start second roll while active");
            while (roll.IsRolling)
                roll.Tick(Dt);
            Assert.IsTrue(roll.TryStart(Vector2.right));

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Cover_BlocksHeadOnBullet_AllowsGlancingHit()
        {
            var cover = Proto_GreyboxCover.FromStageCrates();
            float behindX = Proto_GreyboxLayout.ScreenXPercentToWorldX(63.5f);
            float behindY = Proto_GreyboxLayout.ScreenYPercentToWorldY(84f);
            var behindFoot = new Vector2(behindX, behindY);
            var spawn = new Vector2(behindX, Proto_GreyboxLayout.ScreenYPercentToWorldY(30f));
            var vel = (behindFoot - spawn).normalized * Proto_GreyboxTuning.EnemyBulletSpeed;

            int hp = Proto_GreyboxTuning.PlayerMaxHp;
            var bullet = new Proto_GreyboxBulletSim(spawn, vel, cover);
            while (bullet.Alive)
                bullet.Tick(Dt, Proto_GreyboxBulletSim.PlayerGroundFootprint(behindFoot), false, ref hp);
            Assert.AreEqual(Proto_GreyboxTuning.PlayerMaxHp, hp, "head-on blocked by crate");

            float sideX = Proto_GreyboxLayout.ScreenXPercentToWorldX(40f);
            var sideFoot = new Vector2(sideX, behindY);
            spawn = new Vector2(sideX, Proto_GreyboxLayout.ScreenYPercentToWorldY(30f));
            vel = (sideFoot - spawn).normalized * Proto_GreyboxTuning.EnemyBulletSpeed;
            hp = Proto_GreyboxTuning.PlayerMaxHp;
            bullet = new Proto_GreyboxBulletSim(spawn, vel, cover);
            while (bullet.Alive)
                bullet.Tick(Dt, Proto_GreyboxBulletSim.PlayerGroundFootprint(sideFoot), false, ref hp);
            Assert.Less(hp, Proto_GreyboxTuning.PlayerMaxHp, "glancing path should damage player");

            LogAssert.NoUnexpectedReceived();
        }
    }
}
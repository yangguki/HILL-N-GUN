using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxWaveTests
    {
        const float Dt = 0.02f;

        [Test]
        public void WaveDirector_SpawnsThreeFiveEightAndClears()
        {
            var tuning = new Proto_GreyboxRuntimeTuning();
            var director = new Proto_GreyboxWaveDirector(123);
            director.StartRun(tuning);

            Assert.AreEqual(1, director.CurrentWave);
            SimulateWaveUntilFighting(director, tuning);
            Assert.AreEqual(3, director.Grunts.Count);

            director.KillAllGruntsForTest();
            AdvanceToNextWave(director, tuning);
            Assert.AreEqual(2, director.CurrentWave);
            SimulateWaveUntilFighting(director, tuning);
            Assert.AreEqual(8, director.Grunts.Count);

            director.KillAllGruntsForTest();
            AdvanceToNextWave(director, tuning);
            Assert.AreEqual(3, director.CurrentWave);
            SimulateWaveUntilFighting(director, tuning);
            Assert.AreEqual(16, director.Grunts.Count);

            director.KillAllGruntsForTest();
            for (int i = 0; i < 200; i++)
                director.Tick(Dt, tuning);
            Assert.AreEqual(Proto_GreyboxWaveDirectorState.Cleared, director.State);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void EnemyBullet_AimsAtPlayerWithinTwoDegrees()
        {
            float speed = 3.5f;
            var footY = Proto_GreyboxLayout.EnemyFarFootWorldY;
            var gruntPos = new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(50f), footY);
            var players = new[]
            {
                new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(45f), Proto_GreyboxLayout.ScreenYPercentToWorldY(84f)),
                new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(60f), Proto_GreyboxLayout.ScreenYPercentToWorldY(70f)),
                new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(35f), Proto_GreyboxLayout.ScreenYPercentToWorldY(88f)),
            };

            foreach (var playerFoot in players)
            {
                var vel = Proto_GreyboxGrunt.ComputeShotVelocity(gruntPos, playerFoot, speed);
                Assert.AreEqual(speed, vel.magnitude, 0.001f);
                var expected = (playerFoot - gruntPos).normalized;
                float ang = Vector2.Angle(expected, vel.normalized);
                Assert.Less(ang, 2f);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RuntimeTuning_AppliesMoveSpeedAndEnemyFireInterval()
        {
            var tuning = new Proto_GreyboxRuntimeTuning();
            var motor = new Proto_GreyboxPlayerMotor(
                new Vector2(0f, Proto_GreyboxLayout.PlayerNearFootWorldY), tuning.MoveSpeed);
            var start = motor.FootPosition;
            motor.TickMove(Vector2.right, Dt);
            float defaultDist = (motor.FootPosition - start).magnitude;
            motor.SetFoot(start);

            tuning.MoveSpeed = Proto_GreyboxTuning.MoveSpeed * 2f;
            motor.SetMoveSpeed(tuning.MoveSpeed);
            motor.TickMove(Vector2.right, Dt);
            float fastDist = (motor.FootPosition - start).magnitude;
            Assert.Greater(fastDist, defaultDist * 1.8f);

            var footY = Proto_GreyboxLayout.EnemyFarFootWorldY;
            var pos = new Vector2(-2f, footY);
            var playerFoot = new Vector2(0f, Proto_GreyboxLayout.PlayerNearFootWorldY);
            var grunt = new Proto_GreyboxGrunt(pos, pos, Proto_GreyboxGruntBehavior.Strafe, 3, 0f, 0f);
            grunt.Tick(1.5f, 3f, 0.6f, 1.6f, 3.5f, playerFoot);
            Assert.IsFalse(grunt.FiredThisTick);

            tuning.EnemyFireInterval = 0.5f;
            grunt.Tick(0.55f, 3f, 0.6f, tuning.EnemyFireInterval, 3.5f, playerFoot);
            Assert.IsTrue(grunt.FiredThisTick);

            LogAssert.NoUnexpectedReceived();
        }

        static void SimulateWaveUntilFighting(Proto_GreyboxWaveDirector director, Proto_GreyboxRuntimeTuning tuning)
        {
            for (int i = 0; i < 600; i++)
            {
                director.Tick(Dt, tuning);
                if (director.State == Proto_GreyboxWaveDirectorState.Fighting)
                    return;
            }

            Assert.Fail("wave did not reach fighting state");
        }

        static void AdvanceToNextWave(Proto_GreyboxWaveDirector director, Proto_GreyboxRuntimeTuning tuning)
        {
            for (int i = 0; i < 500; i++)
            {
                director.Tick(Dt, tuning);
                if (director.State == Proto_GreyboxWaveDirectorState.Spawning ||
                    director.State == Proto_GreyboxWaveDirectorState.Fighting)
                    return;
            }
        }
    }
}
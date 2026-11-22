using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxRollDizzyTests
    {
        const float RollDur = Proto_GreyboxTuning.RollDuration;

        void ChainRoll(Proto_GreyboxRollDizzy dizzy, ref float time, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(dizzy.TryAcceptRollStart(time));
                dizzy.NotifyRollStarted(time);
                time += RollDur;
                dizzy.NotifyRollEnded(time);
            }
        }

        [Test]
        public void RollDizzy_ThreeChain_LightNotBlockingMove()
        {
            var dizzy = new Proto_GreyboxRollDizzy();
            float time = 0f;
            ChainRoll(dizzy, ref time, 3);
            dizzy.Tick(time);
            Assert.AreEqual(Proto_GreyboxRollDizzyPhase.Light, dizzy.Phase);
            Assert.LessOrEqual(dizzy.RemainingTime(time), Proto_GreyboxRollDizzy.LightDurationSec + 0.01f);
            Assert.GreaterOrEqual(dizzy.RemainingTime(time), Proto_GreyboxRollDizzy.LightDurationSec - 0.05f);
            Assert.IsFalse(dizzy.BlocksMovement);
            Assert.IsFalse(dizzy.BlocksFire);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RollDizzy_FiveChain_StunBlocksRollAndFire()
        {
            var dizzy = new Proto_GreyboxRollDizzy();
            dizzy.ApplyRuntime(new Proto_GreyboxRuntimeTuning { RollChainMax = 8 });
            float time = 0f;
            ChainRoll(dizzy, ref time, 5);
            dizzy.Tick(time);
            Assert.AreEqual(Proto_GreyboxRollDizzyPhase.Stun, dizzy.Phase);
            Assert.IsFalse(dizzy.TryAcceptRollStart(time));
            Assert.IsTrue(dizzy.BlocksFire);
            Assert.IsTrue(dizzy.BlocksRoll);
            dizzy.Tick(time + Proto_GreyboxRollDizzy.SpinInSec + Proto_GreyboxRollDizzy.SpinBodySec * 0.5f);
            Assert.IsTrue(dizzy.BlocksRoll);
            Assert.IsTrue(dizzy.BlocksFire);
            dizzy.Tick(time + Proto_GreyboxRollDizzy.StunDurationSec + 0.01f);
            Assert.AreEqual(Proto_GreyboxRollDizzyPhase.None, dizzy.Phase);
            Assert.IsTrue(dizzy.TryAcceptRollStart(time + Proto_GreyboxRollDizzy.StunDurationSec + 0.02f));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RollDizzy_GapResetsChain()
        {
            var dizzy = new Proto_GreyboxRollDizzy();
            float time = 0f;
            ChainRoll(dizzy, ref time, 2);
            time += Proto_GreyboxRollDizzy.ChainWindowSec + 0.05f;
            dizzy.Tick(time);
            Assert.AreEqual(0, dizzy.ChainCount);
            Assert.IsTrue(dizzy.TryAcceptRollStart(time));
            dizzy.NotifyRollStarted(time);
            time += RollDur;
            dizzy.NotifyRollEnded(time);
            dizzy.Tick(time);
            Assert.AreNotEqual(Proto_GreyboxRollDizzyPhase.Light, dizzy.Phase);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RollDizzy_TickDuringFirstRollKeepsChain()
        {
            var dizzy = new Proto_GreyboxRollDizzy();
            float time = 1f;
            Assert.IsTrue(dizzy.TryAcceptRollStart(time));
            dizzy.NotifyRollStarted(time);
            for (int i = 0; i < 10; i++)
            {
                time += 0.02f;
                dizzy.Tick(time);
            }
            Assert.AreEqual(1, dizzy.ChainCount);
            dizzy.NotifyRollEnded(time);
            Assert.AreEqual(1, dizzy.ChainCount);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RollDizzy_DefaultCap_ThreeChain_LocksFiveSeconds()
        {
            var dizzy = new Proto_GreyboxRollDizzy();
            dizzy.ApplyRuntime(new Proto_GreyboxRuntimeTuning());
            float time = 0f;
            ChainRoll(dizzy, ref time, 3);
            dizzy.Tick(time);
            Assert.AreEqual(Proto_GreyboxRollDizzyPhase.Light, dizzy.Phase);
            Assert.IsTrue(dizzy.ChainLocked(time));
            Assert.IsFalse(dizzy.TryAcceptRollStart(time));
            time += Proto_GreyboxTuning.RollChainLockSec + 0.05f;
            dizzy.Tick(time);
            Assert.IsFalse(dizzy.ChainLocked(time));
            Assert.IsTrue(dizzy.TryAcceptRollStart(time));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RollDizzy_CapThree_CannotReachStun()
        {
            var tuning = new Proto_GreyboxRuntimeTuning { RollChainMax = 3 };
            var dizzy = new Proto_GreyboxRollDizzy();
            dizzy.ApplyRuntime(tuning);
            float time = 0f;
            for (int i = 0; i < 6; i++)
            {
                if (!dizzy.TryAcceptRollStart(time))
                {
                    time += Proto_GreyboxTuning.RollChainLockSec + 0.2f;
                    dizzy.Tick(time);
                    continue;
                }

                dizzy.NotifyRollStarted(time);
                time += RollDur;
                dizzy.NotifyRollEnded(time);
                dizzy.Tick(time);
            }

            Assert.AreNotEqual(Proto_GreyboxRollDizzyPhase.Stun, dizzy.Phase);
            Assert.Less(dizzy.ChainCount, 5);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RollDizzy_FiveChainWithTicksDuringRolls_Stun()
        {
            var dizzy = new Proto_GreyboxRollDizzy();
            dizzy.ApplyRuntime(new Proto_GreyboxRuntimeTuning { RollChainMax = 8 });
            float time = 0f;
            const float dt = 0.02f;
            for (int n = 0; n < 5; n++)
            {
                Assert.IsTrue(dizzy.TryAcceptRollStart(time));
                dizzy.NotifyRollStarted(time);
                float end = time + RollDur;
                while (time < end)
                {
                    time += dt;
                    dizzy.Tick(time);
                }
                dizzy.NotifyRollEnded(time);
            }
            dizzy.Tick(time);
            Assert.AreEqual(5, dizzy.ChainCount);
            Assert.AreEqual(Proto_GreyboxRollDizzyPhase.Stun, dizzy.Phase);
            LogAssert.NoUnexpectedReceived();
        }

    }
}
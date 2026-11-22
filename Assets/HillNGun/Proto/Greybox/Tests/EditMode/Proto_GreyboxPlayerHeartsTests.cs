using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxPlayerHeartsTests
    {
        const float Dt = 0.02f;

        [Test]
        public void Hearts_ThreeHits_DeadAndIFrameBlocksFourth()
        {
            var hearts = new Proto_GreyboxPlayerHearts();
            hearts.Configure(3, 10f);
            Assert.IsTrue(hearts.TakeHit());
            ExpireIFrames(hearts);
            Assert.IsTrue(hearts.TakeHit());
            ExpireIFrames(hearts);
            Assert.IsTrue(hearts.TakeHit());
            Assert.IsTrue(hearts.IsDead);
            Assert.IsFalse(hearts.TakeHit());
            hearts.Tick(0.1f);
            Assert.IsTrue(hearts.IsInvulnerable);
            Assert.IsFalse(hearts.TakeHit());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Hearts_RegenRestoresAfterTenSeconds()
        {
            var hearts = new Proto_GreyboxPlayerHearts();
            hearts.Configure(3, 10f);
            Assert.IsTrue(hearts.TakeHit());
            Assert.AreEqual(2, hearts.Hearts);
            float t = 0f;
            while (t < 9.5f)
            {
                hearts.Tick(Dt, 10f);
                t += Dt;
            }
            Assert.Greater(hearts.Regen01, 0.2f);
            Assert.Less(hearts.Regen01, 1f);
            while (t < 10.5f)
            {
                hearts.Tick(Dt, 10f);
                t += Dt;
            }
            Assert.AreEqual(3, hearts.Hearts);
            Assert.AreEqual(0f, hearts.Regen01, 0.001f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Hearts_HitDuringRegenResetsProgressAndLosesHeart()
        {
            var hearts = new Proto_GreyboxPlayerHearts();
            hearts.Configure(3, 10f);
            Assert.IsTrue(hearts.TakeHit());
            float t = 0f;
            for (int i = 0; i < 200; i++)
            {
                hearts.Tick(Dt, 10f);
                t += Dt;
            }
            Assert.Greater(hearts.Regen01, 0.3f);
            hearts.Tick(Proto_GreyboxTuning.PlayerHitIFrameSec + 0.05f, 10f);
            Assert.IsTrue(hearts.TakeHit());
            Assert.AreEqual(1, hearts.Hearts);
            Assert.AreEqual(0f, hearts.Regen01, 0.001f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Hearts_TwoHits_RegenTwoSteps()
        {
            var hearts = new Proto_GreyboxPlayerHearts();
            hearts.Configure(3, 10f);
            Assert.IsTrue(hearts.TakeHit());
            ExpireIFrames(hearts);
            Assert.IsTrue(hearts.TakeHit());
            Assert.AreEqual(1, hearts.Hearts);
            float t = 0f;
            while (t < 10.1f)
            {
                hearts.Tick(Dt, 10f);
                t += Dt;
            }
            Assert.AreEqual(2, hearts.Hearts);
            while (t < 20.3f)
            {
                hearts.Tick(Dt, 10f);
                t += Dt;
            }
            Assert.AreEqual(3, hearts.Hearts);
            LogAssert.NoUnexpectedReceived();
        }

        static void ExpireIFrames(Proto_GreyboxPlayerHearts hearts)
        {
            hearts.Tick(Proto_GreyboxTuning.PlayerHitIFrameSec + 0.05f, 10f);
        }
    }
}

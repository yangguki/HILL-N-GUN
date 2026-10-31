using NUnit.Framework;
using HillNGun.Data;
using HillNGun.Cover;
using HillNGun.Core;
using UnityEngine;

namespace HillNGun.Tests.EditMode
{
    [TestFixture]
    public class CoverDurabilityTests
    {
        private CoverData CreateTestCoverData(int maxHp = 1500)
        {
            var cover = ScriptableObject.CreateInstance<CoverData>();
            cover.id = "test_cover";
            cover.maxHp = maxHp;
            cover.width = 1.0f;
            cover.depth = 0.5f;
            cover.stages = 4;
            cover.repairable = true;
            return cover;
        }

        [Test]
        public void CoverStartsIntact()
        {
            var data = CreateTestCoverData(1500);
            var cover = new CoverInstance();
            cover.Initialize(data, Vector2.zero, 0, Team.Player);

            Assert.AreEqual(CoverStage.Intact, cover.Stage);
            Assert.AreEqual(1500, cover.CurrentHp);
        }

        [Test]
        public void CoverTransitionsToCracked()
        {
            var data = CreateTestCoverData(1500);
            var cover = new CoverInstance();
            cover.Initialize(data, Vector2.zero, 0, Team.Player);

            cover.TakeDamage(600);

            Assert.AreEqual(CoverStage.Cracked, cover.Stage);
            Assert.AreEqual(900, cover.CurrentHp);
        }

        [Test]
        public void CoverTransitionsToDamaged()
        {
            var data = CreateTestCoverData(1500);
            var cover = new CoverInstance();
            cover.Initialize(data, Vector2.zero, 0, Team.Player);

            cover.TakeDamage(1100);

            Assert.AreEqual(CoverStage.Damaged, cover.Stage);
            Assert.AreEqual(400, cover.CurrentHp);
        }

        [Test]
        public void CoverTransitionsToDestroyed()
        {
            var data = CreateTestCoverData(1500);
            var cover = new CoverInstance();
            cover.Initialize(data, Vector2.zero, 0, Team.Player);

            cover.TakeDamage(1500);

            Assert.AreEqual(CoverStage.Destroyed, cover.Stage);
            Assert.AreEqual(0, cover.CurrentHp);
            Assert.IsTrue(cover.IsDestroyed);
        }

        [Test]
        public void CoverRepairsCorrectly()
        {
            var data = CreateTestCoverData(1500);
            var cover = new CoverInstance();
            cover.Initialize(data, Vector2.zero, 0, Team.Player);
            cover.TakeDamage(1200);                       // 300 / 1500 = 20% -> Damaged
            Assert.AreEqual(CoverStage.Damaged, cover.Stage);

            cover.Repair(0.4f);                           // +40% of max (Volt 긴급 보수, 01 §6) -> 900 = 60%

            Assert.AreEqual(900, cover.CurrentHp);
            Assert.AreEqual(CoverStage.Cracked, cover.Stage);

            cover.Repair(0.4f);                           // 1500 cap -> back to Intact
            Assert.AreEqual(1500, cover.CurrentHp);
            Assert.AreEqual(CoverStage.Intact, cover.Stage);
        }

        [Test]
        public void DestroyedCoverCannotRepair()
        {
            var data = CreateTestCoverData(1500);
            var cover = new CoverInstance();
            cover.Initialize(data, Vector2.zero, 0, Team.Player);
            cover.TakeDamage(1500);

            cover.Repair(0.4f);

            Assert.AreEqual(0, cover.CurrentHp);
            Assert.IsTrue(cover.IsDestroyed);
        }

        [Test]
        public void DamageMultiplierApplied()
        {
            var data = CreateTestCoverData(1500);
            var cover = new CoverInstance();
            cover.Initialize(data, Vector2.zero, 0, Team.Player);

            cover.TakeDamage(100, 1.5f);

            Assert.AreEqual(1350, cover.CurrentHp);
        }

        [Test]
        public void StageThresholdsCorrect()
        {
            var data = CreateTestCoverData(1000);

            Assert.AreEqual(CoverStage.Intact, data.GetStage(1000));
            Assert.AreEqual(CoverStage.Intact, data.GetStage(670));
            Assert.AreEqual(CoverStage.Cracked, data.GetStage(660));
            Assert.AreEqual(CoverStage.Cracked, data.GetStage(340));
            Assert.AreEqual(CoverStage.Damaged, data.GetStage(330));
            Assert.AreEqual(CoverStage.Damaged, data.GetStage(1));
            Assert.AreEqual(CoverStage.Destroyed, data.GetStage(0));
        }
    }
}

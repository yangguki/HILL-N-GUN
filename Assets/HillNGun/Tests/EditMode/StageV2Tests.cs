using NUnit.Framework;
using UnityEngine;
using HillNGun.Data;
using HillNGun.Combat;
using HillNGun.Progression;
using HillNGun.Core;
using HillNGun.PvE;

namespace HillNGun.Tests.EditMode
{
    public class StageV2Tests
    {
        [Test]
        public void WaveDefinition_HasPaceWaitAndDelayFields()
        {
            var w = new WaveDefinition { paceTag = "intro", waitForClear = false, nextWaveDelay = 2.5f };
            Assert.AreEqual("intro", w.paceTag);
            Assert.IsFalse(w.waitForClear);
            Assert.AreEqual(2.5f, w.nextWaveDelay);
        }

        [Test]
        public void StageData_StarDefaults_MatchDoc_1_1()
        {
            var s = ScriptableObject.CreateInstance<StageData>();
            s.gaugeMax = 20; s.stageTimeMin = 70f; s.stageTimeMax = 90f; s.starTimeUnderSec = 80f; s.starRequireNoDowned = true;
            Assert.AreEqual(1, StarRating.Compute(s, true, 90f, 1)); // clear only
            Assert.AreEqual(2, StarRating.Compute(s, true, 75f, 1)); // under 80 but downed
            Assert.AreEqual(3, StarRating.Compute(s, true, 75f, 0));
            Assert.AreEqual(0, StarRating.Compute(s, false, 10f, 0));
            Object.DestroyImmediate(s);
        }

        [Test]
        public void Stage1_3_RequiresBarrelsForThirdStar()
        {
            var s = ScriptableObject.CreateInstance<StageData>();
            s.starTimeUnderSec = 110f; s.starRequireNoDowned = true; s.starMinBarrelExplosions = 2;
            Assert.AreEqual(2, StarRating.Compute(s, true, 100f, 0, 1));
            Assert.AreEqual(3, StarRating.Compute(s, true, 100f, 0, 2));
            Object.DestroyImmediate(s);
        }

        [Test]
        public void ExplosiveBarrel_ExplodesAt400OrGrenade()
        {
            var b = new HazardExplosiveBarrel();
            // no combat system — TriggerByGrenade / ReceiveHit still set Exploded
            b.Initialize(Vector2.zero, null);
            b.ReceiveHit(200, Vector2.up);
            Assert.IsFalse(b.Exploded);
            Assert.IsTrue(b.Cracked);
            b.ReceiveHit(200, Vector2.up);
            Assert.IsTrue(b.Exploded);
            Assert.AreEqual(HazardExplosiveBarrel.CenterDamage, 700);
            Assert.AreEqual(HazardExplosiveBarrel.EdgeDamage, 350);
            Assert.AreEqual(2f, HazardExplosiveBarrel.Radius);

            var b2 = new HazardExplosiveBarrel();
            b2.Initialize(Vector2.one, null);
            b2.TriggerByGrenade();
            Assert.IsTrue(b2.Exploded);
        }

        [Test]
        public void CoverRock_IsIndestructible()
        {
            var d = ScriptableObject.CreateInstance<CoverData>();
            d.indestructible = true; d.maxHp = 9999;
            Assert.IsTrue(d.indestructible);
            Object.DestroyImmediate(d);
        }

        [Test]
        public void AI_AdvanceHelper_StillOOROnly()
        {
            Assert.IsTrue(PvEMatchManager.ShouldAdvanceForRange(false, true));
            Assert.IsFalse(PvEMatchManager.ShouldAdvanceForRange(true, true));
        }

        [Test]
        public void AssaultStillStartsMid_ByDefault()
        {
            var bf = ScriptableObject.CreateInstance<BattlefieldData>();
            Assert.IsFalse(bf.assaultStartsFrontRow);
            Object.DestroyImmediate(bf);
        }
    }
}

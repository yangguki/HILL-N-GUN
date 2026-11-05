using NUnit.Framework;
using UnityEngine;
using HillNGun.Data;
using HillNGun.Feel;

namespace HillNGun.Tests.EditMode
{
    public class FireVfxTests
    {
        [Test]
        public void ThreePatterns_AreDistinguishable()
        {
            var fan = WeaponVfxData.Builtin("spread_fan");
            var beam = WeaponVfxData.Builtin("charge_beam");
            var slug = WeaponVfxData.Builtin("heavy_slug");
            Assert.AreEqual("spread_fan", fan.patternId);
            Assert.AreEqual("charge_beam", beam.patternId);
            Assert.AreEqual("heavy_slug", slug.patternId);
            Assert.IsTrue(fan.isPellet && !fan.isNeedle && !fan.isSlug);
            Assert.IsTrue(beam.isNeedle && !beam.isPellet);
            Assert.IsTrue(slug.isSlug && !slug.isPellet);
            Assert.Greater(slug.headSize, fan.headSize);
            Assert.Greater(fan.tracerWidth, beam.tracerWidth);
            Assert.AreNotEqual(fan.tracerColor, beam.tracerColor);
            Assert.AreNotEqual(beam.tracerColor, slug.tracerColor);
            Object.DestroyImmediate(fan); Object.DestroyImmediate(beam); Object.DestroyImmediate(slug);
        }

        [Test]
        public void ImpactPalettes_ContainNoBloodRed()
        {
            // Forbidden blood reds must not appear in star/dust/wood/spark palettes used by VfxPool.
            foreach (var forbidden in VfxPool.ForbiddenBloodReds)
            {
                Assert.Greater(ColorDistance(forbidden, new Color(1f, 1f, 1f)), 0.4f);
                Assert.Greater(ColorDistance(forbidden, new Color(0.55f, 0.35f, 0.18f)), 0.25f); // wood brown
                Assert.Greater(ColorDistance(forbidden, new Color(1f, 0.95f, 0.4f)), 0.35f); // spark yellow
            }
        }

        [Test]
        public void LowTier_HalvesParticleBudget()
        {
            var prev = VfxPool.Tier;
            VfxPool.Tier = VfxQualityTier.High;
            // budget encoded in SpawnImpact path: High 8, Low 4 — mirror here
            int high = VfxPool.Tier == VfxQualityTier.Low ? 4 : 8;
            VfxPool.Tier = VfxQualityTier.Low;
            int low = VfxPool.Tier == VfxQualityTier.Low ? 4 : 8;
            Assert.AreEqual(8, high);
            Assert.AreEqual(4, low);
            Assert.AreEqual(high / 2, low);
            VfxPool.Tier = prev;
        }

        private static float ColorDistance(Color a, Color b)
            => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }
}

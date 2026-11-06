using NUnit.Framework;
using UnityEngine;
using HillNGun.Combat;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.Tests.EditMode
{
    [TestFixture]
    public class GrenadeFlightTests
    {
        [Test]
        public void FlightFollowsArcNotTeleport()
        {
            var data = ScriptableObject.CreateInstance<GrenadeData>();
            data.flightTimeBase = 0.6f;
            data.flightTimePerTile = 0f;
            var g = new GrenadeProjectile();
            g.Initialize(new Vector2(0f, 0f), new Vector2(6f, 0f), data, Team.Player);
            g.Update(0.3f); // halfway
            Assert.IsTrue(g.IsActive);
            Assert.AreEqual(0.5f, g.Progress, 0.02f);
            Assert.Greater(g.HeightOffset, 0.1f, "parabola height mid-flight");
            Assert.AreEqual(3f, g.CurrentPosition.x, 0.05f);
            Assert.Greater(g.VisualPosition.y, g.CurrentPosition.y);
            g.Update(0.35f);
            Assert.IsTrue(g.HasLanded());
            Object.DestroyImmediate(data);
        }

        [Test]
        public void BuiltinVfxHasNoBloodPalette()
        {
            var v = GrenadeVfxData.Builtin;
            Assert.Greater(v.flameColor.g, 0.2f);
            Assert.Greater(v.sparkColor.g, 0.4f);
            // flame is orange, not blood: red channel high but green also present
            Assert.Greater(v.flameColor.r, 0.5f);
            Assert.Less(Mathf.Abs(v.flameColor.r - v.flameColor.g), 0.7f);
        }
    }
}

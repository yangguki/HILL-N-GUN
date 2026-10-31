using NUnit.Framework;
using HillNGun.Data;
using UnityEngine;

namespace HillNGun.Tests.EditMode
{
    /// <summary>Movement feel must not change design numbers: roll/row curves keep endpoints, AI/keyboard input = full speed.</summary>
    [TestFixture]
    public class MovementFeelTests
    {
        private MovementFeelData Feel() => ScriptableObject.CreateInstance<MovementFeelData>();

        [Test]
        public void RollEaseKeepsEndpointsAndFrontLoads()
        {
            var f = Feel();
            Assert.AreEqual(0f, f.RollEase(0f), 1e-5f);
            Assert.AreEqual(1f, f.RollEase(1f), 1e-5f);
            Assert.Greater(f.RollEase(0.5f), 0.5f, "fast start -> decel");
        }

        [Test]
        public void RowEaseKeepsEndpoints()
        {
            var f = Feel();
            Assert.AreEqual(0f, f.RowEase(0f), 1e-5f);
            Assert.AreEqual(1f, f.RowEase(1f), 1e-5f);
            Assert.AreEqual(0.5f, f.RowEase(0.5f), 1e-5f);
        }

        [Test]
        public void FullDeflectionAndBotInputsAreFullSpeed()
        {
            var f = Feel();
            Assert.AreEqual(1f, f.AnalogSpeedFactor(1f), 1e-5f);
            Assert.AreEqual(1f, f.AnalogSpeedFactor(0.6f), 1e-5f); // smoke autopilot magnitude
            Assert.AreEqual(0f, f.AnalogSpeedFactor(0.05f), 1e-5f);
            Assert.Less(f.AnalogSpeedFactor(0.3f), 1f, "partial stick walks slower");
        }
    }
}

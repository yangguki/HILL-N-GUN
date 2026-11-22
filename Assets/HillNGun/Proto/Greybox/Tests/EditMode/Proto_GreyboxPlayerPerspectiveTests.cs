using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxPlayerPerspectiveTests
    {
        [SetUp]
        public void SetUp()
        {
            Proto_GreyboxPerspectiveDepth.ResetToDefault();
            Proto_GreyboxMoveZone.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            Proto_GreyboxPerspectiveDepth.ResetToDefault();
            Proto_GreyboxMoveZone.ResetToDefaults();
        }

        [Test]
        public void PlayerHeight_AtMoveBandEnds_MatchesDefaults()
        {
            float top = Proto_GreyboxPerspectiveDepth.PlayerHeightScreenPercentAtFootPercent(62f);
            float bottom = Proto_GreyboxPerspectiveDepth.PlayerHeightScreenPercentAtFootPercent(95f);
            Assert.AreEqual(9f, top, 0.08f);
            Assert.AreEqual(12f, bottom, 0.08f);
        }

        [Test]
        public void PlayerHeight_MidBand_IsBetweenEnds()
        {
            float mid = Proto_GreyboxPerspectiveDepth.PlayerHeightScreenPercentAtFootPercent(78.5f);
            Assert.Greater(mid, 9f);
            Assert.Less(mid, 12f);
            Assert.AreEqual(10.5f, mid, 0.15f);
        }

        [Test]
        public void PlayerHeight_OutsideBand_ClampedToEndScales()
        {
            float below = Proto_GreyboxPerspectiveDepth.PlayerHeightScreenPercentAtFootPercent(30f);
            float above = Proto_GreyboxPerspectiveDepth.PlayerHeightScreenPercentAtFootPercent(99f);
            Assert.AreEqual(9f, below, 0.08f);
            Assert.AreEqual(12f, above, 0.08f);
        }

        [Test]
        public void PlayerFootprint_WiderNearThanFar()
        {
            float yNear = Proto_GreyboxLayout.ScreenYPercentToWorldY(95f);
            float yFar = Proto_GreyboxLayout.ScreenYPercentToWorldY(62f);
            var near = Proto_GreyboxBulletSim.PlayerGroundFootprint(new Vector2(0f, yNear));
            var far = Proto_GreyboxBulletSim.PlayerGroundFootprint(new Vector2(0f, yFar));
            Assert.Greater(near.width, far.width);
            Assert.Greater(near.height, far.height);
        }

        [Test]
        public void WhenPlayerPerspectiveOff_HeightFlatAtBottom()
        {
            Proto_GreyboxPerspectiveDepth.PlayerPerspectiveEnabled = false;
            Assert.AreEqual(12f, Proto_GreyboxPerspectiveDepth.PlayerHeightScreenPercentAtFootPercent(62f), 0.08f);
            Assert.AreEqual(12f, Proto_GreyboxPerspectiveDepth.PlayerHeightScreenPercentAtFootPercent(95f), 0.08f);
            float yFar = Proto_GreyboxLayout.ScreenYPercentToWorldY(62f);
            float yNear = Proto_GreyboxLayout.ScreenYPercentToWorldY(95f);
            Assert.AreEqual(
                Proto_GreyboxBulletSim.PlayerGroundFootprint(new Vector2(0f, yFar)).width,
                Proto_GreyboxBulletSim.PlayerGroundFootprint(new Vector2(0f, yNear)).width, 0.0001f);
        }

        [Test]
        public void VerticalMoveFactor_SlowsAtTop_WhenEnabled()
        {
            Assert.AreEqual(0.9f,
                Proto_GreyboxPerspectiveDepth.PlayerVerticalMoveSpeedFactorAtFootPercent(62f), 0.001f);
            Assert.AreEqual(1f,
                Proto_GreyboxPerspectiveDepth.PlayerVerticalMoveSpeedFactorAtFootPercent(95f), 0.001f);
        }

        [Test]
        public void VerticalMoveFactor_FlatWhenPlayerPerspectiveOff()
        {
            Proto_GreyboxPerspectiveDepth.PlayerPerspectiveEnabled = false;
            Assert.AreEqual(1f,
                Proto_GreyboxPerspectiveDepth.PlayerVerticalMoveSpeedFactorAtFootPercent(62f), 0.001f);
            Proto_GreyboxPerspectiveDepth.PlayerPerspectiveEnabled = true;
            Proto_GreyboxPerspectiveDepth.PlayerDepthMoveEnabled = false;
            Assert.AreEqual(1f,
                Proto_GreyboxPerspectiveDepth.PlayerVerticalMoveSpeedFactorAtFootPercent(62f), 0.001f);
        }
    }
}
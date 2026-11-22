using System.Collections.Generic;
using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxPerspectiveTests
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
        public void EnemyHeight_FollowsSixToEightPercentCurve_ClampedYf()
        {
            Assert.AreEqual(6f, Proto_GreyboxPerspectiveDepth.EnemyHeightScreenPercentAtFootPercent(40f), 0.08f);
            Assert.AreEqual(8f, Proto_GreyboxPerspectiveDepth.EnemyHeightScreenPercentAtFootPercent(52f), 0.08f);
            Assert.AreEqual(6f, Proto_GreyboxPerspectiveDepth.EnemyHeightScreenPercentAtFootPercent(30f), 0.08f);
            float scale52 = Proto_GreyboxPerspectiveDepth.GruntSpriteUniformScaleAtFootPercent(52f);
            Assert.AreEqual(86.4f / 120f, scale52, 0.02f);
        }

        [Test]
        public void WhenDisabled_HeightFlatAndWaveUsesSingleRow()
        {
            Proto_GreyboxPerspectiveDepth.Enabled = false;
            Assert.AreEqual(8f, Proto_GreyboxPerspectiveDepth.EnemyHeightScreenPercentAtFootPercent(40f), 0.01f);
            var director = new Proto_GreyboxWaveDirector(17);
            var tuning = new Proto_GreyboxRuntimeTuning();
            director.StartRun(tuning);
            const float dt = 0.02f;
            for (int i = 0; i < 400 && director.Grunts.Count < 3; i++)
                director.Tick(dt, tuning);
            foreach (var g in director.Grunts)
            {
                float pct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(g.Target.FootPosition.y);
                Assert.AreEqual(37f, pct, 0.5f);
            }
        }

        [Test]
        public void WhenEnabled_WaveUsesMultipleDepthRows()
        {
            var director = new Proto_GreyboxWaveDirector(91);
            var tuning = new Proto_GreyboxRuntimeTuning { Wave1Count = 8 };
            director.StartRun(tuning);
            const float dt = 0.02f;
            for (int i = 0; i < 800 && director.State != Proto_GreyboxWaveDirectorState.Fighting; i++)
                director.Tick(dt, tuning);
            var rows = new HashSet<int>();
            foreach (var g in director.Grunts)
            {
                float pct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(g.Target.FootPosition.y);
                Assert.GreaterOrEqual(pct, 39.5f);
                Assert.LessOrEqual(pct, 52.5f);
                rows.Add(Mathf.RoundToInt(pct));
            }

            Assert.GreaterOrEqual(rows.Count, 2);
        }

        [Test]
        public void GroundFootprint_ScalesWithPerspectiveHitScale()
        {
            float yNear = Proto_GreyboxLayout.ScreenYPercentToWorldY(52f);
            float yFar = Proto_GreyboxLayout.ScreenYPercentToWorldY(40f);
            var near = Proto_GreyboxTarget.GroundFootprint(new Vector2(0f, yNear));
            var far = Proto_GreyboxTarget.GroundFootprint(new Vector2(0f, yFar));
            Assert.Greater(near.width, far.width);
            Assert.Greater(near.height, far.height);
        }

        [Test]
        public void WorldSpriteSorting_EnemiesInFrontOfBuildingsGround_BehindFence()
        {
            float gruntY = Proto_GreyboxLayout.ScreenYPercentToWorldY(46f);
            int gruntOrder = Proto_GreyboxLayout.WorldSpriteSortingOrder(gruntY);
            Assert.Greater(gruntOrder, Proto_GreyboxLayout.SortingBuildingBase);
            Assert.Greater(gruntOrder, Proto_GreyboxLayout.SortingGround);
            Assert.Less(gruntOrder, Proto_GreyboxLayout.SortingFence);
            Assert.Less(Proto_GreyboxLayout.SortingBuildingBase, Proto_GreyboxLayout.SortingFence);
            float playerY = Proto_GreyboxLayout.ScreenYPercentToWorldY(88f);
            Assert.Greater(Proto_GreyboxLayout.WorldSpriteSortingOrder(playerY), Proto_GreyboxLayout.SortingFence);
        }

        [Test]
        public void GruntDeath_HoldFadeThenFinished_NoRestart()
        {
            float t = Proto_GreyboxGruntDeathVisual.LastFrameTime + Proto_GreyboxGruntDeathVisual.HoldDuration * 0.5f;
            Assert.AreEqual(1f, Proto_GreyboxGruntDeathVisual.AlphaAt(t, false), 0.001f);
            float fadeMid = Proto_GreyboxGruntDeathVisual.LastFrameTime + Proto_GreyboxGruntDeathVisual.HoldDuration +
                            Proto_GreyboxGruntDeathVisual.FadeDuration * 0.5f;
            float alphaMid = Proto_GreyboxGruntDeathVisual.AlphaAt(fadeMid, false);
            Assert.Greater(alphaMid, 0f);
            Assert.Less(alphaMid, 1f);
            Assert.IsTrue(Proto_GreyboxGruntDeathVisual.IsFinished(Proto_GreyboxGruntDeathVisual.TotalDuration, false));
            Assert.IsFalse(Proto_GreyboxGruntDeathVisual.ViewShouldBeActive(Proto_GreyboxGruntDeathVisual.TotalDuration, false));
        }

        [Test]
        public void EnemyBulletVisualMul_MatchesHurtboxWithSofterGlow()
        {
            Assert.AreEqual(1f, Proto_GreyboxTuning.EnemyBulletVisualDiameterMul, 0.001f);
            Assert.Greater(Proto_GreyboxTuning.EnemyBulletVisualGlowMul, Proto_GreyboxTuning.EnemyBulletVisualDiameterMul);
        }
    }
}
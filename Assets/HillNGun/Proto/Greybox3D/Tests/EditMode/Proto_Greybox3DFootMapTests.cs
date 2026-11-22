using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D.Tests.EditMode
{
    public class Proto_Greybox3DFootMapTests
    {
        [TearDown]
        public void TearDown()
        {
            Proto_GreyboxViewPresenterRegistry.ResetForTests();
        }

        [Test]
        public void ViewPresenterRegistry_CreatesWithFactory()
        {
            Proto_GreyboxViewPresenterRegistry.RegisterFactory(() => new Proto_Greybox3DViewHost());
            var presenter = Proto_GreyboxViewPresenterRegistry.Ensure();
            Assert.NotNull(presenter);
            presenter.EnsureCreated();
        }
        [Test]
        public void FootToWorld_PreservesX_AndMapsYToZ()
        {
            var foot = new Vector2(1.25f, Proto_GreyboxLayout.ScreenYPercentToWorldY(70f));
            var world = Proto_Greybox3DFootMap.FootToWorld(foot);
            Assert.AreEqual(foot.x, world.x, 1e-4f);
            Assert.AreEqual(0f, world.y, 1e-4f);
            Assert.Greater(world.z, Proto_Greybox3DFootMap.PlayerBandNearZ);
        }

        [Test]
        public void DepthZ_IsMonotonicInFootY()
        {
            float yNear = Proto_Greybox3DFootMap.FootWorldYPlayerNear;
            float yMid = Proto_Greybox3DFootMap.FootWorldYFence;
            float yFar = Proto_Greybox3DFootMap.FootWorldYEnemyFar;
            float zNear = Proto_Greybox3DFootMap.FootWorldYToDepthZ(yNear);
            float zMid = Proto_Greybox3DFootMap.FootWorldYToDepthZ(yMid);
            float zFar = Proto_Greybox3DFootMap.FootWorldYToDepthZ(yFar);
            Assert.Less(zNear, zMid);
            Assert.Less(zMid, zFar);
        }

        [Test]
        public void FenceAndPlayerBand_AreOrderedSanely()
        {
            float fence = Proto_Greybox3DFootMap.FenceDepthZ;
            float player = Proto_Greybox3DFootMap.PlayerBandNearZ;
            Assert.Greater(fence, player);
            Assert.Less(fence, Proto_Greybox3DFootMap.DepthZFar);
        }
    }
}
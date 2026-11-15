using NUnit.Framework;
using HillNGun.Combat;
using HillNGun.Core;
using HillNGun.Proto.TopDown;
using UnityEngine;

namespace HillNGun.Tests.EditMode
{
    public class ProtoMenuAndShotTests
    {
        [TearDown]
        public void TearDown() => ProtoNav.ResetLaunch();

        [Test]
        public void ShotLook_ChangesViewOnly_NotProjectileLogic()
        {
            float allyRadius = 0.18f;
            float enemyRadius = 0.22f;
            var ally = new Projectile();
            ally.InitializeSimple(Vector2.zero, Vector2.right, 12f, allyRadius, 10, 20f, Team.Player, false);
            var enemy = new Projectile();
            enemy.InitializeSimple(Vector2.zero, Vector2.left, 12f, enemyRadius, 10, 20f, Team.Enemy, false);

            Assert.AreEqual(allyRadius, ally.Radius);
            Assert.AreEqual(enemyRadius, enemy.Radius);
            Assert.AreEqual(12f, ally.BaseSpeed);
            Assert.AreEqual(10, ally.Damage);

            Assert.AreNotEqual(ProtoShotLook.AllyColor, ProtoShotLook.EnemyColor);
            Assert.Greater(ProtoShotLook.EnemyScale, ProtoShotLook.AllyScale);
            Assert.AreEqual(ProtoShotLook.AllyColor, ProtoShotLook.ColorFor(Team.Player));
            Assert.AreEqual(ProtoShotLook.EnemyColor, ProtoShotLook.ColorFor(Team.Enemy));
            Assert.AreEqual(ProtoShotLook.AllyScale, ProtoShotLook.ScaleFor(Team.Player));
            Assert.AreEqual(ProtoShotLook.EnemyScale, ProtoShotLook.ScaleFor(Team.Enemy));

            // View mapping must leave the logic projectile untouched.
            Assert.AreNotEqual(ProtoShotLook.AllyScale, ally.Radius);
            Assert.AreNotEqual(ProtoShotLook.EnemyScale, enemy.Radius);
            Assert.AreEqual(allyRadius, ally.Radius);
            Assert.AreEqual(enemyRadius, enemy.Radius);
        }

        [Test]
        public void Menu_MapsButtonsToSceneNamesAndFrontlineMode()
        {
            Assert.AreEqual("Proto_Menu", ProtoNav.MenuSceneName);
            Assert.AreEqual("Proto_TopDown", ProtoNav.TopDownSceneName);
            Assert.AreEqual("Proto_Frontline", ProtoNav.FrontlineSceneName);
            Assert.AreEqual(ProtoNav.MenuSceneName, ProtoNav.SceneNameFor(ProtoAssets.MenuScene));
            Assert.AreEqual(ProtoNav.TopDownSceneName, ProtoNav.SceneNameFor(ProtoAssets.Scene));
            Assert.AreEqual(ProtoNav.FrontlineSceneName, ProtoNav.SceneNameFor(ProtoAssets.FrontlineScene));

            ProtoNav.SetFrontlineLaunch(true);
            Assert.IsTrue(ProtoNav.TryTakeFrontlineThree(out bool three));
            Assert.IsTrue(three);
            Assert.IsFalse(ProtoNav.TryTakeFrontlineThree(out _));

            ProtoNav.SetFrontlineLaunch(false);
            Assert.IsTrue(ProtoNav.TryTakeFrontlineThree(out three));
            Assert.IsFalse(three);
        }
    }
}

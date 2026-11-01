using NUnit.Framework;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Combat;
using HillNGun.Characters;

namespace HillNGun.Tests.EditMode
{
    /// <summary>t49u regression: grenades must recharge (01 §8, 12 s each). GrenadeSystem.Update was never called.</summary>
    public class GrenadeRechargeTests
    {
        private GrenadeData _gd;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _gd = ScriptableObject.CreateInstance<GrenadeData>();
            _gd.rechargeTime = 12f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gd);
        }

        [Test]
        public void GrenadeSystem_RechargesOneEveryRechargeTime()
        {
            var g = new GrenadeSystem(_gd, 2);
            Assert.IsTrue(g.TryUseGrenade());
            Assert.IsTrue(g.TryUseGrenade());
            Assert.AreEqual(0, g.CurrentGrenades);
            for (int i = 0; i < 119; i++) g.Update(0.1f); // 11.9 s
            Assert.AreEqual(0, g.CurrentGrenades, "not before 12 s");
            for (int i = 0; i < 2; i++) g.Update(0.1f);   // 12.1 s
            Assert.AreEqual(1, g.CurrentGrenades);
            for (int i = 0; i < 125; i++) g.Update(0.1f);
            Assert.AreEqual(2, g.CurrentGrenades);
            for (int i = 0; i < 200; i++) g.Update(0.1f);
            Assert.AreEqual(2, g.CurrentGrenades, "never above the carry limit");
        }

        [Test]
        public void Character_TimerTick_RechargesGrenades()
        {
            var cd = ScriptableObject.CreateInstance<CharacterData>();
            var wd = ScriptableObject.CreateInstance<WeaponData>();
            cd.weapon = wd;
            cd.grenade = _gd;
            cd.grenadeCount = 1;
            cd.maxHp = 3000;
            _go = new GameObject("grenade_recharge_test");
            var c = _go.AddComponent<CharacterStateMachine>();
            c.Initialize(cd, Team.Player, 0, new Vector2(12f, 2f), 0);
            Assert.IsTrue(c.GrenadeSystem.TryUseGrenade());
            Assert.IsFalse(c.HasGrenadeReady);
            for (int i = 0; i < 125; i++) c.TickTimers(0.1f);
            Assert.IsTrue(c.HasGrenadeReady, "the character's per-frame timer path must recharge grenades");
            Assert.AreEqual(1, c.GrenadeSystem.CurrentGrenades);
            Object.DestroyImmediate(cd);
            Object.DestroyImmediate(wd);
        }
    }
}

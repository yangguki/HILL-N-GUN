using NUnit.Framework;
using UnityEngine;
using HillNGun.Data;
using HillNGun.Combat;
using HillNGun.Core;
using HillNGun.PvE;

namespace HillNGun.Tests.EditMode
{
    /// <summary>v2 P0 (07_weapon_range.md): range/speed/lifetime/falloff + D1 discarded (mid-row start, advance only OOR).</summary>
    public class WeaponRangeV2Tests
    {
        private WeaponData Make(string id, float range, float speed, float life, float radius,
            float fullPellet = 0f, float falloffStart = 0f, float falloffMul = 1f, bool laser = false,
            int dmg = 150, int pellets = 1, float interval = 0.6f, int cells = 3, float rech = 1.2f,
            int shotsPerCell = 1)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.id = id;
            w.range = range;
            w.projectileSpeed = speed;
            w.projectileLifetime = life;
            w.projectileRadius = radius;
            w.fullPelletRange = fullPellet;
            w.damageFalloffStart = falloffStart;
            w.damageFalloffEndMul = falloffMul;
            w.aimLaser = laser;
            w.damagePerProjectile = dmg;
            w.projectilesPerShot = pellets;
            w.fireInterval = interval;
            w.gaugeCells = cells;
            w.shotsPerCell = shotsPerCell;
            w.rechargePerCell = rech;
            w.spreadDeg = pellets > 1 ? 30f : 0f;
            return w;
        }

        private WeaponData Buster() => Make("wpn_buster", 8f, 18f, 0.44f, 0.30f, fullPellet: 4f, dmg: 150, pellets: 5, interval: 0.6f, cells: 3, rech: 1.2f);
        private WeaponData Raven()
        {
            var w = Make("wpn_raven", 16f, 32f, 0.50f, 0.12f, falloffStart: 12f, falloffMul: 0.7f, laser: true, dmg: 900, interval: 1.2f, cells: 2, rech: 2.0f);
            w.chargeMinTime = 0.3f; w.chargeMaxTime = 1.0f; w.chargeDamageMul = 1.6f; w.fireMode = FireMode.ChargeRelease;
            return w;
        }
        private WeaponData Volt() => Make("wpn_volt", 10f, 22f, 0.45f, 0.25f, dmg: 520, interval: 0.7f, cells: 3, rech: 1.5f);

        [Test]
        public void StarterWeapons_MatchV2RangeTable()
        {
            var b = Buster(); var r = Raven(); var v = Volt();
            Assert.AreEqual(8f, b.rangeTiles);
            Assert.AreEqual(18f, b.projectileSpeed);
            Assert.AreEqual(0.44f, b.projectileLifetime);
            Assert.AreEqual(0.30f, b.projectileRadius);
            Assert.AreEqual(4f, b.fullPelletRange);
            Assert.AreEqual(0f, b.damageFalloffStart);

            Assert.AreEqual(16f, r.range);
            Assert.AreEqual(32f, r.projectileSpeed);
            Assert.AreEqual(0.50f, r.projectileLifetime);
            Assert.AreEqual(0.12f, r.projectileRadius);
            Assert.AreEqual(12f, r.damageFalloffStart);
            Assert.AreEqual(0.7f, r.damageFalloffEndMul);
            Assert.IsTrue(r.aimLaser);

            Assert.AreEqual(10f, v.range);
            Assert.AreEqual(22f, v.projectileSpeed);
            Assert.AreEqual(0.45f, v.projectileLifetime);
            Assert.AreEqual(0.25f, v.projectileRadius);
            Assert.AreEqual(0f, v.damageFalloffStart);
            Assert.IsFalse(v.aimLaser);

            Object.DestroyImmediate(b); Object.DestroyImmediate(r); Object.DestroyImmediate(v);
        }

        [Test]
        public void Lifetime_EqualsRangeOverSpeed_WithinTolerance()
        {
            foreach (var w in new[] { Buster(), Raven(), Volt() })
            {
                // authored lifetime = range / BASE speed; runtime lifetime = range / (base x global factor 0.8)
                Assert.AreEqual(w.range / w.projectileSpeed, w.projectileLifetime, 0.02f, w.id + " authored base lifetime");
                float expected = w.range / (w.projectileSpeed * WeaponData.GlobalProjectileSpeedMul);
                Assert.AreEqual(expected, w.EffectiveLifetime, 1e-4f, w.id);
                Assert.AreEqual(w.projectileLifetime / WeaponData.GlobalProjectileSpeedMul, w.EffectiveLifetime, 0.03f, w.id + " explicit lifetime / mul");
                Object.DestroyImmediate(w);
            }
        }

        [Test]
        public void Buster_Projectile_ReachesMidToEnemyFront_AtLeast6_5Tiles()
        {
            var w = Buster();
            var p = new Projectile();
            p.Initialize(new Vector2(12f, 4.5f), Vector2.up, w, w.damagePerProjectile, Team.Player);
            float dt = 1f / 60f;
            while (p.IsActive) p.Update(dt);
            Assert.GreaterOrEqual(p.DistanceTraveled, 6.5f, "mid-row(4.5) → enemy front(11) = 6.5");
            Assert.AreEqual(w.range, p.DistanceTraveled, 0.2f); // one simulation frame may overshoot
            Object.DestroyImmediate(w);
        }

        [Test]
        public void Raven_Falloff_Between12And16()
        {
            var w = Raven();
            Assert.AreEqual(1f, w.GetDamageMultiplierAtDistance(12f), 0.001f);
            Assert.AreEqual(0.7f, w.GetDamageMultiplierAtDistance(16f), 0.001f);
            float mid = w.GetDamageMultiplierAtDistance(14f);
            Assert.AreEqual(0.85f, mid, 0.02f);

            var p = new Projectile();
            p.Initialize(Vector2.zero, Vector2.up, w, 900, Team.Player);
            // simulate travel to 14 tiles
            float need = 14f;
            while (p.IsActive && p.DistanceTraveled < need) p.Update(0.01f);
            Assert.AreEqual(Mathf.RoundToInt(900 * mid), p.EffectiveDamage, 5);
            // charged 1440 at end range
            var p2 = new Projectile();
            p2.Initialize(Vector2.zero, Vector2.up, w, 1440, Team.Player);
            while (p2.IsActive) p2.Update(0.01f);
            Assert.AreEqual(Mathf.RoundToInt(1440 * 0.7f), p2.EffectiveDamage);
            Object.DestroyImmediate(w);
        }

        [Test]
        public void Buster_SpreadAngles_Unchanged()
        {
            var w = Buster();
            var a = w.GetSpreadAngles();
            Assert.AreEqual(5, a.Length);
            Assert.AreEqual(-15f, a[0], 0.01f);
            Assert.AreEqual(-7.5f, a[1], 0.01f);
            Assert.AreEqual(0f, a[2], 0.01f);
            Assert.AreEqual(7.5f, a[3], 0.01f);
            Assert.AreEqual(15f, a[4], 0.01f);
            Object.DestroyImmediate(w);
        }

        [Test]
        public void AssaultStartsFrontRow_DefaultsOff_MidRowIsDesignDefault()
        {
            var bf = ScriptableObject.CreateInstance<BattlefieldData>();
            Assert.IsFalse(bf.assaultStartsFrontRow, "v2 D1 discarded");
            Assert.AreEqual(1, bf.startRowIndex);
            Assert.AreEqual(4.5f, bf.GetOurRowY(bf.startRowIndex), 0.01f);
            // front row is index 2 when ourRowsY = {2,4.5,7}
            Assert.AreEqual(7f, bf.GetOurRowY(2), 0.01f);
            Object.DestroyImmediate(bf);
        }

        [Test]
        public void AI_AdvancesOnlyWhenOutOfRange()
        {
            Assert.IsFalse(PvEMatchManager.ShouldAdvanceForRange(hasTargetInWeaponRange: true, advanceWhenOutOfRangeEnabled: true));
            Assert.IsTrue(PvEMatchManager.ShouldAdvanceForRange(hasTargetInWeaponRange: false, advanceWhenOutOfRangeEnabled: true));
            Assert.IsFalse(PvEMatchManager.ShouldAdvanceForRange(hasTargetInWeaponRange: false, advanceWhenOutOfRangeEnabled: false));
        }

        [Test]
        public void BurstDpsAndShot_MatchCalcV2_Within5Percent()
        {
            // calc_v2.json "res": burst = shot/interval, shot = full pellet or charged max. HP/dmg/interval unchanged.
            var b = Buster(); var r = Raven(); var v = Volt();
            int bShot = b.damagePerProjectile * b.projectilesPerShot; // 750
            Assert.AreEqual(750, bShot);
            Assert.AreEqual(1250f, bShot / b.fireInterval, 1250f * 0.05f);
            int rShot = Mathf.RoundToInt(r.damagePerProjectile * r.chargeDamageMul); // 900*1.6=1440
            Assert.AreEqual(1440, rShot);
            Assert.AreEqual(1200f, rShot / r.fireInterval, 1200f * 0.05f);
            Assert.AreEqual(520, v.damagePerProjectile);
            Assert.AreEqual(743f, v.damagePerProjectile / v.fireInterval, 743f * 0.05f);
            // calc_v2 ttk shot counts (second element) with current CharacterData HP (3200/2200/2600)
            Assert.AreEqual(5, Mathf.CeilToInt(3200f / 750f));
            Assert.AreEqual(3, Mathf.CeilToInt(2200f / 750f));
            Assert.AreEqual(4, Mathf.CeilToInt(2600f / 750f));
            Assert.AreEqual(3, Mathf.CeilToInt(3200f / 1440f));
            Assert.AreEqual(2, Mathf.CeilToInt(2200f / 1440f));
            Assert.AreEqual(7, Mathf.CeilToInt(3200f / 520f));
            Assert.AreEqual(5, Mathf.CeilToInt(2200f / 520f));
            // Burst DPS already checked ±5% vs calc_v2.res. Range-only change keeps DPS/TTK invariants.
            Object.DestroyImmediate(b); Object.DestroyImmediate(r); Object.DestroyImmediate(v);
        }

    }
}

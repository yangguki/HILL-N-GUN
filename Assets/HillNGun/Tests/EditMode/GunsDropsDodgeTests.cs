using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Combat;
using HillNGun.Cover;
using HillNGun.Progression;

namespace HillNGun.Tests.EditMode
{
    /// <summary>t48u: visible projectile travel, enemy bullet-dodge reaction, gun shop persistence, drop table, pickup, cover judgment.</summary>
    [TestFixture]
    public class ProjectileTravelTests
    {
        [Test]
        public void Bullet_TravelsAtWeaponSpeed_AndExpiresAtRange()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.projectileSpeed = 22f; w.range = 7.5f; w.projectileRadius = 0.25f;
            var p = new Projectile();
            p.Initialize(new Vector2(12f, 4.5f), Vector2.up, w, 380, Team.Player);
            // runtime speed = base 22 x global factor (default 0.8 -> 17.6); range 7.5 unchanged
            float v = 22f * WeaponData.GlobalProjectileSpeedMul;
            p.Update(0.1f);
            Assert.AreEqual(4.5f + v * 0.1f, p.Position.y, 0.001f, "visible travel, not hitscan");
            Assert.AreEqual(v * 0.1f, p.DistanceTraveled, 0.001f);
            Assert.IsTrue(p.IsActive);
            int steps = Mathf.CeilToInt(7.5f / v / 0.1f) - 1;
            for (int i = 0; i < steps - 1; i++) p.Update(0.1f);
            Assert.IsTrue(p.IsActive, "still flying just before range / effective speed");
            p.Update(0.1f);
            Assert.IsFalse(p.IsActive, "gone after range / effective speed (0.43 s at x0.8)");
            Object.DestroyImmediate(w);
        }
    }

    [TestFixture]
    public class BulletDodgeTests
    {
        private static DodgeTuning T(float chance) => new DodgeTuning { chance = chance, delayMin = 0.2f, delayMax = 0.2f, lookahead = 0.6f, margin = 0.15f, cooldown = 2f };

        private static Projectile Bullet(Vector2 from, Vector2 dir, float speed, Team team)
        {
            var p = new Projectile();
            p.InitializeSimple(from, dir, speed, 0.2f, 100, 20f, team);
            return p;
        }

        [Test]
        public void IsThreat_HeadOnInsideLookahead_True_PassingOrFar_False()
        {
            Vector2 me = new Vector2(10f, 13.5f);
            Assert.IsTrue(BulletDodgeSense.IsThreat(me, 0.5f, me + Vector2.down * 4f, Vector2.up, 20f, 0.2f, 0.6f, 0.15f, out float t, out Vector2 side));
            Assert.AreEqual(0.2f, t, 0.001f);
            Assert.AreEqual(0f, side.y); Assert.AreEqual(1f, Mathf.Abs(side.x));
            Assert.IsFalse(BulletDodgeSense.IsThreat(me, 0.5f, me + Vector2.up * 1f, Vector2.up, 20f, 0.2f, 0.6f, 0.15f, out _, out _), "already passed");
            Assert.IsFalse(BulletDodgeSense.IsThreat(me, 0.5f, me + new Vector2(2f, -4f), Vector2.up, 20f, 0.2f, 0.6f, 0.15f, out _, out _), "clear miss");
            Assert.IsFalse(BulletDodgeSense.IsThreat(me, 0.5f, me + Vector2.down * 20f, Vector2.up, 20f, 0.2f, 0.6f, 0.15f, out _, out _), "too far away");
        }

        [Test]
        public void IsThreat_StepsAwayFromTheBulletLine()
        {
            Vector2 me = new Vector2(10f, 13.5f);
            // bullet line passes 0.3 left of us → step right
            Assert.IsTrue(BulletDodgeSense.IsThreat(me, 0.5f, new Vector2(9.7f, 10f), Vector2.up, 20f, 0.2f, 0.6f, 0.15f, out _, out Vector2 side));
            Assert.AreEqual(1f, side.x);
        }

        [Test]
        public void Reaction_HappensAfterDelay_Once_ThenCooldown()
        {
            var sense = new BulletDodgeSense(T(1f), () => 0f);
            Vector2 me = new Vector2(10f, 13.5f);
            var list = new List<Projectile> { Bullet(me + Vector2.down * 3f, Vector2.up, 10f, Team.Player) };
            Assert.IsFalse(sense.Tick(0f, me, 0.5f, list, Team.Player, out _), "not instant");
            Assert.IsTrue(sense.HasPending);
            Assert.IsFalse(sense.Tick(0.1f, me, 0.5f, list, Team.Player, out _));
            Assert.IsTrue(sense.Tick(0.21f, me, 0.5f, list, Team.Player, out Vector2 dir), "reacts after the data delay");
            Assert.AreNotEqual(Vector2.zero, dir);
            // another bullet during cooldown: judged but no reaction
            list.Add(Bullet(me + Vector2.down * 3f, Vector2.up, 10f, Team.Player));
            Assert.IsFalse(sense.Tick(0.5f, me, 0.5f, list, Team.Player, out _));
            Assert.IsFalse(sense.Tick(1.0f, me, 0.5f, list, Team.Player, out _));
            Assert.AreEqual(1, sense.Reactions);
        }

        [Test]
        public void Reaction_RespectsChance_AndIgnoresFriendlyBullets()
        {
            Vector2 me = new Vector2(10f, 13.5f);
            var never = new BulletDodgeSense(T(0.3f), () => 0.9f);
            var list = new List<Projectile> { Bullet(me + Vector2.down * 3f, Vector2.up, 10f, Team.Player) };
            for (int i = 0; i < 10; i++) Assert.IsFalse(never.Tick(i * 0.1f, me, 0.5f, list, Team.Player, out _));
            Assert.AreEqual(1, never.Detected, "each bullet is judged once");
            var friendly = new BulletDodgeSense(T(1f), () => 0f);
            Assert.IsFalse(friendly.Tick(0f, me, 0.5f, list, Team.Enemy, out _));
            Assert.IsFalse(friendly.HasPending);
        }
    }

    [TestFixture]
    public class GunShopTests
    {
        private class MemStorage : ISaveStorage
        {
            public string json;
            public string Location => "memory";
            public bool TryRead(out string j) { j = json; return json != null; }
            public void Write(string j) { json = j; }
            public void Delete() { json = null; }
        }

        private ProgressionData _cfg;
        private WeaponData _starter, _revolver;

        [SetUp]
        public void SetUp()
        {
            _starter = ScriptableObject.CreateInstance<WeaponData>(); _starter.id = "wpn_volt";
            _revolver = ScriptableObject.CreateInstance<WeaponData>(); _revolver.id = "wpn_peacemaker";
            var cat = ScriptableObject.CreateInstance<WeaponCatalogData>();
            cat.offers = new[]
            {
                new WeaponOffer { weapon = _starter, ownedByDefault = true, characterIds = new[] { "volt" } },
                new WeaponOffer { weapon = _revolver, cost = 1800, characterIds = new[] { "volt" } },
            };
            _cfg = ScriptableObject.CreateInstance<ProgressionData>();
            _cfg.weaponCatalog = cat;
            ServiceLocator.Register(_cfg);
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Unregister<ProgressionData>();
            Object.DestroyImmediate(_cfg.weaponCatalog);
            Object.DestroyImmediate(_cfg);
            Object.DestroyImmediate(_starter);
            Object.DestroyImmediate(_revolver);
        }

        [Test]
        public void PurchaseAndEquip_PersistAcrossReload()
        {
            var store = new MemStorage();
            var p = new ProgressionService(store);
            Assert.IsTrue(p.OwnsWeapon("wpn_volt"), "starter gun owned by default");
            Assert.IsFalse(p.TryPurchaseWeapon("wpn_peacemaker"), "not enough bounty");
            p.Data.bounty = 2000;
            Assert.IsTrue(p.TryPurchaseWeapon("wpn_peacemaker"));
            Assert.AreEqual(200, p.Data.bounty);
            Assert.IsFalse(p.TryPurchaseWeapon("wpn_peacemaker"), "no double purchase");
            Assert.IsFalse(p.EquipWeapon("raven", "wpn_peacemaker"), "role lock");
            Assert.IsTrue(p.EquipWeapon("volt", "wpn_peacemaker"));

            var reloaded = new ProgressionService(store);
            Assert.IsTrue(reloaded.OwnsWeapon("wpn_peacemaker"));
            Assert.AreEqual(_revolver, reloaded.GetEquippedWeapon("volt"));
            Assert.AreEqual(200, reloaded.Data.bounty);

            Assert.IsTrue(reloaded.EquipWeapon("volt", "wpn_volt"));
            Assert.IsNull(reloaded.GetEquippedWeapon("volt"), "starter = keep CharacterData.weapon");
        }

        [Test]
        public void Loadout_KeepsDefaultData_OrSwapsOnlyTheWeapon()
        {
            var cd = ScriptableObject.CreateInstance<CharacterData>();
            cd.id = "volt"; cd.weapon = _starter; cd.maxHp = 3200;
            Assert.AreSame(cd, Loadout.WithWeapon(cd, null), "default loadout = same asset (balance untouched)");
            Assert.AreSame(cd, Loadout.WithWeapon(cd, _starter));
            var copy = Loadout.WithWeapon(cd, _revolver);
            Assert.AreNotSame(cd, copy);
            Assert.AreEqual(_revolver, copy.weapon);
            Assert.AreEqual(3200, copy.maxHp);
            Assert.AreSame(copy, Loadout.WithWeapon(cd, _revolver), "cached");
            Assert.AreEqual(_starter, cd.weapon, "source asset not modified");
            Object.DestroyImmediate(copy);
            Object.DestroyImmediate(cd);
        }
    }

    [TestFixture]
    public class DropTableTests
    {
        [Test]
        public void Pick_FollowsWeights()
        {
            var t = ScriptableObject.CreateInstance<DropTableData>();
            float total = t.TotalWeight;
            Assert.Greater(total, 0f);
            Assert.AreEqual(t.entries[0].type, t.Pick(0f).type);
            Assert.AreEqual(t.entries[t.entries.Length - 1].type, t.Pick(0.9999f).type);
            // counts over a uniform sweep match the weights
            var counts = new Dictionary<DropType, int>();
            const int N = 10000;
            for (int i = 0; i < N; i++) { var e = t.Pick((i + 0.5f) / N); counts[e.type] = counts.TryGetValue(e.type, out int c) ? c + 1 : 1; }
            foreach (var e in t.entries) Assert.AreEqual(e.weight / total, counts[e.type] / (float)N, 0.002f, e.type.ToString());
            Object.DestroyImmediate(t);
        }

        [Test]
        public void Roll_UsesChance_AndSpawnHonoursMaxActiveAndLifetime()
        {
            var t = ScriptableObject.CreateInstance<DropTableData>();
            t.maxActive = 3; t.lifetime = 2f; t.tossTime = 0.5f;
            var seq = new Queue<float>(new[] { 0.5f, 0.1f, 0.0f });
            var s = new DropSystem(t, () => seq.Count > 0 ? seq.Dequeue() : 0f);
            Assert.IsNull(s.Roll(0.35f), "0.5 >= 0.35 → no drop");
            Assert.IsNotNull(s.Roll(0.35f), "0.1 < 0.35 → drop");
            for (int i = 0; i < 5; i++) s.Spawn(t.entries[0], Vector2.zero, new Vector2(i, 2f), Team.Player);
            Assert.AreEqual(3, s.Items.Count, "oldest removed beyond maxActive");
            Assert.IsFalse(s.Items[0].Landed);
            s.Tick(0.6f, null);
            Assert.IsTrue(s.Items[0].Landed);
            s.Tick(2.0f, null);
            Assert.AreEqual(0, s.Items.Count, "expired after toss + lifetime");
            Assert.AreEqual(3, s.Expired);
            Object.DestroyImmediate(t);
        }

        [Test]
        public void Pickup_ReachIsHorizontalRadiusOnTheSameRow()
        {
            var t = ScriptableObject.CreateInstance<DropTableData>();
            var s = new DropSystem(t);
            Assert.IsTrue(s.InReach(new Vector2(10f, 4.5f), new Vector2(10.7f, 4.5f)));
            Assert.IsFalse(s.InReach(new Vector2(10f, 4.5f), new Vector2(11f, 4.5f)));
            Assert.IsFalse(s.InReach(new Vector2(10f, 2f), new Vector2(10f, 4.5f)), "other row");
            Object.DestroyImmediate(t);
        }
    }

    [TestFixture]
    public class CoverJudgmentTests
    {
        [Test]
        public void SegmentHitsRect_Basics()
        {
            var r = new Rect(9f, 5f, 2f, 0.6f);
            Assert.IsTrue(CombatSystem.SegmentHitsRect(new Vector2(10f, 3f), new Vector2(10f, 8f), r));
            Assert.IsFalse(CombatSystem.SegmentHitsRect(new Vector2(12f, 3f), new Vector2(12f, 8f), r));
            Assert.IsFalse(CombatSystem.SegmentHitsRect(new Vector2(10f, 3f), new Vector2(10f, 4.9f), r));
        }

        [Test]
        public void DestroyedCover_NoLongerBlocks()
        {
            var go = new GameObject("cs");
            var cs = go.AddComponent<CombatSystem>();
            typeof(CombatSystem).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(cs, null);
            var data = ScriptableObject.CreateInstance<CoverData>();
            data.maxHp = 1000; data.width = 1.5f;
            var cover = new CoverInstance();
            cover.Initialize(data, new Vector2(10f, 7.6f), 2, Team.Player);
            cs.RegisterCover(cover);
            Vector2 a = new Vector2(10f, 12f), b = new Vector2(10f, 5f);
            Assert.IsTrue(cs.CoverBlocksSegment(a, b, Team.Enemy), "intact cover blocks enemy shots");
            Assert.IsFalse(cs.CoverBlocksSegment(b, a, Team.Player), "own cover never blocks own shots");
            cover.TakeDamage(5000);
            Assert.IsTrue(cover.IsDestroyed);
            Assert.IsFalse(cs.CoverBlocksSegment(a, b, Team.Enemy), "destroyed cover loses its cover judgment");
            ServiceLocator.Unregister<CombatSystem>();
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(data);
        }
    }
}

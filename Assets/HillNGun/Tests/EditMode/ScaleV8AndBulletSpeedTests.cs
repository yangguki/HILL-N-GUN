using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Combat;
using HillNGun.Settings;
using SettingsService = HillNGun.Settings.SettingsService;

namespace HillNGun.Tests.EditMode
{
    /// <summary>
    /// v8: visual character scale 14/13/12% (default 2) split from gameplay collision, and the global projectile
    /// speed factor (12_terrain_props §1: x0.8, range kept, lifetime = range / effective speed).
    /// </summary>
    public class ScaleV8AndBulletSpeedTests
    {
        private ScalePresetData _d;
        private const string WeaponDir = "Assets/HillNGun/Data/Weapons/";

        [SetUp]
        public void SetUp()
        {
            _d = Resources.Load<ScalePresetData>(ScalePresetData.ResourcePath);
            Assert.IsNotNull(_d);
            WeaponData.ResetGlobalProjectileSpeedMul();
        }

        [TearDown]
        public void TearDown()
        {
            WeaponData.ResetGlobalProjectileSpeedMul();
            ScalePresets.SetDataForTests(null, 0);
        }

        // ---------------------------------------------------------------- bullet speed

        [Test]
        public void GlobalSpeedMul_DefaultIs0_8_WithDebugOptions()
        {
            Assert.AreEqual(0.8f, WeaponData.DefaultGlobalProjectileSpeedMul, 1e-6f);
            Assert.AreEqual(0.8f, WeaponData.GlobalProjectileSpeedMul, 1e-6f);
            CollectionAssert.AreEqual(new[] { 0.7f, 0.8f, 1.0f }, WeaponData.GlobalProjectileSpeedMulOptions);
        }

        [Test]
        public void StarterWeapons_AssetSpeedsStayBase_EffectiveIsX0_8()
        {
            var expect = new Dictionary<string, (float baseV, float eff, float range, float life)>
            {
                { "wpn_buster", (18f, 14.4f, 8f, 0.5556f) },
                { "wpn_raven", (32f, 25.6f, 16f, 0.6250f) },
                { "wpn_volt", (22f, 17.6f, 10f, 0.5682f) },
            };
            foreach (var kv in expect)
            {
                var w = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponDir + kv.Key + ".asset");
                Assert.IsNotNull(w, kv.Key);
                Assert.AreEqual(kv.Value.baseV, w.projectileSpeed, 1e-4f, kv.Key + " base asset speed unchanged");
                Assert.AreEqual(kv.Value.range, w.range, 1e-4f, kv.Key + " range unchanged");
                Assert.AreEqual(kv.Value.eff, w.EffectiveProjectileSpeed, 1e-3f, kv.Key + " effective speed");
                Assert.AreEqual(kv.Value.life, w.EffectiveLifetime, 1e-3f, kv.Key + " lifetime = range / effective");
            }
        }

        [Test]
        public void EveryToggle_KeepsRange_LifetimeIsRangeOverEffectiveSpeed()
        {
            foreach (var mul in WeaponData.GlobalProjectileSpeedMulOptions)
            {
                WeaponData.GlobalProjectileSpeedMul = mul;
                foreach (var id in new[] { "wpn_buster", "wpn_raven", "wpn_volt", "wpn_peacemaker", "wpn_lever_action", "wpn_coachgun", "wpn_gatling" })
                {
                    var w = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponDir + id + ".asset");
                    if (w == null) continue;
                    var p = new Projectile();
                    p.Initialize(Vector2.zero, Vector2.up, w, 100, Team.Player);
                    Assert.AreEqual(w.projectileSpeed * mul, p.Speed, 1e-4f, $"{id} x{mul} speed");
                    Assert.AreEqual(w.projectileSpeed, p.BaseSpeed, 1e-4f);
                    Assert.AreEqual(w.range / (w.projectileSpeed * mul), p.Lifetime, 1e-4f, $"{id} x{mul} lifetime");
                    Assert.AreEqual(p.Lifetime, w.EffectiveLifetime, 1e-4f);
                    float dt = 1f / 120f, t = 0f;
                    while (p.IsActive && t < 5f) { p.Update(dt); t += dt; }
                    Assert.AreEqual(w.range, p.DistanceTraveled, w.projectileSpeed * mul * dt + 1e-3f, $"{id} x{mul} range kept");
                    Assert.AreEqual(p.Lifetime, t, dt + 1e-4f, $"{id} x{mul} flight time");
                }
            }
        }

        [Test]
        public void SpeedMul_IsAppliedOnce_NotCumulative()
        {
            var w = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponDir + "wpn_buster.asset");
            var pool = new ProjectilePool(4);
            var a = pool.Spawn(Vector2.zero, Vector2.up, w, 1, Team.Player);
            a.Deactivate();
            var b = pool.Spawn(Vector2.zero, Vector2.up, w, 1, Team.Player); // same pooled instance re-initialised
            Assert.AreEqual(14.4f, b.Speed, 1e-4f);
            Assert.AreEqual(18f, w.projectileSpeed, 1e-6f, "asset never mutated");
            var e1 = pool.SpawnSimple(Vector2.zero, Vector2.up, 8f, 0.2f, 1, 10f, Team.Enemy);
            e1.Deactivate();
            var e2 = pool.SpawnSimple(Vector2.zero, Vector2.up, 8f, 0.2f, 1, 10f, Team.Enemy);
            Assert.AreEqual(6.4f, e2.Speed, 1e-4f);
        }

        [Test]
        public void EnemyBullets_And_RexFanShot_HonorMulOnce_RangeLimitedLifetime()
        {
            foreach (var id in new[] { "enemy_pistol_mook", "enemy_dynamite_bandit", "enemy_mounted_raider" })
            {
                var e = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/HillNGun/Data/Enemies/" + id + ".asset");
                Assert.IsNotNull(e, id);
                var p = new Projectile();
                p.InitializeSimple(Vector2.zero, Vector2.down, e.bulletSpeed, e.bulletRadius, 10, e.attackRange, Team.Enemy);
                Assert.AreEqual(e.bulletSpeed * 0.8f, p.Speed, 1e-4f, id);
                Assert.AreEqual(e.attackRange / (e.bulletSpeed * 0.8f), p.Lifetime, 1e-4f, id);
            }
            var rex = AssetDatabase.LoadAssetAtPath<BossData>("Assets/HillNGun/Data/Bosses/boss_rattlesnake_rex.asset");
            Assert.IsNotNull(rex);
            foreach (var ph in rex.phases)
            {
                Assert.AreEqual(9f, ph.fanShotBulletSpeed, 1e-4f, "base asset speed unchanged");
                var p = new Projectile();
                p.InitializeSimple(Vector2.zero, Vector2.down, ph.fanShotBulletSpeed, ph.fanShotBulletRadius, 10, 24f, Team.Enemy);
                Assert.AreEqual(7.2f, p.Speed, 1e-4f, "Rex fan 9 -> 7.2");
                Assert.AreEqual(24f / 7.2f, p.Lifetime, 1e-4f);
                float dt = 1f / 60f; while (p.IsActive) p.Update(dt);
                Assert.AreEqual(24f, p.DistanceTraveled, 7.2f * dt + 1e-3f, "fan range kept");
            }
        }

        [Test]
        public void ThrownDynamite_And_Grenades_AreNotSpeedScaled()
        {
            var bandit = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/HillNGun/Data/Enemies/enemy_dynamite_bandit.asset");
            float speed = bandit.attackRange / Mathf.Max(0.1f, bandit.explosiveFlightTime); // HandleEnemyExplosive
            var p = new Projectile();
            p.InitializeSimple(Vector2.zero, Vector2.down, speed, 0.2f, 10, bandit.attackRange, Team.Enemy, applyGlobalSpeedMul: false);
            Assert.AreEqual(speed, p.Speed, 1e-5f);
            Assert.AreEqual(bandit.explosiveFlightTime, p.Lifetime, 1e-3f, "dynamite flight time unchanged");
            // Grenade code never references the global factor (parabolic flight/timing unchanged)
            foreach (var f in new[] { "GrenadeProjectile.cs", "GrenadeSystem.cs" })
            {
                string src = System.IO.File.ReadAllText("Assets/HillNGun/Scripts/Runtime/Combat/" + f);
                StringAssert.DoesNotContain("GlobalProjectileSpeedMul", src, f);
                StringAssert.DoesNotContain("GlobalSpeed(", src, f);
            }
        }

        // ---------------------------------------------------------------- visual scale

        [Test]
        public void Presets_Visual14_13_12_Default2_GameplayFracsUnchanged()
        {
            Assert.AreEqual(2, _d.defaultOption);
            float[] vis = { 0.14f, 0.13f, 0.12f }, game = { 0.10f, 0.09f, 0.08f };
            for (int n = 1; n <= 3; n++)
            {
                var o = _d.GetOption(n);
                Assert.AreEqual(vis[n - 1], o.VisualHeightFrac, 1e-5f, $"opt{n} visual");
                Assert.AreEqual(game[n - 1], o.characterHeightFrac, 1e-5f, $"opt{n} gameplay frac (hit size) unchanged");
                Assert.AreEqual(vis[n - 1] * 1080f, ScalePresets.VisualHeightWorldFor(_d, o) / _d.screenHeightWorld * 1080f, 0.01f);
            }
            ScalePresets.SetDataForTests(_d, 0);
            Assert.AreEqual(2, ScalePresets.Current);
            Assert.AreEqual(0.13f / _d.baselineCharacterHeightFrac, ScalePresets.VisualMul, 1e-5f);
            Assert.AreEqual(0.09f / _d.baselineCharacterHeightFrac, ScalePresets.CharacterMul, 1e-5f);
        }

        [Test]
        public void VisualFrac_DoesNotChange_HitRadius_CoverFootprint_OrProtectionGeometry()
        {
            var covers = new List<CoverData>();
            foreach (var g in AssetDatabase.FindAssets("t:CoverData"))
                covers.Add(AssetDatabase.LoadAssetAtPath<CoverData>(AssetDatabase.GUIDToAssetPath(g)));
            foreach (var o in _d.options)
            {
                float mul = ScalePresets.CharacterMulFor(_d, o), hurt = ScalePresets.HurtDiameterFor(_d, o);
                var logic = new List<float>();
                foreach (var c in covers) logic.Add(ScalePresets.CoverSizeFor(_d, o, c).logicWidth);
                float keep = o.visualHeightFrac;
                try
                {
                    o.visualHeightFrac = 0.30f; // absurd drawn size: gameplay must not move
                    Assert.AreEqual(mul, ScalePresets.CharacterMulFor(_d, o), 1e-6f);
                    Assert.AreEqual(hurt, ScalePresets.HurtDiameterFor(_d, o), 1e-6f);
                    for (int i = 0; i < covers.Count; i++)
                        Assert.AreEqual(logic[i], ScalePresets.CoverSizeFor(_d, o, covers[i]).logicWidth, 1e-6f, covers[i].id);
                }
                finally { o.visualHeightFrac = keep; }
            }
        }

        [Test]
        public void CoverVisualHeights_AreProposalFractionsOfDrawnActorHeight()
        {
            var want = new Dictionary<CoverSizeType, float>
            {
                { CoverSizeType.HayBale, 0.45f }, { CoverSizeType.Barrel, 0.60f }, { CoverSizeType.Trough, 0.55f },
                { CoverSizeType.SwingDoor, 0.65f }, { CoverSizeType.Wagon, 0.90f }, { CoverSizeType.Rock, 0.85f },
                { CoverSizeType.Crate, 0.55f },
            };
            foreach (var kv in want) Assert.AreEqual(kv.Value, _d.CoverHeightOfActor(kv.Key), 1e-5f, kv.Key.ToString());
            foreach (var g in AssetDatabase.FindAssets("t:CoverData"))
            {
                var c = AssetDatabase.LoadAssetAtPath<CoverData>(AssetDatabase.GUIDToAssetPath(g));
                var t = _d.TypeOf(c.id);
                foreach (var o in _d.options)
                {
                    if (o.GetSize(t) == null) continue;
                    var s = ScalePresets.CoverSizeFor(_d, o, c);
                    Assert.AreEqual(want[t] * ScalePresets.VisualHeightWorldFor(_d, o), s.visualHeight, 1e-4f, $"opt{o.number} {c.id} drawn height");
                    Assert.LessOrEqual(s.visualHeight, ScalePresets.VisualHeightWorldFor(_d, o), "cover never taller than the actor: upper body stays visible");
                }
            }
        }

        [Test]
        public void Hitbox_SitsInsideSpriteTorso_ForEveryPreset()
        {
            var lib = AssetDatabase.LoadAssetAtPath<SpriteLibrary>("Assets/HillNGun/Data/SpriteLibrary.asset");
            Assert.IsNotNull(lib);
            var actors = new List<(string id, Sprite s, float hitR, float viewScale)>();
            foreach (var c in lib.characters) if (c?.idle != null) actors.Add((c.characterId, c.idle, 0.5f, 1f));
            foreach (var e in lib.enemies)
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/HillNGun/Data/Enemies/enemy_" + e.enemyId + ".asset");
                if (e?.idle != null && data != null) actors.Add((e.enemyId, e.idle, data.hitRadius, data.scale));
            }
            var rex = AssetDatabase.LoadAssetAtPath<BossData>("Assets/HillNGun/Data/Bosses/boss_rattlesnake_rex.asset");
            if (lib.boss?.idle != null) actors.Add(("boss", lib.boss.idle, rex.hitRadius, 2f));
            Assert.Greater(actors.Count, 4);
            foreach (var o in _d.options)
            {
                float gMul = ScalePresets.CharacterMulFor(_d, o), vMul = ScalePresets.VisualMulFor(_d, o);
                foreach (var a in actors)
                {
                    Assert.IsTrue(Opaque(a.s, out float bottom, out float top, out float width), a.id + " readable");
                    float k = a.viewScale * vMul;                     // FieldView.Place scale (perspective 1 = front row)
                    float foot = bottom * k, head = top * k, body = head - foot, r = a.hitR * gMul;
                    float lo = (-r - foot) / body, hi = (r - foot) / body; // hit circle centred on the logic position (sprite pivot)
                    Assert.GreaterOrEqual(lo, 0.15f, $"opt{o.number} {a.id}: hit circle dips below the hips ({lo:F2})");
                    Assert.LessOrEqual(hi, 0.85f, $"opt{o.number} {a.id}: hit circle reaches above the head ({hi:F2})");
                    Assert.LessOrEqual(2f * r, width * k, $"opt{o.number} {a.id}: hit circle wider than the drawn body");
                }
            }
        }

        private static bool Opaque(Sprite s, out float bottom, out float top, out float width)
        {
            bottom = top = width = 0f;
            var tex = s.texture; if (tex == null || !tex.isReadable) return false;
            Rect rr = s.textureRect;
            int w = (int)rr.width, h = (int)rr.height;
            var px = tex.GetPixels((int)rr.x, (int)rr.y, w, h);
            int minRow = -1, maxRow = -1, minX = w, maxX = -1;
            for (int y = 0; y < h; y++)
            {
                int cnt = 0;
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > 0.35f) { cnt++; if (x < minX) minX = x; if (x > maxX) maxX = x; }
                if (cnt > 2) { if (minRow < 0) minRow = y; maxRow = y; }
            }
            if (minRow < 0) return false;
            bottom = (minRow - s.pivot.y) / s.pixelsPerUnit;
            top = (maxRow - s.pivot.y) / s.pixelsPerUnit;
            width = (maxX - minX) / s.pixelsPerUnit;
            return true;
        }

        // ---------------------------------------------------------------- save migration

        [Test]
        public void SettingsMigration_StalePreset3_BecomesDefault2_OtherFieldsKept()
        {
            var json = "{\"version\":1,\"masterVolume\":0.3,\"language\":\"en\",\"tutorialDone\":true,\"buttonSizeIndex\":2,\"scalePreset\":3}";
            var d = JsonUtility.FromJson<SettingsData>(json);
            Assert.AreEqual(0, d.scalePresetSchema, "old JSON has no schema -> 0");
            Assert.IsTrue(SettingsService.MigrateScalePreset(d));
            Assert.AreEqual(0, d.scalePreset, "0 = data default (option 2)");
            Assert.AreEqual(SettingsService.CurrentScalePresetSchema, d.scalePresetSchema);
            Assert.AreEqual(0.3f, d.masterVolume, 1e-6f); Assert.AreEqual("en", d.language);
            Assert.IsTrue(d.tutorialDone); Assert.AreEqual(2, d.buttonSizeIndex);
            Assert.IsFalse(SettingsService.MigrateScalePreset(d), "runs once");
            d.scalePreset = 1; // a choice made after migration is kept
            Assert.IsFalse(SettingsService.MigrateScalePreset(d));
            Assert.AreEqual(1, d.scalePreset);
        }
    }
}

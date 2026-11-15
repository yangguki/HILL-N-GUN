using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Combat;
using HillNGun.Data;

namespace HillNGun.Tests.EditMode
{
    /// <summary>v14: logical projectile muzzle is fixed logic (never rig bones/views) and render code cannot consume gameplay RNG.</summary>
    public class LogicIndependenceTests
    {
        private static string Src(string rel) => File.ReadAllText(Path.Combine(Application.dataPath, "HillNGun/Scripts/Runtime", rel));

        [Test]
        public void LogicalMuzzle_IsPositionPlusAimTimesWeaponOffset_IndependentOfAnyTransformOrBone()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.muzzleForwardOffset = 0.85f;
            var pos = new Vector2(3.25f, -1.5f); var dir = new Vector2(0.6f, 0.8f);
            var before = CombatSystem.LogicalMuzzle(pos, dir, w);
            // Pose a fake bone hierarchy arbitrarily: the logical muzzle must not change.
            var root = new GameObject("rig_root"); var hand = new GameObject("forearm_r"); hand.transform.SetParent(root.transform);
            root.transform.SetPositionAndRotation(new Vector3(40f, 9f, 0f), Quaternion.Euler(0, 0, 77f)); hand.transform.localRotation = Quaternion.Euler(0, 0, -120f);
            var after = CombatSystem.LogicalMuzzle(pos, dir, w);
            Assert.AreEqual(pos + dir * 0.85f, before);
            Assert.AreEqual(before, after);
            Object.DestroyImmediate(root); Object.DestroyImmediate(w);
        }

        [Test]
        public void CombatFirePath_UsesLogicalMuzzle_AndCombatCodeNeverReadsRigOrViews()
        {
            string cs = Src("Combat/CombatSystem.cs");
            StringAssert.Contains("Vector2 muzzlePos = LogicalMuzzle(character.Position, direction, weapon);", cs);
            foreach (var f in new[] { "Combat/CombatSystem.cs", "Combat/ProjectilePool.cs", "Combat/Projectile.cs" })
            {
                string s = Src(f);
                foreach (var banned in new[] { "HillNGun.Rig", "CutoutRig", "SpriteSkin", "boneTransforms", "GetMuzzleWorld" })
                    Assert.IsFalse(s.Contains(banned), $"{f} must not depend on rig/view ({banned})");
            }
        }

        [Test]
        public void GameplayRng_SameSeedSameSequence_AndUnityRandomConsumptionDoesNotShiftIt()
        {
            GameplayRng.Seed(12345); var a = new float[64]; for (int i = 0; i < a.Length; i++) a[i] = GameplayRng.Value;
            GameplayRng.Seed(12345); var b = new float[64];
            for (int i = 0; i < b.Length; i++) { for (int k = 0; k < 7; k++) Random.Range(0f, 1f); b[i] = GameplayRng.Value; } // "render/FX" draws in between
            CollectionAssert.AreEqual(a, b);
            foreach (var v in a) Assert.That(v, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        }

        [Test]
        public void Hitstop_UnderFixedStepCapture_UsesFrameClock_NotWallClock()
        {
            string s = Src("Core/GameTime.cs");
            StringAssert.Contains("fixedStep ? Time.frameCount * Time.captureDeltaTime : Time.unscaledTime", s);
            Assert.IsFalse(Regex.IsMatch(s, @"<\s*_hitstopEndTime") && s.Contains("Time.unscaledTime < _hitstopEndTime"), "hitstop compare must go through HitstopClock");
        }

        [TestCase("AI/BotBrain.cs")]
        [TestCase("Combat/DropSystem.cs")]
        [TestCase("Combat/DropDirector.cs")]
        [TestCase("Combat/BulletDodgeSense.cs")]
        public void GameplayLogic_DoesNotUseSharedUnityRandom(string file)
        {
            string s = Regex.Replace(Src(file), @"//.*", "");
            Assert.IsFalse(Regex.IsMatch(s, @"(?<![A-Za-z])Random\.(value|Range)"), $"{file} must use GameplayRng, not the UnityEngine.Random stream shared with VFX");
        }
    }
}

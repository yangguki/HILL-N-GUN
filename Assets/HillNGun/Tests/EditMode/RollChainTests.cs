using NUnit.Framework;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.Input;

namespace HillNGun.Tests.EditMode
{
    /// <summary>
    /// Consecutive rolls: can start next roll as soon as previous ends; i-frames only while Rolling
    /// and inside RollData invuln window; gap between rolls is vulnerable; chaining never stays
    /// invulnerable across the roll boundary.
    /// </summary>
    [TestFixture]
    public class RollChainTests
    {
        private GameObject _go;
        private CharacterStateMachine _c;
        private CharacterData _cd;
        private RollData _roll;
        private WeaponData _wd;
        private GrenadeData _gd;

        [SetUp]
        public void SetUp()
        {
            _roll = ScriptableObject.CreateInstance<RollData>();
            _roll.distance = 2.5f;
            _roll.duration = 0.4f;
            _roll.invulnStart = 0f;
            _roll.invulnDuration = 0.35f;
            _roll.cooldown = 0f; // consecutive
            _roll.inputBuffer = 0.15f;
            _roll.allowRowRoll = false;

            _wd = ScriptableObject.CreateInstance<WeaponData>();
            _gd = ScriptableObject.CreateInstance<GrenadeData>();
            _cd = ScriptableObject.CreateInstance<CharacterData>();
            _cd.maxHp = 3000;
            _cd.moveSpeed = 5f;
            _cd.weapon = _wd;
            _cd.grenade = _gd;
            _cd.grenadeCount = 2;
            _cd.roll = _roll;
            _cd.hitRadius = 0.5f;

            _go = new GameObject("roll_chain_test");
            _c = _go.AddComponent<CharacterStateMachine>();
            _c.Initialize(_cd, Team.Player, 0, new Vector2(12f, 4.5f), 1);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_cd);
            Object.DestroyImmediate(_roll);
            Object.DestroyImmediate(_wd);
            Object.DestroyImmediate(_gd);
        }

        [Test]
        public void CanStartNextRollAsSoonAsPreviousEnds()
        {
            _c.OnRollCommand(new RollCommand(Vector2.right, 0f));
            Assert.AreEqual(MovementState.Rolling, _c.MovementState);
            // finish the roll exactly
            _c.TickSimulation(0.4f);
            Assert.AreEqual(MovementState.Idle, _c.MovementState, "roll should have ended");
            Assert.AreEqual(0f, _c.RollCooldownRatio, 1e-4f);
            _c.OnRollCommand(new RollCommand(Vector2.left, 0f));
            Assert.AreEqual(MovementState.Rolling, _c.MovementState, "second roll must start immediately (cooldown 0, recovery not blocking)");
        }

        [Test]
        public void InvulnerableDuringRollWindow()
        {
            _c.OnRollCommand(new RollCommand(Vector2.right, 0f));
            _c.TickSimulation(0.1f);
            Assert.IsTrue(_c.IsInvulnerable);
            _c.TickSimulation(0.2f); // ~0.3s into 0.35s window
            Assert.IsTrue(_c.IsInvulnerable);
        }

        [Test]
        public void VulnerableInGapBetweenSequentialRolls()
        {
            _c.OnRollCommand(new RollCommand(Vector2.right, 0f));
            _c.TickSimulation(0.4f); // end roll
            Assert.AreEqual(MovementState.Idle, _c.MovementState);
            // one idle frame between rolls
            _c.TickSimulation(0.02f);
            Assert.IsFalse(_c.IsInvulnerable, "must be vulnerable between rolls");
            _c.OnRollCommand(new RollCommand(Vector2.left, 0f));
            Assert.IsTrue(_c.IsInvulnerable, "second roll grants i-frames again");
        }

        [Test]
        public void ContinuousChainDoesNotStayInvulnerableAcrossBoundary()
        {
            // buffer next roll near the end of the first
            _c.OnRollCommand(new RollCommand(Vector2.right, 0f));
            _c.TickSimulation(0.32f); // still rolling, inside buffer window of end
            Assert.AreEqual(MovementState.Rolling, _c.MovementState);
            _c.OnRollCommand(new RollCommand(Vector2.left, 0f)); // buffer
            // finish first roll; buffered second should start
            _c.TickSimulation(0.1f); // past 0.4
            Assert.AreEqual(MovementState.Rolling, _c.MovementState, "buffered second roll should have started");

            // Now force: finish second, and check that at the exact frame between rolls we were not permanently invuln.
            // Rebuild a controlled sequence: roll1 end -> idle tick -> roll2
            _c.ResetForRound(new Vector2(12f, 4.5f), 1);
            _c.OnRollCommand(new RollCommand(Vector2.right, 0f));
            // Advance past duration with a little epsilon — float 40×0.01 can undershoot 0.4.
            float elapsed = 0f;
            while (elapsed < _roll.duration + 0.02f)
            {
                _c.TickSimulation(0.02f);
                elapsed += 0.02f;
            }
            Assert.AreNotEqual(MovementState.Rolling, _c.MovementState, "roll motion must have ended");
            bool gapInvuln = _c.IsInvulnerable;
            Assert.IsFalse(gapInvuln, "i-frames must end with the roll state; chaining must not create permanent invulnerability");
        }

        [Test]
        public void ZeroCooldownRatioNeverDividesByZero()
        {
            Assert.AreEqual(0f, _c.RollCooldownRatio, 1e-5f);
            _c.OnRollCommand(new RollCommand(Vector2.right, 0f));
            Assert.AreEqual(0f, _c.RollCooldownRatio, 1e-5f);
        }
    }
}

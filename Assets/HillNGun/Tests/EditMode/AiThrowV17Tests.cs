using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HillNGun.AI;
using HillNGun.Characters;
using HillNGun.Combat;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Input;
using HillNGun.PvE;
using HillNGun.Rig;

namespace HillNGun.Tests.EditMode
{
    /// <summary>v17: AI-only 0.15 s grenade anticipation (common CSM path), release visual bridge, throw soft clamp,
    /// gun holstered during the throw, PvE dynamite telegraph unchanged.</summary>
    [TestFixture]
    public class AiThrowV17Tests
    {
        private readonly List<Object> _objs = new List<Object>();
        [TearDown] public void TearDown() { foreach (var o in _objs) if (o != null) Object.DestroyImmediate(o); _objs.Clear(); AbilityPilot.DiagHoldActor = null; }
        private T So<T>() where T : ScriptableObject { var o = ScriptableObject.CreateInstance<T>(); _objs.Add(o); return o; }

        private CharacterStateMachine Csm(Team team = Team.Player)
        {
            var gd = So<GrenadeData>(); var wd = So<WeaponData>(); var rd = So<RollData>(); var cd = So<CharacterData>();
            cd.maxHp = 3000; cd.moveSpeed = 5f; cd.weapon = wd; cd.grenade = gd; cd.grenadeCount = 2; cd.roll = rd; cd.hitRadius = 0.5f;
            var go = new GameObject("csm_v17"); _objs.Add(go);
            var c = go.AddComponent<CharacterStateMachine>(); c.Initialize(cd, team, 0, new Vector2(12f, 4.5f), 1);
            return c;
        }
        private static void Sim(CharacterStateMachine c, float secs) { for (float t = 0f; t < secs - 1e-5f; t += 1f / 60f) c.TickSimulation(1f / 60f); }

        [Test]
        public void AiWindup_DataDriven_015()
        {
            Assert.AreEqual(0.15f, So<BotBehaviorData>().aiGrenadeWindup, 1e-4f);
            var b = RuntimeAssetManagerProbe();
            if (b != null) Assert.AreEqual(0.15f, b.aiGrenadeWindup, 1e-4f, "shipped behaviour asset");
        }
        private static BotBehaviorData RuntimeAssetManagerProbe()
        {
            foreach (var g in AssetDatabase.FindAssets("t:BotBehaviorData")) { var b = AssetDatabase.LoadAssetAtPath<BotBehaviorData>(AssetDatabase.GUIDToAssetPath(g)); if (b != null) return b; }
            return null;
        }

        [Test]
        public void AiWindup_NoSpawnBefore015_ExactlyOnceAfter_NoSecond()
        {
            var c = Csm(); int g0 = c.GrenadeSystem.CurrentGrenades;
            Assert.IsTrue(c.BeginAiGrenadeThrow(new Vector2(18f, 4.5f), 0.15f));
            Assert.AreEqual(ActionState.GrenadeAiming, c.ActionState); Assert.IsTrue(c.AiGrenadeWindingUp);
            Sim(c, 0.1333f);
            Assert.AreEqual(0, c.GrenadesReleased, "no spawn during the windup"); Assert.AreEqual(g0, c.GrenadeSystem.CurrentGrenades);
            Sim(c, 0.05f);
            Assert.AreEqual(1, c.GrenadesReleased, "exactly one release after 0.15 s"); Assert.IsFalse(c.AiGrenadeWindingUp); Assert.AreEqual(ActionState.None, c.ActionState);
            Assert.AreEqual(g0 - 1, c.GrenadeSystem.CurrentGrenades);
            Sim(c, 1f);
            Assert.AreEqual(1, c.GrenadesReleased, "no second / late release");
        }

        [Test]
        public void AiWindup_ZeroDuration_KeepsOldImmediateRelease()
        {
            var c = Csm(); Assert.IsTrue(c.BeginAiGrenadeThrow(new Vector2(18f, 4.5f), 0f));
            Assert.AreEqual(1, c.GrenadesReleased);
        }

        [Test]
        public void AiWindup_AimLocked_WhenTargetMoves()
        {
            var c = Csm(); var target = new Vector2(18f, 4.5f);
            c.BeginAiGrenadeThrow(target, 0.15f);
            // the AI's target moves / drag commands arrive during the windup: the throw still goes to the locked point
            for (int i = 0; i < 12; i++) { c.OnGrenadeAimDragCommand(new GrenadeAimDragCommand(new Vector2(-1f, 1f), 6f, 0f)); c.TickSimulation(1f / 60f); }
            Assert.AreEqual(1, c.GrenadesReleased);
            Assert.Less(Vector2.Distance(target, c.LastGrenadeReleaseTarget), 1e-3f, "released at the decision-time target");
        }

        [Test]
        public void AiWindup_CancelOnHit_NoSpawn_GrenadeKept_NoInstantRetry()
        {
            var c = Csm(); int g0 = c.GrenadeSystem.CurrentGrenades;
            c.BeginAiGrenadeThrow(new Vector2(18f, 4.5f), 0.15f); Sim(c, 0.05f);
            c.TakeDamage(10, Vector2.left);
            Assert.IsFalse(c.AiGrenadeWindingUp); Assert.AreEqual("hit", c.LastAiGrenadeCancel); Assert.AreEqual(1, c.AiGrenadeCancels);
            Assert.AreEqual(ActionState.None, c.ActionState, "not stuck aiming");
            Sim(c, 0.5f);
            Assert.AreEqual(0, c.GrenadesReleased, "cancelled throw never spawns"); Assert.AreEqual(g0, c.GrenadeSystem.CurrentGrenades, "grenade not consumed");
            Assert.IsTrue(c.HasGrenadeReady, "no infinite lock-out");
        }

        [Test]
        public void AiWindup_CancelOnDowned_And_Roll()
        {
            var c = Csm(); c.BeginAiGrenadeThrow(new Vector2(18f, 4.5f), 0.15f); Sim(c, 0.05f);
            c.TakeDamage(999999, Vector2.left);
            Assert.AreEqual(CharacterOverallState.Downed, c.OverallState); Assert.IsFalse(c.AiGrenadeWindingUp);
            Sim(c, 0.5f); Assert.AreEqual(0, c.GrenadesReleased);

            var r = Csm(); r.BeginAiGrenadeThrow(new Vector2(18f, 4.5f), 0.15f); Sim(r, 0.05f);
            r.OnRollCommand(new RollCommand(Vector2.left, 0f)); Sim(r, 0.5f);
            Assert.AreEqual(0, r.GrenadesReleased, "roll interrupts the windup"); Assert.AreEqual(1, r.AiGrenadeCancels);
        }

        [Test]
        public void AiWindup_ZeroDamageHit_DoesNotCancel()
        {
            var c = Csm(); c.BeginAiGrenadeThrow(new Vector2(18f, 4.5f), 0.15f); Sim(c, 0.05f);
            c.TakeDamage(0, Vector2.left); Sim(c, 0.2f);
            Assert.AreEqual(1, c.GrenadesReleased);
        }

        [Test]
        public void AiWindup_FailsCleanly_WhenBusy_NotStuck()
        {
            var c = Csm(); c.OnRollCommand(new RollCommand(Vector2.left, 0f)); c.TickSimulation(1f / 60f);
            bool began = c.BeginAiGrenadeThrow(new Vector2(18f, 4.5f), 0.15f);
            if (!began) { Assert.IsFalse(c.AiGrenadeWindingUp); Sim(c, 1.5f); Assert.AreEqual(0, c.GrenadesReleased, "no buffered grenade fires later"); Assert.AreNotEqual(ActionState.GrenadeAiming, c.ActionState); }
        }

        [Test]
        public void PlayerRelease_StillImmediate_NoWindup()
        {
            var c = Csm(); c.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, 0f));
            Assert.IsFalse(c.AiGrenadeWindingUp);
            c.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(new Vector2(18f, 4.5f), true, 0f));
            Assert.AreEqual(1, c.GrenadesReleased, "player command releases on the command");
            c.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, 0f)); c.TakeDamage(10, Vector2.left);
            Assert.AreEqual(0, c.AiGrenadeCancels, "player aim not touched by the AI cancel rule");
        }

        [Test]
        public void PveAlly_AbilityPilot_UsesWindup_TimerSetAtDecision()
        {
            var c = Csm(); var b = So<BotBehaviorData>(); b.useGrenades = true; b.aiGrenadeWindup = 0.15f;
            var p = new AbilityPilot(c, b, 0f);
            var team = new List<CharacterStateMachine> { c };
            Vector2 tgt = c.Position + new Vector2(5f, 0f);
            p.Tick(1f / 60f, tgt, team);
            Assert.AreEqual(1, p.GrenadesThrown); Assert.IsTrue(c.AiGrenadeWindingUp, "pilot decision starts the windup"); Assert.AreEqual(0, c.GrenadesReleased);
            for (int i = 0; i < 12; i++) { c.TickSimulation(1f / 60f); p.Tick(1f / 60f, tgt + new Vector2(0f, i * 0.2f), team); }
            Assert.AreEqual(1, c.GrenadesReleased); Assert.AreEqual(1, p.GrenadesThrown, "no extra decision inside the interval");
            Assert.Less(Vector2.Distance(tgt, c.LastGrenadeReleaseTarget), 1e-3f);
            // cancel -> the pilot does NOT retry immediately (interval timer was set at the decision)
            var c2 = Csm(); var p2 = new AbilityPilot(c2, b, 0f);
            p2.Tick(1f / 60f, tgt, new List<CharacterStateMachine> { c2 }); c2.TakeDamage(5, Vector2.left);
            for (int i = 0; i < 30; i++) { c2.TickSimulation(1f / 60f); p2.Tick(1f / 60f, tgt, new List<CharacterStateMachine> { c2 }); }
            Assert.AreEqual(1, p2.GrenadesThrown, "no instant retry"); Assert.AreEqual(0, c2.GrenadesReleased); Assert.IsTrue(c2.HasGrenadeReady);
        }

        [Test]
        public void DiagHold_OnlyAffectsTheFlaggedActor()
        {
            var c = Csm(); var b = So<BotBehaviorData>(); b.useGrenades = true;
            var p = new AbilityPilot(c, b, 0f); AbilityPilot.DiagHoldActor = c;
            p.Tick(1f / 60f, c.Position + new Vector2(5f, 0f), new List<CharacterStateMachine> { c });
            Assert.AreEqual(0, p.GrenadesThrown); AbilityPilot.DiagHoldActor = null;
            p.Tick(1f / 60f, c.Position + new Vector2(5f, 0f), new List<CharacterStateMachine> { c });
            Assert.AreEqual(1, p.GrenadesThrown);
        }

        [Test]
        public void PvpBot_BotBrain_UsesWindup_CooldownAtDecision()
        {
            var me = Csm(Team.Player); var foe = Csm(Team.Enemy);
            var b = So<BotBehaviorData>(); b.useGrenades = true; b.aiGrenadeWindup = 0.15f;
            var brain = new BotBrain(me, b);
            var T = typeof(BotBrain); var bf = BindingFlags.NonPublic | BindingFlags.Instance;
            T.GetField("_currentTarget", bf).SetValue(brain, foe);
            T.GetMethod("ExecuteGrenade", bf).Invoke(brain, null);
            Assert.IsTrue(me.AiGrenadeWindingUp); Assert.AreEqual(0, me.GrenadesReleased);
            Assert.Greater((float)T.GetField("_grenadeCooldownTimer", bf).GetValue(brain), 1f, "cooldown set at the decision");
            Vector2 locked = foe.Position;
            Sim(me, 0.2f);
            Assert.AreEqual(1, me.GrenadesReleased); Assert.Less(Vector2.Distance(locked, me.LastGrenadeReleaseTarget), 1e-3f);
        }

        [Test]
        public void PveDynamite_ExistingTelegraph_EntranceImmune_OneExplosivePerWindup_NoExtraWindow()
        {
            var bandit = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/HillNGun/Data/Enemies/enemy_dynamite_bandit.asset");
            var layout = AssetDatabase.LoadAssetAtPath<StreetLayoutData>("Assets/HillNGun/Data/Layout/street_layout_dust.asset");
            Assert.IsTrue(bandit.throwsExplosive); Assert.AreEqual(1f, bandit.warningTime, 1e-3f, "telegraph unchanged (no stacked v17 window)");
            var sp = layout.Find("door_L"); var e = new EnemyInstance();
            e.InitializeFree(bandit, sp, layout.IndexOf("door_L"), 1f, 0.8f, new System.Random(1));
            float t = 0f, wind = -1f; var throws = new List<float>(); var ctx = new FreeCtx { player = sp.position + new Vector2(2f, -4f) };
            e.OnExplosiveThrown += (en, p, d) => throws.Add(t);
            float entrance = sp.telegraphTime + sp.entranceTime; bool hitDuring = false;
            for (int i = 0; i < 60 * 14; i++)
            {
                e.UpdateFree(1f / 60f, ctx); t += 1f / 60f; ctx.now = t;
                if (t < entrance - 0.02f && e.CanBeHit) hitDuring = true;
                if (wind < 0f && e.IsWindingUp) wind = t;
            }
            Assert.IsFalse(hitDuring, "entrance immunity kept");
            Assert.GreaterOrEqual(throws.Count, 2, "token released -> it attacks again");
            Assert.AreEqual(bandit.warningTime, throws[0] - wind, 2.5f / 60f, "explosive exactly at the end of the existing warning");
            for (int k = 1; k < throws.Count; k++) Assert.Greater(throws[k] - throws[k - 1], bandit.attackInterval * 0.5f, "no burst");
        }
        private class FreeCtx : IEnemyFreeContext
        {
            public Vector2 player; public FireTokenScheduler fire = new FireTokenScheduler(3); public float now;
            public Vector2 NearestPlayerPos(Vector2 from) => player;
            public Vector2 MoveEnemy(EnemyInstance e, Vector2 delta) => e.Position + delta;
            public Vector2 PickPost(EnemyInstance e) => e.Position;
            public bool TryAcquireFire(EnemyInstance e) => fire.TryAcquire(e, now);
            public void ReleaseFire(EnemyInstance e) => fire.Release(e);
        }

        [Test]
        public void SoftLimit_Continuous_Monotonic_NeverExceeds()
        {
            float prev = -999f;
            for (float x = 0f; x <= 220f; x += 0.5f)
            {
                float y = CutoutRig.SoftLimit(x, 160f, 15f);
                Assert.LessOrEqual(y, 160.001f); Assert.GreaterOrEqual(y, prev - 1e-4f, "monotonic");
                if (prev > -999f) Assert.Less(y - prev, 0.5001f, "no jump");
                if (x <= 145f) Assert.AreEqual(x, y, 1e-3f, "untouched below the band");
                prev = y;
            }
            Assert.AreEqual(-140f, CutoutRig.SoftLimit(-140f, 160f, 15f), 1e-3f);
            Assert.Greater(CutoutRig.SoftLimit(-175f, 160f, 15f), -160.001f, "symmetric");
        }

        [Test]
        public void Bridge_DecaysToLogicPosition_In006()
        {
            var off = new Vector3(-0.5f, 0.4f, 0f);
            Assert.AreEqual(off, GrenadeView.BridgeOffset(off, 0f));
            float m1 = GrenadeView.BridgeOffset(off, 0.02f).magnitude, m2 = GrenadeView.BridgeOffset(off, 0.04f).magnitude;
            Assert.Less(m1, off.magnitude); Assert.Less(m2, m1);
            Assert.AreEqual(Vector3.zero, GrenadeView.BridgeOffset(off, 0.06f)); Assert.AreEqual(Vector3.zero, GrenadeView.BridgeOffset(off, 1f));
        }

        [TestCase("buster")] [TestCase("raven")] [TestCase("volt")]
        public void Throw_NearArmRaised_GunHolstered_ThenRestored(string id)
        {
            var go = new GameObject("rig_v17"); _objs.Add(go);
            var r = new CutoutRig(go.transform, RigDefinition.Load(id), -0.85f, 2.4f);
            RigInput I() => new RigInput { aim = Vector2.right, aiming = true, maxSpeed = 5f, facing = 1f, forceSide = true, sortingOrder = 500, throwCountValid = true };
            void Run(RigInput i, float s) { for (float t = 0f; t < s; t += 1f / 60f) r.Tick(1f / 60f, i); }
            Run(I(), 0.5f);
            Assert.IsFalse(r.WeaponHolstered, "gun visible while aiming");
            Vector3 sh = r.Bone("side", "upper_arm_r").position; float handAim = r.Bone("side", "hand_r").position.y;
            var w = I(); w.aiming = false; w.grenadeAiming = true; Run(w, 0.3f);
            Assert.IsTrue(r.WeaponHolstered, "gun off the throwing hand during the throw (view only)");
            bool twoHanded = id != "volt";
            Assert.AreEqual(twoHanded, r.WeaponRendered, twoHanded ? "rifle stays visible, lowered in the off hand" : "pistol hidden while the gun hand throws");
            if (twoHanded) Assert.Less(r.IkError, 0.05f, "off hand holds the lowered gun (support IK on during the throw)");
            Assert.Greater(r.Bone("side", "hand_r").position.y, handAim + 0.3f, $"{id}: the rendered NEAR arm is raised (not left in the aim pose)");
            Assert.IsTrue(r.HeldBombVisible);
            var rel = I(); rel.throwCount = 1; Run(rel, 0.8f);
            Assert.IsFalse(r.WeaponHolstered, "gun back after the arc"); Assert.IsTrue(r.WeaponRendered);
        }
    }
}

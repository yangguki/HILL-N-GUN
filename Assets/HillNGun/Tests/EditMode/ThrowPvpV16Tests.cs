using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HillNGun.Characters;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Input;
using HillNGun.PvE;
using HillNGun.Rig;

namespace HillNGun.Tests.EditMode
{
    /// <summary>v16: throw visual (pull-back, throw-only 160 deg arm, held bomb hand-off at the LOGIC release) and PvP static doorway kit.</summary>
    [TestFixture]
    public class ThrowPvpV16Tests
    {
        private GameObject _go;
        [TearDown] public void TearDown() { if (_go != null) Object.DestroyImmediate(_go); }

        private CutoutRig Make(string id)
        {
            _go = new GameObject("rig_v16");
            return new CutoutRig(_go.transform, RigDefinition.Load(id), -0.85f, 2.4f);
        }
        private static RigInput Inp() => new RigInput { aim = Vector2.right, aiming = false, maxSpeed = 5f, facing = 1f, forceSide = true, sortingOrder = 500, throwCountValid = true };
        private static void Run(CutoutRig r, RigInput i, float secs) { for (float t = 0f; t < secs; t += 1f / 60f) r.Tick(1f / 60f, i); }
        private static float Arm(CutoutRig r, float rest) => Mathf.Abs(Mathf.DeltaAngle(rest, r.Bone("side", "upper_arm_r").localEulerAngles.z));

        [TestCase("buster")] [TestCase("raven")] [TestCase("volt")]
        public void Throw_ArmLimit160_OnlyWhileThrowing_NormalLimitsUnchanged(string id)
        {
            var def = RigDefinition.Load(id); var st = RigClipSet.Shared.settings;
            Assert.AreEqual(160f, st.throwArmLimitDeg, 1e-3f); Assert.AreEqual(0.15f, st.throwPullback, 1e-4f);
            float normal = RigDefinition.Limit(def.limits, "upper_arm_r", 999f); // v17: the throw uses the NEAR (gun) arm
            Assert.Less(normal, 100.01f, "normal-pose gun arm limit unchanged (data)");
            var r = Make(id); var idle = Inp(); Run(r, idle, 0.5f);
            Assert.AreEqual("side", r.ViewName);
            float rest = r.Bone("side", "upper_arm_r").localEulerAngles.z;
            var aim = Inp(); aim.aiming = true; Run(r, aim, 0.8f);
            Assert.LessOrEqual(Arm(r, rest), normal + 0.5f, "normal aiming pose still within the normal limit");
            var w = Inp(); w.grenadeAiming = true; Run(r, w, 0.3f);
            float pulled = Arm(r, rest);
            Assert.Greater(pulled, 120f, $"{id}: throw pull-back beyond the old 100 deg"); Assert.LessOrEqual(pulled, 160.5f);
            var rel = Inp(); rel.throwCount = 1; Run(r, rel, 0.8f);
            Assert.LessOrEqual(Arm(r, rest), normal + 0.5f, "after the throw the normal limit applies again");
        }

        [TestCase("buster")] [TestCase("raven")] [TestCase("volt")]
        public void Throw_HeldBomb_PullbackVisible_HandOffOnceAtLogicRelease_NoRetrigger(string id)
        {
            var r = Make(id); Run(r, Inp(), 0.3f);
            int arcs0 = CutoutRig.ThrowArcs, hand0 = CutoutRig.BombHandoffs;
            var w = Inp(); w.grenadeAiming = true;
            Run(r, w, 0.05f); Assert.Less(r.PullbackWeight, 0.6f, "0.15 s pull-back, not instant"); Assert.IsTrue(r.HeldBombVisible, "bomb in hand during pull-back");
            Run(r, w, 0.15f); Assert.AreEqual(1f, r.PullbackWeight, 1e-3f); Assert.IsTrue(r.HeldBombVisible);
            var hb = r.Bone("side", "hand_r").Find("held_bomb"); Assert.IsNotNull(hb, "bomb parented to the visual hand");
            // logic release frame: count changes, aim state ends the same frame
            var rel = Inp(); rel.throwCount = 1; r.Tick(1f / 60f, rel);
            Assert.IsFalse(r.HeldBombVisible, "hand-off on the release frame"); Assert.AreEqual(arcs0 + 1, CutoutRig.ThrowArcs); Assert.AreEqual(hand0 + 1, CutoutRig.BombHandoffs);
            Assert.Less(r.ThrowArcTime, CutoutRig.ThrowDur);
            Run(r, rel, 1.0f);
            Assert.AreEqual(arcs0 + 1, CutoutRig.ThrowArcs, "no retrigger"); Assert.IsFalse(r.HeldBombVisible); Assert.GreaterOrEqual(r.ThrowArcTime, CutoutRig.ThrowDur, "arc completes");
        }

        [TestCase("buster")] [TestCase("volt")]
        public void Throw_HeldBomb_HiddenOnRollCancel_Death_Deadzone_NoArc(string id)
        {
            var r = Make(id); Run(r, Inp(), 0.3f); int arcs0 = CutoutRig.ThrowArcs;
            var w = Inp(); w.grenadeAiming = true; Run(r, w, 0.3f); Assert.IsTrue(r.HeldBombVisible);
            var roll = w; roll.rolling = true; roll.rollProgress = 0.2f; r.Tick(1f / 60f, roll);
            Assert.IsFalse(r.HeldBombVisible, "roll cancel hides the bomb");
            Run(r, Inp(), 0.5f);
            Run(r, w, 0.3f); Assert.IsTrue(r.HeldBombVisible);
            var dead = w; dead.downed = true; r.Tick(1f / 60f, dead); Assert.IsFalse(r.HeldBombVisible, "death hides the bomb");
            Run(r, Inp(), 0.5f);
            Run(r, w, 0.3f); Run(r, Inp(), 0.5f); // deadzone cancel: aim ends, count unchanged
            Assert.IsFalse(r.HeldBombVisible); Assert.AreEqual(arcs0, CutoutRig.ThrowArcs, "no arc without a logic release");
        }

        [TestCase("buster")] [TestCase("raven")] [TestCase("volt")]
        public void Throw_Overhand_PullbackBehindHead_ArcOverTheTop_ThenFront(string id)
        {
            var r = Make(id); Run(r, Inp(), 0.5f);
            Vector3 Hp(string b) => r.Bone("side", b).position;
            var w = Inp(); w.grenadeAiming = true; Run(r, w, 0.3f);
            Vector3 sh = Hp("upper_arm_r"), hand = Hp("hand_r"), head = Hp("head");
            Assert.Greater(hand.y, head.y, $"{id}: pull-back hand above the head"); Assert.Less(hand.x, sh.x, "behind the shoulder (facing right)");
            var rel = Inp(); rel.throwCount = 1; float maxY = -9f, minYBeforeFront = 9f; bool front = false;
            for (int k = 0; k < 20; k++)
            {
                r.Tick(1f / 60f, rel); var h = Hp("hand_r");
                if (!front) { maxY = Mathf.Max(maxY, h.y); minYBeforeFront = Mathf.Min(minYBeforeFront, h.y); if (h.x > sh.x + 0.2f) front = true; }
            }
            Assert.IsTrue(front, "arc ends in front");
            Assert.Greater(minYBeforeFront, sh.y, $"{id}: over the top (hand never drops below the shoulder behind the body = no underhand swing)");
        }

        [Test]
        public void Throw_SameFrameAiRelease_OneArc_NoPullback_NoHeldBomb()
        {
            var r = Make("raven"); Run(r, Inp(), 0.3f); int arcs0 = CutoutRig.ThrowArcs;
            var rel = Inp(); rel.throwCount = 1; r.Tick(1f / 60f, rel);   // AI: AimStart + Release in the same frame -> never GrenadeAiming in a view tick
            Assert.AreEqual(arcs0 + 1, CutoutRig.ThrowArcs); Assert.IsFalse(r.HeldBombVisible);
            Assert.Greater(r.ThrowArcTime, 0f, "instant release: forward part of the arc only (no visual delay of the logic)");
            Run(r, rel, 0.6f); Assert.AreEqual(arcs0 + 1, CutoutRig.ThrowArcs);
        }

        [Test]
        public void Throw_LogicRelease_Unchanged_ImmediateSingleSpawn()
        {
            var gd = ScriptableObject.CreateInstance<GrenadeData>(); var wd = ScriptableObject.CreateInstance<WeaponData>(); var rd = ScriptableObject.CreateInstance<RollData>();
            var cd = ScriptableObject.CreateInstance<CharacterData>(); cd.maxHp = 3000; cd.moveSpeed = 5f; cd.weapon = wd; cd.grenade = gd; cd.grenadeCount = 2; cd.roll = rd; cd.hitRadius = 0.5f;
            var go = new GameObject("csm_v16");
            try
            {
                var c = go.AddComponent<CharacterStateMachine>(); c.Initialize(cd, Team.Player, 0, new Vector2(12f, 4.5f), 1);
                c.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, 0f));
                Assert.AreEqual(ActionState.GrenadeAiming, c.ActionState); Assert.AreEqual(0, c.GrenadesReleased);
                c.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(new Vector2(18f, 4.5f), true, 0f));
                Assert.AreEqual(1, c.GrenadesReleased, "released on the command itself (no delay)"); Assert.AreEqual(ActionState.None, c.ActionState);
                c.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(new Vector2(18f, 4.5f), true, 0f));
                Assert.AreEqual(1, c.GrenadesReleased, "no extra / late release");
                c.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, 0f));
                c.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(new Vector2(18f, 4.5f), false, 0f));
                Assert.AreEqual(1, c.GrenadesReleased, "deadzone cancel: no release");
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(cd); Object.DestroyImmediate(gd); Object.DestroyImmediate(wd); Object.DestroyImmediate(rd); }
        }

        [Test]
        public void Pvp_StaticClosedDoors_All4Tod_Background_SelectedTodPreserved()
        {
            var off = new System.Collections.Generic.List<Camera>();
            foreach (var cc in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (cc.enabled) { cc.enabled = false; off.Add(cc); }
            var cam = new GameObject("TestMainCam") { tag = "MainCamera" }; var cm = cam.AddComponent<Camera>(); cm.orthographic = true; cm.orthographicSize = 10f; cam.transform.position = new Vector3(0f, 0f, -10f);
            var layout = AssetDatabase.LoadAssetAtPath<StreetLayoutData>("Assets/HillNGun/Data/Layout/street_layout_dust.asset");
            var vis = AssetDatabase.LoadAssetAtPath<StageVisualData>("Assets/HillNGun/Data/Visuals/visual_dust_1_1_noon.asset");
            var pgo = new GameObject("ParallaxBackdrop"); var pb = pgo.AddComponent<HillNGun.Scenery.ParallaxBackdrop>();
            try
            {
                Assert.AreEqual(TimeOfDay.Noon, HillNGun.Battlefield.BattlefieldRenderer.PvpDiagTod(TimeOfDay.Noon), "no -pvp-tod: match TOD kept");
                Assert.AreEqual(TimeOfDay.Noon, vis.timeOfDay);
                var eng = EntranceDoors.Engine; Assert.IsNotNull(eng);
                pb.Apply(vis);
                foreach (TimeOfDay tod in System.Enum.GetValues(typeof(TimeOfDay)))
                {
                    EntranceDoors.SetupStaticClosed(layout, tod);
                    Assert.IsTrue(EntranceDoors.StaticClosed);
                    var doors = EntranceDoors.Doors;
                    Assert.AreEqual(2, doors.Count);
                    var root = GameObject.Find("EntranceDoors");
                    Assert.AreEqual(doors.Count * 3, root.GetComponentsInChildren<SpriteRenderer>(true).Length, "interior + closed leaf + front frame only");
                    foreach (var d in doors)
                    {
                        Assert.IsTrue(d.Layered); Assert.AreEqual(DoorState.Closed, d.State);
                        StringAssert.Contains("_" + EntranceDoor.StateName(eng, DoorState.Closed) + "_", d.Renderer.sprite.name + "_", "closed leaf");
                        foreach (var r in d.LayerRenderers)
                        {
                            Assert.AreEqual(Color.white, r.color, "no double TOD tint");
                            StringAssert.EndsWith("_" + EntranceDoor.TodName(eng, tod), r.sprite.name);
                            Assert.AreEqual(1f, r.transform.lossyScale.y, 1e-4f, "scale 1 at 54 ppu");
                        }
                    }
                    Assert.IsTrue(pb.DoorwayBackgroundActive);
                    var sp = pb.BaseLayer.sprite; var want = eng.backgrounds.First(b => b.tod == EntranceDoor.TodName(eng, tod)).file;
                    StringAssert.EndsWith(System.IO.Path.GetFileName(want), sp.name, tod.ToString());
                    Assert.AreEqual(54f, sp.pixelsPerUnit, 1e-3f); Assert.AreEqual(1f, pb.BaseLayer.transform.localScale.x, 1e-4f, "no overscan");
                    Assert.AreEqual(Color.white, pb.BaseLayer.color);
                }
                Assert.AreEqual(TimeOfDay.Noon, vis.timeOfDay, "visual asset TOD untouched (no cycling)");
            }
            finally { EntranceDoors.Clear(); Object.DestroyImmediate(pgo); Object.DestroyImmediate(cam); foreach (var c in off) if (c != null) c.enabled = true; }
        }
    }
}

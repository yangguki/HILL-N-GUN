using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;
using HillNGun.Data;
using HillNGun.PvE;
using HillNGun.Rig;

namespace HillNGun.Tests.EditMode
{
    /// <summary>v13: Buster cutout rig (2D Animation bones + SpriteSkin, data-driven views / clips) and designer door states.</summary>
    [TestFixture]
    public class RigDoorTests
    {
        private GameObject _go;
        [TearDown] public void TearDown() { if (_go != null) Object.DestroyImmediate(_go); }

        private CutoutRig Make(string id = "buster")
        {
            _go = new GameObject("rig_test");
            return new CutoutRig(_go.transform, RigDefinition.Load(id), -0.85f, 2.4f);
        }
        private static RigInput Inp(Vector2 aim, bool aiming = true, Vector2 vel = default, bool forceSide = false) =>
            new RigInput { aim = aim, aiming = aiming, velocity = vel, maxSpeed = 5f, facing = 1f, forceSide = forceSide, sortingOrder = 500 };
        private static void Run(CutoutRig r, RigInput i, float secs) { for (float t = 0f; t < secs; t += 1f / 60f) r.Tick(1f / 60f, i); }

        [TestCase("buster")] [TestCase("raven")] [TestCase("volt")]
        public void RigData_ThreeViews_18Parts_OneAtlas_EverySpriteSkinned(string id)
        {
            var def = RigDefinition.Load(id);
            Assert.IsNotNull(def, $"Resources/Rig/{id}_rig.json");
            CollectionAssert.AreEquivalent(new[] { "down_3q", "side", "up_3q" }, def.directions.Select(d => d.name).ToArray());
            foreach (var v in def.directions) Assert.AreEqual(18, v.parts.Length, v.name);
            var sprites = Resources.LoadAll<Sprite>($"Rig/{id}_rig_atlas");
            Assert.AreEqual(54, sprites.Length);
            Assert.AreEqual(1, sprites.Select(s => s.texture).Distinct().Count(), "one atlas texture per character");
            foreach (var s in sprites)
            {
                Assert.AreEqual(1, s.GetBones().Length, s.name + " bones");
                Assert.AreEqual(1, s.GetBindPoses().Length, s.name + " bindpose");
                Assert.AreEqual(s.name.Substring(s.name.IndexOf("__") + 2), s.GetBones()[0].name, "bone named after the part (Spine mapping)");
            }
        }

        [Test]
        public void RigData_Raven_Cape_Volt_Wrench_OneHand_NoSupportIK()
        {
            var raven = RigDefinition.Load("raven"); var volt = RigDefinition.Load("volt"); var buster = RigDefinition.Load("buster");
            Assert.AreEqual("cape", raven.secondaryPart); Assert.AreEqual("wrench", volt.secondaryPart); Assert.AreEqual("scarf", buster.secondaryPart);
            Assert.AreEqual(6f, volt.secondaryMaxDeg, 1e-3f, "wrench spring +-6 deg per designer");
            foreach (var v in volt.directions) Assert.IsFalse(v.supportIK, "Volt one-handed: support-hand IK disabled " + v.name);
            foreach (var v in raven.directions) Assert.IsTrue(v.supportIK, "Raven two-handed long gun " + v.name);
            foreach (var v in buster.directions) Assert.IsTrue(v.supportIK, "Buster v2.1 " + v.name);
            Assert.IsFalse(raven.View("up_3q").supportVisible); Assert.IsFalse(buster.View("up_3q").supportVisible, "hidden support anchor in up_3q");
            // runtime: Volt wrench stays within +-6 under a violent run; hand_l free (no IK error tracked); Raven cape moves, IK holds
            var r = Make("volt");
            Assert.IsTrue(r.AllSkinsValid); Assert.AreEqual(54, r.SkinCount);
            float maxW = 0f;
            for (int i = 0; i < 120; i++) { r.Tick(1f / 60f, Inp(new Vector2(1f, 0f), true, new Vector2(i % 30 < 15 ? 5f : -5f, 0f))); maxW = Mathf.Max(maxW, Mathf.Abs(r.SecondaryAngle)); }
            Assert.LessOrEqual(maxW, 6.001f); Assert.Greater(maxW, 0.5f, "wrench spring active");
            Assert.AreEqual(0f, r.IkError, 1e-5f, "no support IK solved for Volt");
            Object.DestroyImmediate(_go);
            r = Make("raven"); float maxC = 0f;
            for (int i = 0; i < 120; i++) { r.Tick(1f / 60f, Inp(new Vector2(1f, 0.3f), true, new Vector2(i % 30 < 15 ? 5f : -5f, 0f))); maxC = Mathf.Max(maxC, Mathf.Abs(r.SecondaryAngle)); }
            Assert.Greater(maxC, 0.5f, "cape spring active"); Assert.LessOrEqual(maxC, raven.secondaryMaxDeg + 1e-3f);
            Assert.Less(r.IkError, 0.05f, "Raven support hand on the forend");
            // conservative joint limits honoured on the run cycle (draft art)
            float shin = 0f; for (int i = 0; i < 60; i++) { r.Tick(1f / 60f, Inp(new Vector2(1f, 0f), true, new Vector2(5f, 0f))); shin = Mathf.Max(shin, Mathf.Abs(Mathf.DeltaAngle(0f, r.Bone("side", "shin_l").localEulerAngles.z))); }
            Assert.LessOrEqual(shin, 45.01f);
        }

        [Test]
        public void RigData_DrawOrder_FarLimbsBehind_GunBehindBodyInUp3q()
        {
            foreach (var id in new[] { "raven", "volt" })
            {
                var d = RigDefinition.Load(id);
                foreach (var v in d.directions) { Assert.Less(System.Array.IndexOf(v.drawOrder, "upper_arm_l"), System.Array.IndexOf(v.drawOrder, "torso"), id + v.name); Assert.Less(System.Array.IndexOf(v.drawOrder, "thigh_l"), System.Array.IndexOf(v.drawOrder, "pelvis"), id + v.name); }
            }
            var def = RigDefinition.Load("buster");
            int I(string v, string p) => System.Array.IndexOf(def.View(v).drawOrder, p);
            foreach (var v in new[] { "down_3q", "side", "up_3q" }) { Assert.Less(I(v, "upper_arm_l"), I(v, "torso"), v); Assert.Less(I(v, "thigh_l"), I(v, "pelvis"), v); }
            Assert.Less(I("up_3q", "weapon"), I("up_3q", "torso"), "up_3q gun behind the body");
            Assert.Greater(I("side", "weapon"), I("side", "torso"));
            Assert.Greater(I("down_3q", "weapon"), I("down_3q", "torso"));
        }

        [Test]
        public void Rig_Builds_SpriteSkins_ForAllParts_StartsSide()
        {
            var r = Make();
            Assert.IsTrue(r.AllSkinsValid, "SpriteSkin SetRootBone/SetBoneTransforms Ready for every part");
            Assert.AreEqual(54, r.SkinCount);
            Assert.AreEqual("side", r.ViewName);
            Assert.AreEqual(18, r.ActiveRendererCount, "only the current view is active");
            var skin = r.Renderers("side")[0].GetComponent<SpriteSkin>();
            Assert.IsNotNull(skin); Assert.AreEqual(1, skin.boneTransforms.Length);
        }

        [Test]
        public void Rig_ViewSelection_ByAim_WithHysteresis_PvPForcedSide()
        {
            var s = RigClipSet.Shared.settings;
            Assert.AreEqual(0, CutoutRig.SelectView(-60f, 1, s, false, 3));
            Assert.AreEqual(1, CutoutRig.SelectView(0f, 0, s, false, 3));
            Assert.AreEqual(2, CutoutRig.SelectView(60f, 1, s, false, 3));
            Assert.AreEqual(0, CutoutRig.SelectView(s.viewDownBelowDeg + 2f, 0, s, false, 3), "hysteresis keeps down");
            Assert.AreEqual(1, CutoutRig.SelectView(s.viewDownBelowDeg - 2f, 1, s, false, 3), "hysteresis keeps side");
            Assert.AreEqual(1, CutoutRig.SelectView(-80f, 0, s, true, 3), "PvP: side only");
            Assert.AreEqual(-45f, CutoutRig.AimDeg(new Vector2(-1f, -1f), 1f), 0.01f, "facing-relative (mirrored by the flip)");
        }

        [Test]
        public void Rig_ViewSwitch_InstantReplacement_ExactlyOneActiveView()
        {
            // v15 planner: no dual-active view skins during a view change (instant replacement)
            var r = Make();
            r.Tick(1f / 60f, Inp(new Vector2(1f, -2f)));
            Assert.AreEqual("down_3q", r.ViewName);
            Assert.IsFalse(r.Crossfading);
            Assert.AreEqual(1, r.ActiveViewCount);
            Assert.AreEqual(18, r.ActiveRendererCount, "only the new view is drawn");
            Assert.AreEqual(18, r.ActiveSkinCount);
            Run(r, Inp(new Vector2(1f, 2f)), 0.5f);
            Assert.AreEqual("up_3q", r.ViewName);
            Assert.AreEqual(1, r.ActiveViewCount);
        }

        private static float[] Snapshot(CutoutRig r, string view)
        {
            var names = new[] { "pelvis", "torso", "head", "thigh_l", "shin_l", "thigh_r", "shin_r", "upper_arm_r", "forearm_r", "upper_arm_l", "forearm_l", "hand_l", "hat" };
            var o = new System.Collections.Generic.List<float>();
            foreach (var n in names) { var b = r.Bone(view, n); if (b == null) continue; o.Add(b.localEulerAngles.z); o.Add(b.localPosition.x); o.Add(b.localPosition.y); }
            return o.ToArray();
        }

        [Test]
        public void Rig_Offscreen_SkipsPose_ThenReturnPoseEqualsAlwaysPosedRig()
        {
            var off = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.enabled).ToList(); // test scene's default perspective camera
            foreach (var c in off) c.enabled = false;
            var camGo = new GameObject("cull_cam"); var cam = camGo.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 5f; cam.aspect = 16f / 9f;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var goA = new GameObject("rigA"); var goB = new GameObject("rigB"); goB.transform.position = new Vector3(500f, 0f, 0f);
            try
            {
                var a = new CutoutRig(goA.transform, RigDefinition.Load("raven"), -0.85f, 2.4f);
                var b = new CutoutRig(goB.transform, RigDefinition.Load("raven"), -0.85f, 2.4f);
                int skip0 = CutoutRig.SkippedOffscreen;
                for (int i = 0; i < 90; i++)
                {
                    var inp = Inp(new Vector2(1f, 0.2f * Mathf.Sin(i * 0.1f)), i % 20 < 10, new Vector2(i % 40 < 20 ? 4f : -2f, 0.5f));
                    a.Tick(1f / 60f, inp); b.Tick(1f / 60f, inp);
                }
                Assert.Greater(CutoutRig.SkippedOffscreen - skip0, 80, "offscreen rig skipped its pose");
                CollectionAssert.AreNotEqual(Snapshot(a, "side"), Snapshot(b, "side"), "B's bones were not updated offscreen");
                goB.transform.position = Vector3.zero; // returns on screen
                var last = Inp(new Vector2(1f, 0.1f), true, new Vector2(3f, 0f));
                a.Tick(1f / 60f, last); b.Tick(1f / 60f, last);
                var sa = Snapshot(a, "side"); var sb = Snapshot(b, "side");
                Assert.AreEqual(sa.Length, sb.Length);
                for (int i = 0; i < sa.Length; i++) Assert.AreEqual(sa[i], sb[i], 1e-3f, "return pose identical to an always-posed rig (" + i + ")");
            }
            finally { Object.DestroyImmediate(goA); Object.DestroyImmediate(goB); Object.DestroyImmediate(camGo); foreach (var c in off) if (c != null) c.enabled = true; }
        }

        [Test]
        public void Rig_Down_FreezesOnlyAfterFallCompletes_UnfreezesOnRevive()
        {
            var r = Make();
            var down = Inp(Vector2.right, false); down.downed = true;
            var prev = Snapshot(r, "side"); bool changedDuringFall = false;
            for (int i = 0; i < 6; i++) { r.Tick(1f / 60f, down); Assert.IsFalse(r.Frozen, "never frozen mid-fall"); var s1 = Snapshot(r, "side"); if (!Same(prev, s1)) changedDuringFall = true; prev = s1; }
            Assert.IsTrue(changedDuringFall, "fall animates");
            Run(r, down, 1.2f);
            Assert.IsTrue(r.Frozen, "frozen after the final down pose held");
            var f0 = Snapshot(r, "side"); int skip0 = CutoutRig.SkippedFrozen;
            Run(r, down, 0.5f);
            Assert.IsTrue(Same(f0, Snapshot(r, "side")), "bones untouched while frozen");
            Assert.Greater(CutoutRig.SkippedFrozen - skip0, 20);
            Assert.IsTrue(r.StarsVisible, "downed stars keep spinning (not skinned)");
            var up = Inp(Vector2.right, false); r.Tick(1f / 60f, up);
            Assert.IsFalse(r.Frozen, "revive unfreezes immediately");
            Run(r, up, 0.4f);
            Assert.IsFalse(Same(f0, Snapshot(r, "side")), "pose leaves the down pose after revive");
        }
        private static bool Same(float[] a, float[] b) { if (a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (Mathf.Abs(a[i] - b[i]) > 1e-4f) return false; return true; }

        [Test]
        public void Rig_UpperFollowsAim_LowerFollowsMove_SupportHandOnBarrel()
        {
            var r = Make();
            var arm = r.Bone("side", "upper_arm_r"); var thigh = r.Bone("side", "thigh_l");
            Run(r, Inp(new Vector2(1f, 0f)), 0.3f);
            float arm0 = Mathf.DeltaAngle(0f, arm.localEulerAngles.z);
            Run(r, Inp(new Vector2(1f, 0.45f)), 0.3f); // ~24 deg up: still side view, gun arm rotates
            Assert.AreEqual("side", r.ViewName);
            Assert.Greater(Mathf.DeltaAngle(0f, arm.localEulerAngles.z) - arm0, 10f, "gun arm rotates toward the aim");
            Assert.Less(r.IkError, 0.05f, "support hand stays on the forend (2-bone IK)");
            // legs: animate while moving; walking against the facing (walk-back shooting) runs the gait phase backwards
            float a0 = thigh.localEulerAngles.z;
            Run(r, Inp(new Vector2(1f, 0f), true, new Vector2(3f, 0f)), 0.2f);
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(a0, thigh.localEulerAngles.z)), 3f, "legs animate while moving");
            Assert.AreEqual("side", r.ViewName, "moving does not change the aim-selected view");
            float p0 = r.GaitPhase; r.Tick(1f / 60f, Inp(new Vector2(1f, 0f), true, new Vector2(3f, 0f)));
            Assert.Greater(Mathf.Repeat(r.GaitPhase - p0, 1f), 0f); Assert.Less(Mathf.Repeat(r.GaitPhase - p0, 1f), 0.5f, "forward gait");
            p0 = r.GaitPhase; r.Tick(1f / 60f, Inp(new Vector2(1f, 0f), true, new Vector2(-3f, 0f)));
            Assert.Greater(Mathf.Repeat(r.GaitPhase - p0, 1f), 0.5f, "backward gait while aiming forward");
        }

        [Test]
        public void Rig_PvP_ForcedSide_NoAimRotation_StatesDownRoll()
        {
            var r = Make();
            var arm = r.Bone("side", "upper_arm_r");
            Run(r, Inp(new Vector2(1f, -1f), true, default, forceSide: true), 0.4f);
            Assert.AreEqual("side", r.ViewName);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, arm.localEulerAngles.z), 1.0f, "PvP: forward aim only (visual)");
            var down = Inp(new Vector2(1f, 0f), false); down.downed = true;
            Run(r, down, 0.5f);
            Assert.IsTrue(r.StarsVisible, "downed stars");
            var roll = Inp(new Vector2(1f, 0f), false); roll.rolling = true; roll.rollProgress = 0.5f;
            Run(r, roll, 0.2f);
            Assert.Greater(r.ShadowMul.x, 1.15f, "roll stretches the ellipse shadow");
        }

        // ------------------------------------------------------------------ doors
        [Test]
        public void Door_Telegraph_AlternatesAB_100ms_ThenOpen_CloseAfterExit()
        {
            var seq = Enumerable.Range(0, 8).Select(i => DoorTimeline.TelegraphFrame(i * 0.1f + 0.05f, 0.8f)).ToArray();
            CollectionAssert.AreEqual(new[] { DoorState.SwingA, DoorState.SwingB, DoorState.SwingA, DoorState.SwingB, DoorState.SwingA, DoorState.SwingB, DoorState.SwingA, DoorState.SwingB }, seq);
            Assert.AreEqual(DoorState.Open, DoorTimeline.TelegraphFrame(0.8f, 0.8f));
            Assert.AreEqual(DoorState.Open, DoorTimeline.Evaluate(-1f, 0.8f, true, 0f), "open while walking out");
            Assert.AreEqual(DoorState.Open, DoorTimeline.Evaluate(-1f, 0.8f, false, 0.05f));
            Assert.AreEqual(DoorState.SwingB, DoorTimeline.Evaluate(-1f, 0.8f, false, 0.15f), "v15 kit close: open -> swingB");
            Assert.AreEqual(DoorState.SwingA, DoorTimeline.Evaluate(-1f, 0.8f, false, 0.25f), "-> swingA");
            Assert.AreEqual(0.3f, DoorTimeline.CloseDelay, 1e-4f, "~300 ms close per kit occlusion state machine");
            Assert.AreEqual(DoorState.Closed, DoorTimeline.Evaluate(-1f, 0.8f, false, DoorTimeline.CloseDelay + 0.01f), "closes after the enemy has exited");
            Assert.AreEqual(DoorState.Open, DoorTimeline.Evaluate(0.5f, 0.8f, true, 0f), "a walker keeps it open over a new telegraph");
        }

        [Test]
        public void Door_16Frames_AnchorPivot_TimeOfDayMapping_Placement()
        {
            foreach (var kind in new[] { "saloon", "shop" })
                foreach (var tod in new[] { TimeOfDay.Noon, TimeOfDay.Night })
                    foreach (DoorState s in System.Enum.GetValues(typeof(DoorState)))
                    {
                        var sp = EntranceDoors.Frame(DoorTimeline.FrameName(kind, tod, s));
                        Assert.IsNotNull(sp, DoorTimeline.FrameName(kind, tod, s));
                        Assert.AreEqual(new Vector2(128f, 20f), sp.pivot, "designer anchor (128,300) from top = (128,20) from bottom");
                    }
            Assert.AreEqual(16, EntranceDoors.FrameCount);
            StringAssert.Contains("_night_", DoorTimeline.FrameName("shop", TimeOfDay.Night, DoorState.Open));
            StringAssert.Contains("_day_", DoorTimeline.FrameName("shop", TimeOfDay.Sunset, DoorState.Open));
            var pl = EntranceDoors.Placement;
            Assert.IsNotNull(pl);
            Assert.AreEqual("saloon", pl.slots.left.default_kind); Assert.IsFalse(pl.slots.left.flip_x);
            Assert.AreEqual("shop", pl.slots.right.default_kind); Assert.IsTrue(pl.slots.right.flip_x);
            Assert.AreEqual(0.25f, pl.recommended_scale, 1e-4f);
        }

        [Test]
        public void Door_AllFrames_SameRectSizeAndOpaqueBounds_NoStateJump_FadeNearInstant()
        {
            // v13 QA: states must not change size / anchor (no visual jump between closed / swing / open)
            Rect? r0 = null; RectInt? b0 = null;
            foreach (var kind in new[] { "saloon", "shop" })
                foreach (var tod in new[] { TimeOfDay.Noon, TimeOfDay.Night })
                    foreach (DoorState st in System.Enum.GetValues(typeof(DoorState)))
                    {
                        var sp = EntranceDoors.Frame(DoorTimeline.FrameName(kind, tod, st));
                        Assert.AreEqual(256f, sp.rect.width, "no import downscale"); Assert.AreEqual(320f, sp.rect.height);
                        Assert.AreEqual(100f, sp.pixelsPerUnit);
                        if (r0 == null) r0 = sp.rect; else Assert.AreEqual(r0.Value.size, sp.rect.size);
                        var bb = OpaqueBounds(sp.texture);
                        if (b0 == null) b0 = bb; else Assert.AreEqual(b0.Value, bb, $"opaque bounds identical across frames ({kind} {tod} {st})");
                    }
            // designer qa: drawn bbox x31..224, y33..302 from top -> anchor sits on the threshold (y=300) within the 2px AA
            Assert.AreEqual(31, b0.Value.xMin, 1); Assert.AreEqual(320 - 303, b0.Value.yMin, 1);
            Assert.Less(HillNGun.PvE.EnemyView.EntranceFadeShare(true), 0.1f, "designer door: no long translucent body over the dark open frame");
            Assert.AreEqual(0.35f, HillNGun.PvE.EnemyView.EntranceFadeShare(false), 1e-4f, "overlay doorway / alley unchanged");
        }

        private static RectInt OpaqueBounds(Texture2D src)
        {
            var t = new Texture2D(2, 2); t.LoadImage(System.IO.File.ReadAllBytes(UnityEditor.AssetDatabase.GetAssetPath(src))); // source pixels (import is not readable)
            var px = t.GetPixels32(); int w = t.width, h = t.height; int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (px[y * w + x].a > 0) { x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y); }
            return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        [TestCase("buster")] [TestCase("raven")] [TestCase("volt")]
        public void Rig_Roll_IsBodyCurl_KneesTorsoHeadArmsBend_NotRigidTumbleOnly(string id)
        {
            var r = Make(id);
            Run(r, Inp(new Vector2(1f, 0f), false), 0.3f);
            float A(string b) => Mathf.Abs(Mathf.DeltaAngle(0f, r.Bone("side", b).localEulerAngles.z));
            var roll = Inp(new Vector2(1f, 0f), false); roll.rolling = true;
            for (int i = 0; i < 12; i++) { roll.rollProgress = 0.5f * i / 11f; r.Tick(1f / 60f, roll); }
            Assert.GreaterOrEqual(A("thigh_l"), 60f, "knees to chest"); Assert.GreaterOrEqual(A("shin_l"), 60f, "knees bent");
            Assert.GreaterOrEqual(A("torso"), 20f, "torso tucks"); Assert.GreaterOrEqual(A("head"), 15f, "head tucks");
            Assert.GreaterOrEqual(A("forearm_r"), 30f, "arms pulled in");
            Assert.LessOrEqual(A("shin_l"), 95.01f, "draft-art full-body limit");
        }

        [TestCase("buster")] [TestCase("volt")]
        public void Rig_GrenadeWindup_ThrowArmDrawnInFront_ThenRestored(string id)
        {
            var r = Make(id);
            foreach (var view in new[] { "side", "down_3q", "up_3q" })
            {
                var rs = r.Renderers(view); int Ord(string n) => rs.First(x => x.name == "part_" + n).sortingOrder;
                int arm0 = Ord("upper_arm_r");
                var wind = Inp(new Vector2(view == "up_3q" ? 0.2f : 1f, view == "side" ? 0f : (view == "up_3q" ? 1f : -1f)), false, default, view == "side"); wind.grenadeAiming = true;
                Run(r, wind, 0.6f);
                if (r.ViewName != view) continue;
                Assert.IsTrue(r.ThrowArmInFront, view);
                Assert.Greater(Ord("upper_arm_r"), Ord("torso"), $"{id} {view}: windup arm over the torso");
                var rel = wind; rel.grenadeAiming = false; Run(r, rel, 0.6f);
                Assert.IsFalse(r.ThrowArmInFront); Assert.AreEqual(arm0, Ord("upper_arm_r"), "draw order restored after the throw");
            }
        }
    }
}

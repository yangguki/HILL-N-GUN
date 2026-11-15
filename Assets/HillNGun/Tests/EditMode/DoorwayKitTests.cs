using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HillNGun.Data;
using HillNGun.PvE;
using HillNGun.Core;
using HillNGun.Battlefield;

namespace HillNGun.Tests.EditMode
{
    /// <summary>v15 doorway kit (first kit = geometry / layer validation): data-driven 3 depth groups around the enemy,
    /// manifest floor-anchor registration, no TOD double tint, no legacy door drawn, threshold depth switch, 0.4 s visual walk.</summary>
    [TestFixture]
    public class DoorwayKitTests
    {
        private static readonly string[] Ids = { "door_L", "door_R" };
        private GameObject _cam; private StreetLayoutData _layout; private readonly System.Collections.Generic.List<Camera> _off = new System.Collections.Generic.List<Camera>();

        [SetUp] public void SetUp()
        {
            _off.Clear(); // the test scene's default perspective "Main Camera" stands in for the game camera: disable it for the test
            foreach (var cc in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (cc.enabled) { cc.enabled = false; _off.Add(cc); }
            _cam = new GameObject("TestMainCam") { tag = "MainCamera" };
            var c = _cam.AddComponent<Camera>(); c.orthographic = true; c.orthographicSize = 10f; _cam.transform.position = new Vector3(0f, 0f, -10f);
            _layout = AssetDatabase.LoadAssetAtPath<StreetLayoutData>("Assets/HillNGun/Data/Layout/street_layout_dust.asset");
        }
        [TearDown] public void TearDown() { EntranceDoors.Clear(); Object.DestroyImmediate(_cam); foreach (var c in _off) if (c != null) c.enabled = true; }

        private static Texture2D Src(Sprite sp) { var t = new Texture2D(2, 2); t.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sp.texture))); return t; }

        [Test]
        public void Kit_EngineAndManifest_Present_EveryLayerSprite_PivotIsFloorAnchor_Ppu54()
        {
            var e = EntranceDoors.Engine; Assert.IsNotNull(e, "Resources/Doorways/doorways_engine.json + manifest");
            Assert.AreEqual(0.4f, e.visualWalkTime, 1e-4f); Assert.AreEqual(1, e.enemyInsideZ); Assert.IsTrue(e.preGradedNoTodTint);
            CollectionAssert.AreEqual(new[] { 0, 2, 3 }, e.layers.Select(l => l.z).ToArray(), "interior < [enemy z1 before threshold] < door < front_frame");
            foreach (var id in Ids)
            {
                var d = EntranceDoors.KitDoor(id); Assert.IsNotNull(d, id);
                foreach (var tod in e.todNames)
                    foreach (var L in e.layers)
                        foreach (var st in L.stateful ? e.stateNames : new string[] { null })
                        {
                            var sp = EntranceDoors.LoadKitSprite(L.file, id, tod, st);
                            Assert.IsNotNull(sp, $"{id} {L.name} {tod} {st}");
                            Assert.AreEqual(d.canvas_size[0], sp.rect.width); Assert.AreEqual(d.canvas_size[1], sp.rect.height);
                            Assert.AreEqual(d.scale.import_ppu, sp.pixelsPerUnit, 1e-3f);
                            Assert.AreEqual(d.floor_anchor_local_px[0], sp.pivot.x, 0.6f, "pivot x = manifest floor anchor");
                            Assert.AreEqual(d.canvas_size[1] - d.floor_anchor_local_px[1], sp.pivot.y, 0.6f, "pivot y = manifest floor anchor");
                        }
            }
        }

        [Test]
        public void Kit_Opening_AlphaRegistration_PatchAndOpenDoorClearAtOpening_InteriorBehind_Ratio()
        {
            var e = EntranceDoors.Engine; var head = EntranceDoors.ManifestHead;
            foreach (var id in Ids)
            {
                var d = EntranceDoors.KitDoor(id); var o = d.opening_local_px; int H = (int)d.canvas_size[1];
                int cx = (int)((o.x0 + o.x1) * 0.5f), cy = H - 1 - (int)((o.y_top + o.y_floor) * 0.5f); // texture rows are bottom-up
                foreach (var tod in e.todNames)
                {
                    Color32 A(string layer, string st = null) => Src(EntranceDoors.LoadKitSprite(e.layers.First(l => l.name == layer).file, id, tod, st)).GetPixel(cx, cy);
                    Assert.AreEqual(0, A("front_frame").a, $"{id} {tod}: front frame is clear inside the opening (enemy visible)");
                    Assert.Greater(A("interior").a, 0.9f * 255, $"{id} {tod}: dark interior behind the enemy");
                    Assert.AreEqual(0, A("door", "open").a, $"{id} {tod}: open door clear at the centre");
                    // closed state = batwing leaves (lower part of the opening, centre seam): mid-leaf row covered across the opening
                    var closed = Src(EntranceDoors.LoadKitSprite(e.layers.First(l => l.name == "door").file, id, tod, "closed"));
                    var open = Src(EntranceDoors.LoadKitSprite(e.layers.First(l => l.name == "door").file, id, tod, "open"));
                    int row = H - 1 - (int)(o.y_floor - 0.45f * (o.y_floor - o.y_top)), cov = 0, covO = 0, w = 0;
                    for (int x = (int)o.x0 + 4; x < (int)o.x1 - 4; x++, w++) { if (closed.GetPixel(x, row).a > 0.5f) cov++; if (open.GetPixel(x, row).a > 0.5f) covO++; }
                    Assert.GreaterOrEqual(cov / (float)w, 0.9f, $"{id} {tod}: closed leaves cover the opening (mid-leaf row)");
                    Assert.LessOrEqual(covO / (float)w, 0.25f, $"{id} {tod}: open leaves folded to the jambs");
                }
                float openH = o.y_floor - o.y_top;
                Assert.GreaterOrEqual(openH, e.minOpeningToEnemy * head.EnemyPlanPx - 0.01f, $"{id}: opening {openH}px vs 1.15 x planned enemy {head.EnemyPlanPx}px"); Assert.Greater(head.EnemyPlanPx, 100f);
            }
        }

        [Test]
        public void Kit_Setup_Layered_ReplacesLegacyDoor_NoTodTint_SortGroups_AnchorAtPaintedEntrance()
        {
            Assert.IsNotNull(_layout);
            foreach (TimeOfDay tod in System.Enum.GetValues(typeof(TimeOfDay)))
            {
                EntranceDoors.Setup(_layout, tod);
                var doors = EntranceDoors.Doors;
                Assert.AreEqual(_layout.spawnPoints.Count(s => s.type == SpawnPointType.StorefrontDoor), doors.Count);
                var root = GameObject.Find("EntranceDoors");
                Assert.AreEqual(doors.Count * 3, root.GetComponentsInChildren<SpriteRenderer>(true).Length, "only the kit layers: legacy single door not drawn");
                foreach (var d in doors)
                {
                    Assert.IsTrue(d.Layered, d.Point.id);
                    var kd = EntranceDoors.KitDoor(d.Point.id);
                    CollectionAssert.AreEqual(new[] { 0, 2, 3 }, d.LayerRenderers.Select(r => r.sortingOrder - d.BaseOrder).ToArray());
                    Assert.AreEqual((((kd.opening_local_px.x0 + kd.opening_local_px.x1) * 0.5f + kd.canvas_bg_rect[0]) - 960f) / 54f, d.InsideStartWorld.x, 1e-3f, "spawn x = JSON opening centre, not anchor x");
                    Assert.AreEqual(d.InsideStartWorld.x, d.CrossWorld.x, 1e-4f);
                    Assert.AreEqual(FieldView.DepthOrder(d.Point.entranceStart.y + 1f), d.BaseOrder);
                    foreach (var r in d.LayerRenderers)
                    {
                        Assert.AreEqual(Color.white, r.color, "pre-graded per TOD: no TimeOfDayGrade multiply");
                        StringAssert.EndsWith("_" + EntranceDoor.TodName(EntranceDoors.Engine, tod), r.sprite.name);
                        Assert.AreEqual(Vector3.one, new Vector3(Mathf.Abs(r.transform.lossyScale.x), r.transform.lossyScale.y, 1f), "1:1 at 1080p (ppu 54, scale 1)");
                    }
                    // anchor = painted entrance floor (bg px -> world at 54 px/unit, camera centred)
                    Assert.AreEqual((kd.floor_anchor_bg_px[0] - 960f) / 54f, d.AnchorWorld.x, 1e-3f);
                    Assert.AreEqual((540f - kd.floor_anchor_bg_px[1]) / 54f, d.AnchorWorld.y, 1e-3f);
                    Assert.AreEqual(d.AnchorWorld, d.Renderer.transform.position, "L3 pivot on the threshold");
                    Assert.AreEqual(176f, d.OpeningHeightWorld * d.PxPerWorld, 0.5f);
                }
                EntranceDoors.Clear();
            }
        }

        [Test]
        public void Kit_VisualWalk_04s_InsideBehindDoorThenFront_LogicTimingsUnchanged()
        {
            EntranceDoors.Setup(_layout, TimeOfDay.Noon);
            foreach (var d in EntranceDoors.Doors)
            {
                var sp = d.Point; float foot = -0.75f; var post = FieldView.ToWorld(sp.position);
                Assert.AreEqual(0.8f, sp.telegraphTime, 1e-4f, "telegraph unchanged"); Assert.AreEqual(0.5f, sp.entranceTime, 1e-4f, "logic entrance / invulnerability unchanged");
                var p0 = EnemyView.DoorVisualPos(d, post, foot, 0f);
                Assert.IsTrue(EnemyView.DoorIsInside(d, p0.y + foot), "first frame: feet inside the room (drawn at z1, behind patch/door/frame)");
                Assert.AreEqual(d.InsideStartWorld.x, p0.x, 1e-4f);
                Assert.AreEqual(post, EnemyView.DoorVisualPos(d, post, foot, 0.4f), "visual walk done at 0.4 s");
                Assert.AreEqual(post, EnemyView.DoorVisualPos(d, post, foot, 0.5f), "holds the post until logic ends at 0.5 s");
                float tCross = -1f;
                for (float t = 0f; t <= 0.4f; t += 1f / 60f)
                    if (tCross < 0f && !EnemyView.DoorIsInside(d, EnemyView.DoorVisualPos(d, post, foot, t).y + foot)) tCross = t;
                Assert.Greater(tCross, 0.1f, "door in front for the first part of the walk"); Assert.Less(tCross, 0.3f, "enemy in front before the walk ends");
            }
        }

        [Test]
        public void Kit_V2Background_ScreenSpace1to1_NoOverscan_SingleOpaquePlate_RestoredWithoutDoors_NoFirstKitPixels()
        {
            foreach (var old in new[] { "Doorways/door_L/door_L_L2_facade_patch_day", "Doorways/door_L/door_L_L0_interior_day", "Doorways/door_R/door_R_L4_frame_awning_night", "Doorways/doorways_manifest" })
                Assert.IsNull(Resources.Load(old), "first kit removed: " + old);
            StringAssert.StartsWith("doorways_v2.1", EntranceDoors.ManifestHead.version);
            var vis = AssetDatabase.LoadAssetAtPath<StageVisualData>("Assets/HillNGun/Data/Visuals/visual_dust_1_1_noon.asset");
            var orig = vis.layers[2].sprite;
            var pgo = new GameObject("ParallaxBackdrop"); var pb = pgo.AddComponent<HillNGun.Scenery.ParallaxBackdrop>();
            try
            {
                pb.Apply(vis);
                Assert.IsFalse(pb.DoorwayBackgroundActive, "no layered doors -> concept plate (PvP keeps its plate)");
                Assert.AreEqual(orig, pb.BaseLayer.sprite);
                foreach (TimeOfDay tod in System.Enum.GetValues(typeof(TimeOfDay)))
                {
                    vis.timeOfDay = tod; pb.Apply(vis); EntranceDoors.Setup(_layout, tod);
                    Assert.IsTrue(pb.DoorwayBackgroundActive, tod.ToString());
                    var sr = pb.BaseLayer; var sp = sr.sprite;
                    StringAssert.Contains("doorways_v2_1920x1080", sp.name);
                    Assert.AreEqual(1920f, sp.rect.width); Assert.AreEqual(1080f, sp.rect.height); Assert.AreEqual(54f, sp.pixelsPerUnit, 1e-3f);
                    Assert.AreEqual(1f, sr.transform.localScale.x, 1e-4f, "scale 1 (no 2.8333 / 1.02 overscan)");
                    Assert.AreEqual(Vector3.zero, sr.transform.position);
                    Assert.AreEqual(35.5556f, sr.bounds.size.x, 1e-3f); Assert.AreEqual(20f, sr.bounds.size.y, 1e-3f);
                    Assert.AreEqual(Color.white, sr.color, "opaque single plate");
                    var all = pgo.GetComponentsInChildren<SpriteRenderer>(); Assert.IsFalse(all.Any(r => r.enabled && r != sr && r.sprite == orig), "old plate not drawn");
                    // screen origin top-left (px) -> world: anchor px (397,449) -> (-10.426, 1.685)
                    var dl = EntranceDoors.Doors.First(d => d.Point.id == "door_L");
                    Assert.AreEqual(-10.426f, dl.AnchorWorld.x, 2e-3f); Assert.AreEqual(1.685f, dl.AnchorWorld.y, 2e-3f);
                    EntranceDoors.Clear();
                    Assert.IsFalse(pb.DoorwayBackgroundActive, "doors cleared -> plate restored");
                }
            }
            finally { vis.timeOfDay = TimeOfDay.Noon; EntranceDoors.Clear(); Object.DestroyImmediate(pgo); }
        }
    }
}

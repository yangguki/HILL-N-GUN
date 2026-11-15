using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;

namespace HillNGun.PvE
{
    public enum DoorState { Closed, SwingA, SwingB, Open }

    /// <summary>Designer door handoff (Resources/Doors/door_metadata.json + placement.json).</summary>
    [System.Serializable] public class DoorSlot { public float[] anchor_bg_px; public string default_kind; public bool flip_x; }
    [System.Serializable] public class DoorSlots { public DoorSlot left, right; }
    [System.Serializable] public class DoorTimelineStep { public string state; public int from, to; }
    [System.Serializable]
    public class DoorPlacement
    {
        public float[] reference_resolution, sprite_canvas, anchor_in_sprite;
        public float recommended_scale = 0.25f;
        public DoorSlots slots;
    }

    // ---------------------------------------------------------------- v15 doorway kit (3 depth groups around the enemy)
    [System.Serializable] public class DoorwayRect { public float x0, x1, y_top, y_floor; }
    [System.Serializable] public class DoorwayScale { public float import_ppu = 54f, scale = 1f; }
    [System.Serializable]
    public class DoorwayDoor
    {
        public string kind; public float[] floor_anchor_bg_px, canvas_size, floor_anchor_local_px, pivot_normalized;
        public DoorwayRect opening_local_px; public DoorwayScale scale; public bool flip_x;
        // doorways v2 schema (screen-px opening + canvas rect); normalised into the fields above by Normalise()
        public float[] anchor_px, anchor_local, pivot_norm, canvas_bg_rect; public DoorwayRect opening_px; public float opening_center_x;
        public DoorwayDoor Normalise()
        {
            if (floor_anchor_bg_px == null || floor_anchor_bg_px.Length < 2) floor_anchor_bg_px = anchor_px;
            if (floor_anchor_local_px == null || floor_anchor_local_px.Length < 2) floor_anchor_local_px = anchor_local;
            if (pivot_normalized == null || pivot_normalized.Length < 2) pivot_normalized = pivot_norm;
            if ((opening_local_px == null || opening_local_px.y_floor <= 0f) && opening_px != null && canvas_bg_rect != null && canvas_bg_rect.Length >= 2)
                opening_local_px = new DoorwayRect { x0 = opening_px.x0 - canvas_bg_rect[0], x1 = opening_px.x1 - canvas_bg_rect[0], y_top = opening_px.y_top - canvas_bg_rect[1], y_floor = opening_px.y_floor - canvas_bg_rect[1] };
            if (scale == null || scale.import_ppu <= 0f) scale = new DoorwayScale();
            return this;
        }
    }
    [System.Serializable] public class DoorwayBackground { public string tod, file; }
    [System.Serializable] public class DoorwayRef { public float[] resolution; public float orthoSize, px_per_unit; }
    [System.Serializable] public class DoorwayEnemyH { public float planning_px; }
    [System.Serializable]
    public class DoorwayManifestHead
    {
        public string version; public float[] ref_resolution; public float enemy_visual_height_px; public DoorwayRef reference; public DoorwayEnemyH enemy_height;
        public float RefW => ref_resolution != null && ref_resolution.Length > 1 ? ref_resolution[0] : reference?.resolution != null && reference.resolution.Length > 1 ? reference.resolution[0] : 1920f;
        public float RefH => ref_resolution != null && ref_resolution.Length > 1 ? ref_resolution[1] : reference?.resolution != null && reference.resolution.Length > 1 ? reference.resolution[1] : 1080f;
        public float EnemyPlanPx => enemy_visual_height_px > 0f ? enemy_visual_height_px : enemy_height != null ? enemy_height.planning_px : 0f;
    }
    [System.Serializable] public class DoorwayLayerDef { public string name, file; public int z; public bool stateful; }
    [System.Serializable]
    public class DoorwayEngine
    {
        public string status, manifest; public DoorwayLayerDef[] layers; public int enemyInsideZ = 1;
        public string[] todNames, stateNames; public bool preGradedNoTodTint = true;
        public float visualWalkTime = 0.4f, crossFraction = 0.55f, crossBelowAnchorPx = 6f, insideFeetAbovePx = 10f, minOpeningToEnemy = 1.15f;
        public float backgroundPpu = 54f, backgroundScale = 1f; public bool backgroundOpaqueSingleLayer = true, backgroundDisablesRuntimeLanternGlow = true;
        public DoorwayBackground[] backgrounds;
    }

    /// <summary>
    /// v13 storefront doors: designer sprites (saloon / shop x day / night x closed / swingA / swingB / open) placed at the
    /// placement.json slots. Telegraph = swingA/B alternating every 100 ms for the spawn point's telegraph time (0.8 s), then
    /// open while the enemy walks out, closing 0.15 s after it has left. Replaces the v11 dark-opening overlay for doors
    /// (alley gaps keep it: no alley art). The '!' marker, entrance immunity / attack delay and spawn rules are untouched.
    /// </summary>
    public static class DoorTimeline
    {
        public const float FrameTime = 0.1f, CloseDelay = 0.3f; // after exit: open 0.1 s -> swingB -> swingA -> closed

        public static DoorState TelegraphFrame(float elapsed, float telegraphTime)
        {
            if (elapsed >= telegraphTime) return DoorState.Open;
            return ((int)(Mathf.Max(0f, elapsed) / FrameTime) & 1) == 0 ? DoorState.SwingA : DoorState.SwingB;
        }

        /// <param name="telegraphElapsed">longest-running telegraph at this door, &lt;0 = none</param>
        /// <param name="anyWalking">an enemy is still in its walk-out from this door</param>
        /// <param name="sinceExit">seconds since the last enemy finished walking out</param>
        public static DoorState Evaluate(float telegraphElapsed, float telegraphTime, bool anyWalking, float sinceExit)
        {
            if (anyWalking) return DoorState.Open;
            if (telegraphElapsed >= 0f) return TelegraphFrame(telegraphElapsed, telegraphTime);
            if (sinceExit < FrameTime) return DoorState.Open;
            if (sinceExit < 2f * FrameTime) return DoorState.SwingB;
            if (sinceExit < CloseDelay) return DoorState.SwingA;
            return DoorState.Closed;
        }

        public static string FrameName(string kind, TimeOfDay tod, DoorState s)
            => $"door_{kind}_{(tod == TimeOfDay.Night ? "night" : "day")}_{(s == DoorState.Closed ? "closed" : s == DoorState.SwingA ? "swingA" : s == DoorState.SwingB ? "swingB" : "open")}";
    }

    public sealed class EntranceDoor
    {
        public readonly StageSpawnPoint Point;
        public readonly string Kind;
        public readonly bool Flip;
        public readonly SpriteRenderer Renderer;
        public DoorState State { get; private set; } = DoorState.Closed;
        // v15 layered doorway (kit): L0 interior / [enemy inside] / L2 patch / L3 door / L4 frame / L5 props; enemy after threshold above all
        public bool Layered { get; private set; }
        public int BaseOrder { get; private set; }
        public Vector3 AnchorWorld { get; private set; }       // floor threshold (enemy feet cross here)
        public Vector3 InsideStartWorld { get; private set; }  // visual walk start (inside the room, opening centre)
        public Vector3 CrossWorld { get; private set; }        // just past the threshold
        public float OpeningHeightWorld { get; private set; }
        public float PxPerWorld { get; private set; } = 54f;
        public DoorwayEngine Engine { get; private set; }
        public readonly List<SpriteRenderer> LayerRenderers = new List<SpriteRenderer>();
        private DoorwayLayerDef[] _layerDefs; private string _id;

        public EntranceDoor(StageSpawnPoint sp, DoorwayEngine eng, DoorwayDoor dd, TimeOfDay tod, Transform parent, Vector3 anchorWorld, float upp)
        {
            Point = sp; Kind = dd.kind; Flip = dd.flip_x; _tod = tod; Engine = eng; _id = sp.id; Layered = true;
            var go = new GameObject("Doorway_" + sp.id + "_" + dd.kind);
            go.transform.SetParent(parent, false);
            go.transform.position = anchorWorld;
            float sc = (dd.scale != null ? dd.scale.scale * dd.scale.import_ppu : 54f) * upp; // authored px -> screen px at the reference height
            go.transform.localScale = new Vector3(Flip ? -sc : sc, sc, 1f);
            BaseOrder = FieldView.DepthOrder(sp.entranceStart.y + 1f);
            AnchorWorld = anchorWorld; PxPerWorld = 1f / upp;
            float ppw = dd.scale != null ? dd.scale.import_ppu : 54f;
            var o = dd.opening_local_px; float ax = dd.floor_anchor_local_px[0], ay = dd.floor_anchor_local_px[1];
            float cxLocal = ((o.x0 + o.x1) * 0.5f - ax) / ppw * sc * (Flip ? -1f : 1f);
            OpeningHeightWorld = (o.y_floor - o.y_top) / ppw * sc;
            InsideStartWorld = new Vector3(anchorWorld.x + cxLocal, anchorWorld.y + eng.insideFeetAbovePx * upp, 0f); // FEET point inside the room
            CrossWorld = new Vector3(anchorWorld.x + cxLocal, anchorWorld.y - eng.crossBelowAnchorPx * upp, 0f); // FEET point just past the threshold
            _layerDefs = eng.layers;
            foreach (var L in eng.layers)
            {
                var lg = new GameObject(L.name); lg.transform.SetParent(go.transform, false);
                var sr = lg.AddComponent<SpriteRenderer>();
                sr.sortingOrder = BaseOrder + L.z;
                sr.color = eng.preGradedNoTodTint ? Color.white : HillNGun.Scenery.TimeOfDayGrade.MulFor(tod); // kit is pre-graded per TOD: no double tint
                if (!L.stateful) sr.sprite = EntranceDoors.LoadKitSprite(L.file, _id, TodName(eng, tod), null);
                else Renderer = sr;
                LayerRenderers.Add(sr);
            }
            if (Renderer == null) Renderer = LayerRenderers.Count > 0 ? LayerRenderers[0] : null;
            Apply(DoorState.Closed, true);
        }

        public static string TodName(DoorwayEngine e, TimeOfDay tod)
        {
            int i = (int)tod; return e.todNames != null && i >= 0 && i < e.todNames.Length ? e.todNames[i] : "day";
        }
        public static string StateName(DoorwayEngine e, DoorState s)
        {
            int i = (int)s; return e.stateNames != null && i >= 0 && i < e.stateNames.Length ? e.stateNames[i] : s.ToString();
        }
        public int StateChanges { get; private set; }
        private readonly List<EnemyInstance> _users = new List<EnemyInstance>();
        private float _sinceExit = 99f;
        private readonly TimeOfDay _tod;

        public EntranceDoor(StageSpawnPoint sp, string kind, bool flip, TimeOfDay tod, Transform parent, Vector3 pos, float scale)
        {
            Point = sp; Kind = kind; Flip = flip; _tod = tod;
            var go = new GameObject("Door_" + sp.id + "_" + kind);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(flip ? -scale : scale, scale, 1f);
            Renderer = go.AddComponent<SpriteRenderer>();
            Renderer.sortingOrder = FieldView.DepthOrder(sp.entranceStart.y + 1f); // over the painted facade, under actors / covers
            // day art follows the actor/cover time-of-day wash; night art is painted lit (no double grade)
            Renderer.color = tod == TimeOfDay.Night ? Color.white : HillNGun.Scenery.TimeOfDayGrade.MulFor(tod);
            Apply(DoorState.Closed, true);
        }

        public void Register(EnemyInstance e) { if (e != null && !_users.Contains(e)) _users.Add(e); }

        public void Tick(float dt)
        {
            float tele = -1f; bool walking = false;
            for (int i = _users.Count - 1; i >= 0; i--)
            {
                var e = _users[i];
                if (e == null) { _users.RemoveAt(i); continue; }
                if (e.Entrance == EntrancePhase.Telegraph) tele = Mathf.Max(tele, e.TelegraphProgress * Point.telegraphTime);
                else if (e.Entrance == EntrancePhase.Walking) walking = true;
                else if (e.Entrance == EntrancePhase.Done) { _users.RemoveAt(i); _sinceExit = 0f; } // exited: close after CloseDelay
                else if (e.IsDefeated) _users.RemoveAt(i);
            }
            if (walking) _sinceExit = 0f; else _sinceExit += dt;
            Apply(DoorTimeline.Evaluate(tele, Mathf.Max(0.01f, Point.telegraphTime), walking, _sinceExit), false);
        }

        private void Apply(DoorState s, bool force)
        {
            if (!force && s == State) return;
            if (!force) StateChanges++;
            State = s;
            if (Layered)
            {
                for (int i = 0; i < _layerDefs.Length; i++)
                    if (_layerDefs[i].stateful) LayerRenderers[i].sprite = EntranceDoors.LoadKitSprite(_layerDefs[i].file, _id, TodName(Engine, _tod), StateName(Engine, s));
                return;
            }
            var sp = EntranceDoors.Frame(DoorTimeline.FrameName(Kind, _tod, s));
            if (sp != null) Renderer.sprite = sp;
        }
    }

    public sealed class EntranceDoors : MonoBehaviour
    {
        private static EntranceDoors _inst;
        private readonly List<EntranceDoor> _doors = new List<EntranceDoor>(); private TimeOfDay _tod; private bool _static;
        public static IReadOnlyList<EntranceDoor> Doors => _inst != null ? _inst._doors : (IReadOnlyList<EntranceDoor>)System.Array.Empty<EntranceDoor>();

        private static Dictionary<string, Sprite> _frames;
        private static DoorPlacement _placement;
        public static DoorPlacement Placement
        {
            get
            {
                if (_placement != null) return _placement;
                var ta = Resources.Load<TextAsset>("Doors/placement");
                _placement = ta != null ? JsonUtility.FromJson<DoorPlacement>(ta.text) : null;
                return _placement;
            }
        }
        public static Sprite Frame(string name)
        {
            if (_frames == null)
            {
                _frames = new Dictionary<string, Sprite>();
                foreach (var s in Resources.LoadAll<Sprite>("Doors")) _frames[s.name] = s;
            }
            return _frames.TryGetValue(name, out var sp) ? sp : null;
        }
        public static int FrameCount { get { Frame(""); return _frames.Count; } }

        private static DoorwayEngine _engine; private static string _manifestText; private static bool _kitLoaded;
        private static readonly Dictionary<string, Sprite> _kitSprites = new Dictionary<string, Sprite>();
        /// <summary>Doorway kit (Resources/Doorways/doorways_engine.json + the designer manifest it names); null = legacy single doors.</summary>
        public static DoorwayEngine Engine { get { LoadKit(); return _engine; } }
        public static string KitStatus => Engine != null ? Engine.status : "no doorway kit (legacy single door sprites)";
        private static void LoadKit()
        {
            if (_kitLoaded) return; _kitLoaded = true;
            var e = Resources.Load<TextAsset>("Doorways/doorways_engine");
            if (e == null) return;
            var eng = JsonUtility.FromJson<DoorwayEngine>(e.text);
            var m = eng != null && !string.IsNullOrEmpty(eng.manifest) ? Resources.Load<TextAsset>(eng.manifest) : null;
            if (m == null || eng.layers == null || eng.layers.Length == 0) return;
            _engine = eng; _manifestText = m.text;
        }
        public static DoorwayManifestHead ManifestHead => Engine != null ? JsonUtility.FromJson<DoorwayManifestHead>(_manifestText) : null;
        /// <summary>Per-door block of the designer manifest by spawn point id ("doors": { "<id>": {...} }); no hard-coded door ids.</summary>
        public static DoorwayDoor KitDoor(string id)
        {
            if (Engine == null || string.IsNullOrEmpty(id)) return null;
            int d = _manifestText.IndexOf("\"doors\"", System.StringComparison.Ordinal); if (d < 0) return null;
            int k = _manifestText.IndexOf("\"" + id + "\"", d, System.StringComparison.Ordinal); if (k < 0) return null;
            int b = _manifestText.IndexOf('{', k); if (b < 0) return null;
            int depth = 0;
            for (int i = b; i < _manifestText.Length; i++)
            {
                if (_manifestText[i] == '{') depth++;
                else if (_manifestText[i] == '}' && --depth == 0)
                {
                    var dd = JsonUtility.FromJson<DoorwayDoor>(_manifestText.Substring(b, i - b + 1))?.Normalise();
                    if (dd != null && string.IsNullOrEmpty(dd.kind)) dd.kind = id;
                    return dd != null && dd.floor_anchor_bg_px != null && dd.floor_anchor_local_px != null && dd.opening_local_px != null ? dd : null;
                }
            }
            return null;
        }
        /// <summary>Screen-space doorway background for this TOD (data: doorways_engine.json backgrounds[]); only while layered doors are up.</summary>
        public static Sprite ActiveBackground(TimeOfDay tod)
        {
            if (_inst == null || Engine?.backgrounds == null) return null;
            bool layered = false; foreach (var d in _inst._doors) if (d.Layered) layered = true;
            if (!layered) return null;
            string t = EntranceDoor.TodName(Engine, tod);
            foreach (var b in Engine.backgrounds) if (b != null && b.tod == t) return LoadKitSprite(b.file, "", t, null);
            return null;
        }
        public static TimeOfDay ActiveTod => _inst != null ? _inst._tod : TimeOfDay.Noon;

        public static Sprite LoadKitSprite(string pattern, string id, string tod, string state)
        {
            string path = pattern.Replace("{id}", id).Replace("{tod}", tod).Replace("{state}", state ?? "");
            if (_kitSprites.TryGetValue(path, out var sp)) return sp;
            sp = Resources.Load<Sprite>(path);
            if (sp == null) Debug.LogWarning("[Doors] kit sprite missing: " + path);
            _kitSprites[path] = sp;
            return sp;
        }

        /// <summary>Builds the closed doors for a stage (StorefrontDoor points only). Old doors are removed.</summary>
        /// <summary>v16 PvP: same western street, doorway kit drawn STATIC CLOSED (interior + closed leaves + front frame);
        /// no spawn / telegraph / animation, no enemy registration. Visual only.</summary>
        public static bool StaticClosed => _inst != null && _inst._static;
        public static void SetupStaticClosed(StreetLayoutData layout, TimeOfDay tod)
        {
            Setup(layout, tod, true);
        }
        public static void Setup(StreetLayoutData layout, TimeOfDay tod) => Setup(layout, tod, false);
        private static void Setup(StreetLayoutData layout, TimeOfDay tod, bool staticClosed)
        {
            Clear();
            if (staticClosed && Engine == null) return; // PvP: no kit -> nothing (no legacy single doors in PvP)
            if (layout?.spawnPoints == null || (Placement?.slots == null && Engine == null)) return;
            var go = new GameObject("EntranceDoors");
            _inst = go.AddComponent<EntranceDoors>(); _inst._tod = tod; _inst._static = staticClosed;
            var cam = Camera.main;
            float ref_h = Placement?.reference_resolution != null && Placement.reference_resolution.Length > 1 ? Placement.reference_resolution[1] : 1080f;
            float ref_w = Placement?.reference_resolution != null && Placement.reference_resolution.Length > 1 ? Placement.reference_resolution[0] : 1920f;
            float spritePx = Placement?.sprite_canvas != null && Placement.sprite_canvas.Length > 1 ? Placement.sprite_canvas[1] : 320f;
            foreach (var sp in layout.spawnPoints)
            {
                if (sp.type != SpawnPointType.StorefrontDoor) continue;
                var kd = KitDoor(sp.id);
                if (kd != null && cam != null && cam.orthographic)
                {
                    var head = ManifestHead;
                    float kh = head != null ? head.RefH : ref_h;
                    float kw = head != null ? head.RefW : ref_w;
                    float kupp = 2f * cam.orthographicSize / kh; var kc = cam.transform.position;
                    var aw = new Vector3(kc.x + (kd.floor_anchor_bg_px[0] - kw * 0.5f) * kupp, kc.y + (kh * 0.5f - kd.floor_anchor_bg_px[1]) * kupp, 0f);
                    var ld = new EntranceDoor(sp, Engine, kd, tod, go.transform, aw, kupp);
                    _inst._doors.Add(ld);
                    Debug.Log($"[Doors] {sp.id} LAYERED kit={Engine.status} kind={ld.Kind} tod={tod}/{EntranceDoor.TodName(Engine, tod)} anchorWorld=({aw.x:F2},{aw.y:F2}) layers={ld.LayerRenderers.Count} baseOrder={ld.BaseOrder} openingH={ld.OpeningHeightWorld * ld.PxPerWorld:F0}px insideStart=({ld.InsideStartWorld.x:F2},{ld.InsideStartWorld.y:F2}) cross=({ld.CrossWorld.x:F2},{ld.CrossWorld.y:F2}) legacyDoorDrawn=False");
                    continue;
                }
                if (staticClosed || Placement?.slots == null) continue;
                var slot = sp.side == SpawnSide.Right ? Placement.slots.right : Placement.slots.left;
                if (slot == null) continue;
                Vector3 logicPos = FieldView.ToWorld(sp.entranceStart);
                Vector3 pos = logicPos; float scale = 0.6f;
                if (cam != null && cam.orthographic && slot.anchor_bg_px != null && slot.anchor_bg_px.Length > 1)
                {
                    // designer slot px on the 1920x1080 reference frame -> world (orthographic camera keeps the plate height)
                    float upp = 2f * cam.orthographicSize / ref_h;
                    var c = cam.transform.position;
                    pos = new Vector3(c.x + (slot.anchor_bg_px[0] - ref_w * 0.5f) * upp, c.y + (ref_h * 0.5f - slot.anchor_bg_px[1]) * upp, 0f);
                    scale = spritePx * Placement.recommended_scale * upp / (spritePx / 100f); // sprite ppu 100
                }
                var door = new EntranceDoor(sp, string.IsNullOrEmpty(slot.default_kind) ? "saloon" : slot.default_kind, slot.flip_x, tod, go.transform, pos, scale);
                _inst._doors.Add(door);
                Debug.Log($"[Doors] {sp.id} kind={door.Kind} flip={door.Flip} tod={tod} world=({pos.x:F2},{pos.y:F2}) scale={scale:F3} entranceStartWorld=({logicPos.x:F2},{logicPos.y:F2}) frames={FrameCount}");
            }
            var pb = Object.FindFirstObjectByType<HillNGun.Scenery.ParallaxBackdrop>();
            if (pb != null) pb.RefreshDoorwayBackground(); // screen-space doorway BG replaces the old plate (painted tiny doors) while layered doors are up
        }

        public static void Clear()
        {
            if (_inst != null) { _inst._doors.Clear(); var pb0 = Object.FindFirstObjectByType<HillNGun.Scenery.ParallaxBackdrop>(); if (pb0 != null) pb0.RefreshDoorwayBackground(); }
            if (_inst != null) { if (Application.isPlaying) Destroy(_inst.gameObject); else DestroyImmediate(_inst.gameObject); }
            _inst = null;
        }

        public static EntranceDoor For(StageSpawnPoint sp)
        {
            if (_inst == null || sp == null) return null;
            foreach (var d in _inst._doors) if (d.Point == sp || d.Point.id == sp.id) return d;
            return null;
        }

        private void LateUpdate()
        {
            float dt = GameTime.DeltaTime;
            if (_static) return; // PvP: closed, never animated
            foreach (var d in _doors) d.Tick(dt);
        }

        private void OnDestroy() { if (_inst == this) _inst = null; }
    }
}

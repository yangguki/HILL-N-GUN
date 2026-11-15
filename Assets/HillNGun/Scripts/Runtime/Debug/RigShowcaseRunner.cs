using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Battlefield;
using HillNGun.Characters;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Feel;
using HillNGun.PvE;
using HillNGun.Rig;
using HillNGun.Input;

namespace HillNGun.DebugTools
{
    /// <summary>
    /// v13 rig / door evidence capture (desktop window, not a device): -rig-showcase with the FreeMove PvE smoke.
    /// Phase 1 (stage 1, from 2 s): eight Buster rig mannequins (MotionRig + CutoutRig, the same classes the hero view uses,
    /// no gameplay objects: they never shoot real bullets or take real damage) scripted through walk + aim sweep, run with
    /// walk-back shooting, roll, throw windup/release + hit, downed stars, and three static idle views, under a zoomed
    /// camera. Phase 2: the zoom follows storefront doors whenever a real enemy telegraph / exit animates them (gameplay
    /// events), otherwise the controlled hero. JPG frames at a fixed 30 fps game clock into scale_smoke/rigframes + PNG shots.
    /// </summary>
    public class RigShowcaseRunner : MonoBehaviour
    {
        private static bool _wanted;
        private static float _p1Secs = 33f, _p2Secs = 34f;
        private string _dir, _framesDir;
        private bool _started, _done;
        private int _frames;
        private Camera _zoom;
        private readonly List<Mannequin> _m = new List<Mannequin>();
        private readonly HashSet<string> _shots = new HashSet<string>();
        private float _t;
        private string _phase = "-";

        private sealed class Mannequin
        {
            public string label; public Transform root; public MotionRig motion; public CutoutRig rig; public Vector2 logic, home; public TextMesh caption;
            public Vector2 vel; public float facing = 1f; public bool downed, rolling; public float rollT = -1f; public Vector2 rollDir; public bool windup; public float nextShot, nextHit, nextThrow, nextRoll;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a.Equals("-rig-showcase", StringComparison.OrdinalIgnoreCase)) _wanted = true;
                if (a.Equals("-rig-throwtakes", StringComparison.OrdinalIgnoreCase)) { _wanted = true; _throwTakes = true; }
                if (a.Equals("-rig-throwsheet", StringComparison.OrdinalIgnoreCase)) { _wanted = true; _throwSheet = true; }
            }
            if (!_wanted) return;
            var go = new GameObject("[RigShowcaseRunner]"); DontDestroyOnLoad(go); go.AddComponent<RigShowcaseRunner>();
        }

        private void Awake()
        {
            _dir = Path.Combine(Application.persistentDataPath, "scale_smoke");
            _framesDir = Path.Combine(_dir, "rigframes");
            Directory.CreateDirectory(_framesDir);
            foreach (var f in Directory.GetFiles(_framesDir, "*.jpg")) File.Delete(f);
            Debug.Log("[RigShow] armed (desktop window, not a device)");
        }

        private void Update()
        {
            if (_done || !SceneManager.GetActiveScene().name.EndsWith("_Match")) return;
            var pve = FindFirstObjectByType<PvEMatchManager>();
            if (pve == null || !pve.IsMatchActive || !pve.IsFreeMove) return;
            if (_throwSheet)
            {
                if (!_started && pve.CurrentStageIndex == 0 && pve.StageTimer > 2f) { _started = true; Time.captureFramerate = 30; StartCoroutine(Record()); Debug.Log("[RigShow] throw SHEET (rendered rig check, NOT gameplay): 3 views x 3 heroes, off-field strip"); }
                if (!_started) return;
                _t += GameTime.DeltaTime; TickThrowSheet(); return;
            }
            if (_throwTakes)
            {
                if (!_started && pve.CurrentStageIndex == 0 && pve.StageTimer > 2f) { _started = true; Time.captureFramerate = 30; StartCoroutine(Record()); Debug.Log("[RigShow] throw takes start (one actor per take, fixed game camera)"); }
                if (!_started) return;
                _t += GameTime.DeltaTime; TickThrowTakes(pve); return;
            }
            if (!_started && pve.CurrentStageIndex == 0 && pve.StageTimer > 2f) StartShow(pve);
            if (!_started) return;
            float dt = GameTime.DeltaTime;
            _t += dt;
            if (_t < _p1Secs)
            {
                int seg = Mathf.Min(Chars.Length - 1, (int)(_t / SegSecs));
                if (seg != _seg) { foreach (var m in _m) Destroy(m.root.gameObject); _m.Clear(); _seg = seg; BuildMannequins(Chars[seg]); }
                _phase = "rig_" + Chars[_seg]; TickMannequins(dt); AimZoomAbs(_showCentre, 5.0f, true);
            }
            else
            {
                if (_m.Count > 0) { foreach (var m in _m) Destroy(m.root.gameObject); _m.Clear(); if (_backdrop != null) Destroy(_backdrop); _zoomInit = false; Debug.Log("[RigShow] phase 2: doors + gameplay"); }
                // v15 QA: doors on the UNZOOMED game camera (fixed, 1920x1080, the real gameplay framing) from before any warning
                // through closed -> swingA/B -> open + walk -> closed after exit; every state / depth change logged with its frame index.
                // Then each hero action (roll / grenade) in its own single fixed-camera take (camera locked where the action starts).
                if (_doorDoneT < 0f)
                {
                    _phase = "door_fixedcam"; if (_zoom != null) _zoom.enabled = false;
                    if (_doorCap == null) MakeDoorCaption();
                    string cap = "";
                    foreach (var d in EntranceDoors.Doors)
                    {
                        if (!_doorState.TryGetValue(d.Point.id, out var prevSt) || prevSt != d.State)
                        {
                            Debug.Log($"[RigShow] DOORSTATE {d.Point.id} {prevSt}->{d.State} frame={_frames} t={_t:F2}");
                            _doorState[d.Point.id] = d.State; if (d.State != DoorState.Closed) _doorActive = true;
                            Shot($"door_{d.Point.id}_{d.State}_{_frames:D4}");
                        }
                        cap += $"{d.Point.id}: {d.State}   ";
                    }
                    foreach (var ev in FindObjectsByType<EnemyView>(FindObjectsSortMode.None))
                    {
                        if (!ev.DoorInsideKnown) continue;
                        int key = ev.GetInstanceID(); bool ins = ev.DoorInside;
                        if (!_inside.TryGetValue(key, out bool was) || was != ins)
                        {
                            Debug.Log($"[RigShow] DOORDEPTH {ev.name} inside={ins} order={ev.DoorSortOrder} feetY={ev.DoorFeetY:F3} frame={_frames} t={_t:F2}");
                            _inside[key] = ins;
                            if (!ins) Shot($"door_cross_{_frames:D4}");
                        }
                        if (ins) cap += $"[enemy INSIDE z1 order {ev.DoorSortOrder}] ";
                    }
                    if (_doorCap != null) _doorCap.text = $"{cap} t={_t:F2}s  (fixed game camera 1920x1080, {EntranceDoors.KitStatus})";
                    bool allClosed = true; foreach (var d in EntranceDoors.Doors) if (d.State != DoorState.Closed) allClosed = false;
                    if (_doorActive && allClosed) { _qaClosedT += dt; if (_qaClosedT > 1.0f) { _doorDoneT = _t; Shot($"door_closed_afterExit_{_frames:D4}"); Debug.Log($"[RigShow] DOORSEQ done frame={_frames}"); if (_doorCap != null) Destroy(_doorCap.gameObject); } }
                    else _qaClosedT = 0f;
                    if (_t > _p1Secs + 22f && _doorDoneT < 0f) { _doorDoneT = _t; Debug.Log("[RigShow] DOORSEQ timeout (no door event)"); }
                }
                else
                {
                    _phase = "play_rollthrow";
                    var team = pve.PlayerTeam; var c = team.Count > pve.PlayerControlledIndex ? team[pve.PlayerControlledIndex] : null;
                    if (_focus != null && _focus.OverallState == CharacterOverallState.Active && _t < _nextAct) c = _focus;
                    else if (c != null && _act % 2 == 1 && !c.HasGrenadeReady) foreach (var m in team) if (m != null && m.OverallState == CharacterOverallState.Active && m.HasGrenadeReady) { c = m; break; }
                    if (c != null)
                    {
                        if (_t >= _nextAct) _takePos = FieldView.ToWorld(c.Position) + new Vector3(0f, 0.9f, 0f); // lock one camera per take
                        PlaceZoom(_takePos, Camera.main.orthographicSize * 0.42f);
                        DriveHero(c);
                    }
                }
                if (_t >= _p1Secs + _p2Secs) Finish();
            }
        }

        // ---- v17: rendered-rig throw check (-rig-throwsheet): diagnostic strip, NOT gameplay ----
        private static bool _throwSheet;
        private int _shSeg = -1; private readonly List<(CutoutRig rig, MotionRig motion, Transform root, Vector2 aim, string view)> _sh = new List<(CutoutRig, MotionRig, Transform, Vector2, string)>();
        private int _shCount; private GameObject _shBg; private TextMesh _shCap;
        private const float ShSeg = 4.2f, ShCycle = 1.4f;
        private void TickThrowSheet()
        {
            float dt = GameTime.DeltaTime;
            int seg = (int)(_t / ShSeg);
            if (seg >= Chars.Length) { Finish(); return; }
            const float OX = 70f;
            if (seg != _shSeg)
            {
                foreach (var m in _sh) Destroy(m.root.gameObject); _sh.Clear(); _shSeg = seg; _shCount = 0;
                if (_shBg == null)
                {
                    var bd = new GameObject("ThrowSheetBg").AddComponent<SpriteRenderer>();
                    var tex = new Texture2D(4, 4); var px = new Color[16]; for (int k = 0; k < 16; k++) px[k] = new Color(0.93f, 0.78f, 0.55f); tex.SetPixels(px); tex.Apply();
                    bd.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f); bd.sortingOrder = -2000; _shBg = bd.gameObject;
                    bd.transform.position = FieldView.ToWorld(new Vector2(OX + 4f, 5f)); bd.transform.localScale = new Vector3(40f, 24f, 1f);
                    var cg = new GameObject("ThrowSheetCap"); _shCap = cg.AddComponent<TextMesh>(); _shCap.fontSize = 48; _shCap.characterSize = 0.03f; _shCap.anchor = TextAnchor.UpperCenter; _shCap.color = Color.black;
                    cg.GetComponent<MeshRenderer>().sortingOrder = FieldView.OverlayOrder + 5;
                }
                var def = RigDefinition.Load(Chars[seg]); var md = MotionFx.Data;
                var views = new[] { ("side", Vector2.right), ("down_3q", new Vector2(0.35f, -1f)), ("up_3q", new Vector2(0.35f, 1f)) };
                for (int i = 0; i < 3; i++)
                {
                    var root = new GameObject("ThrowSheet_" + i).transform;
                    var mo = new MotionRig(root, -0.85f, new Vector2(1.1f, 1.1f * md.shadowAspect), md.heroArtFacesRight); mo.Renderer.enabled = false;
                    var rg = new CutoutRig(mo.Body, def, -0.85f, 2.35f);
                    FieldView.Place(root, new Vector2(OX + i * 3.2f, 4.2f), ScalePresets.VisualMul);
                    _sh.Add((rg, mo, root, views[i].Item2, views[i].Item1));
                }
            }
            float t = (_t - seg * ShSeg) % ShCycle; int cyc = (int)((_t - seg * ShSeg) / ShCycle);
            bool wind = t >= 0.3f && t < 0.75f; int count = cyc + (t >= 0.75f ? 1 : 0);
            string phase = t < 0.3f ? "gun aim" : wind ? $"pull-back/held {t - 0.3f:F2}s" : $"release+{t - 0.75f:F2}s";
            if (_shCap != null)
            {
                var c0 = (_sh[0].root.position + _sh[2].root.position) * 0.5f;
                _shCap.transform.position = c0 + new Vector3(0f, 3.4f, 0f);
                _shCap.text = $"RENDERED RIG CHECK (diagnostic strip, not gameplay)  {Chars[seg].ToUpperInvariant()}  side / down_3q / up_3q   {phase}";
                PlaceZoom(c0 + new Vector3(0f, 1.3f, 0f), 2.6f);
            }
            foreach (var m in _sh)
            {
                m.motion.SetFacing(1f);
                m.motion.SortingOrder = FieldView.DepthOrder(4.2f);
                m.motion.Tick(dt, Vector2.zero, 5f, true, 0f, 0f, 0f, 0f, true);
                m.rig.Tick(dt, new RigInput { aim = m.aim, aiming = !wind && t < 0.3f, maxSpeed = 5f, facing = 1f, forceSide = m.view == "side", sortingOrder = FieldView.DepthOrder(4.2f), grenadeAiming = wind, throwCount = count, throwCountValid = true });
            }
            var r0 = _sh[0].rig; var ua = r0.Bone("side", "upper_arm_r"); var hr = r0.Bone("side", "hand_r");
            if (_frames % 2 == 0) Debug.Log($"[RigShow] SHEET {Chars[seg]} f={_frames} t={t:F2} {phase} views=[{_sh[0].rig.ViewName},{_sh[1].rig.ViewName},{_sh[2].rig.ViewName}] side ua_r={ua.localEulerAngles.z:F0} hand_r=({hr.position.x - _sh[0].root.position.x:F2},{hr.position.y - _sh[0].root.position.y:F2}) bomb={r0.HeldBombVisible} holster={r0.WeaponHolstered} armFront={r0.ThrowArmInFront}");
        }

        // ---- v16/v17: isolated ACTUAL gameplay throw takes (-rig-throwtakes) ----
        // v17: each take's actor has its autopilot FIRE/SKILL/GRENADE/MOVE held (AbilityPilot.DiagHoldActor, diagnostic flag,
        // null in normal play) so only the scripted command is visible. Kinds: player = real player commands (hold 0.6 s,
        // immediate release); ai = the common AI path BeginAiGrenadeThrow (0.15 s locked windup); aicancel = AI windup
        // interrupted by a roll command at +0.07 s (no spawn, grenade kept).
        private static bool _throwTakes;
        private static readonly (string id, string kind)[] Takes = { ("buster", "player"), ("buster", "ai"), ("raven", "player"), ("raven", "ai"), ("volt", "player"), ("volt", "ai"), ("volt", "aicancel") };
        private int _ti, _tkPhase; private float _tkT, _tkWait; private CharacterStateMachine _tkActor; private int _tkRel0, _tkSpawn0, _tkSpawns, _tkCancel0;
        private int _dynShots; private float _dynShotT = -9f;
        private void TryDynShot(float now)
        {
            if (_dynShots >= 2 || _frames % 5 != 0) return;
            foreach (var ev in FindObjectsByType<HillNGun.PvE.EnemyView>(FindObjectsSortMode.None))
                if (ev.HeldDynamiteVisible && ev.HeldDynamiteAge > 0.35f && now - _dynShotT > 3f) { _dynShotT = now; Shot($"enemy_dynamite_windup_{_dynShots++}"); Debug.Log($"[RigShow] ENEMY held dynamite visible frame={_frames} age={ev.HeldDynamiteAge:F2} (existing EnemyData.warningTime telegraph)"); break; }
        }
        private TextMesh _tkCap; private bool _tkHooked; private int _tkPost; private Vector2 _tkDrag, _tkDir;
        private void TickThrowTakes(PvEMatchManager pve)
        {
            float dt = GameTime.DeltaTime; float now = Time.time;
            if (_zoom != null) _zoom.enabled = false;
            if (!_tkHooked) { var cs = ServiceLocator.Get<HillNGun.Combat.CombatSystem>(); if (cs != null) { cs.OnGrenadeThrown += (g, st) => _tkSpawns++; _tkHooked = true; } }
            if (_tkCap == null)
            {
                var main = Camera.main; var go = new GameObject("ThrowTakeCaption"); go.transform.position = main.transform.position + new Vector3(0f, main.orthographicSize * 0.93f, 10f);
                _tkCap = go.AddComponent<TextMesh>(); _tkCap.fontSize = 48; _tkCap.characterSize = 0.04f; _tkCap.anchor = TextAnchor.UpperCenter; _tkCap.color = Color.white;
                go.GetComponent<MeshRenderer>().sortingOrder = FieldView.OverlayOrder + 6;
            }
            if (_ti >= Takes.Length || _t > 110f)
            {
                PlaceholderCharacterView.DebugSoloActor = null; HillNGun.AI.AbilityPilot.DiagHoldActor = null;
                if (_dynShots < 2 && _t < 110f) { _tkCap.text = "v17 PvE dynamite bandit: held lit dynamite during the EXISTING 1 s warning (view only)"; TryDynShot(Time.time); return; }
                Finish(); return;
            }
            string id = Takes[_ti].id, kind = Takes[_ti].kind, tag = id + "_" + kind;
            TryDynShot(now);

            if (_tkPhase == 0)
            {
                _tkActor = null;
                foreach (var m in pve.PlayerTeam) if (m != null && m.Data != null && m.Data.id == id) _tkActor = m;
                if (_tkActor == null) { Debug.Log($"[RigShow] THROWTAKE {tag} not in team -> skip"); _ti++; return; }
                PlaceholderCharacterView.DebugSoloActor = _tkActor; // other hero views parked off screen (view only)
                HillNGun.AI.AbilityPilot.DiagHoldActor = _tkActor;   // DIAGNOSTIC: autopilot fire/skill/move off for this actor
                _tkWait += dt;
                if (_tkActor.ActionState == ActionState.Aiming || _tkActor.ActionState == ActionState.Charging) _tkActor.OnAimReleaseCommand(new AimReleaseCommand(_tkActor.AimDirection, false, now)); // held autopilot left the trigger down
                bool ok = _tkActor.OverallState == CharacterOverallState.Active && _tkActor.HasGrenadeReady && _tkActor.MovementState != MovementState.Rolling && _tkActor.ActionState == ActionState.None;
                _tkCap.text = $"v17 THROW TAKE {id.ToUpperInvariant()} [{kind}]  one actor, autopilot fire/skill OFF (diag)  fixed game camera  waiting ready {_tkWait:F1}s";
                if (ok && _tkWait > 1.0f && _tkWait < 20f)
                {   // isolation: no enemy figure overlapping the take actor on screen (fading / entering enemies included)
                    Vector3 me = FieldView.ToWorld(_tkActor.Position);
                    foreach (var ev in FindObjectsByType<HillNGun.PvE.EnemyView>(FindObjectsSortMode.None))
                        if (Mathf.Abs(ev.transform.position.x - me.x) < 2.2f && Mathf.Abs(ev.transform.position.y - me.y) < 2.6f) { ok = false; break; }
                }
                if (ok && _tkWait > 1.0f)
                {
                    _tkRel0 = _tkActor.GrenadesReleased; _tkSpawn0 = _tkSpawns; _tkCancel0 = _tkActor.AiGrenadeCancels;
                    _tkDir = new Vector2(_tkActor.Position.x < 12f ? 1f : -1f, 0f); // sideways toward the field centre: side view, overhead arc reads
                    if (kind == "player")
                    {
                        _tkActor.OnAimReleaseCommand(new AimReleaseCommand(_tkActor.AimDirection, false, now));
                        _tkActor.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, now));
                        _tkDrag = _tkDir; _tkActor.OnGrenadeAimDragCommand(new GrenadeAimDragCommand(_tkDrag, 6f, now));
                    }
                    else
                    {
                        _tkDrag = Vector2.zero;
                        bool began = _tkActor.BeginAiGrenadeThrow(_tkActor.Position + _tkDir * 6f, RuntimeAssetManager.Instance?.BotBehavior != null ? RuntimeAssetManager.Instance.BotBehavior.aiGrenadeWindup : 0.15f);
                        Debug.Log($"[RigShow] THROWTAKE {tag} BeginAiGrenadeThrow={began} windupLeft={_tkActor.AiGrenadeWindupLeft:F2}");
                    }
                    Debug.Log($"[RigShow] THROWTAKE {tag} start frame={_frames} t={_t:F2} state={_tkActor.ActionState} aim={_tkActor.AimDirection} waited={_tkWait:F1}s pos={_tkActor.Position} screen={Camera.main.WorldToScreenPoint(FieldView.ToWorld(_tkActor.Position))}");
                    _tkPhase = 1; _tkT = 0f;
                }
                else if (_tkWait > 25f) { Debug.Log($"[RigShow] THROWTAKE {tag} timeout (ready={_tkActor.HasGrenadeReady} action={_tkActor.ActionState} move={_tkActor.MovementState} overall={_tkActor.OverallState})"); _ti++; _tkWait = 0f; }
                return;
            }
            _tkT += dt;
            var rig = FindRig(_tkActor);
            float hold = kind == "player" ? 0.6f : 0.15f;
            _tkCap.text = $"v17 THROW TAKE {id.ToUpperInvariant()} [{kind}{(kind == "player" ? " hold 0.6s" : " 0.15s windup")}]  {(_tkPhase == 1 ? "pull-back + held bomb" : "release -> overhead arc -> flight")}  t={_tkT:F2}s";
            if (_tkPhase == 1)
            {
                if (_tkDrag != Vector2.zero && _tkActor.ActionState == ActionState.GrenadeAiming) _tkActor.OnGrenadeAimDragCommand(new GrenadeAimDragCommand(_tkDrag, 6f, now));
                if (kind == "player")
                {
                    if (_tkT >= 0.05f && _tkT < 0.08f) Shot($"throw_{tag}_pull05");
                    if (_tkT >= 0.15f && _tkT < 0.18f) Shot($"throw_{tag}_pull15");
                    if (_tkT >= 0.45f && _tkT < 0.48f) Shot($"throw_{tag}_held45");
                }
                else if (_tkT >= 0.06f && _tkT < 0.09f) Shot($"throw_{tag}_windup07");
                if (_frames % 2 == 0) Debug.Log($"[RigShow] THROWTAKE {tag} hold t={_tkT:F2} view={rig?.ViewName} state={_tkActor.ActionState} aiWind={_tkActor.AiGrenadeWindingUp} pull={rig?.PullbackWeight:F2} bomb={rig?.HeldBombVisible} holster={rig?.WeaponHolstered} spawns+={_tkSpawns - _tkSpawn0} frame={_frames}");
                if (kind == "aicancel" && _tkT >= 0.07f)
                {
                    _tkActor.OnRollCommand(new RollCommand(new Vector2(0f, -1f), now)); // toward the camera: stays clear of cover
                    Debug.Log($"[RigShow] THROWTAKE {tag} roll at t={_tkT:F2} -> cancels={_tkActor.AiGrenadeCancels - _tkCancel0} reason={_tkActor.LastAiGrenadeCancel} spawns+={_tkSpawns - _tkSpawn0} ready={_tkActor.HasGrenadeReady} frame={_frames}");
                    _tkPhase = 2; _tkT = 0f; _tkPost = 0; return;
                }
                if (kind == "ai" && _tkT >= hold - 0.001f && _tkActor.GrenadesReleased > _tkRel0)
                {
                    Debug.Log($"[RigShow] THROWTAKE {tag} AI release after {_tkT:F3}s frame={_frames} spawns+={_tkSpawns - _tkSpawn0} target={_tkActor.LastGrenadeReleaseTarget}");
                    Shot($"throw_{tag}_release"); _tkPhase = 2; _tkT = 0f; _tkPost = 0; return;
                }
                if (kind != "player" && _tkT > 0.5f) { Debug.Log($"[RigShow] THROWTAKE {tag} AI windup did not release (state={_tkActor.ActionState})"); _tkPhase = 2; _tkT = 0f; return; }
                if (kind == "player" && _tkActor.ActionState != ActionState.GrenadeAiming && _tkT > 0.05f) { Debug.Log($"[RigShow] THROWTAKE {tag} aim ended by game (state={_tkActor.ActionState}) frame={_frames}"); _tkPhase = 2; _tkT = 0f; return; }
                if (kind == "player" && _tkT >= hold)
                {
                    Vector2 tgt = _tkActor.Position + (_tkActor.AimDirection.sqrMagnitude > 0.01f ? _tkActor.AimDirection.normalized : Vector2.right) * 6f;
                    _tkActor.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(tgt, true, now));
                    Debug.Log($"[RigShow] THROWTAKE {tag} release frame={_frames} t={_t:F2} released+={_tkActor.GrenadesReleased - _tkRel0} spawns+={_tkSpawns - _tkSpawn0} bombAfter={rig?.HeldBombVisible} arcT={rig?.ThrowArcTime:F2}");
                    Shot($"throw_{tag}_release");
                    _tkPhase = 2; _tkT = 0f; _tkPost = 0;
                }
                return;
            }
            if (_tkPost < 3) { _tkPost++; Debug.Log($"[RigShow] THROWTAKE {tag} post-release tick{_tkPost} frame={_frames} bomb={rig?.HeldBombVisible} arcT={rig?.ThrowArcTime:F3} view={rig?.ViewName} arcs={CutoutRig.ThrowArcs} bridges={HillNGun.Combat.GrenadeView.BridgesStarted}/{HillNGun.Combat.GrenadeView.BridgesLive}"); }
            if (kind != "aicancel")
            {
                if (_tkT >= 0.03f && _tkT < 0.06f) Shot($"throw_{tag}_bridge03");
                if (_tkT >= 0.10f && _tkT < 0.13f) Shot($"throw_{tag}_arc10");
                if (_tkT >= 0.20f && _tkT < 0.23f) Shot($"throw_{tag}_arc20");
            }
            else if (_tkT >= 0.10f && _tkT < 0.13f) Shot($"throw_{tag}_roll10");
            if (_tkT >= 1.6f)
            {
                Debug.Log($"[RigShow] THROWTAKE {tag} done released+={_tkActor.GrenadesReleased - _tkRel0} spawns+={_tkSpawns - _tkSpawn0} cancels+={_tkActor.AiGrenadeCancels - _tkCancel0} ready={_tkActor.HasGrenadeReady} frame={_frames}");
                _ti++; _tkPhase = 0; _tkWait = 0f;
            }
        }
        private static HillNGun.Rig.CutoutRig FindRig(CharacterStateMachine c)
        {
            foreach (var v in FindObjectsByType<PlaceholderCharacterView>(FindObjectsSortMode.None)) if (v.Character == c) return v.Cutout;
            return null;
        }

        private readonly Dictionary<string, DoorState> _doorState = new Dictionary<string, DoorState>();
        private readonly Dictionary<int, bool> _inside = new Dictionary<int, bool>();
        private bool _doorActive; private Vector3 _takePos;
        private void MakeDoorCaption()
        {
            var main = Camera.main; var go = new GameObject("DoorQACaption");
            go.transform.position = main.transform.position + new Vector3(0f, main.orthographicSize * 0.93f, 10f);
            _doorCap = go.AddComponent<TextMesh>(); _doorCap.fontSize = 48; _doorCap.characterSize = 0.045f; _doorCap.anchor = TextAnchor.UpperCenter; _doorCap.color = Color.white;
            go.GetComponent<MeshRenderer>().sortingOrder = FieldView.OverlayOrder + 6;
        }
        private Vector3 _zoomPos, _showCentre; private float _zoomSize, _doorSince = -1f; private bool _zoomInit; private GameObject _backdrop; private EntranceDoor _qaDoor; private float _qaDoorT, _qaClosedT, _doorDoneT = -1f, _lockSize, _nextAct; private Vector3 _lockPos; private TextMesh _doorCap; private int _act;

        private void LockDoorCam(EntranceDoor d)
        {
            var main = Camera.main;
            Vector3 dp = d.Renderer.transform.position;
            Vector3 ep = FieldView.ToWorld(d.Point.position);
            _lockPos = Vector3.Lerp(dp, ep, 0.35f) + new Vector3(0f, 0.8f, 0f); _lockPos.z = main.transform.position.z;
            _lockSize = main.orthographicSize * 0.34f;
            var go = new GameObject("DoorQACaption"); go.transform.position = new Vector3(_lockPos.x, _lockPos.y + _lockSize * 0.82f, 0f);
            _doorCap = go.AddComponent<TextMesh>(); _doorCap.fontSize = 48; _doorCap.characterSize = _lockSize * 0.012f; _doorCap.anchor = TextAnchor.UpperCenter; _doorCap.color = Color.white;
            go.GetComponent<MeshRenderer>().sortingOrder = FieldView.OverlayOrder + 6;
        }

        private void PlaceZoom(Vector3 pos, float size)
        {
            var main = Camera.main; if (main == null) return;
            if (_zoom == null)
            {
                var go = new GameObject("RigShowZoomCam"); _zoom = go.AddComponent<Camera>();
                _zoom.CopyFrom(main); _zoom.depth = main.depth + 20; _zoom.clearFlags = CameraClearFlags.SolidColor; _zoom.backgroundColor = Color.black;
            }
            pos.z = main.transform.position.z; _zoom.transform.position = pos; _zoom.orthographicSize = size; _zoom.enabled = true;
        }

        /// <summary>Debug-only (-rig-showcase): real gameplay commands on the hero: alternate roll and grenade windup/throw.</summary>
        private void DriveHero(CharacterStateMachine c)
        {
            float now = Time.time;
            if (_t < _nextAct) { if (_act % 2 == 0 && _t > _nextAct - 1.6f && c.ActionState == ActionState.GrenadeAiming) Release(c, now); if (_rollShotAt > 0f && _t >= _rollShotAt) { _rollShotAt = -1f; Shot($"play_rollmid_{_act}"); } return; }
            // grenade take: wait (max 3 s) for a real auto-aim target to the SIDE so the windup / release reads in the side view
            if ((_act + 1) % 2 == 0 && c.HasGrenadeReady && (Mathf.Abs(c.AimDirection.x) < 0.8f * c.AimDirection.magnitude || Crowded(c)) && _sideWait < 4f) { _sideWait += GameTime.DeltaTime; return; }
            if ((_act + 1) % 2 == 0) Debug.Log($"[RigShow] grenade actor={c.Data.id} aim dir={c.AimDirection} crowded={Crowded(c)} waited={_sideWait:F2}s");
            _sideWait = 0f;
            _act++;
            if (_act % 2 == 1 && c.MovementState != MovementState.Rolling)
            {
                Vector2 dir = c.Position.x < 12f ? Vector2.right : Vector2.left;
                c.OnRollCommand(new RollCommand(dir, now)); Debug.Log($"[RigShow] TAKE roll_{_act} start frame={_frames} actor={c.Data.id} t={_t:F2} dir={dir} cam={_takePos} size={Camera.main.orthographicSize * 0.42f:F2}");
                Shot($"play_roll_{_act}"); _rollShotAt = _t + 0.2f;
            }
            else if (_act % 2 == 0 && c.HasGrenadeReady)
            {
                Debug.Log($"[RigShow] grenade by {c.Data.id}");
                c.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, now)); Debug.Log($"[RigShow] TAKE grenade_{_act} start(windup) frame={_frames} actor={c.Data.id} t={_t:F2} state={c.ActionState} cam={_takePos}");
            }
            _nextAct = _t + 2.2f; _focus = c;
        }
        private CharacterStateMachine _focus; private float _sideWait;
        private bool Crowded(CharacterStateMachine c)
        {   // one-actor take: no teammate overlapping the thrower
            var pve = FindFirstObjectByType<PvEMatchManager>(); if (pve == null) return false;
            foreach (var m in pve.PlayerTeam) if (m != null && m != c && m.OverallState == CharacterOverallState.Active && (m.Position - c.Position).magnitude < 2.0f) return true;
            return false;
        }
        private float _rollShotAt = -1f;
        private void Release(CharacterStateMachine c, float now)
        {
            Vector2 tgt = c.Position + (c.AimDirection.sqrMagnitude > 0.01f ? c.AimDirection.normalized : Vector2.up) * 6f; // toward the real auto-aim target
            c.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(tgt, true, now)); Debug.Log($"[RigShow] TAKE grenade_{_act} release frame={_frames} t={_t:F2}");
            Shot($"play_throw_{_act}");
        }
        private void AimZoomAbs(Vector3 target, float size, bool snap)
        {
            var main = Camera.main; if (main == null) return;
            AimZoom(target, size / Mathf.Max(0.01f, main.orthographicSize));
        }
        private void AimZoom(Vector3 target, float sizeMul)
        {
            var main = Camera.main; if (main == null) return;
            if (_zoom == null)
            {
                var go = new GameObject("RigShowZoomCam"); _zoom = go.AddComponent<Camera>();
                _zoom.CopyFrom(main); _zoom.depth = main.depth + 20; _zoom.clearFlags = CameraClearFlags.SolidColor; _zoom.backgroundColor = Color.black;
            }
            float size = main.orthographicSize * sizeMul;
            target.z = main.transform.position.z;
            float k = _zoomInit ? 1f - Mathf.Exp(-GameTime.DeltaTime * 6f) : 1f;
            _zoomPos = Vector3.Lerp(_zoomPos, target, k); _zoomSize = Mathf.Lerp(_zoomSize, size, k); _zoomInit = true;
            _zoom.transform.position = _zoomPos; _zoom.orthographicSize = _zoomSize;
        }

        private Vector3 MannequinCentre() { var c = Vector3.zero; foreach (var m in _m) c += m.root.position; return _m.Count > 0 ? c / _m.Count + new Vector3(0f, 0.9f, 0f) : Vector3.zero; }

        private void StartShow(PvEMatchManager pve)
        {
            _started = true;
            var md = MotionFx.Data;
            _labels = new[] { "walk + aim sweep (view switch, .08s crossfade)", "run + walk-back shooting", "roll (curl + tumble)", "throw windup/release + hit", "downed (stars)", "idle down_3q", "idle side", "idle up_3q" };
            // off-field showcase strip (logic x + 70, plain sand backdrop): no covers / actors overlap the mannequins
            const float OX = 70f;
            _pos = new[] { new Vector2(OX + 0f, 8.0f), new Vector2(OX + 5.5f, 7.2f), new Vector2(OX + 11f, 7.2f), new Vector2(OX + 0f, 3.4f), new Vector2(OX + 3.6f, 3.4f), new Vector2(OX + 6.6f, 3.4f), new Vector2(OX + 9.0f, 3.4f), new Vector2(OX + 11.4f, 3.4f) };
            var bd = new GameObject("RigShowBackdrop").AddComponent<SpriteRenderer>();
            var tex = new Texture2D(4, 4); var px = new Color[16]; for (int k = 0; k < 16; k++) px[k] = new Color(0.93f, 0.78f, 0.55f); tex.SetPixels(px); tex.Apply();
            bd.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            bd.sortingOrder = -2000; _backdrop = bd.gameObject;
            Vector3 c0 = FieldView.ToWorld(new Vector2(OX + 5.7f, 5.6f)); bd.transform.position = c0; bd.transform.localScale = new Vector3(40f, 24f, 1f);
            _showCentre = c0 + new Vector3(0f, 0.3f, 0f);
            _seg = -1;
            Time.captureFramerate = 30;
            StartCoroutine(Record());
            Debug.Log($"[RigShow] start stage={pve.CurrentStageId} t={pve.StageTimer:F1}s chars={string.Join(",", Chars)} activeRig(gameplay views)={PlaceholderCharacterView.ActiveCutoutRigs}");
        }

        private static readonly string[] Chars = { "buster", "raven", "volt" };
        private const float SegSecs = 11f;
        private int _seg = -1; private string[] _labels; private Vector2[] _pos;

        private void BuildMannequins(string id)
        {
            var def = RigDefinition.Load(id);
            if (def == null) { Debug.LogError("[RigShow] no rig for " + id); return; }
            var md = MotionFx.Data; var labels = _labels; var pos = _pos;
            for (int i = 0; i < labels.Length; i++)
            {
                var m = new Mannequin { label = labels[i], logic = pos[i], home = pos[i] };
                m.root = new GameObject("RigMannequin_" + i).transform;
                m.motion = new MotionRig(m.root, -0.85f, new Vector2(1.1f, 1.1f * md.shadowAspect), md.heroArtFacesRight);
                m.motion.Renderer.enabled = false;
                m.motion.WholeBodyScale = RigClipSet.Shared.settings.wholeBodyMotionScale;
                m.rig = new CutoutRig(m.motion.Body, def, -0.85f, 2.35f);
                var cap = new GameObject("cap"); cap.transform.SetParent(m.root, false); cap.transform.localPosition = new Vector3(0f, -1.25f, 0f);
                m.caption = cap.AddComponent<TextMesh>(); m.caption.text = (i == 0 ? id.ToUpperInvariant() + " (" + def.secondaryPart + " spring, IK " + (def.View("side").supportIK ? "on" : "off: one-hand") + ")\n" : "") + labels[i]; m.caption.fontSize = 40; m.caption.characterSize = 0.035f; m.caption.anchor = TextAnchor.UpperCenter; m.caption.color = Color.white;
                cap.GetComponent<MeshRenderer>().sortingOrder = FieldView.OverlayOrder + 5;
                m.downed = i == 4;
                _m.Add(m);
            }
            Debug.Log($"[RigShow] segment {id}: mannequins={_m.Count} skinsEach={_m[0].rig.SkinCount} valid={_m[0].rig.AllSkinsValid} status={def.status}");
        }

        private void TickMannequins(float dt)
        {
            float t = _t - Mathf.Max(0, _seg) * SegSecs; string cid = _seg >= 0 ? Chars[_seg] : "-";
            for (int i = 0; i < _m.Count; i++)
            {
                var m = _m[i];
                Vector2 aim = Vector2.right; bool aiming = false; Vector2 vel = Vector2.zero; float spin = 0f;
                switch (i)
                {
                    case 0: // circle walk, aim sweeps a full turn every 6 s, shooting
                        { float a = t * 0.9f; vel = new Vector2(-Mathf.Sin(a), Mathf.Cos(a)) * 1.4f; float s = t * Mathf.PI * 2f / 6f; aim = new Vector2(Mathf.Cos(s), Mathf.Sin(s)); aiming = true; if (t > m.nextShot) { m.rig.Shoot(); m.nextShot = t + 0.4f; } break; }
                    case 1: // run back and forth, aim stays right: walking left = walk-back shooting
                        { float ph = Mathf.Repeat(t, 3.2f); vel = new Vector2(ph < 1.6f ? 4.6f : -4.6f, 0f); aim = Vector2.right; aiming = true; if (t > m.nextShot) { m.rig.Shoot(); m.nextShot = t + 0.3f; } break; }
                    case 2: // roll every 1.6 s
                        {
                            if (!m.rolling && t > m.nextRoll) { m.rolling = true; m.rollT = 0f; m.rollDir = new Vector2(Mathf.Sign(Mathf.Sin(t)) , 0f); if (m.rollDir.x == 0f) m.rollDir.x = 1f; m.nextRoll = t + 1.6f; }
                            if (m.rolling) { m.rollT += dt / 0.4f; vel = m.rollDir * 6f; spin = 360f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m.rollT)); if (m.rollT >= 1f) m.rolling = false; }
                            aim = new Vector2(m.facing, 0f); break;
                        }
                    case 3: // windup 0.9 s -> release, hit flinch every 2.4 s
                        { float ph = Mathf.Repeat(t, 2.0f); m.windup = ph < 0.9f; aim = new Vector2(1f, 0.2f); if (t > m.nextHit) { m.rig.Hit(); m.nextHit = t + 2.4f; } break; }
                    case 5: aim = new Vector2(1f, -1.6f); aiming = true; break;
                    case 6: aim = Vector2.right; aiming = true; break;
                    case 7: aim = new Vector2(1f, 1.6f); aiming = true; break;
                }
                // keep the walkers near home (logic space), flip by velocity unless aiming
                m.logic += vel * dt;
                if ((m.logic - m.home).magnitude > 2.6f) m.logic = m.home + (m.logic - m.home).normalized * 2.6f;
                if (aiming && Mathf.Abs(aim.x) > 0.2f) m.facing = Mathf.Sign(aim.x);
                else if (Mathf.Abs(vel.x) > 0.3f) m.facing = Mathf.Sign(vel.x);
                m.motion.SetFacing(m.facing);
                FieldView.Place(m.root, m.logic, ScalePresets.VisualMul);
                int order = FieldView.DepthOrder(m.logic.y);
                m.motion.SortingOrder = order;
                m.motion.Tick(dt, m.rolling ? Vector2.zero : vel, 5f, !m.rolling, MotionFx.Data.bobHeight, MotionFx.Data.leanMaxDeg, 0f, spin, true);
                m.rig.SetColor(HillNGun.Scenery.TimeOfDayGrade.Apply(Color.white));
                m.rig.Tick(dt, new RigInput { velocity = vel, maxSpeed = 5f, aim = aim, aiming = aiming, rolling = m.rolling, rollProgress = Mathf.Clamp01(m.rollT), downed = m.downed, grenadeAiming = m.windup, facing = m.motion.FacingSign, sortingOrder = order });
                m.motion.ShadowMul = m.rig.ShadowMul;
            }
            foreach (var s in new[] { 1.0f, 3.0f, 5.2f, 7.5f, 10.0f }) if (t >= s) Shot($"rig_{cid}_t{s:F1}");
        }

        private void Shot(string key)
        {
            if (_shots.Contains(key)) return;
            _shots.Add(key);
            string path = Path.Combine(_dir, $"rigshow_{key}_{Screen.width}x{Screen.height}.png");
            ScreenCapture.CaptureScreenshot(path);
            SmokeTestRunner.MarkShotFrame();
            string views = ""; foreach (var m in _m) views += m.rig.ViewName + (m.rig.Crossfading ? "*" : "") + " ";
            Debug.Log($"[RigShow] SHOT {key} t={_t:F2} phase={_phase} views=[{views.Trim()}]");
        }

        private IEnumerator Record()
        {
            while (!_done)
            {
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(_framesDir, $"rig_{_frames:D4}.jpg"), tex.EncodeToJPG(72));
                Destroy(tex);
                _frames++;
                if (_frames % 90 == 0) Debug.Log($"[RigShow] frames={_frames} t={_t:F1} phase={_phase}");
            }
        }

        private void Finish()
        {
            if (_done) return;
            _done = true;
            Time.captureFramerate = 0;
            Debug.Log($"[RigShow] REC done frames={_frames} span={_t:F1}s doors={EntranceDoors.Doors.Count} -> {_framesDir}");
            StartCoroutine(QuitSoon());
        }
        private IEnumerator QuitSoon() { yield return new WaitForSecondsRealtime(1f); Application.Quit(); }
    }
}

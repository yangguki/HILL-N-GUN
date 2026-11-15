using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D.Animation;

namespace HillNGun.Rig
{
    public struct RigInput
    {
        public Vector2 velocity;      // logic units / s
        public float maxSpeed;
        public Vector2 aim;           // logic aim direction
        public bool aiming;           // shooting / aiming weapon this frame
        public bool rolling;
        public float rollProgress;    // 0..1
        public bool downed;
        public bool grenadeAiming;
        /// <summary>v16: logic grenade release counter (CharacterStateMachine.GrenadesReleased). When valid, the overhead throw
        /// arc + held-bomb hand-off fire exactly when it changes (logic release); otherwise (mannequins) on windup end.</summary>
        public int throwCount; public bool throwCountValid;
        public bool forceSide;        // PvP: fixed forward aim, side view only (visual)
        public float facing;          // +1 right / -1 left (MotionRig facing)
        public int sortingOrder;
    }

    /// <summary>
    /// v13 articulated cutout rig (Unity 2D Animation): per view a bone hierarchy built from <see cref="RigDefinition"/> and
    /// one SpriteRenderer + <see cref="SpriteSkin"/> per designer part (each part sprite carries one bone + bindpose from
    /// RigSetup). Views (down_3q / side / up_3q, mirrored by the MotionRig flip) are chosen from the aim and crossfaded.
    /// Lower body plays locomotion (walk / run, backwards when moving against the facing), upper body follows the aim
    /// (gun arm rotation, torso / head share), support hand pinned to the barrel with a 2-bone IK where the data has a
    /// forend point; shoot / hit additive, roll / down full-body; hat / scarf springs; stars when downed.
    /// View only: no logic / hitbox / stat reads besides the inputs.
    /// </summary>
    public sealed class CutoutRig
    {
        private sealed class View
        {
            public RigView data;
            public RigViewSettings vs;
            public Transform root;
            public SortingGroup group;
            public readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
            public readonly Dictionary<string, Vector3> rest = new Dictionary<string, Vector3>();
            public SpriteRenderer[] renderers;
            public int[] baseOrder; public int[] throwArm; public bool armFront;
            public SpriteRenderer heldBomb; public int weaponIdx = -1; public bool holstered; public Vector2 wpPos, fePos; public float wpDeg; public bool wpValid;
            public SpriteSkin[] skins;
            public Transform forend;
            public Vector2 wristFromForend;   // rest (root space)
            public float weaponRestDeg;
            public float alpha;
            public Color lastColor = new Color(-1f, -1f, -1f, -1f);
        }

        public readonly Transform Root;
        public readonly RigDefinition Def;
        private readonly RigClipSet _clips;
        private readonly RigSettings S;
        private readonly View[] _views;
        private readonly float _k;
        private int _cur = 1, _prev = -1;
        private float _fade = 1f, _viewHold;
        private float _phase, _walkW, _runW, _fullRollW, _downW, _windupW;
        private float _shootT = 99f, _hitT = 99f, _throwT = 99f, _idleT;
        private bool _wasWindup;
        private float _aimDelta;
        private float _hatA, _hatV, _scarfA, _scarfV;
        private Vector2 _lastVel;
        private Color _color = Color.white;
        private SpriteRenderer[] _stars;
        private float _starT;
        private readonly Dictionary<string, Vector3> _pose = new Dictionary<string, Vector3>(); // bone -> (rot, x, y)

        public int ViewIndex => _cur;
        public float GaitPhase => _phase;
        public string ViewName => _views[_cur].data.name;
        public float CrossfadeT => _fade;
        public bool Crossfading => _prev >= 0;
        public int SkinCount { get { int n = 0; foreach (var v in _views) if (v.skins != null) n += v.skins.Length; return n; } }
        public int ActiveRendererCount { get { int n = 0; foreach (var v in _views) if (v.root.gameObject.activeSelf) n += v.renderers.Length; return n; } }
        public bool AllSkinsValid { get; private set; }
        public Vector2 ShadowMul { get; private set; } = Vector2.one;
        public bool StarsVisible => _stars != null && _stars[0].enabled;
        public float IkError { get; private set; }
        public Transform Bone(string view, string bone) { foreach (var v in _views) if (v.data.name == view && v.bones.TryGetValue(bone, out var t)) return t; return null; }
        public SpriteRenderer[] Renderers(string view) { foreach (var v in _views) if (v.data.name == view) return v.renderers; return null; }

        private static readonly Dictionary<string, Dictionary<string, Sprite>> _atlasCache = new Dictionary<string, Dictionary<string, Sprite>>();
        private static Dictionary<string, Sprite> Atlas(RigDefinition def)
        {
            string key = "Rig/" + System.IO.Path.GetFileNameWithoutExtension(def.atlas);
            if (_atlasCache.TryGetValue(key, out var m)) return m;
            m = new Dictionary<string, Sprite>();
            foreach (var s in Resources.LoadAll<Sprite>(key)) m[s.name] = s;
            _atlasCache[key] = m;
            return m;
        }

        /// <param name="parent">MotionRig.Body (flip / roll spin / hop inherit)</param>
        /// <param name="footY">local y of the feet in parent space</param>
        /// <param name="figureHeight">target drawn height (local units), matched to the style-4 sprite</param>
        public CutoutRig(Transform parent, RigDefinition def, float footY, float figureHeight)
        {
            Def = def; _clips = RigClipSet.Shared; S = _clips.settings ?? new RigSettings();
            _k = def.CanvasToUnits;
            Root = new GameObject("CutoutRig_" + def.character).transform;
            Root.SetParent(parent, false);
            var side = def.View("side") ?? def.directions[0];
            float scale = figureHeight / Mathf.Max(0.01f, side.FigureHeightPx * _k);
            Root.localPosition = new Vector3(0f, footY, 0f);
            Root.localScale = Vector3.one * scale;
            var atlas = Atlas(def);
            var list = new List<View>();
            bool valid = true;
            foreach (var vn in RigDefinition.ViewNames)
            {
                var vd = def.View(vn); if (vd == null) continue;
                var v = Build(vd, atlas, ref valid);
                list.Add(v);
            }
            _views = list.ToArray();
            for (int i = 0; i < _views.Length; i++) if (_views[i].data.name == "side") _cur = i;
            for (int i = 0; i < _views.Length; i++) { _views[i].root.gameObject.SetActive(i == _cur); _views[i].alpha = i == _cur ? 1f : 0f; }
            AllSkinsValid = valid;
        }

        private Vector3 CanvasToRoot(RigView vd, Vector2 px) { var g = vd.GroundCanvas; return new Vector3((px.x - g.x) * _k, (g.y - px.y) * _k, 0f); }

        private View Build(RigView vd, Dictionary<string, Sprite> atlas, ref bool valid)
        {
            var v = new View { data = vd, vs = S.View(vd.name) };
            v.root = new GameObject("View_" + vd.name).transform;
            v.root.SetParent(Root, false);
            v.group = v.root.gameObject.AddComponent<SortingGroup>();
            var skel = new GameObject("root").transform; skel.SetParent(v.root, false);
            v.bones["root"] = skel;
            // bones (parents first: iterate until all placed)
            var pending = new List<RigPart>(vd.parts);
            int guard = 0;
            while (pending.Count > 0 && guard++ < 64)
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    var p = pending[i];
                    string par = string.IsNullOrEmpty(p.parent) ? "root" : p.parent;
                    if (!v.bones.TryGetValue(par, out var pt)) continue;
                    var b = new GameObject(p.name).transform;
                    b.SetParent(pt, false);
                    Vector3 parentPos = par == "root" ? Vector3.zero : CanvasToRoot(vd, vd.Part(par).PivotCanvas);
                    b.localPosition = CanvasToRoot(vd, p.PivotCanvas) - parentPos;
                    v.bones[p.name] = b; v.rest[p.name] = b.localPosition;
                    pending.RemoveAt(i);
                }
            // renderers + SpriteSkin (draw order from data: far limbs / gun behind body per view)
            var order = new List<string>(vd.drawOrder != null && vd.drawOrder.Length > 0 ? vd.drawOrder : System.Array.ConvertAll(vd.parts, p => p.name));
            var rs = new List<SpriteRenderer>(); var ss = new List<SpriteSkin>();
            foreach (var p in vd.parts)
            {
                if (!atlas.TryGetValue(p.sprite, out var sp) || !v.bones.TryGetValue(p.name, out var bone)) { valid = false; continue; }
                var go = new GameObject("part_" + p.name);
                go.transform.SetParent(v.root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                int idx = order.IndexOf(p.name);
                sr.sortingOrder = idx >= 0 ? idx : p.zorder;
                var skin = go.AddComponent<SpriteSkin>();
                var st1 = skin.SetRootBone(skel);
                var st2 = skin.SetBoneTransforms(new[] { bone });
                skin.alwaysUpdate = false;
                if (st2 != SpriteSkinState.Ready) valid = false;
                rs.Add(sr); ss.Add(skin);
            }
            v.renderers = rs.ToArray(); v.skins = ss.ToArray();
            v.baseOrder = new int[v.renderers.Length]; var ta = new List<int>();
            for (int i = 0; i < v.renderers.Length; i++)
            {
                v.baseOrder[i] = v.renderers[i].sortingOrder;
                string pn = v.renderers[i].gameObject.name.Substring(5);
                if (System.Array.IndexOf(ThrowArmParts, pn) >= 0) ta.Add(i);
                if (pn == "weapon") v.weaponIdx = i;
            }
            v.throwArm = ta.ToArray();
            if (v.bones.TryGetValue(ThrowHandBone, out var hand))
            {   // v16: small held bomb in the throwing hand (visual only; hidden except during the pull-back / windup)
                var hb = new GameObject("held_bomb"); hb.transform.SetParent(hand, false);
                v.heldBomb = hb.AddComponent<SpriteRenderer>(); v.heldBomb.sprite = BombSprite();
                v.heldBomb.sortingOrder = v.renderers.Length * 2 + 4; v.heldBomb.enabled = false;
            }
            // support-hand IK target: barrel forend point carried by the weapon bone
            if (vd.supportIK && vd.TryJoint("forend_support", out var fpx) && v.bones.TryGetValue("weapon", out var wb) && v.bones.ContainsKey("hand_l"))
            {
                v.forend = new GameObject("ik_forend").transform;
                v.forend.SetParent(wb, false);
                v.forend.localPosition = CanvasToRoot(vd, fpx) - CanvasToRoot(vd, vd.Part("weapon").PivotCanvas);
                Vector3 wrist = CanvasToRoot(vd, vd.Part("hand_l").PivotCanvas);
                v.wristFromForend = (Vector2)(wrist - CanvasToRoot(vd, fpx));
                // v17 carry pose for the throw = the BIND pose of the gun (as drawn), never a stale per-view aim pose
                v.wpPos = CanvasToRoot(vd, vd.Part("weapon").PivotCanvas); v.fePos = CanvasToRoot(vd, fpx); v.wpDeg = 0f; v.wpValid = true;
            }
            return v;
        }

        // ---------------------------------------------------------------- events
        public void Shoot() => _shootT = 0f;
        public void Hit() => _hitT = 0f;

        public void SetColor(Color c) => _color = c;

        /// <summary>Facing-relative aim angle (-90 down .. +90 up), view band from data with hysteresis.</summary>
        public static int SelectView(float aimDeg, int current, RigSettings s, bool forceSide, int count)
        {
            if (forceSide || count < 3) return count >= 3 ? 1 : Mathf.Min(current, count - 1);
            float down = s.viewDownBelowDeg, up = s.viewUpAboveDeg, h = s.viewHysteresisDeg;
            float hh = h * 0.5f; // enter a band past threshold - h/2, leave it past threshold + h/2
            if (current == 0) return aimDeg < down + hh ? 0 : (aimDeg > up + hh ? 2 : 1);
            if (current == 2) return aimDeg > up - hh ? 2 : (aimDeg < down - hh ? 0 : 1);
            if (aimDeg < down - hh) return 0;
            if (aimDeg > up + hh) return 2;
            return 1;
        }

        public static float AimDeg(Vector2 aim, float facing)
        {
            if (aim.sqrMagnitude < 1e-6f) return 0f;
            float x = Mathf.Abs(aim.x) < 1e-4f ? 1e-4f : Mathf.Abs(aim.x);
            return Mathf.Atan2(aim.y, x) * Mathf.Rad2Deg;
        }

        public void Tick(float dt, in RigInput inp)
        {
            dt = Mathf.Max(0f, dt);
            float aimDeg = inp.forceSide ? 0f : AimDeg(inp.aim, inp.facing);
            // ---- view selection + crossfade
            _viewHold -= dt;
            int want = SelectView(aimDeg, _cur, S, inp.forceSide, _views.Length);
            if (inp.rolling || inp.downed) want = _cur;
            // v16: keep the throw's view through the 0.32 s overhead arc (gun aim resuming right after the release must not
            // snap the thrower to another view mid-throw); visual only
            if (_throwT < ThrowDur || (inp.throwCountValid && _throwSeen && inp.throwCount != _lastThrowCount)) want = _cur;
            if (want != _cur && _viewHold <= 0f)
            {
                // v15 perf: instant replacement, exactly one active view (no dual 2x18-skin crossfade)
                _views[_cur].root.gameObject.SetActive(false); _views[_cur].alpha = 0f;
                _cur = want; _fade = 1f; _viewHold = 0.15f; _prev = -1;
                _views[_cur].root.gameObject.SetActive(true); _frozen = false; // new view must be posed this tick
                ViewSwitches++;
            }
            _views[_cur].alpha = 1f;

            // ---- layer weights
            float speed = inp.velocity.magnitude;
            float m01 = inp.maxSpeed > 0.01f ? Mathf.Clamp01(speed / inp.maxSpeed) : 0f;
            bool moving = !inp.rolling && !inp.downed && m01 > 0.06f;
            float runTarget = moving ? Mathf.InverseLerp(S.runSpeed01 - 0.1f, S.runSpeed01 + 0.1f, m01) : 0f;
            _walkW = Mathf.MoveTowards(_walkW, moving ? 1f - runTarget : 0f, dt / 0.12f);
            _runW = Mathf.MoveTowards(_runW, runTarget, dt / 0.12f);
            float relX = inp.velocity.x * inp.facing;
            bool backwards = relX < -0.2f && Mathf.Abs(inp.velocity.x) > Mathf.Abs(inp.velocity.y) * 0.5f; // walk-back shooting
            _phase += (backwards ? -1f : 1f) * speed * dt / Mathf.Max(0.2f, S.walkCycleTiles);
            _phase -= Mathf.Floor(_phase);
            _idleT = (_idleT + dt / 2.4f) % 1f;
            float blend = S.fullBlend > 0f ? dt / S.fullBlend : 1f;
            _fullRollW = Mathf.MoveTowards(_fullRollW, inp.rolling ? 1f : 0f, blend);
            _downW = Mathf.MoveTowards(_downW, inp.downed ? 1f : 0f, blend * 0.5f);
            // v16 throw: 0.15 s pull-back while the logic is in GrenadeAiming (held), overhead arc fired by the LOGIC release
            bool aimG = inp.grenadeAiming && !inp.downed && !inp.rolling;
            _pullT = aimG ? _pullT + dt : 0f;
            float pullDur = S.throwPullback > 0.01f ? S.throwPullback : 0.15f;
            float wPrev = _windupW;
            _windupW = aimG ? Mathf.Clamp01(_pullT / pullDur) : Mathf.MoveTowards(_windupW, 0f, blend);
            bool released;
            if (inp.throwCountValid) { released = _throwSeen && inp.throwCount != _lastThrowCount; _lastThrowCount = inp.throwCount; _throwSeen = true; }
            else released = _wasWindup && !inp.grenadeAiming && !inp.downed;
            _wasWindup = inp.grenadeAiming;
            if (released && !inp.downed)
            {
                // arc starts where the pull-back got to (an instant logic release = short forward flick, no pop to the back pose)
                float pulled = Mathf.Max(wPrev, aimG ? _windupW : 0f);
                _throwT = (1f - pulled) * S.throwArcStartShare * ThrowDur; _windupW = 0f; _pullT = 0f; ThrowArcs++;
                if (_heldShown) { BombHandoffs++; _heldShown = false; }
            }
            _bombWanted = aimG && _windupW > 0.02f;
            _shootT += dt; _hitT += dt; if (!released) _throwT += dt;

            // aim: gun arm delta from the view's aim centre (held a moment after the last shot)
            float targetAim = 0f;
            if (inp.aiming && !inp.rolling && !inp.downed)
                targetAim = Mathf.Clamp(aimDeg - _views[_cur].vs.aimCenterDeg, -S.aimMaxDeg, S.aimMaxDeg);
            _aimDelta = Mathf.MoveTowards(_aimDelta, targetAim, dt * 400f);

            // springs (hat / scarf lag the body)
            Vector2 acc = dt > 1e-5f ? (inp.velocity - _lastVel) / dt : Vector2.zero; _lastVel = inp.velocity;
            float drive = -(relX * 2.2f) - acc.x * inp.facing * 0.08f + (inp.rolling ? 8f : 0f);
            Spring(ref _hatA, ref _hatV, drive * S.hatSpring, dt);
            Spring(ref _scarfA, ref _scarfV, drive * Def.secondaryGain - Mathf.Abs(inp.velocity.y) * 1.5f * (Def.secondaryGain / 1.6f), dt);
            _hatA = Mathf.Clamp(_hatA, -Def.hatMaxDeg, Def.hatMaxDeg); _scarfA = Mathf.Clamp(_scarfA, -Def.secondaryMaxDeg, Def.secondaryMaxDeg);

            // shadow: wider on run, stretched on roll
            float r = _fullRollW;
            ShadowMul = new Vector2((1f + 0.12f * _runW) * Mathf.Lerp(1f, 1.25f, r), Mathf.Lerp(1f, 0.85f, r));

            // v15 perf: skip the bone pose (and SpriteSkin deformation, alwaysUpdate=false) while no rendering camera can see
            // the rig (position test with margin, so the pose is refreshed BEFORE it re-enters the view; renderer.isVisible
            // would be a frame late and SpriteSkin bounds would be stale). Downed: freeze once the fall has fully blended in
            // and held DownFreezeAfter (never mid-fall); any change (revive, view switch, roll) unfreezes immediately.
            var cv = _views[_cur];
            bool onScreen = OnScreen();
            if (inp.downed && _downW >= 1f) _downHeld += dt; else { _downHeld = 0f; _frozen = false; }
            bool freeze = inp.downed && _downHeld >= DownFreezeAfter && _posedOnce;
            if (!onScreen && _posedOnce) { SkippedOffscreen++; _needPose = true; }
            else if (freeze && _frozen && !_needPose) { SkippedFrozen++; }
            else { Pose(cv, inp); _posedOnce = true; _needPose = false; _frozen = freeze; Posed++; }
            ApplyColorSort(cv, inp);

            UpdateStars(dt, inp);
        }

        private void Spring(ref float a, ref float v, float target, float dt)
        {
            float h = Mathf.Min(dt, 1f / 60f); int n = Mathf.Max(1, Mathf.CeilToInt(dt / Mathf.Max(1e-4f, h)));
            float sdt = dt / n;
            for (int i = 0; i < n; i++) { v += (-S.springStiffness * (a - target) - S.springDamping * v) * sdt; a += v * sdt; }
        }

        private void AddClip(string clipName, float t, float w, float legScale, float liftScale)
        {
            if (w <= 0.0001f) return;
            var c = _clips.Clip(clipName); if (c?.tracks == null) return;
            foreach (var tr in c.tracks)
            {
                _pose.TryGetValue(tr.bone, out var p);
                bool leg = tr.bone.StartsWith("thigh") || tr.bone.StartsWith("shin") || tr.bone.StartsWith("foot");
                float ls = leg ? legScale : 1f, ly = tr.bone == "pelvis" ? liftScale : 1f;
                if (RigClipSet.Sample(tr, t, 0, out float rot)) p.x += rot * w * ls;
                if (RigClipSet.Sample(tr, t, 1, out float x)) p.y += x * w;
                if (RigClipSet.Sample(tr, t, 2, out float y)) p.z += y * w * ly;
                _pose[tr.bone] = p;
            }
        }

        private readonly Dictionary<string, Vector3> _full = new Dictionary<string, Vector3>();
        private readonly List<string> _tmpKeys = new List<string>();
        public float SecondaryAngle => _scarfA;

        private void Pose(View v, in RigInput inp)
        {
            _pose.Clear();
            float ls = v.vs.legScale, lift = v.vs.liftScale;
            AddClip("walk", _phase, _walkW, ls, lift);
            AddClip("run", _phase, _runW, ls, lift);
            float upperBusy = Mathf.Max(_windupW, _throwT < ThrowDur ? 1f : 0f);
            AddClip("idle", _idleT, 1f - Mathf.Max(_walkW, _runW) * 0.8f, 1f, 1f);
            AddClip("throw_windup", _idleT * 4f % 1f, Mathf.SmoothStep(0f, 1f, _windupW), 1f, 1f);
            if (_throwT < ThrowDur) AddClip("throw", _throwT / ThrowDur, 1f - _windupW, 1f, 1f);
            if (_shootT < 0.16f) AddClip("shoot", _shootT / 0.16f, 1f - upperBusy, 1f, 1f);
            if (_hitT < 0.22f) AddClip("hit", _hitT / 0.22f, 1f, 1f, 1f);
            // aim: torso / head share + the rest on the gun shoulder
            float aim = _aimDelta * (1f - upperBusy);
            Add("torso", aim * S.aimTorsoShare);
            Add("head", Mathf.Clamp(aim * S.aimHeadShare, -12f, 12f) - aim * S.aimTorsoShare);
            Add("upper_arm_r", aim * (1f - S.aimTorsoShare));
            Add("hat", _hatA);
            Add(Def.secondaryPart, _scarfA);
            // conservative joint limits (draft art: auto-filled occluded areas show at large angles)
            if (Def.limits != null)
            {
                bool throwing = upperBusy > 0.001f && (_windupW > 0.001f || _throwT < ThrowDur);
                _tmpKeys.Clear(); _tmpKeys.AddRange(_pose.Keys);
                foreach (var b in _tmpKeys)
                {
                    var q = _pose[b];
                    if (throwing && b == ThrowArmParts[0]) q.x = SoftLimit(Mathf.DeltaAngle(0f, q.x), S.throwArmLimitDeg, S.throwSoftBandDeg); // throw-only wide arm, soft 15 deg deceleration into 160
                    else if (throwing && b == ThrowArmParts[1]) q.x = SoftLimit(q.x, S.throwForearmLimitDeg, S.throwSoftBandDeg);
                    else q.x = RigDefinition.Limit(Def.limits, b, q.x);
                    _pose[b] = q;
                }
            }
            // full-body overrides (roll curl / down)
            float fw = Mathf.Max(_fullRollW, _downW);
            if (fw > 0.0001f)
            {
                var keep = new Dictionary<string, Vector3>(_pose);
                _pose.Clear();
                if (_fullRollW > 0f) AddClip("roll", inp.rollProgress, 1f, 1f, 1f);
                if (_downW > 0f) AddClip("down", _idleT, 1f, 1f, 1f);
                _full.Clear(); foreach (var kv in _pose) { var q = kv.Value; q.x = RigDefinition.Limit(Def.fullLimits, kv.Key, q.x); _full[kv.Key] = q; }
                _pose.Clear();
                foreach (var b in v.bones.Keys)
                {
                    keep.TryGetValue(b, out var a); _full.TryGetValue(b, out var f);
                    if (b == "hat" || b == Def.secondaryPart) f += a; // springs stay on
                    _pose[b] = Vector3.Lerp(a, f, fw);
                }
            }
            // apply FK
            foreach (var kv in v.bones)
            {
                if (kv.Key == "root") continue;
                _pose.TryGetValue(kv.Key, out var p);
                kv.Value.localRotation = Quaternion.Euler(0f, 0f, p.x);
                kv.Value.localPosition = v.rest[kv.Key] + new Vector3(p.y * _k, p.z * _k, 0f);
            }
            // v17: two-handed guns (forend support) stay in the OFF hand, lowered, while the near arm throws: the gun is
            // detached from the throwing hand and held at its last aim pose rotated barrel-down (view only; covers the torso
            // area the source art never painted under the gun). One-handed guns (Volt) are hidden instead (ApplyColorSort).
            if (v.weaponIdx >= 0 && v.bones.TryGetValue("weapon", out var wpn))
            {
                var R = v.root;
                if (upperBusy > 0.001f && v.forend != null && v.wpValid)
                {
                    // pivot about the support grip (forend stays within the off arm's reach): muzzle down, stock up
                    float gw = Mathf.SmoothStep(0f, 1f, upperBusy);
                    float td = v.wpDeg - S.throwGunLowerDeg;
                    Vector2 feT = v.fePos + new Vector2(0f, -S.throwGunDrop);
                    Vector2 gripToFe = v.fePos - v.wpPos;                       // at the aim angle
                    Vector2 tp = feT - Rot(gripToFe, td - v.wpDeg);
                    Vector2 cur = R.InverseTransformPoint(wpn.position); float curDeg = Ang(R.InverseTransformDirection(wpn.right));
                    Vector2 np = Vector2.Lerp(cur, tp, gw); float nd = curDeg + Mathf.DeltaAngle(curDeg, td) * gw;
                    wpn.position = R.TransformPoint(new Vector3(np.x, np.y, R.InverseTransformPoint(wpn.position).z));
                    wpn.localRotation = Quaternion.Euler(0f, 0f, wpn.localEulerAngles.z + Mathf.DeltaAngle(curDeg, nd));
                }
            }
            // support hand IK (barrel forend), off while rolling / down; v17: kept ON during a throw (the off hand holds the gun)
            float ikW = v.forend != null && v.wpValid ? 1f - fw : (1f - Mathf.Max(upperBusy, fw));
            if (v.forend != null && ikW > 0.01f) SolveSupportIK(v, ikW);
        }

        /// <summary>v15: the throwing arm (windup clip bones) is drawn in front of the body while winding up / releasing, so the
        /// gameplay grenade throw reads in every view (it is the far arm, hidden behind torso / backpack / wrench otherwise).</summary>
        /// v17: the throw uses the NEAR arm (r = the gun arm: large, drawn over the body in every view); the gun is holstered
        /// (weapon sprite hidden, logic trigger untouched) during the windup / arc. v16 used the far arm (l): hidden by the
        /// head / torso / gun, so the throw did not read on screen.
        public static readonly string[] ThrowArmParts = { "upper_arm_r", "forearm_r", "hand_r" };
        public const string ThrowHandBone = "hand_r";
        /// <summary>Continuous soft limit: identity below (lim - band), then a tanh deceleration that approaches lim (C1).</summary>
        public static float SoftLimit(float x, float lim, float band)
        {
            float a = Mathf.Abs(x), k = lim - Mathf.Max(0.01f, band);
            if (band <= 0f) return Mathf.Clamp(x, -lim, lim);
            if (a <= k) return x;
            float t = (float)System.Math.Tanh((a - k) / band);
            return Mathf.Sign(x) * (k + band * t);
        }
        /// <summary>v17: gun off the throwing hand (two-handed: lowered in the off hand; one-handed: hidden).</summary>
        public bool WeaponHolstered => _views[_cur].holstered;
        public bool WeaponRendered => _views[_cur].weaponIdx >= 0 && _views[_cur].renderers[_views[_cur].weaponIdx].enabled;
        // v17 release bridge (view only): where the held bomb was drawn on the last frame before the logic release
        private Vector3 _lastBombWorld, _lastRootWorld; private float _lastBombTime = -99f;
        private static readonly List<CutoutRig> _live = new List<CutoutRig>();
        public static float HandOriginMaxAge = 0.15f;
        /// <summary>Held-bomb world position of the rig nearest to <paramref name="feetWorld"/> (a thrower's logical position
        /// projected to world) whose bomb was visible within HandOriginMaxAge. Visual only (GrenadeView bridge).</summary>
        public static bool TryGetHandOrigin(Vector3 feetWorld, out Vector3 hand)
        {
            hand = default; float best = 2.5f; bool ok = false; float now = Time.time;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var r = _live[i];
                if (r == null || r._views == null || r._views[r._cur].root == null || now - r._lastBombTime > 2f) { _live.RemoveAt(i); continue; }
                if (now - r._lastBombTime > HandOriginMaxAge) continue;
                float d = Vector2.Distance(r._lastRootWorld, feetWorld);
                if (d < best) { best = d; hand = r._lastBombWorld; ok = true; }
            }
            return ok;
        }
        public bool ThrowArmInFront => _views[_cur].armFront;
        public const float ThrowDur = 0.32f;
        public static int ThrowArcs, BombHandoffs;
        private float _pullT; private int _lastThrowCount; private bool _throwSeen, _bombWanted, _heldShown;
        public bool HeldBombVisible => _views[_cur].heldBomb != null && _views[_cur].heldBomb.enabled;
        public float PullbackWeight => _windupW;
        public float ThrowArcTime => _throwT;
        /// <summary>World size of the held bomb (matches the flying GrenadeView bomb at scale 1); sprite set by the view layer.</summary>
        public static float HeldBombWorldSize = 0.4f;
        public static Sprite HeldBombSprite;
        private static Sprite _fallbackBomb;
        private static Sprite BombSprite()
        {
            if (HeldBombSprite != null) return HeldBombSprite;
            if (_fallbackBomb != null) return _fallbackBomb;
            const int N = 16; var t = new Texture2D(N, N) { filterMode = FilterMode.Bilinear }; var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float dx = x - 7.5f, dy = y - 7.5f; float d = Mathf.Sqrt(dx * dx + dy * dy); px[y * N + x] = d < 7f ? new Color(0.12f, 0.12f, 0.14f, 1f) : Color.clear; }
            t.SetPixels(px); t.Apply(); _fallbackBomb = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N); return _fallbackBomb;
        }
        private void UpdateHeldBomb(View v)
        {
            if (v.heldBomb == null) return;
            bool show = _bombWanted;
            if (show != v.heldBomb.enabled) v.heldBomb.enabled = show;
            if (show)
            {
                if (HeldBombSprite != null && v.heldBomb.sprite != HeldBombSprite) v.heldBomb.sprite = HeldBombSprite;
                var ps = v.heldBomb.transform.parent.lossyScale; float sz = v.heldBomb.sprite.bounds.size.y;
                float k = HeldBombWorldSize / Mathf.Max(1e-4f, sz * Mathf.Abs(ps.y));
                v.heldBomb.transform.localScale = new Vector3(k * Mathf.Sign(ps.x) * Mathf.Sign(ps.x), k, 1f);
                v.heldBomb.color = _color;
                _heldShown = true;
                _lastBombWorld = v.heldBomb.transform.position; _lastBombTime = Time.time; _lastRootWorld = v.root.position;
                if (!_live.Contains(this)) _live.Add(this);
            }
            foreach (var o in _views) if (o != v && o.heldBomb != null && o.heldBomb.enabled) o.heldBomb.enabled = false;
        }
        private void ApplyColorSort(View v, in RigInput inp)
        {
            UpdateHeldBomb(v);
            bool holster = !inp.downed && (_windupW > 0.15f || _throwT < ThrowDur * 0.9f);
            bool hide = holster && v.forend == null; // one-handed gun: hidden; two-handed: lowered in the off hand (Pose)
            if (v.weaponIdx >= 0 && holster != v.holstered) { v.holstered = holster; v.renderers[v.weaponIdx].enabled = !hide; }
            foreach (var o in _views) if (o != v && o.holstered && o.weaponIdx >= 0) { o.holstered = false; o.renderers[o.weaponIdx].enabled = true; }
            bool front = !inp.downed && (_windupW > 0.25f || _throwT < ThrowDur);
            if (front != v.armFront && v.throwArm != null)
            {
                v.armFront = front;
                for (int k = 0; k < v.throwArm.Length; k++) { int i = v.throwArm[k]; v.renderers[i].sortingOrder = front ? v.renderers.Length + 1 + v.baseOrder[i] : v.baseOrder[i]; }
            }
            var c = _color; c.a *= v.alpha;
            if (c != v.lastColor) { v.lastColor = c; for (int i = 0; i < v.renderers.Length; i++) v.renderers[i].color = c; }
            if (v.group.sortingOrder != inp.sortingOrder) v.group.sortingOrder = inp.sortingOrder;
        }

        // ---------------------------------------------------------------- v15 perf: visibility / freeze / counters
        public const float DownFreezeAfter = 0.35f;
        public static int Posed, SkippedOffscreen, SkippedFrozen, ViewSwitches; // cumulative (diagnostics)
        private bool _frozen, _posedOnce, _needPose;
        private float _downHeld;
        public bool Frozen => _frozen;
        public int ActiveSkinCount { get { int n = 0; foreach (var v in _views) if (v.root.gameObject.activeSelf) foreach (var k in v.skins) if (k != null && k.enabled) n++; return n; } }
        public int ActiveViewCount { get { int n = 0; foreach (var v in _views) if (v.root.gameObject.activeSelf) n++; return n; } }
        /// <summary>Margin (world units) around the rig root for the camera test: covers the drawn figure + roll tumble.</summary>
        public static float CullMargin = 3.0f;
        private static Camera[] _camBuf = new Camera[8];
        private bool OnScreen()
        {
            int n = Camera.allCamerasCount;
            if (n == 0) return true; // no camera (edit-mode tests / loading): always pose
            if (_camBuf.Length < n) _camBuf = new Camera[n];
            n = Camera.GetAllCameras(_camBuf);
            Vector3 p = Root.position;
            for (int i = 0; i < n; i++)
            {
                var cam = _camBuf[i];
                if (cam == null || !cam.enabled) continue;
                if (!cam.orthographic) return true; // conservative
                float h = cam.orthographicSize + CullMargin, w = cam.orthographicSize * cam.aspect + CullMargin;
                Vector3 c = cam.transform.position;
                if (Mathf.Abs(p.x - c.x) <= w && Mathf.Abs(p.y - c.y) <= h) return true;
            }
            return false;
        }

        private void Add(string bone, float rot) { _pose.TryGetValue(bone, out var p); p.x += rot; _pose[bone] = p; }

        private static Vector2 Rot(Vector2 a, float deg) { float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r); return new Vector2(a.x * c - a.y * s, a.x * s + a.y * c); }
        private static float Ang(Vector2 a) => Mathf.Atan2(a.y, a.x) * Mathf.Rad2Deg;

        /// <summary>Analytic 2-bone IK in the view-root space (no mirroring inside the rig): upper_arm_l / forearm_l reach the
        /// wrist target carried by the barrel; hand_l turns with the gun.</summary>
        private void SolveSupportIK(View v, float w)
        {
            var R = v.root;
            Transform up = v.bones["upper_arm_l"], fo = v.bones["forearm_l"], ha = v.bones["hand_l"], wp = v.bones["weapon"];
            Vector2 P(Transform t) => R.InverseTransformPoint(t.position);
            float weaponDeg = Ang(R.InverseTransformDirection(wp.right));
            Vector2 S0 = P(up), E0 = P(fo), W0 = P(ha);
            Vector2 T = (Vector2)R.InverseTransformPoint(v.forend.position) + Rot(v.wristFromForend, weaponDeg);
            T = Vector2.Lerp(W0, T, w);
            float L1 = (E0 - S0).magnitude, L2 = (W0 - E0).magnitude;
            Vector2 st = T - S0; float d = Mathf.Clamp(st.magnitude, Mathf.Abs(L1 - L2) + 1e-4f, L1 + L2 - 1e-4f);
            float cross = (E0 - S0).x * (W0 - E0).y - (E0 - S0).y * (W0 - E0).x;
            float sign = cross >= 0f ? 1f : -1f;
            float cosA = Mathf.Clamp((L1 * L1 + d * d - L2 * L2) / (2f * L1 * d), -1f, 1f);
            Vector2 dir = st.sqrMagnitude > 1e-8f ? st.normalized : Vector2.right;
            Vector2 E1 = S0 + Rot(dir, -sign * Mathf.Acos(cosA) * Mathf.Rad2Deg) * L1;
            up.localRotation = Quaternion.Euler(0f, 0f, up.localEulerAngles.z + Mathf.DeltaAngle(Ang(E0 - S0), Ang(E1 - S0)));
            Vector2 W1 = P(ha);
            fo.localRotation = Quaternion.Euler(0f, 0f, fo.localEulerAngles.z + Mathf.DeltaAngle(Ang(W1 - E1), Ang(T - E1)));
            float handDeg = Ang(R.InverseTransformDirection(ha.right));
            ha.localRotation = Quaternion.Euler(0f, 0f, ha.localEulerAngles.z + Mathf.DeltaAngle(handDeg, weaponDeg));
            IkError = (P(ha) - T).magnitude;
        }

        // ---------------------------------------------------------------- downed stars
        private static Sprite _starSprite;
        private static Sprite StarSprite()
        {
            if (_starSprite != null) return _starSprite;
            const int n = 32; var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Atan2(dy, dx), r = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = Mathf.Lerp(0.42f, 0.95f, 0.5f + 0.5f * Mathf.Cos(5f * (a - Mathf.PI / 2f)));
                    float inside = Mathf.Clamp01((edge - r) * 12f);
                    float ink = Mathf.Clamp01((edge + 0.12f - r) * 12f);
                    px[y * n + x] = inside > 0f ? Color.Lerp(new Color(0.25f, 0.15f, 0.05f, ink), new Color(1f, 0.86f, 0.2f, 1f), inside) : new Color(0.25f, 0.15f, 0.05f, ink);
                }
            tex.SetPixels(px); tex.Apply();
            _starSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            _starSprite.name = "rig_down_star";
            return _starSprite;
        }

        private void UpdateStars(float dt, in RigInput inp)
        {
            bool on = inp.downed && _downW > 0.5f;
            if (!on && _stars == null) return;
            if (_stars == null)
            {
                _stars = new SpriteRenderer[3];
                for (int i = 0; i < 3; i++)
                {
                    var go = new GameObject("DownStar" + i); go.transform.SetParent(Root, false);
                    _stars[i] = go.AddComponent<SpriteRenderer>(); _stars[i].sprite = StarSprite();
                }
            }
            _starT += dt;
            var v = _views[_cur];
            Vector3 head = v.bones.TryGetValue("hat", out var hb) ? Root.InverseTransformPoint(hb.position) : new Vector3(0f, 4f, 0f);
            for (int i = 0; i < 3; i++)
            {
                _stars[i].enabled = on;
                if (!on) continue;
                float a = _starT * 3.2f + i * Mathf.PI * 2f / 3f;
                float s = 1f / Mathf.Max(0.01f, Root.localScale.x);
                _stars[i].transform.localPosition = head + new Vector3(Mathf.Cos(a) * 0.55f * s, 0.35f * s + Mathf.Sin(a) * 0.16f * s, 0f);
                _stars[i].transform.localScale = Vector3.one * (0.32f + 0.06f * Mathf.Sin(a)) * s;
                _stars[i].sortingOrder = inp.sortingOrder + (Mathf.Sin(a) > 0f ? -1 : 1);
                _stars[i].color = _color;
            }
        }
    }
}

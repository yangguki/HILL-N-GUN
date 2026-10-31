using UnityEngine;
using HillNGun.Data;

namespace HillNGun.View3D
{
    /// <summary>Per-frame inputs the views hand to a 3D actor (all read from logic; logic never reads back).</summary>
    public struct ActorPose
    {
        public Vector2 velocity;       // logic tiles / s
        public float maxSpeed;
        public Vector2 aimDir;         // logic space
        public bool aiming;            // weapon raised (aiming / firing / charging)
        public float defaultYaw;       // where the actor looks when idle (0 = up-screen, back to camera)
        public float torsoFollowMove;  // 0..1 how much the torso turns towards the walk direction when not aiming
        public bool rolling;
        public float rollProgress;     // 0..1
        public float rollSpin01;       // eased 0..1 (MovementFeel.RollEase)
        public Vector2 rollDir;
        public float rowHop;           // 0..1 hop weight (sin of row-change progress)
        public bool downed;
        public bool defeated;
        public float strideMul;        // 1 = default
    }

    /// <summary>
    /// One 3D actor = code-built rig + procedural animator. Gameplay-agnostic: views feed ActorPose + events
    /// (Fire / Hit / Throw / Warn / TailSweep / FanShot) and Place() it on the field.
    /// Legs follow movement (speed-synced stride with knee bend and foot plant), torso/head/arms follow aim.
    /// </summary>
    public sealed class Actor3D
    {
        public readonly Actor3DRig Rig;
        private readonly Actor3DAnimData A;
        private readonly Actor3DData D;
        private readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int TintId = Shader.PropertyToID("_Tint");

        private float _phase, _gaitW, _pelvisYaw, _torsoYaw, _aimW, _recoil, _flinch, _downW, _lastAimTime = -99f;
        private float _gaitDir = 1f, _land, _throwT = -1f, _sweepT = -1f, _sweepSign = 1f, _fanT = -1f, _warnW, _starSpin;
        private float _horsePhase, _time, _settle;
        private bool _wasRolling, _initialized, _warnTail, _warnFan;
        private float _flash, _lastFlash = -1f;
        private Color _tint = Color.white, _lastTint = new Color(-1, 0, 0);
        private Color _starColor;

        public float Scale { get; private set; } = 1f;
        public bool Visible { get; private set; } = true;
        public float HeadHeightWorld => Rig.Height * Scale * D.modelScale;

        public Actor3D(Actor3DModelDef def, Material mat, Actor3DData data, Actor3DAnimData anim, Color skinTint, float skinStrength, Transform parent, string name)
        {
            D = data;
            A = anim;
            Rig = new Actor3DRig(def, mat, data.actorLayer, skinTint, skinStrength, parent, name);
            _starColor = anim.dizzyStarColor;
        }

        public void SetTint(Color tint) => _tint = tint;
        public void Flash(float amount) => _flash = Mathf.Max(_flash, amount);
        public void Fire() { _recoil = 1f; _lastAimTime = _time; }
        public void Hit() { _flinch = 1f; Flash(0.85f); }
        public void Throw() { _throwT = 0f; }
        public void Warn(bool tail, bool fan) { _warnTail = tail; _warnFan = fan; }
        public void TailSweep(float sign) { _sweepT = 0f; _sweepSign = sign >= 0f ? 1f : -1f; }
        public void FanShot() { _fanT = 0f; _recoil = 1f; }
        public void SetStarColor(Color c) => _starColor = c;

        public void SetVisible(bool v)
        {
            if (Visible == v || Rig.Root == null) return;
            Visible = v;
            Rig.Root.gameObject.SetActive(v);
        }

        /// <summary>Feet world position (2D view space), uniform view scale (perspective × data), logic Y for depth.</summary>
        public void Place(Vector3 feetWorld, float scale, float logicY)
        {
            Scale = scale;
            var t = Rig.Root;
            t.position = new Vector3(feetWorld.x, feetWorld.y, D.depthBase + logicY * D.depthPerTile);
            t.rotation = Quaternion.Euler(-D.viewTiltDeg, 0f, 0f);
            float s = scale * D.modelScale;
            t.localScale = new Vector3(s, s, s);
        }

        private static float YawOf(Vector2 v) => Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
        private static float Damp(float sharp, float dt) => 1f - Mathf.Exp(-sharp * dt);
        private static Quaternion E(float x, float y, float z) => Quaternion.Euler(x, y, z);

        public void Tick(float dt, in ActorPose p)
        {
            if (!Visible) return;
            dt = Mathf.Min(dt, 0.1f);
            _time += dt;
            var r = Rig;
            var def = r.Def;

            float speed = p.velocity.magnitude;
            bool canWalk = !p.rolling && !p.downed && !p.defeated;
            bool moving = canWalk && speed > A.minWalkSpeed;
            float moveYaw = speed > 0.05f ? YawOf(p.velocity) : _pelvisYaw;
            float aimYaw = p.aimDir.sqrMagnitude > 0.01f ? YawOf(p.aimDir) : p.defaultYaw;
            if (p.aiming) _lastAimTime = _time;
            bool aimHeld = _time - _lastAimTime < A.aimHoldTime;

            if (!_initialized) { _pelvisYaw = _torsoYaw = p.defaultYaw; _initialized = true; }

            // ---- facing: torso -> aim (or idle facing), legs -> movement ----
            float torsoTarget;
            if (p.rolling) torsoTarget = YawOf(p.rollDir.sqrMagnitude > 0.01f ? p.rollDir : new Vector2(0f, 1f));
            else if (aimHeld) torsoTarget = aimYaw;
            else if (moving) torsoTarget = p.defaultYaw + Mathf.DeltaAngle(p.defaultYaw, moveYaw) * Mathf.Clamp01(p.torsoFollowMove);
            else torsoTarget = p.defaultYaw;
            _torsoYaw = Mathf.LerpAngle(_torsoYaw, torsoTarget, Damp(p.rolling ? 30f : A.torsoYawSharpness, dt));

            float pelvisTarget = _torsoYaw;
            _gaitDir = 1f;
            if (moving)
            {
                float diff = Mathf.DeltaAngle(_torsoYaw, moveYaw);
                if (Mathf.Abs(diff) > A.backpedalAngleDeg) { pelvisTarget = moveYaw + 180f; _gaitDir = -1f; }
                else pelvisTarget = moveYaw;
            }
            if (p.rolling) pelvisTarget = _torsoYaw;
            _pelvisYaw = Mathf.LerpAngle(_pelvisYaw, pelvisTarget, Damp(p.rolling ? 30f : A.legYawSharpness, dt));
            float twist = Mathf.Clamp(Mathf.DeltaAngle(_pelvisYaw, _torsoYaw), -A.torsoTwistMaxDeg, A.torsoTwistMaxDeg);
            // keep the torso pointed where we want by letting legs give way if the twist would exceed the limit
            if (Mathf.Abs(Mathf.DeltaAngle(_pelvisYaw, _torsoYaw)) > A.torsoTwistMaxDeg)
                _pelvisYaw = _torsoYaw - twist;
            float headYaw = 0f;

            // ---- gait ----
            float speedF = Mathf.Clamp01(speed / Mathf.Max(0.1f, p.maxSpeed));
            _gaitW = Mathf.Lerp(_gaitW, moving ? 1f : 0f, Damp(A.gaitBlendSharpness, dt));
            float stride = A.strideLength * Mathf.Lerp(0.6f, 1f, speedF) * (p.strideMul > 0f ? p.strideMul : 1f);
            float worldLeg = r.LegLength * D.modelScale;
            if (moving) _phase += _gaitDir * speed / Mathf.Max(0.2f, stride) * dt;
            else
            {
                // settle towards the nearest neutral phase (feet together) when stopping
                float target = Mathf.Round(_phase * 2f) * 0.5f + 0.25f;
                _phase = Mathf.Lerp(_phase, target, Damp(A.settleSharpness, dt) * (1f - _gaitW));
            }
            float swing = Mathf.Min(A.maxLegSwingDeg, Mathf.Asin(Mathf.Clamp(stride * 0.25f / Mathf.Max(0.1f, worldLeg), 0f, 0.95f)) * Mathf.Rad2Deg * A.legSwingScale) * _gaitW;
            float phL = _phase, phR = _phase + 0.5f;
            float twoPi = Mathf.PI * 2f;
            float fL = swing * Mathf.Cos(twoPi * phL), fR = swing * Mathf.Cos(twoPi * phR);
            float kL = (A.kneeMaxDeg * A.Knee(phL) * _gaitW) * Mathf.Lerp(0.6f, 1f, speedF), kR = (A.kneeMaxDeg * A.Knee(phR) * _gaitW) * Mathf.Lerp(0.6f, 1f, speedF);
            float stanceKnee = 4f;
            kL += stanceKnee; kR += stanceKnee;

            // ---- weights ----
            _aimW = Mathf.Lerp(_aimW, aimHeld && canWalk ? 1f : 0f, Damp(A.aimBlendSharpness, dt));
            _recoil = Mathf.Max(0f, _recoil - _recoil * A.recoilDecay * dt - dt * 0.5f);
            _flinch = Mathf.Max(0f, _flinch - _flinch * A.flinchDecay * dt - dt);
            bool down = p.downed || p.defeated;
            float downRate = dt / Mathf.Max(0.05f, down ? A.downedBlendTime : A.reviveBlendTime);
            _downW = Mathf.MoveTowards(_downW, down ? 1f : 0f, downRate);
            float dw = Mathf.SmoothStep(0f, 1f, _downW);
            if (_wasRolling && !p.rolling) _land = 1f;
            _wasRolling = p.rolling;
            _land = Mathf.Max(0f, _land - dt / Mathf.Max(0.01f, A.landSquashTime));

            float rollW = p.rolling ? 1f : 0f;
            float tuck = p.rolling ? A.RollTuck(p.rollProgress) : 0f;
            float hopTuck = p.rowHop;

            // ---- root / body ----
            r.Yaw.localRotation = E(0f, _pelvisYaw, 0f);
            float bodyY = 0f;
            if (!def.mounted)
            {
                float hL = r.Thigh * Mathf.Cos(fL * Mathf.Deg2Rad) + r.Shin * Mathf.Cos((kL - fL) * Mathf.Deg2Rad);
                float hR = r.Thigh * Mathf.Cos(fR * Mathf.Deg2Rad) + r.Shin * Mathf.Cos((kR - fR) * Mathf.Deg2Rad);
                bodyY -= (r.Thigh + r.Shin - Mathf.Max(hL, hR)) * (1f - rollW);
                bodyY -= A.hipBob * Mathf.Cos(2f * twoPi * _phase) * _gaitW * 0.5f;
            }
            bodyY += A.rowHopHeight * p.rowHop;
            if (p.rolling) bodyY += A.rollLift * Mathf.Sin(Mathf.PI * p.rollProgress);
            bodyY -= A.downedDrop * dw;
            r.Body.localPosition = new Vector3(0f, bodyY, 0f);
            float squash = A.landSquash * Mathf.Sin(Mathf.PI * (1f - _land)) * (_land > 0f ? 1f : 0f);
            float breath = Mathf.Sin(_time * A.breathRate * twoPi);
            r.Body.localScale = new Vector3(1f + squash * 0.6f, 1f - squash, 1f + squash * 0.6f);

            // lean into the walk direction (in pelvis frame)
            float rel = Mathf.DeltaAngle(_pelvisYaw, moveYaw) * Mathf.Deg2Rad;
            float leanF = A.runLeanDeg * speedF * _gaitW * Mathf.Cos(rel);
            float leanS = A.runLeanDeg * 0.6f * speedF * _gaitW * Mathf.Sin(rel);
            float spin = p.rolling ? A.rollSpinDeg * p.rollSpin01 : 0f;
            r.RollPivot.localRotation = Quaternion.Slerp(E(spin + leanF + hopTuck * 8f, 0f, -leanS), E(-A.downedLieDeg, 0f, 0f), dw);

            // pelvis sway / twist
            float sway = A.hipSwayDeg * Mathf.Sin(twoPi * phL) * _gaitW;
            float hipTw = A.hipTwistDeg * Mathf.Cos(twoPi * phL) * _gaitW;
            r.Pelvis.localRotation = E(0f, -hipTw, sway);

            // ---- legs ----
            float splay = def.mounted ? 0f : 2.5f;
            if (def.mounted)
            {
                r.HipL.localRotation = E(-78f, 0f, -24f); r.HipR.localRotation = E(-78f, 0f, 24f);
                r.KneeL.localRotation = E(95f, 0f, 0f); r.KneeR.localRotation = E(95f, 0f, 0f);
                r.FootL.localRotation = E(-15f, 0f, 0f); r.FootR.localRotation = E(-15f, 0f, 0f);
            }
            else
            {
                float tuckThigh = A.tuckThighDeg * tuck + A.rowHopTuckDeg * hopTuck;
                float tuckKnee = A.tuckKneeDeg * tuck + A.rowHopTuckDeg * 1.8f * hopTuck;
                float dSpread = 12f * dw;
                SetLeg(r.HipL, r.KneeL, r.FootL, Mathf.Lerp(fL, 0f, tuck) + tuckThigh, Mathf.Lerp(kL, 0f, tuck) + tuckKnee, -splay - dSpread, fL);
                SetLeg(r.HipR, r.KneeR, r.FootR, Mathf.Lerp(fR, 0f, tuck) + tuckThigh, Mathf.Lerp(kR, 0f, tuck) + tuckKnee, splay + dSpread, fR);
            }

            // ---- spine / head ----
            float recoilSp = -A.recoilSpineDeg * _recoil;
            float flinch = -A.flinchDeg * _flinch;
            float counter = -hipTw * (A.shoulderCounterDeg / Mathf.Max(0.01f, A.hipTwistDeg)) * 1.6f;
            float spinePitch = A.tuckSpineDeg * tuck + recoilSp + flinch + 4f * _gaitW * speedF;
            r.Spine.localRotation = E(spinePitch, twist + counter, -sway * 0.6f);
            float bs = 1f + A.breathScale * breath * (1f - _gaitW * 0.5f);
            if (def.tail) bs = 1f + A.bossBreathScale * breath;
            r.Spine.localScale = new Vector3(1f + (bs - 1f) * 0.5f, bs, 1f + (bs - 1f) * 0.5f);
            r.Head.localRotation = E(tuck * 25f + flinch * 1.2f - dw * 10f + Mathf.Sin(_time * 1.3f) * A.idleSwayDeg * (1f - _gaitW), headYaw - counter * 0.5f, sway * 0.3f);
            if (r.Cape != null)
                r.Cape.localRotation = E(-(6f + 28f * speedF * _gaitW + 30f * rollW) + 3f * Mathf.Sin(_time * 2.2f + _phase * twoPi), 0f, 2f * Mathf.Sin(_time * 1.7f));

            // ---- arms ----
            float out0 = 7f + 14f * Mathf.Clamp01(def.bulk - 1f);
            float armSwing = A.armSwingDeg * _gaitW * Mathf.Lerp(0.5f, 1f, speedF);
            float aL = -armSwing * Mathf.Cos(twoPi * phL) * -1f, aR = -armSwing * Mathf.Cos(twoPi * phR) * -1f;
            // aL > 0 means the arm swings forward (opposite to its leg)
            aL = armSwing * Mathf.Cos(twoPi * phR);
            aR = armSwing * Mathf.Cos(twoPi * phL);
            float idleArm = A.idleArmDeg * breath * (1f - _gaitW);
            bool twoHanded = def.twoHanded;
            bool dual = def.weapon == Actor3DWeapon.DualPistol;

            // relaxed pose (two-handed weapons at port arms)
            Quaternion relL = E(-(aL + idleArm), 0f, -out0), relR = E(-(aR + idleArm), 0f, out0);
            Quaternion relEL = E(-(A.elbowBaseDeg + A.elbowSwingDeg * Mathf.Max(0f, aL / Mathf.Max(1f, A.armSwingDeg))), 0f, 0f);
            Quaternion relER = E(-(A.elbowBaseDeg + A.elbowSwingDeg * Mathf.Max(0f, aR / Mathf.Max(1f, A.armSwingDeg))), 0f, 0f);
            if (twoHanded && !def.mounted)
            {
                relR = E(-28f - aR * 0.3f, -10f, out0);
                relER = E(-70f, 0f, 0f);
                relL = E(-48f - aL * 0.3f, 32f, -out0 * 0.5f);
                relEL = E(-55f, 0f, 0f);
            }
            if (def.mounted)
            {
                relL = E(-30f, 0f, -out0); relR = E(-30f, 0f, out0);
                relEL = E(-40f, 0f, 0f); relER = E(-40f, 0f, 0f);
            }

            // aim pose
            float rk = A.recoilArmDeg * _recoil;
            Quaternion aimR = E(A.aimArmPitchDeg - rk, -8f, 4f);
            Quaternion aimER = E(-6f + rk * 0.3f, 0f, 0f);
            Quaternion aimL, aimEL;
            if (twoHanded) { aimL = E(A.supportArmPitchDeg - rk * 0.6f, A.supportArmInDeg, 0f); aimEL = E(-40f, 0f, 0f); }
            else if (dual) { aimL = E(A.aimArmPitchDeg - rk, 8f, -4f); aimEL = aimER; }
            else { aimL = relL; aimEL = relEL; }

            // boss fan shot: both arms spread
            if (_fanT >= 0f)
            {
                _fanT += dt;
                float fw = Mathf.Sin(Mathf.PI * Mathf.Clamp01(_fanT / 0.6f));
                if (_fanT > 0.6f) _fanT = -1f;
                aimR = Quaternion.Slerp(aimR, E(A.aimArmPitchDeg - 10f, A.fanSpreadDeg, 10f), fw);
                aimL = Quaternion.Slerp(aimL, E(A.aimArmPitchDeg - 10f, -A.fanSpreadDeg, -10f), fw);
            }
            float warnW = _warnFan ? 1f : 0f;
            _warnW = Mathf.Lerp(_warnW, warnW, Damp(8f, dt));

            float aw = Mathf.Max(_aimW, _warnW);
            Quaternion shL = Quaternion.Slerp(relL, aimL, aw), shR = Quaternion.Slerp(relR, aimR, aw);
            Quaternion elL = Quaternion.Slerp(relEL, aimEL, aw), elR = Quaternion.Slerp(relER, aimER, aw);

            // throw (dynamite): wind-up behind, then over the top
            if (_throwT >= 0f)
            {
                _throwT += dt;
                float t = _throwT / Mathf.Max(0.05f, A.throwTime);
                float ang = t < 0.4f ? Mathf.Lerp(0f, 110f, t / 0.4f) : t < 0.7f ? Mathf.Lerp(110f, A.throwArmDeg + 60f, (t - 0.4f) / 0.3f) : Mathf.Lerp(A.throwArmDeg + 60f, -40f, (t - 0.7f) / 0.3f);
                float tw = t < 0.85f ? 1f : Mathf.Clamp01((1f - t) / 0.15f);
                shR = Quaternion.Slerp(shR, E(-ang, 0f, 12f), tw);
                elR = Quaternion.Slerp(elR, E(-35f, 0f, 0f), tw);
                if (t >= 1f) _throwT = -1f;
            }

            // tuck while rolling: hug the knees
            if (tuck > 0.001f)
            {
                shL = Quaternion.Slerp(shL, E(A.tuckArmDeg, 15f, -10f), tuck);
                shR = Quaternion.Slerp(shR, E(A.tuckArmDeg, -15f, 10f), tuck);
                elL = Quaternion.Slerp(elL, E(-70f, 0f, 0f), tuck);
                elR = Quaternion.Slerp(elR, E(-70f, 0f, 0f), tuck);
            }
            // downed: arms flop out
            if (dw > 0.001f)
            {
                shL = Quaternion.Slerp(shL, E(-20f, 0f, -70f), dw);
                shR = Quaternion.Slerp(shR, E(-20f, 0f, 70f), dw);
                elL = Quaternion.Slerp(elL, E(-25f, 0f, 0f), dw);
                elR = Quaternion.Slerp(elR, E(-25f, 0f, 0f), dw);
            }
            // flinch: arms jerk up a little
            if (_flinch > 0.01f)
            {
                shL = shL * E(-20f * _flinch, 0f, -10f * _flinch);
                shR = shR * E(-20f * _flinch, 0f, 10f * _flinch);
            }
            r.ShoulderL.localRotation = shL; r.ShoulderR.localRotation = shR;
            r.ElbowL.localRotation = elL; r.ElbowR.localRotation = elR;
            // keep held guns pointing along the arm when aiming, hanging when relaxed
            r.HandR.localRotation = E(Mathf.Lerp(twoHanded ? 70f : 0f, 0f, aw), 0f, 0f);
            r.HandL.localRotation = E(Mathf.Lerp(dual ? 0f : 0f, 0f, aw), 0f, 0f);

            // ---- tail ----
            if (r.Tail != null) TickTail(dt, dw);

            // ---- horse ----
            if (def.mounted) TickHorse(dt, speed, moving);

            // ---- dizzy stars ----
            TickStars(dt, p.downed || p.defeated, dw);

            // ---- material (flash / tint) ----
            _flash = Mathf.Max(0f, _flash - dt * 6f);
            ApplyMaterial();
        }

        private void SetLeg(Transform hip, Transform knee, Transform foot, float thighFwd, float kneeBend, float splay, float rawFwd)
        {
            hip.localRotation = E(-thighFwd, 0f, splay);
            knee.localRotation = E(kneeBend, 0f, 0f);
            // keep the sole roughly parallel to the ground (foot plant), toe lifts at push-off
            float footPitch = (thighFwd - kneeBend) * A.footFlatten;
            foot.localRotation = E(footPitch, 0f, -splay);
        }

        private static readonly float[] TailRest = { -45f, -25f, 10f, 20f, 25f, 15f, 0f, 10f, 5f, 5f, 5f, 5f };

        private void TickTail(float dt, float dw)
        {
            var tail = Rig.Tail;
            float sweep = 0f;
            if (_sweepT >= 0f)
            {
                _sweepT += dt;
                float t = Mathf.Clamp01(_sweepT / Mathf.Max(0.05f, A.tailSweepTime));
                sweep = Mathf.Sin(Mathf.PI * t) * A.tailSweepDeg * _sweepSign;
                if (t >= 1f) _sweepT = -1f;
            }
            float coil = _warnTail ? A.tailWarnCoilDeg : 0f;
            for (int i = 0; i < tail.Length; i++)
            {
                float k = i / (float)Mathf.Max(1, tail.Length - 1);
                float sway = A.tailSwayDeg * Mathf.Sin(_time * A.tailSwayRate * Mathf.PI * 2f - i * 0.7f) * (0.4f + k);
                float yaw = sway + (i == 0 ? sweep : sweep * 0.25f) + coil * (k - 0.3f);
                float pitch = (i < TailRest.Length ? TailRest[i] : 0f) * (1f - dw * 0.8f);
                if (i == tail.Length - 1) pitch += 25f * Mathf.Abs(Mathf.Sin(_time * 18f)) * (_warnTail ? 1f : 0.2f);
                tail[i].localRotation = E(pitch, yaw, 0f);
            }
        }

        private void TickHorse(float dt, float speed, bool moving)
        {
            var r = Rig;
            float w = _gaitW;
            if (moving) _horsePhase += speed / Mathf.Max(0.2f, A.horseStrideLength) * dt;
            float twoPi = Mathf.PI * 2f;
            float[] off = { 0f, 0.5f, 0.5f, 0f }; // trot: diagonal pairs
            for (int i = 0; i < 4; i++)
            {
                float ph = _horsePhase + off[i];
                float f = A.horseLegSwingDeg * Mathf.Cos(twoPi * ph) * w;
                float k = 70f * A.Knee(ph) * w * (i < 2 ? 1f : -0.7f);
                r.HorseUpper[i].localRotation = E(-f, 0f, 0f);
                r.HorseLower[i].localRotation = E(i < 2 ? k : k, 0f, 0f);
            }
            float bob = A.horseBob * Mathf.Abs(Mathf.Sin(twoPi * _horsePhase)) * w + A.horseIdleBreath * Mathf.Sin(_time * 1.8f) * (1f - w);
            r.Horse.localPosition = new Vector3(0f, bob, 0f);
            r.HorseHead.localRotation = E(8f * Mathf.Sin(twoPi * _horsePhase * 2f) * w + 4f * Mathf.Sin(_time * 0.9f), 0f, 0f);
            // rider bounces with the horse
            var bp = r.Body.localPosition;
            r.Body.localPosition = bp;
            r.RollPivot.localPosition = new Vector3(0f, r.RollPivotHeight + bob * 1.4f, 0f);
        }

        private void TickStars(float dt, bool show, float dw)
        {
            var r = Rig;
            bool vis = show && dw > 0.5f;
            if (r.Stars.gameObject.activeSelf != vis) r.Stars.gameObject.SetActive(vis);
            if (!vis) return;
            _starSpin += A.dizzyStarSpinDeg * dt;
            var headLocal = r.Root.InverseTransformPoint(r.Head.TransformPoint(new Vector3(0f, r.Def.headRadius * 1.2f, 0f)));
            r.Stars.localPosition = headLocal + new Vector3(0f, 0.35f, 0f);
            r.Stars.localRotation = Quaternion.identity;
            int n = Mathf.Min(r.Stars.childCount, Mathf.Max(1, A.dizzyStarCount));
            for (int i = 0; i < r.Stars.childCount; i++)
            {
                var s = r.Stars.GetChild(i);
                s.gameObject.SetActive(i < n);
                float a = (_starSpin + i * 360f / n) * Mathf.Deg2Rad;
                s.localPosition = new Vector3(Mathf.Cos(a) * A.dizzyStarRadius, 0.06f * Mathf.Sin(a * 2f), Mathf.Sin(a) * A.dizzyStarRadius);
                s.localRotation = Quaternion.Euler(0f, 0f, _starSpin * 2f);
            }
            foreach (var sr in r.StarRenderers)
            {
                _mpb.Clear();
                _mpb.SetColor(TintId, _starColor);
                sr.SetPropertyBlock(_mpb);
            }
        }

        private void ApplyMaterial()
        {
            if (Mathf.Abs(_flash - _lastFlash) < 0.002f && _tint == _lastTint) return;
            _lastFlash = _flash;
            _lastTint = _tint;
            _mpb.Clear();
            _mpb.SetFloat(FlashId, _flash);
            _mpb.SetColor(TintId, _tint);
            foreach (var rr in Rig.Renderers) if (rr != null) rr.SetPropertyBlock(_mpb);
        }

        public void Destroy() => Rig.Destroy();
    }
}

using UnityEngine;

namespace HillNGun.Proto.TopDown
{
    public enum ProtoFacingDir
    {
        Down = 0,
        Up = 1,
        Side = 2
    }

    public enum ProtoAnimKind
    {
        Idle = 0,
        Run = 1,
        Shoot = 2
    }

    /// <summary>Pure facing / anim / move-smooth helpers for proto flipbooks (no MonoBehaviour).</summary>
    public static class ProtoFacingSelector
    {
        public const float SideElevDeg = 45f;
        public const float HysteresisDeg = 10f;
        public const float MinHoldSec = 0.15f;
        public const float MoveSpeedEps = 0.12f;
        public const float AimEpsSqr = 0.01f;
        public const float MoveEpsSqr = 0.01f;
        public const float RunBaseFps = 12f;
        public const float RunRateMin = 0.6f;
        public const float RunRateMax = 1.2f;
        public const float MoveTimeConstant = 0.08f;

        public struct Input
        {
            public Vector2 MoveVec;
            public Vector2 AimVec;
            public bool IsAiming;
            public bool IsShooting;
            public ProtoFacingDir CurrentFacing;
            public bool CurrentFlipX;
            public float TimeSinceFacingChange;
            public float Dt;
            public float ActualSpeed;
            public float MaxSpeed;
        }

        public struct Result
        {
            public ProtoFacingDir Facing;
            public bool FlipX;
            public ProtoAnimKind Anim;
            /// <summary>Absolute playback fps (run uses 12 × clamped speed ratio).</summary>
            public float PlaybackFps;
            public float TimeSinceFacingChange;
            public Vector2 FaceDir;
        }

        /// <summary>
        /// Elevation from horizontal in degrees (−90..90). Side when |elev| ≤ 45°,
        /// with ≈10° hysteresis at the ±45° boundaries.
        /// </summary>
        public static float ElevationDeg(Vector2 dir)
        {
            if (dir.sqrMagnitude < 1e-8f) return 0f;
            return Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg;
        }

        public static ProtoFacingDir FacingFromElevation(float elevDeg, ProtoFacingDir current, bool applyHysteresis)
        {
            if (!applyHysteresis)
            {
                if (elevDeg > SideElevDeg) return ProtoFacingDir.Up;
                if (elevDeg < -SideElevDeg) return ProtoFacingDir.Down;
                return ProtoFacingDir.Side;
            }

            // Enter side↔up/down at 55°; leave back to side at 35° (10° hysteresis around 45°).
            float enter = SideElevDeg + HysteresisDeg; // 55
            float leave = SideElevDeg - HysteresisDeg; // 35
            switch (current)
            {
                case ProtoFacingDir.Up:
                    if (elevDeg >= leave) return ProtoFacingDir.Up;
                    if (elevDeg <= -enter) return ProtoFacingDir.Down;
                    return ProtoFacingDir.Side;
                case ProtoFacingDir.Down:
                    if (elevDeg <= -leave) return ProtoFacingDir.Down;
                    if (elevDeg >= enter) return ProtoFacingDir.Up;
                    return ProtoFacingDir.Side;
                default:
                    if (elevDeg > enter) return ProtoFacingDir.Up;
                    if (elevDeg < -enter) return ProtoFacingDir.Down;
                    return ProtoFacingDir.Side;
            }
        }

        public static ProtoFacingDir FacingFromDir(Vector2 dir, ProtoFacingDir current, bool applyHysteresis)
        {
            return FacingFromElevation(ElevationDeg(dir), current, applyHysteresis);
        }

        /// <summary>
        /// Direction snaps to wish; only magnitude eases with time constant (~63% in timeConstant seconds).
        /// </summary>
        public static Vector2 SmoothVelocity(Vector2 current, Vector2 wish, float dt, float timeConstant = MoveTimeConstant)
        {
            float curMag = current.magnitude;
            float wishMag = wish.magnitude;
            float tau = Mathf.Max(1e-4f, timeConstant);
            float alpha = 1f - Mathf.Exp(-Mathf.Max(0f, dt) / tau);
            float newMag = Mathf.Lerp(curMag, wishMag, alpha);
            if (wishMag > 1e-6f) return wish.normalized * newMag;
            if (curMag > 1e-6f) return current.normalized * newMag;
            return Vector2.zero;
        }

        public static Result Select(in Input input)
        {
            Vector2 faceSrc = Vector2.zero;
            bool useAim = (input.IsAiming || input.IsShooting) && input.AimVec.sqrMagnitude >= AimEpsSqr;
            if (useAim) faceSrc = input.AimVec;
            else if (input.MoveVec.sqrMagnitude >= MoveEpsSqr) faceSrc = input.MoveVec;

            var facing = input.CurrentFacing;
            bool flipX = input.CurrentFlipX;
            float since = input.TimeSinceFacingChange + Mathf.Max(0f, input.Dt);
            Vector2 faceDir = FaceDirFromState(facing, flipX);

            if (faceSrc.sqrMagnitude >= AimEpsSqr)
            {
                var proposed = FacingFromDir(faceSrc, facing, true);
                bool proposedFlip = proposed == ProtoFacingDir.Side && faceSrc.x < 0f;
                bool changed = proposed != facing || (proposed == ProtoFacingDir.Side && proposedFlip != flipX);
                if (!changed)
                {
                    facing = proposed;
                    if (proposed == ProtoFacingDir.Side) flipX = proposedFlip;
                }
                else if (since >= MinHoldSec)
                {
                    facing = proposed;
                    flipX = proposed == ProtoFacingDir.Side && faceSrc.x < 0f;
                    since = 0f;
                }
                faceDir = faceSrc.normalized;
            }

            bool moving = input.ActualSpeed >= MoveSpeedEps || input.MoveVec.sqrMagnitude >= MoveEpsSqr;
            ProtoAnimKind anim;
            float fps;
            if (input.IsShooting)
            {
                anim = ProtoAnimKind.Shoot;
                fps = RunBaseFps;
            }
            else if (moving)
            {
                anim = ProtoAnimKind.Run;
                float max = Mathf.Max(0.01f, input.MaxSpeed);
                float ratio = Mathf.Clamp(input.ActualSpeed / max, RunRateMin, RunRateMax);
                fps = RunBaseFps * ratio;
            }
            else
            {
                anim = ProtoAnimKind.Idle;
                fps = 6f;
            }

            return new Result
            {
                Facing = facing,
                FlipX = facing == ProtoFacingDir.Side && flipX,
                Anim = anim,
                PlaybackFps = fps,
                TimeSinceFacingChange = since,
                FaceDir = faceDir.sqrMagnitude > 1e-8f ? faceDir.normalized : FaceDirFromState(facing, flipX)
            };
        }

        public static Vector2 FaceDirFromState(ProtoFacingDir facing, bool flipX)
        {
            switch (facing)
            {
                case ProtoFacingDir.Up: return Vector2.up;
                case ProtoFacingDir.Down: return Vector2.down;
                default: return flipX ? Vector2.left : Vector2.right;
            }
        }
    }
}

using UnityEngine;

namespace HillNGun.Proto.TopDown
{
    /// <summary>View-only flipbook player. Presentation owns one per actor Vis.</summary>
    public class ProtoFlipbookAnimator
    {
        ProtoFlipbookSet _set;
        ProtoPixelArt.PoseSet _fallback;
        SpriteRenderer _body;
        ProtoFacingDir _facing = ProtoFacingDir.Down;
        bool _flipX;
        float _sinceFacing = ProtoFacingSelector.MinHoldSec;
        ProtoAnimKind _anim = ProtoAnimKind.Idle;
        float _time;
        float _fps = 6f;
        bool _shootPlaying;

        public ProtoFacingDir Facing => _facing;
        public bool FlipX => _flipX;
        public ProtoAnimKind Anim => _anim;
        public bool ShootPlaying => _shootPlaying;

        public void Bind(SpriteRenderer body, ProtoFlipbookSet set, ProtoPixelArt.PoseSet fallback)
        {
            _body = body;
            _set = set;
            _fallback = fallback;
        }

        public void ResetFacing(ProtoFacingDir facing, bool flipX)
        {
            _facing = facing;
            _flipX = flipX;
            _sinceFacing = ProtoFacingSelector.MinHoldSec;
        }

        /// <summary>
        /// Advances facing/anim selection and applies the current sprite.
        /// Returns true when a flipbook frame was applied.
        /// </summary>
        public bool Tick(
            Vector2 moveVec,
            Vector2 aimVec,
            bool isAiming,
            bool isShooting,
            float actualSpeed,
            float maxSpeed,
            float dt)
        {
            if (isShooting && !_shootPlaying)
            {
                _shootPlaying = true;
                _time = 0f;
            }

            var input = new ProtoFacingSelector.Input
            {
                MoveVec = moveVec,
                AimVec = aimVec,
                IsAiming = isAiming,
                IsShooting = isShooting || _shootPlaying,
                CurrentFacing = _facing,
                CurrentFlipX = _flipX,
                TimeSinceFacingChange = _sinceFacing,
                Dt = dt,
                ActualSpeed = actualSpeed,
                MaxSpeed = maxSpeed
            };
            var result = ProtoFacingSelector.Select(input);

            if (result.Facing != _facing || result.FlipX != _flipX)
            {
                _facing = result.Facing;
                _flipX = result.FlipX;
                _sinceFacing = result.TimeSinceFacingChange;
                if (!_shootPlaying) _time = 0f;
            }
            else
            {
                _sinceFacing = result.TimeSinceFacingChange;
            }

            var next = _shootPlaying ? ProtoAnimKind.Shoot : result.Anim;
            if (next != _anim)
            {
                _anim = next;
                _time = 0f;
            }

            _fps = result.PlaybackFps;
            if (_set != null && _set.TryGet(_facing, _anim, out var meta) && meta.fps > 0.1f)
            {
                if (_anim == ProtoAnimKind.Run)
                    _fps = ProtoFacingSelector.RunBaseFps * Mathf.Clamp(
                        actualSpeed / Mathf.Max(0.01f, maxSpeed),
                        ProtoFacingSelector.RunRateMin,
                        ProtoFacingSelector.RunRateMax);
                else
                    _fps = meta.fps;
            }

            _time += Mathf.Max(0f, dt);
            return ApplySprite();
        }

        bool ApplySprite()
        {
            if (_body == null) return false;
            var clip = _set != null ? _set.Find(_facing, _anim) : null;
            if (clip == null || !clip.Ready)
            {
                if (_shootPlaying && _anim == ProtoAnimKind.Shoot)
                    _shootPlaying = false;
                ApplyFallback();
                return false;
            }

            int n = clip.FrameCount;
            int frame;
            if (clip.loop)
            {
                float period = n / Mathf.Max(0.1f, _fps);
                float t = period > 1e-4f ? Mathf.Repeat(_time, period) : 0f;
                frame = Mathf.Clamp(Mathf.FloorToInt(t * _fps), 0, n - 1);
            }
            else
            {
                frame = Mathf.Clamp(Mathf.FloorToInt(_time * _fps), 0, n - 1);
                if (_time * _fps >= n)
                {
                    frame = n - 1;
                    if (_anim == ProtoAnimKind.Shoot)
                        _shootPlaying = false;
                }
            }

            var spr = clip.frames[frame];
            if (spr == null)
            {
                ApplyFallback();
                return false;
            }
            _body.sprite = spr;
            _body.flipX = _facing == ProtoFacingDir.Side && _flipX && clip.flipXForLeft;
            return true;
        }

        void ApplyFallback()
        {
            if (_body == null || _fallback == null || !_fallback.Ready) return;
            var face = ProtoFacingSelector.FaceDirFromState(_facing, _flipX);
            if (Mathf.Abs(face.x) > Mathf.Abs(face.y) * 0.85f)
            {
                _body.sprite = _fallback.side;
                _body.flipX = face.x < 0f;
            }
            else
            {
                _body.flipX = false;
                _body.sprite = face.y >= 0f ? _fallback.back : _fallback.front;
            }
        }
    }
}

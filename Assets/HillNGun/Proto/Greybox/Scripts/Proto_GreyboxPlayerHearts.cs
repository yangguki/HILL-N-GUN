using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxPlayerHearts
    {
        int _maxHearts;
        int _hearts;
        float _regenSec;
        float _iFrameSec = Proto_GreyboxTuning.PlayerHitIFrameSec;
        float _regen01;
        float _iFrameRemaining;
        float _popShakeT;
        float _restoreFlashT;

        public int Hearts => _hearts;
        public int MaxHearts => _maxHearts;
        public float Regen01 => _regen01;
        public float IFrameRemaining => _iFrameRemaining;
        public float PopShake01 => _popShakeT > 0f ? Mathf.Clamp01(_popShakeT / 0.28f) : 0f;
        public float RestoreFlash01 => _restoreFlashT > 0f ? Mathf.Clamp01(_restoreFlashT / 0.25f) : 0f;
        public bool IsDead => _hearts <= 0;
        public bool IsInvulnerable => _iFrameRemaining > 0f;

        public void Configure(int maxHearts, float regenSec, float iFrameSec = -1f)
        {
            _maxHearts = Mathf.Max(1, maxHearts);
            _regenSec = Mathf.Max(0.1f, regenSec);
            if (iFrameSec > 0f)
                _iFrameSec = iFrameSec;
            ResetFull();
            _iFrameRemaining = 0f;
            _popShakeT = 0f;
            _restoreFlashT = 0f;
        }

        public void ResetFull()
        {
            _hearts = _maxHearts;
            _regen01 = 0f;
        }

        public bool TakeHit()
        {
            if (_iFrameRemaining > 0f || _hearts <= 0)
                return false;
            _hearts = Mathf.Max(0, _hearts - 1);
            _regen01 = 0f;
            _iFrameRemaining = _iFrameSec;
            _popShakeT = 0.28f;
            return true;
        }

        public void Tick(float dt, float regenSecOverride = -1f)
        {
            float regenSec = regenSecOverride > 0f ? regenSecOverride : _regenSec;
            if (_iFrameRemaining > 0f)
                _iFrameRemaining = Mathf.Max(0f, _iFrameRemaining - dt);
            if (_popShakeT > 0f)
                _popShakeT = Mathf.Max(0f, _popShakeT - dt);
            if (_restoreFlashT > 0f)
                _restoreFlashT = Mathf.Max(0f, _restoreFlashT - dt);

            if (_hearts <= 0 || _hearts >= _maxHearts)
            {
                _regen01 = 0f;
                return;
            }

            _regen01 += dt / regenSec;
            if (_regen01 < 1f) return;
            _hearts++;
            _regen01 = 0f;
            _restoreFlashT = 0.25f;
        }

        public int IndexOfRegenTarget() => _hearts;

        public bool IsHeartFilled(int index) => index < _hearts;

        public float FillForHeart(int index)
        {
            if (index < _hearts) return 1f;
            if (index > _hearts) return 0f;
            return Mathf.Clamp01(_regen01);
        }
    }
}
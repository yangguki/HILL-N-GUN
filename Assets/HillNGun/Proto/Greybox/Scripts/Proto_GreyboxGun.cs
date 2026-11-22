namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxGun
    {
        float _lastFireTime = -999f;
        float _nextRoundInsertTime;
        float _reloadStartTime;
        int _ammo = Proto_GreyboxTuning.MagazineSize;
        bool _reloading;
        int _reloadQueue;
        bool _casingSpawnedThisSequence;
        float _fireInterval = Proto_GreyboxTuning.FireInterval;
        int _magazineSize = Proto_GreyboxTuning.MagazineSize;
        float _reloadRoundInterval = Proto_GreyboxTuning.ReloadRoundInterval;
        int _cylinderRotationIndex;

        public int Ammo => _ammo;
        public bool IsReloading => _reloading;
        public int ReloadQueue => _reloadQueue;
        public int MagazineSize => _magazineSize;
        /// <summary>Chambers emptied since last full mag; casings spawn on first StartReload only after shots.</summary>
        public int SpentSinceFull { get; private set; }
        /// <summary>Rounds to show in casing VFX for this tap; 0 on queued taps.</summary>
        public int ReloadCasingCount { get; private set; }
        public int CylinderRotationIndex => _cylinderRotationIndex;

        public void ApplyRuntime(Proto_GreyboxRuntimeTuning tuning)
        {
            _fireInterval = tuning.FireInterval;
            _reloadRoundInterval = tuning.ReloadRoundInterval;
            _magazineSize = tuning.MagazineSize;
            _ammo = UnityEngine.Mathf.Clamp(_ammo, 0, _magazineSize);
        }

        public bool TryFire(float time)
        {
            if (time - _lastFireTime < _fireInterval - 1e-5f) return false;
            if (_ammo <= 0) return false;
            if (_reloading)
                CancelReload();

            _ammo--;
            _lastFireTime = time;
            _cylinderRotationIndex = (_cylinderRotationIndex + 1) % UnityEngine.Mathf.Max(1, _magazineSize);
            SpentSinceFull = _magazineSize - _ammo;
            _casingSpawnedThisSequence = false;
            return true;
        }

        public void StartReload(float time)
        {
            if (_ammo >= _magazineSize) return;
            int roundsStillNeeded = _magazineSize - _ammo;
            int pending = (_reloading ? 1 : 0) + _reloadQueue;
            if (pending >= roundsStillNeeded) return;

            if (!_reloading)
            {
                _reloading = true;
                ReloadCasingCount = !_casingSpawnedThisSequence && SpentSinceFull > 0 ? SpentSinceFull : 0;
                if (ReloadCasingCount > 0)
                    _casingSpawnedThisSequence = true;
                _reloadStartTime = time;
                _nextRoundInsertTime = time + _reloadRoundInterval;
            }
            else
            {
                _reloadQueue++;
                ReloadCasingCount = 0;
            }
        }

        public void CancelReload()
        {
            _reloading = false;
            _reloadQueue = 0;
        }

        public float ReloadProgress01(float time)
        {
            if (!_reloading) return 0f;
            if (_ammo >= _magazineSize) return 1f;
            float intoRound = time - (_nextRoundInsertTime - _reloadRoundInterval);
            return UnityEngine.Mathf.Clamp01(intoRound / _reloadRoundInterval);
        }

        public void Tick(float time)
        {
            if (!_reloading) return;
            if (_ammo < _magazineSize && time >= _nextRoundInsertTime - 1e-5f)
            {
                _ammo++;
                if (_ammo >= _magazineSize)
                {
                    _reloading = false;
                    _reloadQueue = 0;
                    SpentSinceFull = 0;
                    _casingSpawnedThisSequence = false;
                    return;
                }

                if (_reloadQueue > 0)
                {
                    _reloadQueue--;
                    _nextRoundInsertTime = time + _reloadRoundInterval;
                }
                else
                    _reloading = false;
            }
        }
    }
}
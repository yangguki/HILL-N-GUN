namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxGun
    {
        float _lastFireTime = -999f;
        float _reloadDoneTime;
        float _reloadStartTime;
        int _ammo = Proto_GreyboxTuning.MagazineSize;
        bool _reloading;
        float _fireInterval = Proto_GreyboxTuning.FireInterval;
        int _magazineSize = Proto_GreyboxTuning.MagazineSize;
        float _reloadDuration = Proto_GreyboxTuning.ReloadDuration;

        public int Ammo => _ammo;
        public bool IsReloading => _reloading;
        public int MagazineSize => _magazineSize;

        public void ApplyRuntime(Proto_GreyboxRuntimeTuning tuning)
        {
            _fireInterval = tuning.FireInterval;
            _reloadDuration = tuning.ReloadDuration;
            _magazineSize = tuning.MagazineSize;
        }

        public bool TryFire(float time)
        {
            if (_reloading) return false;
            if (time - _lastFireTime < _fireInterval - 1e-5f) return false;
            if (_ammo <= 0) return false;

            _ammo--;
            _lastFireTime = time;
            return true;
        }

        public void StartReload(float time)
        {
            if (_reloading) return;
            if (_ammo >= _magazineSize) return;
            _reloading = true;
            _reloadStartTime = time;
            _reloadDoneTime = time + _reloadDuration;
        }

        public void CancelReload()
        {
            _reloading = false;
        }

        public float ReloadProgress01(float time)
        {
            if (!_reloading) return 0f;
            return UnityEngine.Mathf.Clamp01((time - _reloadStartTime) / _reloadDuration);
        }

        public void Tick(float time)
        {
            if (!_reloading) return;
            if (time >= _reloadDoneTime)
            {
                _reloading = false;
                _ammo = _magazineSize;
                _lastFireTime = time - _fireInterval;
            }
        }
    }
}
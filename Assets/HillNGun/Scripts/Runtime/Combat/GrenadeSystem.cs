using HillNGun.Data;

namespace HillNGun.Combat
{
    public class GrenadeSystem
    {
        private readonly GrenadeData _data;
        private readonly int _maxGrenades;
        private int _currentGrenades;
        private float _rechargeTimer;

        public int CurrentGrenades => _currentGrenades;
        public int MaxGrenades => _maxGrenades;
        public float RechargeProgress => _data != null ? _rechargeTimer / _data.rechargeTime : 0f;

        public GrenadeSystem(GrenadeData data, int maxGrenades)
        {
            _data = data;
            _maxGrenades = maxGrenades;
            _currentGrenades = maxGrenades;
            _rechargeTimer = 0f;
        }

        public void Update(float deltaTime)
        {
            if (_data == null) return;

            if (_currentGrenades < _maxGrenades)
            {
                _rechargeTimer += deltaTime;

                if (_rechargeTimer >= _data.rechargeTime)
                {
                    _rechargeTimer = 0f;
                    _currentGrenades++;
                }
            }
        }

        public bool HasGrenade()
        {
            return _currentGrenades > 0;
        }

        public bool TryUseGrenade()
        {
            if (_currentGrenades <= 0) return false;

            _currentGrenades--;
            return true;
        }

        public void Refill()
        {
            _currentGrenades = _maxGrenades;
            _rechargeTimer = 0f;
        }

        public void Reset()
        {
            _currentGrenades = _maxGrenades;
            _rechargeTimer = 0f;
        }
    }
}

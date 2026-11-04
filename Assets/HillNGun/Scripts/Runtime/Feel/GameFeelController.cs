using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Feel
{
    public class GameFeelController : MonoBehaviour
    {
        [SerializeField] private FeelData _feelData;

        private float _hitStopTimer;
        private float _originalTimeScale = 1f;
        private bool _isInHitStop;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GameFeelController>();
            if (_isInHitStop)
            {
                Time.timeScale = _originalTimeScale;
            }
        }

        private void Update()
        {
            if (_isInHitStop)
            {
                _hitStopTimer -= Time.unscaledDeltaTime;
                if (_hitStopTimer <= 0f)
                {
                    EndHitStop();
                }
            }
        }

        public void TriggerHitStop(float duration)
        {
            if (_feelData != null && !_feelData.hitStopEnabled) return;

            if (_isInHitStop)
            {
                _hitStopTimer = Mathf.Max(_hitStopTimer, duration);
                return;
            }

            _originalTimeScale = Time.timeScale;
            _isInHitStop = true;
            _hitStopTimer = duration;
            Time.timeScale = 0.05f;
        }

        public void TriggerHitStopLight()
        {
            if (_feelData == null) return;
            TriggerHitStop(_feelData.hitStopLightDuration);
        }

        public void TriggerHitStopHeavy()
        {
            if (_feelData == null) return;
            TriggerHitStop(_feelData.hitStopHeavyDuration);
        }

        private void EndHitStop()
        {
            _isInHitStop = false;
            Time.timeScale = _originalTimeScale;
        }

        public void OnCharacterHit(int damage, bool isCrit)
        {
            var shake = ServiceLocator.Get<ScreenShake>();
            var audio = PlaceholderAudio.Instance;
            var effects = EffectsManager.Instance;

            if (damage >= 300)
            {
                TriggerHitStopHeavy();
                shake?.ShakeHeavyHit();
            }
            else
            {
                TriggerHitStopLight();
            }

            audio?.PlayHit(damage);
        }

        public void OnCoverHit(bool destroyed)
        {
            var shake = ServiceLocator.Get<ScreenShake>();
            var audio = PlaceholderAudio.Instance;

            if (destroyed)
            {
                shake?.ShakeCover();
                audio?.PlayCoverBreak();
            }
            else
            {
                audio?.PlayCoverHit();
            }
        }

        public void OnGrenadeExplode()
        {
            var shake = ServiceLocator.Get<ScreenShake>();
            var audio = PlaceholderAudio.Instance;

            TriggerHitStopHeavy();
            shake?.ShakeGrenade();
            audio?.PlayGrenadeExplode();
        }

        public void OnCharacterDowned()
        {
            TriggerHitStopHeavy();
            PlaceholderAudio.Instance?.PlayDowned();
        }

        public void OnCharacterRevived()
        {
            PlaceholderAudio.Instance?.PlayRevive();
        }

        public void SetFeelData(FeelData data)
        {
            _feelData = data;
        }
    }
}

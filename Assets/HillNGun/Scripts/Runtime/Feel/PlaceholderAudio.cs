using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using System.Collections.Generic;

namespace HillNGun.Feel
{
    public class PlaceholderAudio : MonoBehaviour
    {
        private static PlaceholderAudio _instance;
        public static PlaceholderAudio Instance => _instance;

        [Header("Settings")]
        [SerializeField] private bool _soundEnabled = true;
        [SerializeField] [Range(0f, 1f)] private float _masterVolume = 0.5f;
        [SerializeField] private AudioLibrary _audioLibrary;

        private AudioSource _sfxSource;
        private AudioSource _bgmSource;
        private Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();
        private bool _useLibrary;

        public bool SoundEnabled
        {
            get => _soundEnabled;
            set => _soundEnabled = value;
        }

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                UpdateBGMVolume();
            }
        }

        private void Awake()
        {
            _instance = this;
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;

            if (_audioLibrary == null)
            {
                _audioLibrary = ServiceLocator.Get<AudioLibrary>();
            }

            _useLibrary = _audioLibrary != null && _audioLibrary.shotLight != null;
            
            if (!_useLibrary)
            {
                GenerateClips();
            }

            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<PlaceholderAudio>();
            _instance = null;
        }

        public void SetAudioLibrary(AudioLibrary library)
        {
            _audioLibrary = library;
            _useLibrary = library != null && library.shotLight != null;
        }

        private void GenerateClips()
        {
            _clipCache["shot_light"] = GenerateShotSound(0.05f, 800f, 200f);
            _clipCache["shot_heavy"] = GenerateShotSound(0.08f, 400f, 150f);
            _clipCache["shot_shotgun"] = GenerateShotSound(0.1f, 300f, 100f);
            _clipCache["hit_light"] = GenerateHitSound(0.03f, 1200f);
            _clipCache["hit_heavy"] = GenerateHitSound(0.06f, 600f);
            _clipCache["reload_click"] = GenerateClickSound(0.02f, 1500f);
            _clipCache["reload_full"] = GenerateClickSound(0.04f, 2000f);
            _clipCache["roll"] = GenerateWhooshSound(0.15f);
            _clipCache["grenade_throw"] = GenerateWhooshSound(0.1f);
            _clipCache["grenade_explode"] = GenerateExplosionSound(0.2f);
            _clipCache["cover_hit"] = GenerateWoodHitSound(0.05f);
            _clipCache["cover_break"] = GenerateWoodBreakSound(0.15f);
            _clipCache["downed"] = GenerateDownedSound(0.2f);
            _clipCache["revive"] = GenerateReviveSound(0.3f);
            _clipCache["skill_activate"] = GenerateSkillSound(0.1f);
            _clipCache["charge_loop"] = GenerateChargeSound(0.5f);
            _clipCache["charge_ready"] = GeneratePingSound(0.1f);
            _clipCache["ui_click"] = GenerateClickSound(0.02f, 1000f);
        }

        private AudioClip GenerateShotSound(float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float envelope = (1f - t) * (1f - t);
                float noise = (Random.value * 2f - 1f) * 0.3f;
                samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t * duration) * 0.7f + noise) * envelope;
            }

            return CreateClip("shot", samples, sampleRate);
        }

        private AudioClip GenerateHitSound(float duration, float freq)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = (1f - t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope;
            }

            return CreateClip("hit", samples, sampleRate);
        }

        private AudioClip GenerateClickSound(float duration, float freq)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 30f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope;
            }

            return CreateClip("click", samples, sampleRate);
        }

        private AudioClip GenerateWhooshSound(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Sin(t * Mathf.PI);
                float noise = Random.value * 2f - 1f;
                float filtered = noise * (1f - t * 0.5f);
                samples[i] = filtered * envelope * 0.5f;
            }

            return CreateClip("whoosh", samples, sampleRate);
        }

        private AudioClip GenerateExplosionSound(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 8f);
                float noise = Random.value * 2f - 1f;
                float lowFreq = Mathf.Sin(2f * Mathf.PI * 60f * t * duration);
                samples[i] = (noise * 0.6f + lowFreq * 0.4f) * envelope;
            }

            return CreateClip("explosion", samples, sampleRate);
        }

        private AudioClip GenerateWoodHitSound(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 40f);
                float tone = Mathf.Sin(2f * Mathf.PI * 400f * t * duration);
                float noise = (Random.value * 2f - 1f) * 0.3f;
                samples[i] = (tone * 0.7f + noise) * envelope;
            }

            return CreateClip("wood_hit", samples, sampleRate);
        }

        private AudioClip GenerateWoodBreakSound(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 15f);
                float noise = Random.value * 2f - 1f;
                float crackle = Mathf.Sin(2f * Mathf.PI * (200f + Random.value * 300f) * t * duration);
                samples[i] = (noise * 0.5f + crackle * 0.5f) * envelope;
            }

            return CreateClip("wood_break", samples, sampleRate);
        }

        private AudioClip GenerateDownedSound(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(600f, 200f, t);
                float envelope = (1f - t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope * 0.5f;
            }

            return CreateClip("downed", samples, sampleRate);
        }

        private AudioClip GenerateReviveSound(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(400f, 800f, t);
                float envelope = Mathf.Sin(t * Mathf.PI);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope * 0.4f;
            }

            return CreateClip("revive", samples, sampleRate);
        }

        private AudioClip GenerateSkillSound(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = 1000f + Mathf.Sin(t * 20f) * 200f;
                float envelope = Mathf.Exp(-t * 10f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope * 0.5f;
            }

            return CreateClip("skill", samples, sampleRate);
        }

        private AudioClip GenerateChargeSound(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = 200f + t * 600f;
                float envelope = t * 0.3f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t * duration) * envelope;
            }

            return CreateClip("charge", samples, sampleRate);
        }

        private AudioClip GeneratePingSound(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 20f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 2000f * t * duration) * envelope * 0.4f;
            }

            return CreateClip("ping", samples, sampleRate);
        }

        private AudioClip CreateClip(string name, float[] samples, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void PlayShot(bool isHeavy = false, bool isShotgun = false)
        {
            if (_useLibrary)
            {
                AudioClip clip = isShotgun ? _audioLibrary.shotShotgun :
                                 isHeavy ? _audioLibrary.shotHeavy : _audioLibrary.shotLight;
                PlayClip(clip);
            }
            else
            {
                if (isShotgun) Play("shot_shotgun");
                else if (isHeavy) Play("shot_heavy");
                else Play("shot_light");
            }
        }

        public void PlayHit(int damage)
        {
            if (_useLibrary)
            {
                PlayClip(damage >= 300 ? _audioLibrary.hitHeavy : _audioLibrary.hitLight);
            }
            else
            {
                Play(damage >= 300 ? "hit_heavy" : "hit_light");
            }
        }

        public void PlayReloadClick()
        {
            if (_useLibrary) PlayClip(_audioLibrary.reloadClick);
            else Play("reload_click");
        }

        public void PlayReloadFull()
        {
            if (_useLibrary) PlayClip(_audioLibrary.reloadFull);
            else Play("reload_full");
        }

        public void PlayRoll()
        {
            if (_useLibrary) PlayClip(_audioLibrary.roll);
            else Play("roll");
        }

        public void PlayGrenadeThrow()
        {
            if (_useLibrary) PlayClip(_audioLibrary.grenadeThrow);
            else Play("grenade_throw");
        }

        public void PlayGrenadeExplode()
        {
            if (_useLibrary) PlayClip(_audioLibrary.grenadeExplode);
            else Play("grenade_explode");
        }

        public void PlayCoverHit()
        {
            if (_useLibrary) PlayClip(_audioLibrary.coverHit);
            else Play("cover_hit");
        }

        public void PlayCoverBreak()
        {
            if (_useLibrary) PlayClip(_audioLibrary.coverBreak);
            else Play("cover_break");
        }

        public void PlayDowned()
        {
            if (_useLibrary) PlayClip(_audioLibrary.downed);
            else Play("downed");
        }

        public void PlayRevive()
        {
            if (_useLibrary) PlayClip(_audioLibrary.revive);
            else Play("revive");
        }

        public void PlaySkillActivate()
        {
            if (_useLibrary) PlayClip(_audioLibrary.skillActivate);
            else Play("skill_activate");
        }

        public void PlayChargeReady()
        {
            if (_useLibrary) PlayClip(_audioLibrary.chargeReady);
            else Play("charge_ready");
        }

        public void PlayUIClick()
        {
            if (_useLibrary) PlayClip(_audioLibrary.uiClick);
            else Play("ui_click");
        }

        public void PlayBossRoar()
        {
            if (_useLibrary) PlayClip(_audioLibrary.bossRoar);
        }

        public void PlayBossWarning()
        {
            if (_useLibrary) PlayClip(_audioLibrary.bossWarning);
        }

        public void PlayVictory()
        {
            if (_useLibrary) PlayClip(_audioLibrary.victoryFanfare);
        }

        public void PlayDefeat()
        {
            if (_useLibrary) PlayClip(_audioLibrary.defeatSting);
        }

        public void PlayMenuBGM()
        {
            if (_useLibrary && _audioLibrary.bgmMenu != null)
            {
                PlayBGM(_audioLibrary.bgmMenu);
            }
        }

        public void PlayBattleBGM()
        {
            if (_useLibrary && _audioLibrary.bgmBattle != null)
            {
                PlayBGM(_audioLibrary.bgmBattle);
            }
        }

        public void PlayBossBGM()
        {
            if (_useLibrary && _audioLibrary.bgmBoss != null)
            {
                PlayBGM(_audioLibrary.bgmBoss);
            }
        }

        public void StopBGM()
        {
            if (_bgmSource != null && _bgmSource.isPlaying)
            {
                _bgmSource.Stop();
            }
        }

        private void PlayBGM(AudioClip clip)
        {
            if (_bgmSource == null || clip == null) return;

            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.clip = clip;
            _bgmSource.volume = GetBGMVolume();
            _bgmSource.Play();
        }

        private float GetBGMVolume()
        {
            if (_useLibrary)
            {
                return _masterVolume * _audioLibrary.bgmVolume;
            }
            return _masterVolume * 0.5f;
        }

        private void UpdateBGMVolume()
        {
            if (_bgmSource != null)
            {
                _bgmSource.volume = GetBGMVolume();
            }
        }

        private void PlayClip(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (!_soundEnabled || clip == null) return;

            float vol = _useLibrary ? _masterVolume * _audioLibrary.sfxVolume : _masterVolume;
            _sfxSource.PlayOneShot(clip, vol * volumeMultiplier);
        }

        private void Play(string clipName, float volumeMultiplier = 1f)
        {
            if (!_soundEnabled) return;
            if (!_clipCache.TryGetValue(clipName, out AudioClip clip)) return;

            _sfxSource.PlayOneShot(clip, _masterVolume * volumeMultiplier);
        }
    }
}

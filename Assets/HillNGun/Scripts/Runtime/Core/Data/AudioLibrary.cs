using UnityEngine;
using System;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "HillNGun/Data/AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 0.7f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float bgmVolume = 0.5f;

        [Header("Weapon Sounds")]
        public AudioClip shotLight;
        public AudioClip shotHeavy;
        public AudioClip shotShotgun;
        public AudioClip chargeLoop;
        public AudioClip chargeReady;

        [Header("Reload")]
        public AudioClip reloadClick;
        public AudioClip reloadFull;

        [Header("Movement")]
        public AudioClip roll;
        public AudioClip footstep;

        [Header("Grenade")]
        public AudioClip grenadeThrow;
        public AudioClip grenadeExplode;

        [Header("Cover")]
        public AudioClip coverHit;
        public AudioClip coverBreak;

        [Header("Character")]
        public AudioClip hitLight;
        public AudioClip hitHeavy;
        public AudioClip downed;
        public AudioClip revive;
        public AudioClip skillActivate;

        [Header("Boss")]
        public AudioClip bossRoar;
        public AudioClip bossWarning;
        public AudioClip bossPhaseChange;

        [Header("UI")]
        public AudioClip uiClick;
        public AudioClip uiHover;
        public AudioClip uiBack;

        [Header("Results")]
        public AudioClip victoryFanfare;
        public AudioClip defeatSting;

        [Header("BGM")]
        public AudioClip bgmMenu;
        public AudioClip bgmBattle;
        public AudioClip bgmBoss;

        public float GetEffectiveVolume(SoundCategory category)
        {
            float categoryVol = category == SoundCategory.BGM ? bgmVolume : sfxVolume;
            return masterVolume * categoryVol;
        }
    }

    public enum SoundCategory
    {
        SFX,
        BGM
    }
}

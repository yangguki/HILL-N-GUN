using UnityEngine;
using System;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "SpriteLibrary", menuName = "HillNGun/Data/SpriteLibrary")]
    public class SpriteLibrary : ScriptableObject
    {
        [Header("Characters")]
        public CharacterSprites[] characters;

        [Header("Enemies")]
        public EnemySprites[] enemies;

        [Header("Boss")]
        public BossSprites boss;

        [Header("Region bosses (deck portraits, lobby stage select)")]
        public RegionBossArt[] regionBosses;

        [Header("Covers")]
        public CoverSprites[] covers;
        [Tooltip("손상 단계별 색/투명도 (intact, cracked, damaged, destroyed). 전용 손상 그림이 없을 때 같은 그림에 곱함")]
        public Color[] coverStageTints =
        {
            Color.white,
            new Color(0.88f, 0.8f, 0.72f, 1f),
            new Color(0.68f, 0.55f, 0.46f, 0.9f),
            new Color(0.42f, 0.36f, 0.32f, 0.45f)
        };
        // Stretch cap lives here (CoverView.InitArtSprite). Wagon (cover_wagon) bypasses this and fits the ScalePreset box exactly once the wide cover_wagon_small.png is in.
        [Tooltip("엄폐물 그림을 ScalePreset 폭×높이 상자에 맞출 때 허용하는 가로/세로 늘림 한도 (그림 비율 왜곡 상한). 높이는 항상 정확히 맞춤. 마차는 CoverView에서 상한 없이 스펙 폭에 맞춤")]
        [Min(1f)] public float coverSpriteMaxStretch = 1.35f;
        [Tooltip("엄폐물 그림을 3D 캐릭터 레이어(깊이 테스트)에 그려서 엄폐물 뒤 캐릭터가 가려지게 함")]
        public bool coverSpritesOnActorLayer = true;

        /// <summary>Damage-stage tint for cover art (intact → destroyed). White when not configured.</summary>
        public Color CoverStageTint(HillNGun.Core.CoverStage stage)
        {
            int i = (int)stage;
            return coverStageTints != null && i >= 0 && i < coverStageTints.Length ? coverStageTints[i] : Color.white;
        }

        [Header("Projectiles")]
        public Sprite bullet;
        public Sprite bulletHeavy;
        public Sprite grenade;

        [Header("Effects")]
        public Sprite muzzleFlash;
        public Sprite hitSpark;
        public Sprite dustPuff;
        public Sprite explosion;

        [Header("Background")]
        public Sprite skyGradient;
        public Sprite mesas;
        public Sprite saloonFacades;
        public Sprite ground;

        [Header("UI")]
        public Sprite hpBarBg;
        public Sprite hpBarFill;
        public Sprite ammoCell;
        public Sprite ammoCellEmpty;
        public Sprite buttonBg;
        public Sprite joystickBg;
        public Sprite joystickKnob;

        public CharacterSprites GetCharacterSprites(string characterId)
        {
            if (characters == null) return null;
            foreach (var c in characters)
            {
                if (c != null && c.characterId == characterId)
                    return c;
            }
            return characters.Length > 0 ? characters[0] : null;
        }

        public EnemySprites GetEnemySprites(string enemyId)
        {
            if (enemies == null) return null;
            foreach (var e in enemies)
            {
                if (e != null && e.enemyId == enemyId)
                    return e;
            }
            return enemies.Length > 0 ? enemies[0] : null;
        }

        public Sprite GetRegionPortrait(string regionId)
        {
            if (regionBosses == null) return null;
            foreach (var r in regionBosses) if (r != null && r.regionId == regionId) return r.portrait;
            return null;
        }

        public CoverSprites GetCoverSprites(string coverId)
        {
            if (covers == null) return null;
            foreach (var c in covers)
            {
                if (c != null && c.coverId == coverId)
                    return c;
            }
            return covers.Length > 0 ? covers[0] : null;
        }
    }

    [Serializable]
    public class CharacterSprites
    {
        public string characterId;
        public Sprite idle;
        public Sprite shoot;
        public Sprite roll;
        public Sprite downed;
        [Tooltip("Large art for menus (character select, lobby). Optional: falls back to idle")]
        public Sprite portrait;
    }

    [Serializable]
    public class RegionBossArt
    {
        public string regionId;
        public Sprite portrait;
    }

    [Serializable]
    public class EnemySprites
    {
        public string enemyId;
        public Sprite idle;
    }

    [Serializable]
    public class BossSprites
    {
        public string bossId;
        public Sprite idle;
        public Sprite warningIndicator;
        public Sprite portrait;
    }

    [Serializable]
    public class CoverSprites
    {
        public string coverId;
        public Sprite intact;
        public Sprite cracked;
        public Sprite damaged;
        public Sprite destroyed;

        public Sprite GetSpriteForStage(HillNGun.Core.CoverStage stage)
        {
            switch (stage)
            {
                case Core.CoverStage.Intact: return intact;
                case Core.CoverStage.Cracked: return cracked;
                case Core.CoverStage.Damaged: return damaged;
                case Core.CoverStage.Destroyed: return destroyed;
                default: return intact;
            }
        }
    }
}

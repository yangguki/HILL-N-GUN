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

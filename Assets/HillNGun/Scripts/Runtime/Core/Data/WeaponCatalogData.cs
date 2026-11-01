using System;
using UnityEngine;

namespace HillNGun.Data
{
    [Serializable]
    public class WeaponOffer
    {
        public WeaponData weapon;
        [Tooltip("Fallback display name when the localization key is missing")]
        public string displayName;
        [Tooltip("Fallback one-liner (role / feel) when the localization key is missing")]
        public string description;
        [Tooltip("Bounty price. 0 + ownedByDefault = the character's starting gun")]
        public int cost;
        public bool ownedByDefault;
        [Tooltip("Characters that can equip it (role identity: 03 역할별 무기군). Empty = everyone")]
        public string[] characterIds = new string[0];

        public string Id => weapon != null ? weapon.id : null;

        public bool AllowedFor(string characterId)
        {
            if (characterIds == null || characterIds.Length == 0) return true;
            foreach (var c in characterIds) if (c == characterId) return true;
            return false;
        }
    }

    /// <summary>
    /// Gun shop / loadout catalog (t48u). Every entry is a WeaponData SO (speed, fire rate, damage, range, magazine =
    /// 총알 게이지). Starting guns keep the 03 balance numbers; shop guns are proposals tuned against the bounty economy
    /// (see implementation_status.md). Equipped guns replace the character's CharacterData.weapon at match start.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponCatalog", menuName = "HillNGun/Data/Weapon Catalog")]
    public class WeaponCatalogData : ScriptableObject
    {
        public WeaponOffer[] offers = new WeaponOffer[0];

        public WeaponOffer Find(string weaponId)
        {
            if (offers == null || string.IsNullOrEmpty(weaponId)) return null;
            foreach (var o in offers) if (o != null && o.Id == weaponId) return o;
            return null;
        }
    }
}

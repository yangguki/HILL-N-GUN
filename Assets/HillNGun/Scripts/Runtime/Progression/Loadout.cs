using System.Collections.Generic;
using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Progression
{
    /// <summary>
    /// Applies the saved gun loadout at match start: returns the CharacterData unchanged when the starting gun is
    /// equipped (default balance untouched), otherwise a cached runtime copy whose weapon is the equipped shop gun.
    /// Only the player's team uses loadouts; PvP enemies / PvE allies-as-bots keep the same data path.
    /// </summary>
    public static class Loadout
    {
        private static readonly Dictionary<string, CharacterData> _cache = new Dictionary<string, CharacterData>();

        public static CharacterData Resolve(CharacterData data, ProgressionService progression = null)
        {
            if (data == null) return null;
            var p = progression ?? ProgressionService.Instance;
            var gun = p?.GetEquippedWeapon(data.id);
            return WithWeapon(data, gun);
        }

        public static CharacterData WithWeapon(CharacterData data, WeaponData gun)
        {
            if (data == null || gun == null || gun == data.weapon) return data;
            string key = data.GetInstanceID() + "|" + gun.GetInstanceID();
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var copy = Object.Instantiate(data);
            copy.name = data.name;
            copy.weapon = gun;
            copy.hideFlags = HideFlags.DontSave;
            _cache[key] = copy;
            return copy;
        }
    }
}

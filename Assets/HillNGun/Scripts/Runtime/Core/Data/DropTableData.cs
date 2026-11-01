using System;
using UnityEngine;

namespace HillNGun.Data
{
    public enum DropType { WeaponBoost, Heal, Ammo, Grenade, Coins }

    [Serializable]
    public class DropEntry
    {
        public DropType type;
        [Tooltip("Relative weight in the roll")]
        public float weight = 1f;
        [Tooltip("Heal: ratio of max HP · WeaponBoost: damage multiplier")]
        public float value;
        [Tooltip("WeaponBoost: seconds")]
        public float duration;
        [Tooltip("Grenade: count · Coins: bounty")]
        public int amount;
        public Color color = Color.white;
    }

    /// <summary>
    /// Field drops (t48u): destroyed covers and defeated enemies may drop a pickup that sparkles on the ground and is
    /// collected by walking over it. Chances and contents are tuned so stage times stay inside the 03 targets
    /// (numbers + measured times in implementation_status.md).
    /// </summary>
    [CreateAssetMenu(fileName = "DropTable", menuName = "HillNGun/Data/Drop Table")]
    public class DropTableData : ScriptableObject
    {
        [Header("Chances")]
        [Range(0f, 1f)] public float coverDropChance = 0.35f;
        [Range(0f, 1f)] public float enemyDropChance = 0.18f;
        [Tooltip("Bigger enemies (gaugeValue >= this) use the elite chance")]
        public int eliteGaugeValue = 3;
        [Range(0f, 1f)] public float eliteDropChance = 0.35f;
        [Tooltip("PvP: enemy-character eliminations drop too")]
        public bool dropsInPvPFromCharacters = false;

        [Header("Contents")]
        public DropEntry[] entries = new DropEntry[]
        {
            new DropEntry { type = DropType.WeaponBoost, weight = 18f, value = 1.25f, duration = 8f, color = new Color(1f, 0.55f, 0.15f) },
            new DropEntry { type = DropType.Heal, weight = 26f, value = 0.25f, color = new Color(0.45f, 0.95f, 0.45f) },
            new DropEntry { type = DropType.Ammo, weight = 20f, color = new Color(1f, 0.88f, 0.4f) },
            new DropEntry { type = DropType.Grenade, weight = 14f, amount = 1, color = new Color(0.95f, 0.3f, 0.22f) },
            new DropEntry { type = DropType.Coins, weight = 22f, amount = 25, color = new Color(1f, 0.82f, 0.2f) },
        };

        [Header("Pickup")]
        [Tooltip("Horizontal pickup reach (tiles)")]
        public float pickupRadius = 0.8f;
        [Tooltip("Vertical tolerance (tiles): the item lies on a row; characters on that row can take it")]
        public float rowTolerance = 0.9f;
        [Tooltip("Seconds on the ground before it disappears (blinks for the last blinkTime)")]
        public float lifetime = 12f;
        public float blinkTime = 3f;
        [Tooltip("Toss flight time from the source to the landing spot")]
        public float tossTime = 0.45f;
        [Tooltip("Max items on the field at once (oldest removed first)")]
        public int maxActive = 6;
        [Tooltip("Random horizontal scatter when landing (tiles)")]
        public float landScatter = 0.9f;

        [Header("View")]
        public float iconSize = 0.62f;
        public float sparkleRate = 3.5f;

        public float TotalWeight
        {
            get { float w = 0f; if (entries != null) foreach (var e in entries) if (e != null) w += Mathf.Max(0f, e.weight); return w; }
        }

        /// <summary>Pick an entry by weight with r in [0,1).</summary>
        public DropEntry Pick(float r)
        {
            float total = TotalWeight;
            if (total <= 0f || entries == null) return null;
            float x = Mathf.Clamp01(r) * total;
            DropEntry last = null;
            foreach (var e in entries)
            {
                if (e == null || e.weight <= 0f) continue;
                last = e;
                if (x < e.weight) return e;
                x -= e.weight;
            }
            return last;
        }
    }
}

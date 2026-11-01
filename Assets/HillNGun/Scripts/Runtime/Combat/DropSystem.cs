using System;
using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;

namespace HillNGun.Combat
{
    public class DropItem
    {
        public int Id;
        public DropEntry Entry;
        public DropType Type => Entry.type;
        public Vector2 From;
        public Vector2 Position;   // landing spot (logic); pickup only after landing
        public float Age;
        public float TossTime;
        public bool Landed => Age >= TossTime;
        /// <summary>Team allowed to take it (PvE: the player team; PvP: the destroyer's team).</summary>
        public Team Team;
    }

    /// <summary>
    /// Pure logic for field drops: roll (DropTableData chances / weights), spawn, age, walk-over pickup and effect.
    /// No rendering here (DropView reads Items). Injected RNG for tests.
    /// </summary>
    public class DropSystem
    {
        private readonly DropTableData _table;
        private readonly Func<float> _rng;
        private readonly List<DropItem> _items = new List<DropItem>();
        private int _nextId = 1;

        public IReadOnlyList<DropItem> Items => _items;
        public DropTableData Table => _table;
        public int Spawned { get; private set; }
        public int PickedUp { get; private set; }
        public int Expired { get; private set; }
        public int CoinsCollected { get; private set; }
        public readonly int[] PickedByType = new int[5];

        public event Action<DropItem> OnSpawned;
        public event Action<DropItem, CharacterStateMachine> OnPicked;

        public DropSystem(DropTableData table, Func<float> rng = null)
        {
            _table = table;
            _rng = rng ?? (() => UnityEngine.Random.value);
        }

        /// <summary>Roll a drop with the given chance. Returns the entry or null.</summary>
        public DropEntry Roll(float chance)
        {
            if (_table == null || chance <= 0f) return null;
            if (_rng() >= chance) return null;
            return _table.Pick(_rng() * 0.99999f);
        }

        public DropItem Spawn(DropEntry entry, Vector2 from, Vector2 land, Team team)
        {
            if (entry == null || _table == null) return null;
            if (_table.maxActive > 0 && _items.Count >= _table.maxActive) _items.RemoveAt(0);
            var it = new DropItem { Id = _nextId++, Entry = entry, From = from, Position = land, Age = 0f, TossTime = Mathf.Max(0f, _table.tossTime), Team = team };
            _items.Add(it);
            Spawned++;
            OnSpawned?.Invoke(it);
            return it;
        }

        /// <summary>Roll + spawn in one go.</summary>
        public DropItem TryDrop(float chance, Vector2 from, Vector2 land, Team team)
        {
            var e = Roll(chance);
            return e != null ? Spawn(e, from, land, team) : null;
        }

        /// <summary>Age items, expire old ones, and let active characters of the right team walk over them.</summary>
        public void Tick(float dt, IReadOnlyList<CharacterStateMachine> pickers)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var it = _items[i];
                it.Age += dt;
                if (it.Age >= it.TossTime + _table.lifetime) { _items.RemoveAt(i); Expired++; continue; }
                if (!it.Landed || pickers == null) continue;
                for (int k = 0; k < pickers.Count; k++)
                {
                    var c = pickers[k];
                    if (c == null || c.Team != it.Team || c.OverallState != CharacterOverallState.Active) continue;
                    if (!InReach(c.Position, it.Position)) continue;
                    if (!Apply(c, it)) continue; // e.g. full grenades: leave it for someone else
                    _items.RemoveAt(i);
                    PickedUp++;
                    PickedByType[(int)it.Type]++;
                    OnPicked?.Invoke(it, c);
                    break;
                }
            }
        }

        public bool InReach(Vector2 c, Vector2 item) =>
            Mathf.Abs(c.x - item.x) <= _table.pickupRadius && Mathf.Abs(c.y - item.y) <= _table.rowTolerance;

        /// <summary>Apply the pickup. Returns false if it would do nothing (item stays).</summary>
        public bool Apply(CharacterStateMachine c, DropItem it)
        {
            var e = it.Entry;
            switch (e.type)
            {
                case DropType.Heal:
                    if (c.CurrentHp >= c.MaxHp) return false;
                    c.Heal(e.value);
                    return true;
                case DropType.Ammo:
                    if (c.AmmoGauge != null && c.AmmoGauge.IsFull) return false;
                    c.RefillAmmo();
                    return true;
                case DropType.Grenade:
                    return c.GrenadeSystem != null && c.GrenadeSystem.AddGrenades(Mathf.Max(1, e.amount));
                case DropType.WeaponBoost:
                    c.ApplyDamageBoost(e.value, e.duration);
                    return true;
                case DropType.Coins:
                    CoinsCollected += Mathf.Max(0, e.amount);
                    return true;
            }
            return false;
        }

        public void Clear() { _items.Clear(); }

        public string Summary =>
            $"spawned={Spawned} picked={PickedUp} expired={Expired} onField={_items.Count} coins=${CoinsCollected} " +
            $"[boost {PickedByType[0]} heal {PickedByType[1]} ammo {PickedByType[2]} gren {PickedByType[3]} coin {PickedByType[4]}]";
    }
}

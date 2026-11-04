using UnityEngine;
using HillNGun.Core;
using HillNGun.Combat;

namespace HillNGun.PvP
{
    /// <summary>
    /// PvP late-round supply crate (FP 256 제안): drops in the middle, either team shoots it open.
    /// Pure logic; SupplyCrateView draws it. Opening team is decided by the projectile that breaks it.
    /// </summary>
    public class SupplyCrate : INeutralTarget
    {
        public Vector2 Position { get; private set; }
        public float HitRadius { get; private set; }
        public Team Team => Team.Player; // ignored for hit filtering (INeutralTarget)
        public bool CanBeHit => IsActive && !IsOpened;
        public bool IsActive { get; private set; }
        public bool IsOpened { get; private set; }
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public float HpRatio => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;

        public event System.Action<SupplyCrate> OnBroken;

        public void Drop(Vector2 position, int hp, float radius)
        {
            Position = position;
            MaxHp = Mathf.Max(1, hp);
            CurrentHp = MaxHp;
            HitRadius = radius;
            IsActive = true;
            IsOpened = false;
        }

        public void ReceiveHit(int damage, Vector2 direction)
        {
            if (!CanBeHit) return;
            CurrentHp = Mathf.Max(0, CurrentHp - damage);
            if (CurrentHp == 0)
            {
                IsOpened = true;
                OnBroken?.Invoke(this);
            }
        }

        public void Clear()
        {
            IsActive = false;
            IsOpened = false;
        }
    }
}

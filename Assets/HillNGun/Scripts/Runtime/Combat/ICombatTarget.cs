using UnityEngine;
using HillNGun.Core;

namespace HillNGun.Combat
{
    /// <summary>Non-character hittable (PvE enemy, boss). Pure logic.</summary>
    public interface ICombatTarget
    {
        Vector2 Position { get; }
        float HitRadius { get; }
        Team Team { get; }
        bool CanBeHit { get; }
        void ReceiveHit(int damage, Vector2 direction);
    }
}

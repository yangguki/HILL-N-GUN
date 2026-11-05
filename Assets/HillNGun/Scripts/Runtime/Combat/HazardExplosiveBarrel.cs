using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Feel;

namespace HillNGun.Combat
{
    /// <summary>06 §5 explosive_barrel: accumulate 400 HP or grenade → explode 700/350 r=2.</summary>
    public class HazardExplosiveBarrel : INeutralTarget
    {
        public const int TriggerHp = 400;
        public const int CenterDamage = 700;
        public const int EdgeDamage = 350;
        public const float Radius = 2f;

        public Vector2 Position { get; private set; }
        public float HitRadius => 0.45f;
        public Team Team => Team.Enemy; // ignored via INeutralTarget
        public bool CanBeHit => !Exploded;
        public bool Exploded { get; private set; }
        public int Accumulated { get; private set; }
        public bool Cracked => Accumulated >= TriggerHp / 2 && !Exploded;

        public event System.Action<HazardExplosiveBarrel> OnExploded;

        private CombatSystem _combat;

        public void Initialize(Vector2 pos, CombatSystem combat)
        {
            Position = pos;
            _combat = combat;
            Accumulated = 0;
            Exploded = false;
            combat?.RegisterCombatTarget(this);
        }

        public void ReceiveHit(int damage, Vector2 direction)
        {
            if (Exploded) return;
            Accumulated += Mathf.Max(0, damage);
            if (Accumulated >= TriggerHp) Explode();
        }

        /// <summary>Grenade contact / overlap: instant explode.</summary>
        public void TriggerByGrenade()
        {
            if (!Exploded) Explode();
        }

        private void Explode()
        {
            if (Exploded) return;
            Exploded = true;
            _combat?.UnregisterCombatTarget(this);
            // Deal AoE via CombatSystem grenade-like path if available
            _combat?.ExplodeHazard(Position, Radius, CenterDamage, EdgeDamage, 1.5f);
            EffectsManager.Instance?.SpawnExplosion(Position, Radius);
            EffectsManager.Instance?.SpawnCoverDebris(Position, 1.2f, true);
            OnExploded?.Invoke(this);
        }
    }
}

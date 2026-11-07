using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Combat
{
    public class ProjectilePool
    {
        private readonly List<Projectile> _projectiles;
        private readonly int _maxProjectiles;

        public IReadOnlyList<Projectile> ActiveProjectiles => _projectiles;
        public int ActiveCount { get; private set; }

        public ProjectilePool(int maxProjectiles = 120)
        {
            _maxProjectiles = maxProjectiles;
            _projectiles = new List<Projectile>(maxProjectiles);

            for (int i = 0; i < maxProjectiles; i++)
            {
                _projectiles.Add(new Projectile());
            }

            ActiveCount = 0;
        }

        public Projectile Spawn(Vector2 startPos, Vector2 direction, WeaponData weapon, int damage, Team team, int pierces = 0)
        {
            for (int i = 0; i < _projectiles.Count; i++)
            {
                if (!_projectiles[i].IsActive)
                {
                    _projectiles[i].Initialize(startPos, direction, weapon, damage, team, pierces);
                    ActiveCount++;
                    return _projectiles[i];
                }
            }

            return null;
        }

        public Projectile SpawnSimple(Vector2 startPos, Vector2 direction, float speed, float radius, int damage, float range, Team team, bool applyGlobalSpeedMul = true)
        {
            for (int i = 0; i < _projectiles.Count; i++)
            {
                if (!_projectiles[i].IsActive)
                {
                    _projectiles[i].InitializeSimple(startPos, direction, speed, radius, damage, range, team, applyGlobalSpeedMul);
                    ActiveCount++;
                    return _projectiles[i];
                }
            }

            return null;
        }

        public void Update(float deltaTime)
        {
            ActiveCount = 0;
            for (int i = 0; i < _projectiles.Count; i++)
            {
                if (_projectiles[i].IsActive)
                {
                    _projectiles[i].Update(deltaTime);
                    if (_projectiles[i].IsActive)
                    {
                        ActiveCount++;
                    }
                }
            }
        }

        public void DeactivateProjectile(Projectile projectile)
        {
            if (projectile != null && projectile.IsActive)
            {
                projectile.Deactivate();
                ActiveCount = Mathf.Max(0, ActiveCount - 1);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _projectiles.Count; i++)
            {
                _projectiles[i].Reset();
            }
            ActiveCount = 0;
        }
    }
}

using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;

namespace HillNGun.Combat
{
    public class Projectile
    {
        public Vector2 Position { get; private set; }
        public Vector2 Direction { get; private set; }
        /// <summary>Runtime speed (base x WeaponData.GlobalProjectileSpeedMul, fixed at spawn).</summary>
        public float Speed { get; private set; }
        /// <summary>Base (asset) speed before the global factor.</summary>
        public float BaseSpeed { get; private set; }
        /// <summary>Range-limited lifetime of this projectile = Range / Speed (recomputed per projectile).</summary>
        public float Lifetime => Speed > 0.0001f ? Range / Speed : 0f;
        public float Radius { get; private set; }
        public int Damage { get; private set; }
        public float Range { get; private set; }
        public Team OwnerTeam { get; private set; }
        public bool IsActive { get; private set; }
        /// <summary>Weapon falloff start (0 = none). Applied via EffectiveDamage.</summary>
        public float FalloffStart { get; private set; }
        public float FalloffEndMul { get; private set; } = 1f;
        public HillNGun.Data.WeaponVfxData Vfx { get; private set; }
        /// <summary>Damage after distance falloff (before weakpoint).</summary>
        public int EffectiveDamage
        {
            get
            {
                if (FalloffStart <= 0f) return Damage;
                if (_distanceTraveled <= FalloffStart) return Damage;
                float end = Mathf.Max(Range, FalloffStart + 0.001f);
                float t = Mathf.InverseLerp(FalloffStart, end, _distanceTraveled);
                return Mathf.RoundToInt(Damage * Mathf.Lerp(1f, FalloffEndMul, t));
            }
        }

        // Modifiers (set per shot by CombatSystem; defaults = plain bullet)
        /// <summary>Character that fired this (null for enemy/boss bullets).</summary>
        public HillNGun.Characters.CharacterStateMachine Owner { get; private set; }
        public float CoverDamageMul { get; private set; } = 1f;
        public float AfterPierceMul { get; private set; } = 0.8f;
        /// <summary>Damage multiplier when the bullet hits a target's weak point (1 = none).</summary>
        public float WeakpointMul { get; private set; } = 1f;
        public float WeakpointRadius { get; private set; }
        public bool HasPierce => _piercesRemaining > 0;

        public void SetModifiers(HillNGun.Characters.CharacterStateMachine owner, float coverDamageMul,
            float afterPierceMul, float weakpointMul, float weakpointRadius)
        {
            Owner = owner;
            CoverDamageMul = coverDamageMul;
            AfterPierceMul = afterPierceMul;
            WeakpointMul = weakpointMul;
            WeakpointRadius = weakpointRadius;
        }

        private void ClearModifiers()
        {
            Owner = null;
            CoverDamageMul = 1f;
            AfterPierceMul = 0.8f;
            WeakpointMul = 1f;
            WeakpointRadius = 0f;
        }

        private Vector2 _startPosition;
        private float _distanceTraveled;
        private int _piercesRemaining;
        /// <summary>Distance flown so far (view: trail length is clamped to it).</summary>
        public float DistanceTraveled => _distanceTraveled;

        public void Initialize(Vector2 startPos, Vector2 direction, WeaponData weapon, int damage, Team team, int pierces = 0)
        {
            Position = startPos;
            _startPosition = startPos;
            Direction = direction.normalized;
            BaseSpeed = weapon.projectileSpeed;
            Speed = weapon.EffectiveProjectileSpeed; // global factor applied once, here
            Radius = weapon.projectileRadius;
            Damage = damage;
            Range = weapon.range;
            FalloffStart = weapon.damageFalloffStart;
            FalloffEndMul = weapon.damageFalloffEndMul;
            Vfx = weapon.vfx;
            OwnerTeam = team;
            _distanceTraveled = 0f;
            _piercesRemaining = pierces;
            LastPiercedCover = null;
            ClearModifiers();
            IsActive = true;
        }

        /// <param name="speed">BASE speed (data value). The global factor is applied here once unless applyGlobalSpeedMul is false
        /// (thrown dynamite, whose speed is derived from a fixed flight time).</param>
        public void InitializeSimple(Vector2 startPos, Vector2 direction, float speed, float radius, int damage, float range, Team team, bool applyGlobalSpeedMul = true)
        {
            Position = startPos;
            _startPosition = startPos;
            Direction = direction.normalized;
            BaseSpeed = speed;
            Speed = applyGlobalSpeedMul ? WeaponData.GlobalSpeed(speed) : speed;
            Radius = radius;
            Damage = damage;
            Range = range;
            FalloffStart = 0f;
            FalloffEndMul = 1f;
            Vfx = null;
            OwnerTeam = team;
            _distanceTraveled = 0f;
            _piercesRemaining = 0;
            LastPiercedCover = null;
            ClearModifiers();
            IsActive = true;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            Vector2 previousPosition = Position;
            float moveDistance = Speed * deltaTime;
            Position += Direction * moveDistance;
            _distanceTraveled += moveDistance;

            if (_distanceTraveled >= Range)
            {
                Deactivate();
            }
        }

        public bool CheckHit(Vector2 targetCenter, float targetRadius, out float hitT)
        {
            hitT = 0f;
            if (!IsActive) return false;

            Vector2 previousPosition = Position - Direction * Speed * GameTime.DeltaTime;
            
            return CoordinateHelper.SweptCircleCircleIntersection(
                previousPosition,
                Position,
                Radius,
                targetCenter,
                targetRadius,
                out hitT
            );
        }

        public bool CheckRectHit(Vector2 rectCenter, float width, float height, out float hitT)
        {
            hitT = 0f;
            if (!IsActive) return false;

            Vector2 previousPosition = Position - Direction * Speed * GameTime.DeltaTime;
            
            float expandedWidth = width + Radius * 2f;
            float expandedHeight = height + Radius * 2f;

            return CoordinateHelper.LineRectIntersection(
                previousPosition,
                Position,
                rectCenter,
                expandedWidth,
                expandedHeight,
                out hitT
            );
        }

        /// <summary>Cover just pierced (skipped by the collision pass so the same cover is not hit twice).</summary>
        public object LastPiercedCover { get; private set; }

        public void OnHitCover() => OnHitCover(null);

        /// <summary>
        /// Pierce: first cover takes damage and is passed (damage x afterPierceMul), the next one stops it. Indestructible
        /// rock: TakeDamage is ignored (0) but the pierce is still consumed (12 §3.1 제안 — same as existing behaviour).
        /// </summary>
        public void OnHitCover(object cover)
        {
            if (_piercesRemaining > 0)
            {
                LastPiercedCover = cover;
                _piercesRemaining--;
                Damage = Mathf.RoundToInt(Damage * AfterPierceMul);
            }
            else
            {
                Deactivate();
            }
        }

        public void OnHitTarget()
        {
            Deactivate();
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Reset()
        {
            IsActive = false;
            ClearModifiers();
            Position = Vector2.zero;
            Direction = Vector2.zero;
            _distanceTraveled = 0f;
        }
    }
}

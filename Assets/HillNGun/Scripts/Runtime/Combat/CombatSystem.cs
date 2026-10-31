using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.Cover;
using HillNGun.Battlefield;

namespace HillNGun.Combat
{
    public class CombatSystem : MonoBehaviour
    {
        [SerializeField] private FeelData _feelData;
        [SerializeField] private MatchRulesData _matchRules;

        private ProjectilePool _projectilePool;
        private List<GrenadeProjectile> _grenades;
        private List<CoverInstance> _covers;
        private List<CharacterStateMachine> _characters;
        private List<TargetInstance> _targets;
        private List<ICombatTarget> _combatTargets;

        public FeelData FeelData => _feelData;
        public IReadOnlyList<CoverInstance> Covers => _covers;
        public IReadOnlyList<CharacterStateMachine> Characters => _characters;
        public ProjectilePool ProjectilePool => _projectilePool;

        public event System.Action<Projectile, CharacterStateMachine, int> OnCharacterHit;
        public event System.Action<Projectile, CoverInstance, int> OnCoverHit;
        public event System.Action<Projectile, TargetInstance, int> OnTargetHit;
        public event System.Action<GrenadeProjectile, Vector2> OnGrenadeExploded;
        public event System.Action<Projectile, ICombatTarget, int> OnCombatTargetHit;

        private void Awake()
        {
            _projectilePool = new ProjectilePool(120);
            _grenades = new List<GrenadeProjectile>(16);
            _covers = new List<CoverInstance>(32);
            _characters = new List<CharacterStateMachine>(8);
            _targets = new List<TargetInstance>(16);
            _combatTargets = new List<ICombatTarget>(32);

            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<CombatSystem>();
        }

        private void Update()
        {
            float dt = GameTime.DeltaTime;

            _projectilePool.Update(dt);
            UpdateGrenades(dt);
            ProcessCollisions();
        }

        public void RegisterCharacter(CharacterStateMachine character)
        {
            if (!_characters.Contains(character))
            {
                _characters.Add(character);
                character.OnFired += HandleCharacterFired;
            }
        }

        public void UnregisterCharacter(CharacterStateMachine character)
        {
            if (_characters.Contains(character))
            {
                _characters.Remove(character);
                character.OnFired -= HandleCharacterFired;
            }
        }

        public void RegisterCover(CoverInstance cover)
        {
            if (!_covers.Contains(cover))
            {
                _covers.Add(cover);
            }
        }

        public void RegisterTarget(TargetInstance target)
        {
            if (!_targets.Contains(target))
            {
                _targets.Add(target);
            }
        }

        public void UnregisterTarget(TargetInstance target)
        {
            _targets.Remove(target);
        }

        /// <summary>PvE enemies / boss (non-character hittables).</summary>
        public void RegisterCombatTarget(ICombatTarget target)
        {
            if (target != null && !_combatTargets.Contains(target)) _combatTargets.Add(target);
        }

        public void UnregisterCombatTarget(ICombatTarget target)
        {
            _combatTargets.Remove(target);
        }

        public void ClearCombatTargets()
        {
            _combatTargets.Clear();
        }

        private void HandleCharacterFired(CharacterStateMachine character, Vector2 direction, int damage)
        {
            if (character.Data?.weapon == null) return;

            var weapon = character.Data.weapon;
            Vector2 muzzlePos = character.Position + direction * weapon.muzzleForwardOffset;

            float[] angles = weapon.GetSpreadAngles();
            int pierces = 0;

            if (character.IsSkillActive && character.Data.skill?.id == "skill_pierce")
            {
                pierces = (int)character.Data.skill.GetParam("pierceCovers", 1);
            }

            foreach (float angle in angles)
            {
                Vector2 bulletDir = CoordinateHelper.RotateDirection(direction, angle);
                _projectilePool.Spawn(muzzlePos, bulletDir, weapon, damage, character.Team, pierces);
            }
        }

        public void SpawnGrenade(Vector2 start, Vector2 target, GrenadeData data, Team team)
        {
            var grenade = new GrenadeProjectile();
            grenade.Initialize(start, target, data, team);
            _grenades.Add(grenade);
        }

        public void SpawnEnemyProjectile(Vector2 start, Vector2 direction, float speed, float radius, int damage, float range, Team team)
        {
            _projectilePool.SpawnSimple(start, direction, speed, radius, damage, range, team);
        }

        private void UpdateGrenades(float dt)
        {
            for (int i = _grenades.Count - 1; i >= 0; i--)
            {
                var grenade = _grenades[i];
                if (!grenade.IsActive)
                {
                    _grenades.RemoveAt(i);
                    continue;
                }

                grenade.Update(dt);

                if (grenade.HasLanded())
                {
                    ExplodeGrenade(grenade);
                    grenade.Deactivate();
                    _grenades.RemoveAt(i);
                }
            }
        }

        private void ExplodeGrenade(GrenadeProjectile grenade)
        {
            Vector2 explosionPos = grenade.TargetPosition;
            var data = grenade.Data;

            OnGrenadeExploded?.Invoke(grenade, explosionPos);

            foreach (var character in _characters)
            {
                if (character.OverallState == CharacterOverallState.Eliminated) continue;
                if (!_matchRules.friendlyFire && character.Team == grenade.OwnerTeam) continue;

                float dist = CoordinateHelper.Distance(character.Position, explosionPos);
                if (dist <= data.radius)
                {
                    int damage = data.GetDamageAtDistance(dist);

                    if (character.OverallState == CharacterOverallState.Downed)
                    {
                        character.TakeDownedDamage(damage);
                    }
                    else
                    {
                        character.TakeDamage(damage, (explosionPos - character.Position).normalized);
                    }

                    if (data.knockback > 0f && character.OverallState == CharacterOverallState.Active)
                    {
                        Vector2 knockbackDir = (character.Position - explosionPos).normalized;
                        character.SetPosition(character.Position + knockbackDir * data.knockback);
                    }
                }
            }

            foreach (var target in _targets)
            {
                float dist = CoordinateHelper.Distance(target.Position, explosionPos);
                if (dist <= data.radius)
                {
                    int damage = data.GetDamageAtDistance(dist);
                    target.TakeDamage(damage);
                }
            }

            if (!data.ignoresCover)
            {
                foreach (var cover in _covers)
                {
                    if (cover.IsDestroyed) continue;

                    Rect bounds = cover.GetBounds();
                    float dist = DistanceToRect(explosionPos, bounds);
                    if (dist <= data.radius)
                    {
                        int damage = data.GetDamageAtDistance(dist);
                        cover.TakeDamage(damage, data.coverDamageMul);
                    }
                }
            }

            if (_feelData != null)
            {
                GameTime.ApplyHitstop(_feelData.hitstopGrenade);
            }
        }

        private void ProcessCollisions()
        {
            foreach (var projectile in _projectilePool.ActiveProjectiles)
            {
                if (!projectile.IsActive) continue;

                ProcessProjectileCollisions(projectile);
            }
        }

        private void ProcessProjectileCollisions(Projectile projectile)
        {
            float closestT = float.MaxValue;
            CoverInstance hitCover = null;
            CharacterStateMachine hitCharacter = null;
            TargetInstance hitTarget = null;
            ICombatTarget hitCombatTarget = null;
            bool ownCoverBlocks = _matchRules != null && _matchRules.ownCoverBlocksOwnShots;

            foreach (var cover in _covers)
            {
                if (cover.IsDestroyed) continue;
                if (!ownCoverBlocks && cover.Team == projectile.OwnerTeam) continue;

                Rect bounds = cover.GetBounds();
                if (projectile.CheckRectHit(bounds.center, bounds.width, bounds.height, out float t))
                {
                    if (t < closestT)
                    {
                        closestT = t;
                        hitCover = cover;
                        hitCharacter = null;
                        hitTarget = null;
                        hitCombatTarget = null;
                    }
                }
            }

            foreach (var character in _characters)
            {
                if (character.OverallState == CharacterOverallState.Eliminated) continue;
                if (!_matchRules.friendlyFire && character.Team == projectile.OwnerTeam) continue;
                if (character.IsInvulnerable) continue;

                if (projectile.CheckHit(character.Position, character.HitRadius, out float t))
                {
                    if (t < closestT)
                    {
                        closestT = t;
                        hitCharacter = character;
                        hitCover = null;
                        hitTarget = null;
                        hitCombatTarget = null;
                    }
                }
            }

            foreach (var target in _targets)
            {
                if (target.IsDestroyed) continue;
                if (target.Team == projectile.OwnerTeam) continue;

                if (projectile.CheckHit(target.Position, target.HitRadius, out float t))
                {
                    if (t < closestT)
                    {
                        closestT = t;
                        hitTarget = target;
                        hitCharacter = null;
                        hitCover = null;
                        hitCombatTarget = null;
                    }
                }
            }

            for (int i = 0; i < _combatTargets.Count; i++)
            {
                var ct = _combatTargets[i];
                if (ct == null || !ct.CanBeHit) continue;
                if (ct.Team == projectile.OwnerTeam) continue;

                if (projectile.CheckHit(ct.Position, ct.HitRadius, out float t))
                {
                    if (t < closestT)
                    {
                        closestT = t;
                        hitCombatTarget = ct;
                        hitTarget = null;
                        hitCharacter = null;
                        hitCover = null;
                    }
                }
            }

            if (hitCover != null)
            {
                hitCover.TakeDamage(projectile.Damage);
                OnCoverHit?.Invoke(projectile, hitCover, projectile.Damage);
                projectile.OnHitCover();
                ApplyHitFeel(projectile.Damage, false);
            }
            else if (hitCharacter != null)
            {
                int damage = projectile.Damage;

                if (hitCharacter.Data?.weakpointRadius > 0)
                {
                    float distToCenter = CoordinateHelper.Distance(
                        projectile.Position, 
                        hitCharacter.Position
                    );
                    
                    if (distToCenter <= hitCharacter.Data.weakpointRadius)
                    {
                        var attacker = GetAttacker(projectile.OwnerTeam);
                        if (attacker != null)
                        {
                            damage = Mathf.RoundToInt(damage * attacker.GetWeakpointBonusMultiplier());
                        }
                    }
                }

                if (hitCharacter.OverallState == CharacterOverallState.Downed)
                {
                    hitCharacter.TakeDownedDamage(damage);
                }
                else
                {
                    hitCharacter.TakeDamage(damage, projectile.Direction);
                }

                OnCharacterHit?.Invoke(projectile, hitCharacter, damage);
                projectile.OnHitTarget();
                ApplyHitFeel(damage, true);
            }
            else if (hitTarget != null)
            {
                hitTarget.TakeDamage(projectile.Damage);
                OnTargetHit?.Invoke(projectile, hitTarget, projectile.Damage);
                projectile.OnHitTarget();
                ApplyHitFeel(projectile.Damage, true);
            }
            else if (hitCombatTarget != null)
            {
                int dmg = projectile.Damage;
                hitCombatTarget.ReceiveHit(dmg, projectile.Direction);
                OnCombatTargetHit?.Invoke(projectile, hitCombatTarget, dmg);
                projectile.OnHitTarget();
                ApplyHitFeel(dmg, true);
            }
        }

        private CharacterStateMachine GetAttacker(Team team)
        {
            foreach (var character in _characters)
            {
                if (character.Team == team && character.OverallState == CharacterOverallState.Active)
                {
                    return character;
                }
            }
            return null;
        }

        private void ApplyHitFeel(int damage, bool isCharacterHit)
        {
            if (_feelData == null) return;

            float hitstop = _feelData.GetHitstopDuration(damage);
            if (hitstop > 0f)
            {
                GameTime.ApplyHitstop(hitstop);
            }
        }

        private float DistanceToRect(Vector2 point, Rect rect)
        {
            float dx = Mathf.Max(rect.xMin - point.x, 0f, point.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - point.y, 0f, point.y - rect.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public void Clear()
        {
            _projectilePool.Clear();
            _grenades.Clear();

            foreach (var cover in _covers)
            {
                cover.Reset();
            }
        }
    }

    public class TargetInstance
    {
        public TargetData Data { get; private set; }
        public Vector2 Position { get; set; }
        public int CurrentHp { get; private set; }
        public float HitRadius => Data?.hitRadius ?? 0.5f;
        public Team Team { get; private set; }
        public bool IsDestroyed => CurrentHp <= 0;

        public event System.Action<TargetInstance, int> OnDamaged;
        public event System.Action<TargetInstance> OnDestroyed;

        public void Initialize(TargetData data, Vector2 position, Team team = Team.Enemy)
        {
            Data = data;
            Position = position;
            CurrentHp = data.hp;
            Team = team;
        }

        public void TakeDamage(int damage)
        {
            if (IsDestroyed) return;

            CurrentHp = Mathf.Max(0, CurrentHp - damage);
            OnDamaged?.Invoke(this, damage);

            if (CurrentHp <= 0)
            {
                OnDestroyed?.Invoke(this);
            }
        }

        public void Reset()
        {
            CurrentHp = Data?.hp ?? 3000;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.Cover;
using HillNGun.Battlefield;
using HillNGun.Feel;

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
        public IReadOnlyList<ICombatTarget> CombatTargets => _combatTargets;

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
                character.OnSkillActivated += HandleSkillActivated;
            }
        }

        public void UnregisterCharacter(CharacterStateMachine character)
        {
            if (_characters.Contains(character))
            {
                _characters.Remove(character);
                character.OnFired -= HandleCharacterFired;
                character.OnSkillActivated -= HandleSkillActivated;
            }
        }

        /// <summary>Extra cover damage multiplier for every hit (PvP late-round decay; 1 = off).</summary>
        public float GlobalCoverDamageMul { get; set; } = 1f;
        public event System.Action<CoverInstance> OnCoverDestroyed;

        public void RegisterCover(CoverInstance cover)
        {
            if (!_covers.Contains(cover))
            {
                _covers.Add(cover);
                cover.OnDestroyed += c => OnCoverDestroyed?.Invoke(c);
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

        public event System.Action<CharacterStateMachine, CoverInstance, int> OnCoverRepaired;
        public event System.Action<CharacterStateMachine, SkillEffect> OnSkillEffect;

        private void HandleCharacterFired(CharacterStateMachine character, Vector2 direction, int damage)
        {
            if (character.Data?.weapon == null) return;

            var weapon = character.Data.weapon;
            var skill = character.Data.skill;
            Vector2 muzzlePos = character.Position + direction * weapon.muzzleForwardOffset;

            // 버스터 돌파 사격: wider fixed spread + cover damage x2 while active (03 §2)
            float spread = weapon.spreadDeg;
            float coverMul = weapon.coverDamageMul;
            if (character.IsSpreadBoostActive && skill != null)
            {
                spread = skill.GetParam("spreadDeg", spread);
                coverMul *= skill.GetParam("coverDamageMul", 1f);
            }

            // 레이븐 관통탄: only the next shot (03 §2 "다음 한 발")
            int pierces = 0;
            float afterPierceMul = 0.8f;
            if (character.ConsumePierceShot() && skill != null)
            {
                pierces = Mathf.RoundToInt(skill.GetParam("pierceCovers", 1f));
                afterPierceMul = skill.GetParam("afterPierceMul", 0.8f);
                OnSkillEffect?.Invoke(character, SkillEffect.PierceNextShot);
            }

            // 레이븐 패시브: aim held >= aimTime → weak-point hits x1.25 (01 §5)
            float wpMul = character.GetWeakpointBonusMultiplier();
            float wpRadius = character.Data.passive != null ? character.Data.passive.GetParam("weakpointRadius", 0.2f) : 0f;

            float[] angles = weapon.GetSpreadAngles(spread);
            foreach (float angle in angles)
            {
                Vector2 bulletDir = CoordinateHelper.RotateDirection(direction, angle);
                var p = _projectilePool.Spawn(muzzlePos, bulletDir, weapon, damage, character.Team, pierces);
                p?.SetModifiers(character, coverMul, afterPierceMul, wpMul, wpRadius);
            }
        }

        private void HandleSkillActivated(CharacterStateMachine character, SkillEffect effect, Vector2? target)
        {
            OnSkillEffect?.Invoke(character, effect);
            if (effect != SkillEffect.RepairCover) return;

            // 볼트 긴급 보수: nearest own (not destroyed) cover to the aimed point, within targetRange (03 §2)
            var skill = character.Data.skill;
            float range = skill != null ? skill.targetRange : 6f;
            Vector2 aim = target ?? character.Position;
            if (Vector2.Distance(aim, character.Position) > range)
                aim = character.Position + (aim - character.Position).normalized * range;

            CoverInstance best = null;
            float bestDist = float.MaxValue;
            foreach (var cover in _covers)
            {
                if (cover.IsDestroyed || cover.Team != character.Team) continue;
                if (Vector2.Distance(cover.Position, character.Position) > range + cover.Data.width * 0.5f) continue;
                float d = DistanceToRect(aim, cover.GetBounds());
                if (d < bestDist) { bestDist = d; best = cover; }
            }
            if (best == null) return;

            int before = best.CurrentHp;
            best.Repair(skill != null ? skill.GetParam("repairPercent", 0.4f) : 0.4f);
            OnCoverRepaired?.Invoke(character, best, best.CurrentHp - before);
        }

        /// <summary>Own-team cover most in need of repair within range (for bots / autopilot). Null if none below ratio.</summary>
        public CoverInstance FindCoverToRepair(CharacterStateMachine character, float belowRatio)
        {
            float range = character.Data?.skill != null ? character.Data.skill.targetRange : 6f;
            CoverInstance best = null;
            float bestRatio = belowRatio;
            foreach (var cover in _covers)
            {
                if (cover.IsDestroyed || cover.Team != character.Team || !cover.Data.repairable) continue;
                if (Vector2.Distance(cover.Position, character.Position) > range) continue;
                if (cover.HpRatio < bestRatio) { bestRatio = cover.HpRatio; best = cover; }
            }
            return best;
        }

        public void SpawnGrenade(Vector2 start, Vector2 target, GrenadeData data, Team team)
        {
            var grenade = new GrenadeProjectile();
            grenade.Initialize(start, target, data, team);
            _grenades.Add(grenade);
        }

        public void SpawnEnemyProjectile(Vector2 start, Vector2 direction, float speed, float radius, int damage, float range, Team team, float coverDamageMul = 1f)
        {
            var p = _projectilePool.SpawnSimple(start, direction, speed, radius, damage, range, team);
            if (p != null && !Mathf.Approximately(coverDamageMul, 1f))
                p.SetModifiers(null, coverDamageMul, 0.8f, 1f, 0f);
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

            // PvE enemies / boss inside the blast
            for (int i = _combatTargets.Count - 1; i >= 0; i--)
            {
                if (i >= _combatTargets.Count) continue; // list may be cleared by a stage ending mid-loop
                var ct = _combatTargets[i];
                if (ct == null || !ct.CanBeHit || (ct.Team == grenade.OwnerTeam && !(ct is INeutralTarget))) continue;
                float dist = Mathf.Max(0f, CoordinateHelper.Distance(ct.Position, explosionPos) - ct.HitRadius * 0.5f);
                if (dist <= data.radius)
                {
                    if (ct is HazardExplosiveBarrel barrel) { barrel.TriggerByGrenade(); continue; }
                    int damage = data.GetDamageAtDistance(dist);
                    ct.ReceiveHit(damage, (ct.Position - explosionPos).normalized);
                    OnCombatTargetHit?.Invoke(null, ct, damage);
                }
            }

            // Covers always take blast damage x coverDamageMul (01 §6 수류탄 ×1.5).
            // ignoresCover only means characters behind cover are still hit (01 §8, decision D4).
            foreach (var cover in _covers)
            {
                if (cover.IsDestroyed) continue;
                if (!data.ignoresCover && cover.Team == grenade.OwnerTeam) continue;

                Rect bounds = cover.GetBounds();
                float dist = DistanceToRect(explosionPos, bounds);
                if (dist <= data.radius)
                {
                    int damage = data.GetDamageAtDistance(dist);
                    cover.TakeDamage(damage, data.coverDamageMul * GlobalCoverDamageMul);
                }
            }

            if (_feelData != null)
            {
                GameTime.ApplyHitstop(_feelData.hitstopGrenade);
            }
        }

        /// <summary>Hazard barrel AoE (06 §5): center/edge damage over radius, cover × coverMul.</summary>
        public void ExplodeHazard(Vector2 explosionPos, float radius, int centerDamage, int edgeDamage, float coverMul)
        {
            foreach (var character in _characters)
            {
                if (character.OverallState == CharacterOverallState.Eliminated) continue;
                float dist = CoordinateHelper.Distance(character.Position, explosionPos);
                if (dist > radius) continue;
                float t = radius > 0f ? dist / radius : 1f;
                int damage = Mathf.RoundToInt(Mathf.Lerp(centerDamage, edgeDamage, t));
                if (character.OverallState == CharacterOverallState.Downed) character.TakeDownedDamage(damage);
                else character.TakeDamage(damage, (explosionPos - character.Position).normalized);
            }
            for (int i = _combatTargets.Count - 1; i >= 0; i--)
            {
                if (i >= _combatTargets.Count) continue;
                var ct = _combatTargets[i];
                if (ct == null || !ct.CanBeHit || ct is HazardExplosiveBarrel) continue;
                float dist = Mathf.Max(0f, CoordinateHelper.Distance(ct.Position, explosionPos) - ct.HitRadius * 0.5f);
                if (dist > radius) continue;
                float t = radius > 0f ? dist / radius : 1f;
                int damage = Mathf.RoundToInt(Mathf.Lerp(centerDamage, edgeDamage, t));
                ct.ReceiveHit(damage, (ct.Position - explosionPos).normalized);
            }
            foreach (var cover in _covers)
            {
                if (cover.IsDestroyed) continue;
                float dist = DistanceToRect(explosionPos, cover.GetBounds());
                if (dist > radius) continue;
                float t = radius > 0f ? dist / radius : 1f;
                int damage = Mathf.RoundToInt(Mathf.Lerp(centerDamage, edgeDamage, t));
                cover.TakeDamage(damage, coverMul * GlobalCoverDamageMul);
            }
            if (_feelData != null) GameTime.ApplyHitstop(_feelData.hitstopGrenade);
        }

        /// <summary>
        /// Would a bullet from shooterTeam flying from → to be stopped by a cover? Same skip rules as the projectile
        /// collision pass (destroyed covers never block; own covers only when ownCoverBlocksOwnShots). Used by tests /
        /// smoke to prove a destroyed cover loses its cover judgment.
        /// </summary>
        public bool CoverBlocksSegment(Vector2 from, Vector2 to, Team shooterTeam)
        {
            bool ownCoverBlocks = _matchRules != null && _matchRules.ownCoverBlocksOwnShots;
            foreach (var cover in _covers)
            {
                if (cover == null || cover.IsDestroyed) continue;
                if (!ownCoverBlocks && cover.Team == shooterTeam) continue;
                if (SegmentHitsRect(from, to, cover.GetBounds())) return true;
            }
            return false;
        }

        public static bool SegmentHitsRect(Vector2 a, Vector2 b, Rect r)
        {
            float t0 = 0f, t1 = 1f;
            Vector2 d = b - a;
            float[] p = { -d.x, d.x, -d.y, d.y };
            float[] q = { a.x - r.xMin, r.xMax - a.x, a.y - r.yMin, r.yMax - a.y };
            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Approximately(p[i], 0f)) { if (q[i] < 0f) return false; continue; }
                float t = q[i] / p[i];
                if (p[i] < 0f) { if (t > t1) return false; if (t > t0) t0 = t; }
                else { if (t < t0) return false; if (t < t1) t1 = t; }
            }
            return true;
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
                if (ct.Team == projectile.OwnerTeam && !(ct is INeutralTarget)) continue;

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
                hitCover.TakeDamage(projectile.Damage, projectile.CoverDamageMul * GlobalCoverDamageMul);
                OnCoverHit?.Invoke(projectile, hitCover, projectile.Damage);
                projectile.OnHitCover();
                ApplyHitFeel(projectile.Damage, false);
                EffectsManager.Instance?.SpawnPatternImpact(hitCover.Position, true, projectile.Vfx);
            }
            else if (hitCharacter != null)
            {
                int damage = ApplyWeakpoint(projectile, hitCharacter.Position, projectile.EffectiveDamage);

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
                EffectsManager.Instance?.SpawnPatternImpact(hitCharacter.Position, false, projectile.Vfx);
            }
            else if (hitTarget != null)
            {
                int td = projectile.EffectiveDamage;
                hitTarget.TakeDamage(td);
                OnTargetHit?.Invoke(projectile, hitTarget, td);
                projectile.OnHitTarget();
                ApplyHitFeel(projectile.Damage, true);
            }
            else if (hitCombatTarget != null)
            {
                int dmg = ApplyWeakpoint(projectile, hitCombatTarget.Position, projectile.EffectiveDamage);
                hitCombatTarget.ReceiveHit(dmg, projectile.Direction);
                OnCombatTargetHit?.Invoke(projectile, hitCombatTarget, dmg);
                projectile.OnHitTarget();
                ApplyHitFeel(dmg, true);
            }
        }

        public event System.Action<Projectile, Vector2, int> OnWeakpointHit;

        /// <summary>Weak point = centre radius from the shooter's passive; applies to any target (decision D5).</summary>
        private int ApplyWeakpoint(Projectile projectile, Vector2 targetCenter, int baseDamage = -1)
        {
            int damage = baseDamage >= 0 ? baseDamage : projectile.EffectiveDamage;
            if (projectile.WeakpointMul <= 1f || projectile.WeakpointRadius <= 0f) return damage;
            // closest approach of this frame's sweep to the centre
            Vector2 prev = projectile.Position - projectile.Direction * projectile.Speed * GameTime.DeltaTime;
            Vector2 seg = projectile.Position - prev;
            float t = seg.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(targetCenter - prev, seg) / seg.sqrMagnitude) : 1f;
            float d = Vector2.Distance(prev + seg * t, targetCenter);
            if (d > projectile.WeakpointRadius) return damage;
            damage = Mathf.RoundToInt(damage * projectile.WeakpointMul);
            OnWeakpointHit?.Invoke(projectile, targetCenter, damage);
            return damage;
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
            _covers.Clear();
        }

        /// <summary>Drop registered covers without touching projectiles (stage re-layout).</summary>
        public void ClearCovers()
        {
            _covers.Clear();
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

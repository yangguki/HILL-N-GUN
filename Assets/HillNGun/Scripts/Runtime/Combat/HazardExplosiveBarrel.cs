using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Feel;

namespace HillNGun.Combat
{
    public enum BarrelState { Idle, Armed, Exploded }

    /// <summary>
    /// 06 §5 explosive_barrel + 12 §3.2 (제안) state machine: Idle -> Armed (accumulated 400, grenade blast contact or
    /// chain) -> warningSec (0.4) -> Exploded once. Values from TerrainRulesData (proposal defaults). A hazard, never a
    /// cover slot. Chains are queued: an explosion only ARMS barrels in range; each then runs its own warning.
    /// </summary>
    public class HazardExplosiveBarrel : INeutralTarget
    {
        // Contract values (06 existing). Kept as constants for older callers / tests; runtime reads TerrainRules.
        public const int TriggerHp = 400;
        public const int CenterDamage = 700;
        public const int EdgeDamage = 350;
        public const float Radius = 2f;

        private static int _nextId;

        public string InstanceId { get; private set; }
        public Vector2 Position { get; private set; }
        public float HitRadius => TerrainRules.Data.barrelHitRadius;
        public Team Team => Team.Enemy; // ignored via INeutralTarget
        public bool CanBeHit => State != BarrelState.Exploded;
        public BarrelState State { get; private set; }
        public bool Exploded => State == BarrelState.Exploded;
        public bool Armed => State == BarrelState.Armed;
        public int Accumulated { get; private set; }
        /// <summary>Kept for the old view blink: half-way to the trigger or armed.</summary>
        public bool Cracked => !Exploded && (Armed || Accumulated >= TriggerDamage / 2);
        public float WarningRemaining { get; private set; }
        public float WarningTotal => TerrainRules.Data.barrelWarningSec;
        /// <summary>Team of the last damage context that armed it (chains inherit the source barrel's team).</summary>
        public Team? TriggerTeam { get; private set; }
        public string TriggerSource { get; private set; }
        public int ExplodeCount { get; private set; }

        public static int TriggerDamage => TerrainRules.Data.barrelTriggerDamage;

        public event System.Action<HazardExplosiveBarrel> OnArmed;
        public event System.Action<HazardExplosiveBarrel> OnExploded;

        private CombatSystem _combat;

        public void Initialize(Vector2 pos, CombatSystem combat, string instanceId = null)
        {
            Position = pos;
            _combat = combat;
            Accumulated = 0;
            State = BarrelState.Idle;
            WarningRemaining = 0f;
            TriggerTeam = null;
            ExplodeCount = 0;
            InstanceId = instanceId ?? ("barrel_" + (++_nextId));
            combat?.RegisterCombatTarget(this);
        }

        public void ReceiveHit(int damage, Vector2 direction) => ReceiveHit(damage, direction, null);

        /// <summary>Bullets / non-grenade damage accumulate; at the trigger total the barrel arms (it keeps absorbing).</summary>
        public void ReceiveHit(int damage, Vector2 direction, Team? team)
        {
            if (Exploded) return;
            Accumulated += Mathf.Max(0, damage);
            if (State == BarrelState.Idle && team.HasValue) TriggerTeam = team; // attribution = the hit that arms it
            if (Accumulated >= TriggerDamage) Arm(team, "damage");
        }

        /// <summary>Grenade blast contact: arms (does NOT skip the warning — 12 §3.2 제안).</summary>
        public void TriggerByGrenade(Team? team = null)
        {
            if (!TerrainRules.Data.barrelGrenadeTriggers) return;
            Arm(team, "grenade");
        }

        /// <summary>Idle -> Armed with a warning. Already armed / exploded: ignored (no duplicate reservation).</summary>
        public bool Arm(Team? team, string source)
        {
            if (State != BarrelState.Idle) return false;
            State = BarrelState.Armed;
            WarningRemaining = Mathf.Max(0f, TerrainRules.Data.barrelWarningSec);
            TriggerTeam = team ?? TriggerTeam;
            TriggerSource = source;
            _combat?.QueueArmedHazard(this);
            Debug.Log($"[Terrain] prop_armed id=explosive_barrel instance={InstanceId} source={source} team={TriggerTeam?.ToString() ?? "neutral"} warning={WarningRemaining:F2}s");
            OnArmed?.Invoke(this);
            if (WarningRemaining <= 0f) Explode();
            return true;
        }

        /// <summary>Advance the warning; explodes once when it runs out. Returns true on the explode frame.</summary>
        public bool Tick(float dt)
        {
            if (State != BarrelState.Armed) return false;
            WarningRemaining -= dt;
            if (WarningRemaining > 0f) return false;
            Explode();
            return true;
        }

        private void Explode()
        {
            if (Exploded) return;
            State = BarrelState.Exploded;
            ExplodeCount++;
            WarningRemaining = 0f;
            var r = TerrainRules.Data;
            _combat?.UnregisterCombatTarget(this);
            Debug.Log($"[Terrain] prop_exploded id=explosive_barrel instance={InstanceId} team={TriggerTeam?.ToString() ?? "neutral"} source={TriggerSource}");
            _combat?.ExplodeHazard(Position, r.barrelRadius, r.barrelCenterDamage, r.barrelEdgeDamage, r.barrelCoverDamageMul, this);
            EffectsManager.Instance?.SpawnExplosion(Position, r.barrelRadius);
            EffectsManager.Instance?.SpawnCoverDebris(Position, 1.2f, true);
            OnExploded?.Invoke(this);
        }

        /// <summary>Center damage inside innerRadius, linear to edge damage at radius (12 §3.2 제안 inner 0.5).</summary>
        public static int DamageAt(float dist, float radius, float inner, int center, int edge)
        {
            if (dist > radius) return 0;
            if (dist <= inner) return center;
            float t = radius > inner ? (dist - inner) / (radius - inner) : 1f;
            return Mathf.RoundToInt(Mathf.Lerp(center, edge, t));
        }
    }
}

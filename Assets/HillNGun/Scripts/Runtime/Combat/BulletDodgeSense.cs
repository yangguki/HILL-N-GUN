using System;
using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;

namespace HillNGun.Combat
{
    /// <summary>Data-driven reaction to incoming bullets (EnemyData / BotBehaviorData fill it).</summary>
    [Serializable]
    public struct DodgeTuning
    {
        [Tooltip("Chance to react to one incoming bullet (rolled once per bullet)")]
        [Range(0f, 1f)] public float chance;
        [Tooltip("Reaction delay (s), random between min and max")]
        public float delayMin, delayMax;
        [Tooltip("Only bullets that would arrive within this many seconds count")]
        public float lookahead;
        [Tooltip("Extra miss distance (tiles) still treated as a hit")]
        public float margin;
        [Tooltip("Minimum time between two dodges (s)")]
        public float cooldown;

        public bool Enabled => chance > 0f;
    }

    /// <summary>
    /// Pure logic: watches the projectile list for bullets that will hit this unit soon and schedules ONE reaction after
    /// a data-driven delay (so units don't dodge everything instantly). Each bullet is judged once. The caller decides
    /// what the reaction is (PvE enemy side-step, bot roll / side-step). Injected RNG for tests.
    /// </summary>
    public class BulletDodgeSense
    {
        private readonly HashSet<Projectile> _judged = new HashSet<Projectile>();
        private readonly List<Projectile> _stale = new List<Projectile>();
        private readonly Func<float> _rng;
        private float _pendingAt = -1f;
        private Vector2 _pendingDir;
        private float _cooldownUntil = -999f;

        public DodgeTuning Tuning;
        public int Detected { get; private set; }
        public int Reactions { get; private set; }
        public bool HasPending => _pendingAt >= 0f;

        public BulletDodgeSense(DodgeTuning tuning, Func<float> rng = null)
        {
            Tuning = tuning;
            _rng = rng ?? (() => UnityEngine.Random.value);
        }

        /// <summary>Will a bullet at projPos moving along projDir hit a circle at pos within lookahead? side = dodge direction.</summary>
        public static bool IsThreat(Vector2 pos, float radius, Vector2 projPos, Vector2 projDir, float projSpeed, float projRadius,
                                    float lookahead, float margin, out float timeToHit, out Vector2 dodgeDir)
        {
            timeToHit = 0f; dodgeDir = Vector2.zero;
            if (projSpeed <= 0f || projDir.sqrMagnitude < 1e-6f) return false;
            Vector2 d = projDir.normalized;
            Vector2 rel = pos - projPos;
            float along = Vector2.Dot(rel, d);
            if (along <= 0f) return false;                      // already past us
            timeToHit = along / projSpeed;
            if (timeToHit > lookahead) return false;
            Vector2 perp = rel - d * along;
            if (perp.magnitude > radius + projRadius + margin) return false;
            // step sideways (x is the free axis on the rows): away from the bullet line, random-ish when dead centre
            float sx = perp.x;
            if (Mathf.Abs(sx) < 0.05f) sx = d.y >= 0f ? (d.x >= 0f ? -1f : 1f) : (d.x >= 0f ? -1f : 1f);
            dodgeDir = new Vector2(Mathf.Sign(sx), 0f);
            return true;
        }

        /// <summary>
        /// Per frame. Returns true (once) when a scheduled reaction is due; dodgeDir is the side to step to.
        /// hostileTeam = team whose bullets are dangerous to this unit.
        /// </summary>
        public bool Tick(float now, Vector2 pos, float radius, IReadOnlyList<Projectile> projectiles, Team hostileTeam, out Vector2 dodgeDir)
        {
            dodgeDir = Vector2.zero;
            if (!Tuning.Enabled) return false;
            if (_pendingAt >= 0f && now >= _pendingAt)
            {
                _pendingAt = -1f;
                _cooldownUntil = now + Tuning.cooldown;
                dodgeDir = _pendingDir;
                Reactions++;
                return true;
            }
            if (projectiles == null) return false;
            for (int i = 0; i < projectiles.Count; i++)
            {
                var p = projectiles[i];
                if (p == null || !p.IsActive || p.OwnerTeam != hostileTeam || _judged.Contains(p)) continue;
                if (!IsThreat(pos, radius, p.Position, p.Direction, p.Speed, p.Radius, Tuning.lookahead, Tuning.margin, out _, out var dir)) continue;
                _judged.Add(p);
                Detected++;
                if (_pendingAt >= 0f || now < _cooldownUntil) continue;
                if (_rng() >= Tuning.chance) continue;
                float delay = Mathf.Lerp(Tuning.delayMin, Tuning.delayMax, _rng());
                _pendingAt = now + Mathf.Max(0f, delay);
                _pendingDir = dir;
            }
            // forget bullets that are gone (pooled objects get reused)
            if (_judged.Count > 0)
            {
                _stale.Clear();
                foreach (var p in _judged) if (!p.IsActive) _stale.Add(p);
                for (int i = 0; i < _stale.Count; i++) _judged.Remove(_stale[i]);
            }
            return false;
        }

        public void Reset() { _judged.Clear(); _pendingAt = -1f; _cooldownUntil = -999f; }
    }
}

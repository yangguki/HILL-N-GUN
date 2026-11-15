using System;
using System.Collections.Generic;
using UnityEngine;
using HillNGun.Battlefield;
using HillNGun.Combat;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.PvE;

namespace HillNGun.Proto.TopDown
{
    /// <summary>
    /// Drives EnemyInstance + EnemySpawnDirector + FireTokenScheduler. Wave counts stay inside the existing 3–4 band.
    /// Gaps are the stage 1-1 delays (1s / 0.5s before a wave, 5 / 5 / 4.5 / 4s after clear) and the director's door-busy
    /// time, so a clear lands around 70–110s without touching weapon, HP, or speed numbers.
    /// </summary>
    public sealed class ProtoEnemyDriver
    {
        static readonly int[] Counts = { 3, 4, 3, 4, 3, 4, 3 };
        static readonly float[] NextDelay = { 5f, 5f, 4.5f, 4f, 5f, 5f, 4.5f };

        readonly List<EnemyInstance> _enemies = new List<EnemyInstance>(8);
        readonly EnemySpawnDirector _director;
        readonly FireTokenScheduler _fire;
        readonly FreeMoveWorld _world;
        readonly CombatSystem _combat;
        readonly MatchRulesData _rules;
        readonly EnemyData _data;
        readonly Context _ctx;
        readonly System.Random _postRng;
        readonly List<Vector2> _playerBuf = new List<Vector2>(1);
        int _wave = -1;
        float _wait = 1f;
        bool _waitingToSpawn = true;
        bool _waitingClear;

        public IReadOnlyList<EnemyInstance> Enemies => _enemies;
        public int WaveNumber => Mathf.Max(0, _wave + 1);
        public int WaveCount => Counts.Length;
        public bool AllWavesCleared { get; private set; }

        public ProtoEnemyDriver(FreeMoveWorld world, StreetLayoutData layout, CombatSystem combat, MatchRulesData rules, EnemyData data, System.Func<Vector2> playerPos)
        {
            _world = world;
            _combat = combat;
            _rules = rules;
            _data = data;
            _postRng = new System.Random(20261115);
            _fire = new FireTokenScheduler(rules != null ? rules.pveMaxConcurrentEnemyFire : 3);
            _director = new EnemySpawnDirector(layout, 20261115);
            if (rules != null)
            {
                _director.SafeRadius = rules.pveSpawnSafeRadius;
                _director.RetryInterval = rules.pveSpawnRetryInterval;
                _director.RelaxAfter = rules.pveSpawnRelaxAfter;
            }
            _ctx = new Context(this, playerPos);
        }

        public void Tick(float dt, float now, Vector2 playerPos)
        {
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e.IsDefeated)
                {
                    e.UpdateDefeated(dt, _ctx);
                    if (e.IsGone) _enemies.RemoveAt(i);
                    continue;
                }
                e.UpdateFree(dt, _ctx);
                e.TickDodge(dt, now, _combat.ProjectilePool.ActiveProjectiles, -50f, 50f, e.FreeMover);
            }

            if (AllWavesCleared) return;

            if (_waitingToSpawn)
            {
                _wait -= dt;
                if (_wait > 0f) return;
                _waitingToSpawn = false;
                BeginWave(now);
            }

            _playerBuf.Clear();
            _playerBuf.Add(playerPos);
            _director.Tick(now, _playerBuf, Spawn);

            if (_waitingClear && _director.PendingCount == 0 && !AnyLiving())
            {
                _waitingClear = false;
                if (_wave >= Counts.Length - 1) { AllWavesCleared = true; return; }
                _wait = NextDelay[Mathf.Clamp(_wave, 0, NextDelay.Length - 1)];
                _waitingToSpawn = true;
            }
        }

        void BeginWave(float now)
        {
            _wave++;
            _director.BeginWave(_wave);
            float stagger = _rules != null ? _rules.pveSpawnStagger : 0.3f;
            _director.Enqueue(new EnemySpawnEntry
            {
                enemyData = _data,
                count = Counts[Mathf.Clamp(_wave, 0, Counts.Length - 1)],
                spawnPointId = "door_top",
                spawnType = SpawnPointType.StorefrontDoor
            }, now, stagger);
            _waitingClear = true;
            Debug.Log($"[ProtoTopDown] wave {_wave + 1}/{Counts.Length} count={Counts[_wave]}");
        }

        bool AnyLiving()
        {
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i].IsAlive) return true;
            return false;
        }

        void Spawn(EnemySpawnDirector.Pending pending, int pointIndex)
        {
            var sp = _director.Layout.spawnPoints[pointIndex];
            var enemy = new EnemyInstance();
            float hpMul = _rules != null ? _rules.GetPvEEnemyHpMul(1) : 1f;
            float delay = _rules != null ? _rules.pveEntranceAttackDelay : 0.8f;
            int seed = 92821 + pending.serial * 7919;
            enemy.InitializeFree(pending.data, sp, pointIndex, hpMul, delay, new System.Random(seed));
            enemy.Serial = pending.serial;
            var feel = ServiceLocator.Get<MovementFeelData>() ?? MovementFeelData.Fallback;
            enemy.BodyRadius = feel.freeMoveEnemyRadius * Mathf.Max(0.5f, pending.data.scale);
            var en = enemy;
            enemy.FreeMover = d => _world.Move(en.Position, d, en.BodyRadius);
            enemy.OnEntranceComplete += e =>
            {
                if (!e.Data.stationary) e.SetPosition(_world.Depenetrate(e.Position, e.BodyRadius));
            };
            enemy.OnDefeated += e =>
            {
                _fire.Release(e);
                _combat.UnregisterCombatTarget(e);
            };
            enemy.OnAttack += (e, dir, dmg) => Fire(e, dir, dmg);
            enemy.OnExplosiveThrown += (e, dir, dmg) => Throw(e, dir, dmg);
            _combat.RegisterCombatTarget(enemy);
            _enemies.Add(enemy);
        }

        void Fire(EnemyInstance enemy, Vector2 direction, int damage)
        {
            var d = enemy.Data;
            if (d == null) return;
            int count = Mathf.Max(1, d.projectilesPerShot);
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? Mathf.Lerp(-d.spreadDeg * 0.5f, d.spreadDeg * 0.5f, i / (float)(count - 1)) : 0f;
                Vector2 dir = CoordinateHelper.RotateDirection(direction, angle);
                _combat.SpawnEnemyProjectile(enemy.Position + dir * 0.6f, dir, d.bulletSpeed, d.bulletRadius, damage, d.attackRange, Team.Enemy);
            }
        }

        void Throw(EnemyInstance enemy, Vector2 direction, int damage)
        {
            var d = enemy.Data;
            if (d == null) return;
            float speed = d.attackRange / Mathf.Max(0.1f, d.explosiveFlightTime);
            _combat.SpawnEnemyProjectile(enemy.Position + direction * 0.6f, direction, speed, d.explosiveRadius * 0.25f, damage, d.attackRange, Team.Enemy, 1f, false);
        }

        sealed class Context : IEnemyFreeContext
        {
            readonly ProtoEnemyDriver _d;
            readonly System.Func<Vector2> _player;

            public Context(ProtoEnemyDriver d, System.Func<Vector2> player) { _d = d; _player = player; }

            public Vector2 NearestPlayerPos(Vector2 from) => _player();

            public Vector2 MoveEnemy(EnemyInstance e, Vector2 delta) => _d._world.Move(e.Position, delta, e.BodyRadius);

            public bool TryAcquireFire(EnemyInstance e) => _d._fire.TryAcquire(e, Time.time);

            public void ReleaseFire(EnemyInstance e) => _d._fire.Release(e);

            public Vector2 PickPost(EnemyInstance e)
            {
                var layout = _d._director.Layout;
                if (layout == null || e?.Data == null) return e != null ? e.Position : Vector2.zero;
                Rect z = layout.enemyPostZone;
                float desired = Mathf.Clamp(e.Data.keepDistance + 2f, 3f, Mathf.Max(3f, e.Data.attackRange * 0.8f));
                Vector2 best = e.Position;
                float bestScore = float.MinValue;
                var rng = _d._postRng;
                for (int k = 0; k < 14; k++)
                {
                    float y = z.yMin + (float)rng.NextDouble() * z.height;
                    if (!FreeMoveCollision.SpanAtY(_d._world.Polygon, y, out float x0, out float x1)) continue;
                    x0 = Mathf.Max(x0 + layout.enemyPostEdgeMargin, z.xMin);
                    x1 = Mathf.Min(x1 - layout.enemyPostEdgeMargin, z.xMax);
                    if (x1 <= x0) continue;
                    var c = new Vector2(Mathf.Lerp(x0, x1, (float)rng.NextDouble()), y);
                    if (!_d._world.IsFree(c, e.BodyRadius + 0.2f)) continue;
                    float score = -Mathf.Abs((_player() - c).magnitude - desired);
                    float spread = 3f;
                    for (int i = 0; i < _d._enemies.Count; i++)
                    {
                        var o = _d._enemies[i];
                        if (o == e || !o.IsAlive) continue;
                        spread = Mathf.Min(spread, Mathf.Min((o.Post - c).magnitude, (o.Position - c).magnitude));
                    }
                    score += 0.6f * spread - 0.08f * (c - e.Position).magnitude;
                    if (score > bestScore) { bestScore = score; best = c; }
                }
                return best;
            }
        }
    }
}

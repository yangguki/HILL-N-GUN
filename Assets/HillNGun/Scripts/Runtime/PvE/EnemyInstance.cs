using UnityEngine;
using System;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.PvE
{
    /// <summary>What a FreeMove enemy needs from the match (collision, posts, fire tokens). Tests use a fake.</summary>
    public interface IEnemyFreeContext
    {
        Vector2 NearestPlayerPos(Vector2 from);
        /// <summary>Swept move with covers / walk area; returns the new position.</summary>
        Vector2 MoveEnemy(EnemyInstance e, Vector2 delta);
        Vector2 PickPost(EnemyInstance e);
        bool TryAcquireFire(EnemyInstance e);
        void ReleaseFire(EnemyInstance e);
    }

    public enum EntrancePhase { None, Telegraph, Walking, Done }

    public class EnemyInstance : HillNGun.Combat.ICombatTarget
    {
        public EnemyData Data { get; private set; }
        public Vector2 Position { get; private set; }
        public int CurrentRowIndex { get; private set; }
        public int CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0 && !IsFleeing && !IsDefeated;
        public bool IsFleeing { get; private set; }
        public bool IsDefeated { get; private set; }
        public float HitRadius => (Data?.hitRadius ?? 0.5f) * ScalePresets.CharacterMul; // scale preset
        /// <summary>True while walking in from off-screen to the first row (cannot attack yet).</summary>
        public bool IsEntering => FreeMove ? InEntrance : CurrentRowIndex < 0;

        // ---- FreeMove (PvE) ----
        public bool FreeMove { get; private set; }
        public StageSpawnPoint SpawnPoint { get; private set; }
        public int SpawnPointIndex { get; private set; } = -1;
        public EntrancePhase Entrance { get; private set; }
        /// <summary>Telegraph ('!' at the door/window) or entrance path running: no hits, no attacks.</summary>
        public bool InEntrance => FreeMove && (Entrance == EntrancePhase.Telegraph || Entrance == EntrancePhase.Walking);
        /// <summary>Hidden while the door/window telegraph plays.</summary>
        public bool IsBodyVisible => !(FreeMove && Entrance == EntrancePhase.Telegraph);
        public float EntranceProgress { get; private set; }
        public float TelegraphProgress { get; private set; }
        public float AttackDelayRemaining => Mathf.Max(0f, _attackDelay);
        public bool IsWindingUp => _isAttacking;
        public Vector2 AttackDirection => _attackDirection;
        public float WarningRemaining => _isAttacking ? Mathf.Max(0f, _warningTimer) : 0f;
        public bool HoldsFireToken { get; private set; }
        public Vector2 Post => _post;
        /// <summary>Defeated and finished its exit / down animation (FreeMove): remove.</summary>
        public bool IsGone { get; private set; }
        public float DefeatTime { get; private set; }
        public float BodyRadius { get; set; } = 0.3f;
        /// <summary>FreeMove swept mover (delta → new position), assigned once at spawn (no per-frame lambda).</summary>
        public Func<Vector2, Vector2> FreeMover { get; set; }
        public int Serial { get; set; }

        private float _attackDelay;
        private float _entranceTimer;
        private Vector2 _post;
        private float _repositionTimer;
        private float _stuckTimer;
        private float _retreatCooldown;
        private System.Random _rng;
        private Vector2 _dodgeTarget2;

        // ICombatTarget
        public Team Team => Team.Enemy;
        public bool CanBeHit => IsAlive && (FreeMove ? !InEntrance : Position.y <= _hittableMaxY);
        public void ReceiveHit(int damage, Vector2 direction) => TakeDamage(damage, direction);

        private float _hittableMaxY = float.MaxValue;
        public void SetHittableMaxY(float maxY) { _hittableMaxY = maxY; }

        private float _attackTimer;
        private float _warningTimer;
        private bool _isAttacking;
        private Vector2 _attackDirection;
        private float _rowChangeTimer;
        private float _moveTimer;
        private int _targetRow;

        public event Action<EnemyInstance> OnDefeated;
        public event Action<EnemyInstance, int, Vector2> OnDamageTaken;
        public event Action<EnemyInstance, Vector2, int> OnAttack;
        public event Action<EnemyInstance, Vector2, int> OnExplosiveThrown;
        public event Action<EnemyInstance> OnWarningStart;

        /// <summary>Max HP after party-size scaling (MatchRulesData.pveEnemyHpMultipliers).</summary>
        public int MaxHp { get; private set; }

        public void Initialize(EnemyData data, Vector2 position, int rowIndex, float hpMultiplier = 1f)
        {
            Data = data;
            Position = position;
            // Spawned off-screen: start in "entering" state (-1) and walk to the preferred row.
            CurrentRowIndex = -1;
            MaxHp = Mathf.Max(1, Mathf.RoundToInt(data.maxHp * Mathf.Max(0.01f, hpMultiplier)));
            CurrentHp = MaxHp;
            _attackTimer = UnityEngine.Random.Range(0.5f, data.attackInterval);
            _rowChangeTimer = data.rowChangeInterval;
            _targetRow = rowIndex;
            IsFleeing = false;
            IsDefeated = false;
            _dodge = null;
            _dodging = false;
            FreeMove = false;
            Entrance = EntrancePhase.None;
            IsGone = false;
        }

        /// <summary>
        /// FreeMove spawn at a painted entrance: telegraph (hidden, '!'), then the entrance path (visible, immune), then
        /// attackDelay seconds without attacking. rng = stage-seeded (deterministic timers / posts).
        /// </summary>
        public void InitializeFree(EnemyData data, StageSpawnPoint point, int pointIndex, float hpMultiplier, float attackDelay, System.Random rng)
        {
            Initialize(data, point != null ? point.entranceStart : Vector2.zero, 0, hpMultiplier);
            FreeMove = true;
            CurrentRowIndex = 0;
            _targetRow = 0;
            SpawnPoint = point;
            SpawnPointIndex = pointIndex;
            _rng = rng ?? new System.Random(1);
            _attackTimer = Mathf.Lerp(0.5f, Mathf.Max(0.5f, data.attackInterval), (float)_rng.NextDouble());
            _attackDelay = attackDelay;
            _entranceTimer = 0f;
            EntranceProgress = 0f;
            TelegraphProgress = 0f;
            Entrance = point != null && point.telegraphTime > 0f ? EntrancePhase.Telegraph : EntrancePhase.Walking;
            _post = point != null ? point.position : Position;
            _repositionTimer = 0.4f + (float)_rng.NextDouble() * 0.6f;
            HoldsFireToken = false;
        }

        private float Rand01() => _rng != null ? (float)_rng.NextDouble() : UnityEngine.Random.value;

        /// <summary>Fired once when the entrance path ends (manager depenetrates / logs).</summary>
        public event Action<EnemyInstance> OnEntranceComplete;

        /// <summary>FreeMove tick: entrance → post movement (swept) → token-gated 2D attacks.</summary>
        public void UpdateFree(float dt, IEnemyFreeContext ctx)
        {
            if (!IsAlive || !FreeMove) return;
            if (InEntrance) { TickEntrance(dt); return; }
            if (_attackDelay > 0f) _attackDelay -= dt;
            if (_retreatCooldown > 0f) _retreatCooldown -= dt;
            Vector2 player = ctx.NearestPlayerPos(Position);

            if (_isAttacking)
            {
                _warningTimer -= dt;
                if (_warningTimer <= 0f)
                {
                    ExecuteAttack();
                    _isAttacking = false;
                    if (HoldsFireToken) { ctx.ReleaseFire(this); HoldsFireToken = false; }
                }
                return;
            }

            if (!Data.stationary && !_dodging) TickPost(dt, ctx, player);

            _attackTimer -= dt;
            if (_attackTimer <= 0f && _attackDelay <= 0f)
            {
                float dist = (player - Position).magnitude;
                if (dist <= Data.attackRange * 0.95f && ctx.TryAcquireFire(this))
                {
                    HoldsFireToken = true;
                    StartAttack(player);
                }
            }
        }

        private void TickEntrance(float dt)
        {
            _entranceTimer += dt;
            if (Entrance == EntrancePhase.Telegraph)
            {
                float tt = SpawnPoint != null ? SpawnPoint.telegraphTime : 0f;
                TelegraphProgress = tt > 0f ? Mathf.Clamp01(_entranceTimer / tt) : 1f;
                if (_entranceTimer < tt) return;
                Entrance = EntrancePhase.Walking;
                _entranceTimer = 0f;
            }
            float dur = SpawnPoint != null ? Mathf.Max(0.05f, SpawnPoint.entranceTime) : 0.05f;
            float t = Mathf.Clamp01(_entranceTimer / dur);
            EntranceProgress = t;
            if (SpawnPoint != null) Position = Vector2.Lerp(SpawnPoint.entranceStart, SpawnPoint.position, t);
            if (t >= 1f)
            {
                Entrance = EntrancePhase.Done;
                _post = Position;
                OnEntranceComplete?.Invoke(this);
                _post = Position; // the handler may depenetrate
            }
        }

        private void TickPost(float dt, IEnemyFreeContext ctx, Vector2 player)
        {
            _repositionTimer -= dt;
            float near = (player - Position).magnitude;
            if (near < Data.keepDistance * 0.6f && _retreatCooldown <= 0f)
            {
                _post = ctx.PickPost(this); // too close: back off
                _retreatCooldown = 1.5f;
            }
            Vector2 to = _post - Position;
            float dist = to.magnitude;
            if (dist < 0.15f)
            {
                if (_repositionTimer <= 0f)
                {
                    _post = ctx.PickPost(this);
                    _repositionTimer = Mathf.Max(0.5f, Data.repositionInterval) * Mathf.Lerp(0.7f, 1.3f, Rand01());
                }
                _stuckTimer = 0f;
                return;
            }
            float stepLen = Mathf.Min(dist, Data.moveSpeed * dt);
            Vector2 np = ctx.MoveEnemy(this, to / dist * stepLen);
            float moved = (np - Position).magnitude;
            Position = np;
            if (moved < 0.25f * stepLen) { _stuckTimer += dt; if (_stuckTimer > 0.5f) { _post = ctx.PickPost(this); _stuckTimer = 0f; } }
            else _stuckTimer = 0f;
        }

        /// <summary>FreeMove defeat: walk back out of the entrance (fleeing) or go down in place (sniper), then IsGone.</summary>
        public void UpdateDefeated(float dt, IEnemyFreeContext ctx)
        {
            if (!FreeMove || !IsDefeated || IsGone) return;
            DefeatTime += dt;
            if (IsFleeing && SpawnPoint != null && !Data.stationary)
            {
                Vector2 to = SpawnPoint.entranceStart - Position;
                float d = to.magnitude;
                float step = Mathf.Max(1f, Data.fleeSpeed) * dt;
                Position = d <= step ? SpawnPoint.entranceStart : Position + to / d * step; // exit path ignores colliders (defeat visual)
                if (d <= step || DefeatTime > 2.5f) IsGone = true;
            }
            else if (DefeatTime >= 0.6f) IsGone = true;
        }

        // ---- bullet dodge (t48u) ----
        private HillNGun.Combat.BulletDodgeSense _dodge;
        private float _dodgeTargetX;
        private bool _dodging;
        public int DodgeCount => _dodge?.Reactions ?? 0;
        public bool IsDodging => _dodging;

        /// <summary>
        /// Watches player bullets and side-steps (x) after a data-driven delay. Not while winding up an attack (the aim
        /// is locked) or entering. minX / maxX = walkable field span.
        /// </summary>
        public void TickDodge(float dt, float now, System.Collections.Generic.IReadOnlyList<HillNGun.Combat.Projectile> projectiles, float minX, float maxX)
        {
            TickDodge(dt, now, projectiles, minX, maxX, null);
        }

        /// <param name="mover">FreeMove: swept move (delta → new position); dodges step perpendicular to the bullet in 2D.</param>
        public void TickDodge(float dt, float now, System.Collections.Generic.IReadOnlyList<HillNGun.Combat.Projectile> projectiles, float minX, float maxX, Func<Vector2, Vector2> mover)
        {
            if (Data == null || !IsAlive || IsEntering) return;
            if (FreeMove && Data.stationary) return;
            if (_dodge == null)
            {
                if (!Data.bulletDodge.Enabled) return;
                _dodge = new HillNGun.Combat.BulletDodgeSense(Data.bulletDodge);
            }
            if (_dodging && FreeMove && mover != null)
            {
                Vector2 to = _dodgeTarget2 - Position;
                float step = Data.dodgeStepSpeed * dt;
                Vector2 np = mover(to.magnitude <= step ? to : to.normalized * step);
                bool stuck = (np - Position).magnitude < step * 0.2f;
                Position = np;
                if ((_dodgeTarget2 - Position).magnitude < 0.02f || stuck) _dodging = false;
                return;
            }
            if (_dodging)
            {
                float x = Mathf.MoveTowards(Position.x, _dodgeTargetX, Data.dodgeStepSpeed * dt);
                Position = new Vector2(x, Position.y);
                if (Mathf.Approximately(x, _dodgeTargetX)) _dodging = false;
                return;
            }
            if (_isAttacking) return;
            if (_dodge.Tick(now, Position, HitRadius, projectiles, Team.Player, out var dir))
            {
                if (FreeMove && mover != null)
                {
                    Vector2 d2 = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.right;
                    _dodgeTarget2 = Position + d2 * Data.dodgeStepDistance;
                    _dodging = true;
                    return;
                }
                float tx = Position.x + dir.x * Data.dodgeStepDistance;
                if (tx < minX || tx > maxX) tx = Position.x - dir.x * Data.dodgeStepDistance; // wall: step the other way
                _dodgeTargetX = Mathf.Clamp(tx, minX, maxX);
                _dodging = true;
            }
        }

        public void SetPosition(Vector2 pos)
        {
            Position = pos;
        }

        public void Update(float deltaTime, Vector2 playerPosition)
        {
            if (!IsAlive) return;
            if (IsEntering) return;

            if (_isAttacking)
            {
                _warningTimer -= deltaTime;
                if (_warningTimer <= 0f)
                {
                    ExecuteAttack();
                    _isAttacking = false;
                }
                return;
            }

            _attackTimer -= deltaTime;
            if (_attackTimer <= 0f)
            {
                StartAttack(playerPosition);
            }

            if (Data.movesBetweenRows)
            {
                _rowChangeTimer -= deltaTime;
                if (_rowChangeTimer <= 0f)
                {
                    ChangeRow();
                }
            }
        }

        private void StartAttack(Vector2 playerPosition)
        {
            _isAttacking = true;
            _warningTimer = Data.warningTime;
            _attackDirection = (playerPosition - Position).normalized;
            OnWarningStart?.Invoke(this);
        }

        private void ExecuteAttack()
        {
            if (Data.throwsExplosive)
            {
                OnExplosiveThrown?.Invoke(this, _attackDirection, Data.explosiveDamage);
            }
            else
            {
                OnAttack?.Invoke(this, _attackDirection, Data.attackDamage);
            }

            _attackTimer = Data.attackInterval;
        }

        private void ChangeRow()
        {
            if (IsEntering) return;
            int newRow = (CurrentRowIndex + 1) % 3;
            _targetRow = newRow;
            _rowChangeTimer = Data.rowChangeInterval;
        }

        public void MoveTowardsTargetRow(float deltaTime, float[] rowYPositions)
        {
            if (_targetRow == CurrentRowIndex || rowYPositions == null || rowYPositions.Length == 0) return;

            float targetY = rowYPositions[Mathf.Clamp(_targetRow, 0, rowYPositions.Length - 1)];
            float currentY = Position.y;

            float step = Data.moveSpeed * deltaTime * (IsEntering ? 2f : 1f);
            float newY = Mathf.MoveTowards(currentY, targetY, step);
            Position = new Vector2(Position.x, newY);

            if (Mathf.Approximately(newY, targetY))
            {
                CurrentRowIndex = _targetRow;
            }
        }

        public void TakeDamage(int damage, Vector2 direction)
        {
            if (!IsAlive) return;

            CurrentHp -= damage;
            OnDamageTaken?.Invoke(this, damage, direction);

            if (CurrentHp <= 0)
            {
                Defeat();
            }
        }

        private void Defeat()
        {
            CurrentHp = 0;
            IsDefeated = true;

            if (Data.fleesOnDefeat)
            {
                IsFleeing = true;
            }

            OnDefeated?.Invoke(this);
        }

        public void UpdateFlee(float deltaTime)
        {
            if (!IsFleeing) return;

            float fleeY = Position.y + Data.fleeSpeed * deltaTime;
            Position = new Vector2(Position.x, fleeY);
        }

        public bool IsOffScreen(float maxY)
        {
            return FreeMove ? IsGone : Position.y > maxY;
        }

        public void Reset()
        {
            CurrentHp = MaxHp > 0 ? MaxHp : Data.maxHp;
            IsFleeing = false;
            IsDefeated = false;
            _attackTimer = UnityEngine.Random.Range(0.5f, Data.attackInterval);
        }
    }
}

using UnityEngine;
using System;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.PvE
{
    public class EnemyInstance
    {
        public EnemyData Data { get; private set; }
        public Vector2 Position { get; private set; }
        public int CurrentRowIndex { get; private set; }
        public int CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0 && !IsFleeing && !IsDefeated;
        public bool IsFleeing { get; private set; }
        public bool IsDefeated { get; private set; }
        public float HitRadius => Data?.hitRadius ?? 0.5f;

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

        public void Initialize(EnemyData data, Vector2 position, int rowIndex)
        {
            Data = data;
            Position = position;
            CurrentRowIndex = rowIndex;
            CurrentHp = data.maxHp;
            _attackTimer = UnityEngine.Random.Range(0.5f, data.attackInterval);
            _rowChangeTimer = data.rowChangeInterval;
            _targetRow = rowIndex;
            IsFleeing = false;
            IsDefeated = false;
        }

        public void SetPosition(Vector2 pos)
        {
            Position = pos;
        }

        public void Update(float deltaTime, Vector2 playerPosition)
        {
            if (!IsAlive) return;

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
            int newRow = (CurrentRowIndex + 1) % 3;
            _targetRow = newRow;
            _rowChangeTimer = Data.rowChangeInterval;
        }

        public void MoveTowardsTargetRow(float deltaTime, float[] rowYPositions)
        {
            if (_targetRow == CurrentRowIndex || rowYPositions == null || rowYPositions.Length == 0) return;

            float targetY = rowYPositions[Mathf.Clamp(_targetRow, 0, rowYPositions.Length - 1)];
            float currentY = Position.y;

            float step = Data.moveSpeed * deltaTime;
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
            return Position.y > maxY;
        }

        public void Reset()
        {
            CurrentHp = Data.maxHp;
            IsFleeing = false;
            IsDefeated = false;
            _attackTimer = UnityEngine.Random.Range(0.5f, Data.attackInterval);
        }
    }
}

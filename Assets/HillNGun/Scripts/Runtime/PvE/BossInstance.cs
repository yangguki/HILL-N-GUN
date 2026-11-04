using UnityEngine;
using System;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.PvE
{
    public class BossInstance : HillNGun.Combat.ICombatTarget
    {
        public BossData Data { get; private set; }
        public Vector2 Position { get; private set; }
        public int CurrentHp { get; private set; }
        /// <summary>Max HP after party-size scaling (BossData.partyHpMultipliers).</summary>
        public int MaxHp { get; private set; }
        public float HpRatio => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;
        /// <summary>1-based index of the volley within the current fan-shot burst.</summary>
        public int LastBurstIndex { get; private set; }
        private int _burstRemaining;
        private float _burstTimer;
        public bool IsAlive => CurrentHp > 0;
        public bool IsDefeated => CurrentHp <= 0;
        public float HitRadius => Data?.hitRadius ?? 1.5f;
        public Team Team => Team.Enemy;
        public bool CanBeHit => IsAlive && !IsInEntryPhase();
        public void ReceiveHit(int damage, Vector2 direction) => TakeDamage(damage, direction);
        public BossPhase CurrentPhase { get; private set; }
        public BossPhaseData CurrentPhaseData => _currentPhaseData;

        /// <summary>X (tiles) the tail sweep will be centred on; set by the match manager (e.g. nearest player).</summary>
        public float SweepTargetX { get; set; } = 12f;
        /// <summary>Locked hit area (logic tiles) of the pending/last tail sweep.</summary>
        public Rect TailSweepArea { get; private set; }
        public bool IsWarning => _isWarning;
        public WarningType CurrentWarning => _isWarning ? _currentWarningType : WarningType.None;
        public float WarningRemaining => _isWarning ? _warningTimer : 0f;

        private BossPhaseData _currentPhaseData;
        private float _fanShotTimer;
        private float _tailSweepTimer;
        private float _summonTimer;
        private float _moveTimer;
        private int _moveDirection = 1;

        private bool _isEntering = true;
        private float _entryTimer;

        private bool _isWarning;
        private float _warningTimer;
        private WarningType _currentWarningType;

        public enum WarningType { None, FanShot, TailSweep }

        public event Action<BossInstance> OnPhaseChanged;
        public event Action<BossInstance, int, Vector2> OnDamageTaken;
        public event Action<BossInstance> OnDefeated;
        public event Action<BossInstance, Vector2[], int> OnFanShot;
        public event Action<BossInstance, float> OnTailSweep;
        public event Action<BossInstance> OnSummonMooks;
        public event Action<BossInstance, WarningType> OnWarningStart;
        public event Action<BossInstance> OnEntryComplete;

        public void Initialize(BossData data, Vector2 position, int maxHpOverride = 0)
        {
            Data = data;
            Position = data.entryStartPosition;
            SweepTargetX = data.battlePosition.x;
            MaxHp = maxHpOverride > 0 ? maxHpOverride : data.maxHp;
            CurrentHp = MaxHp;
            _burstRemaining = 0;
            CurrentPhase = BossPhase.Phase1;
            _currentPhaseData = GetPhaseData(CurrentPhase);

            _isEntering = true;
            _entryTimer = data.entryDuration;

            ResetTimers();
        }

        private void ResetTimers()
        {
            if (_currentPhaseData == null) return;

            _fanShotTimer = _currentPhaseData.fanShotInterval;
            _tailSweepTimer = _currentPhaseData.tailSweepInterval;
            _summonTimer = _currentPhaseData.summonInterval;
            _moveTimer = _currentPhaseData.moveInterval;
        }

        private BossPhaseData GetPhaseData(BossPhase phase)
        {
            if (Data?.phases == null) return null;

            foreach (var phaseData in Data.phases)
            {
                if (phaseData.phase == phase)
                    return phaseData;
            }
            return Data.phases.Length > 0 ? Data.phases[0] : null;
        }

        public void Update(float deltaTime)
        {
            if (!IsAlive) return;

            if (_isEntering)
            {
                UpdateEntry(deltaTime);
                return;
            }

            if (_burstRemaining > 0 && _currentPhaseData != null)
            {
                _burstTimer -= deltaTime * _currentPhaseData.enrageAttackSpeedMultiplier;
                if (_burstTimer <= 0f)
                {
                    _burstRemaining--;
                    LastBurstIndex++;
                    _burstTimer = _currentPhaseData.fanShotBurstInterval;
                    ExecuteFanShotVolley();
                }
                UpdateMovement(deltaTime);
                return;
            }

            if (_isWarning)
            {
                _warningTimer -= deltaTime;
                if (_warningTimer <= 0f)
                {
                    ExecuteCurrentAttack();
                }
                return;
            }

            UpdateAttacks(deltaTime);
            UpdateMovement(deltaTime);
        }

        private void UpdateEntry(float deltaTime)
        {
            _entryTimer -= deltaTime;
            float t = 1f - (_entryTimer / Data.entryDuration);
            t = Mathf.Clamp01(t);

            Position = Vector2.Lerp(Data.entryStartPosition, Data.battlePosition, t);

            if (_entryTimer <= 0f)
            {
                _isEntering = false;
                Position = Data.battlePosition;
                OnEntryComplete?.Invoke(this);
            }
        }

        private void UpdateAttacks(float deltaTime)
        {
            if (_currentPhaseData == null) return;

            float speedMul = _currentPhaseData.enrageAttackSpeedMultiplier;

            if (_currentPhaseData.usesFanShot)
            {
                _fanShotTimer -= deltaTime * speedMul;
                if (_fanShotTimer <= 0f)
                {
                    StartWarning(WarningType.FanShot);
                }
            }

            if (_currentPhaseData.usesTailSweep)
            {
                _tailSweepTimer -= deltaTime * speedMul;
                if (_tailSweepTimer <= 0f)
                {
                    StartWarning(WarningType.TailSweep);
                }
            }

            if (_currentPhaseData.summonsMooks)
            {
                _summonTimer -= deltaTime;
                if (_summonTimer <= 0f)
                {
                    SummonMooks();
                    _summonTimer = _currentPhaseData.summonInterval;
                }
            }
        }

        private void StartWarning(WarningType type)
        {
            _isWarning = true;
            _currentWarningType = type;

            if (type == WarningType.TailSweep && _currentPhaseData != null)
            {
                float w = _currentPhaseData.tailSweepWidth;
                float yMin = _currentPhaseData.tailSweepYMin;
                float yMax = Mathf.Max(yMin + 0.5f, _currentPhaseData.tailSweepYMax);
                TailSweepArea = new Rect(SweepTargetX - w * 0.5f, yMin, w, yMax - yMin);
            }

            _warningTimer = type switch
            {
                WarningType.FanShot => _currentPhaseData.fanShotWarning,
                WarningType.TailSweep => _currentPhaseData.tailSweepWarning,
                _ => 1f
            };

            OnWarningStart?.Invoke(this, _currentWarningType);
        }

        private void ExecuteCurrentAttack()
        {
            _isWarning = false;

            switch (_currentWarningType)
            {
                case WarningType.FanShot:
                    ExecuteFanShot();
                    _fanShotTimer = _currentPhaseData.fanShotInterval;
                    break;
                case WarningType.TailSweep:
                    ExecuteTailSweep();
                    _tailSweepTimer = _currentPhaseData.tailSweepInterval;
                    break;
            }

            _currentWarningType = WarningType.None;
        }

        private void ExecuteFanShot()
        {
            // Phase 3 탄막: fanShotBurstCount volleys per warning (03 §4 / decision D9)
            LastBurstIndex = 1;
            _burstRemaining = Mathf.Max(1, _currentPhaseData.fanShotBurstCount) - 1;
            _burstTimer = _currentPhaseData.fanShotBurstInterval;
            ExecuteFanShotVolley();
        }

        private void ExecuteFanShotVolley()
        {
            int count = _currentPhaseData.fanShotCount;
            float totalAngle = _currentPhaseData.fanShotAngle;
            float startAngle = -totalAngle / 2f;
            float angleStep = count > 1 ? totalAngle / (count - 1) : 0f;

            Vector2[] directions = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (startAngle + angleStep * i - 90f) * Mathf.Deg2Rad;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            OnFanShot?.Invoke(this, directions, _currentPhaseData.fanShotDamage);
        }

        private void ExecuteTailSweep()
        {
            OnTailSweep?.Invoke(this, _currentPhaseData.tailSweepWidth);
        }

        private void SummonMooks()
        {
            OnSummonMooks?.Invoke(this);
        }

        private void UpdateMovement(float deltaTime)
        {
            if (_currentPhaseData == null) return;

            // Phase battle line (phase 3 descends closer to the player cover line)
            if (_currentPhaseData.battleY >= 0f && !Mathf.Approximately(Position.y, _currentPhaseData.battleY))
            {
                float ny = Mathf.MoveTowards(Position.y, _currentPhaseData.battleY, Mathf.Max(0.5f, Data.moveSpeed) * deltaTime);
                Position = new Vector2(Position.x, ny);
            }

            if (!_currentPhaseData.movesHorizontally) return;

            float speedMul = _currentPhaseData.enrageSpeedMultiplier;
            _moveTimer -= deltaTime * speedMul;

            if (_moveTimer <= 0f)
            {
                _moveDirection *= -1;
                _moveTimer = _currentPhaseData.moveInterval;
            }

            float targetX = Data.battlePosition.x + _moveDirection * _currentPhaseData.moveRange / 2f;
            float step = Data.moveSpeed * speedMul * deltaTime;
            float newX = Mathf.MoveTowards(Position.x, targetX, step);
            Position = new Vector2(newX, Position.y);
        }

        public void TakeDamage(int damage, Vector2 direction)
        {
            if (!IsAlive) return;

            CurrentHp -= damage;
            OnDamageTaken?.Invoke(this, damage, direction);

            CheckPhaseTransition();

            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
                OnDefeated?.Invoke(this);
            }
        }

        private void CheckPhaseTransition()
        {
            if (Data?.phases == null || CurrentHp <= 0) return;

            float hpPercent = (float)CurrentHp / Mathf.Max(1, MaxHp);

            BossPhase newPhase = CurrentPhase;
            foreach (var phaseData in Data.phases)
            {
                if (hpPercent <= phaseData.hpThresholdPercent)
                {
                    if (phaseData.phase > newPhase)
                    {
                        newPhase = phaseData.phase;
                    }
                }
            }

            if (newPhase != CurrentPhase)
            {
                CurrentPhase = newPhase;
                _currentPhaseData = GetPhaseData(CurrentPhase);
                _burstRemaining = 0;
                ResetTimers();
                OnPhaseChanged?.Invoke(this);
            }
        }

        public void SetPosition(Vector2 pos)
        {
            Position = pos;
        }

        public bool IsInEntryPhase()
        {
            return _isEntering;
        }
    }
}

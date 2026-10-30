using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Input;
using HillNGun.Combat;

namespace HillNGun.Characters
{
    public class CharacterStateMachine : MonoBehaviour, IInputReceiver
    {
        [SerializeField] private CharacterData _data;

        private MovementState _movementState = MovementState.Idle;
        private ActionState _actionState = ActionState.None;
        private CharacterOverallState _overallState = CharacterOverallState.Active;

        private Vector2 _position;
        private Vector2 _moveDirection;
        private float _moveSpeed;
        private int _currentRowIndex = 1;
        private Team _team = Team.Player;

        private int _currentHp;
        private int _downedHp;
        private float _downedTimer;
        private float _reviveProgress;

        private float _rollTimer;
        private float _rollCooldownTimer;
        private Vector2 _rollDirection;
        private Vector2 _rollStartPos;
        private int _rollTargetRow = -1;

        private float _skillCooldownTimer;
        private float _skillActiveTimer;
        private float _skillHoldTimer;

        private float _rowChangeTimer;
        private int _targetRowIndex;
        private float _rowChangeStartY;

        private Vector2 _lastAimDirection = Vector2.up;
        private bool _isAiming;
        private float _aimStartTime;
        private bool _rollShotBonusActive;
        private float _rollShotBonusTimer;

        private AmmoGauge _ammoGauge;
        private GrenadeSystem _grenadeSystem;

        private ICharacterView _view;

        public CharacterData Data => _data;
        public Vector2 Position => _position;
        public int CurrentRowIndex => _currentRowIndex;
        public Team Team => _team;
        public MovementState MovementState => _movementState;
        public ActionState ActionState => _actionState;
        public CharacterOverallState OverallState => _overallState;
        public int CurrentHp => _currentHp;
        public int MaxHp => _data?.maxHp ?? 1;
        public float HpRatio => (float)_currentHp / MaxHp;
        public bool IsInvulnerable => _movementState == MovementState.Rolling && 
                                       _data?.roll?.IsInvulnerable(_rollTimer) == true;
        public float HitRadius => _data?.hitRadius ?? 0.5f;
        public bool IsCharging => _actionState == ActionState.Charging;
        public Vector2 AimDirection => _lastAimDirection;
        public AmmoGauge AmmoGauge => _ammoGauge;
        public GrenadeSystem GrenadeSystem => _grenadeSystem;

        public event System.Action<CharacterStateMachine, int, Vector2> OnDamageTaken;
        public event System.Action<CharacterStateMachine> OnDowned;
        public event System.Action<CharacterStateMachine> OnRevived;
        public event System.Action<CharacterStateMachine> OnEliminated;
        public event System.Action<CharacterStateMachine, Vector2, int> OnFired;
        public event System.Action<CharacterStateMachine> OnRollStarted;
        public event System.Action<CharacterStateMachine> OnSkillUsed;

        public void Initialize(CharacterData data, Team team, int playerIndex, Vector2 startPosition, int startRow)
        {
            _data = data;
            _team = team;
            _position = startPosition;
            _currentRowIndex = startRow;
            _currentHp = data.maxHp;
            _overallState = CharacterOverallState.Active;
            _movementState = MovementState.Idle;
            _actionState = ActionState.None;

            _ammoGauge = new AmmoGauge(data.weapon);
            _grenadeSystem = new GrenadeSystem(data.grenade, data.grenadeCount);
        }

        public void SetView(ICharacterView view)
        {
            _view = view;
        }

        private void Update()
        {
            if (_overallState == CharacterOverallState.Eliminated) return;

            float dt = GameTime.DeltaTime;

            UpdateCooldowns(dt);
            UpdateMovement(dt);
            UpdateAmmoRecharge(dt);
            UpdateDowned(dt);
            UpdatePassive(dt);

            _view?.UpdateView(this);
        }

        private void UpdateCooldowns(float dt)
        {
            if (_rollCooldownTimer > 0f)
                _rollCooldownTimer -= dt;

            if (_skillCooldownTimer > 0f)
                _skillCooldownTimer -= dt;

            if (_skillActiveTimer > 0f)
                _skillActiveTimer -= dt;
        }

        private void UpdateMovement(float dt)
        {
            switch (_movementState)
            {
                case MovementState.Moving:
                    float speed = _data.GetMoveSpeed(IsCharging);
                    _position += _moveDirection * speed * dt;
                    ClampPosition();
                    break;

                case MovementState.RowChanging:
                    UpdateRowChange(dt);
                    break;

                case MovementState.Rolling:
                    UpdateRoll(dt);
                    break;
            }
        }

        private void UpdateRowChange(float dt)
        {
            float duration = 2.5f / _data.RowChangeSpeed;
            _rowChangeTimer += dt;

            float t = Mathf.Clamp01(_rowChangeTimer / duration);
            float targetY = ServiceLocator.Get<Battlefield.BattlefieldSystem>()?.GetRowY(_targetRowIndex, _team) ?? _rowChangeStartY;
            _position.y = Mathf.Lerp(_rowChangeStartY, targetY, t);

            if (_moveDirection.x != 0f)
            {
                _position.x += _moveDirection.x * _data.moveSpeed * dt;
                ClampPosition();
            }

            if (t >= 1f)
            {
                _currentRowIndex = _targetRowIndex;
                _position.y = targetY;
                _movementState = _moveDirection.magnitude > 0.1f ? MovementState.Moving : MovementState.Idle;
            }
        }

        private void UpdateRoll(float dt)
        {
            _rollTimer += dt;

            float t = Mathf.Clamp01(_rollTimer / _data.roll.duration);
            float distance = _data.roll.distance * t;

            if (_rollTargetRow >= 0)
            {
                float targetY = ServiceLocator.Get<Battlefield.BattlefieldSystem>()?.GetRowY(_rollTargetRow, _team) ?? _position.y;
                _position = Vector2.Lerp(_rollStartPos, new Vector2(_rollStartPos.x, targetY), t);
            }
            else
            {
                _position = _rollStartPos + _rollDirection * distance;
            }

            ClampPosition();

            if (t >= 1f)
            {
                if (_rollTargetRow >= 0)
                {
                    _currentRowIndex = _rollTargetRow;
                    _rollTargetRow = -1;
                }

                _movementState = MovementState.Idle;

                if (_data.passive?.passiveType == PassiveType.RollShot)
                {
                    _rollShotBonusActive = true;
                    _rollShotBonusTimer = _data.passive.GetParam("window", 1f);
                }
            }
        }

        private void UpdateAmmoRecharge(float dt)
        {
            bool canRecharge = !_isAiming && _actionState != ActionState.Firing && _actionState != ActionState.Charging;
            _ammoGauge?.Update(dt, canRecharge);
        }

        private void UpdateDowned(float dt)
        {
            if (_overallState != CharacterOverallState.Downed) return;

            var downedData = ServiceLocator.Get<DownedData>();
            if (downedData == null) return;

            _downedTimer += dt;

            if (_moveDirection.magnitude > 0.1f)
            {
                _position += _moveDirection.normalized * downedData.crawlSpeed * dt;
                ClampPosition();
            }

            if (_downedTimer >= downedData.downedDuration)
            {
                Eliminate();
            }
        }

        private void UpdatePassive(float dt)
        {
            if (_rollShotBonusActive)
            {
                _rollShotBonusTimer -= dt;
                if (_rollShotBonusTimer <= 0f)
                {
                    _rollShotBonusActive = false;
                }
            }
        }

        private void ClampPosition()
        {
            var battlefield = ServiceLocator.Get<Battlefield.BattlefieldSystem>();
            if (battlefield != null)
            {
                _position = battlefield.ClampToField(_position);
            }
        }

        public void OnMoveCommand(MoveCommand cmd)
        {
            if (_overallState != CharacterOverallState.Active) return;
            if (_movementState == MovementState.Rolling || _movementState == MovementState.RowChanging) return;

            _moveDirection = cmd.Direction;

            if (cmd.Magnitude > 0.1f)
            {
                _movementState = MovementState.Moving;
            }
            else
            {
                _movementState = MovementState.Idle;
            }
        }

        public void OnRowChangeCommand(RowChangeCommand cmd)
        {
            if (_overallState != CharacterOverallState.Active) return;
            if (_movementState == MovementState.Rolling || _movementState == MovementState.RowChanging) return;

            var battlefield = ServiceLocator.Get<Battlefield.BattlefieldSystem>();
            if (battlefield == null) return;

            int newRow = _currentRowIndex + cmd.Direction;
            if (!battlefield.CanMoveToRow(_currentRowIndex, cmd.Direction, _team)) return;

            _targetRowIndex = newRow;
            _rowChangeStartY = _position.y;
            _rowChangeTimer = 0f;
            _movementState = MovementState.RowChanging;
        }

        public void OnAimStartCommand(AimStartCommand cmd)
        {
            if (_overallState != CharacterOverallState.Active) return;
            if (!CanPerformAction()) return;

            _isAiming = true;
            _aimStartTime = cmd.Timestamp;
            _actionState = ActionState.Aiming;
        }

        public void OnAimDragCommand(AimDragCommand cmd)
        {
            if (!_isAiming) return;

            _lastAimDirection = cmd.Direction;

            if (cmd.IsOutsideDeadzone && _actionState == ActionState.Aiming)
            {
                if (_data.weapon.fireMode == FireMode.ChargeRelease)
                {
                    _actionState = ActionState.Charging;
                }
                else
                {
                    TryFire();
                }
            }
            else if (cmd.IsOutsideDeadzone && _actionState == ActionState.Firing)
            {
                TryFire();
            }
        }

        public void OnAimReleaseCommand(AimReleaseCommand cmd)
        {
            if (!_isAiming) return;

            _isAiming = false;
            _lastAimDirection = cmd.Direction;

            if (!cmd.WasOutsideDeadzone)
            {
                _actionState = ActionState.None;
                return;
            }

            if (_actionState == ActionState.Charging)
            {
                float chargeTime = cmd.Timestamp - _aimStartTime;
                TryFireCharged(chargeTime);
            }

            _actionState = ActionState.None;
        }

        private void TryFire()
        {
            if (!_ammoGauge.CanFire()) return;

            _ammoGauge.ConsumeShot();
            _actionState = ActionState.Firing;

            int damage = _data.weapon.damagePerProjectile;

            if (_rollShotBonusActive && _data.passive?.passiveType == PassiveType.RollShot)
            {
                float bonus = _data.passive.GetParam("firstShotMul", 1.3f);
                damage = Mathf.RoundToInt(damage * bonus);
                _rollShotBonusActive = false;
            }

            OnFired?.Invoke(this, _lastAimDirection, damage);
        }

        private void TryFireCharged(float chargeTime)
        {
            if (!_ammoGauge.CanFire()) return;

            _ammoGauge.ConsumeShot();

            int damage = _data.weapon.GetChargeDamage(chargeTime);
            OnFired?.Invoke(this, _lastAimDirection, damage);
        }

        public void OnRollCommand(RollCommand cmd)
        {
            if (_overallState != CharacterOverallState.Active) return;
            if (_movementState == MovementState.Rolling) return;
            if (_rollCooldownTimer > 0f) return;

            Vector2 rollDir = cmd.Direction;
            if (rollDir.magnitude < 0.1f)
            {
                rollDir = _moveDirection.magnitude > 0.1f ? _moveDirection : Vector2.right;
            }

            if (_data.roll.allowRowRoll && Mathf.Abs(rollDir.y) > 0.7f)
            {
                var battlefield = ServiceLocator.Get<Battlefield.BattlefieldSystem>();
                int direction = rollDir.y > 0 ? 1 : -1;
                int newRow = _currentRowIndex + direction;

                if (battlefield != null && battlefield.CanMoveToRow(_currentRowIndex, direction, _team))
                {
                    _rollTargetRow = newRow;
                    rollDir = new Vector2(0f, direction);
                }
                else
                {
                    _rollTargetRow = -1;
                }
            }
            else
            {
                _rollTargetRow = -1;
            }

            _rollDirection = rollDir.normalized;
            _rollStartPos = _position;
            _rollTimer = 0f;
            _rollCooldownTimer = _data.roll.cooldown;
            _movementState = MovementState.Rolling;

            if (_actionState == ActionState.Charging)
            {
                _actionState = ActionState.None;
                _isAiming = false;
            }

            OnRollStarted?.Invoke(this);
        }

        public void OnGrenadeAimStartCommand(GrenadeAimStartCommand cmd)
        {
            if (_overallState != CharacterOverallState.Active) return;
            if (!CanPerformAction()) return;
            if (!_grenadeSystem.HasGrenade()) return;

            _actionState = ActionState.GrenadeAiming;
        }

        public void OnGrenadeAimDragCommand(GrenadeAimDragCommand cmd)
        {
            if (_actionState != ActionState.GrenadeAiming) return;
            _lastAimDirection = cmd.Direction;
        }

        public void OnGrenadeReleaseCommand(GrenadeReleaseCommand cmd)
        {
            if (_actionState != ActionState.GrenadeAiming)
            {
                _actionState = ActionState.None;
                return;
            }

            if (!cmd.WasOutsideDeadzone)
            {
                _actionState = ActionState.None;
                return;
            }

            if (_grenadeSystem.TryUseGrenade())
            {
                var combatSystem = ServiceLocator.Get<CombatSystem>();
                combatSystem?.SpawnGrenade(_position, cmd.TargetPosition, _data.grenade, _team);
            }

            _actionState = ActionState.None;
        }

        public void OnSkillStartCommand(SkillStartCommand cmd)
        {
            if (_overallState != CharacterOverallState.Active) return;
            if (!CanPerformAction()) return;
            if (_skillCooldownTimer > 0f) return;
            if (_data.skill == null) return;

            switch (_data.skill.useType)
            {
                case SkillUseType.Tap:
                    ActivateSkill();
                    break;

                case SkillUseType.Hold:
                case SkillUseType.DragRelease:
                    _actionState = ActionState.UsingSkill;
                    _skillHoldTimer = 0f;
                    break;
            }
        }

        public void OnSkillHoldCommand(SkillHoldCommand cmd)
        {
            if (_actionState != ActionState.UsingSkill) return;

            _skillHoldTimer = cmd.HoldTime;
            _lastAimDirection = cmd.Direction;

            if (_data.skill.useType == SkillUseType.Hold && _skillHoldTimer >= _data.skill.holdTime)
            {
                ActivateSkill();
            }
        }

        public void OnSkillReleaseCommand(SkillReleaseCommand cmd)
        {
            if (_actionState != ActionState.UsingSkill)
            {
                _actionState = ActionState.None;
                return;
            }

            if (_data.skill.useType == SkillUseType.DragRelease)
            {
                ActivateSkill(cmd.TargetPosition);
            }

            _actionState = ActionState.None;
        }

        private void ActivateSkill(Vector2? targetPosition = null)
        {
            if (_data.skill == null) return;

            _skillCooldownTimer = _data.skill.cooldown;
            _skillActiveTimer = _data.skill.duration;

            OnSkillUsed?.Invoke(this);
        }

        public bool IsSkillActive => _skillActiveTimer > 0f;
        public float SkillCooldownRatio => _data?.skill != null 
            ? Mathf.Clamp01(_skillCooldownTimer / _data.skill.cooldown) 
            : 0f;
        public float RollCooldownRatio => _data?.roll != null 
            ? Mathf.Clamp01(_rollCooldownTimer / _data.roll.cooldown) 
            : 0f;

        private bool CanPerformAction()
        {
            if (_movementState == MovementState.Rolling && !_data.roll.canFireDuring)
                return false;

            return _actionState == ActionState.None || _actionState == ActionState.Aiming;
        }

        public void TakeDamage(int damage, Vector2 sourceDirection)
        {
            if (_overallState != CharacterOverallState.Active) return;
            if (IsInvulnerable) return;

            _currentHp = Mathf.Max(0, _currentHp - damage);
            OnDamageTaken?.Invoke(this, damage, sourceDirection);

            if (_currentHp <= 0)
            {
                EnterDowned();
            }
        }

        public void TakeDownedDamage(int damage)
        {
            if (_overallState != CharacterOverallState.Downed) return;

            _downedHp = Mathf.Max(0, _downedHp - damage);

            if (_downedHp <= 0)
            {
                Eliminate();
            }
        }

        private void EnterDowned()
        {
            _overallState = CharacterOverallState.Downed;
            _movementState = MovementState.Idle;
            _actionState = ActionState.None;
            _isAiming = false;
            _downedTimer = 0f;

            var downedData = ServiceLocator.Get<DownedData>();
            _downedHp = downedData?.downedHp ?? 600;

            OnDowned?.Invoke(this);
        }

        public void Revive()
        {
            if (_overallState != CharacterOverallState.Downed) return;

            var downedData = ServiceLocator.Get<DownedData>();
            float ratio = downedData?.reviveHpRatio ?? 0.3f;

            _currentHp = Mathf.RoundToInt(_data.maxHp * ratio);
            _overallState = CharacterOverallState.Active;
            _movementState = MovementState.Idle;
            _downedTimer = 0f;
            _downedHp = 0;

            OnRevived?.Invoke(this);
        }

        private void Eliminate()
        {
            _overallState = CharacterOverallState.Eliminated;
            OnEliminated?.Invoke(this);
        }

        public void SetPosition(Vector2 position)
        {
            _position = position;
        }

        public void SetRowIndex(int rowIndex)
        {
            _currentRowIndex = rowIndex;
        }

        public void ResetForRound(Vector2 spawnPosition, int rowIndex)
        {
            _position = spawnPosition;
            _currentRowIndex = rowIndex;
            _currentHp = _data.maxHp;
            _overallState = CharacterOverallState.Active;
            _movementState = MovementState.Idle;
            _actionState = ActionState.None;
            _isAiming = false;
            _downedTimer = 0f;
            _downedHp = 0;
            _reviveProgress = 0f;
            _rollTimer = 0f;
            _rollCooldownTimer = 0f;
            _skillCooldownTimer = 0f;
            _skillActiveTimer = 0f;
            _rollShotBonusActive = false;

            _ammoGauge?.Reset();
            _grenadeSystem?.Reset();
        }

        public float GetWeakpointBonusMultiplier()
        {
            if (_data.passive?.passiveType != PassiveType.Weakpoint) return 1f;

            float aimTime = _data.passive.GetParam("aimTime", 2f);
            float currentAimTime = Time.time - _aimStartTime;

            if (currentAimTime >= aimTime)
            {
                return _data.passive.GetParam("weakpointMul", 1.25f);
            }

            return 1f;
        }
    }
}

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

        // Movement feel (MovementFeelData): continuous accel/decel, analog input, eased row/roll, recovery, input buffer
        private MovementFeelData _feel;
        private float _velX;
        private float _moveInputMag;
        private float _lastMoveDirX = 1f;
        private Vector2 _velocity;
        private float _recoveryTimer;
        private float _clock;
        private float _lastFireTime = -10f;
        private bool _bufRoll; private Vector2 _bufRollDir; private float _bufRollTime;
        private bool _bufAim; private float _bufAimTime; private bool _bufAimHeld;
        private bool _bufGrenade; private float _bufGrenadeTime;
        private int _bufRowDir; private float _bufRowTime;

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

        public MovementFeelData Feel
        {
            get
            {
                if (_feel == null) _feel = ServiceLocator.Get<MovementFeelData>() ?? MovementFeelData.Fallback;
                return _feel;
            }
        }
        /// <summary>Actual logic velocity (tiles/s) measured this frame (moves, row changes, rolls, crawl).</summary>
        public Vector2 Velocity => _velocity;
        /// <summary>Signed horizontal input (-1..1) after analog magnitude.</summary>
        public float MoveInputX => _moveDirection.x * _moveInputMag;
        public float FacingX => _lastMoveDirX;
        public float RollProgress => _movementState == MovementState.Rolling && _data?.roll != null ? Mathf.Clamp01(_rollTimer / _data.roll.duration) : 0f;
        public Vector2 RollDirection => _rollDirection;
        public bool IsRowRoll => _movementState == MovementState.Rolling && _rollTargetRow >= 0;
        public bool IsRecovering => _recoveryTimer > 0f;
        public float RowChangeProgress => _movementState == MovementState.RowChanging && _data != null ? Mathf.Clamp01(_rowChangeTimer / (2.5f / _data.RowChangeSpeed)) : 0f;
        /// <summary>Time.time of the last shot (view: aim-facing priority).</summary>
        public float LastFireTime => _lastFireTime;
        public bool IsAimingWeapon => _isAiming;
        private float BufferWindow => Feel.inputBufferWindow >= 0f ? Feel.inputBufferWindow : (_data?.roll != null ? _data.roll.inputBuffer : 0.1f);
        private bool RollBlocksActions => _movementState == MovementState.Rolling && _data?.roll != null && !_data.roll.canFireDuring;
        private bool RecoveryBlocksActions => _recoveryTimer > 0f && Feel.recoveryBlocksActions;
        /// <summary>True while a roll / landing recovery blocks shooting (input layers queue instead of dropping).</summary>
        public bool ActionsBlockedByMotion => RollBlocksActions || RecoveryBlocksActions;
        /// <summary>Seconds until roll/recovery stop blocking actions.</summary>
        private float TimeUntilActionsFree()
        {
            float t = 0f;
            if (_movementState == MovementState.Rolling && _data?.roll != null)
                t = Mathf.Max(0f, _data.roll.duration - _rollTimer) + (Feel.recoveryBlocksActions ? Feel.rollRecoveryTime : 0f);
            else if (RecoveryBlocksActions)
                t = _recoveryTimer;
            return t;
        }
        public GrenadeSystem GrenadeSystem => _grenadeSystem;

        public event System.Action<CharacterStateMachine, int, Vector2> OnDamageTaken;
        public event System.Action<CharacterStateMachine> OnDowned;
        public event System.Action<CharacterStateMachine> OnRevived;
        public event System.Action<CharacterStateMachine> OnEliminated;
        public event System.Action<CharacterStateMachine, Vector2, int> OnFired;
        public event System.Action<CharacterStateMachine> OnRollStarted;
        public event System.Action<CharacterStateMachine> OnSkillUsed;
        /// <summary>Skill fired: effect + optional aimed point (logic handled by CombatSystem).</summary>
        public event System.Action<CharacterStateMachine, SkillEffect, Vector2?> OnSkillActivated;

        private bool _pierceArmed;
        /// <summary>0..1 progress of an ally reviving this downed character.</summary>
        public float ReviveProgress => _reviveProgress;
        public bool IsBeingRevived { get; private set; }
        public float DownedTimeRemaining
        {
            get
            {
                var d = ServiceLocator.Get<DownedData>();
                return Mathf.Max(0f, (d != null ? d.downedDuration : 10f) - _downedTimer);
            }
        }
        public SkillEffect ActiveSkillEffect => _data?.skill != null ? _data.skill.effect : SkillEffect.None;
        public bool IsSpreadBoostActive => _skillActiveTimer > 0f && ActiveSkillEffect == SkillEffect.SpreadBoost;
        public bool IsPierceArmed => _pierceArmed;
        public bool IsSkillReady => _data?.skill != null && _skillCooldownTimer <= 0f;
        public bool IsHoldingSkill => _actionState == ActionState.UsingSkill;
        // ---- temporary gun upgrade (t48u drop) ----
        private float _damageBoostMul = 1f;
        private float _damageBoostTimer;
        public bool IsDamageBoosted => _damageBoostTimer > 0f;
        public float DamageBoostRemaining => Mathf.Max(0f, _damageBoostTimer);
        public float DamageBoostMul => IsDamageBoosted ? _damageBoostMul : 1f;

        /// <summary>Temporary weapon upgrade: shot damage × mul for duration seconds (refreshes, keeps the larger mul).</summary>
        public void ApplyDamageBoost(float mul, float duration)
        {
            if (_overallState != CharacterOverallState.Active || mul <= 0f || duration <= 0f) return;
            _damageBoostMul = IsDamageBoosted ? Mathf.Max(_damageBoostMul, mul) : mul;
            _damageBoostTimer = Mathf.Max(_damageBoostTimer, duration);
        }

        /// <summary>Ammo pickup: refill the whole bullet gauge.</summary>
        public void RefillAmmo() => _ammoGauge?.Reset();

        /// <summary>Heal an active character by a ratio of max HP (supply crate).</summary>
        public void Heal(float ratio)
        {
            if (_overallState != CharacterOverallState.Active || ratio <= 0f) return;
            _currentHp = Mathf.Min(MaxHp, _currentHp + Mathf.RoundToInt(MaxHp * ratio));
        }

        public bool HasGrenadeReady => _grenadeSystem != null && _grenadeSystem.HasGrenade();
        public bool IsBusy => !CanPerformAction();

        /// <summary>Returns true once if the next shot was armed by the pierce skill, then disarms.</summary>
        public bool ConsumePierceShot()
        {
            if (!_pierceArmed) return false;
            _pierceArmed = false;
            return true;
        }

        /// <summary>Adds revive progress (called by ReviveSystem). Completes the revive at 1.</summary>
        public void AddReviveProgress(float amount)
        {
            if (_overallState != CharacterOverallState.Downed) return;
            IsBeingRevived = amount > 0f;
            _reviveProgress = Mathf.Clamp01(_reviveProgress + amount);
            if (_reviveProgress >= 1f) Revive();
        }

        public void ResetReviveProgress()
        {
            _reviveProgress = 0f;
            IsBeingRevived = false;
        }

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
            _clock += dt;
            Vector2 before = _position;

            UpdateCooldowns(dt);
            UpdateMovement(dt);
            UpdateAmmoRecharge(dt);
            UpdateDowned(dt);
            UpdatePassive(dt);
            UpdateInputBuffer();

            if (dt > 0f) _velocity = (_position - before) / dt;

            _view?.UpdateView(this);
        }

        /// <summary>Advance cooldown/recharge timers only (same path Update uses). Public for tests.</summary>
        public void TickTimers(float dt) => UpdateCooldowns(dt);

        private void UpdateCooldowns(float dt)
        {
            // grenade recharge (01 §8: one grenade every GrenadeData.rechargeTime). Was never ticked before t49u.
            _grenadeSystem?.Update(dt);
            if (_damageBoostTimer > 0f) _damageBoostTimer -= dt;
            if (_rollCooldownTimer > 0f)
                _rollCooldownTimer -= dt;

            if (_skillCooldownTimer > 0f)
                _skillCooldownTimer -= dt;

            if (_skillActiveTimer > 0f)
                _skillActiveTimer -= dt;

            if (_recoveryTimer > 0f)
                _recoveryTimer = Mathf.Max(0f, _recoveryTimer - dt);
        }

        private void UpdateMovement(float dt)
        {
            switch (_movementState)
            {
                case MovementState.Idle:
                case MovementState.Moving:
                    if (_overallState != CharacterOverallState.Active) { _velX = 0f; break; }
                    UpdateHorizontal(dt, _data.GetMoveSpeed(IsCharging));
                    _movementState = (Feel.AnalogSpeedFactor(Mathf.Abs(MoveInputX)) > 0f || Mathf.Abs(_velX) > 0.02f)
                        ? MovementState.Moving : MovementState.Idle;
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
            // Same duration as the design (2.5 tiles / speed×1.5); only the curve is eased. Never stops mid-way.
            _position.y = Mathf.Lerp(_rowChangeStartY, targetY, Feel.RowEase(t));

            // Diagonal input keeps strafing during the row change (01 §3)
            UpdateHorizontal(dt, _data.moveSpeed);

            if (t >= 1f)
            {
                _currentRowIndex = _targetRowIndex;
                _position.y = targetY;
                _movementState = (Mathf.Abs(_velX) > 0.02f || _moveInputMag > 0.1f) ? MovementState.Moving : MovementState.Idle;
            }
        }

        private void UpdateRoll(float dt)
        {
            _rollTimer += dt;

            float t = Mathf.Clamp01(_rollTimer / _data.roll.duration);
            // Eased: fast start -> decel. Same total distance / duration / i-frames as RollData.
            float e = Feel.RollEase(t);
            float distance = _data.roll.distance * e;

            if (_rollTargetRow >= 0)
            {
                float targetY = ServiceLocator.Get<Battlefield.BattlefieldSystem>()?.GetRowY(_rollTargetRow, _team) ?? _position.y;
                _position = Vector2.Lerp(_rollStartPos, new Vector2(_rollStartPos.x, targetY), e);
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
                    _rollTargetRowWasSet = true;
                }

                _movementState = MovementState.Idle;
                _velX = _rollTargetRowWasSet ? 0f : _rollDirection.x * _data.moveSpeed * Feel.rollExitCarry;
                _rollTargetRowWasSet = false;
                _recoveryTimer = Feel.rollRecoveryTime;
                OnRollEnded?.Invoke(this);

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

            if (!(IsBeingRevived && downedData.pauseDownedTimerWhileReviving))
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

        /// <summary>Continuous horizontal movement: accel/decel toward the analog target speed.</summary>
        private void UpdateHorizontal(float dt, float maxSpeed)
        {
            var f = Feel;
            float inX = MoveInputX;
            float factor = f.AnalogSpeedFactor(Mathf.Abs(inX));
            float target = factor > 0f ? Mathf.Sign(inX) * factor * maxSpeed : 0f;
            if (_recoveryTimer > 0f) target *= f.rollRecoveryMoveMul;

            bool accelerating = Mathf.Abs(target) > 0.0001f &&
                                (Mathf.Abs(_velX) < 0.0001f || Mathf.Sign(target) == Mathf.Sign(_velX)) &&
                                Mathf.Abs(target) >= Mathf.Abs(_velX);
            float rate;
            if (accelerating)
            {
                rate = f.accelTime <= 0f ? float.MaxValue
                    : maxSpeed / f.accelTime * Mathf.Max(0.05f, f.accelCurve != null && f.accelCurve.length > 0 ? f.accelCurve.Evaluate(Mathf.Abs(_velX) / Mathf.Max(0.01f, maxSpeed)) : 1f);
            }
            else
            {
                rate = f.decelTime <= 0f ? float.MaxValue : maxSpeed / f.decelTime;
                if (Mathf.Abs(target) > 0.0001f && Mathf.Abs(_velX) > 0.0001f && Mathf.Sign(target) != Mathf.Sign(_velX))
                    rate *= f.turnDecelMul;
            }
            _velX = rate >= float.MaxValue * 0.5f ? target : Mathf.MoveTowards(_velX, target, rate * dt);
            if (Mathf.Abs(_velX) < 0.0001f) return;

            float prevX = _position.x;
            _position.x += _velX * dt;
            ClampPosition();
            if (Mathf.Abs(_position.x - (prevX + _velX * dt)) > 0.0001f) _velX = 0f; // hit the field edge
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

            // Always remember the latest stick (also during roll / row change) so a held direction
            // continues smoothly after the roll instead of stopping until the input changes.
            _moveDirection = cmd.Direction;
            _moveInputMag = cmd.Direction.sqrMagnitude > 0.0001f ? Mathf.Clamp01(cmd.Magnitude) : 0f;
            if (_moveInputMag > 0.1f && Mathf.Abs(cmd.Direction.x) > 0.2f) _lastMoveDirX = Mathf.Sign(cmd.Direction.x);

            if (_movementState == MovementState.Rolling || _movementState == MovementState.RowChanging) return;

            _movementState = (Feel.AnalogSpeedFactor(Mathf.Abs(MoveInputX)) > 0f || Mathf.Abs(_velX) > 0.02f)
                ? MovementState.Moving : MovementState.Idle;
        }

        public void OnRowChangeCommand(RowChangeCommand cmd)
        {
            if (_overallState != CharacterOverallState.Active) return;
            if (_movementState == MovementState.Rolling || _movementState == MovementState.RowChanging)
            {
                float wait = _movementState == MovementState.Rolling
                    ? _data.roll.duration - _rollTimer
                    : (2.5f / _data.RowChangeSpeed) - _rowChangeTimer;
                if (wait <= BufferWindow) { _bufRowDir = cmd.Direction; _bufRowTime = _clock; }
                return;
            }

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
            if (!CanPerformAction())
            {
                // Pressed during roll / landing: queue it (design 01 §9: 0.1s buffer, then fire when the roll ends)
                if ((RollBlocksActions || RecoveryBlocksActions) && TimeUntilActionsFree() <= BufferWindow &&
                    (_actionState == ActionState.None || _actionState == ActionState.Aiming))
                { _bufAim = true; _bufAimTime = _clock; }
                return;
            }

            _isAiming = true;
            _aimStartTime = cmd.Timestamp;
            _actionState = ActionState.Aiming;
        }

        public void OnAimDragCommand(AimDragCommand cmd)
        {
            if (!_isAiming) return;

            _lastAimDirection = cmd.Direction;
            if (RollBlocksActions || RecoveryBlocksActions) return; // no shots while rolling (01 §9)

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
            _bufAim = false;
            _bufAimHeld = false;
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
            _lastFireTime = Time.time;

            int damage = _data.weapon.damagePerProjectile;

            if (_rollShotBonusActive && _data.passive?.passiveType == PassiveType.RollShot)
            {
                float bonus = _data.passive.GetParam("firstShotMul", 1.3f);
                damage = Mathf.RoundToInt(damage * bonus);
                _rollShotBonusActive = false;
            }
            if (IsDamageBoosted) damage = Mathf.RoundToInt(damage * _damageBoostMul);

            OnFired?.Invoke(this, _lastAimDirection, damage);
        }

        private void TryFireCharged(float chargeTime)
        {
            if (!_ammoGauge.CanFire()) return;

            _ammoGauge.ConsumeShot();

            _lastFireTime = Time.time;
            int damage = _data.weapon.GetChargeDamage(chargeTime);
            if (IsDamageBoosted) damage = Mathf.RoundToInt(damage * _damageBoostMul);
            OnFired?.Invoke(this, _lastAimDirection, damage);
        }

        public void OnRollCommand(RollCommand cmd)
        {
            if (_overallState != CharacterOverallState.Active) return;
            bool rolling = _movementState == MovementState.Rolling;
            if (rolling || _recoveryTimer > 0f || _rollCooldownTimer > 0f)
            {
                float wait = Mathf.Max(rolling ? (_data.roll.duration - _rollTimer) + Feel.rollRecoveryTime : _recoveryTimer, _rollCooldownTimer);
                if (wait <= BufferWindow) { _bufRoll = true; _bufRollDir = cmd.Direction; _bufRollTime = _clock; }
                return;
            }
            StartRoll(cmd.Direction);
        }

        private bool _rollTargetRowWasSet;
        public event System.Action<CharacterStateMachine> OnRollEnded;

        private void StartRoll(Vector2 rollDir)
        {
            if (rollDir.magnitude < 0.1f)
            {
                // 01 §9: stick direction, or the last move direction when there is no input
                rollDir = (_moveInputMag > 0.1f && _moveDirection.magnitude > 0.1f) ? _moveDirection : new Vector2(_lastMoveDirX, 0f);
            }

            _rollTargetRow = -1;
            _rollTargetRowWasSet = false;
            bool rowRoll = false;
            if (_data.roll.allowRowRoll && Mathf.Abs(rollDir.y) > 0.7f)
            {
                var battlefield = ServiceLocator.Get<Battlefield.BattlefieldSystem>();
                int direction = rollDir.y > 0 ? 1 : -1;
                int newRow = _currentRowIndex + direction;

                if (battlefield != null && battlefield.CanMoveToRow(_currentRowIndex, direction, _team))
                {
                    _rollTargetRow = newRow;
                    rollDir = new Vector2(0f, direction);
                    rowRoll = true;
                }
            }

            if (!rowRoll && Feel.horizontalRollOnly)
            {
                // Stay on the row: diagonal / blocked-vertical rolls become left-right rolls
                float x = Mathf.Abs(rollDir.x) > 0.05f ? Mathf.Sign(rollDir.x) : _lastMoveDirX;
                rollDir = new Vector2(x, 0f);
            }
            if (Mathf.Abs(rollDir.x) > 0.05f) _lastMoveDirX = Mathf.Sign(rollDir.x);

            _rollDirection = rollDir.normalized;
            _rollStartPos = _position;
            _rollTimer = 0f;
            _rollCooldownTimer = _data.roll.cooldown;
            _movementState = MovementState.Rolling;
            _recoveryTimer = 0f;

            if (_actionState == ActionState.Charging)
            {
                // 01 §9: Raven charge is cancelled; if the button is still held the aim resumes after the roll
                _actionState = ActionState.None;
                _isAiming = false;
                _bufAimHeld = true;
            }

            OnRollStarted?.Invoke(this);
        }

        /// <summary>Runs queued roll / shot / grenade / row inputs as soon as they become legal.</summary>
        private void UpdateInputBuffer()
        {
            if (!_bufRoll && !_bufAim && !_bufAimHeld && !_bufGrenade && _bufRowDir == 0) return;
            if (_overallState != CharacterOverallState.Active) { ClearInputBuffer(); return; }
            float expire = BufferWindow + 0.05f;

            if (_bufRowDir != 0)
            {
                if (_clock - _bufRowTime > expire) _bufRowDir = 0;
                else if (_movementState != MovementState.Rolling && _movementState != MovementState.RowChanging)
                {
                    int d = _bufRowDir; _bufRowDir = 0;
                    OnRowChangeCommand(new RowChangeCommand(d, Time.time));
                }
            }
            if (_bufRoll)
            {
                if (_clock - _bufRollTime > expire) _bufRoll = false;
                else if (_movementState != MovementState.Rolling && _recoveryTimer <= 0f && _rollCooldownTimer <= 0f)
                {
                    _bufRoll = false;
                    StartRoll(_bufRollDir);
                }
            }
            if ((_bufAim || _bufAimHeld) && !_bufAimHeld && _clock - _bufAimTime > expire) _bufAim = false;
            if ((_bufAim || _bufAimHeld) && CanPerformAction() && !_isAiming)
            {
                _bufAim = false; _bufAimHeld = false;
                _isAiming = true;
                _aimStartTime = Time.time;
                _actionState = ActionState.Aiming;
            }
            if (_bufGrenade)
            {
                if (_clock - _bufGrenadeTime > expire) _bufGrenade = false;
                else if (CanPerformAction())
                {
                    _bufGrenade = false;
                    if (_grenadeSystem.HasGrenade()) _actionState = ActionState.GrenadeAiming;
                }
            }
        }

        private void ClearInputBuffer()
        {
            _bufRoll = false; _bufAim = false; _bufAimHeld = false; _bufGrenade = false; _bufRowDir = 0;
        }

        public void OnGrenadeAimStartCommand(GrenadeAimStartCommand cmd)
        {
            if (_overallState != CharacterOverallState.Active) return;
            if (!_grenadeSystem.HasGrenade()) return;
            if (!CanPerformAction())
            {
                if ((RollBlocksActions || RecoveryBlocksActions) && TimeUntilActionsFree() <= BufferWindow &&
                    (_actionState == ActionState.None || _actionState == ActionState.Aiming))
                { _bufGrenade = true; _bufGrenadeTime = _clock; }
                return;
            }

            _actionState = ActionState.GrenadeAiming;
        }

        public void OnGrenadeAimDragCommand(GrenadeAimDragCommand cmd)
        {
            if (_actionState != ActionState.GrenadeAiming) return;
            _lastAimDirection = cmd.Direction;
        }

        public void OnGrenadeReleaseCommand(GrenadeReleaseCommand cmd)
        {
            _bufGrenade = false;
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
            else if (_data.skill.useType == SkillUseType.Hold && cmd.HoldTime >= _data.skill.holdTime)
            {
                ActivateSkill();
            }

            _actionState = ActionState.None;
        }

        private void ActivateSkill(Vector2? targetPosition = null)
        {
            if (_data.skill == null) return;

            _skillCooldownTimer = _data.skill.cooldown;
            _skillActiveTimer = _data.skill.duration;
            if (_data.skill.effect == SkillEffect.PierceNextShot) _pierceArmed = true;
            if (_actionState == ActionState.UsingSkill) _actionState = ActionState.None;

            OnSkillUsed?.Invoke(this);
            OnSkillActivated?.Invoke(this, _data.skill.effect, targetPosition);
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
            if (RecoveryBlocksActions)
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
            _reviveProgress = 0f;
            IsBeingRevived = false;
            _pierceArmed = false;
            _skillActiveTimer = 0f;
            _velX = 0f;
            _recoveryTimer = 0f;
            _rollTargetRow = -1;
            ClearInputBuffer();

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
            _reviveProgress = 0f;
            IsBeingRevived = false;

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
            _velX = 0f;
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
            _pierceArmed = false;
            IsBeingRevived = false;
            _velX = 0f;
            _velocity = Vector2.zero;
            _recoveryTimer = 0f;
            _rollTargetRow = -1;
            _moveInputMag = 0f;
            ClearInputBuffer();

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

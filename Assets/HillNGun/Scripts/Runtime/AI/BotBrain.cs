using UnityEngine;
using HillNGun.Input;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.Core;

namespace HillNGun.AI
{
    public class BotBrain : IInputReceiver
    {
        private CharacterStateMachine _character;
        private BotBehaviorData _behaviorData;
        private CharacterStateMachine[] _allies;

        private float _decisionTimer;
        private float _aimTimer;
        private float _rollCooldownTimer;
        private float _grenadeCooldownTimer;
        private float _skillCooldownTimer;

        private BotState _currentState = BotState.Idle;
        private CharacterStateMachine _currentTarget;
        private CharacterStateMachine _reviveTarget;
        private Vector2 _aimDirection;
        private bool _isAiming;
        private float _aimStartTime;

        public CharacterStateMachine Character => _character;

        /// <summary>Optional shootable objective (PvP supply crate). Bots shoot it when it is in range.</summary>
        public System.Func<Vector2?> ObjectiveProvider { get; set; }
        private bool _shootObjective;
        private Vector2 _objectivePos;

        public BotBrain(CharacterStateMachine character, BotBehaviorData behaviorData)
        {
            _character = character;
            _behaviorData = behaviorData ?? ScriptableObject.CreateInstance<BotBehaviorData>();
        }

        public void SetAllies(CharacterStateMachine[] allies)
        {
            _allies = allies;
        }

        public void Update(float deltaTime, CharacterStateMachine[] enemies)
        {
            if (_character == null || _character.OverallState != CharacterOverallState.Active)
                return;

            _decisionTimer -= deltaTime;
            _rollCooldownTimer -= deltaTime;

            if (TickBulletDodge(deltaTime)) return;
            _grenadeCooldownTimer -= deltaTime;
            _skillCooldownTimer -= deltaTime;

            if (_decisionTimer <= 0f)
            {
                MakeDecision(enemies);
                _decisionTimer = _behaviorData.decisionInterval;
            }

            if (_skillHolding)
            {
                TickSkillHold(deltaTime);
                return;
            }

            ExecuteCurrentState(deltaTime, enemies);
        }

        // ---- bullet dodge (t48u): roll if it is ready, otherwise a short side-step ----
        private HillNGun.Combat.BulletDodgeSense _bulletSense;
        private float _sidestepTimer;
        private Vector2 _sidestepDir;
        public int BulletDodges => _bulletSense?.Reactions ?? 0;

        private bool TickBulletDodge(float dt)
        {
            if (_sidestepTimer > 0f)
            {
                _sidestepTimer -= dt;
                _character.OnMoveCommand(new MoveCommand(_sidestepDir, 1f, Time.time));
                if (_sidestepTimer <= 0f) _character.OnMoveCommand(MoveCommand.Stop(Time.time));
                return true;
            }
            if (!_behaviorData.bulletDodge.Enabled) return false;
            _bulletSense ??= new HillNGun.Combat.BulletDodgeSense(_behaviorData.bulletDodge);
            var combat = ServiceLocator.Get<HillNGun.Combat.CombatSystem>();
            var list = combat?.ProjectilePool?.ActiveProjectiles;
            Team hostile = _character.Team == Team.Player ? Team.Enemy : Team.Player;
            float radius = _character.Data != null ? _character.Data.hitRadius : 0.5f;
            if (!_bulletSense.Tick(Time.time, _character.Position, radius, list, hostile, out var dir)) return false;
            if (_isAiming) { _character.OnAimReleaseCommand(new AimReleaseCommand(_aimDirection, false, Time.time)); _isAiming = false; }
            if (_rollCooldownTimer <= 0f && _character.RollCooldownRatio <= 0f && !_character.ActionsBlockedByMotion)
            {
                _character.OnRollCommand(new RollCommand(dir, Time.time));
                _rollCooldownTimer = _character.Data?.roll?.cooldown ?? 4f;
            }
            else
            {
                _sidestepDir = dir;
                _sidestepTimer = _behaviorData.sidestepTime;
            }
            _currentState = BotState.Idle;
            return true;
        }

        private bool _skillHolding;
        private float _skillHoldTime;

        private void TickSkillHold(float dt)
        {
            if (!_character.IsHoldingSkill) { _skillHolding = false; return; }
            _skillHoldTime += dt;
            Vector2 dir = _currentTarget != null ? (_currentTarget.Position - _character.Position).normalized : Vector2.up;
            _character.OnSkillHoldCommand(new SkillHoldCommand(_skillHoldTime, dir, Time.time));
            if (!_character.IsHoldingSkill) _skillHolding = false;
        }

        private void MakeDecision(CharacterStateMachine[] enemies)
        {
            _reviveTarget = FindDownedAlly();
            if (_reviveTarget != null && ShouldRevive(_reviveTarget))
            {
                _currentState = BotState.Reviving;
                return;
            }

            _currentTarget = FindBestTarget(enemies);
            _shootObjective = false;

            Vector2? objective = ObjectiveProvider?.Invoke();
            if (objective.HasValue)
            {
                float range = _character.Data?.weapon?.range ?? 7f;
                float d = Vector2.Distance(_character.Position, objective.Value);
                bool targetInRange = _currentTarget != null && Vector2.Distance(_character.Position, _currentTarget.Position) <= range;
                if (d <= range && (!targetInRange || Random.value < 0.35f))
                {
                    _shootObjective = true;
                    _objectivePos = objective.Value;
                    _currentState = BotState.Attacking;
                    return;
                }
            }

            if (_currentTarget == null)
            {
                _currentState = BotState.Idle;
                return;
            }

            float distanceToTarget = Vector2.Distance(_character.Position, _currentTarget.Position);
            float weaponRange = _character.Data?.weapon?.range ?? 7f;

            if (ShouldDodge())
            {
                _currentState = BotState.Dodging;
            }
            else if (ShouldUseGrenade(_currentTarget))
            {
                _currentState = BotState.ThrowingGrenade;
            }
            else if (ShouldUseSkill())
            {
                _currentState = BotState.UsingSkill;
            }
            else if (distanceToTarget <= weaponRange)
            {
                _currentState = BotState.Attacking;
            }
            else
            {
                _currentState = BotState.Moving;
            }
        }

        private void ExecuteCurrentState(float deltaTime, CharacterStateMachine[] enemies)
        {
            switch (_currentState)
            {
                case BotState.Idle:
                    ExecuteIdle();
                    break;
                case BotState.Moving:
                    ExecuteMoving();
                    break;
                case BotState.Attacking:
                    ExecuteAttacking(deltaTime);
                    break;
                case BotState.Dodging:
                    ExecuteDodging();
                    break;
                case BotState.ThrowingGrenade:
                    ExecuteGrenade();
                    break;
                case BotState.UsingSkill:
                    ExecuteSkill();
                    break;
                case BotState.Reviving:
                    ExecuteReviving(enemies);
                    break;
            }
        }

        private void ExecuteIdle()
        {
            if (_isAiming)
            {
                CancelAim();
            }

            _character.OnMoveCommand(MoveCommand.Stop(Time.time));
        }

        private void ExecuteMoving()
        {
            if (_currentTarget == null) return;

            if (_isAiming)
            {
                CancelAim();
            }

            // Row-based approach (01 §3: always stand on a row). Out of range -> step to the row nearer the
            // target, otherwise strafe along the row toward the target's X at full stick.
            if (_character.MovementState == MovementState.RowChanging || _character.MovementState == MovementState.Rolling) return;
            Vector2 delta = _currentTarget.Position - _character.Position;
            int towardRow = TowardTargetRowDirection(delta.y);
            if (towardRow != 0 && Random.value < _behaviorData.rowChangeChance)
            {
                _character.OnRowChangeCommand(new RowChangeCommand(towardRow, Time.time));
            }
            else if (Mathf.Abs(delta.x) > 0.3f)
            {
                _character.OnMoveCommand(new MoveCommand(new Vector2(Mathf.Sign(delta.x), 0f), 1f, Time.time));
            }
            else
            {
                _character.OnMoveCommand(MoveCommand.Stop(Time.time));
            }
        }

        /// <summary>+1/-1 row step that reduces the vertical gap to the target, 0 if already on the closest row.</summary>
        private int TowardTargetRowDirection(float dy)
        {
            var bf = ServiceLocator.Get<HillNGun.Battlefield.BattlefieldSystem>();
            if (bf == null || Mathf.Abs(dy) < 0.5f) return 0;
            int cur = _character.CurrentRowIndex;
            float y = bf.GetRowY(cur, _character.Team);
            for (int d = -1; d <= 1; d += 2)
            {
                if (!bf.CanMoveToRow(cur, d, _character.Team)) continue;
                float ny = bf.GetRowY(cur + d, _character.Team);
                if ((ny - y) * dy > 0f) return d;
            }
            return 0;
        }

        private void ExecuteAttacking(float deltaTime)
        {
            if (_currentTarget == null && !_shootObjective) return;

            Vector2 aimAt = _shootObjective ? _objectivePos : _currentTarget.Position;
            Vector2 dirToTarget = (aimAt - _character.Position).normalized;
            float jitter = _behaviorData.aimJitter * Mathf.Deg2Rad;
            float randomAngle = Random.Range(-jitter, jitter);
            _aimDirection = RotateVector(dirToTarget, randomAngle);

            if (!_isAiming)
            {
                StartAim();
            }
            else
            {
                _aimTimer += deltaTime;

                _character.OnAimDragCommand(new AimDragCommand(
                    _aimDirection,
                    100f,
                    true,
                    Time.time
                ));

                if (_aimTimer >= _behaviorData.aimDuration)
                {
                    Fire();
                }
            }
        }

        private void StartAim()
        {
            _isAiming = true;
            _aimTimer = 0f;
            _aimStartTime = Time.time;

            _character.OnAimStartCommand(new AimStartCommand(
                Vector2.zero,
                Time.time
            ));
        }

        private void Fire()
        {
            _character.OnAimReleaseCommand(new AimReleaseCommand(
                _aimDirection,
                true,
                Time.time
            ));

            _isAiming = false;
            _aimTimer = 0f;
            _currentState = BotState.Idle;
        }

        private void CancelAim()
        {
            if (!_isAiming) return;

            _character.OnAimReleaseCommand(new AimReleaseCommand(
                Vector2.zero,
                false,
                Time.time
            ));

            _isAiming = false;
            _aimTimer = 0f;
        }

        private void ExecuteDodging()
        {
            if (_rollCooldownTimer > 0f)
            {
                _currentState = BotState.Moving;
                return;
            }

            Vector2 rollDir = Random.value > 0.5f ? Vector2.left : Vector2.right;

            if (_currentTarget != null)
            {
                Vector2 awayFromTarget = (_character.Position - _currentTarget.Position).normalized;
                rollDir = awayFromTarget;
            }

            _character.OnRollCommand(new RollCommand(rollDir, Time.time));
            _rollCooldownTimer = _character.Data?.roll?.cooldown ?? 4f;
            _currentState = BotState.Idle;
        }

        private void ExecuteGrenade()
        {
            if (_grenadeCooldownTimer > 0f || _currentTarget == null || !_behaviorData.useGrenades || !_character.HasGrenadeReady)
            {
                _currentState = BotState.Idle;
                return;
            }

            Vector2 grenadeTarget = _currentTarget.Position;

            _character.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, Time.time));
            _character.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(grenadeTarget, true, Time.time));

            _grenadeCooldownTimer = _character.Data?.grenade?.rechargeTime ?? 12f;
            _currentState = BotState.Idle;
        }

        private void ExecuteSkill()
        {
            if (_skillCooldownTimer > 0f)
            {
                _currentState = BotState.Idle;
                return;
            }

            var skill = _character.Data?.skill;
            if (skill == null || !_behaviorData.useSkills || !_character.IsSkillReady)
            {
                _currentState = BotState.Idle;
                return;
            }

            Vector2 targetPos = _currentTarget?.Position ?? _character.Position;
            if (skill.effect == SkillEffect.RepairCover)
            {
                // 긴급 보수: only when an own cover is damaged enough
                var combat = ServiceLocator.Get<HillNGun.Combat.CombatSystem>();
                var cover = combat?.FindCoverToRepair(_character, _behaviorData.repairCoverBelowRatio);
                if (cover == null) { _currentState = BotState.Idle; return; }
                targetPos = cover.Position;
            }

            _character.OnSkillStartCommand(new SkillStartCommand(Vector2.zero, Time.time));
            switch (skill.useType)
            {
                case SkillUseType.Hold:
                    // hold over time (same as a player); TickSkillHold activates at skill.holdTime
                    _skillHolding = _character.IsHoldingSkill;
                    _skillHoldTime = 0f;
                    break;
                case SkillUseType.DragRelease:
                    _character.OnSkillReleaseCommand(new SkillReleaseCommand(targetPos, 0f, Time.time));
                    break;
            }

            _skillCooldownTimer = skill.cooldown;
            _currentState = BotState.Idle;
        }

        private void ExecuteReviving(CharacterStateMachine[] enemies)
        {
            if (_reviveTarget == null || _reviveTarget.OverallState != CharacterOverallState.Downed)
            {
                _currentState = BotState.Idle;
                _reviveTarget = null;
                return;
            }

            float distance = Vector2.Distance(_character.Position, _reviveTarget.Position);
            var downedData = ServiceLocator.Get<DownedData>();
            float reviveRange = downedData?.reviveRange ?? 1.5f;

            if (distance <= reviveRange * 0.8f)
            {
                _character.OnMoveCommand(MoveCommand.Stop(Time.time));
            }
            else if (_reviveTarget.CurrentRowIndex != _character.CurrentRowIndex
                     && Mathf.Abs(_reviveTarget.Position.y - _character.Position.y) > reviveRange * 0.5f)
            {
                if (_character.MovementState != MovementState.RowChanging)
                    _character.OnRowChangeCommand(new RowChangeCommand(
                        _reviveTarget.CurrentRowIndex > _character.CurrentRowIndex ? 1 : -1, Time.time));
            }
            else
            {
                float dx = _reviveTarget.Position.x - _character.Position.x;
                _character.OnMoveCommand(new MoveCommand(new Vector2(Mathf.Sign(dx), 0f), 1f, Time.time));
            }
        }

        private CharacterStateMachine FindDownedAlly()
        {
            if (_allies == null) return null;

            CharacterStateMachine closest = null;
            float closestDist = float.MaxValue;

            foreach (var ally in _allies)
            {
                if (ally == null || ally == _character) continue;
                if (ally.OverallState != CharacterOverallState.Downed) continue;

                float dist = Vector2.Distance(_character.Position, ally.Position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = ally;
                }
            }

            return closest;
        }

        private bool ShouldRevive(CharacterStateMachine target)
        {
            if (target == null) return false;

            float distance = Vector2.Distance(_character.Position, target.Position);
            float seekDist = _behaviorData?.reviveSeekDistance ?? 5f;

            if (distance <= seekDist)
            {
                return Random.value < (_behaviorData?.revivePriority ?? 0.6f);
            }

            return false;
        }

        private CharacterStateMachine FindBestTarget(CharacterStateMachine[] enemies)
        {
            if (enemies == null || enemies.Length == 0) return null;

            CharacterStateMachine best = null;
            float bestScore = float.MinValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.OverallState != CharacterOverallState.Active)
                    continue;

                float score = CalculateTargetScore(enemy);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            return best;
        }

        private float CalculateTargetScore(CharacterStateMachine target)
        {
            float distance = Vector2.Distance(_character.Position, target.Position);
            float distanceScore = 1f - Mathf.Clamp01(distance / 20f);

            float hpRatio = (float)target.CurrentHp / target.Data.maxHp;
            float hpScore = 1f - hpRatio;

            bool sameRow = target.CurrentRowIndex == _character.CurrentRowIndex;
            float rowScore = sameRow ? 0.3f : 0f;

            return distanceScore * _behaviorData.distanceWeight +
                   hpScore * _behaviorData.lowHpWeight +
                   rowScore;
        }

        private bool ShouldDodge()
        {
            if (_rollCooldownTimer > 0f) return false;

            float hpRatio = (float)_character.CurrentHp / _character.Data.maxHp;
            if (hpRatio < _behaviorData.dodgeHpThreshold && Random.value < _behaviorData.dodgeChance)
            {
                return true;
            }

            return false;
        }

        private bool ShouldUseGrenade(CharacterStateMachine target)
        {
            if (_grenadeCooldownTimer > 0f) return false;
            if (target == null) return false;
            if (_character.Data?.grenadeCount <= 0) return false;

            float distance = Vector2.Distance(_character.Position, target.Position);
            float minRange = _character.Data?.grenade?.minRange ?? 2f;
            float maxRange = _character.Data?.grenade?.maxRange ?? 9f;

            if (distance >= minRange && distance <= maxRange)
            {
                return Random.value < _behaviorData.grenadeUseChance;
            }

            return false;
        }

        private bool ShouldUseSkill()
        {
            if (_skillCooldownTimer > 0f) return false;

            return Random.value < _behaviorData.skillUseChance;
        }

        private Vector2 RotateVector(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        public void OnMoveCommand(MoveCommand cmd) { }
        public void OnRowChangeCommand(RowChangeCommand cmd) { }
        public void OnAimStartCommand(AimStartCommand cmd) { }
        public void OnAimDragCommand(AimDragCommand cmd) { }
        public void OnAimReleaseCommand(AimReleaseCommand cmd) { }
        public void OnRollCommand(RollCommand cmd) { }
        public void OnGrenadeAimStartCommand(GrenadeAimStartCommand cmd) { }
        public void OnGrenadeAimDragCommand(GrenadeAimDragCommand cmd) { }
        public void OnGrenadeReleaseCommand(GrenadeReleaseCommand cmd) { }
        public void OnSkillStartCommand(SkillStartCommand cmd) { }
        public void OnSkillHoldCommand(SkillHoldCommand cmd) { }
        public void OnSkillReleaseCommand(SkillReleaseCommand cmd) { }
    }

    public enum BotState
    {
        Idle,
        Moving,
        Attacking,
        Dodging,
        ThrowingGrenade,
        UsingSkill,
        Reviving
    }
}

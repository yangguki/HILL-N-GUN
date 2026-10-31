using UnityEngine;
using System.Collections.Generic;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.Combat;
using HillNGun.Input;

namespace HillNGun.AI
{
    /// <summary>
    /// Pure-logic driver for a character's skill, grenade and ally revive (PvE allies, PvP bots, smoke autopilot).
    /// Uses the same input commands a player would send. Toggles/thresholds come from BotBehaviorData.
    /// </summary>
    public class AbilityPilot
    {
        private readonly CharacterStateMachine _c;
        private readonly BotBehaviorData _b;
        private float _grenadeTimer;
        private float _skillHold;
        private bool _holding;

        public CharacterStateMachine Character => _c;
        /// <summary>True while a hold-type skill is being charged (don't fire/aim this tick).</summary>
        public bool IsHolding => _holding;
        public CharacterStateMachine ReviveTarget { get; private set; }
        public int SkillsUsed { get; private set; }
        public int GrenadesThrown { get; private set; }

        public AbilityPilot(CharacterStateMachine character, BotBehaviorData behavior, float grenadeInitialDelay = 2f)
        {
            _c = character;
            _b = behavior;
            _grenadeTimer = grenadeInitialDelay;
        }

        /// <param name="target">Enemy target in weapon range (null if none).</param>
        /// <param name="team">Own team (for revive).</param>
        /// <returns>True when the pilot took over movement this tick (moving to revive).</returns>
        public bool Tick(float dt, Vector2? target, IReadOnlyList<CharacterStateMachine> team)
        {
            if (_c == null || _c.OverallState != CharacterOverallState.Active)
            {
                _holding = false;
                ReviveTarget = null;
                return false;
            }
            float now = Time.time;
            _grenadeTimer -= dt;

            if (_holding)
            {
                ContinueHold(dt, target, now);
                return false;
            }

            bool moving = TickRevive(team, now);
            if (!_c.IsBusy) TickSkill(target, now);
            if (!_holding && !_c.IsBusy) TickGrenade(target, now);
            return moving;
        }

        private bool TickRevive(IReadOnlyList<CharacterStateMachine> team, float now)
        {
            ReviveTarget = null;
            if (_b != null && !_b.reviveAllies) return false;
            if (team == null) return false;
            float seek = _b != null ? _b.reviveSeekDistance : 5f;
            var downed = ServiceLocator.Get<DownedData>();
            float range = downed != null ? downed.reviveRange : 1f;

            CharacterStateMachine best = null;
            float bestDist = seek;
            foreach (var t in team)
            {
                if (t == null || t == _c || t.OverallState != CharacterOverallState.Downed) continue;
                float d = Vector2.Distance(t.Position, _c.Position);
                if (d <= bestDist) { bestDist = d; best = t; }
            }
            if (best == null) return false;
            ReviveTarget = best;
            if (bestDist <= range * 0.8f)
            {
                _c.OnMoveCommand(MoveCommand.Stop(now));
                return true;
            }
            if (_c.MovementState == MovementState.RowChanging || _c.MovementState == MovementState.Rolling) return true;

            if (best.CurrentRowIndex != _c.CurrentRowIndex && Mathf.Abs(best.Position.y - _c.Position.y) > range * 0.5f)
            {
                _c.OnRowChangeCommand(new RowChangeCommand(best.CurrentRowIndex > _c.CurrentRowIndex ? 1 : -1, now));
                return true;
            }
            float dx = best.Position.x - _c.Position.x;
            _c.OnMoveCommand(Mathf.Abs(dx) > 0.2f ? new MoveCommand(new Vector2(Mathf.Sign(dx), 0f), 1f, now) : MoveCommand.Stop(now));
            return true;
        }

        private void TickSkill(Vector2? target, float now)
        {
            if (_b != null && !_b.useSkills) return;
            if (!_c.IsSkillReady) return;
            var skill = _c.Data.skill;

            switch (skill.effect)
            {
                case SkillEffect.RepairCover:
                {
                    var combat = ServiceLocator.Get<CombatSystem>();
                    var cover = combat?.FindCoverToRepair(_c, _b != null ? _b.repairCoverBelowRatio : 0.6f);
                    if (cover == null) return;
                    _c.OnSkillStartCommand(new SkillStartCommand(Vector2.zero, now));
                    _c.OnSkillReleaseCommand(new SkillReleaseCommand(cover.Position, 0f, now));
                    if (!_c.IsSkillReady) SkillsUsed++;
                    return;
                }
                default:
                    if (!target.HasValue) return;
                    if (_c.IsPierceArmed || _c.IsSkillActive) return;
                    _c.OnSkillStartCommand(new SkillStartCommand(Vector2.zero, now));
                    if (skill.useType == SkillUseType.Hold)
                    {
                        _holding = _c.IsHoldingSkill;
                        _skillHold = 0f;
                    }
                    else if (skill.useType == SkillUseType.DragRelease)
                    {
                        _c.OnSkillReleaseCommand(new SkillReleaseCommand(target.Value, 0f, now));
                    }
                    if (!_c.IsSkillReady) SkillsUsed++;
                    return;
            }
        }

        private void ContinueHold(float dt, Vector2? target, float now)
        {
            if (!_c.IsHoldingSkill)
            {
                _holding = false;
                if (!_c.IsSkillReady) SkillsUsed++;
                return;
            }
            _skillHold += dt;
            Vector2 dir = target.HasValue ? (target.Value - _c.Position).normalized : Vector2.up;
            _c.OnSkillHoldCommand(new SkillHoldCommand(_skillHold, dir, now));
            if (!_c.IsHoldingSkill)
            {
                _holding = false;
                SkillsUsed++;
            }
        }

        private void TickGrenade(Vector2? target, float now)
        {
            if (_b != null && !_b.useGrenades) return;
            if (_grenadeTimer > 0f || !target.HasValue || !_c.HasGrenadeReady) return;
            var g = _c.Data.grenade;
            if (g == null) return;
            float d = Vector2.Distance(target.Value, _c.Position);
            if (d < g.minRange || d > g.maxRange) return;

            _c.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, now));
            _c.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(target.Value, true, now));
            _grenadeTimer = _b != null ? _b.grenadeMinInterval : 4f;
            GrenadesThrown++;
        }
    }
}

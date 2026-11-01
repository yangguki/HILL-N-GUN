using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.Combat;

namespace HillNGun.Input
{
    /// <summary>Runtime ITouchControlContext over a CharacterStateMachine + CombatSystem (read-only adapter).</summary>
    public class CharacterTouchContext : ITouchControlContext
    {
        public CharacterStateMachine Character { get; set; }

        public CharacterTouchContext(CharacterStateMachine c) { Character = c; }

        private CharacterData D => Character != null ? Character.Data : null;

        public bool HasCharacter => Character != null && D != null;
        public float Now => Time.time;
        public Vector2 CharacterPosition => Character != null ? Character.Position : Vector2.zero;
        public bool IsChargeWeapon => D?.weapon != null && D.weapon.fireMode == FireMode.ChargeRelease;
        public float FireInterval => D?.weapon != null ? D.weapon.fireInterval : 0.5f;
        public bool HoldToRepeat => D?.weapon != null && D.weapon.holdToRepeat;
        public float WeaponRange => D?.weapon != null ? D.weapon.range : 6f;
        public bool CanFireNow => Character != null && Character.OverallState == CharacterOverallState.Active &&
                                  !Character.ActionsBlockedByMotion &&
                                  Character.ActionState != ActionState.GrenadeAiming && Character.ActionState != ActionState.UsingSkill;
        public bool IsAiming => Character != null && Character.IsAimingWeapon;
        public bool HasSkill => D?.skill != null;
        public SkillUseType SkillType => D?.skill != null ? D.skill.useType : SkillUseType.Tap;
        public float SkillRange => D?.skill != null ? D.skill.targetRange : 6f;
        public bool IsUsingSkill => Character != null && Character.IsHoldingSkill;
        public bool HasGrenade => Character != null && Character.HasGrenadeReady;
        public bool IsGrenadeAiming => Character != null && Character.ActionState == ActionState.GrenadeAiming;
        public float GrenadeMinRange => D?.grenade != null ? D.grenade.minRange : 2f;
        public float GrenadeMaxRange => D?.grenade != null ? D.grenade.maxRange : 9f;
        public float GrenadeRadius => D?.grenade != null ? D.grenade.radius : 1.5f;

        public Vector2? NearestEnemy(float maxRange)
        {
            if (Character == null) return null;
            var combat = ServiceLocator.Get<CombatSystem>();
            if (combat == null) return null;
            Vector2 from = Character.Position;
            Vector2? best = null;
            float bestD = maxRange;
            var chars = combat.Characters;
            for (int i = 0; i < chars.Count; i++)
            {
                var c = chars[i];
                if (c == null || c.Team == Character.Team || c.OverallState != CharacterOverallState.Active) continue;
                float d = Vector2.Distance(from, c.Position);
                if (d <= bestD) { bestD = d; best = c.Position; }
            }
            var targets = combat.CombatTargets;
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t == null || !t.CanBeHit || t.Team == Character.Team || t is INeutralTarget) continue;
                float d = Vector2.Distance(from, t.Position);
                if (d <= bestD) { bestD = d; best = t.Position; }
            }
            return best;
        }

        public Vector2 ClampToField(Vector2 p)
        {
            var bf = ServiceLocator.Get<Battlefield.BattlefieldSystem>();
            return bf != null ? bf.ClampToField(p) : p;
        }
    }
}

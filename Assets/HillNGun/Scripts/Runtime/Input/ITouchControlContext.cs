using UnityEngine;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.Input
{
    /// <summary>
    /// What the touch router needs to know about the controlled character (read-only). Lets the router stay pure
    /// logic: the runtime uses CharacterTouchContext, tests use a fake.
    /// </summary>
    public interface ITouchControlContext
    {
        bool HasCharacter { get; }
        float Now { get; }
        Vector2 CharacterPosition { get; }
        bool IsChargeWeapon { get; }
        float FireInterval { get; }
        /// <summary>Weapon keeps firing at FireInterval while the attack stick is held aimed (WeaponData.holdToRepeat).</summary>
        bool HoldToRepeat { get; }
        float WeaponRange { get; }
        /// <summary>False while rolling / landing / downed (a release then queues instead of being dropped).</summary>
        bool CanFireNow { get; }
        /// <summary>The character currently holds an aim (AimStart accepted).</summary>
        bool IsAiming { get; }
        bool HasSkill { get; }
        SkillUseType SkillType { get; }
        float SkillRange { get; }
        bool IsUsingSkill { get; }
        bool HasGrenade { get; }
        bool IsGrenadeAiming { get; }
        float GrenadeMinRange { get; }
        float GrenadeMaxRange { get; }
        float GrenadeRadius { get; }
        /// <summary>Nearest hittable enemy within maxRange (logic tiles), or null.</summary>
        Vector2? NearestEnemy(float maxRange);
        Vector2 ClampToField(Vector2 p);
    }
}

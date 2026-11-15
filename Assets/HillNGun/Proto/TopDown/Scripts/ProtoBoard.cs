using System.Collections.Generic;
using UnityEngine;
using HillNGun.Characters;
using HillNGun.Combat;
using HillNGun.Cover;
using HillNGun.PvE;

namespace HillNGun.Proto.TopDown
{
    /// <summary>What the shared pixel view reads. Top-down fills enemies; frontline fills fighters and a divider.</summary>
    public interface IProtoBoard
    {
        CharacterStateMachine Player { get; }
        CombatSystem Combat { get; }
        IReadOnlyList<CoverInstance> Covers { get; }
        Vector2 AimDirection { get; }
        bool ShowGrenadeAim { get; }
        Vector2 GrenadeAimPoint { get; }
        IReadOnlyList<EnemyInstance> Enemies { get; }
        IReadOnlyList<CharacterStateMachine> Fighters { get; }
        /// <summary>Vertical divider X, or NaN when the split is horizontal.</summary>
        float DividerX { get; }
        /// <summary>Horizontal divider Y, or NaN when the split is vertical / absent.</summary>
        float DividerY { get; }
        float DividerMinX { get; }
        float DividerMaxX { get; }
        float DividerMinY { get; }
        float DividerMaxY { get; }
    }
}

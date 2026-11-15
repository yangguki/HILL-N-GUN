using UnityEngine;
using HillNGun.Core;

namespace HillNGun.Proto.TopDown
{
    /// <summary>
    /// Pixel/diorama bullet draw only. Ally shots read small and yellow; enemy shots read large and red-orange.
    /// Never write these values into Projectile radius, speed, or damage.
    /// </summary>
    public static class ProtoShotLook
    {
        public static readonly Color AllyColor = new Color(1f, 0.92f, 0.28f, 1f);
        public static readonly Color EnemyColor = new Color(1f, 0.38f, 0.12f, 1f);
        public const float AllyScale = 0.11f;
        public const float EnemyScale = 0.30f;

        public static Color ColorFor(Team team) => team == Team.Player ? AllyColor : EnemyColor;

        public static float ScaleFor(Team team) => team == Team.Player ? AllyScale : EnemyScale;
    }
}

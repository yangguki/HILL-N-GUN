using System;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>5 drawn directions + mirror map for perspective 2D characters.</summary>
    public static class Proto_GreyboxPersp2DDirection
    {
        public const string South = "S";
        public const string Southeast = "SE";
        public const string East = "E";
        public const string Northeast = "NE";
        public const string North = "N";

        static readonly string[] Drawn = { South, Southeast, East, Northeast, North };

        public static string PickFromVector(Vector2 v)
        {
            if (v.sqrMagnitude < 1e-6f) return South;
            float ang = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            // 8-way buckets centered on drawn dirs (every 45°)
            int oct = Mathf.RoundToInt(ang / 45f) % 8;
            switch (oct)
            {
                case 0: return South;
                case 1: return Southeast;
                case 2: return East;
                case 3: return Northeast;
                case 4: return North;
                case 5: return Northeast;
                case 6: return East;
                case 7: return Southeast;
                default: return South;
            }
        }

        public static void Resolve(string logicalDir, out string spriteDir, out bool flipX) =>
            Resolve("hero", logicalDir, out spriteDir, out flipX);

        public static void Resolve(string who, string logicalDir, out string spriteDir, out bool flipX)
        {
            Proto_GreyboxPersp2DAnimJson.Warm(Proto_GreyboxPersp2DCatalog.LoadRuntime());
            Proto_GreyboxPersp2DAnimJson.Resolve8Way(who, logicalDir, out spriteDir, out flipX);
        }

        internal static void ResolveLegacyMirror(string logicalDir, out string spriteDir, out bool flipX)
        {
            flipX = false;
            switch (logicalDir)
            {
                case "SW":
                    spriteDir = Southeast;
                    flipX = true;
                    return;
                case "W":
                    spriteDir = East;
                    flipX = true;
                    return;
                case "NW":
                    spriteDir = Northeast;
                    flipX = true;
                    return;
            }

            spriteDir = logicalDir;
            if (Array.IndexOf(Drawn, spriteDir) < 0)
                spriteDir = South;
        }

        public static string EnemyFacePlayer(Vector2 enemyPos, Vector2 playerPos)
        {
            var d = playerPos - enemyPos;
            return PickFromVector(d);
        }
    }
}
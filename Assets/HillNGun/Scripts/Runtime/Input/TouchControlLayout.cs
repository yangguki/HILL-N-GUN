using System.Collections.Generic;
using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Input
{
    /// <summary>Resolved on-screen positions of the touch controls, in screen pixels with a bottom-left origin
    /// (the same space as Input System touch positions).</summary>
    public struct TouchLayout
    {
        public bool Valid;
        public Vector2 Screen;
        public Rect Safe;
        public float Scale;
        public bool IsTablet;

        public Rect MoveZone;
        public Vector2 MoveRest;
        public float MoveRadius, MoveKnobRadius;

        public Vector2 Attack;
        public float AttackRadius, AttackKnobRadius, AttackHitRadius;

        public Vector2 Roll, Skill, Grenade;
        public float RollRadius, SkillRadius, GrenadeRadius;
        public float ButtonHitScale;

        public float GrenadeDeadzonePx, GrenadeDragFullPx, SkillDragFullPx;
        public float TopReservedY;

        public Rect BoundsOfRightCluster()
        {
            Rect r = CircleRect(Attack, AttackRadius);
            r = Union(r, CircleRect(Roll, RollRadius));
            r = Union(r, CircleRect(Skill, SkillRadius));
            r = Union(r, CircleRect(Grenade, GrenadeRadius));
            return r;
        }

        public static Rect CircleRect(Vector2 c, float r) => new Rect(c.x - r, c.y - r, r * 2f, r * 2f);

        public static Rect Union(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
    }

    /// <summary>
    /// Pure layout maths for the touch controls (logic only, unit-tested): scales with the device, keeps every control
    /// inside the safe area and the thumb arc, and pushes the clusters away from HUD rectangles.
    /// </summary>
    public static class TouchControlLayout
    {
        public static float ComputeScale(TouchControlsData d, Vector2 screen, out bool isTablet)
        {
            float shortSide = Mathf.Max(1f, Mathf.Min(screen.x, screen.y));
            float longSide = Mathf.Max(screen.x, screen.y);
            isTablet = longSide / shortSide <= d.tabletAspectMax;
            float s = shortSide / Mathf.Max(1f, d.referenceShortSide);
            if (isTablet) s *= d.tabletScale;
            return Mathf.Clamp(s, d.minScale, d.maxScale);
        }

        /// <param name="avoid">HUD rectangles (screen px, bottom-left origin) the controls must not cover.</param>
        public static TouchLayout Compute(TouchControlsData d, Vector2 screen, Rect safeArea, IReadOnlyList<Rect> avoid = null)
        {
            var L = new TouchLayout { Valid = d != null && screen.x > 0f && screen.y > 0f, Screen = screen };
            if (!L.Valid) return L;

            float s = ComputeScale(d, screen, out L.IsTablet);
            L.Scale = s;
            float pad = d.safeAreaPadding * s;
            Rect safe = Rect.MinMaxRect(safeArea.xMin + pad, safeArea.yMin + pad, safeArea.xMax - pad, safeArea.yMax - pad);
            L.Safe = safe;
            L.TopReservedY = safe.yMax - d.topReserved * s;
            L.ButtonHitScale = d.buttonHitScale;
            L.GrenadeDeadzonePx = d.grenadeDeadzonePx * s;
            L.GrenadeDragFullPx = Mathf.Max(L.GrenadeDeadzonePx + 1f, d.grenadeDragFull * s);
            L.SkillDragFullPx = Mathf.Max(1f, d.skillDragFull * s);

            L.MoveRadius = d.moveRadius * s;
            L.MoveKnobRadius = d.moveKnobRadius * s;
            L.AttackRadius = d.attackRadius * s;
            L.AttackKnobRadius = d.attackKnobRadius * s;
            L.AttackHitRadius = L.AttackRadius * d.attackHitScale;
            L.RollRadius = d.rollRadius * s;
            L.SkillRadius = d.skillRadius * s;
            L.GrenadeRadius = d.grenadeRadius * s;

            // Right cluster: attack stick anchored to the safe-area bottom-right corner, buttons on an arc around it
            L.Attack = new Vector2(safe.xMax - d.attackOffset.x * s, safe.yMin + d.attackOffset.y * s);
            PlaceArc(ref L, d, s);
            // Never outside the safe area (very small / odd screens)
            Rect rb = L.BoundsOfRightCluster();
            Vector2 shift = Vector2.zero;
            if (rb.xMax > safe.xMax) shift.x -= rb.xMax - safe.xMax;
            if (rb.yMin < safe.yMin) shift.y += safe.yMin - rb.yMin;
            ShiftRight(ref L, shift);

            // Left: floating stick zone (left part of the safe area under the top HUD band) + idle hint position
            L.MoveZone = Rect.MinMaxRect(safe.xMin, safe.yMin, safe.xMin + safe.width * d.moveZoneWidth, L.TopReservedY);
            L.MoveRest = new Vector2(safe.xMin + d.moveRestOffset.x * s, safe.yMin + d.moveRestOffset.y * s);
            L.MoveRest = ClampCircle(L.MoveRest, L.MoveRadius, safe);

            if (avoid != null && avoid.Count > 0)
            {
                float gap = d.hudGap * s;
                // Push the right cluster up until it clears every HUD rect it touches (keep it under the top band)
                for (int iter = 0; iter < 4; iter++)
                {
                    float need = 0f;
                    foreach (var a in avoid)
                    {
                        need = Mathf.Max(need, RaiseToClear(L.Attack, L.AttackRadius, a, gap));
                        need = Mathf.Max(need, RaiseToClear(L.Roll, L.RollRadius, a, gap));
                        need = Mathf.Max(need, RaiseToClear(L.Skill, L.SkillRadius, a, gap));
                        need = Mathf.Max(need, RaiseToClear(L.Grenade, L.GrenadeRadius, a, gap));
                    }
                    if (need <= 0f) break;
                    float room = L.TopReservedY - L.BoundsOfRightCluster().yMax;
                    if (room <= 0f) break;
                    ShiftRight(ref L, new Vector2(0f, Mathf.Min(need, room)));
                }
                for (int iter = 0; iter < 4; iter++)
                {
                    float need = 0f;
                    foreach (var a in avoid) need = Mathf.Max(need, RaiseToClear(L.MoveRest, L.MoveRadius, a, gap));
                    if (need <= 0f) break;
                    L.MoveRest.y = Mathf.Min(L.MoveRest.y + need, L.TopReservedY - L.MoveRadius);
                }
            }
            return L;
        }

        private static void PlaceArc(ref TouchLayout L, TouchControlsData d, float s)
        {
            float r = d.arcRadius * s;
            L.Roll = L.Attack + Dir(d.rollAngle) * r;
            L.Skill = L.Attack + Dir(d.skillAngle) * r;
            L.Grenade = L.Attack + Dir(d.grenadeAngle) * r;
        }

        private static void ShiftRight(ref TouchLayout L, Vector2 delta)
        {
            L.Attack += delta; L.Roll += delta; L.Skill += delta; L.Grenade += delta;
        }

        private static Vector2 Dir(float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));

        public static Vector2 ClampCircle(Vector2 c, float r, Rect area)
        {
            c.x = area.width >= 2f * r ? Mathf.Clamp(c.x, area.xMin + r, area.xMax - r) : area.center.x;
            c.y = area.height >= 2f * r ? Mathf.Clamp(c.y, area.yMin + r, area.yMax - r) : area.center.y;
            return c;
        }

        public static bool CircleOverlapsRect(Vector2 c, float r, Rect a, float gap = 0f)
        {
            float dx = Mathf.Max(a.xMin - gap - c.x, 0f, c.x - (a.xMax + gap));
            float dy = Mathf.Max(a.yMin - gap - c.y, 0f, c.y - (a.yMax + gap));
            return dx * dx + dy * dy < r * r;
        }

        /// <summary>How far up a circle must move to clear the rect (0 when it already clears it).</summary>
        private static float RaiseToClear(Vector2 c, float r, Rect a, float gap)
        {
            if (!CircleOverlapsRect(c, r, a, gap)) return 0f;
            return (a.yMax + gap + r) - c.y;
        }

        /// <summary>Which control a touch-down at p belongs to.</summary>
        public enum Hit { None, Move, Attack, Roll, Skill, Grenade }

        public static Hit HitTest(in TouchLayout L, Vector2 p)
        {
            if (!L.Valid) return Hit.None;
            // Small buttons first (they sit next to the big stick); nearest wins when hit circles overlap
            Hit best = Hit.None;
            float bestD = float.MaxValue;
            Try(Hit.Grenade, L.Grenade, L.GrenadeRadius * L.ButtonHitScale);
            Try(Hit.Skill, L.Skill, L.SkillRadius * L.ButtonHitScale);
            Try(Hit.Roll, L.Roll, L.RollRadius * L.ButtonHitScale);
            if (best != Hit.None) return best;
            if ((p - L.Attack).sqrMagnitude <= L.AttackHitRadius * L.AttackHitRadius) return Hit.Attack;
            if (L.MoveZone.Contains(p)) return Hit.Move;
            return Hit.None;

            void Try(Hit h, Vector2 c, float r)
            {
                float dd = (p - c).sqrMagnitude;
                if (dd <= r * r && dd < bestD) { bestD = dd; best = h; }
            }
        }
    }
}

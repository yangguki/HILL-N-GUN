using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Battlefield
{
    /// <summary>
    /// Pure 2D swept circle vs. walk polygon (inside) + axis-aligned obstacle rects (outside). Moves in substeps of at
    /// most half the radius and pushes out along the contact normal after each step, so motion slides along walls and
    /// covers, cannot tunnel through thin rects and never gets stuck in a corner (iterated push-out). Spawn / teleport
    /// positions go through <see cref="Depenetrate"/> (deterministic spiral search for the nearest free spot).
    /// </summary>
    public static class FreeMoveCollision
    {
        public const int MaxSubsteps = 64;
        public const int Iterations = 6;
        private const float Eps = 1e-4f;

        public static Vector2 Move(Vector2 from, Vector2 delta, float r, IList<Vector2> poly, IList<Rect> rects)
        {
            r = Mathf.Max(0.01f, r);
            Vector2 p = Resolve(from, r, poly, rects);
            float len = delta.magnitude;
            if (len < 1e-6f) return p;
            int n = Mathf.Clamp(Mathf.CeilToInt(len / (0.5f * r)), 1, MaxSubsteps);
            Vector2 step = delta / n;
            for (int i = 0; i < n; i++) p = Resolve(p + step, r, poly, rects);
            return p;
        }

        public static Vector2 Resolve(Vector2 p, float r, IList<Vector2> poly, IList<Rect> rects)
        {
            for (int it = 0; it < Iterations; it++)
            {
                bool moved = false;
                if (rects != null)
                    for (int i = 0; i < rects.Count; i++)
                        moved |= PushOutOfRect(ref p, r, rects[i]);
                if (poly != null && poly.Count >= 3)
                    moved |= KeepInsidePolygon(ref p, r, poly);
                if (!moved) break;
            }
            return p;
        }

        public static bool PushOutOfRect(ref Vector2 p, float r, Rect rc)
        {
            float qx = Mathf.Clamp(p.x, rc.xMin, rc.xMax), qy = Mathf.Clamp(p.y, rc.yMin, rc.yMax);
            float dx = p.x - qx, dy = p.y - qy;
            float d2 = dx * dx + dy * dy;
            if (d2 >= r * r - Eps) return false;
            if (d2 > 1e-10f)
            {
                float d = Mathf.Sqrt(d2);
                p = new Vector2(qx, qy) + new Vector2(dx, dy) / d * (r + Eps);
                return true;
            }
            // centre inside the rect: leave through the nearest side
            float l = p.x - rc.xMin, rr = rc.xMax - p.x, b = p.y - rc.yMin, t = rc.yMax - p.y;
            float m = Mathf.Min(Mathf.Min(l, rr), Mathf.Min(b, t));
            if (m == b) p.y = rc.yMin - r - Eps;
            else if (m == t) p.y = rc.yMax + r + Eps;
            else if (m == l) p.x = rc.xMin - r - Eps;
            else p.x = rc.xMax + r + Eps;
            return true;
        }

        public static bool KeepInsidePolygon(ref Vector2 p, float r, IList<Vector2> poly)
        {
            bool inside = PointInPolygon(p, poly);
            float bestD2 = float.MaxValue; Vector2 bestQ = p; int bestEdge = -1;
            bool moved = false;
            int n = poly.Count;
            if (!inside)
            {
                for (int i = 0; i < n; i++)
                {
                    Vector2 q = ClosestOnSegment(p, poly[i], poly[(i + 1) % n]);
                    float d2 = (p - q).sqrMagnitude;
                    if (d2 < bestD2) { bestD2 = d2; bestQ = q; bestEdge = i; }
                }
                p = bestQ + InwardNormal(poly, bestEdge) * (r + Eps);
                return true;
            }
            for (int i = 0; i < n; i++)
            {
                Vector2 q = ClosestOnSegment(p, poly[i], poly[(i + 1) % n]);
                Vector2 v = p - q;
                float d2 = v.sqrMagnitude;
                if (d2 >= r * r - Eps) continue;
                Vector2 nrm = d2 > 1e-10f ? v / Mathf.Sqrt(d2) : InwardNormal(poly, i);
                p = q + nrm * (r + Eps);
                moved = true;
            }
            return moved;
        }

        public static Vector2 InwardNormal(IList<Vector2> poly, int edge)
        {
            Vector2 a = poly[edge], b = poly[(edge + 1) % poly.Count];
            Vector2 d = (b - a).normalized;
            Vector2 left = new Vector2(-d.y, d.x);
            return SignedArea(poly) >= 0f ? left : -left; // CCW: interior is on the left
        }

        public static float SignedArea(IList<Vector2> poly)
        {
            float a = 0f;
            for (int i = 0, n = poly.Count; i < n; i++)
            {
                Vector2 p = poly[i], q = poly[(i + 1) % n];
                a += p.x * q.y - q.x * p.y;
            }
            return a * 0.5f;
        }

        public static Vector2 ClosestOnSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 < 1e-12f) return a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            return a + ab * t;
        }

        public static bool PointInPolygon(Vector2 p, IList<Vector2> poly)
        {
            bool c = false;
            for (int i = 0, n = poly.Count, j = n - 1; i < n; j = i++)
            {
                Vector2 a = poly[i], b = poly[j];
                if (((a.y > p.y) != (b.y > p.y)) && (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)) c = !c;
            }
            return c;
        }

        /// <summary>Circle fully inside the polygon and outside every rect.</summary>
        public static bool IsFree(Vector2 p, float r, IList<Vector2> poly, IList<Rect> rects, float slack = 0.002f)
        {
            float rr = Mathf.Max(0f, r - slack);
            if (poly != null && poly.Count >= 3)
            {
                if (!PointInPolygon(p, poly)) return false;
                for (int i = 0, n = poly.Count; i < n; i++)
                    if ((p - ClosestOnSegment(p, poly[i], poly[(i + 1) % n])).sqrMagnitude < rr * rr) return false;
            }
            if (rects != null)
                for (int i = 0; i < rects.Count; i++)
                {
                    var rc = rects[i];
                    float qx = Mathf.Clamp(p.x, rc.xMin, rc.xMax), qy = Mathf.Clamp(p.y, rc.yMin, rc.yMax);
                    float dx = p.x - qx, dy = p.y - qy;
                    if (dx * dx + dy * dy < rr * rr) return false;
                }
            return true;
        }

        /// <summary>Nearest free position (push-out first, then a deterministic spiral search). Never returns an overlap if any free spot exists within 8 tiles.</summary>
        public static Vector2 Depenetrate(Vector2 p, float r, IList<Vector2> poly, IList<Rect> rects)
        {
            Vector2 q = Resolve(p, r, poly, rects);
            if (IsFree(q, r, poly, rects)) return q;
            for (int ring = 1; ring <= 40; ring++)
            {
                float rad = ring * 0.2f;
                int steps = 8 + ring * 2;
                for (int k = 0; k < steps; k++)
                {
                    float a = k * Mathf.PI * 2f / steps;
                    Vector2 c = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                    if (IsFree(c, r, poly, rects)) return c;
                }
            }
            return q;
        }

        /// <summary>Horizontal span of the polygon at height y (first/last crossing). False if y is outside.</summary>
        public static bool SpanAtY(IList<Vector2> poly, float y, out float minX, out float maxX)
        {
            minX = float.MaxValue; maxX = float.MinValue;
            for (int i = 0, n = poly.Count; i < n; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % n];
                if ((a.y > y) == (b.y > y)) continue;
                float x = a.x + (y - a.y) / (b.y - a.y) * (b.x - a.x);
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
            return maxX > minX;
        }
    }
}

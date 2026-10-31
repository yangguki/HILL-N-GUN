using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Scenery
{
    /// <summary>Tiny 2D vertex-coloured mesh builder for the backdrop bake (z = 0, cull off shader).</summary>
    public sealed class Geo
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<Color> C = new List<Color>();
        public readonly List<Vector2> U = new List<Vector2>();
        public readonly List<int> T = new List<int>();
        /// <summary>World -> UV scale for textured (ground grain) geometry; zero = constant UV.</summary>
        public Vector2 UvScale;

        public int Count => V.Count;

        public int P(Vector2 p, Color c)
        {
            V.Add(new Vector3(p.x, p.y, 0f));
            C.Add(c);
            U.Add(UvScale == Vector2.zero ? new Vector2(0.5f, 0.5f) : new Vector2(p.x * UvScale.x, p.y * UvScale.y));
            return V.Count - 1;
        }

        public void Tri(Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
        {
            int i = P(a, ca), j = P(b, cb), k = P(c, cc);
            T.Add(i); T.Add(j); T.Add(k);
        }
        public void Tri(Vector2 a, Vector2 b, Vector2 c, Color col) => Tri(a, b, c, col, col, col);

        /// <summary>Quad bl, br, tr, tl.</summary>
        public void Quad(Vector2 bl, Vector2 br, Vector2 tr, Vector2 tl, Color cbl, Color cbr, Color ctr, Color ctl)
        {
            int a = P(bl, cbl), b = P(br, cbr), c = P(tr, ctr), d = P(tl, ctl);
            T.Add(a); T.Add(c); T.Add(b);
            T.Add(a); T.Add(d); T.Add(c);
        }
        public void Quad(Vector2 bl, Vector2 br, Vector2 tr, Vector2 tl, Color col) => Quad(bl, br, tr, tl, col, col, col, col);
        public void QuadV(Vector2 bl, Vector2 br, Vector2 tr, Vector2 tl, Color bottom, Color top) => Quad(bl, br, tr, tl, bottom, bottom, top, top);

        public void Rect(float x0, float y0, float x1, float y1, Color col)
            => Quad(new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1), col);
        public void RectV(float x0, float y0, float x1, float y1, Color bottom, Color top)
            => QuadV(new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1), bottom, top);

        /// <summary>Convex polygon (fan).</summary>
        public void Poly(IList<Vector2> pts, Color col)
        {
            if (pts.Count < 3) return;
            int b = P(pts[0], col);
            int prev = P(pts[1], col);
            for (int i = 2; i < pts.Count; i++)
            {
                int cur = P(pts[i], col);
                T.Add(b); T.Add(prev); T.Add(cur);
                prev = cur;
            }
        }

        public void Ellipse(Vector2 c, float rx, float ry, Color col, int seg = 18, float rot = 0f)
        {
            int ci = P(c, col);
            int first = -1, prev = -1;
            float cr = Mathf.Cos(rot), sr = Mathf.Sin(rot);
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var d = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
                var p = c + new Vector2(d.x * cr - d.y * sr, d.x * sr + d.y * cr);
                int cur = P(p, col);
                if (prev >= 0) { T.Add(ci); T.Add(prev); T.Add(cur); }
                else first = cur;
                prev = cur;
            }
        }

        /// <summary>Radial soft blob: centre colour fading to fully transparent at the rim.</summary>
        public void SoftEllipse(Vector2 c, float rx, float ry, Color center, int seg = 20, float hardness = 0f)
        {
            var edge = new Color(center.r, center.g, center.b, 0f);
            if (hardness > 0f)
            {
                // inner solid ellipse + soft ring
                Ellipse(c, rx * hardness, ry * hardness, center, seg);
                for (int i = 0; i < seg; i++)
                {
                    float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                    var i0 = c + new Vector2(Mathf.Cos(a0) * rx * hardness, Mathf.Sin(a0) * ry * hardness);
                    var i1 = c + new Vector2(Mathf.Cos(a1) * rx * hardness, Mathf.Sin(a1) * ry * hardness);
                    var o0 = c + new Vector2(Mathf.Cos(a0) * rx, Mathf.Sin(a0) * ry);
                    var o1 = c + new Vector2(Mathf.Cos(a1) * rx, Mathf.Sin(a1) * ry);
                    Quad(i0, i1, o1, o0, center, center, edge, edge);
                }
                return;
            }
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                Tri(c, c + new Vector2(Mathf.Cos(a0) * rx, Mathf.Sin(a0) * ry), c + new Vector2(Mathf.Cos(a1) * rx, Mathf.Sin(a1) * ry), center, edge, edge);
            }
        }

        public void Line(Vector2 a, Vector2 b, float w, Color col)
        {
            var d = (b - a);
            if (d.sqrMagnitude < 1e-8f) return;
            var n = new Vector2(-d.y, d.x).normalized * (w * 0.5f);
            Quad(a - n, b - n, b + n, a + n, col);
        }

        /// <summary>Line with soft (transparent) long edges.</summary>
        public void SoftLine(Vector2 a, Vector2 b, float w, Color col)
        {
            var d = (b - a);
            if (d.sqrMagnitude < 1e-8f) return;
            var n = new Vector2(-d.y, d.x).normalized * (w * 0.5f);
            var e = new Color(col.r, col.g, col.b, 0f);
            Quad(a, b, b + n, a + n, col, col, e, e);
            Quad(a - n, b - n, b, a, e, e, col, col);
        }

        public void SoftPolyline(IList<Vector2> pts, float w, Color col)
        {
            for (int i = 0; i + 1 < pts.Count; i++) SoftLine(pts[i], pts[i + 1], w, col);
        }

        public Mesh Build(string name)
        {
            var m = new Mesh { name = name };
            if (V.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(V);
            m.SetColors(C);
            m.SetUVs(0, U);
            m.SetTriangles(T, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}

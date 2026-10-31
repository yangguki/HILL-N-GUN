using System.Collections.Generic;
using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Scenery
{
    public static partial class BackdropBaker
    {
        private sealed class Ctx
        {
            public StageThemeData t;
            public FieldMap m;
            public Rect r, view;
            public System.Random rnd;
            public float hy;
            public Vector2 W(float lx, float ly) => m.W(lx, ly);
            public float S(float ly) => m.S(ly);
            /// <summary>Field edge x at ly (side -1 = left, +1 = right).</summary>
            public Vector2 E(int side, float ly) => m.W(side < 0 ? 0f : m.sizeX, ly);
            public bool RailGap(float ly, float pad = 0f) => t.road == ThemeRoadKind.Railway && ly > 7.6f - pad && ly < 10.4f + pad;
        }

        private struct Item
        {
            public float ly;
            public System.Action<Job> draw;
            public Item(float ly, System.Action<Job> d) { this.ly = ly; draw = d; }
        }

        private static void ComposeField(Job job, StageThemeData t, FieldMap m, Rect r, Rect view)
        {
            var c = new Ctx { t = t, m = m, r = r, view = view, rnd = new System.Random(t.seed * 7919 + 17), hy = m.Horizon };
            Sky(job.G, c, c.hy, r.yMax);
            Far(job.G, c);
            job.Flush();
            Ground(job, c);
            GroundMarks(job.G, c);
            SideGround(job.G, c);
            job.Flush();

            var items = new List<Item>();
            BuildItems(c, items);
            items.Sort((a, b) => b.ly.CompareTo(a.ly)); // far (top) first
            foreach (var it in items) it.draw(job);
            job.Flush();
            Vignette(job.G, c.r, c.view, t);
        }

        // ------------------------------------------------------------------ sky / horizon

        private static void Sky(Geo g, Ctx c, float horizon, float top)
        {
            var t = c.t;
            float k = Mathf.Clamp01((top - horizon) / 2.4f);
            g.RectV(c.r.xMin, horizon - 0.4f, c.r.xMax, top, t.skyHorizon, Mix(t.skyHorizon, t.skyTop, k));
            var sp = new Vector2(c.view.xMin + t.sunViewport.x * c.view.width, c.view.yMin + t.sunViewport.y * c.view.height);
            g.SoftEllipse(sp, t.sunRadius * 3.4f, t.sunRadius * 2.2f, A(t.sunColor, t.sunGlow), 28);
            g.SoftEllipse(sp, t.sunRadius * 1.15f, t.sunRadius * 1.15f, A(t.sunColor, 0.85f), 32, 0.86f);
            if (t.clouds)
                for (int i = 0; i < 5; i++)
                {
                    var cc = new Vector2(Rf(c.rnd, c.view.xMin, c.view.xMax), horizon + Rf(c.rnd, 0.55f, 1.25f));
                    float w = Rf(c.rnd, 1.2f, 2.6f);
                    for (int j = 0; j < 3; j++)
                        g.SoftEllipse(cc + new Vector2(Rf(c.rnd, -w, w) * 0.5f, Rf(c.rnd, -0.06f, 0.08f)), w * Rf(c.rnd, 0.45f, 0.7f), Rf(c.rnd, 0.12f, 0.2f), new Color(1f, 0.98f, 0.94f, 0.55f), 16, 0.4f);
                }
        }

        private static void Far(Geo g, Ctx c)
        {
            var t = c.t;
            float b = c.hy - 0.08f;
            float x0 = c.r.xMin - 1f, x1 = c.r.xMax + 1f;
            switch (t.far)
            {
                case ThemeFarKind.Mesas:
                    MesaRow(g, c, b, x0, x1, t.farColor2, 0.55f, 1.15f, 3f, 7f);
                    MesaRow(g, c, b, x0, x1, t.farColor, 0.3f, 0.75f, 2f, 5f);
                    break;
                case ThemeFarKind.StationYard:
                    MesaRow(g, c, b, x0, x1, t.farColor2, 0.4f, 0.9f, 3f, 7f);
                    SkylineRow(g, c, b, -6f, 6f, t.farColor, 0.25f, 0.55f);
                    WaterTowerSil(g, new Vector2(-8.5f, b), 1.05f, t.farColor);
                    for (float x = 4f; x < x1; x += 2.6f) { g.Line(new Vector2(x, b), new Vector2(x, b + 0.75f), 0.05f, t.farColor); g.Line(new Vector2(x - 0.2f, b + 0.68f), new Vector2(x + 0.2f, b + 0.68f), 0.04f, t.farColor); }
                    break;
                case ThemeFarKind.SunsetTown:
                    MesaRow(g, c, b, x0, x1, t.farColor2, 0.3f, 0.8f, 3f, 7f);
                    SkylineRow(g, c, b, x0, x1, t.farColor, 0.45f, 1.1f);
                    WindmillSil(g, new Vector2(-10.5f, b), 1.6f, t.farColor);
                    WaterTowerSil(g, new Vector2(9.8f, b), 1.4f, t.farColor);
                    SteepleSil(g, new Vector2(3.2f, b), 1.5f, t.farColor);
                    break;
                default: // TownSkyline
                    MesaRow(g, c, b, x0, x1, t.farColor2, 0.35f, 0.85f, 3f, 7f);
                    SkylineRow(g, c, b, -9f, 9f, t.farColor, 0.3f, 0.75f);
                    WindmillSil(g, new Vector2(-10.8f, b), 1.35f, t.farColor);
                    WaterTowerSil(g, new Vector2(10.6f, b), 1.15f, t.farColor);
                    break;
            }
            // atmospheric haze over the horizon line
            g.RectV(c.r.xMin, b - 0.25f, c.r.xMax, b + 0.9f, t.hazeColor, A(t.hazeColor, 0f));
        }

        private static void MesaRow(Geo g, Ctx c, float b, float x0, float x1, Color col, float hMin, float hMax, float wMin, float wMax)
        {
            float x = x0 - Rf(c.rnd, 0f, 2f);
            while (x < x1)
            {
                float w = Rf(c.rnd, wMin, wMax), h = Rf(c.rnd, hMin, hMax), slope = Rf(c.rnd, 0.25f, 0.6f);
                g.Quad(new Vector2(x, b), new Vector2(x + w, b), new Vector2(x + w - slope, b + h), new Vector2(x + slope, b + h), col);
                // lit top band
                g.Quad(new Vector2(x + slope * 0.85f, b + h * 0.86f), new Vector2(x + w - slope * 0.85f, b + h * 0.86f), new Vector2(x + w - slope, b + h), new Vector2(x + slope, b + h), Mix(col, Color.white, 0.12f));
                x += w + Rf(c.rnd, -1f, 1.5f);
            }
        }

        private static void SkylineRow(Geo g, Ctx c, float b, float x0, float x1, Color col, float hMin, float hMax)
        {
            float x = x0;
            while (x < x1)
            {
                float w = Rf(c.rnd, 0.55f, 1.3f), h = Rf(c.rnd, hMin, hMax);
                g.Rect(x, b, x + w, b + h, col);
                if (c.rnd.NextDouble() < 0.5) g.Rect(x + w * 0.2f, b + h, x + w * 0.8f, b + h + 0.08f, col); // false front step
                if (c.t.litWindows > 0f && c.rnd.NextDouble() < c.t.litWindows)
                    g.Rect(x + w * 0.35f, b + h * 0.45f, x + w * 0.5f, b + h * 0.62f, A(c.t.windowLit, 0.85f));
                x += w + Rf(c.rnd, 0f, 0.35f);
            }
        }

        private static void WaterTowerSil(Geo g, Vector2 b, float h, Color col)
        {
            float w = h * 0.5f;
            g.Line(b + new Vector2(-w * 0.45f, 0f), b + new Vector2(-w * 0.3f, h * 0.55f), h * 0.035f, col);
            g.Line(b + new Vector2(w * 0.45f, 0f), b + new Vector2(w * 0.3f, h * 0.55f), h * 0.035f, col);
            g.Line(b + new Vector2(-w * 0.38f, h * 0.25f), b + new Vector2(w * 0.38f, h * 0.3f), h * 0.02f, col);
            g.Rect(b.x - w * 0.42f, b.y + h * 0.55f, b.x + w * 0.42f, b.y + h * 0.88f, col);
            g.Tri(b + new Vector2(-w * 0.5f, h * 0.88f), b + new Vector2(w * 0.5f, h * 0.88f), b + new Vector2(0f, h), col);
        }

        private static void WindmillSil(Geo g, Vector2 b, float h, Color col)
        {
            g.Quad(b + new Vector2(-h * 0.14f, 0f), b + new Vector2(h * 0.14f, 0f), b + new Vector2(h * 0.03f, h * 0.8f), b + new Vector2(-h * 0.03f, h * 0.8f), col);
            var hub = b + new Vector2(0f, h * 0.82f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var n = new Vector2(-d.y, d.x) * h * 0.035f;
                g.Quad(hub + d * h * 0.05f - n * 0.3f, hub + d * h * 0.05f + n * 0.3f, hub + d * h * 0.3f + n, hub + d * h * 0.3f - n, col);
            }
            g.Quad(hub + new Vector2(0.02f, -0.02f), hub + new Vector2(h * 0.35f, -0.03f), hub + new Vector2(h * 0.38f, h * 0.08f), hub + new Vector2(0.02f, 0.02f), col);
        }

        private static void SteepleSil(Geo g, Vector2 b, float h, Color col)
        {
            g.Rect(b.x - h * 0.28f, b.y, b.x + h * 0.28f, b.y + h * 0.45f, col);
            g.Rect(b.x - h * 0.1f, b.y + h * 0.45f, b.x + h * 0.1f, b.y + h * 0.72f, col);
            g.Tri(b + new Vector2(-h * 0.13f, h * 0.72f), b + new Vector2(h * 0.13f, h * 0.72f), b + new Vector2(0f, h), col);
        }

        // ------------------------------------------------------------------ ground

        private static Color GroundColor(Ctx c, float wx, float wy)
        {
            var t = c.t;
            var lp = c.m.ToLogic(new Vector2(wx, wy));
            float lx = lp.x, ly = lp.y;
            float tFar = Mathf.InverseLerp(-2f, c.m.sizeY + 0.5f, ly);
            Color col = Mix(t.sand, t.sandFar, tFar);
            float road = 0f;
            switch (t.road)
            {
                case ThemeRoadKind.MainStreet:
                    road = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.5f, 10.5f, Mathf.Abs(lx - c.m.sizeX * 0.5f)));
                    break;
                case ThemeRoadKind.CanyonTrail:
                    road = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.2f, 3.4f, Mathf.Abs(lx - TrailX(c, ly))));
                    break;
                case ThemeRoadKind.Railway:
                    road = 0.25f;
                    break;
            }
            col = Mix(col, t.dirt, road * 0.55f);
            float outside = lx < 0f ? -lx : lx > c.m.sizeX ? lx - c.m.sizeX : 0f;
            col = Mix(col, t.outerGround, Mathf.Clamp01(outside / 2.5f));
            float n1 = Mathf.PerlinNoise(wx * 0.21f + t.seed * 3.1f, wy * 0.33f + t.seed) - 0.5f;
            float n2 = Mathf.PerlinNoise(wx * 0.85f + 40f, wy * 1.2f + t.seed * 7f) - 0.5f;
            float k = 1f + (n1 * 0.24f + n2 * 0.09f) * t.blotchStrength * 2f;
            k *= Mathf.Lerp(0.95f, 1.02f, tFar); // slightly darker foreground
            col = new Color(col.r * k, col.g * k, col.b * k, 1f);
            // compensate the grain texture mean (~0.92) so the theme colours read as authored
            return new Color(Mathf.Min(1f, col.r / 0.92f), Mathf.Min(1f, col.g / 0.92f), Mathf.Min(1f, col.b / 0.92f), 1f);
        }

        private static float TrailX(Ctx c, float ly) => c.m.sizeX * 0.5f + 4.2f * Mathf.Sin(ly * 0.26f + 0.8f);

        private static void Ground(Job job, Ctx c)
        {
            var g = new Geo { UvScale = new Vector2(0.22f, 0.22f) };
            const float step = 0.5f;
            float top = c.hy + 0.05f;
            for (float y = c.r.yMin; y < top; y += step)
            {
                float y2 = Mathf.Min(top, y + step);
                for (float x = c.r.xMin; x < c.r.xMax; x += step)
                {
                    float x2 = x + step;
                    g.Quad(new Vector2(x, y), new Vector2(x2, y), new Vector2(x2, y2), new Vector2(x, y2),
                        GroundColor(c, x, y), GroundColor(c, x2, y), GroundColor(c, x2, y2), GroundColor(c, x, y2));
                }
            }
            job.AddGrain(g);
        }

        private static void GroundMarks(Geo g, Ctx c)
        {
            var t = c.t;
            var rnd = c.rnd;
            // soft dirt / pale sand patches
            for (int i = 0; i < 22; i++)
            {
                float lx = Rf(rnd, -2f, c.m.sizeX + 2f), ly = Rf(rnd, -0.5f, c.m.sizeY);
                float s = c.S(ly);
                var col = rnd.NextDouble() < 0.5 ? A(t.dirt, Rf(rnd, 0.12f, 0.2f)) : A(t.sandFar, Rf(rnd, 0.14f, 0.22f));
                g.SoftEllipse(c.W(lx, ly), Rf(rnd, 1f, 2.6f) * s, Rf(rnd, 0.3f, 0.7f) * s, col, 18, 0.25f);
            }

            switch (t.road)
            {
                case ThemeRoadKind.MainStreet:
                    foreach (float rx in new[] { 7.4f, 8.18f, 15.82f, 16.6f }) Rut(g, c, ly => rx + 0.14f * Mathf.Sin(ly * 0.7f + rx), -1.5f, 18.4f);
                    break;
                case ThemeRoadKind.CanyonTrail:
                    Rut(g, c, ly => TrailX(c, ly) - 0.4f, -1.5f, 18.4f);
                    Rut(g, c, ly => TrailX(c, ly) + 0.4f, -1.5f, 18.4f);
                    break;
                case ThemeRoadKind.Railway:
                    Rut(g, c, ly => 4.3f, -1.5f, 7.7f); Rut(g, c, ly => 5.05f, -1.5f, 7.7f);
                    Rut(g, c, ly => 18.9f, 10.3f, 18.4f); Rut(g, c, ly => 19.65f, 10.3f, 18.4f);
                    Railway(g, c);
                    break;
            }

            // faint row guides (fixed rows stay readable)
            if (t.rowGuideAlpha > 0.001f)
            {
                var col = A(Shade(t.rut, 0.9f), t.rowGuideAlpha);
                void Row(float y)
                {
                    for (float a = 0.6f; a < c.m.sizeX - 0.6f; a += 2.0f)
                        g.SoftLine(c.W(a, y), c.W(Mathf.Min(a + 1.1f, c.m.sizeX - 0.6f), y), 0.07f, col);
                }
                if (c.m.ourRows != null) foreach (var y in c.m.ourRows) Row(y);
                if (c.m.enemyRows != null) foreach (var y in c.m.enemyRows) Row(y);
            }

            // gravel: small + low contrast inside the field, bigger outside
            for (int i = 0; i < t.gravelCount; i++)
            {
                var w = new Vector2(Rf(rnd, c.r.xMin, c.r.xMax), Rf(rnd, c.r.yMin, c.hy - 0.15f));
                var lp = c.m.ToLogic(w);
                bool inField = lp.x > 0f && lp.x < c.m.sizeX && lp.y > 0f && lp.y < c.m.sizeY;
                float s = c.S(lp.y);
                float size = (inField ? Rf(rnd, 0.025f, 0.055f) : Rf(rnd, 0.04f, 0.1f)) * s;
                var col = Shade(Mix(t.gravel, t.sandFar, Rf(rnd, 0f, 0.45f)), Rf(rnd, 0.85f, 1.08f));
                float alpha = inField ? 0.7f : 1f;
                g.Ellipse(w + new Vector2(size * 0.25f, -size * 0.35f), size * 1.05f, size * 0.6f, A(t.groundShadow, t.groundShadow.a * 0.7f * alpha), 6);
                g.Ellipse(w, size, size * 0.7f, A(col, alpha), 6, Rf(rnd, 0f, 3f));
                if (size > 0.06f) g.Ellipse(w + new Vector2(-size * 0.25f, size * 0.2f), size * 0.4f, size * 0.25f, A(Mix(col, Color.white, 0.35f), alpha), 5);
            }

            // dry grass tufts: outside the field or hugging its edges (never in the lanes)
            for (int i = 0; i < t.grassTufts; i++)
            {
                float lx, ly;
                if (rnd.NextDouble() < 0.55)
                {
                    bool left = rnd.NextDouble() < 0.5;
                    lx = left ? Rf(rnd, -6f, -0.2f) : Rf(rnd, c.m.sizeX + 0.2f, c.m.sizeX + 6f);
                    ly = Rf(rnd, -1.2f, c.m.sizeY + 0.8f);
                }
                else
                {
                    if (rnd.NextDouble() < 0.5) { lx = rnd.NextDouble() < 0.5 ? Rf(rnd, 0.05f, 0.6f) : Rf(rnd, c.m.sizeX - 0.6f, c.m.sizeX - 0.05f); ly = Rf(rnd, 0f, c.m.sizeY); }
                    else { lx = Rf(rnd, 0f, c.m.sizeX); ly = rnd.NextDouble() < 0.5 ? Rf(rnd, -1.2f, 0.4f) : Rf(rnd, c.m.sizeY - 0.4f, c.m.sizeY + 0.6f); }
                }
                if (c.RailGap(ly, 0.3f)) continue;
                Tuft(g, c.W(lx, ly), c.S(ly) * Rf(rnd, 0.8f, 1.3f), t.grass, rnd, t.groundShadow);
            }
        }

        private static void Rut(Geo g, Ctx c, System.Func<float, float> xAt, float ly0, float ly1)
        {
            var pts = new List<Vector2>();
            var ridge = new List<Vector2>();
            for (float ly = ly0; ly <= ly1 + 0.001f; ly += 0.5f)
            {
                if (c.RailGap(ly)) { Flush(); continue; }
                pts.Add(c.W(xAt(ly), ly));
                ridge.Add(c.W(xAt(ly) + 0.2f, ly));
            }
            Flush();
            void Flush()
            {
                if (pts.Count > 1)
                {
                    g.SoftPolyline(pts, 0.17f, A(c.t.rut, 0.5f));
                    g.SoftPolyline(ridge, 0.09f, A(Mix(c.t.sandFar, Color.white, 0.2f), 0.3f));
                }
                pts.Clear(); ridge.Clear();
            }
        }

        private static void Railway(Geo g, Ctx c)
        {
            var t = c.t;
            float a = -16f, b = c.m.sizeX + 16f;
            // ballast bed with soft fringes
            var core = A(t.gravel, 0.9f);
            g.Quad(c.W(a, 8.2f), c.W(b, 8.2f), c.W(b, 9.8f), c.W(a, 9.8f), core);
            g.Quad(c.W(a, 7.75f), c.W(b, 7.75f), c.W(b, 8.2f), c.W(a, 8.2f), A(t.gravel, 0f), A(t.gravel, 0f), core, core);
            g.Quad(c.W(a, 9.8f), c.W(b, 9.8f), c.W(b, 10.25f), c.W(a, 10.25f), core, core, A(t.gravel, 0f), A(t.gravel, 0f));
            for (int i = 0; i < 420; i++)
            {
                var w = c.W(Rf(c.rnd, a, b), Rf(c.rnd, 7.9f, 10.1f));
                float sz = Rf(c.rnd, 0.025f, 0.05f);
                g.Ellipse(w, sz, sz * 0.7f, Shade(t.gravel, Rf(c.rnd, 0.75f, 1.2f)), 5);
            }
            var tie = Shade(Mix(t.roof, t.wallB, 0.25f), 1.05f);
            for (float x = a; x < b; x += 0.62f)
            {
                g.Quad(c.W(x - 0.13f, 8.22f), c.W(x + 0.19f, 8.22f), c.W(x + 0.19f, 9.7f), c.W(x - 0.13f, 9.7f), A(t.groundShadow, t.groundShadow.a * 1.2f));
                g.Quad(c.W(x - 0.16f, 8.28f), c.W(x + 0.16f, 8.28f), c.W(x + 0.16f, 9.76f), c.W(x - 0.16f, 9.76f), tie, tie, Shade(tie, 1.15f), Shade(tie, 1.15f));
            }
            var steel = new Color(0.36f, 0.35f, 0.36f);
            foreach (float ry in new[] { 8.62f, 9.38f })
            {
                g.Line(c.W(a, ry - 0.06f), c.W(b, ry - 0.06f), 0.06f, A(t.groundShadow, 0.5f));
                g.Line(c.W(a, ry), c.W(b, ry), 0.11f, steel);
                g.Line(c.W(a, ry + 0.035f), c.W(b, ry + 0.035f), 0.035f, new Color(0.78f, 0.76f, 0.74f));
            }
        }

        private static void Tuft(Geo g, Vector2 p, float s, Color grass, System.Random rnd, Color shadow)
        {
            g.Ellipse(p + new Vector2(0.03f * s, -0.02f * s), 0.16f * s, 0.05f * s, A(shadow, shadow.a * 0.8f), 8);
            int blades = 5 + rnd.Next(3);
            for (int i = 0; i < blades; i++)
            {
                float ang = Rf(rnd, -0.65f, 0.65f), h = Rf(rnd, 0.14f, 0.3f) * s;
                var dir = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang));
                var basep = p + new Vector2(Rf(rnd, -0.06f, 0.06f) * s, 0f);
                var col = Shade(Mix(grass, new Color(0.86f, 0.78f, 0.52f), Rf(rnd, 0f, 0.45f)), Rf(rnd, 0.8f, 1.1f));
                g.Tri(basep - new Vector2(0.022f * s, 0f), basep + new Vector2(0.022f * s, 0f), basep + dir * h, Shade(col, 0.75f), Shade(col, 0.75f), col);
            }
        }

        // ------------------------------------------------------------------ sides (ground level)

        private static void SideGround(Geo g, Ctx c)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var kind = side < 0 ? c.t.leftSide : c.t.rightSide;
                switch (kind)
                {
                    case ThemeSideKind.TownFacades: Boardwalk(g, c, side, 1.3f, false); break;
                    case ThemeSideKind.StationPlatform: Boardwalk(g, c, side, 2.0f, true); break;
                    case ThemeSideKind.Boxcars:
                        Boardwalk(g, c, side, 1.3f, false);
                        break;
                    case ThemeSideKind.CanyonWalls: CanyonWall(g, c, side); break;
                }
            }
        }

        private static Color Plank(StageThemeData t) => Mix(t.roof, t.trim, 0.28f);

        private static void Boardwalk(Geo g, Ctx c, int side, float walkU, bool platform)
        {
            var t = c.t;
            var plank = platform ? Mix(Plank(t), new Color(0.6f, 0.58f, 0.55f), 0.35f) : Plank(t);
            var dark = Shade(t.roof, 0.9f);
            const float step = 0.35f;
            for (float ly = -2.5f; ly < c.m.sizeY + 2.5f; ly += step)
            {
                float l2 = ly + step;
                if (c.RailGap(ly + step * 0.5f, 0.25f)) continue;
                var a = c.E(side, ly); var b = c.E(side, l2);
                float s1 = c.S(ly), s2 = c.S(l2);
                var ao = a + new Vector2(side * walkU * s1, 0f); var bo = b + new Vector2(side * walkU * s2, 0f);
                float k = 0.92f + 0.12f * Mathf.PerlinNoise(ly * 1.7f, side * 3f);
                var col = Shade(plank, k);
                // shadow strip on the street side + planks
                g.Quad(a, b, b + new Vector2(-side * 0.35f * s2, 0f), a + new Vector2(-side * 0.35f * s1, 0f), A(t.groundShadow, t.groundShadow.a * 0.9f), A(t.groundShadow, t.groundShadow.a * 0.9f), A(t.groundShadow, 0f), A(t.groundShadow, 0f));
                g.Quad(a, ao, bo, b, col);
                g.Line(a, ao, 0.03f, A(dark, 0.55f));
            }
            // edge beam along the street
            for (float ly = -2.5f; ly < c.m.sizeY + 2.5f; ly += 0.5f)
            {
                if (c.RailGap(ly + 0.25f, 0.25f)) continue;
                g.Line(c.E(side, ly), c.E(side, ly + 0.5f), 0.1f * c.S(ly), dark);
            }
        }

        private static void CanyonWall(Geo g, Ctx c, int side)
        {
            var t = c.t;
            float outerX = side < 0 ? c.r.xMin - 1f : c.r.xMax + 1f;
            Vector2 prevB = default, prevR = default;
            bool has = false;
            for (float ly = -3f; ly <= c.m.sizeY + 3f; ly += 0.4f)
            {
                float s = c.S(ly);
                float nb = Mathf.PerlinNoise(ly * 0.45f, side * 11f + t.seed);
                float nr = Mathf.PerlinNoise(ly * 0.32f + 7f, side * 5f + t.seed);
                var e = c.E(side, ly);
                var bp = e + new Vector2(side * (0.35f + 0.9f * nb) * s, 0f);
                var rp = bp + new Vector2(side * (2.0f + 1.3f * nr) * s, 0.15f * s);
                if (has)
                {
                    // base shadow on the ground, cliff face gradient, plateau
                    g.Quad(prevB, bp, bp + new Vector2(-side * 0.9f * s, 0f), prevB + new Vector2(-side * 0.9f * s, 0f), A(t.groundShadow, t.groundShadow.a * 1.1f), A(t.groundShadow, t.groundShadow.a * 1.1f), A(t.groundShadow, 0f), A(t.groundShadow, 0f));
                    g.Quad(prevB, bp, rp, prevR, Shade(t.wallA, 0.78f), Shade(t.wallA, 0.78f), t.wallB, t.wallB);
                    g.Quad(prevR, rp, new Vector2(outerX, rp.y), new Vector2(outerX, prevR.y), t.wallC);
                }
                prevB = bp; prevR = rp; has = true;
            }
            // strata streaks along the face + rim highlight + gullies
            for (int k = 1; k <= 4; k++)
            {
                var pts = new List<Vector2>();
                float f = k / 5f;
                for (float ly = -3f; ly <= c.m.sizeY + 3f; ly += 0.4f)
                {
                    float s = c.S(ly);
                    float nb = Mathf.PerlinNoise(ly * 0.45f, side * 11f + t.seed);
                    float nr = Mathf.PerlinNoise(ly * 0.32f + 7f, side * 5f + t.seed);
                    var bp = c.E(side, ly) + new Vector2(side * (0.35f + 0.9f * nb) * s, 0f);
                    float fw = (2.0f + 1.3f * nr) * s;
                    pts.Add(bp + new Vector2(side * fw * (f + 0.04f * Mathf.Sin(ly * 2.3f + k)), 0.15f * s * f));
                }
                g.SoftPolyline(pts, 0.09f, A(Shade(t.wallA, 0.62f), 0.35f));
            }
            for (int i = 0; i < 26; i++)
            {
                float ly = Rf(c.rnd, -2f, c.m.sizeY + 2f), s = c.S(ly);
                float nb = Mathf.PerlinNoise(ly * 0.45f, side * 11f + t.seed);
                float nr = Mathf.PerlinNoise(ly * 0.32f + 7f, side * 5f + t.seed);
                var bp = c.E(side, ly) + new Vector2(side * (0.35f + 0.9f * nb) * s, 0f);
                var rp = bp + new Vector2(side * (2.0f + 1.3f * nr) * s, 0.15f * s);
                var mid = Vector2.Lerp(bp, rp, Rf(c.rnd, 0.2f, 0.5f));
                g.SoftLine(mid, Vector2.Lerp(bp, rp, 0.95f), 0.08f * s, A(Shade(t.wallA, 0.55f), 0.45f));
                if (c.rnd.NextDouble() < 0.5) g.SoftLine(Vector2.Lerp(bp, rp, 0.97f), Vector2.Lerp(bp, rp, 0.97f) + new Vector2(0f, 0.4f * s), 0.12f * s, A(t.trim, 0.35f));
            }
        }

        // ------------------------------------------------------------------ upright items

        private static void BuildItems(Ctx c, List<Item> items)
        {
            var t = c.t;
            bool canyon = t.leftSide == ThemeSideKind.CanyonWalls;
            // boundary fences (or rock lines in the canyon) behind / in front of the lanes
            if (!canyon)
            {
                items.Add(new Item(c.m.sizeY + 0.05f, j => Fence(j.G, c, c.m.sizeY + 0.05f)));
                items.Add(new Item(-0.05f, j => Fence(j.G, c, -0.05f)));
            }
            else
            {
                for (float lx = -0.5f; lx < c.m.sizeX + 0.6f; lx += Rf(c.rnd, 0.9f, 1.6f))
                {
                    float x1 = lx, sz = Rf(c.rnd, 0.22f, 0.4f);
                    items.Add(new Item(c.m.sizeY + 0.1f, j => RockItem(j.G, c, x1, c.m.sizeY + 0.1f, sz)));
                    float x2 = lx + 0.4f, sz2 = Rf(c.rnd, 0.22f, 0.4f);
                    items.Add(new Item(-0.15f, j => RockItem(j.G, c, x2, -0.15f, sz2)));
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                var kind = side < 0 ? t.leftSide : t.rightSide;
                var signs = side < 0 ? t.leftSigns : t.rightSigns;
                int sd = side;
                switch (kind)
                {
                    case ThemeSideKind.TownFacades:
                    {
                        float[] bases = { 19.4f, 15.1f, 10.8f, 6.5f, 2.2f, -2.1f };
                        string[] assign = new string[bases.Length];
                        int[] order = { 2, 3, 1, 4, 0, 5 };
                        for (int i = 0; i < order.Length && signs != null && i < signs.Length; i++) assign[order[i]] = signs[i];
                        for (int i = 0; i < bases.Length; i++)
                        {
                            float ly = bases[i]; string sg = assign[i];
                            float w = Rf(c.rnd, 6.4f, 7.6f), h = Rf(c.rnd, 3.9f, 4.7f); int wall = i;
                            items.Add(new Item(ly, j => Facade(j, c, sd, ly, w, h, sg, wall)));
                        }
                        for (int i = 0; i < 5; i++)
                        {
                            float ly = Rf(c.rnd, 0.6f, c.m.sizeY - 0.6f), lx = side < 0 ? -0.62f : c.m.sizeX + 0.62f;
                            int kindI = i % 3;
                            items.Add(new Item(ly, j => { if (kindI == 0) Barrel(j.G, c, lx, ly); else if (kindI == 1) HitchRail(j.G, c, lx, ly); else CrateItem(j.G, c, lx, ly); }));
                        }
                        break;
                    }
                    case ThemeSideKind.StationPlatform:
                    {
                        float[] bases = { 19.4f, 14.9f, 11.1f, 3.1f, -1.9f };
                        for (int i = 0; i < bases.Length; i++)
                        {
                            float ly = bases[i]; string sg = signs != null && i - 1 >= 0 && i - 1 < signs.Length ? signs[i - 1] : null;
                            float w = Rf(c.rnd, 6.6f, 7.8f), h = Rf(c.rnd, 3.8f, 4.5f); int wall = i;
                            items.Add(new Item(ly, j => Facade(j, c, sd, ly, w, h, sg, wall, 2.0f)));
                        }
                        float wtX = side < 0 ? -3.6f : c.m.sizeX + 3.6f;
                        items.Add(new Item(6.1f, j => WaterTower(j.G, c, wtX, 6.1f)));
                        float xbX = side < 0 ? -0.55f : c.m.sizeX + 0.55f;
                        items.Add(new Item(10.7f, j => Crossbuck(j, c, xbX, 10.7f)));
                        items.Add(new Item(7.2f, j => Crossbuck(j, c, xbX, 7.2f)));
                        for (int i = 0; i < 6; i++)
                        {
                            float ly = Rf(c.rnd, 0.5f, c.m.sizeY - 0.5f);
                            if (c.RailGap(ly, 0.8f)) continue;
                            float lx = side < 0 ? Rf(c.rnd, -1.6f, -0.6f) : c.m.sizeX + Rf(c.rnd, 0.6f, 1.6f);
                            int k = i % 2;
                            items.Add(new Item(ly, j => { if (k == 0) CrateItem(j.G, c, lx, ly); else Sacks(j.G, c, lx, ly); }));
                        }
                        break;
                    }
                    case ThemeSideKind.Boxcars:
                    {
                        float[] bases = { 19.4f, 13.6f, 3.4f, -2.0f };
                        for (int i = 0; i < bases.Length; i++)
                        {
                            float ly = bases[i]; string sg = signs != null && i - 1 >= 0 && i - 1 < signs.Length ? signs[i - 1] : null;
                            float w = Rf(c.rnd, 7f, 8f), h = Rf(c.rnd, 3.4f, 4.0f);
                            items.Add(new Item(ly, j => Facade(j, c, sd, ly, w, h, sg, 2 + (int)ly % 2, 1.3f, 4)));
                        }
                        float px = side < 0 ? -0.5f : c.m.sizeX + 0.5f;
                        for (float ly = 0.5f; ly < c.m.sizeY; ly += 3.1f)
                        {
                            if (c.RailGap(ly, 0.5f)) continue;
                            float l0 = ly;
                            items.Add(new Item(l0, j => TelegraphPole(j.G, c, px, l0)));
                        }
                        for (int i = 0; i < 5; i++)
                        {
                            float ly = Rf(c.rnd, 0.5f, c.m.sizeY - 0.5f);
                            if (c.RailGap(ly, 0.8f)) continue;
                            float lx = side < 0 ? -0.95f : c.m.sizeX + 0.95f;
                            items.Add(new Item(ly, j => CrateItem(j.G, c, lx, ly, 1.15f)));
                        }
                        break;
                    }
                }
            }

            // scattered desert decor in free slots (never inside the playable lanes)
            var slots = new List<System.Func<Vector2>>();
            slots.Add(() => new Vector2(Rf(c.rnd, 0.8f, c.m.sizeX - 0.8f), Rf(c.rnd, -1.2f, -0.45f)));          // below the near fence
            slots.Add(() => new Vector2(Rf(c.rnd, -1.5f, c.m.sizeX + 1.5f), Rf(c.rnd, c.m.sizeY + 0.35f, c.m.sizeY + 0.9f))); // behind the far fence
            if (t.leftSide == ThemeSideKind.CanyonWalls || t.leftSide == ThemeSideKind.OpenDesert)
                slots.Add(() => new Vector2(Rf(c.rnd, -7f, -2.6f), Rf(c.rnd, -1f, c.m.sizeY + 1f)));
            if (t.rightSide == ThemeSideKind.CanyonWalls || t.rightSide == ThemeSideKind.OpenDesert)
                slots.Add(() => new Vector2(Rf(c.rnd, c.m.sizeX + 2.6f, c.m.sizeX + 7f), Rf(c.rnd, -1f, c.m.sizeY + 1f)));
            Vector2 Slot(int i) => slots[i % slots.Count]();
            for (int i = 0; i < t.cactusCount; i++) { var p = Slot(i + 1); float h = Rf(c.rnd, 0.9f, 1.6f); items.Add(new Item(p.y, j => Cactus(j.G, c, p.x, p.y, h))); }
            for (int i = 0; i < t.skullCount; i++) { var p = Slot(i * 2); items.Add(new Item(p.y, j => Skull(j.G, c, p.x, p.y))); }
            for (int i = 0; i < t.rockCount; i++) { var p = Slot(i + 2); float sz = Rf(c.rnd, 0.25f, 0.6f); items.Add(new Item(p.y, j => RockItem(j.G, c, p.x, p.y, sz))); }
            for (int i = 0; i < t.tumbleweedStatic; i++) { var p = Slot(i * 3 + 1); items.Add(new Item(p.y, j => StaticWeed(j.G, c, p.x, p.y))); }
            if (canyon)
                for (int i = 0; i < 10; i++)
                {
                    int side = i % 2 == 0 ? -1 : 1;
                    float ly = Rf(c.rnd, -0.5f, c.m.sizeY + 0.5f), lx = side < 0 ? Rf(c.rnd, -0.9f, -0.35f) : c.m.sizeX + Rf(c.rnd, 0.35f, 0.9f), sz = Rf(c.rnd, 0.3f, 0.65f);
                    items.Add(new Item(ly, j => RockItem(j.G, c, lx, ly, sz)));
                }
        }
    }
}

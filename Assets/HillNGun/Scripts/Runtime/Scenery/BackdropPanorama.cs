using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Scenery
{
    public static partial class BackdropBaker
    {
        /// <summary>
        /// Menu panorama: a dusty main street running to a frontier town on the horizon under a dusk sky, mesas behind,
        /// cactus / skull / fence in the foreground. Same palette system as the stage backdrops.
        /// </summary>
        private static void ComposePanorama(Job job, StageThemeData t, Rect r)
        {
            var c = new Ctx { t = t, m = FieldMap.Default, r = r, view = r, rnd = new System.Random(t.seed * 31 + 5) };
            float hy = r.yMin + r.height * 0.42f;
            c.hy = hy;
            var g = job.G;
            Sky(g, c, hy, r.yMax);
            float b = hy - 0.02f;
            MesaRow(g, c, b, r.xMin - 1f, r.xMax + 1f, t.farColor2, 0.5f, 1.3f, 2.5f, 6f);
            g.RectV(r.xMin, b - 0.1f, r.xMax, b + 0.7f, A(t.hazeColor, t.hazeColor.a * 0.8f), A(t.hazeColor, 0f));
            float cx = r.center.x;
            // town on the horizon, gap for the street
            TownCluster(g, c, b, cx - r.width * 0.36f, cx - 0.55f);
            TownCluster(g, c, b, cx + 0.55f, cx + r.width * 0.36f);
            WindmillSil(g, new Vector2(cx - r.width * 0.42f, b), 2.1f, t.farColor);
            WaterTowerSil(g, new Vector2(cx + r.width * 0.4f, b), 1.8f, t.farColor);
            job.Flush();

            // ground with perspective street
            var gg = new Geo { UvScale = new Vector2(0.3f, 0.3f) };
            const int nx = 40, ny = 14;
            Color GC(float x, float y)
            {
                float k = Mathf.InverseLerp(hy, r.yMin, y);            // 0 at horizon .. 1 at bottom
                float half = Mathf.Lerp(0.5f, r.width * 0.2f, k);
                float road = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(half * 0.7f, half * 1.25f, Mathf.Abs(x - cx)));
                var col = Mix(t.sandFar, t.sand, k);
                col = Mix(col, t.dirt, road * 0.5f);
                float n = Mathf.PerlinNoise(x * 0.4f + t.seed, y * 0.9f) - 0.5f;
                float m = (1f + n * 0.22f * t.blotchStrength * 2f) * Mathf.Lerp(1.02f, 0.9f, k);
                return new Color(Mathf.Min(1f, col.r * m / 0.92f), Mathf.Min(1f, col.g * m / 0.92f), Mathf.Min(1f, col.b * m / 0.92f), 1f);
            }
            for (int iy = 0; iy < ny; iy++)
                for (int ix = 0; ix < nx; ix++)
                {
                    float x0 = r.xMin + r.width * ix / nx, x1 = r.xMin + r.width * (ix + 1) / nx;
                    float y0 = r.yMin + (hy - r.yMin) * iy / ny, y1 = r.yMin + (hy - r.yMin) * (iy + 1) / ny;
                    gg.Quad(new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1), GC(x0, y0), GC(x1, y0), GC(x1, y1), GC(x0, y1));
                }
            job.AddGrain(gg);
            g = job.G;
            // wheel ruts converging to the vanishing point
            var vp = new Vector2(cx, hy);
            foreach (float off in new[] { -0.5f, -0.38f, 0.38f, 0.5f })
            {
                var bottom = new Vector2(cx + off * r.width * 0.38f, r.yMin - 0.3f);
                g.SoftLine(bottom, Vector2.Lerp(bottom, vp, 0.97f), 0.14f, A(t.rut, 0.45f));
            }
            // fence posts receding along the left of the street
            for (int i = 0; i < 9; i++)
            {
                float k = Mathf.Pow(i / 9f, 1.6f);
                var bp = Vector2.Lerp(new Vector2(cx - r.width * 0.42f, r.yMin + 0.4f), vp + new Vector2(-0.9f, 0f), k);
                float s = Mathf.Lerp(1.5f, 0.15f, k);
                g.Rect(bp.x - 0.06f * s, bp.y, bp.x + 0.06f * s, bp.y + 0.7f * s, t.fenceColor);
                if (i < 8)
                {
                    float k2 = Mathf.Pow((i + 1) / 9f, 1.6f);
                    var bp2 = Vector2.Lerp(new Vector2(cx - r.width * 0.42f, r.yMin + 0.4f), vp + new Vector2(-0.9f, 0f), k2);
                    float s2 = Mathf.Lerp(1.5f, 0.15f, k2);
                    g.Line(bp + new Vector2(0f, 0.5f * s), bp2 + new Vector2(0f, 0.5f * s2), 0.07f * s, t.fenceColor);
                    g.Line(bp + new Vector2(0f, 0.25f * s), bp2 + new Vector2(0f, 0.25f * s2), 0.07f * s, Shade(t.fenceColor, 0.9f));
                }
            }
            // scattered tufts + decor
            for (int i = 0; i < 26; i++)
            {
                float y = Rf(c.rnd, r.yMin, hy - 0.3f);
                float k = Mathf.InverseLerp(hy, r.yMin, y);
                float x = Rf(c.rnd, r.xMin, r.xMax);
                if (Mathf.Abs(x - cx) < Mathf.Lerp(0.5f, r.width * 0.2f, k) * 1.2f) continue;
                Tuft(g, new Vector2(x, y), Mathf.Lerp(0.4f, 1.8f, k), t.grass, c.rnd, t.groundShadow);
            }
            CactusW(g, t, new Vector2(r.xMin + r.width * 0.08f, r.yMin + 0.6f), 2.9f, 0.7f);
            CactusW(g, t, new Vector2(r.xMax - r.width * 0.2f, hy - 1.0f), 1.0f, 0.2f);
            CactusW(g, t, new Vector2(r.xMin + r.width * 0.26f, hy - 0.6f), 0.7f, 0.45f);
            RockW(g, t, new Vector2(r.xMax - r.width * 0.1f, r.yMin + 0.4f), 0.9f, 0.3f);
            RockW(g, t, new Vector2(r.xMax - r.width * 0.15f, r.yMin + 0.3f), 0.5f, 0.6f);
            SkullW(g, t, new Vector2(r.xMax - r.width * 0.28f, r.yMin + 0.6f), 1.6f, false);
            WeedW(g, t, new Vector2(cx + r.width * 0.12f, hy - 1.6f), 1.2f, 0.4f);
            job.Flush();
            Vignette(job.G, r, r, t);
        }

        private static void TownCluster(Geo g, Ctx c, float b, float x0, float x1)
        {
            var t = c.t;
            float x = x0;
            int i = 0;
            while (x < x1 - 0.3f)
            {
                float w = Mathf.Min(Rf(c.rnd, 0.8f, 1.5f), x1 - x);
                float h = Rf(c.rnd, 0.7f, 1.4f) * (1f - 0.35f * Mathf.Abs((x - c.r.center.x) / (c.r.width * 0.4f)));
                var col = Mix(t.farColor, WallColor(t, i), 0.18f);
                g.Rect(x, b, x + w, b + h, col);
                g.Rect(x + w * 0.15f, b + h, x + w * 0.85f, b + h + 0.12f, col);
                g.Rect(x - 0.03f, b + h * 0.38f, x + w + 0.03f, b + h * 0.43f, Shade(col, 0.75f));
                bool lit = t.litWindows > 0f && c.rnd.NextDouble() < Mathf.Max(0.35f, t.litWindows);
                for (int k = 0; k < 2; k++)
                {
                    float wx = x + w * (0.25f + 0.4f * k);
                    g.Rect(wx, b + h * 0.55f, wx + w * 0.14f, b + h * 0.75f, lit ? A(t.windowLit, 0.9f) : Shade(col, 0.7f));
                }
                x += w + Rf(c.rnd, 0.02f, 0.25f);
                i++;
            }
        }
    }
}

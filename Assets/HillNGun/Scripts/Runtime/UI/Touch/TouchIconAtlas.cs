using UnityEngine;

namespace HillNGun.UI.Touch
{
    /// <summary>
    /// Code-drawn 256x256 atlas (4x4 cells of 64 px) for the touch controls: discs, rings, knob and simple icons.
    /// One texture for every Image keeps the whole control canvas in one or two batches. No external art.
    /// </summary>
    public static class TouchIconAtlas
    {
        public enum Cell { Disc = 0, Ring = 1, Knob = 2, Crosshair = 3, Roll = 4, Dynamite = 5, Star = 6, RingThin = 7, Pip = 8, Arrow = 9,
                          Cross = 10, Bullet = 11, Coin = 12, Bolt = 13, Glow = 14, Sparkle = 15 }

        /// <summary>UV rect (0..1) of a cell, for meshes that sample the atlas directly (field drops).</summary>
        public static Rect UV(Cell c)
        {
            int i = (int)c;
            return new Rect((i % Cols) * N / (float)Size, (i / Cols) * N / (float)Size, N / (float)Size, N / (float)Size);
        }

        private const int N = 64, Cols = 4, Size = 256;
        private static Texture2D _tex;
        private static Sprite[] _sprites;

        public static Texture2D Texture { get { Ensure(); return _tex; } }

        public static Sprite Get(Cell c)
        {
            Ensure();
            return _sprites[(int)c];
        }

        private static void Ensure()
        {
            if (_tex != null && _sprites != null) return;
            _tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "TouchControlsAtlas",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[Size * Size];
            _sprites = new Sprite[16];
            for (int i = 0; i < 16; i++)
            {
                int cx = (i % Cols) * N, cy = (i / Cols) * N;
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        // cell space -1..1 with a 2 px margin so bilinear sampling never bleeds into neighbours
                        float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                        u *= 1.07f; v *= 1.07f;
                        Color c = Shade(i, u, v);
                        px[(cy + y) * Size + cx + x] = c;
                    }
                _sprites[i] = Sprite.Create(_tex, new Rect(cx, cy, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                _sprites[i].name = "touch_" + (Cell)i;
            }
            _tex.SetPixels32(px);
            _tex.Apply(false, true);
        }

        private const float AA = 2.2f / N; // ~1 px anti-alias band in cell space

        private static float Fill(float d) => Mathf.Clamp01(0.5f - d / AA); // d < 0 inside

        private static Color Shade(int cell, float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            switch ((Cell)cell)
            {
                case Cell.Disc: return new Color(1f, 1f, 1f, Fill(r - 0.96f));
                case Cell.Pip: return new Color(1f, 1f, 1f, Fill(r - 0.7f));
                case Cell.Ring: return new Color(1f, 1f, 1f, Fill(Mathf.Abs(r - 0.88f) - 0.085f));
                case Cell.RingThin: return new Color(1f, 1f, 1f, Fill(Mathf.Abs(r - 0.92f) - 0.045f));
                case Cell.Knob:
                {
                    float a = Fill(r - 0.96f);
                    float rim = Fill(Mathf.Abs(r - 0.86f) - 0.08f);
                    float hi = Mathf.Clamp01(1f - ((u + 0.3f) * (u + 0.3f) + (v - 0.35f) * (v - 0.35f)) / 0.18f) * 0.25f;
                    float g = Mathf.Lerp(1f, 0.72f, rim) + hi;
                    return new Color(g, g, g, a);
                }
                case Cell.Crosshair:
                {
                    float ring = Fill(Mathf.Abs(r - 0.55f) - 0.07f);
                    float dot = Fill(r - 0.13f);
                    float ticks = 0f;
                    if (Mathf.Abs(u) < 0.07f && Mathf.Abs(v) > 0.3f && Mathf.Abs(v) < 0.9f) ticks = 1f;
                    if (Mathf.Abs(v) < 0.07f && Mathf.Abs(u) > 0.3f && Mathf.Abs(u) < 0.9f) ticks = 1f;
                    return WithOutline(Mathf.Max(ring, dot, ticks), u, v, Cell.Crosshair);
                }
                case Cell.Roll:
                {
                    // 300 degree arc + arrow head = "tumble"
                    float ang = Mathf.Atan2(v, u) * Mathf.Rad2Deg; // -180..180
                    float arc = (ang > -60f || ang < -120f) ? Fill(Mathf.Abs(r - 0.6f) - 0.12f) : 0f;
                    // arrow head at the arc end (angle -60)
                    Vector2 tip = new Vector2(Mathf.Cos(-50f * Mathf.Deg2Rad), Mathf.Sin(-50f * Mathf.Deg2Rad)) * 0.6f;
                    float head = Tri(new Vector2(u, v), tip + new Vector2(-0.38f, -0.2f), tip + new Vector2(0.18f, 0.36f), tip + new Vector2(0.15f, -0.45f));
                    return WithOutline(Mathf.Max(arc, head), u, v, Cell.Roll);
                }
                case Cell.Dynamite:
                {
                    // tilted stick + band + fuse + spark
                    float ca = Mathf.Cos(35f * Mathf.Deg2Rad), sa = Mathf.Sin(35f * Mathf.Deg2Rad);
                    float x = u * ca + v * sa, y = -u * sa + v * ca;
                    float stick = Fill(Box(x + 0.05f, y, 0.62f, 0.22f) - 0.04f);
                    float band = (Mathf.Abs(x + 0.05f) < 0.08f && Mathf.Abs(y) < 0.27f) ? 0.55f : 0f;
                    float fuse = (x > 0.55f && x < 0.8f && Mathf.Abs(y - (x - 0.55f) * 0.6f) < 0.05f) ? 1f : 0f;
                    Vector2 sp = new Vector2(0.62f, 0.62f);
                    float spark = Fill(Star(new Vector2(u, v) - sp, 0.22f, 0.09f, 4));
                    float a = Mathf.Max(stick, fuse, spark);
                    Color c = WithOutline(a, u, v, Cell.Dynamite);
                    if (band > 0f && stick > 0.5f) c = new Color(c.r * 0.75f, c.g * 0.75f, c.b * 0.75f, c.a);
                    return c;
                }
                case Cell.Star: return WithOutline(Fill(Star(new Vector2(u, v), 0.82f, 0.36f, 5)), u, v, Cell.Star);
                case Cell.Cross:
                {
                    float a = Mathf.Min(Box(u, v, 0.2f, 0.62f), Box(u, v, 0.62f, 0.2f));
                    return WithOutline(Fill(a - 0.04f), u, v, Cell.Cross);
                }
                case Cell.Bullet:
                {
                    // brass cartridge: case + rounded tip
                    float body = Box(u, v + 0.2f, 0.24f, 0.45f);
                    float tip = new Vector2(u, (v - 0.25f) * 0.8f).magnitude - 0.24f;
                    float rim = Box(u, v + 0.62f, 0.3f, 0.06f);
                    float a = Fill(Mathf.Min(body, tip, rim) - 0.02f);
                    Color c = WithOutline(a, u, v, Cell.Bullet);
                    if (a > 0.5f && v > 0.18f) c = new Color(c.r * 0.82f, c.g * 0.82f, c.b * 0.82f, c.a); // darker lead tip
                    return c;
                }
                case Cell.Coin:
                {
                    float disc = Fill(r - 0.8f);
                    float inner = Fill(Mathf.Abs(r - 0.58f) - 0.05f);
                    float bar = Fill(Box(u, v, 0.07f, 0.42f));
                    float a = disc;
                    Color c = WithOutline(a, u, v, Cell.Coin);
                    float shade = 1f - 0.28f * Mathf.Max(inner, bar);
                    return new Color(c.r * shade, c.g * shade, c.b * shade, c.a);
                }
                case Cell.Bolt:
                {
                    Vector2 p = new Vector2(u, v);
                    float a = Mathf.Max(Tri(p, new Vector2(0.15f, 0.9f), new Vector2(-0.45f, -0.08f), new Vector2(0.12f, -0.08f)),
                                        Tri(p, new Vector2(-0.12f, 0.1f), new Vector2(0.45f, 0.1f), new Vector2(-0.15f, -0.9f)));
                    return WithOutline(a, u, v, Cell.Bolt);
                }
                case Cell.Glow:
                {
                    float g = Mathf.Clamp01(1f - r);
                    return new Color(1f, 1f, 1f, g * g);
                }
                case Cell.Sparkle:
                {
                    float s = Mathf.Clamp01(1f - (Mathf.Abs(u) * 5f + Mathf.Abs(v) * 0.9f)) + Mathf.Clamp01(1f - (Mathf.Abs(v) * 5f + Mathf.Abs(u) * 0.9f));
                    float core = Mathf.Clamp01(1f - r * 3.2f);
                    return new Color(1f, 1f, 1f, Mathf.Clamp01(s + core));
                }
                case Cell.Arrow:
                {
                    float shaft = Fill(Box(u, v + 0.15f, 0.12f, 0.45f));
                    float head = Tri(new Vector2(u, v), new Vector2(-0.45f, 0.2f), new Vector2(0.45f, 0.2f), new Vector2(0f, 0.8f));
                    return WithOutline(Mathf.Max(shaft, head), u, v, Cell.Arrow);
                }
            }
            return new Color(1f, 1f, 1f, 0f);
        }

        /// <summary>White icon with a soft dark outline (reads on any button colour).</summary>
        private static Color WithOutline(float a, float u, float v, Cell cell)
        {
            if (a >= 0.99f) return Color.white;
            // cheap outline: max coverage of the shape in a small neighbourhood
            float o = 0f;
            const float k = 0.075f;
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f;
                float uu = u + Mathf.Cos(ang) * k, vv = v + Mathf.Sin(ang) * k;
                o = Mathf.Max(o, Coverage(cell, uu, vv));
            }
            float outline = Mathf.Clamp01(o) * 0.55f;
            float alpha = Mathf.Max(a, outline);
            float g = alpha > 0f ? a / alpha : 0f;
            return new Color(g, g * 0.97f, g * 0.92f, alpha);
        }

        private static bool _inCoverage;

        private static float Coverage(Cell cell, float u, float v)
        {
            if (_inCoverage) return 0f;
            _inCoverage = true;
            float a = Shade((int)cell, u, v).a;
            _inCoverage = false;
            return a;
        }

        private static float Box(float x, float y, float hx, float hy)
        {
            float dx = Mathf.Abs(x) - hx, dy = Mathf.Abs(y) - hy;
            float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(dx, dy), 0f);
        }

        private static float Star(Vector2 p, float rOuter, float rInner, int points)
        {
            float ang = Mathf.Atan2(p.x, p.y);
            float seg = Mathf.PI * 2f / points;
            float a = Mathf.Repeat(ang, seg) / seg;           // 0..1 inside one point
            float t = Mathf.Abs(a - 0.5f) * 2f;               // 1 at the tip, 0 between tips
            float edge = Mathf.Lerp(rInner, rOuter, t * t);
            return p.magnitude - edge;
        }

        private static float Tri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float e0 = Edge(p, a, b), e1 = Edge(p, b, c), e2 = Edge(p, c, a);
            bool inside = (e0 >= 0 && e1 >= 0 && e2 >= 0) || (e0 <= 0 && e1 <= 0 && e2 <= 0);
            return inside ? 1f : 0f;
        }

        private static float Edge(Vector2 p, Vector2 a, Vector2 b) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    }
}

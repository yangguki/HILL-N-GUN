using System.Collections.Generic;
using UnityEngine;
using HillNGun.Battlefield;
using HillNGun.Data;

namespace HillNGun.Scenery
{
    /// <summary>
    /// Ambient Western life on the field: tumbleweeds rolling with the wind (bounce + spin + ground shadow) and
    /// drifting dust motes. Pure view. Everything is one dynamic mesh with one procedural atlas at one sorting order under
    /// every actor = exactly 1 draw call. Uses unscaled-independent Time.deltaTime (pauses with the game).
    /// </summary>
    public class AmbientFx : MonoBehaviour
    {
        private const int Order = FieldView.ActorBaseOrder - 8;

        private static Texture2D _atlas;
        private static readonly Rect WeedUV = new Rect(0f, 0f, 64f / 96f, 1f);
        private static readonly Rect BlobUV = new Rect(64f / 96f, 0f, 32f / 96f, 0.5f);

        private sealed class Weed
        {
            public float lx, ly, vx, vy, angle, hop, hopV, size; public bool active; public float wait;
        }

        private sealed class Mote
        {
            public Vector2 p, v; public float life, maxLife, size;
        }

        private readonly List<Weed> _weeds = new List<Weed>();
        private readonly List<Mote> _motes = new List<Mote>();
        private StageThemeData _t;
        private FieldMap _m;
        private System.Random _rnd = new System.Random(77);
        private Rect _view;

        // single dynamic mesh = exactly one draw call for the whole ambient layer
        private Mesh _mesh;
        private Material _mat;
        private MeshRenderer _mr;
        private readonly List<Vector3> _v = new List<Vector3>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<Vector2> _u = new List<Vector2>();
        private readonly List<int> _tri = new List<int>();

        public void Configure(StageThemeData theme, FieldMap map)
        {
            _t = theme; _m = map;
            EnsureAtlas();
            _weeds.Clear(); _motes.Clear();
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "AmbientFx" };
                _mesh.MarkDynamic();
                _mat = new Material(Shader.Find("Sprites/Default")) { name = "AmbientFx", mainTexture = _atlas };
                gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
                _mr = gameObject.AddComponent<MeshRenderer>();
                _mr.sharedMaterial = _mat;
                _mr.sortingOrder = Order;
                _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _mr.receiveShadows = false;
            }
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (theme == null) { _mesh.Clear(); return; }
            _rnd = new System.Random(theme.seed * 13 + 1);
            for (int i = 0; i < Mathf.Clamp(theme.tumbleweeds, 0, 6); i++)
                _weeds.Add(new Weed { wait = i * theme.tumbleweedInterval * 0.5f + (float)_rnd.NextDouble() * 2f });
            int motes = System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-nodust") ? 0 : Mathf.Clamp(theme.dustMotes, 0, 40);
            for (int i = 0; i < motes; i++)
            {
                var m = new Mote();
                RespawnMote(m, true);
                _motes.Add(m);
            }
        }

        private void Quad(Vector2 c, Vector2 half, float angDeg, Rect uv, Color col)
        {
            int i = _v.Count;
            float a = angDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Vector3 P(float x, float y) => new Vector3(c.x + x * ca - y * sa, c.y + x * sa + y * ca, 0f);
            _v.Add(P(-half.x, -half.y)); _v.Add(P(half.x, -half.y)); _v.Add(P(half.x, half.y)); _v.Add(P(-half.x, half.y));
            _u.Add(new Vector2(uv.xMin, uv.yMin)); _u.Add(new Vector2(uv.xMax, uv.yMin)); _u.Add(new Vector2(uv.xMax, uv.yMax)); _u.Add(new Vector2(uv.xMin, uv.yMax));
            _c.Add(col); _c.Add(col); _c.Add(col); _c.Add(col);
            _tri.Add(i); _tri.Add(i + 2); _tri.Add(i + 1); _tri.Add(i); _tri.Add(i + 3); _tri.Add(i + 2);
        }

        private void Update()
        {
            if (_t == null || _mesh == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var cam = Camera.main;
            if (cam != null) { var b = FieldView.CameraBounds(cam); _view = new Rect(b.min.x, b.min.y, b.size.x, b.size.y); }
            Vector2 wind = _t.wind;
            float dir = wind.x >= 0f ? 1f : -1f;
            _v.Clear(); _c.Clear(); _u.Clear(); _tri.Clear();

            // dust motes first (furthest back in the layer)
            foreach (var m in _motes)
            {
                m.life += dt;
                m.p += (m.v + wind * 0.6f) * dt;
                float k = m.life / m.maxLife;
                if (k >= 1f) { RespawnMote(m, false); continue; }
                var col = _t.dustColor; col.a *= Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
                Quad(m.p, new Vector2(m.size * 1.1f, m.size * 0.5f), 0f, BlobUV, col);
            }

            var sc = _t.groundShadow; sc.a = Mathf.Min(1f, sc.a * 1.3f);
            var weedCol = Color.Lerp(Color.white, _t.grass, 0.25f);
            foreach (var w in _weeds)
            {
                if (!w.active)
                {
                    w.wait -= dt;
                    if (w.wait > 0f) continue;
                    w.active = true;
                    w.ly = Mathf.Lerp(0.6f, _m.sizeY - 0.6f, (float)_rnd.NextDouble());
                    w.lx = dir > 0f ? -6f : _m.sizeX + 6f;
                    w.size = Mathf.Lerp(0.55f, 0.85f, (float)_rnd.NextDouble());
                    w.vx = Mathf.Abs(wind.x) * Mathf.Lerp(0.8f, 1.4f, (float)_rnd.NextDouble()) * dir;
                    w.vy = wind.y * (float)(_rnd.NextDouble() - 0.3);
                    w.hop = 0f; w.hopV = 0f;
                }
                float gust = 1f + 0.35f * Mathf.Sin(Time.time * 1.3f + w.ly);
                w.lx += w.vx * gust * dt;
                w.ly = Mathf.Clamp(w.ly + w.vy * dt, 0.3f, _m.sizeY - 0.3f);
                w.hopV -= 9f * dt;
                w.hop += w.hopV * dt;
                if (w.hop <= 0f) { w.hop = 0f; w.hopV = Mathf.Lerp(1.2f, 2.6f, (float)_rnd.NextDouble()) * Mathf.Min(1f, Mathf.Abs(w.vx) / 1.5f); }
                float s = _m.S(w.ly) * w.size;
                w.angle -= w.vx * gust * dt / (0.5f * s) * Mathf.Rad2Deg;
                var p = _m.W(w.lx, w.ly);
                float sh = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(w.hop / 0.5f));
                Quad(p + new Vector2(0.1f * s, 0.02f), new Vector2(0.55f * s * sh, 0.14f * s * sh), 0f, BlobUV, sc);
                Quad(p + new Vector2(0f, (0.5f + w.hop) * s), new Vector2(0.5f * s, 0.5f * s), w.angle, WeedUV, weedCol);
                if ((dir > 0f && w.lx > _m.sizeX + 7f) || (dir < 0f && w.lx < -7f))
                {
                    w.active = false;
                    w.wait = _t.tumbleweedInterval * Mathf.Lerp(0.7f, 1.4f, (float)_rnd.NextDouble());
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(_v);
            _mesh.SetColors(_c);
            _mesh.SetUVs(0, _u);
            _mesh.SetTriangles(_tri, 0, false);
            _mesh.bounds = new Bounds(Vector3.zero, new Vector3(200f, 200f, 10f));
        }

        private void RespawnMote(Mote m, bool anywhere)
        {
            var v = _view.width > 0f ? _view : new Rect(-17.8f, -10f, 35.6f, 20f);
            float x = anywhere ? Mathf.Lerp(v.xMin, v.xMax, (float)_rnd.NextDouble()) : (_t.wind.x >= 0f ? v.xMin + (float)_rnd.NextDouble() * v.width * 0.5f : v.xMax - (float)_rnd.NextDouble() * v.width * 0.5f);
            float y = Mathf.Lerp(v.yMin, _m.Horizon - _m.sizeY * 0.5f + 0.5f, (float)_rnd.NextDouble());
            m.p = new Vector2(x, y);
            m.v = new Vector2(((float)_rnd.NextDouble() - 0.5f) * 0.4f, ((float)_rnd.NextDouble() - 0.3f) * 0.25f);
            m.life = 0f;
            m.maxLife = Mathf.Lerp(3f, 6f, (float)_rnd.NextDouble());
            m.size = Mathf.Lerp(0.25f, 0.6f, (float)_rnd.NextDouble());
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_mat != null) Destroy(_mat);
        }

        // ------------------------------------------------------------------ procedural atlas (no assets)

        private static void EnsureAtlas()
        {
            if (_atlas != null) return;
            const int W = 96, H = 64;
            _atlas = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "AmbientAtlas", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, 0f);
            // tumbleweed: tangle of twig strokes in a 64x64 cell
            var rnd = new System.Random(4242);
            var twig = new Color(0.62f, 0.48f, 0.3f, 1f);
            var twigDark = new Color(0.42f, 0.3f, 0.18f, 1f);
            for (int i = 0; i < 46; i++)
            {
                double a0 = rnd.NextDouble() * Mathf.PI * 2, a1 = a0 + 1.2 + rnd.NextDouble() * 2.2;
                float r0 = 14f + (float)rnd.NextDouble() * 15f, r1 = 12f + (float)rnd.NextDouble() * 17f;
                var p0 = new Vector2(32f + Mathf.Cos((float)a0) * r0, 32f + Mathf.Sin((float)a0) * r0);
                var p1 = new Vector2(32f + Mathf.Cos((float)a1) * r1, 32f + Mathf.Sin((float)a1) * r1);
                var bend = (p0 + p1) * 0.5f + new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 10f;
                var col = i % 3 == 0 ? twigDark : twig;
                for (int k = 0; k <= 40; k++)
                {
                    float t = k / 40f;
                    var p = Vector2.Lerp(Vector2.Lerp(p0, bend, t), Vector2.Lerp(bend, p1, t), t);
                    Dab(px, W, H, p.x, p.y, 1.1f, col);
                }
            }
            // soft round blob in the 32x32 cell at (64, 0)
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float dx = (x + 0.5f - 16f) / 16f, dy = (y + 0.5f - 16f) / 16f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    px[y * W + 64 + x] = new Color(1f, 1f, 1f, a);
                }
            _atlas.SetPixels(px);
            _atlas.Apply(false, true);
        }

        private static void Dab(Color[] px, int w, int h, float cx, float cy, float r, Color c)
        {
            for (int y = Mathf.FloorToInt(cy - r - 1); y <= Mathf.CeilToInt(cy + r + 1); y++)
                for (int x = Mathf.FloorToInt(cx - r - 1); x <= Mathf.CeilToInt(cx + r + 1); x++)
                {
                    if (x < 0 || y < 0 || x >= 64 || y >= h) continue;
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    float a = Mathf.Clamp01(r + 0.5f - d);
                    if (a <= 0f) continue;
                    var o = px[y * w + x];
                    float na = Mathf.Max(o.a, a);
                    px[y * w + x] = o.a <= 0f ? new Color(c.r, c.g, c.b, a) : new Color(Mathf.Lerp(o.r, c.r, a), Mathf.Lerp(o.g, c.g, a), Mathf.Lerp(o.b, c.b, a), na);
                }
        }
    }
}

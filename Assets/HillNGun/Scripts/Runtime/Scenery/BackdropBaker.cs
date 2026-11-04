using System.Collections.Generic;
using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Scenery
{
    /// <summary>
    /// Builds the whole static stage backdrop (sky, horizon silhouettes, textured ground, ruts, rails, side buildings /
    /// cliffs, decor, fences, vignette) from code and renders it ONCE into a texture. At runtime the field background
    /// is then a single textured quad = 1 draw call, however detailed the art is. All art is procedural (no assets).
    /// </summary>
    public static partial class BackdropBaker
    {
        public const int BakeLayer = 30;

        private static Material _plain, _grainMat;
        private static Texture2D _grain;
        private static float _grainStrength = -1f;
        private static Font _font;

        // ------------------------------------------------------------------ public API

        /// <summary>Field backdrop for a camera view (world rect) plus margin (camera shake). pxPerUnit from the screen.</summary>
        public static Texture2D BakeField(StageThemeData t, FieldMap m, Rect view, float margin, int maxSize, int pxHeightForView)
        {
            var r = new Rect(view.xMin - margin, view.yMin - margin, view.width + margin * 2f, view.height + margin * 2f);
            int pxH = Mathf.Max(64, Mathf.RoundToInt(pxHeightForView * r.height / view.height));
            int pxW = Mathf.RoundToInt(pxH * r.width / r.height);
            Fit(ref pxW, ref pxH, maxSize);
            var job = new Job(t);
            try
            {
                ComposeField(job, t, m, r, view);
                return job.Render(r, pxW, pxH, t.skyHorizon);
            }
            finally { job.Dispose(); }
        }

        /// <summary>Wide menu panorama (lobby / stage select / title) for the given pixel size.</summary>
        public static Texture2D BakePanorama(StageThemeData t, int pxW, int pxH, int maxSize)
        {
            Fit(ref pxW, ref pxH, maxSize);
            float h = 9f, w = h * pxW / Mathf.Max(1f, pxH);
            var r = new Rect(0f, 0f, w, h);
            var job = new Job(t);
            try
            {
                ComposePanorama(job, t, r);
                return job.Render(r, pxW, pxH, t.skyHorizon);
            }
            finally { job.Dispose(); }
        }

        private static void Fit(ref int w, ref int h, int maxSize)
        {
            maxSize = Mathf.Clamp(maxSize, 256, 4096);
            float k = Mathf.Min(1f, maxSize / (float)Mathf.Max(w, h));
            w = Mathf.Max(16, Mathf.RoundToInt(w * k));
            h = Mathf.Max(16, Mathf.RoundToInt(h * k));
        }

        // ------------------------------------------------------------------ job (temporary scene objects)

        private sealed class Job
        {
            public readonly GameObject Root;
            public Geo G = new Geo();                 // current plain (vertex colour) batch
            private readonly List<Object> _trash = new List<Object>();
            private int _order;

            public Job(StageThemeData t)
            {
                Root = new GameObject("BackdropBake") { layer = BakeLayer };
                EnsureMaterials(t != null ? t.grainStrength : 0.5f);
            }

            /// <summary>Flushes the current plain geometry as the next layer.</summary>
            public void Flush()
            {
                if (G.Count > 0) AddMesh(G, _plain);
                G = new Geo();
            }

            public void AddGrain(Geo g) { Flush(); AddMesh(g, _grainMat); }

            private void AddMesh(Geo g, Material m)
            {
                var go = new GameObject("L" + _order) { layer = BakeLayer };
                go.transform.SetParent(Root.transform, false);
                var mesh = g.Build("bake" + _order);
                _trash.Add(mesh);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = m;
                mr.sortingOrder = _order++;
            }

            /// <summary>Sign text, centred, fitted into height / max width (world units).</summary>
            public void Text(string s, Vector2 center, float height, float maxWidth, Color c)
            {
                if (string.IsNullOrEmpty(s)) return;
                Flush();
                var go = new GameObject("T" + _order) { layer = BakeLayer };
                go.transform.SetParent(Root.transform, false);
                go.transform.position = new Vector3(center.x, center.y, 0f);
                var tm = go.AddComponent<TextMesh>();
                var f = DefaultFont();
                if (f != null) tm.font = f;
                tm.text = s;
                tm.fontSize = 64;
                tm.fontStyle = FontStyle.Bold;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = c;
                // estimated metrics (cap height ~0.07 * size * charSize, avg advance ~0.06)
                float cs = height / (64f * 0.075f);
                float estW = s.Length * 64f * 0.068f * cs;
                if (estW > maxWidth) cs *= maxWidth / estW;
                tm.characterSize = cs;
                var mr = go.GetComponent<MeshRenderer>();
                if (f != null) mr.sharedMaterial = f.material;
                mr.sortingOrder = _order++;
            }

            public Texture2D Render(Rect r, int pxW, int pxH, Color clear)
            {
                Flush();
                var rt = RenderTexture.GetTemporary(pxW, pxH, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
                var resolved = RenderTexture.GetTemporary(pxW, pxH, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
                var camGo = new GameObject("BackdropBakeCam");
                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                cam.orthographic = true;
                cam.targetTexture = rt;
                cam.orthographicSize = r.height * 0.5f;
                cam.aspect = r.width / r.height;
                cam.transform.position = new Vector3(r.center.x, r.center.y, -50f);
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 200f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = clear;
                cam.cullingMask = 1 << BakeLayer;
                cam.allowHDR = false;
                cam.allowMSAA = true;
                cam.useOcclusionCulling = false;
                Texture2D tex = null;
                var prev = RenderTexture.active;
                try
                {
                    cam.Render();
                    Graphics.Blit(rt, resolved);
                    RenderTexture.active = resolved;
                    tex = new Texture2D(pxW, pxH, TextureFormat.RGB24, false) { name = "Backdrop", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                    tex.ReadPixels(new Rect(0, 0, pxW, pxH), 0, 0, false);
                    tex.Apply(false, true);
                }
                finally
                {
                    RenderTexture.active = prev;
                    cam.targetTexture = null;
                    Object.Destroy(camGo);
                    RenderTexture.ReleaseTemporary(rt);
                    RenderTexture.ReleaseTemporary(resolved);
                }
                return tex;
            }

            public void Dispose()
            {
                if (Root != null) { Root.SetActive(false); Object.Destroy(Root); }
                foreach (var o in _trash) if (o != null) Object.Destroy(o);
                _trash.Clear();
            }
        }

        private static Font DefaultFont()
        {
            if (_font != null) return _font;
            try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { _font = null; }
            if (_font == null) try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { _font = null; }
            return _font;
        }

        private static void EnsureMaterials(float grainStrength)
        {
            var sh = Shader.Find("Sprites/Default");
            if (_plain == null) _plain = new Material(sh) { name = "BackdropPlain" };
            if (_grain == null || Mathf.Abs(_grainStrength - grainStrength) > 0.01f)
            {
                if (_grain != null) Object.Destroy(_grain);
                _grain = GrainTexture(256, grainStrength, 1234);
                _grainStrength = grainStrength;
            }
            if (_grainMat == null) _grainMat = new Material(sh) { name = "BackdropGrain" };
            _grainMat.mainTexture = _grain;
        }

        /// <summary>Tileable sand grain: value-noise fbm + bright/dark speckles + tiny pebbles. Mean ~0.92.</summary>
        public static Texture2D GrainTexture(int n, float strength, int seed)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "SandGrain", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 2 };
            var px = new Color[n * n];
            var rnd = new System.Random(seed);
            float[] lattice(int g)
            {
                var a = new float[g * g];
                for (int i = 0; i < a.Length; i++) a[i] = (float)rnd.NextDouble();
                return a;
            }
            int[] grids = { 4, 8, 16, 32, 64 };
            float[] amps = { 0.35f, 0.25f, 0.18f, 0.12f, 0.10f };
            var lat = new float[grids.Length][];
            for (int k = 0; k < grids.Length; k++) lat[k] = lattice(grids[k]);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = 0f;
                    for (int k = 0; k < grids.Length; k++)
                    {
                        int g = grids[k];
                        float fx = x * g / (float)n, fy = y * g / (float)n;
                        int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
                        float tx = fx - x0, ty = fy - y0;
                        tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
                        float a = lat[k][(y0 % g) * g + (x0 % g)], b = lat[k][(y0 % g) * g + ((x0 + 1) % g)];
                        float c = lat[k][((y0 + 1) % g) * g + (x0 % g)], d = lat[k][((y0 + 1) % g) * g + ((x0 + 1) % g)];
                        v += Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty) * amps[k];
                    }
                    // v ~ 0..1 centred 0.5
                    float g2 = 0.92f + (v - 0.5f) * 0.22f * strength;
                    px[y * n + x] = new Color(g2, g2 * 0.995f, g2 * 0.985f, 1f);
                }
            // speckles (single grains) and small pebbles
            int specks = (int)(n * n * 0.035f * (0.4f + strength));
            for (int i = 0; i < specks; i++)
            {
                int x = rnd.Next(n), y = rnd.Next(n);
                float d = rnd.NextDouble() < 0.55 ? -0.10f - (float)rnd.NextDouble() * 0.12f : 0.05f + (float)rnd.NextDouble() * 0.05f;
                var c = px[y * n + x];
                px[y * n + x] = new Color(Mathf.Clamp01(c.r + d), Mathf.Clamp01(c.g + d), Mathf.Clamp01(c.b + d * 1.1f), 1f);
            }
            int pebbles = (int)(18 * (0.5f + strength));
            for (int i = 0; i < pebbles; i++)
            {
                int cx = rnd.Next(n), cy = rnd.Next(n), r = 1 + rnd.Next(2);
                float d = -0.12f - (float)rnd.NextDouble() * 0.08f;
                for (int yy = -r; yy <= r; yy++)
                    for (int xx = -r; xx <= r; xx++)
                    {
                        if (xx * xx + yy * yy > r * r + 1) continue;
                        int X = (cx + xx + n) % n, Y = (cy + yy + n) % n;
                        float k = yy > 0 ? 0.6f : 1f; // lit top
                        var c = px[Y * n + X];
                        px[Y * n + X] = new Color(c.r + d * k, c.g + d * k, c.b + d * k * 1.1f, 1f);
                    }
            }
            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        // ------------------------------------------------------------------ colour helpers

        internal static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
        internal static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);
        internal static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, t);
        internal static float Rf(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
    }
}

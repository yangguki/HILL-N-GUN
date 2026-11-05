using System.Collections.Generic;
using UnityEngine;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.View3D
{
    /// <summary>
    /// Code-built 3D toon cover props (barrel, water trough, saloon swing door, wagon; crate fallback) with one mesh
    /// per damage stage (Intact / Cracked / Damaged / Destroyed rubble). Meshes are cached per (id, width, stage) and share
    /// the instanced toon material, so identical props draw with GPU instancing. View only: hit boxes stay in CoverInstance.
    /// Model space: +X along the cover width, +Y up, -Z faces the camera.
    /// </summary>
    public sealed class CoverProp3D
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();
        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        public readonly Transform Root;
        private readonly MeshFilter _filter;
        private readonly MeshRenderer _renderer;
        private readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();
        private readonly string _id;
        private readonly float _width;
        private string _visualKey;
        private Color _tint = Color.white;
        private CoverStage _stage = (CoverStage)(-1);
        private float _flash, _shake, _lastFlash = -1f;
        // scale preset: target drawn size (world units at perspective 1); <= 0 = mesh as built
        private float _targetW = -1f, _targetH = -1f, _minYScale = 0.25f;
        private Vector3 _sizeMul = Vector3.one;
        public Vector3 SizeMul => _sizeMul;

        public CoverProp3D(string id, float width, int layer, Material mat)
        {
            _id = id;
            _visualKey = id;
            _width = width;
            var go = new GameObject("Cover3D_" + id) { layer = layer };
            Root = go.transform;
            _filter = go.AddComponent<MeshFilter>();
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = mat;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        public void SetStage(CoverStage stage)
        {
            if (stage == _stage) return;
            _stage = stage;
            _filter.sharedMesh = GetMesh(_visualKey, _width, stage, _tint);
        }

        /// <summary>Mesh key for a cover look: the cover id itself (Default) or "v_&lt;Visual&gt;" (stage theme).</summary>
        public static string VisualKey(string coverId, CoverVisual v) => v == CoverVisual.Default ? coverId : "v_" + v;

        public string VisualKeyCurrent => _visualKey;

        /// <summary>Swap the look (stage theme) without touching the cover's footprint: meshes always fill _width.</summary>
        public void SetVisual(string key, Color tint)
        {
            if (string.IsNullOrEmpty(key)) key = _id;
            if (key == _visualKey && tint == _tint) return;
            _visualKey = key;
            _tint = tint;
            if ((int)_stage >= 0) _filter.sharedMesh = GetMesh(_visualKey, _width, _stage, _tint);
            RecomputeSize();
        }

        /// <summary>
        /// Scale preset size (view only): the prop is stretched so its on-screen footprint is width × height
        /// (height measured after the camera tilt, i.e. front face + visible top). Collision stays in CoverInstance.
        /// </summary>
        public void SetTargetSize(float width, float height, float minYScale)
        {
            _targetW = width;
            _targetH = height;
            _minYScale = minYScale;
            RecomputeSize();
        }

        private void RecomputeSize()
        {
            _sizeMul = ComputeSizeMul(GetMesh(_visualKey, _width, CoverStage.Intact, _tint).bounds.size, _width, _targetW, _targetH,
                Actor3DStage.Data.viewTiltDeg, _minYScale);
        }

        /// <summary>Per-axis model scale that makes a mesh of <paramref name="meshSize"/> draw targetW wide and targetH tall
        /// under a <paramref name="tiltDeg"/> view tilt. targetH &lt;= 0 keeps proportions (uniform targetW / builtWidth).</summary>
        public static Vector3 ComputeSizeMul(Vector3 meshSize, float builtWidth, float targetW, float targetH, float tiltDeg, float minYScale)
        {
            if (targetW <= 0f) return Vector3.one;
            if (targetH <= 0f || meshSize.x <= 1e-4f || meshSize.y <= 1e-4f)
            {
                float u = targetW / Mathf.Max(0.01f, builtWidth);
                return new Vector3(u, u, u);
            }
            float sx = targetW / meshSize.x;
            float sz = sx;
            float t = tiltDeg * Mathf.Deg2Rad;
            float sy = (targetH - meshSize.z * sz * Mathf.Sin(t)) / (meshSize.y * Mathf.Cos(t));
            sy = Mathf.Max(sy, minYScale * sx);
            return new Vector3(sx, sy, sz);
        }

        public void Hit(float flash, float shake) { _flash = Mathf.Max(_flash, flash); _shake = Mathf.Max(_shake, shake); }

        /// <summary>Base (ground contact) in 2D view space, uniform view scale, logic Y for depth.</summary>
        public void Place(Vector3 baseWorld, float scale, float logicY, float dt)
        {
            var d = Actor3DStage.Data;
            _shake = Mathf.Max(0f, _shake - dt * 4f);
            float jx = _shake > 0f ? Mathf.Sin(Time.time * 70f) * _shake * 0.08f : 0f;
            Root.position = new Vector3(baseWorld.x + jx, baseWorld.y, d.depthBase + logicY * d.depthPerTile - d.coverDepthPull);
            Root.rotation = Quaternion.Euler(-d.viewTiltDeg, 0f, 0f);
            Root.localScale = _sizeMul * scale;
            _flash = Mathf.Max(0f, _flash - dt * 6f);
            if (Mathf.Abs(_flash - _lastFlash) > 0.002f)
            {
                _lastFlash = _flash;
                _mpb.Clear();
                _mpb.SetFloat(FlashId, _flash);
                _renderer.SetPropertyBlock(_mpb);
            }
        }

        public void SetVisible(bool v) { if (Root != null && Root.gameObject.activeSelf != v) Root.gameObject.SetActive(v); }
        public void Destroy() { if (Root != null) Object.Destroy(Root.gameObject); }

        // ------------------------------------------------------------------ meshes

        public static Mesh GetMesh(string id, float width, CoverStage stage) => GetMesh(id, width, stage, Color.white);

        public static Mesh GetMesh(string id, float width, CoverStage stage, Color tint)
        {
            bool tinted = id == "v_Rock";
            string key = id + "|" + width.ToString("F2") + "|" + stage + (tinted ? "|" + ColorUtility.ToHtmlStringRGB(tint) : "");
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            var b = new ToonMeshBuilder();
            var rnd = new System.Random(key.GetHashCode());
            int s = (int)stage; // 0 intact .. 3 destroyed
            switch (id)
            {
                case "cover_barrel": Barrel(b, width, s, rnd); break;
                case "cover_trough": Trough(b, width, s, rnd); break;
                case "cover_swing_door": SwingDoor(b, width, s, rnd); break;
                case "cover_wagon": Wagon(b, width, s, rnd); break;
                case "cover_hay": HayBale(b, width, s, rnd); break;
                case "cover_crate": Crate(b, width, s, rnd); break;
                case "v_Barrel": Barrel(b, width, s, rnd); break;
                case "v_Trough": Trough(b, width, s, rnd); break;
                case "v_SwingDoor": SwingDoor(b, width, s, rnd); break;
                case "v_Wagon": Wagon(b, width, s, rnd); break;
                case "v_HayBale": HayBale(b, width, s, rnd); break;
                case "v_Rock": Rock(b, width, s, rnd, tint); break;
                case "v_Fence": Fence(b, width, s, rnd); break;
                case "v_CrateStack": CrateStack(b, width, s, rnd); break;
                default: Crate(b, width, s, rnd); break;
            }
            m = b.Build("cover_" + key);
            Cache[key] = m;
            return m;
        }

        private static CoverPalette P => Actor3DStage.Data.coverPalette ?? CoverPalette.Default;

        private static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
        private static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        /// <summary>Zig-zag crack on a face (no outline).</summary>
        private static void Crack(ToonMeshBuilder b, Vector3 start, float len, float zFace, System.Random r, int segs = 3)
        {
            float w0 = b.OutlineWeight;
            b.OutlineWeight = 0f;
            var p = start;
            for (int i = 0; i < segs; i++)
            {
                var q = p + new Vector3(R(r, -0.12f, 0.12f), -len / segs, 0f);
                var mid = (p + q) * 0.5f;
                float ang = Mathf.Atan2(q.x - p.x, p.y - q.y) * Mathf.Rad2Deg;
                b.Box(new Vector3(mid.x, mid.y, zFace), new Vector3(0.03f, (q - p).magnitude, 0.01f), P.crack, Quaternion.Euler(0f, 0f, ang));
                p = q;
            }
            b.OutlineWeight = w0;
        }

        /// <summary>Scattered planks/debris for the destroyed stage.</summary>
        private static void Rubble(ToonMeshBuilder b, float width, System.Random r, Color wood, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float x = R(r, -width * 0.5f, width * 0.5f), z = R(r, -0.35f, 0.35f);
                float len = R(r, 0.3f, 0.7f);
                b.Box(new Vector3(x, 0.05f + i * 0.012f, z), new Vector3(len, 0.06f, 0.14f), i % 3 == 0 ? Shade(wood, 0.75f) : wood,
                    Quaternion.Euler(R(r, -8f, 8f), R(r, 0f, 180f), R(r, -10f, 10f)));
            }
            for (int i = 0; i < 4; i++)
                b.Ellipsoid(new Vector3(R(r, -width * 0.45f, width * 0.45f), 0.02f, R(r, -0.3f, 0.3f)), new Vector3(R(r, 0.1f, 0.2f), 0.04f, R(r, 0.08f, 0.15f)), P.dust);
        }

        private static void Barrel(ToonMeshBuilder b, float width, int s, System.Random r)
        {
            var p = P;
            float rad = Mathf.Min(0.46f, width * 0.46f), h = 1.25f;
            if (s >= 3)
            {
                Rubble(b, width, r, p.wood, 6);
                b.Frustum(new Vector3(0.15f, 0f, 0.05f), new Vector3(0.15f, 0.06f, 0.05f), rad * 0.95f, rad * 0.95f, p.metal, 14, false);
                b.Frustum(new Vector3(-0.2f, 0f, -0.1f), new Vector3(-0.2f, 0.25f, -0.1f), rad * 0.8f, rad * 0.75f, Shade(p.wood, 0.85f), 10);
                return;
            }
            float top = s == 2 ? h * 0.68f : h;
            b.Frustum(Vector3.zero, new Vector3(0f, top * 0.5f, 0f), rad * 0.82f, rad, p.wood, 14, false);
            b.Frustum(new Vector3(0f, top * 0.5f, 0f), new Vector3(0f, top, 0f), rad, s == 2 ? rad * 0.96f : rad * 0.82f, p.wood, 14, true);
            // hoops
            foreach (float y in new[] { 0.12f, 0.5f, 0.88f })
                if (y * h < top - 0.05f)
                {
                    float k = 1f - Mathf.Abs(y * h - top * 0.5f) / (h * 0.5f) * 0.18f;
                    b.Frustum(new Vector3(0f, y * h - 0.04f, 0f), new Vector3(0f, y * h + 0.04f, 0f), rad * k * 1.03f, rad * k * 1.03f, p.metal, 14, false);
                }
            b.OutlineWeight = 0f;
            b.Disc(new Vector3(0f, top + 0.005f, 0f), Vector3.up, (s == 2 ? rad * 0.92f : rad * 0.78f), 0.01f, s == 2 ? p.crack : Shade(p.wood, 0.8f), 14);
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 15f) * Mathf.Deg2Rad;
                b.Box(new Vector3(Mathf.Sin(a) * rad * 0.99f, top * 0.5f, Mathf.Cos(a) * rad * 0.99f), new Vector3(0.015f, top * 0.8f, 0.015f), Shade(p.wood, 0.7f), Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f));
            }
            b.OutlineWeight = 1f;
            if (s >= 1) { Crack(b, new Vector3(-0.1f, top * 0.85f, 0f), top * 0.55f, -rad * 1.02f, r); }
            if (s >= 2)
            {
                Crack(b, new Vector3(0.18f, top * 0.7f, 0f), top * 0.5f, -rad * 1.02f, r);
                for (int i = 0; i < 5; i++)
                {
                    float a = (i * 72f + R(r, 0f, 30f)) * Mathf.Deg2Rad;
                    var basePt = new Vector3(Mathf.Sin(a) * rad * 0.9f, top, Mathf.Cos(a) * rad * 0.9f);
                    b.Cone(basePt, basePt + new Vector3(0f, R(r, 0.1f, 0.25f), 0f), 0.06f, p.wood, 4);
                }
                b.Box(new Vector3(rad + 0.15f, 0.04f, -0.15f), new Vector3(0.12f, 0.06f, 0.6f), p.wood, Quaternion.Euler(0f, 30f, 0f));
            }
        }

        private static void Trough(ToonMeshBuilder b, float width, int s, System.Random r)
        {
            var p = P;
            float L = width * 0.95f, D = 0.7f, legH = 0.22f, H = 0.72f;
            if (s >= 3) { Rubble(b, width, r, p.wood, 9); b.Ellipsoid(new Vector3(0.1f, 0.01f, 0f), new Vector3(L * 0.35f, 0.02f, 0.3f), Shade(p.water, 0.8f)); return; }
            float tilt = s == 2 ? 7f : 0f;
            var q = Quaternion.Euler(0f, 0f, tilt);
            Vector3 O(float x, float y, float z) => q * new Vector3(x, y, z) + new Vector3(0f, s == 2 ? -0.05f : 0f, 0f);
            // legs
            for (int ix = -1; ix <= 1; ix += 2)
                for (int iz = -1; iz <= 1; iz += 2)
                    if (!(s == 2 && ix > 0 && iz < 0))
                        b.Box(O(ix * (L * 0.5f - 0.12f), legH * 0.5f, iz * (D * 0.5f - 0.08f)), new Vector3(0.12f, legH, 0.12f), p.woodDark, q);
            // walls
            float frontH = s == 2 ? H * 0.55f : H;
            b.Box(O(0f, legH + 0.04f, 0f), new Vector3(L, 0.08f, D), p.woodDark, q);
            b.Box(O(0f, legH + frontH * 0.5f, -D * 0.5f + 0.04f), new Vector3(L, frontH, 0.08f), p.wood, q);
            b.Box(O(0f, legH + H * 0.5f, D * 0.5f - 0.04f), new Vector3(L, H, 0.08f), p.wood, q);
            b.Box(O(-L * 0.5f + 0.04f, legH + H * 0.5f, 0f), new Vector3(0.08f, H, D), Shade(p.wood, 0.92f), q);
            b.Box(O(L * 0.5f - 0.04f, legH + H * 0.5f, 0f), new Vector3(0.08f, H, D), Shade(p.wood, 0.92f), q);
            // rim + water
            b.Box(O(0f, legH + H + 0.03f, D * 0.5f - 0.04f), new Vector3(L + 0.06f, 0.06f, 0.12f), p.woodDark, q);
            if (s < 2) b.Box(O(0f, legH + H + 0.03f, -D * 0.5f + 0.04f), new Vector3(L + 0.06f, 0.06f, 0.12f), p.woodDark, q);
            b.OutlineWeight = 0f;
            b.Box(O(0f, legH + (s == 2 ? frontH - 0.06f : H - 0.12f), 0f), new Vector3(L - 0.16f, 0.02f, D - 0.16f), s == 2 ? Shade(p.water, 0.7f) : p.water, q);
            for (int i = 1; i < 3; i++)
                b.Box(O(0f, legH + frontH * i / 3f, -D * 0.5f - 0.002f), new Vector3(L * 0.98f, 0.02f, 0.01f), Shade(p.wood, 0.7f), q);
            // metal bands
            b.Box(O(-L * 0.3f, legH + H * 0.5f, -D * 0.5f - 0.004f), new Vector3(0.06f, Mathf.Min(H, frontH) * 0.96f, 0.012f), p.metal, q);
            b.Box(O(L * 0.3f, legH + H * 0.5f, -D * 0.5f - 0.004f), new Vector3(0.06f, Mathf.Min(H, frontH) * 0.96f, 0.012f), p.metal, q);
            b.OutlineWeight = 1f;
            if (s >= 1) { Crack(b, O(-L * 0.12f, legH + frontH * 0.95f, 0f), frontH * 0.8f, -D * 0.5f - 0.01f, r); Crack(b, O(L * 0.18f, legH + frontH * 0.9f, 0f), frontH * 0.6f, -D * 0.5f - 0.01f, r, 2); }
            if (s >= 2)
            {
                for (int i = 0; i < 4; i++) b.Cone(O(R(r, -L * 0.45f, L * 0.45f), legH + frontH, -D * 0.5f + 0.04f), O(R(r, -L * 0.45f, L * 0.45f), legH + frontH + R(r, 0.08f, 0.22f), -D * 0.5f + 0.04f), 0.05f, p.wood, 4);
                b.Box(new Vector3(L * 0.2f, 0.05f, -D * 0.5f - 0.25f), new Vector3(L * 0.5f, 0.07f, 0.16f), p.wood, Quaternion.Euler(0f, 12f, 4f));
                b.Ellipsoid(new Vector3(-L * 0.1f, 0.01f, -D * 0.5f - 0.3f), new Vector3(L * 0.3f, 0.015f, 0.18f), Shade(p.water, 0.8f));
            }
        }

        private static void SwingDoor(ToonMeshBuilder b, float width, int s, System.Random r)
        {
            var p = P;
            float W = width, postH = 1.7f, panelW = W * 0.5f - 0.1f, y0 = 0.35f, y1 = 1.35f;
            if (s >= 3)
            {
                b.Box(new Vector3(-W * 0.5f, 0.2f, 0f), new Vector3(0.14f, 0.4f, 0.14f), p.woodDark);
                b.Box(new Vector3(W * 0.5f, 0.12f, 0f), new Vector3(0.14f, 0.24f, 0.14f), p.woodDark);
                Rubble(b, width, r, p.door, 6);
                return;
            }
            // posts + header
            b.Box(new Vector3(-W * 0.5f, postH * 0.5f, 0f), new Vector3(0.14f, postH, 0.14f), p.woodDark);
            float rp = s == 2 ? postH * 0.7f : postH;
            b.Box(new Vector3(W * 0.5f, rp * 0.5f, 0f), new Vector3(0.14f, rp, 0.14f), p.woodDark);
            if (s < 2) b.Box(new Vector3(0f, postH - 0.06f, 0f), new Vector3(W + 0.2f, 0.12f, 0.16f), p.woodDark);
            for (int side = -1; side <= 1; side += 2)
            {
                // hinge at the post, panel swings slightly open; damage tilts / drops panels
                float open = side * 10f;
                float droop = 0f, missingTop = 0f;
                if (s == 1 && side > 0) droop = 8f;
                if (s == 2 && side < 0) droop = 32f;
                if (s == 2 && side > 0) missingTop = 0.45f;
                var hinge = new Vector3(side * (W * 0.5f - 0.07f), y1, 0f);
                var q = Quaternion.Euler(0f, open, side * -droop);
                Vector3 Pt(float x, float y) => hinge + q * new Vector3(-side * x, y - y1, 0f);
                float top = y1 - missingTop;
                b.Box(Pt(panelW * 0.5f, (y0 + top) * 0.5f), new Vector3(panelW, top - y0, 0.06f), p.door, q);
                // louvre slats + rounded top
                b.OutlineWeight = 0f;
                for (float y = y0 + 0.12f; y < top - 0.08f; y += 0.13f)
                    b.Box(Pt(panelW * 0.5f, y) + q * new Vector3(0f, 0f, -0.035f), new Vector3(panelW * 0.8f, 0.03f, 0.012f), Shade(p.door, 0.72f), q);
                b.OutlineWeight = 1f;
                if (missingTop <= 0f)
                    b.Ellipsoid(Pt(panelW * 0.5f, top), new Vector3(panelW * 0.5f, 0.12f, 0.03f), p.door, q);
                b.Box(Pt(panelW * 0.5f, y0 + 0.03f), new Vector3(panelW, 0.06f, 0.08f), Shade(p.door, 0.8f), q);
                if (s >= 1) Crack(b, Pt(panelW * 0.4f, top - 0.05f) , (top - y0) * 0.7f, (q * new Vector3(0, 0, -0.035f)).z + hinge.z, r, 3);
                if (s == 2 && side > 0)
                    for (int i = 0; i < 3; i++) b.Cone(Pt(panelW * (0.2f + 0.3f * i), top), Pt(panelW * (0.25f + 0.3f * i), top + R(r, 0.08f, 0.18f)), 0.05f, p.door, 4);
            }
            if (s == 2) Rubble(b, width * 0.6f, r, p.door, 2);
        }

        private static void Wagon(ToonMeshBuilder b, float width, int s, System.Random r)
        {
            var p = P;
            float L = width * 0.92f, D = 0.95f, bedY = 0.62f, wheelR = 0.42f;
            if (s >= 3)
            {
                b.Box(new Vector3(0f, 0.12f, 0f), new Vector3(L * 0.8f, 0.12f, D * 0.8f), p.woodDark, Quaternion.Euler(0f, 4f, 3f));
                Wheel(b, new Vector3(-L * 0.42f, 0.05f, -D * 0.4f), wheelR, Quaternion.Euler(8f, 20f, 0f), 0);
                Rubble(b, width, r, p.wood, 8);
                return;
            }
            float tilt = s == 2 ? -9f : 0f;
            var q = Quaternion.Euler(0f, 0f, tilt);
            Vector3 O(float x, float y, float z) => q * new Vector3(x, y, z) + new Vector3(0f, s == 2 ? -0.08f : 0f, 0f);
            // wheels (front face toward camera = -Z)
            for (int ix = -1; ix <= 1; ix += 2)
                for (int iz = -1; iz <= 1; iz += 2)
                {
                    if (s == 2 && ix > 0 && iz < 0) continue; // lost wheel
                    var c = new Vector3(ix * L * 0.32f, wheelR, iz * (D * 0.5f + 0.05f));
                    if (s == 2 && ix > 0) c.y -= 0.1f;
                    Wheel(b, c, wheelR, Quaternion.Euler(90f, 0f, 0f), s);
                }
            if (s == 2) Wheel(b, new Vector3(L * 0.45f, 0.05f, -D * 0.5f - 0.35f), wheelR, Quaternion.Euler(6f, 25f, 0f), 2); // fallen wheel lying flat
            // bed + sideboards
            b.Box(O(0f, bedY, 0f), new Vector3(L, 0.14f, D), p.woodDark, q);
            float boardH = 0.42f;
            float frontBoard = s == 2 ? boardH * 0.45f : boardH;
            b.Box(O(0f, bedY + 0.07f + frontBoard * 0.5f, -D * 0.5f + 0.04f), new Vector3(L, frontBoard, 0.08f), p.wood, q);
            b.Box(O(0f, bedY + 0.07f + boardH * 0.5f, D * 0.5f - 0.04f), new Vector3(L, boardH, 0.08f), p.wood, q);
            b.Box(O(-L * 0.5f + 0.04f, bedY + 0.07f + boardH * 0.5f, 0f), new Vector3(0.08f, boardH, D), Shade(p.wood, 0.92f), q);
            b.Box(O(L * 0.5f - 0.04f, bedY + 0.07f + boardH * 0.5f, 0f), new Vector3(0.08f, boardH, D), Shade(p.wood, 0.92f), q);
            b.OutlineWeight = 0f;
            for (int i = 1; i < 3; i++) b.Box(O(0f, bedY + 0.07f + frontBoard * i / 3f, -D * 0.5f - 0.002f), new Vector3(L * 0.98f, 0.02f, 0.01f), Shade(p.wood, 0.7f), q);
            for (int i = -1; i <= 1; i++) b.Box(O(i * L * 0.33f, bedY + 0.07f + frontBoard * 0.5f, -D * 0.5f - 0.004f), new Vector3(0.05f, frontBoard * 0.95f, 0.012f), p.metal, q);
            b.OutlineWeight = 1f;
            // tongue + cargo
            b.Box(O(L * 0.5f + 0.45f, bedY - 0.08f, 0f), new Vector3(0.9f, 0.07f, 0.08f), p.woodDark, q * Quaternion.Euler(0f, 0f, -12f));
            if (s < 2)
            {
                b.Ellipsoid(O(-L * 0.22f, bedY + 0.34f, 0.05f), new Vector3(0.3f, 0.24f, 0.26f), p.sack, q);
                b.Ellipsoid(O(-L * 0.02f, bedY + 0.3f, 0.12f), new Vector3(0.26f, 0.2f, 0.24f), Shade(p.sack, 0.92f), q);
                b.Box(O(L * 0.25f, bedY + 0.32f, 0.08f), new Vector3(0.45f, 0.42f, 0.45f), Shade(p.wood, 1.08f), q * Quaternion.Euler(0f, 12f, 0f));
            }
            if (s >= 1) { Crack(b, O(-L * 0.3f, bedY + 0.07f + frontBoard, 0f), frontBoard * 0.9f, -D * 0.5f - 0.01f, r, 2); Crack(b, O(L * 0.1f, bedY + 0.07f + frontBoard, 0f), frontBoard * 0.9f, -D * 0.5f - 0.01f, r, 2); }
            if (s >= 2)
            {
                for (int i = 0; i < 5; i++) b.Cone(O(R(r, -L * 0.45f, L * 0.45f), bedY + 0.07f + frontBoard, -D * 0.5f + 0.04f), O(R(r, -L * 0.45f, L * 0.45f), bedY + 0.07f + frontBoard + R(r, 0.08f, 0.2f), -D * 0.5f + 0.04f), 0.05f, p.wood, 4);
                b.Ellipsoid(O(-L * 0.15f, bedY + 0.2f, 0.1f), new Vector3(0.3f, 0.14f, 0.26f), p.sack, q);
                Rubble(b, width * 0.7f, r, p.wood, 3);
            }
        }

        private static void Wheel(ToonMeshBuilder b, Vector3 c, float rad, Quaternion q, int stage)
        {
            var p = P;
            var axis = q * Vector3.up;
            b.Frustum(c - axis * 0.04f, c + axis * 0.04f, rad, rad, p.woodDark, 18, false);
            b.Frustum(c - axis * 0.045f, c + axis * 0.045f, rad * 0.82f, rad * 0.82f, Shade(p.wood, 0.6f), 18, true);
            b.Frustum(c - axis * 0.07f, c + axis * 0.07f, rad * 0.16f, rad * 0.16f, p.metal, 10, true);
            int spokes = stage >= 2 ? 5 : 8;
            for (int i = 0; i < spokes; i++)
            {
                var dir = q * (Quaternion.Euler(0f, i * 360f / 8f, 0f) * Vector3.forward);
                b.Box(c + dir * rad * 0.45f, new Vector3(0.05f, 0.05f, rad * 0.78f), p.wood, Quaternion.LookRotation(dir, axis));
            }
        }

        private static void Crate(ToonMeshBuilder b, float width, int s, System.Random r)
        {
            var p = P;
            if (s >= 3) { Rubble(b, width, r, p.wood, 7); return; }
            float h = s == 2 ? 0.7f : 1.0f;
            b.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(width * 0.95f, h, 0.8f), p.wood);
            // lid rim + corner posts so the crate reads as a box, not a flat panel
            if (s < 2) b.Box(new Vector3(0f, h + 0.03f, 0f), new Vector3(width * 0.97f, 0.07f, 0.82f), Shade(p.wood, 1.15f));
            b.OutlineWeight = 0f;
            b.Box(new Vector3(-width * 0.455f, h * 0.5f, -0.402f), new Vector3(0.08f, h, 0.01f), p.woodDark);
            b.Box(new Vector3(width * 0.455f, h * 0.5f, -0.402f), new Vector3(0.08f, h, 0.01f), p.woodDark);
            b.Box(new Vector3(0f, h - 0.05f, -0.402f), new Vector3(width * 0.95f, 0.07f, 0.01f), p.woodDark);
            b.Box(new Vector3(0f, 0.05f, -0.402f), new Vector3(width * 0.95f, 0.07f, 0.01f), p.woodDark);
            // X brace corner to corner (stays inside the crate face at every height)
            float bw = width * 0.86f, bh = h * 0.84f, blen = Mathf.Sqrt(bw * bw + bh * bh), bang = Mathf.Atan2(bh, bw) * Mathf.Rad2Deg;
            b.Box(new Vector3(0f, h * 0.5f, -0.401f), new Vector3(blen, 0.07f, 0.01f), p.woodDark, Quaternion.Euler(0f, 0f, bang));
            b.Box(new Vector3(0f, h * 0.5f, -0.401f), new Vector3(blen, 0.07f, 0.01f), p.woodDark, Quaternion.Euler(0f, 0f, -bang));
            b.OutlineWeight = 1f;
            if (s >= 1) Crack(b, new Vector3(0.1f, h * 0.9f, 0f), h * 0.7f, -0.41f, r);
        }
    
        // ------------------------------------------------------------------ stage-theme looks (same footprint)

        private static void HayBale(ToonMeshBuilder b, float width, int s, System.Random r)
        {
            var p = P;
            if (s >= 3)
            {
                for (int i = 0; i < 9; i++)
                    b.Ellipsoid(new Vector3(R(r, -width * 0.4f, width * 0.4f), 0.03f, R(r, -0.3f, 0.3f)), new Vector3(R(r, 0.1f, 0.22f), 0.05f, R(r, 0.08f, 0.16f)), i % 3 == 0 ? p.hayDark : p.hay, null, 7, 4);
                b.Box(new Vector3(0f, 0.14f, 0f), new Vector3(width * 0.45f, 0.28f, 0.5f), p.hay, Quaternion.Euler(0f, 12f, 5f));
                return;
            }
            int n = width > 1.5f ? 2 : 1;
            float L = width * 0.92f / n - (n > 1 ? 0.04f : 0f), D = 0.7f;
            for (int k = 0; k < n; k++)
            {
                float cx = n == 1 ? 0f : (k == 0 ? -1f : 1f) * (L * 0.5f + 0.02f);
                // bottom bale + a smaller bale stacked on top (intact / cracked); damaged loses the top one
                Bale(b, r, new Vector3(cx, 0f, 0f), L, 0.62f, D, Quaternion.identity, s);
                if (s < 2) Bale(b, r, new Vector3(cx + (k == 0 ? 0.04f : -0.04f), 0.62f, 0.04f), L * 0.78f, 0.52f, D * 0.85f, Quaternion.Euler(0f, k == 0 ? 7f : -6f, 0f), s);
                else
                    for (int i = 0; i < 5; i++)
                        b.Ellipsoid(new Vector3(cx + R(r, -L * 0.4f, L * 0.4f), 0.66f + R(r, 0f, 0.05f), R(r, -D * 0.3f, D * 0.2f)), new Vector3(R(r, 0.08f, 0.16f), 0.05f, 0.1f), Shade(p.hay, 1.05f), null, 7, 4);
            }
            if (s == 2)
                for (int i = 0; i < 5; i++)
                    b.Ellipsoid(new Vector3(R(r, -width * 0.4f, width * 0.4f), 0.02f, R(r, -0.45f, -0.25f)), new Vector3(R(r, 0.08f, 0.16f), 0.03f, 0.08f), p.hay, null, 7, 4);
        }

        private static void Bale(ToonMeshBuilder b, System.Random r, Vector3 o, float L, float H, float D, Quaternion q, int s)
        {
            var p = P;
            Vector3 O(float x, float y, float z) => o + q * new Vector3(x, y, z);
            b.Box(O(0f, H * 0.5f, 0f), new Vector3(L, H, D), p.hay, q);
            // rounded top edge + darker cut ends
            b.Frustum(O(-L * 0.5f + 0.02f, H - 0.06f, -D * 0.5f + 0.06f), O(L * 0.5f - 0.02f, H - 0.06f, -D * 0.5f + 0.06f), 0.09f, 0.09f, Shade(p.hay, 1.06f), 8, false);
            b.OutlineWeight = 0f;
            b.Box(O(-L * 0.5f - 0.004f, H * 0.5f, 0f), new Vector3(0.01f, H * 0.94f, D * 0.94f), p.hayDark, q);
            b.Box(O(L * 0.5f + 0.004f, H * 0.5f, 0f), new Vector3(0.01f, H * 0.94f, D * 0.94f), p.hayDark, q);
            for (int i = 0; i < 9; i++)
            {
                float y = R(r, 0.06f, H - 0.08f);
                b.Box(O(R(r, -L * 0.25f, L * 0.25f), y, -D * 0.5f - 0.004f), new Vector3(L * R(r, 0.25f, 0.6f), 0.022f, 0.01f), i % 3 == 0 ? Shade(p.hayDark, 0.85f) : p.hayDark, q);
            }
            foreach (float f in new[] { -0.28f, 0.28f })
            {
                b.Box(O(L * f, H * 0.5f, -D * 0.5f - 0.008f), new Vector3(0.04f, H * 0.98f, 0.012f), p.twine, q);
                b.Box(O(L * f, H + 0.008f, 0f), new Vector3(0.04f, 0.012f, D * 0.98f), p.twine, q);
            }
            b.OutlineWeight = 1f;
            int tufts = 2 + s * 3;
            for (int i = 0; i < tufts; i++)
            {
                var basePt = O(R(r, -L * 0.42f, L * 0.42f), H - 0.02f, R(r, -D * 0.4f, 0f));
                b.Cone(basePt, basePt + new Vector3(R(r, -0.06f, 0.06f), R(r, 0.06f, 0.12f + s * 0.05f), R(r, -0.05f, 0.02f)), 0.025f, Shade(p.hay, 1.1f), 3);
            }
            if (s >= 1) Crack(b, O(R(r, -L * 0.2f, L * 0.2f), H * 0.9f, 0f), H * 0.6f, (o + q * new Vector3(0f, 0f, -D * 0.5f - 0.012f)).z, r, 2);
        }

        private static void Rock(ToonMeshBuilder b, float width, int s, System.Random r, Color tint)
        {
            var p = P;
            var stone = tint == Color.white ? p.stone : new Color(tint.r, tint.g, tint.b, 1f);
            var dark = Shade(stone, 0.72f);
            float hw = width * 0.46f;
            if (s >= 3)
            {
                for (int i = 0; i < 7; i++)
                {
                    float ry = R(r, 0.06f, 0.12f);
                    b.Ellipsoid(new Vector3(R(r, -hw * 0.85f, hw * 0.85f), ry * 0.9f, R(r, -0.3f, 0.3f)), new Vector3(R(r, 0.1f, 0.2f), ry, R(r, 0.08f, 0.16f)), i % 2 == 0 ? stone : dark, Quaternion.Euler(0f, R(r, 0f, 180f), 0f), 6, 4);
                }
                return;
            }
            float H = s == 2 ? 0.72f : 1.05f;
            // main boulder + side boulder (faceted, low poly)
            b.Ellipsoid(new Vector3(-hw * 0.18f, H * 0.5f, 0f), new Vector3(hw * 0.78f, H * 0.5f, 0.42f), stone, Quaternion.Euler(0f, 18f, R(r, -6f, 6f)), 7, 5);
            b.Ellipsoid(new Vector3(hw * 0.52f, H * 0.31f, -0.06f), new Vector3(hw * 0.46f, H * 0.3f, 0.36f), Shade(stone, 0.9f), Quaternion.Euler(0f, -24f, 8f), 6, 4);
            b.Ellipsoid(new Vector3(-hw * 0.22f, H * 0.82f, -0.04f), new Vector3(hw * 0.42f, H * 0.14f, 0.26f), Mix(stone, Color.white, 0.14f), Quaternion.Euler(0f, 10f, 0f), 6, 4);
            if (width > 1.5f) b.Ellipsoid(new Vector3(-hw * 0.82f, H * 0.18f, -0.1f), new Vector3(hw * 0.2f, H * 0.18f, 0.24f), dark, Quaternion.Euler(0f, 30f, 0f), 6, 4);
            b.OutlineWeight = 0f;
            for (int i = 0; i < 3; i++)
                b.Box(new Vector3(-hw * 0.18f + R(r, -hw * 0.4f, hw * 0.4f), H * R(r, 0.25f, 0.65f), -0.41f), new Vector3(hw * R(r, 0.25f, 0.5f), 0.02f, 0.01f), dark, Quaternion.Euler(0f, 0f, R(r, -12f, 12f)));
            b.OutlineWeight = 1f;
            if (s >= 1) Crack(b, new Vector3(-hw * 0.1f, H * 0.85f, 0f), H * 0.65f, -0.43f, r, 3);
            if (s >= 2)
            {
                Crack(b, new Vector3(hw * 0.3f, H * 0.7f, 0f), H * 0.5f, -0.43f, r, 2);
                for (int i = 0; i < 4; i++)
                    b.Ellipsoid(new Vector3(R(r, -hw * 0.8f, hw * 0.8f), 0.05f, R(r, -0.5f, -0.3f)), new Vector3(R(r, 0.06f, 0.12f), 0.05f, 0.07f), dark, null, 6, 4);
            }
        }

        private static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, t);

        private static void Fence(ToonMeshBuilder b, float width, int s, System.Random r)
        {
            var p = P;
            float W = width * 0.92f, H = 1.15f;
            if (s >= 3)
            {
                b.Box(new Vector3(-W * 0.5f + 0.06f, 0.15f, 0f), new Vector3(0.12f, 0.3f, 0.12f), p.woodDark);
                b.Box(new Vector3(W * 0.5f - 0.06f, 0.1f, 0f), new Vector3(0.12f, 0.2f, 0.12f), p.woodDark);
                Rubble(b, width * 0.8f, r, p.wood, 6);
                return;
            }
            float[] postX = { -W * 0.5f + 0.06f, 0f, W * 0.5f - 0.06f };
            for (int i = 0; i < 3; i++)
            {
                float ph = s == 2 && i == 1 ? H * 0.55f : H * (0.96f + 0.04f * i % 2);
                b.Box(new Vector3(postX[i], ph * 0.5f, 0f), new Vector3(0.12f, ph, 0.12f), p.woodDark, Quaternion.Euler(0f, 0f, R(r, -2f, 2f)));
                b.Cone(new Vector3(postX[i], ph, 0f), new Vector3(postX[i], ph + 0.08f, 0f), 0.07f, p.woodDark, 4);
            }
            foreach (float y in new[] { 0.38f, 0.78f })
            {
                if (s == 2 && y > 0.5f)
                {
                    // top rail snapped: two halves hanging
                    b.Box(new Vector3(-W * 0.26f, H * y - 0.06f, -0.08f), new Vector3(W * 0.5f, 0.1f, 0.05f), p.wood, Quaternion.Euler(0f, 0f, -10f));
                    b.Box(new Vector3(W * 0.3f, H * y - 0.14f, -0.08f), new Vector3(W * 0.42f, 0.1f, 0.05f), p.wood, Quaternion.Euler(0f, 0f, 16f));
                    continue;
                }
                b.Box(new Vector3(0f, H * y, -0.08f), new Vector3(W, 0.11f, 0.05f), p.wood, Quaternion.Euler(0f, 0f, R(r, -1.5f, 1.5f)));
            }
            // cross brace in the bays
            b.Box(new Vector3(-W * 0.25f, H * 0.58f, -0.11f), new Vector3(W * 0.5f, 0.07f, 0.03f), Shade(p.wood, 0.9f), Quaternion.Euler(0f, 0f, 28f));
            if (s >= 1) Crack(b, new Vector3(W * 0.2f, H * 0.82f, 0f), 0.2f, -0.11f, r, 2);
            if (s == 2) Rubble(b, width * 0.5f, r, p.wood, 2);
        }

        private static void CrateStack(ToonMeshBuilder b, float width, int s, System.Random r)
        {
            var p = P;
            if (s >= 3) { Rubble(b, width, r, p.wood, 9); return; }
            float cw = width * 0.46f, h = s == 2 ? 0.62f : 0.78f;
            for (int k = -1; k <= 1; k += 2)
            {
                float cx = k * (cw * 0.5f + 0.02f);
                var q = Quaternion.Euler(0f, k * 3f, 0f);
                b.Box(new Vector3(cx, h * 0.5f, 0f), new Vector3(cw * 0.96f, h, 0.75f), k < 0 ? p.wood : Shade(p.wood, 1.06f), q);
                b.OutlineWeight = 0f;
                b.Box(new Vector3(cx, h * 0.5f, -0.38f), new Vector3(cw * 1.02f, 0.06f, 0.01f), p.woodDark, Quaternion.Euler(0f, 0f, k * 30f));
                b.Box(new Vector3(cx, h - 0.04f, -0.38f), new Vector3(cw * 0.96f, 0.06f, 0.01f), p.woodDark);
                b.Box(new Vector3(cx, 0.04f, -0.38f), new Vector3(cw * 0.96f, 0.06f, 0.01f), p.woodDark);
                b.OutlineWeight = 1f;
                if (s >= 1) Crack(b, new Vector3(cx + 0.05f, h * 0.9f, 0f), h * 0.6f, -0.39f, r, 2);
            }
            if (s < 2)
            {
                float tw = width * 0.38f, th = 0.62f;
                b.Box(new Vector3(-width * 0.06f, h + th * 0.5f, 0.04f), new Vector3(tw, th, 0.62f), Shade(p.wood, 1.1f), Quaternion.Euler(0f, 8f, 0f));
                b.OutlineWeight = 0f;
                b.Box(new Vector3(-width * 0.06f, h + th * 0.5f, -0.28f), new Vector3(tw * 1.02f, 0.06f, 0.01f), p.woodDark, Quaternion.Euler(0f, 8f, -32f));
                b.OutlineWeight = 1f;
            }
            else Rubble(b, width * 0.7f, r, p.wood, 3);
        }
    }
}

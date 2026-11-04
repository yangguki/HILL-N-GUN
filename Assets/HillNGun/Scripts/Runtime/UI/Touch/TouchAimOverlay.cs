using System.Collections.Generic;
using UnityEngine;
using HillNGun.Battlefield;
using HillNGun.Core;
using HillNGun.Characters;
using HillNGun.Data;
using HillNGun.Input;

namespace HillNGun.UI.Touch
{
    /// <summary>
    /// World-space aim guides for the touch controls (view only, one dynamic mesh): attack aim strip (or a fan for spread
    /// weapons, gold while charging), grenade landing disc + radius ring + throw arc dots, skill target ring.
    /// Built in logic space and mapped vertex-by-vertex through FieldView.ToWorld, so the perspective matches the field.
    /// </summary>
    public class TouchAimOverlay : MonoBehaviour
    {
        private Mesh _mesh;
        private MeshRenderer _mr;
        private readonly List<Vector3> _v = new List<Vector3>(512);
        private readonly List<Color32> _c = new List<Color32>(512);
        private readonly List<int> _t = new List<int>(1024);
        private static Material _mat;

        /// <summary>Last frame drew: bit 1 aim, 2 grenade, 4 skill (smoke / tests).</summary>
        public int LastMask { get; private set; }
        public float LastAimLength { get; private set; }
        public float LastGrenadeRadius { get; private set; }

        public static TouchAimOverlay Create(Transform parent)
        {
            var go = new GameObject("TouchAimOverlay");
            go.transform.SetParent(parent, false);
            return go.AddComponent<TouchAimOverlay>();
        }

        private void Awake()
        {
            _mesh = new Mesh { name = "TouchAim" };
            _mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _mr = gameObject.AddComponent<MeshRenderer>();
            if (_mat == null) _mat = new Material(Shader.Find("Sprites/Default")) { name = "TouchAim_shared" };
            _mr.sharedMaterial = _mat;
            _mr.sortingOrder = FieldView.ProjectileOrder - 10;
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
            _mr.enabled = false;
        }

        public void Sync(TouchControlRouter r, CharacterStateMachine c, TouchControlsData d)
        {
            _v.Clear(); _c.Clear(); _t.Clear();
            int mask = 0;
            if (r != null && c != null && d != null && c.Data != null && c.OverallState == CharacterOverallState.Active)
            {
                Vector2 p = c.Position;
                var w = c.Data.weapon;
                if (r.AttackActive && r.AttackAiming && w != null)
                {
                    mask |= 1;
                    Color col = c.IsCharging ? d.chargeLineColor : d.aimLineColor;
                    float range = w.range;
                    LastAimLength = range;
                    if (w.projectilesPerShot > 1 && w.spreadDeg > 0.5f) Fan(p, r.AimDir, range, w.spreadDeg, col);
                    else Strip(p, r.AimDir, range, Mathf.Max(d.aimLineWidth, w.projectileRadius * 2f), col);
                }
                if (r.GrenadeActive && r.GrenadeAiming)
                {
                    mask |= 2;
                    float rad = c.Data.grenade != null ? c.Data.grenade.radius : 1.5f;
                    LastGrenadeRadius = rad;
                    Vector2 tgt = r.GrenadeTarget;
                    Disc(tgt, rad, d.grenadeAreaColor);
                    Ring(tgt, rad, 0.09f, new Color(d.grenadeAreaColor.r, d.grenadeAreaColor.g, d.grenadeAreaColor.b, 0.95f));
                    Ring(tgt, 0.22f, 0.07f, new Color(1f, 1f, 1f, 0.9f));
                    ArcDots(p, tgt, new Color(1f, 0.95f, 0.8f, 0.85f));
                }
                if (r.SkillActive && r.SkillAiming)
                {
                    mask |= 4;
                    float rad = c.Data.skill != null ? Mathf.Max(0.6f, c.Data.skill.GetParam("radius", 1.2f)) : 1f;
                    Ring(r.SkillTarget, rad, 0.08f, d.skillColor);
                    Disc(r.SkillTarget, rad, new Color(d.skillColor.r, d.skillColor.g, d.skillColor.b, 0.2f));
                }
            }
            LastMask = mask;
            _mesh.Clear();
            if (_v.Count > 0)
            {
                _mesh.SetVertices(_v);
                _mesh.SetColors(_c);
                _mesh.SetTriangles(_t, 0);
                _mesh.bounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f));
            }
            _mr.enabled = _v.Count > 0;
        }

        // ---- primitives (logic space in, world space out) ----
        private int V(Vector2 logic, Color col) { _v.Add(FieldView.ToWorld(logic)); _c.Add(col); return _v.Count - 1; }

        private void Strip(Vector2 from, Vector2 dir, float len, float width, Color col)
        {
            dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.up;
            Vector2 side = new Vector2(-dir.y, dir.x) * (width * 0.5f);
            Vector2 start = from + dir * 0.35f;
            Vector2 end = from + dir * len;
            Color edge = new Color(col.r, col.g, col.b, col.a * 0.35f);
            // body: bright centre, soft edges (3 lanes)
            int a0 = V(start - side, edge), a1 = V(start - side * 0.35f, col), a2 = V(start + side * 0.35f, col), a3 = V(start + side, edge);
            int b0 = V(end - side, edge), b1 = V(end - side * 0.35f, col), b2 = V(end + side * 0.35f, col), b3 = V(end + side, edge);
            Q(a0, b0, b1, a1); Q(a1, b1, b2, a2); Q(a2, b2, b3, a3);
            // arrow head
            Color tip = new Color(col.r, col.g, col.b, Mathf.Min(1f, col.a + 0.3f));
            int t0 = V(end - side * 1.6f, tip), t1 = V(end + dir * width * 1.4f, tip), t2 = V(end + side * 1.6f, tip);
            _t.Add(t0); _t.Add(t1); _t.Add(t2);
        }

        private void Fan(Vector2 from, Vector2 dir, float len, float spreadDeg, Color col)
        {
            const int seg = 14;
            float half = spreadDeg * 0.5f;
            int centre = V(from + dir.normalized * 0.3f, col);
            Color rim = new Color(col.r, col.g, col.b, col.a * 0.55f);
            int prev = -1;
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)seg);
                Vector2 d = Rotate(dir.normalized, a);
                int cur = V(from + d * len, rim);
                if (prev >= 0) { _t.Add(centre); _t.Add(prev); _t.Add(cur); }
                prev = cur;
            }
            Ring(from, len, 0f, default, -half, half, dir, 0.07f, new Color(col.r, col.g, col.b, Mathf.Min(1f, col.a + 0.3f)));
        }

        private void Disc(Vector2 c, float r, Color col)
        {
            const int seg = 40;
            int centre = V(c, col);
            int first = -1, prev = -1;
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                int cur = V(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col);
                if (prev >= 0) { _t.Add(centre); _t.Add(prev); _t.Add(cur); } else first = cur;
                prev = cur;
            }
            _t.Add(centre); _t.Add(prev); _t.Add(first);
        }

        private void Ring(Vector2 c, float r, float thick, Color col)
        {
            const int seg = 48;
            int p0 = -1, p1 = -1, f0 = -1, f1 = -1;
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                Vector2 u = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                int i0 = V(c + u * (r - thick), col), i1 = V(c + u * (r + thick), col);
                if (p0 >= 0) Q(p0, i0, i1, p1); else { f0 = i0; f1 = i1; }
                p0 = i0; p1 = i1;
            }
            Q(p0, f0, f1, p1);
        }

        // arc segment of a ring between two angles around dir
        private void Ring(Vector2 c, float r, float _, Color __, float fromDeg, float toDeg, Vector2 dir, float thick, Color col)
        {
            const int seg = 14;
            int p0 = -1, p1 = -1;
            for (int i = 0; i <= seg; i++)
            {
                Vector2 u = Rotate(dir.normalized, Mathf.Lerp(fromDeg, toDeg, i / (float)seg));
                int i0 = V(c + u * (r - thick), col), i1 = V(c + u * (r + thick), col);
                if (p0 >= 0) Q(p0, i0, i1, p1);
                p0 = i0; p1 = i1;
            }
        }

        private void ArcDots(Vector2 from, Vector2 to, Color col)
        {
            float dist = Vector2.Distance(from, to);
            int n = Mathf.Clamp(Mathf.RoundToInt(dist * 1.6f), 4, 16);
            float h = Mathf.Clamp(dist * 0.28f, 0.6f, 2.4f);
            for (int i = 1; i < n; i++)
            {
                float t = i / (float)n;
                Vector2 p = Vector2.Lerp(from, to, t) + new Vector2(0f, 4f * h * t * (1f - t));
                float s = 0.09f + 0.05f * Mathf.Sin(t * Mathf.PI);
                int a = V(p + new Vector2(-s, -s), col), b = V(p + new Vector2(-s, s), col), cc = V(p + new Vector2(s, s), col), d = V(p + new Vector2(s, -s), col);
                Q(a, b, cc, d);
            }
        }

        private void Q(int a, int b, int c, int d) { _t.Add(a); _t.Add(b); _t.Add(c); _t.Add(a); _t.Add(c); _t.Add(d); }

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }
    }
}

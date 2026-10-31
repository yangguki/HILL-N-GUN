using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.View3D
{
    /// <summary>
    /// Tiny procedural mesh kit for the stylized low-poly actors: ellipsoids, boxes, frustums/capsules and cones,
    /// all vertex-coloured. Build() welds smoothed normals into TANGENT.xyz for the inverted-hull outline
    /// (TANGENT.w = per-part outline weight). Pure geometry: no gameplay, no assets.
    /// </summary>
    public class ToonMeshBuilder
    {
        private readonly List<Vector3> _v = new List<Vector3>(512);
        private readonly List<Vector3> _n = new List<Vector3>(512);
        private readonly List<Color> _c = new List<Color>(512);
        private readonly List<float> _w = new List<float>(512);
        private readonly List<int> _t = new List<int>(1536);

        public int VertexCount => _v.Count;
        public bool IsEmpty => _v.Count == 0;

        /// <summary>Outline weight applied to parts added from now on (1 = full outline, 0 = none).</summary>
        public float OutlineWeight = 1f;

        private int Add(Vector3 p, Vector3 n, Color c)
        {
            _v.Add(p); _n.Add(n); _c.Add(c); _w.Add(OutlineWeight);
            return _v.Count - 1;
        }

        /// <summary>Faceted-but-smooth ellipsoid (lat/long sphere scaled by radii).</summary>
        public void Ellipsoid(Vector3 center, Vector3 radii, Color color, Quaternion? rot = null, int seg = 12, int rings = 8)
        {
            var q = rot ?? Quaternion.identity;
            int start = _v.Count;
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                float sy = Mathf.Cos(phi), sr = Mathf.Sin(phi);
                for (int s = 0; s <= seg; s++)
                {
                    float th = 2f * Mathf.PI * s / seg;
                    var unit = new Vector3(sr * Mathf.Sin(th), sy, sr * Mathf.Cos(th));
                    var p = Vector3.Scale(unit, radii);
                    var n = new Vector3(unit.x / Mathf.Max(radii.x, 1e-4f), unit.y / Mathf.Max(radii.y, 1e-4f), unit.z / Mathf.Max(radii.z, 1e-4f)).normalized;
                    Add(center + q * p, q * n, color);
                }
            }
            int stride = seg + 1;
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < seg; s++)
                {
                    int a = start + r * stride + s, b = a + stride;
                    _t.Add(a); _t.Add(b); _t.Add(a + 1);
                    _t.Add(a + 1); _t.Add(b); _t.Add(b + 1);
                }
        }

        public void Sphere(Vector3 center, float radius, Color color, int seg = 10, int rings = 7)
            => Ellipsoid(center, Vector3.one * radius, color, null, seg, rings);

        /// <summary>Axis-aligned (then rotated) box with flat normals.</summary>
        public void Box(Vector3 center, Vector3 size, Color color, Quaternion? rot = null)
        {
            var q = rot ?? Quaternion.identity;
            var h = size * 0.5f;
            Face(center, q, new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0), h.z, h.x, h.y, color);
            Face(center, q, new Vector3(0, 0, -1), new Vector3(-1, 0, 0), new Vector3(0, 1, 0), h.z, h.x, h.y, color);
            Face(center, q, new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0), h.x, h.z, h.y, color);
            Face(center, q, new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0), h.x, h.z, h.y, color);
            Face(center, q, new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1), h.y, h.x, h.z, color);
            Face(center, q, new Vector3(0, -1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), h.y, h.x, h.z, color);
        }

        private void Face(Vector3 c, Quaternion q, Vector3 n, Vector3 u, Vector3 v, float dn, float du, float dv, Color col)
        {
            var nn = q * n;
            var o = c + q * (n * dn);
            var uu = q * (u * du);
            var vv = q * (v * dv);
            int a = Add(o - uu - vv, nn, col);
            int b = Add(o + uu - vv, nn, col);
            int d = Add(o + uu + vv, nn, col);
            int e = Add(o - uu + vv, nn, col);
            _t.Add(a); _t.Add(b); _t.Add(d);
            _t.Add(a); _t.Add(d); _t.Add(e);
        }

        /// <summary>Frustum from a (radius ra) to b (radius rb) with caps. rb = 0 gives a cone.</summary>
        public void Frustum(Vector3 a, Vector3 b, float ra, float rb, Color color, int seg = 10, bool caps = true, float squashX = 1f)
        {
            var axis = b - a;
            float len = axis.magnitude;
            if (len < 1e-5f) return;
            var dir = axis / len;
            var q = Quaternion.FromToRotation(Vector3.up, dir);
            float slope = (ra - rb) / len;
            int start = _v.Count;
            for (int s = 0; s <= seg; s++)
            {
                float th = 2f * Mathf.PI * s / seg;
                var radial = new Vector3(Mathf.Sin(th) * squashX, 0, Mathf.Cos(th));
                var n = q * new Vector3(radial.x, slope, radial.z).normalized;
                Add(a + q * (radial * ra), n, color);
                Add(b + q * (radial * rb), n, color);
            }
            for (int s = 0; s < seg; s++)
            {
                int i0 = start + s * 2;
                _t.Add(i0); _t.Add(i0 + 2); _t.Add(i0 + 1);
                _t.Add(i0 + 1); _t.Add(i0 + 2); _t.Add(i0 + 3);
            }
            if (!caps) return;
            Cap(a, q, ra, -dir, color, seg, squashX, true);
            if (rb > 1e-4f) Cap(b, q, rb, dir, color, seg, squashX, false);
        }

        private void Cap(Vector3 c, Quaternion q, float r, Vector3 n, Color col, int seg, float squashX, bool flip)
        {
            int center = Add(c, n, col);
            int start = _v.Count;
            for (int s = 0; s <= seg; s++)
            {
                float th = 2f * Mathf.PI * s / seg;
                Add(c + q * new Vector3(Mathf.Sin(th) * r * squashX, 0, Mathf.Cos(th) * r), n, col);
            }
            for (int s = 0; s < seg; s++)
            {
                if (flip) { _t.Add(center); _t.Add(start + s + 1); _t.Add(start + s); }
                else { _t.Add(center); _t.Add(start + s); _t.Add(start + s + 1); }
            }
        }

        /// <summary>Rounded limb: frustum with spheres at both ends.</summary>
        public void Capsule(Vector3 a, Vector3 b, float ra, float rb, Color color, int seg = 10)
        {
            Frustum(a, b, ra, rb, color, seg, false);
            Sphere(a, ra, color, seg, 6);
            Sphere(b, rb, color, seg, 6);
        }

        public void Cone(Vector3 baseCenter, Vector3 tip, float radius, Color color, int seg = 10)
            => Frustum(baseCenter, tip, radius, 0f, color, seg, true);

        /// <summary>Flat disc (thin cylinder), e.g. hat brims.</summary>
        public void Disc(Vector3 center, Vector3 normal, float radius, float thickness, Color color, int seg = 16, float squashX = 1f)
        {
            var h = normal.normalized * thickness * 0.5f;
            Frustum(center - h, center + h, radius, radius, color, seg, true, squashX);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (_v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(_v);
            mesh.SetNormals(_n);
            mesh.SetColors(_c);
            mesh.SetTriangles(_t, 0);
            mesh.SetTangents(SmoothNormals());
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);
            return mesh;
        }

        /// <summary>Average normals of vertices sharing a position so the outline hull stays closed on hard edges.</summary>
        private List<Vector4> SmoothNormals()
        {
            var acc = new Dictionary<Vector3Int, Vector3>(_v.Count);
            for (int i = 0; i < _v.Count; i++)
            {
                var k = Key(_v[i]);
                acc.TryGetValue(k, out var s);
                acc[k] = s + _n[i];
            }
            var res = new List<Vector4>(_v.Count);
            for (int i = 0; i < _v.Count; i++)
            {
                var s = acc[Key(_v[i])];
                var n = s.sqrMagnitude > 1e-8f ? s.normalized : _n[i];
                res.Add(new Vector4(n.x, n.y, n.z, _w[i]));
            }
            return res;
        }

        private static Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * 2000f), Mathf.RoundToInt(p.y * 2000f), Mathf.RoundToInt(p.z * 2000f));
    }
}

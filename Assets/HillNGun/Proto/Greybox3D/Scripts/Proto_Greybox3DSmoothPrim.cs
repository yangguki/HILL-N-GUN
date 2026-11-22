using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>Higher-segment spheres/capsules with smoothed normals for chibi dress and props.</summary>
    public static class Proto_Greybox3DSmoothPrim
    {
        static Mesh s_sphere;
        static Mesh s_capsule;
        static Mesh s_disk;

        public static Mesh SphereMesh()
        {
            if (s_sphere != null) return s_sphere;
            s_sphere = BuildSphere(28, 20);
            s_sphere.name = "Greybox3D_SmoothSphere";
            return s_sphere;
        }

        public static Mesh CapsuleMesh()
        {
            if (s_capsule != null) return s_capsule;
            s_capsule = BuildCapsule(20, 8);
            s_capsule.name = "Greybox3D_SmoothCapsule";
            return s_capsule;
        }

        public static Mesh DiskMesh()
        {
            if (s_disk != null) return s_disk;
            s_disk = BuildCylinder(28, 0.5f);
            s_disk.name = "Greybox3D_SmoothDisk";
            return s_disk;
        }

        public static Transform Part(Transform parent, string name, Mesh mesh, Vector3 localPos, Vector3 localScale,
            Color color, float outline = 0.012f, Quaternion? localRot = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Proto_Greybox3DMaterials.CelShared(color, outline);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        public static Transform Sphere(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color,
            float outline = 0.012f) =>
            Part(parent, name, SphereMesh(), localPos, localScale, color, outline);

        public static Transform CapsuleY(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color,
            float outline = 0.012f) =>
            Part(parent, name, CapsuleMesh(), localPos, localScale, color, outline);

        public static Transform CapsuleZ(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color,
            float outline = 0.012f) =>
            Part(parent, name, CapsuleMesh(), localPos, localScale, color, outline, Quaternion.Euler(90f, 0f, 0f));

        public static Transform DiskY(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color,
            float outline = 0.012f) =>
            Part(parent, name, DiskMesh(), localPos, localScale, color, outline);

        static Mesh BuildSphere(int longSegs, int latSegs)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int y = 0; y <= latSegs; y++)
            {
                float v = y / (float)latSegs;
                float pitch = Mathf.PI * (v - 0.5f);
                float cy = Mathf.Sin(pitch);
                float cr = Mathf.Cos(pitch);
                for (int x = 0; x <= longSegs; x++)
                {
                    float u = x / (float)longSegs;
                    float yaw = u * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Cos(yaw) * cr, cy, Mathf.Sin(yaw) * cr);
                    verts.Add(p);
                    norms.Add(p.normalized);
                    uvs.Add(new Vector2(u, v));
                }
            }

            int stride = longSegs + 1;
            for (int y = 0; y < latSegs; y++)
            for (int x = 0; x < longSegs; x++)
            {
                int a = y * stride + x;
                int b = a + stride;
                tris.Add(a);
                tris.Add(b);
                tris.Add(a + 1);
                tris.Add(a + 1);
                tris.Add(b);
                tris.Add(b + 1);
            }

            return Finish(verts, norms, uvs, tris);
        }

        static Mesh BuildCapsule(int segs, int hemiRings)
        {
            // Unit capsule along Y, radius 0.5, total height 2 (body 1 + two hemispheres).
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            int rings = hemiRings * 2 + 2;
            for (int y = 0; y <= rings; y++)
            {
                float v = y / (float)rings;
                float cy, cr, ny;
                if (y <= hemiRings)
                {
                    float t = y / (float)hemiRings;
                    float pitch = Mathf.PI * 0.5f * (t - 1f);
                    cr = 0.5f * Mathf.Cos(pitch);
                    cy = -0.5f + 0.5f * Mathf.Sin(pitch);
                    ny = Mathf.Sin(pitch);
                }
                else if (y >= rings - hemiRings)
                {
                    float t = (y - (rings - hemiRings)) / (float)hemiRings;
                    float pitch = Mathf.PI * 0.5f * t;
                    cr = 0.5f * Mathf.Cos(pitch);
                    cy = 0.5f + 0.5f * Mathf.Sin(pitch);
                    ny = Mathf.Sin(pitch);
                }
                else
                {
                    float t = (y - hemiRings) / (float)(rings - 2 * hemiRings);
                    cr = 0.5f;
                    cy = Mathf.Lerp(-0.5f, 0.5f, t);
                    ny = 0f;
                }

                for (int x = 0; x <= segs; x++)
                {
                    float u = x / (float)segs;
                    float yaw = u * Mathf.PI * 2f;
                    var lateral = new Vector3(Mathf.Cos(yaw) * cr * 2f, 0f, Mathf.Sin(yaw) * cr * 2f);
                    var p = new Vector3(lateral.x * 0.5f, cy, lateral.z * 0.5f);
                    var n = new Vector3(Mathf.Cos(yaw) * (cr > 0.001f ? 1f : 0f), ny, Mathf.Sin(yaw) * (cr > 0.001f ? 1f : 0f));
                    if (n.sqrMagnitude < 1e-6f) n = Vector3.up * Mathf.Sign(cy);
                    verts.Add(p);
                    norms.Add(n.normalized);
                    uvs.Add(new Vector2(u, v));
                }
            }

            int stride = segs + 1;
            for (int y = 0; y < rings; y++)
            for (int x = 0; x < segs; x++)
            {
                int a = y * stride + x;
                int b = a + stride;
                tris.Add(a);
                tris.Add(b);
                tris.Add(a + 1);
                tris.Add(a + 1);
                tris.Add(b);
                tris.Add(b + 1);
            }

            return Finish(verts, norms, uvs, tris);
        }

        static Mesh BuildCylinder(int segs, float height)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float hy = height * 0.5f;
            for (int y = 0; y <= 1; y++)
            {
                float cy = y == 0 ? -hy : hy;
                for (int x = 0; x <= segs; x++)
                {
                    float u = x / (float)segs;
                    float yaw = u * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                    verts.Add(n * 0.5f + Vector3.up * cy);
                    norms.Add(n);
                    uvs.Add(new Vector2(u, y));
                }
            }

            int stride = segs + 1;
            for (int x = 0; x < segs; x++)
            {
                int a = x;
                int b = a + stride;
                tris.Add(a);
                tris.Add(b);
                tris.Add(a + 1);
                tris.Add(a + 1);
                tris.Add(b);
                tris.Add(b + 1);
            }

            // caps
            int topCenter = verts.Count;
            verts.Add(new Vector3(0f, hy, 0f));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));
            int botCenter = verts.Count;
            verts.Add(new Vector3(0f, -hy, 0f));
            norms.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int x = 0; x < segs; x++)
            {
                tris.Add(topCenter);
                tris.Add(stride + x + 1);
                tris.Add(stride + x);
                tris.Add(botCenter);
                tris.Add(x);
                tris.Add(x + 1);
            }

            return Finish(verts, norms, uvs, tris);
        }

        static Mesh Finish(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris)
        {
            var m = new Mesh { hideFlags = HideFlags.DontSave };
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        }
    }
}

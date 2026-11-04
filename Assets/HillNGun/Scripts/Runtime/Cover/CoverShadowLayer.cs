using System.Collections.Generic;
using UnityEngine;
using HillNGun.Battlefield;
using HillNGun.Feel;

namespace HillNGun.Cover
{
    /// <summary>
    /// Ground contact shadows of every cover prop drawn as ONE dynamic mesh (one draw call in total), in the shared
    /// stage-theme shadow tone, on the ground-decal band under every actor. View only.
    /// </summary>
    public class CoverShadowLayer : MonoBehaviour
    {
        public struct Entry { public Vector3 center; public Vector2 size; public Color color; public bool visible; }

        private static CoverShadowLayer _instance;
        private readonly List<CoverView> _views = new List<CoverView>();
        private Mesh _mesh;
        private Material _mat;
        private readonly List<Vector3> _v = new List<Vector3>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<Vector2> _u = new List<Vector2>();
        private readonly List<int> _t = new List<int>();

        public static void Register(CoverView v)
        {
            if (_instance == null)
            {
                var go = new GameObject("CoverShadowLayer");
                _instance = go.AddComponent<CoverShadowLayer>();
                _instance.Init();
            }
            if (!_instance._views.Contains(v)) _instance._views.Add(v);
        }

        public static void Unregister(CoverView v)
        {
            if (_instance != null) _instance._views.Remove(v);
        }

        private void Init()
        {
            var sprite = MotionFx.ShadowSprite;
            _mesh = new Mesh { name = "CoverShadows" };
            _mesh.MarkDynamic();
            _mat = new Material(Shader.Find("Sprites/Default")) { name = "CoverShadows", mainTexture = sprite != null ? sprite.texture : Texture2D.whiteTexture };
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _mat;
            mr.sortingOrder = FieldView.GroundDecalOrder - 1;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void LateUpdate()
        {
            _v.Clear(); _c.Clear(); _u.Clear(); _t.Clear();
            for (int k = _views.Count - 1; k >= 0; k--)
            {
                var view = _views[k];
                if (view == null) { _views.RemoveAt(k); continue; }
                if (!view.isActiveAndEnabled) continue;
                var e = view.ShadowEntry;
                if (!e.visible || e.color.a <= 0.001f) continue;
                int i = _v.Count;
                float hx = e.size.x * 0.5f, hy = e.size.y * 0.5f;
                _v.Add(e.center + new Vector3(-hx, -hy)); _v.Add(e.center + new Vector3(hx, -hy)); _v.Add(e.center + new Vector3(hx, hy)); _v.Add(e.center + new Vector3(-hx, hy));
                _u.Add(new Vector2(0, 0)); _u.Add(new Vector2(1, 0)); _u.Add(new Vector2(1, 1)); _u.Add(new Vector2(0, 1));
                _c.Add(e.color); _c.Add(e.color); _c.Add(e.color); _c.Add(e.color);
                _t.Add(i); _t.Add(i + 2); _t.Add(i + 1); _t.Add(i); _t.Add(i + 3); _t.Add(i + 2);
            }
            _mesh.Clear();
            _mesh.SetVertices(_v);
            _mesh.SetColors(_c);
            _mesh.SetUVs(0, _u);
            _mesh.SetTriangles(_t, 0, false);
            _mesh.bounds = new Bounds(Vector3.zero, new Vector3(200f, 200f, 10f));
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_mesh != null) Destroy(_mesh);
            if (_mat != null) Destroy(_mat);
        }
    }
}

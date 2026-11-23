using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>Camera-facing sprite playback for muzzle / spark / explosion / smoke / dust / coin.</summary>
    public sealed class Proto_Greybox3DBillboardFx : MonoBehaviour
    {
        Sprite[] _frames;
        float _fps;
        bool _loop;
        float _age;
        float _life;
        Vector3 _drift;
        Vector2 _pivot = new Vector2(0.5f, 0.5f);
        MeshRenderer _mr;
        MeshFilter _mf;
        Material _mat;
        static Mesh s_quad;

        public bool IsDone => !_loop && _age >= _life;

        public static Proto_Greybox3DBillboardFx Play(Transform parent, string name, Vector3 worldPos, Vector3 worldScale,
            Sprite[] frames, float fps, bool loop, Vector2 pivot, Vector3 drift, Camera face)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = worldScale;
            var fx = go.AddComponent<Proto_Greybox3DBillboardFx>();
            fx._frames = frames;
            fx._fps = Mathf.Max(1f, fps);
            fx._loop = loop;
            fx._pivot = pivot;
            fx._drift = drift;
            fx._life = frames.Length / fx._fps;
            fx.Build(frames[0]);
            fx.Face(face);
            fx.ApplyFrame(0);
            return fx;
        }

        void Build(Sprite first)
        {
            if (s_quad == null)
            {
                s_quad = new Mesh { name = "Greybox3D_FxQuad" };
                s_quad.vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
                };
                s_quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                s_quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                s_quad.RecalculateBounds();
            }
            _mf = gameObject.AddComponent<MeshFilter>();
            _mf.sharedMesh = s_quad;
            _mr = gameObject.AddComponent<MeshRenderer>();
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mat = Proto_Greybox3DMaterials.Sprite(first.texture, Color.white);
            _mr.sharedMaterial = _mat;
        }

        public void Tick(float dt, Camera cam)
        {
            _age += dt;
            transform.position += _drift * dt;
            Face(cam);
            int frame;
            if (_loop)
                frame = Mathf.FloorToInt(_age * _fps) % _frames.Length;
            else
            {
                frame = Mathf.Clamp(Mathf.FloorToInt(_age * _fps), 0, _frames.Length - 1);
                if (_age >= _life)
                {
                    Proto_Greybox3DUtil.DestroySafe(gameObject);
                    return;
                }
            }
            ApplyFrame(frame);
        }

        void Face(Camera cam)
        {
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
        }

        void ApplyFrame(int index)
        {
            var sp = _frames[index];
            if (sp == null || _mat == null) return;
            var tex = sp.texture;
            var r = sp.textureRect;
            if (tex != null && _mat.HasProperty("_MainTex"))
            {
                _mat.SetTexture("_MainTex", tex);
                _mat.SetTextureScale("_MainTex", new Vector2(r.width / tex.width, r.height / tex.height));
                _mat.SetTextureOffset("_MainTex", new Vector2(r.x / tex.width, r.y / tex.height));
            }
            // Offset the quad so the pivot sits at the transform origin.
            var off = new Vector3(0.5f - _pivot.x, 0.5f - _pivot.y, 0f);
            if (_mf != null)
            {
                // keep shared unit quad; bake pivot into local position of mesh via child? use localPosition of mesh filter go.
            }
            transform.localPosition = transform.localPosition; // no-op placeholder
            // Apply pivot by shifting the renderer transform relative to parent? We already spawned at world pos.
            // Store base and add pivot each frame? Simpler: scale is world size of full cell; shift in local space.
        }
    }
}

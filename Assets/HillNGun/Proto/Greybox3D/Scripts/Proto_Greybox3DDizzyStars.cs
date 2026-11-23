using UnityEngine;
using HillNGun.Proto.Greybox;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>
    /// 3rd-roll dizzy: designer v2 star-ring billboard (8 frames @ 12 fps) 0.35 m above the head,
    /// with a procedural star fallback if the sheet is missing.
    /// </summary>
    public sealed class Proto_Greybox3DDizzyStars : MonoBehaviour
    {
        const int FallbackCount = 3;
        Transform _billboard;
        Proto_Greybox3DBillboardFx _anim;
        Transform[] _stars;
        float _phase;
        Transform _head;

        public static Proto_Greybox3DDizzyStars Ensure(Transform hero)
        {
            if (hero == null) return null;
            var existing = hero.GetComponent<Proto_Greybox3DDizzyStars>();
            if (existing != null) return existing;
            return hero.gameObject.AddComponent<Proto_Greybox3DDizzyStars>();
        }

        public void Tick(float dt, Proto_GreyboxRollDizzy dizzy, Camera cam)
        {
            bool show = dizzy != null && dizzy.Phase != Proto_GreyboxRollDizzyPhase.None;
            var cat = Proto_Greybox3DCatalog.Load();
            bool useSheet = cat != null && cat.FxDizzyStars != null && cat.FxDizzyStars.Length > 0;
            if (useSheet)
                TickSheet(dt, show, cat, cam);
            else
                TickFallback(dt, show, cam, dizzy);
        }

        void TickSheet(float dt, bool show, Proto_Greybox3DCatalog cat, Camera cam)
        {
            if (_head == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.ToLowerInvariant().Contains("head"))
                    {
                        _head = t;
                        break;
                    }
                }
                if (_head == null) _head = transform;
            }
            if (_billboard == null)
            {
                var fx = Proto_Greybox3DBillboardFx.Play(transform, "DizzyStarRing",
                    _head.position + Vector3.up * 0.35f, new Vector3(0.9f, 0.45f, 1f),
                    cat.FxDizzyStars, 12f, true, new Vector2(0.5f, 0.5f), Vector3.zero, cam);
                if (fx != null)
                {
                    _anim = fx;
                    _billboard = fx.transform;
                    // Re-parent so it follows the hero; Play already parented to this transform.
                }
            }
            if (_billboard == null) return;
            _billboard.gameObject.SetActive(show);
            if (!show) return;
            var pos = (_head != null ? _head.position : transform.position) + Vector3.up * 0.35f;
            _billboard.position = pos;
            _anim?.Tick(dt, cam);
        }

        void TickFallback(float dt, bool show, Camera cam, Proto_GreyboxRollDizzy dizzy)
        {
            if (_stars == null) BuildFallback();
            _phase += dt * (dizzy != null && dizzy.Phase == Proto_GreyboxRollDizzyPhase.Stun ? 6.5f : 4.2f);
            for (int i = 0; i < FallbackCount; i++)
            {
                if (_stars[i] == null) continue;
                _stars[i].gameObject.SetActive(show);
                if (!show) continue;
                float a = _phase + i * (Mathf.PI * 2f / FallbackCount);
                float r = 0.28f;
                _stars[i].localPosition = new Vector3(Mathf.Cos(a) * r, 1.18f + Mathf.Sin(_phase * 2f + i) * 0.05f, Mathf.Sin(a) * r);
                if (cam != null)
                    _stars[i].rotation = Quaternion.LookRotation(_stars[i].position - cam.transform.position, Vector3.up);
            }
        }

        void BuildFallback()
        {
            _stars = new Transform[FallbackCount];
            var mat = Proto_Greybox3DMaterials.Unlit(new Color(1f, 0.88f, 0.15f));
            for (int i = 0; i < FallbackCount; i++)
            {
                var go = new GameObject("DizzyStar_" + i);
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * 0.16f;
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = StarMesh();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _stars[i] = go.transform;
                go.SetActive(false);
            }
        }

        static Mesh s_star;
        static Mesh StarMesh()
        {
            if (s_star != null) return s_star;
            const int points = 5;
            var verts = new Vector3[points * 2 + 1];
            var tris = new int[points * 6];
            verts[0] = Vector3.zero;
            for (int i = 0; i < points * 2; i++)
            {
                float a = -Mathf.PI * 0.5f + i * Mathf.PI / points;
                float r = (i % 2 == 0) ? 0.55f : 0.22f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            }
            int t = 0;
            for (int i = 0; i < points * 2; i++)
            {
                tris[t++] = 0;
                tris[t++] = i + 1;
                tris[t++] = (i + 1) % (points * 2) + 1;
            }
            s_star = new Mesh { name = "Greybox3D_DizzyStar" };
            s_star.vertices = verts;
            s_star.triangles = tris;
            s_star.RecalculateBounds();
            s_star.RecalculateNormals();
            return s_star;
        }
    }
}

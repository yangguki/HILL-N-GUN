using System.Collections.Generic;
using UnityEngine;
using HillNGun.Battlefield;
using HillNGun.Combat;
using HillNGun.Data;
using HillNGun.UI.Touch;

namespace HillNGun.Feel
{
    /// <summary>
    /// Draws field drops (view only, one mesh on the code-drawn icon atlas): toss arc, soft glow, bobbing icon, rotating
    /// sparkles, blink before expiry, and a ground shadow. Reads DropSystem.Items each LateUpdate.
    /// </summary>
    public class DropView : MonoBehaviour
    {
        private DropSystem _system;
        private Mesh _mesh;
        private MeshRenderer _mr;
        private readonly List<Vector3> _v = new List<Vector3>(256);
        private readonly List<Color32> _c = new List<Color32>(256);
        private readonly List<Vector2> _uv = new List<Vector2>(256);
        private readonly List<int> _t = new List<int>(384);
        private static Material _mat;

        public int LastDrawn { get; private set; }

        public static DropView Create(Transform parent)
        {
            var go = new GameObject("DropView");
            go.transform.SetParent(parent, false);
            return go.AddComponent<DropView>();
        }

        public void Bind(DropSystem s) => _system = s;

        private void Awake()
        {
            _mesh = new Mesh { name = "Drops" };
            _mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _mr = gameObject.AddComponent<MeshRenderer>();
            if (_mat == null) _mat = new Material(Shader.Find("Sprites/Default")) { name = "Drops_shared", mainTexture = TouchIconAtlas.Texture };
            _mr.sharedMaterial = _mat;
            _mr.sortingOrder = FieldView.ProjectileOrder - 20;
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
        }

        private static TouchIconAtlas.Cell IconOf(DropType t)
        {
            switch (t)
            {
                case DropType.Heal: return TouchIconAtlas.Cell.Cross;
                case DropType.Ammo: return TouchIconAtlas.Cell.Bullet;
                case DropType.Grenade: return TouchIconAtlas.Cell.Dynamite;
                case DropType.WeaponBoost: return TouchIconAtlas.Cell.Bolt;
                default: return TouchIconAtlas.Cell.Coin;
            }
        }

        private void LateUpdate()
        {
            _v.Clear(); _c.Clear(); _uv.Clear(); _t.Clear();
            var items = _system?.Items;
            var table = _system?.Table;
            int n = 0;
            if (items != null && table != null)
            {
                float now = Time.time;
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    n++;
                    float persp = FieldView.PerspectiveScale(it.Position.y);
                    float size = table.iconSize * persp;
                    Vector2 lp;
                    float alpha = 1f;
                    if (!it.Landed)
                    {
                        float k = it.TossTime > 0f ? it.Age / it.TossTime : 1f;
                        lp = Vector2.Lerp(it.From, it.Position, k) + new Vector2(0f, 2.2f * 4f * k * (1f - k));
                    }
                    else
                    {
                        lp = it.Position;
                        float left = it.TossTime + table.lifetime - it.Age;
                        if (left < table.blinkTime) alpha = Mathf.Repeat(left * 6f, 1f) < 0.5f ? 0.25f : 1f;
                    }
                    Vector3 ground = FieldView.ToWorld(it.Landed ? it.Position : lp);
                    float bob = it.Landed ? (0.12f + 0.08f * Mathf.Sin(now * 4f + it.Id)) * persp : 0f;
                    Vector3 c = (it.Landed ? ground : FieldView.ToWorld(lp)) + new Vector3(0f, size * 0.55f + bob, 0f);
                    Color col = it.Entry.color;

                    if (it.Landed) Quad(ground + new Vector3(0f, size * 0.05f, 0f), size * 0.55f, size * 0.18f, 0f, TouchIconAtlas.Cell.Glow, new Color(0f, 0f, 0f, 0.45f * alpha)); // shadow
                    float pulse = 0.85f + 0.15f * Mathf.Sin(now * 5f + it.Id * 1.3f);
                    Quad(c, size * 1.15f * pulse, size * 1.15f * pulse, 0f, TouchIconAtlas.Cell.Glow, new Color(col.r, col.g, col.b, 0.75f * alpha));
                    Quad(c, size * 0.62f, size * 0.62f, 0f, TouchIconAtlas.Cell.Disc, new Color(0.13f, 0.09f, 0.06f, 0.85f * alpha));
                    Quad(c, size * 0.5f, size * 0.5f, 0f, IconOf(it.Type), new Color(Mathf.Lerp(col.r, 1f, 0.35f), Mathf.Lerp(col.g, 1f, 0.35f), Mathf.Lerp(col.b, 1f, 0.35f), alpha));
                    // sparkles: two twinkling stars orbiting
                    for (int s = 0; s < 2; s++)
                    {
                        float ph = now * table.sparkleRate + it.Id * 2.1f + s * Mathf.PI;
                        float tw = Mathf.Clamp01(Mathf.Sin(ph * 1.7f) * 0.5f + 0.5f);
                        Vector3 o = new Vector3(Mathf.Cos(ph * 0.6f), Mathf.Sin(ph * 0.6f) * 0.8f, 0f) * size * 0.72f;
                        float ss = size * (0.25f + 0.25f * tw);
                        Quad(c + o, ss, ss, ph * 40f, TouchIconAtlas.Cell.Sparkle, new Color(1f, 1f, 0.9f, tw * alpha));
                    }
                }
            }
            LastDrawn = n;
            _mesh.Clear();
            if (_v.Count > 0)
            {
                _mesh.SetVertices(_v);
                _mesh.SetColors(_c);
                _mesh.SetUVs(0, _uv);
                _mesh.SetTriangles(_t, 0);
                _mesh.bounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f));
            }
            _mr.enabled = _v.Count > 0;
        }

        private void Quad(Vector3 c, float hw, float hh, float rotDeg, TouchIconAtlas.Cell cell, Color col)
        {
            Rect uv = TouchIconAtlas.UV(cell);
            float r = rotDeg * Mathf.Deg2Rad, cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            Vector3 ax = new Vector3(cs, sn, 0f) * hw, ay = new Vector3(-sn, cs, 0f) * hh;
            int i = _v.Count;
            _v.Add(c - ax - ay); _v.Add(c - ax + ay); _v.Add(c + ax + ay); _v.Add(c + ax - ay);
            _uv.Add(new Vector2(uv.xMin, uv.yMin)); _uv.Add(new Vector2(uv.xMin, uv.yMax)); _uv.Add(new Vector2(uv.xMax, uv.yMax)); _uv.Add(new Vector2(uv.xMax, uv.yMin));
            Color32 c32 = col;
            _c.Add(c32); _c.Add(c32); _c.Add(c32); _c.Add(c32);
            _t.Add(i); _t.Add(i + 1); _t.Add(i + 2); _t.Add(i); _t.Add(i + 2); _t.Add(i + 3);
        }
    }
}

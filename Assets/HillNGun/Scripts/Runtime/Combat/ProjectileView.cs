using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;

namespace HillNGun.Combat
{
    /// <summary>
    /// Renders every active projectile as a bright round with a fading trail, all in ONE dynamic mesh (one draw call).
    /// View only: reads the logic pool in LateUpdate; sizes/colours come from FeelData. Bullets stay visible-speed
    /// projectiles (01 §: 투사체, 피할 수 있음) — the trail just makes their path readable on small screens.
    /// </summary>
    public class ProjectileView : MonoBehaviour
    {
        private CombatSystem _combat;
        private Mesh _mesh;
        private MeshRenderer _mr;
        private readonly List<Vector3> _v = new List<Vector3>(512);
        private readonly List<Color32> _c = new List<Color32>(512);
        private readonly List<Vector2> _uv = new List<Vector2>(512);
        private readonly List<int> _t = new List<int>(768);
        private static Material _mat;
        private static Texture2D _tex;

        public int LastDrawn { get; private set; }

        public static ProjectileView Ensure()
        {
            var existing = FindFirstObjectByType<ProjectileView>();
            if (existing != null) return existing;
            return new GameObject("[ProjectileView]").AddComponent<ProjectileView>();
        }

        private void Awake()
        {
            _mesh = new Mesh { name = "BulletFx" };
            _mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _mr = gameObject.AddComponent<MeshRenderer>();
            _mr.sharedMaterial = Material;
            _mr.sortingOrder = FieldView.ProjectileOrder;
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
        }

        private static Material Material
        {
            get
            {
                if (_mat != null) return _mat;
                _mat = new Material(Shader.Find("Sprites/Default")) { name = "BulletFx_shared", mainTexture = Texture };
                return _mat;
            }
        }

        /// <summary>64x32: left half = soft round head with a white core, right half = trail gradient (u 0 tail → 1 head).</summary>
        private static Texture2D Texture
        {
            get
            {
                if (_tex != null) return _tex;
                const int W = 64, H = 32;
                _tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "BulletFx", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        Color col;
                        if (x < 32)
                        {
                            float dx = (x + 0.5f - 16f) / 15f, dy = (y + 0.5f - 16f) / 15f;
                            float r = Mathf.Sqrt(dx * dx + dy * dy);
                            float glow = Mathf.Clamp01(1f - r);
                            float core = Mathf.Clamp01((0.45f - r) / 0.12f);
                            float a = Mathf.Clamp01(glow * glow * 1.3f + core);
                            float w = Mathf.Lerp(0.85f, 1f, core);
                            col = new Color(w, w, w, a);
                        }
                        else
                        {
                            float u = (x - 32 + 0.5f) / 32f;           // 0 tail .. 1 head
                            float v = Mathf.Abs((y + 0.5f) / H * 2f - 1f);
                            float a = Mathf.Pow(u, 1.6f) * Mathf.Clamp01(1f - v * v);
                            col = new Color(1f, 1f, 1f, a);
                        }
                        px[y * W + x] = col;
                    }
                _tex.SetPixels32(px);
                _tex.Apply(false, true);
                return _tex;
            }
        }

        private void LateUpdate()
        {
            if (_combat == null) _combat = ServiceLocator.Get<CombatSystem>();
            var list = _combat?.ProjectilePool?.ActiveProjectiles;
            var feel = _combat != null && _combat.FeelData != null ? _combat.FeelData : ServiceLocator.Get<FeelData>();
            float head = feel != null ? feel.bulletHeadSize : 0.62f;
            float halo = feel != null ? feel.bulletHaloAlpha : 0.5f;
            float trailLen = feel != null ? feel.bulletTrailLength : 1.8f;
            float trailW = feel != null ? feel.bulletTrailWidth : 0.28f;
            Color pc = feel != null ? feel.playerBulletColor : new Color(1f, 0.93f, 0.55f);
            Color ec = feel != null ? feel.enemyBulletColor : new Color(1f, 0.42f, 0.28f);

            _v.Clear(); _c.Clear(); _uv.Clear(); _t.Clear();
            int n = 0;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var p = list[i];
                    if (!p.IsActive) continue;
                    n++;
                    var vfx = p.Vfx;
                    Color col = p.OwnerTeam == Team.Player ? pc : ec;
                    float useHead = head, useTrail = trailLen, useW = trailW;
                    if (vfx != null)
                    {
                        col = p.OwnerTeam == Team.Player ? vfx.tracerColor : Color.Lerp(vfx.tracerColor, ec, 0.35f);
                        useHead = vfx.headSize; useTrail = vfx.tracerLength; useW = vfx.tracerWidth;
                    }
                    float persp = FieldView.PerspectiveScale(p.Position.y);
                    float sizeMul = Mathf.Clamp(p.Radius / 0.25f, 0.75f, 1.35f) * (p.Damage >= 500 ? 1.15f : 1f);
                    if (vfx != null && vfx.isSlug) sizeMul *= 1.15f;
                    if (vfx != null && vfx.isNeedle) sizeMul *= 0.85f;
                    Vector3 hw = FieldView.ToWorld(p.Position);
                    float len = Mathf.Min(useTrail * sizeMul, p.DistanceTraveled + 0.2f);
                    Vector3 tw = FieldView.ToWorld(p.Position - p.Direction * len);
                    // trail quad
                    Vector3 axis = hw - tw;
                    Vector3 side = axis.sqrMagnitude > 1e-6f ? new Vector3(-axis.y, axis.x, 0f).normalized : Vector3.right;
                    float w = useW * sizeMul * persp * 0.5f;
                    Color tc = new Color(col.r, col.g, col.b, 0.85f);
                    Quad(tw - side * w * 0.35f, tw + side * w * 0.35f, hw + side * w, hw - side * w,
                         new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), tc);
                    // head (dark halo first for contrast, then the bright core)
                    float h = useHead * sizeMul * persp * 0.5f;
                    if (halo > 0f)
                    {
                        float hh = h * 1.6f;
                        Quad(hw + new Vector3(-hh, -hh), hw + new Vector3(-hh, hh), hw + new Vector3(hh, hh), hw + new Vector3(hh, -hh),
                             new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                             new Color(0.22f, 0.1f, 0.04f, halo));
                    }
                    Quad(hw + new Vector3(-h, -h), hw + new Vector3(-h, h), hw + new Vector3(h, h), hw + new Vector3(h, -h),
                         new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                         Color.Lerp(col, Color.white, 0.35f));
                }
            }
            LastDrawn = n;
            _mesh.Clear();
            if (n > 0)
            {
                _mesh.SetVertices(_v);
                _mesh.SetColors(_c);
                _mesh.SetUVs(0, _uv);
                _mesh.SetTriangles(_t, 0);
                _mesh.bounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f));
            }
            _mr.enabled = n > 0;
        }

        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Color col)
        {
            int i = _v.Count;
            _v.Add(a); _v.Add(b); _v.Add(c); _v.Add(d);
            _uv.Add(ua); _uv.Add(ub); _uv.Add(uc); _uv.Add(ud);
            Color32 c32 = col;
            _c.Add(c32); _c.Add(c32); _c.Add(c32); _c.Add(c32);
            _t.Add(i); _t.Add(i + 1); _t.Add(i + 2);
            _t.Add(i); _t.Add(i + 2); _t.Add(i + 3);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;
using HillNGun.Feel;

namespace HillNGun.Combat
{
    /// <summary>
    /// View-only: draws flying grenades (spin + smoke trail) and cartoon explode bursts.
    /// Logic stays in CombatSystem / GrenadeProjectile. 12+: no blood.
    /// </summary>
    public class GrenadeView : MonoBehaviour
    {
        private CombatSystem _combat;
        private readonly List<BombSprite> _bombs = new List<BombSprite>(8);
        private readonly List<TrailPuff> _puffs = new List<TrailPuff>(48);
        private Sprite _bombSprite;
        private static Material _spriteMat;

        public int LastDrawn { get; private set; }
        public int ActiveBombs => CountActiveBombs();

        public static GrenadeView Ensure()
        {
            var existing = FindFirstObjectByType<GrenadeView>();
            if (existing != null) return existing;
            return new GameObject("[GrenadeView]").AddComponent<GrenadeView>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            EnsureBombPool(6);
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void TryBind()
        {
            if (_combat != null) return;
            _combat = ServiceLocator.Get<CombatSystem>();
            if (_combat == null) return;
            _combat.OnGrenadeThrown += HandleThrown;
            _combat.OnGrenadeExploded += HandleExploded;
        }

        private void Unbind()
        {
            if (_combat == null) return;
            _combat.OnGrenadeThrown -= HandleThrown;
            _combat.OnGrenadeExploded -= HandleExploded;
            _combat = null;
        }

        private void LateUpdate()
        {
            if (_combat == null) TryBind();
            var list = _combat?.ActiveGrenades;
            var lib = RuntimeAssetManager.Instance?.SpriteLibrary;
            if (_bombSprite == null && lib != null) _bombSprite = lib.grenade;

            // hide all, then show active
            for (int i = 0; i < _bombs.Count; i++) _bombs[i].Hide();
            LastDrawn = 0;
            if (list == null) { TickPuffs(Time.deltaTime); return; }

            int bi = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var g = list[i];
                if (g == null || !g.IsActive) continue;
                if (bi >= _bombs.Count) EnsureBombPool(bi + 2);
                var vfx = g.Data != null && g.Data.vfx != null ? g.Data.vfx : GrenadeVfxData.Builtin;
                var bomb = _bombs[bi++];
                Vector2 logic = g.VisualPosition;
                Vector3 world = FieldView.ToWorld(logic);
                float persp = FieldView.PerspectiveScale(logic.y);
                float size = vfx.bombSize * persp;
                float spin = g.ElapsedTime * vfx.spinDegPerSec;
                bomb.Show(world, size, spin, vfx.bombTint, _bombSprite, FieldView.ProjectileOrder + 2);

                // emit a short smoke puff along the trail periodically
                if ((LastDrawn + bi) % 2 == 0 || g.ElapsedTime < 0.05f)
                    EmitPuff(world, vfx.trailColor, 0.12f * persp, 0.28f);

                LastDrawn++;
            }

            TickPuffs(Time.deltaTime);
        }

        private void HandleThrown(GrenadeProjectile g, Vector2 start)
        {
            PlaceholderAudio.Instance?.PlayGrenadeThrow();
            var vfx = g?.Data != null && g.Data.vfx != null ? g.Data.vfx : GrenadeVfxData.Builtin;
            Vector3 w = FieldView.ToWorld(start);
            EmitPuff(w, vfx.trailColor, 0.18f, 0.35f);
        }

        private void HandleExploded(GrenadeProjectile g, Vector2 pos)
        {
            var vfx = g?.Data != null && g.Data.vfx != null ? g.Data.vfx : GrenadeVfxData.Builtin;
            float radius = g?.Data != null ? g.Data.radius : 1.5f;
            EffectsManager.Instance?.SpawnCartoonExplosion(pos, radius, vfx);
            var shake = ServiceLocator.Get<ScreenShake>();
            shake?.ShakeGrenade();
            PlaceholderAudio.Instance?.PlayGrenadeExplode();
        }

        private void EmitPuff(Vector3 world, Color col, float size, float life)
        {
            TrailPuff p = null;
            for (int i = 0; i < _puffs.Count; i++)
                if (!_puffs[i].Active) { p = _puffs[i]; break; }
            if (p == null)
            {
                p = new TrailPuff();
                _puffs.Add(p);
            }
            p.Spawn(world, col, size, life);
        }

        private void TickPuffs(float dt)
        {
            for (int i = 0; i < _puffs.Count; i++) _puffs[i].Tick(dt);
        }

        private void OnRenderObject()
        {
            var cam = Camera.current;
            var target = HillNGun.View3D.Actor3DStage.OverlayCamera != null
                ? HillNGun.View3D.Actor3DStage.OverlayCamera : Camera.main;
            if (cam == null || cam != target) return;
            bool any = false;
            for (int i = 0; i < _puffs.Count; i++) if (_puffs[i].Active) { any = true; break; }
            if (!any) return;
            EffectsManager.SharedFxMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < _puffs.Count; i++) _puffs[i].Emit();
            GL.End();
            GL.PopMatrix();
        }

        private void EnsureBombPool(int n)
        {
            while (_bombs.Count < n)
            {
                var go = new GameObject("GrenadeBomb_" + _bombs.Count);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = SpriteMat();
                sr.sortingOrder = FieldView.ProjectileOrder + 2;
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sr.receiveShadows = false;
                go.SetActive(false);
                _bombs.Add(new BombSprite(go, sr));
            }
        }

        private static Material SpriteMat()
        {
            if (_spriteMat != null) return _spriteMat;
            _spriteMat = new Material(Shader.Find("Sprites/Default")) { name = "GrenadeBomb_shared" };
            return _spriteMat;
        }

        private int CountActiveBombs()
        {
            int n = 0;
            for (int i = 0; i < _bombs.Count; i++) if (_bombs[i].Visible) n++;
            return n;
        }

        private class BombSprite
        {
            private readonly GameObject _go;
            private readonly SpriteRenderer _sr;
            public bool Visible => _go.activeSelf;
            public BombSprite(GameObject go, SpriteRenderer sr) { _go = go; _sr = sr; }
            public void Hide() { if (_go.activeSelf) _go.SetActive(false); }
            public void Show(Vector3 world, float size, float spinDeg, Color tint, Sprite sprite, int order)
            {
                if (sprite != null) _sr.sprite = sprite;
                else if (_sr.sprite == null) _sr.sprite = FallbackDisc();
                _sr.color = tint;
                _sr.sortingOrder = order;
                _go.transform.position = world;
                _go.transform.rotation = Quaternion.Euler(0f, 0f, spinDeg);
                float s = size;
                if (_sr.sprite != null)
                {
                    float px = Mathf.Max(_sr.sprite.bounds.size.x, 0.01f);
                    s = size / px;
                }
                _go.transform.localScale = new Vector3(s, s, 1f);
                if (!_go.activeSelf) _go.SetActive(true);
            }

            private static Sprite _disc;
            private static Sprite FallbackDisc()
            {
                if (_disc != null) return _disc;
                var t = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var px = new Color32[32 * 32];
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float dx = (x - 15.5f) / 15f, dy = (y - 15.5f) / 15f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        byte a = (byte)(Mathf.Clamp01(1f - r) * 255f);
                        // olive bomb body + fuse highlight — never blood red
                        px[y * 32 + x] = r < 0.85f
                            ? new Color32(55, 70, 40, a)
                            : new Color32(0, 0, 0, 0);
                    }
                t.SetPixels32(px); t.Apply(false, true);
                _disc = Sprite.Create(t, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
                return _disc;
            }
        }

        private class TrailPuff
        {
            public bool Active;
            private Vector3 _p;
            private Color _c;
            private float _life, _max, _size;
            public void Spawn(Vector3 p, Color c, float size, float life)
            {
                _p = p; _c = c; _size = size; _max = Mathf.Max(0.05f, life); _life = _max; Active = true;
            }
            public void Tick(float dt)
            {
                if (!Active) return;
                _life -= dt;
                _p += Vector3.up * (0.35f * dt);
                _size *= 1f + 0.8f * dt;
                if (_life <= 0f) Active = false;
            }
            public void Emit()
            {
                if (!Active) return;
                float a = Mathf.Clamp01(_life / _max) * _c.a;
                GL.Color(new Color(_c.r, _c.g, _c.b, a));
                float s = _size;
                const int seg = 8;
                for (int j = 0; j < seg; j++)
                {
                    float a0 = j * Mathf.PI * 2f / seg, a1 = (j + 1) * Mathf.PI * 2f / seg;
                    GL.Vertex(_p);
                    GL.Vertex(_p + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0) * 0.75f, 0f) * s);
                    GL.Vertex(_p + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1) * 0.75f, 0f) * s);
                }
            }
        }
    }
}

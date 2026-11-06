using UnityEngine;
using HillNGun.Core;
using HillNGun.Battlefield;
using System.Collections.Generic;

namespace HillNGun.Feel
{
    public class EffectsManager : MonoBehaviour
    {
        private static EffectsManager _instance;
        public static EffectsManager Instance => _instance;

        private BattlefieldSystem _battlefield;
        
        private List<MuzzleFlash> _muzzleFlashPool = new List<MuzzleFlash>();
        private List<Tracer> _tracerPool = new List<Tracer>();
        private List<ImpactEffect> _impactPool = new List<ImpactEffect>();
        private List<DamageNumber> _damageNumberPool = new List<DamageNumber>();
        private List<ExplosionRing> _explosionPool = new List<ExplosionRing>();
        private List<CartoonExplosion> _cartoonBoomPool = new List<CartoonExplosion>();
        private List<DebrisBurst> _debrisPool = new List<DebrisBurst>();
        private HillNGun.Data.FeelData _feel;

        private const int POOL_SIZE = 20;

        private void Awake()
        {
            _instance = this;
            ServiceLocator.Register(this);
            InitializePools();
        }

        private void Start()
        {
            _battlefield = ServiceLocator.Get<BattlefieldSystem>();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<EffectsManager>();
            _instance = null;
        }

        private void InitializePools()
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                _muzzleFlashPool.Add(new MuzzleFlash());
                _tracerPool.Add(new Tracer());
                _impactPool.Add(new ImpactEffect());
                var dn = new DamageNumber();
                dn.Bind(transform, i);
                _damageNumberPool.Add(dn);
                _explosionPool.Add(new ExplosionRing());
                if (i < 8) _cartoonBoomPool.Add(new CartoonExplosion());
                if (i < 6) _debrisPool.Add(new DebrisBurst());
            }
        }

        public void SpawnMuzzleFlash(Vector2 position, Vector2 direction, bool isShotgun)
            => SpawnMuzzleFlash(position, direction, isShotgun, 1f);

        /// <summary>
        /// Muzzle flash at the gun muzzle (position + aim * forwardOffset), oriented along the on-screen direction of the
        /// shot (perspective-aware), size/lifetime from FeelData. muzzleForward = WeaponData.muzzleForwardOffset.
        /// </summary>
        public void SpawnMuzzleFlash(Vector2 position, Vector2 direction, bool isShotgun, float muzzleForward)
            => SpawnMuzzleFlashPattern(position, direction, isShotgun, muzzleForward, null);

        public void SpawnMuzzleFlashPattern(Vector2 position, Vector2 direction, bool isShotgun, float muzzleForward, HillNGun.Data.WeaponVfxData vfx)
        {
            if (_feel == null) _feel = ServiceLocator.Get<HillNGun.Data.FeelData>();
            float size = _feel != null ? _feel.muzzleFlashSize : 1.05f;
            float time = _feel != null ? _feel.muzzleFlashTime : 0.1f;
            float charMul = ScalePresets.CharacterMul; // muzzle sits on the drawn gun: follows the character scale preset
            size *= charMul;
            float fwd = (_feel != null ? _feel.muzzleFlashForward : 0.55f) * Mathf.Max(0f, muzzleForward) * charMul;
            Vector2 d = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector2.up;
            Vector2 muzzle = position + d * fwd;
            Vector3 w0 = ToWorld(muzzle);
            Vector3 w1 = ToWorld(muzzle + d * 0.5f);
            Vector2 wd = (Vector2)(w1 - w0);
            if (wd.sqrMagnitude < 1e-8f) wd = d;
            float persp = Battlefield.FieldView.PerspectiveScale(muzzle.y);
            float mul = isShotgun || (vfx != null && vfx.isPellet) ? 1.25f : (vfx != null && vfx.isSlug ? 1.15f : 1f);
            var flash = GetFromPool(_muzzleFlashPool);
            flash.Spawn(w0, wd.normalized, isShotgun || (vfx != null && vfx.isPellet), size * persp * mul, time);
            if (vfx != null) flash.Tint = vfx.muzzleColor;
        }

        public void SpawnPatternImpact(Vector2 position, bool isCover, HillNGun.Data.WeaponVfxData vfx)
        {
            VfxPool.Ensure();
            string id = vfx == null ? (isCover ? "impact_wood" : "impact_stars")
                : (isCover ? vfx.impactCoverId : vfx.impactEnemyId);
            VfxPool.Instance?.SpawnImpact(position, id, vfx);
            if (isCover) SpawnImpact(position, true); // keep existing wood chip GL
        }

        private Vector3 ToWorld(Vector2 logic) => _battlefield?.LogicToWorldPosition(logic) ?? Battlefield.FieldView.ToWorld(logic);

        /// <summary>Cover destroyed: splinters fly out + dust puffs (view only, pooled).</summary>
        public void SpawnCoverDebris(Vector2 position, float width, bool isWood)
        {
            if (_feel == null) _feel = ServiceLocator.Get<HillNGun.Data.FeelData>();
            int n = _feel != null ? _feel.debrisSplinters : 16;
            float life = _feel != null ? _feel.debrisLifetime : 0.75f;
            float persp = Battlefield.FieldView.PerspectiveScale(position.y);
            GetFromPool(_debrisPool).Spawn(ToWorld(position), Mathf.Max(0.6f, width) * persp, persp, isWood, n, life);
            SpawnImpact(position, isWood);
        }

        public void SpawnTracer(Vector2 start, Vector2 end, Team team)
        {
            var tracer = GetFromPool(_tracerPool);
            Vector3 startWorld = _battlefield?.LogicToWorldPosition(start) ?? (Vector3)start;
            Vector3 endWorld = _battlefield?.LogicToWorldPosition(end) ?? (Vector3)end;
            tracer.Spawn(startWorld, endWorld, team);
        }

        public void SpawnImpact(Vector2 position, bool isWood)
        {
            var impact = GetFromPool(_impactPool);
            Vector3 worldPos = _battlefield?.LogicToWorldPosition(position) ?? (Vector3)position;
            impact.Spawn(worldPos, isWood);
        }

        public void SpawnDamageNumber(Vector2 position, int damage, bool isCrit)
        {
            var dmgNum = GetFromPool(_damageNumberPool);
            Vector3 worldPos = _battlefield?.LogicToWorldPosition(position) ?? (Vector3)position;
            dmgNum.Spawn(worldPos, damage, isCrit);
        }

        /// <summary>Floating label (skill / revive / weak point) at a logic position.</summary>
        public void SpawnFloatingText(Vector2 position, string text, Color color)
        {
            var dmgNum = GetFromPool(_damageNumberPool);
            Vector3 worldPos = _battlefield?.LogicToWorldPosition(position) ?? (Vector3)position;
            dmgNum.SpawnText(worldPos, text, color);
        }

        public void SpawnExplosion(Vector2 position, float radius)
        {
            var explosion = GetFromPool(_explosionPool);
            Vector3 worldPos = _battlefield?.LogicToWorldPosition(position) ?? (Vector3)position;
            explosion.Spawn(worldPos, radius);
        }

        /// <summary>12+ cartoon grenade burst: ring + stars + sparks + smoke. No blood.</summary>
        public void SpawnCartoonExplosion(Vector2 position, float radius, HillNGun.Data.GrenadeVfxData vfx = null)
        {
            vfx = vfx != null ? vfx : HillNGun.Data.GrenadeVfxData.Builtin;
            Vector3 worldPos = _battlefield?.LogicToWorldPosition(position) ?? Battlefield.FieldView.ToWorld(position);
            float persp = Battlefield.FieldView.PerspectiveScale(position.y);
            float worldR = Mathf.Max(0.4f, radius) * persp;
            GetFromPool(_cartoonBoomPool).Spawn(worldPos, worldR, vfx);
            // keep legacy ring for readability on tiny screens
            GetFromPool(_explosionPool).Spawn(worldPos, worldR);
        }

        private T GetFromPool<T>(List<T> pool) where T : IPooledEffect
        {
            foreach (var item in pool)
            {
                if (!item.IsActive)
                    return item;
            }
            return pool[0];
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var f in _muzzleFlashPool) f.Update(dt);
            foreach (var t in _tracerPool) t.Update(dt);
            foreach (var i in _impactPool) i.Update(dt);
            foreach (var d in _damageNumberPool) d.Update(dt);
            foreach (var e in _explosionPool) e.Update(dt);
            foreach (var c in _cartoonBoomPool) c.Update(dt);
            foreach (var b in _debrisPool) b.Update(dt);
        }

        private static Material _fxMat;
        public static Material SharedFxMaterial => _fxMat ?? (_fxMat = MakeFxMat());
        private static Material MakeFxMat()
        {
            var m = new Material(Shader.Find("Sprites/Default"));
            m.SetInt("_ZWrite", 0);
            return m;
        }

        /// <summary>Active pooled effects (perf diagnostics).</summary>
        public static string ActiveSummary
        {
            get
            {
                var m = FindAnyObjectByType<EffectsManager>();
                if (m == null) return "-";
                int f = 0, t = 0, i = 0, d = 0, e = 0;
                foreach (var x in m._muzzleFlashPool) if (x.IsActive) f++;
                foreach (var x in m._tracerPool) if (x.IsActive) t++;
                foreach (var x in m._impactPool) if (x.IsActive) i++;
                foreach (var x in m._damageNumberPool) if (x.IsActive) d++;
                foreach (var x in m._explosionPool) if (x.IsActive) e++;
                return $"flash{f}/tracer{t}/impact{i}/dmgNum{d}/ring{e}";
            }
        }

        /// <summary>
        /// Immediate-mode effects, drawn once per frame on the top-most field camera (the 3D actor overlay when present,
        /// so flashes sit above the 3D models) and batched: one SetPass + one GL batch per primitive type.
        /// </summary>
        private void OnRenderObject()
        {
            var cam = Camera.current;
            var target = HillNGun.View3D.Actor3DStage.OverlayCamera != null ? HillNGun.View3D.Actor3DStage.OverlayCamera : Camera.main;
            if (cam == null || cam != target) return;
            bool any = false;
            foreach (var f in _muzzleFlashPool) any |= f.IsActive;
            foreach (var t in _tracerPool) any |= t.IsActive;
            foreach (var i in _impactPool) any |= i.IsActive;
            foreach (var e in _explosionPool) any |= e.IsActive;
            foreach (var c in _cartoonBoomPool) any |= c.IsActive;
            foreach (var b in _debrisPool) any |= b.IsActive;
            if (!any) return;
            SharedFxMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.TRIANGLES);
            foreach (var b in _debrisPool) b.EmitDust();
            foreach (var c in _cartoonBoomPool) c.EmitTris();
            foreach (var f in _muzzleFlashPool) f.Emit();
            foreach (var b in _debrisPool) b.EmitSplinters();
            GL.End();
            GL.Begin(GL.LINES);
            foreach (var t in _tracerPool) t.Emit();
            foreach (var e in _explosionPool) e.Emit();
            foreach (var c in _cartoonBoomPool) c.EmitLines();
            GL.End();
            GL.Begin(GL.QUADS);
            foreach (var i in _impactPool) i.Emit();
            GL.End();
            GL.PopMatrix();
        }

        private void OnGUI()
        {
            if (HillNGun.DebugTools.SmokeTestRunner.NoGui) return;
            // Damage numbers / floating labels are world-space TextMeshes now (one shared font material, batched).
        }
    }

    public interface IPooledEffect
    {
        bool IsActive { get; }
        void Update(float dt);
    }

    public class MuzzleFlash : IPooledEffect
    {
        public Color Tint = new Color(1f, 0.55f, 0.12f, 1f);
        public bool IsActive { get; private set; }
        private Vector3 _position;
        private Vector2 _direction;
        private float _timer, _life;
        private float _size;
        private bool _isShotgun;
        private float _spin;

        public void Spawn(Vector3 pos, Vector2 dir, bool shotgun) => Spawn(pos, dir, shotgun, shotgun ? 0.8f : 0.5f, 0.08f);

        public void Spawn(Vector3 pos, Vector2 dir, bool shotgun, float size, float life)
        {
            Tint = new Color(1f, 0.55f, 0.12f, 1f);
            _position = pos;
            _direction = dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector2.up;
            _life = Mathf.Max(0.02f, life);
            _timer = _life;
            _isShotgun = shotgun;
            _size = size;
            _spin = Random.Range(0f, 360f);
            IsActive = true;
        }

        public void Update(float dt)
        {
            if (!IsActive) return;
            _timer -= dt;
            if (_timer <= 0) IsActive = false;
        }

        /// <summary>Triangles: hot core star, forward flame tongue(s), side sparks. Bright for the first half, then fades.</summary>
        public void Emit()
        {
            if (!IsActive) return;
            float k = Mathf.Clamp01(_timer / _life);            // 1 → 0
            float grow = Mathf.Lerp(1.15f, 0.7f, k);              // pops out fast
            float alpha = Mathf.Clamp01(k * 1.6f);
            Vector3 f = (Vector3)_direction;
            Vector3 r = new Vector3(-_direction.y, _direction.x, 0f);
            float s = _size * grow;

            // outer flame
            GL.Color(new Color(Tint.r, Tint.g, Tint.b, alpha * 0.85f));
            Tri(_position - f * s * 0.12f, _position + f * s + r * s * 0.16f, _position + f * s - r * s * 0.16f);
            Tri(_position, _position + f * s * 0.45f + r * s * 0.32f, _position + f * s * 0.62f);
            Tri(_position, _position + f * s * 0.45f - r * s * 0.32f, _position + f * s * 0.62f);
            if (_isShotgun)
            {
                Vector3 f1 = Quaternion.Euler(0, 0, 24) * f, f2 = Quaternion.Euler(0, 0, -24) * f;
                Tri(_position, _position + f1 * s * 0.8f + r * s * 0.1f, _position + f1 * s * 0.8f - r * s * 0.1f);
                Tri(_position, _position + f2 * s * 0.8f + r * s * 0.1f, _position + f2 * s * 0.8f - r * s * 0.1f);
            }
            // hot core star (4 points, random spin per shot)
            GL.Color(new Color(1f, 0.97f, 0.75f, alpha));
            float cs = s * 0.26f;
            for (int i = 0; i < 4; i++)
            {
                float a0 = (_spin + i * 90f) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f);
                Vector3 q = new Vector3(-p.y, p.x, 0f);
                Tri(_position + f * s * 0.1f + p * cs, _position + f * s * 0.1f + q * cs * 0.3f, _position + f * s * 0.1f - q * cs * 0.3f);
            }
            GL.Color(new Color(1f, 1f, 0.92f, alpha));
            Tri(_position + f * s * 0.62f, _position + r * s * 0.09f, _position - r * s * 0.09f);
        }

        private static void Tri(Vector3 a, Vector3 b, Vector3 c) { GL.Vertex(a); GL.Vertex(b); GL.Vertex(c); }
    }

    /// <summary>Cover destruction: splinters (spinning slivers with gravity) + expanding dust puffs.</summary>
    public class DebrisBurst : IPooledEffect
    {
        public bool IsActive { get; private set; }
        private const int Max = 28;
        private readonly Vector3[] _p = new Vector3[Max];
        private readonly Vector3[] _v = new Vector3[Max];
        private readonly float[] _rot = new float[Max];
        private readonly float[] _spin = new float[Max];
        private readonly float[] _len = new float[Max];
        private readonly Vector3[] _dust = new Vector3[6];
        private int _n;
        private float _timer, _life, _scale, _width;
        private Vector3 _origin;
        private bool _wood;

        public void Spawn(Vector3 pos, float width, float scale, bool wood, int splinters, float life)
        {
            _origin = pos; _width = width; _scale = scale; _wood = wood;
            _life = Mathf.Max(0.2f, life); _timer = _life;
            _n = Mathf.Clamp(splinters, 4, Max);
            for (int i = 0; i < _n; i++)
            {
                float a = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                float sp = Random.Range(2.5f, 6.5f) * scale;
                _p[i] = pos + new Vector3(Random.Range(-0.5f, 0.5f) * width, Random.Range(0f, 0.5f) * scale, 0f);
                _v[i] = new Vector3(Mathf.Cos(a) * sp, Mathf.Sin(a) * sp, 0f);
                _rot[i] = Random.Range(0f, 360f);
                _spin[i] = Random.Range(-720f, 720f);
                _len[i] = Random.Range(0.12f, 0.3f) * scale;
            }
            for (int i = 0; i < _dust.Length; i++)
                _dust[i] = pos + new Vector3((i / 5f - 0.5f) * width, Random.Range(0.05f, 0.35f) * scale, 0f);
            IsActive = true;
        }

        public void Update(float dt)
        {
            if (!IsActive) return;
            _timer -= dt;
            for (int i = 0; i < _n; i++)
            {
                _v[i].y -= 14f * _scale * dt;
                _p[i] += _v[i] * dt;
                _rot[i] += _spin[i] * dt;
            }
            if (_timer <= 0f) IsActive = false;
        }

        public void EmitDust()
        {
            if (!IsActive) return;
            float k = 1f - _timer / _life;
            float a = 0.5f * (1f - k);
            GL.Color(_wood ? new Color(0.78f, 0.66f, 0.5f, a) : new Color(0.72f, 0.7f, 0.66f, a));
            float r = (0.25f + 0.65f * Mathf.Sqrt(k)) * _scale;
            for (int i = 0; i < _dust.Length; i++)
            {
                Vector3 c = _dust[i] + new Vector3(0f, k * 0.4f * _scale, 0f);
                const int seg = 10;
                for (int j = 0; j < seg; j++)
                {
                    float a0 = j * Mathf.PI * 2f / seg, a1 = (j + 1) * Mathf.PI * 2f / seg;
                    GL.Vertex(c);
                    GL.Vertex(c + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0) * 0.7f, 0f) * r);
                    GL.Vertex(c + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1) * 0.7f, 0f) * r);
                }
            }
        }

        public void EmitSplinters()
        {
            if (!IsActive) return;
            float a = Mathf.Clamp01(_timer / _life * 1.5f);
            for (int i = 0; i < _n; i++)
            {
                GL.Color(_wood ? (i % 3 == 0 ? new Color(0.86f, 0.66f, 0.4f, a) : new Color(0.5f, 0.32f, 0.16f, a))
                               : new Color(0.55f, 0.55f, 0.58f, a));
                float rr = _rot[i] * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(rr), Mathf.Sin(rr), 0f) * _len[i];
                Vector3 q = new Vector3(-d.y, d.x, 0f) * 0.22f;
                GL.Vertex(_p[i] - d - q); GL.Vertex(_p[i] + d); GL.Vertex(_p[i] - d + q);
            }
        }
    }

    public class Tracer : IPooledEffect
    {
        public bool IsActive { get; private set; }
        private Vector3 _start, _end, _current;
        private float _timer;
        private Color _color;
        private static Material _mat;

        public void Spawn(Vector3 start, Vector3 end, Team team)
        {
            _start = start;
            _end = end;
            _current = start;
            _timer = 0.15f;
            _color = team == Team.Player ? new Color(1f, 0.9f, 0.5f) : new Color(1f, 0.5f, 0.3f);
            IsActive = true;
        }

        public void Update(float dt)
        {
            if (!IsActive) return;
            _timer -= dt;
            float t = 1f - (_timer / 0.15f);
            _current = Vector3.Lerp(_start, _end, Mathf.Min(t * 2f, 1f));
            if (_timer <= 0) IsActive = false;
        }

        public void Emit()
        {
            if (!IsActive) return;


            float alpha = _timer / 0.15f;
            GL.Color(new Color(_color.r, _color.g, _color.b, alpha));

            float t = 1f - (_timer / 0.15f);
            Vector3 trailStart = Vector3.Lerp(_start, _end, Mathf.Max(t * 2f - 0.5f, 0f));
            GL.Vertex(trailStart);
            GL.Vertex(_current);

        }
    }

    public class ImpactEffect : IPooledEffect
    {
        public bool IsActive { get; private set; }
        private Vector3 _position;
        private float _timer;
        private bool _isWood;
        private Vector3[] _particles = new Vector3[8];
        private Vector3[] _velocities = new Vector3[8];
        private static Material _mat;

        public void Spawn(Vector3 pos, bool isWood)
        {
            _position = pos;
            _timer = 0.4f;
            _isWood = isWood;
            IsActive = true;

            for (int i = 0; i < _particles.Length; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(1f, 3f);
                _particles[i] = pos;
                _velocities[i] = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 2f, 0);
            }
        }

        public void Update(float dt)
        {
            if (!IsActive) return;
            _timer -= dt;
            
            for (int i = 0; i < _particles.Length; i++)
            {
                _velocities[i].y -= 15f * dt;
                _particles[i] += _velocities[i] * dt;
            }

            if (_timer <= 0) IsActive = false;
        }

        public void Emit()
        {
            if (!IsActive) return;


            float alpha = _timer / 0.4f;
            Color color = _isWood ? new Color(0.6f, 0.4f, 0.2f, alpha) : new Color(0.7f, 0.6f, 0.5f, alpha);
            GL.Color(color);

            float size = _isWood ? 0.08f : 0.05f;
            foreach (var p in _particles)
            {
                GL.Vertex(p + new Vector3(-size, -size, 0));
                GL.Vertex(p + new Vector3(size, -size, 0));
                GL.Vertex(p + new Vector3(size, size, 0));
                GL.Vertex(p + new Vector3(-size, size, 0));
            }

        }
    }

    /// <summary>
    /// Floating damage number / label. World-space TextMesh pair (text + drop shadow) sharing the built-in font
    /// material, so all numbers batch instead of costing one IMGUI draw call each. Drawn on the 3D overlay layer
    /// when 3D actors are on (so numbers stay above the models), otherwise on the default layer above all sprites.
    /// </summary>
    public class DamageNumber : IPooledEffect
    {
        public bool IsActive { get; private set; }
        private Vector3 _position;
        private float _timer;
        private int _damage;
        private bool _isCrit;
        private float _yOffset;
        private string _text;
        private Color _color;

        private GameObject _go;
        private TextMesh _main, _shadow;
        private MeshRenderer _mainR, _shadowR;
        private const float CharSize = 0.11f;

        public void Bind(Transform parent, int index)
        {
            _go = new GameObject("DamageNumber_" + index);
            _go.transform.SetParent(parent, false);
            _shadow = MakeText(_go.transform, "Shadow", new Vector3(0.04f, -0.04f, 0.01f), out _shadowR, 32000);
            _main = MakeText(_go.transform, "Text", Vector3.zero, out _mainR, 32001);
            _go.SetActive(false);
        }

        private static TextMesh MakeText(Transform parent, string name, Vector3 local, out MeshRenderer r, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var tm = go.AddComponent<TextMesh>();
            tm.fontSize = 48;
            tm.characterSize = CharSize;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            r = go.GetComponent<MeshRenderer>();
            r.sortingOrder = order;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return tm;
        }

        public void Spawn(Vector3 pos, int damage, bool isCrit)
        {
            _position = pos;
            _timer = 0.8f;
            _damage = damage;
            _isCrit = isCrit;
            _yOffset = 0;
            _text = null;
            IsActive = true;
            Show(damage.ToString(), false);
        }

        public void SpawnText(Vector3 pos, string text, Color color)
        {
            _position = pos;
            _timer = 1.4f;
            _text = text;
            _color = color;
            _isCrit = false;
            _yOffset = 0;
            IsActive = true;
            Show(text, true);
        }

        private void Show(string text, bool shadow)
        {
            if (_go == null) return;
            int layer = HillNGun.View3D.Actor3DStage.OverlayCamera != null ? HillNGun.View3D.Actor3DStage.Data.actorLayer : 0;
            _go.layer = layer; _main.gameObject.layer = layer; _shadow.gameObject.layer = layer;
            _main.text = text;
            _shadow.text = shadow ? text : "";
            _shadowR.enabled = shadow;
            _go.SetActive(true);
            Apply();
        }

        public void Update(float dt)
        {
            if (!IsActive) return;
            _timer -= dt;
            _yOffset += dt * (_text != null ? 0.8f : 2f);
            if (_timer <= 0) { IsActive = false; if (_go != null) _go.SetActive(false); return; }
            Apply();
        }

        private void Apply()
        {
            if (_go == null) return;
            float alpha = Mathf.Min(_timer / 0.3f, 1f);
            float scale = _isCrit ? 1.5f : 1f;
            if (_text == null && _timer > 0.6f) scale *= 1f + ((_timer - 0.6f) / 0.2f) * 0.5f;
            if (_text != null) scale = 0.92f;
            _go.transform.position = new Vector3(_position.x, _position.y + _yOffset, 0f);
            _go.transform.localScale = Vector3.one * scale;
            Color c = _text != null ? new Color(_color.r, _color.g, _color.b, alpha)
                    : _isCrit ? new Color(1f, 0.8f, 0.2f, alpha) : new Color(1f, 1f, 1f, alpha);
            _main.color = c;
            if (_text != null) _shadow.color = new Color(0f, 0f, 0f, alpha * 0.8f);
        }
    }

    public class ExplosionRing : IPooledEffect
    {
        public bool IsActive { get; private set; }
        private Vector3 _position;
        private float _timer;
        private float _maxRadius;
        private static Material _mat;

        public void Spawn(Vector3 pos, float radius)
        {
            _position = pos;
            _timer = 0.5f;
            _maxRadius = radius;
            IsActive = true;
        }

        public void Update(float dt)
        {
            if (!IsActive) return;
            _timer -= dt;
            if (_timer <= 0) IsActive = false;
        }

        public void Emit()
        {
            if (!IsActive) return;


            float progress = 1f - (_timer / 0.5f);
            float radius = _maxRadius * Mathf.Sin(progress * Mathf.PI * 0.5f);
            float alpha = _timer / 0.5f;

            GL.Color(new Color(1f, 0.6f, 0.2f, alpha));

            int segments = 32;
            for (int i = 0; i < segments; i++)
            {
                float a1 = (i / (float)segments) * Mathf.PI * 2f;
                float a2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                GL.Vertex(_position + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * radius);
                GL.Vertex(_position + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2), 0) * radius);
            }

            float innerRadius = radius * 0.6f;
            GL.Color(new Color(1f, 0.9f, 0.5f, alpha * 0.5f));
            for (int i = 0; i < segments; i++)
            {
                float a1 = (i / (float)segments) * Mathf.PI * 2f;
                float a2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                GL.Vertex(_position + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * innerRadius);
                GL.Vertex(_position + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2), 0) * innerRadius);
            }

        }
    }

    /// <summary>Cartoon grenade boom: expanding flame disc, star burst, sparks, smoke puffs. Never blood-red.</summary>
    public class CartoonExplosion : IPooledEffect
    {
        public bool IsActive { get; private set; }
        private Vector3 _origin;
        private float _timer, _life, _radius;
        private Color _flame, _spark, _star, _smoke;
        private int _stars, _sparks, _smokes;
        private readonly Vector3[] _sparkP = new Vector3[16];
        private readonly Vector3[] _sparkV = new Vector3[16];
        private readonly float[] _starAng = new float[12];
        private readonly Vector3[] _smokeP = new Vector3[8];
        private readonly float[] _smokeR = new float[8];

        public void Spawn(Vector3 pos, float radius, HillNGun.Data.GrenadeVfxData vfx)
        {
            _origin = pos;
            _radius = Mathf.Max(0.3f, radius);
            _life = Mathf.Max(0.2f, vfx.explodeDuration);
            _timer = _life;
            _flame = vfx.flameColor;
            _spark = vfx.sparkColor;
            _star = vfx.starColor;
            _smoke = vfx.smokeColor;
            _stars = Mathf.Clamp(vfx.starCount, 4, 12);
            _sparks = Mathf.Clamp(vfx.sparkCount, 4, 16);
            _smokes = Mathf.Clamp(vfx.smokePuffs, 2, 8);
            for (int i = 0; i < _sparks; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float sp = Random.Range(2.2f, 5.5f) * _radius;
                _sparkP[i] = pos;
                _sparkV[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.85f + 0.6f, 0f) * sp;
            }
            for (int i = 0; i < _stars; i++)
                _starAng[i] = (i / (float)_stars) * 360f + Random.Range(-8f, 8f);
            for (int i = 0; i < _smokes; i++)
            {
                float a = (i / (float)_smokes) * Mathf.PI * 2f;
                _smokeP[i] = pos + new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.6f, 0f) * _radius * 0.25f;
                _smokeR[i] = _radius * Random.Range(0.2f, 0.4f);
            }
            IsActive = true;
        }

        public void Update(float dt)
        {
            if (!IsActive) return;
            _timer -= dt;
            for (int i = 0; i < _sparks; i++)
            {
                _sparkV[i].y -= 9f * dt;
                _sparkP[i] += _sparkV[i] * dt;
                _sparkV[i] *= 0.96f;
            }
            for (int i = 0; i < _smokes; i++)
            {
                _smokeP[i] += Vector3.up * (0.6f * dt);
                _smokeR[i] += _radius * 0.55f * dt;
            }
            if (_timer <= 0f) IsActive = false;
        }

        public void EmitTris()
        {
            if (!IsActive) return;
            float k = 1f - _timer / _life; // 0..1
            float pop = Mathf.Sin(Mathf.Clamp01(k * 1.4f) * Mathf.PI); // pop then fade
            float a = Mathf.Clamp01((_timer / _life) * 1.5f);

            // flame disc
            GL.Color(new Color(_flame.r, _flame.g, _flame.b, a * 0.85f));
            float fr = _radius * (0.35f + 0.75f * pop);
            const int seg = 14;
            for (int j = 0; j < seg; j++)
            {
                float a0 = j * Mathf.PI * 2f / seg, a1 = (j + 1) * Mathf.PI * 2f / seg;
                GL.Vertex(_origin);
                GL.Vertex(_origin + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * fr);
                GL.Vertex(_origin + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * fr);
            }
            // hot core
            GL.Color(new Color(1f, 0.97f, 0.7f, a));
            float cr = fr * 0.35f;
            for (int j = 0; j < seg; j++)
            {
                float a0 = j * Mathf.PI * 2f / seg, a1 = (j + 1) * Mathf.PI * 2f / seg;
                GL.Vertex(_origin);
                GL.Vertex(_origin + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * cr);
                GL.Vertex(_origin + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * cr);
            }
            // stars
            GL.Color(new Color(_star.r, _star.g, _star.b, a));
            float reach = _radius * (0.55f + 0.55f * pop);
            for (int i = 0; i < _stars; i++)
            {
                float ang = _starAng[i] * Mathf.Deg2Rad;
                Vector3 tip = _origin + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * reach;
                Vector3 side = new Vector3(-Mathf.Sin(ang), Mathf.Cos(ang), 0f) * (_radius * 0.08f);
                GL.Vertex(_origin + (tip - _origin) * 0.25f);
                GL.Vertex(tip + side);
                GL.Vertex(tip - side);
            }
            // smoke
            float sa = a * 0.55f;
            GL.Color(new Color(_smoke.r, _smoke.g, _smoke.b, sa));
            for (int i = 0; i < _smokes; i++)
            {
                Vector3 c = _smokeP[i];
                float r = _smokeR[i];
                for (int j = 0; j < 10; j++)
                {
                    float a0 = j * Mathf.PI * 2f / 10f, a1 = (j + 1) * Mathf.PI * 2f / 10f;
                    GL.Vertex(c);
                    GL.Vertex(c + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0) * 0.7f, 0f) * r);
                    GL.Vertex(c + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1) * 0.7f, 0f) * r);
                }
            }
        }

        public void EmitLines()
        {
            if (!IsActive) return;
            float a = Mathf.Clamp01(_timer / _life * 1.4f);
            GL.Color(new Color(_spark.r, _spark.g, _spark.b, a));
            for (int i = 0; i < _sparks; i++)
            {
                Vector3 p = _sparkP[i];
                Vector3 d = _sparkV[i].normalized * (_radius * 0.12f);
                GL.Vertex(p - d);
                GL.Vertex(p + d);
            }
        }
    }

}

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
            }
        }

        public void SpawnMuzzleFlash(Vector2 position, Vector2 direction, bool isShotgun)
        {
            var flash = GetFromPool(_muzzleFlashPool);
            Vector3 worldPos = _battlefield?.LogicToWorldPosition(position) ?? (Vector3)position;
            flash.Spawn(worldPos, direction, isShotgun);
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
        }

        private static Material _fxMat;

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
            if (!any) return;
            if (_fxMat == null)
            {
                _fxMat = new Material(Shader.Find("Sprites/Default"));
                _fxMat.SetInt("_ZWrite", 0);
            }
            _fxMat.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.TRIANGLES);
            foreach (var f in _muzzleFlashPool) f.Emit();
            GL.End();
            GL.Begin(GL.LINES);
            foreach (var t in _tracerPool) t.Emit();
            foreach (var e in _explosionPool) e.Emit();
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
        public bool IsActive { get; private set; }
        private Vector3 _position;
        private Vector2 _direction;
        private float _timer;
        private float _size;
        private bool _isShotgun;
        private static Material _mat;

        public void Spawn(Vector3 pos, Vector2 dir, bool shotgun)
        {
            _position = pos;
            _direction = dir.normalized;
            _timer = 0.08f;
            _isShotgun = shotgun;
            _size = shotgun ? 0.8f : 0.5f;
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


            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            float alpha = _timer / 0.08f;

            GL.Color(new Color(1f, 0.9f, 0.3f, alpha));

            Vector3 forward = (Vector3)_direction * _size;
            Vector3 right = new Vector3(-_direction.y, _direction.x, 0) * _size * 0.4f;

            GL.Vertex(_position);
            GL.Vertex(_position + forward + right * 0.5f);
            GL.Vertex(_position + forward - right * 0.5f);

            if (_isShotgun)
            {
                Vector3 r1 = Quaternion.Euler(0, 0, 20) * forward;
                Vector3 r2 = Quaternion.Euler(0, 0, -20) * forward;
                GL.Vertex(_position);
                GL.Vertex(_position + r1 * 0.7f + right * 0.3f);
                GL.Vertex(_position + r1 * 0.7f - right * 0.3f);
                GL.Vertex(_position);
                GL.Vertex(_position + r2 * 0.7f + right * 0.3f);
                GL.Vertex(_position + r2 * 0.7f - right * 0.3f);
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
}

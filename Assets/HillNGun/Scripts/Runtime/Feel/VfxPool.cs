using UnityEngine;
using System.Collections.Generic;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.Feel
{
    public enum VfxQualityTier { High, Low }

    /// <summary>Pooled impact particles for fire VFX (08). Low tier = half particle count. No blood colors.</summary>
    public class VfxPool : MonoBehaviour
    {
        public static VfxPool Instance { get; private set; }
        public static VfxQualityTier Tier = VfxQualityTier.High;

        private readonly List<Burst> _bursts = new List<Burst>(32);
        private static readonly Color[] StarColors = {
            new Color(1f, 1f, 1f), new Color(1f, 0.95f, 0.55f), new Color(1f, 0.85f, 0.35f),
            new Color(0.9f, 0.9f, 0.95f), new Color(1f, 0.78f, 0.4f)
        };
        private static readonly Color[] DustColors = {
            new Color(0.65f, 0.55f, 0.4f), new Color(0.55f, 0.45f, 0.32f), new Color(0.72f, 0.62f, 0.48f)
        };
        private static readonly Color[] WoodColors = {
            new Color(0.55f, 0.35f, 0.18f), new Color(0.45f, 0.28f, 0.14f), new Color(0.62f, 0.42f, 0.22f)
        };
        private static readonly Color[] SparkColors = {
            new Color(1f, 0.95f, 0.4f), new Color(1f, 0.85f, 0.2f), new Color(1f, 1f, 0.7f)
        };

        // Blood-like reds are forbidden (12+). Exposed for tests.
        public static readonly Color[] ForbiddenBloodReds = {
            new Color(0.8f, 0.05f, 0.05f), new Color(0.6f, 0f, 0f), new Color(0.9f, 0.1f, 0.15f)
        };

        private void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
            for (int i = 0; i < 24; i++) _bursts.Add(new Burst());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ServiceLocator.Unregister<VfxPool>();
        }

        public static void Ensure()
        {
            if (Instance != null) return;
            var go = new GameObject("[VfxPool]");
            go.AddComponent<VfxPool>();
        }

        public int ParticleMul => Tier == VfxQualityTier.Low ? 1 : 2; // Low = half of High budget below

        public void SpawnImpact(Vector2 logicPos, string impactId, WeaponVfxData vfx = null)
        {
            int budget = vfx != null && vfx.particleBudgetOverride > 0 ? vfx.particleBudgetOverride
                : (Tier == VfxQualityTier.Low ? 4 : 8);
            Color[] palette;
            switch (impactId)
            {
                case "impact_wood": palette = WoodColors; break;
                case "impact_sparks": palette = SparkColors; break;
                case "impact_flash": palette = StarColors; break;
                default: palette = StarColors; break; // stars / dust — never blood
            }
            var b = Get();
            Vector3 w = Battlefield.FieldView.ToWorld(logicPos);
            b.Spawn(w, palette, budget, impactId == "impact_wood" ? 0.35f : 0.22f);
        }

        private Burst Get()
        {
            foreach (var b in _bursts) if (!b.Active) return b;
            return _bursts[0];
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var b in _bursts) b.Tick(dt);
        }

        private void OnRenderObject()
        {
            var cam = Camera.current;
            var target = HillNGun.View3D.Actor3DStage.OverlayCamera != null ? HillNGun.View3D.Actor3DStage.OverlayCamera : Camera.main;
            if (cam == null || cam != target) return;
            bool any = false;
            foreach (var b in _bursts) if (b.Active) { any = true; break; }
            if (!any) return;
            var mat = EffectsManager.SharedFxMaterial;
            if (mat == null) return;
            mat.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.TRIANGLES);
            foreach (var b in _bursts) b.Emit();
            GL.End();
            GL.PopMatrix();
        }

        private class Burst
        {
            public bool Active;
            private struct P { public Vector3 p, v; public Color c; public float life, max; public float size; }
            private P[] _ps = new P[16];
            private int _n;

            public void Spawn(Vector3 pos, Color[] palette, int count, float speed)
            {
                _n = Mathf.Min(count, _ps.Length);
                Active = true;
                for (int i = 0; i < _n; i++)
                {
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    float spd = speed * Random.Range(0.6f, 1.4f);
                    _ps[i] = new P {
                        p = pos, v = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * spd,
                        c = palette[i % palette.Length], life = 0f, max = Random.Range(0.15f, 0.28f),
                        size = Random.Range(0.04f, 0.09f)
                    };
                }
            }

            public void Tick(float dt)
            {
                if (!Active) return;
                bool alive = false;
                for (int i = 0; i < _n; i++)
                {
                    var p = _ps[i];
                    p.life += dt;
                    p.p += p.v * dt;
                    p.v *= 0.92f;
                    _ps[i] = p;
                    if (p.life < p.max) alive = true;
                }
                if (!alive) Active = false;
            }

            public void Emit()
            {
                if (!Active) return;
                for (int i = 0; i < _n; i++)
                {
                    var p = _ps[i];
                    if (p.life >= p.max) continue;
                    float a = 1f - p.life / p.max;
                    var c = p.c; c.a = a;
                    float s = p.size * (0.7f + 0.3f * a);
                    GL.Color(c);
                    GL.Vertex(p.p + new Vector3(-s, -s));
                    GL.Vertex(p.p + new Vector3(-s, s));
                    GL.Vertex(p.p + new Vector3(s, s));
                    GL.Vertex(p.p + new Vector3(-s, -s));
                    GL.Vertex(p.p + new Vector3(s, s));
                    GL.Vertex(p.p + new Vector3(s, -s));
                }
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>Muzzle flashes, casings, crate impacts, grenade puff + shockwave, dust, shake.</summary>
    public sealed class Proto_Greybox3DFx : MonoBehaviour
    {
        struct Burst
        {
            public Transform Xform;
            public float Age;
            public float Life;
            public Vector3 Vel;
            public Vector3 BaseScale;
            public int Kind; // 0 muzzle, 1 casing, 2 puff, 3 ring, 4 dust
        }

        readonly List<Burst> _live = new List<Burst>();
        readonly List<Burst> _scratch = new List<Burst>();
        float _shake;
        bool _exploding;
        Transform _motes;

        public float Shake => _shake;
        public Vector3 ShakeOffset =>
            _shake <= 0f ? Vector3.zero
                : new Vector3(Mathf.Sin(_shake * 73f), Mathf.Cos(_shake * 51f), 0f) * (0.055f * _shake);

        public static Proto_Greybox3DFx Ensure(Transform stage)
        {
            var existing = stage.GetComponentInChildren<Proto_Greybox3DFx>(true);
            if (existing != null) return existing;
            var go = new GameObject("Greybox3D_Fx");
            go.transform.SetParent(stage, false);
            var fx = go.AddComponent<Proto_Greybox3DFx>();
            fx.BuildMotes(stage);
            return fx;
        }

        void BuildMotes(Transform stage)
        {
            _motes = new GameObject("Greybox3D_DustMotes").transform;
            _motes.SetParent(stage, false);
            var ps = _motes.gameObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.duration = 4f;
            main.startLifetime = 3.5f;
            main.startSpeed = 0.12f;
            main.startSize = 0.06f;
            main.startColor = new Color(1f, 0.88f, 0.62f, 0.35f);
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.gravityModifier = 0.01f;
            var em = ps.emission;
            em.rateOverTime = 6f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(12f, 3f, 16f);
            _motes.position = new Vector3(0f, 1.4f, 8f);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Proto_Greybox3DMaterials.Glow(new Color(1f, 0.9f, 0.65f, 0.4f), 1.1f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (Application.isPlaying) ps.Play();
        }

        public void Tick(float dt)
        {
            if (_shake > 0f)
                _shake = Mathf.Max(0f, _shake - dt * 3.2f);

            _scratch.Clear();
            for (int i = 0; i < _live.Count; i++)
            {
                var b = _live[i];
                b.Age += dt;
                if (b.Xform == null || b.Age >= b.Life)
                {
                    if (b.Xform != null) Proto_Greybox3DUtil.DestroySafe(b.Xform.gameObject);
                    continue;
                }

                float t = b.Age / b.Life;
                if (b.Kind == 1)
                {
                    b.Vel += Vector3.down * 18f * dt;
                    b.Xform.position += b.Vel * dt;
                    b.Xform.Rotate(200f * dt, 90f * dt, 40f * dt, Space.World);
                }
                else if (b.Kind == 2 || b.Kind == 0)
                {
                    float s = b.Kind == 0
                        ? Mathf.Lerp(0.4f, 1.15f, t) * (1f - t)
                        : Mathf.Lerp(0.35f, 1.25f, Mathf.SmoothStep(0f, 1f, t));
                    b.Xform.localScale = b.BaseScale * s;
                    b.Xform.position += b.Vel * dt;
                    b.Vel *= 0.92f;
                }
                else if (b.Kind == 3)
                {
                    float s = Mathf.Lerp(0.4f, 1.6f, t);
                    b.Xform.localScale = new Vector3(b.BaseScale.x * s, b.BaseScale.y, b.BaseScale.z * s);
                }
                else if (b.Kind == 4)
                {
                    b.Xform.position += b.Vel * dt;
                    b.Xform.localScale = b.BaseScale * (1f - t);
                }

                FadeRenderer(b.Xform, 1f - t);
                _scratch.Add(b);
            }
            _live.Clear();
            _live.AddRange(_scratch);
        }

        public void Muzzle(Vector3 pos, Vector3 dir)
        {
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
            var go = MakeSphere("Muzzle", pos + dir * 0.12f, new Color(1f, 0.92f, 0.45f, 0.95f), 2.6f, 0.28f);
            _live.Add(new Burst { Xform = go, Life = 0.11f, BaseScale = go.localScale, Kind = 0, Vel = dir * 1.2f });
            var lobe = MakeSphere("MuzzleHot", pos + dir * 0.04f, new Color(1f, 1f, 0.85f, 0.9f), 3f, 0.14f);
            _live.Add(new Burst { Xform = lobe, Life = 0.08f, BaseScale = lobe.localScale, Kind = 0, Vel = dir * 0.4f });
        }

        public void Casing(Vector3 pos, Vector3 right)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Casing";
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.04f, 0.07f, 0.04f);
            Proto_Greybox3DUtil.DestroySafe(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial =
                Proto_Greybox3DMaterials.CelShared(new Color(0.92f, 0.78f, 0.28f), 0f);
            var vel = Vector3.up * 3.2f + right * 1.6f + Vector3.forward * Random.Range(-0.4f, 0.4f);
            _live.Add(new Burst { Xform = go.transform, Life = 0.55f, Vel = vel, BaseScale = go.transform.localScale, Kind = 1 });
        }

        public void Impact(Vector3 pos)
        {
            for (int i = 0; i < 4; i++)
            {
                var off = new Vector3(Mathf.Cos(i * 1.7f), 0.2f, Mathf.Sin(i * 1.7f)) * 0.15f;
                var go = MakeSphere("Impact", pos + off, new Color(0.95f, 0.82f, 0.55f, 0.85f), 1.4f, 0.22f);
                _live.Add(new Burst
                {
                    Xform = go, Life = 0.28f, BaseScale = go.localScale, Kind = 2,
                    Vel = (off + Vector3.up * 0.4f) * 3f
                });
            }
        }

        public void Explosion(Vector3 pos, float radius)
        {
            _shake = 1f;
            _exploding = true;
            Color[] puffs =
            {
                new Color(1f, 0.85f, 0.35f, 0.95f),
                new Color(1f, 0.55f, 0.15f, 0.9f),
                new Color(0.95f, 0.95f, 0.9f, 0.85f)
            };
            for (int i = 0; i < 8; i++)
            {
                float ang = i * (Mathf.PI * 2f / 8f);
                var off = new Vector3(Mathf.Cos(ang), 0.25f + (i % 3) * 0.15f, Mathf.Sin(ang)) * (radius * 0.25f);
                var col = puffs[i % puffs.Length];
                var go = MakeSphere("BoomPuff", pos + off, col, i % 2 == 0 ? 2.4f : 1.6f, 0.45f + (i % 3) * 0.12f);
                _live.Add(new Burst
                {
                    Xform = go, Life = 0.42f, BaseScale = go.localScale, Kind = 2,
                    Vel = (off.normalized + Vector3.up * 0.8f) * 3.4f
                });
            }

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Shockwave";
            ring.transform.SetParent(transform, false);
            ring.transform.position = pos + Vector3.up * 0.05f;
            ring.transform.localScale = new Vector3(radius * 0.6f, 0.03f, radius * 0.6f);
            Proto_Greybox3DUtil.DestroySafe(ring.GetComponent<Collider>());
            ring.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedRing;
            _live.Add(new Burst
            {
                Xform = ring.transform, Life = 0.4f,
                BaseScale = new Vector3(radius * 1.4f, 0.03f, radius * 1.4f), Kind = 3
            });
        }

        public void RollDust(Vector3 pos)
        {
            var go = MakeSphere("RollDust", pos + Vector3.up * 0.08f, new Color(0.85f, 0.65f, 0.35f, 0.7f), 1.1f, 0.28f);
            _live.Add(new Burst
            {
                Xform = go, Life = 0.32f, BaseScale = go.localScale, Kind = 4,
                Vel = Vector3.up * 0.6f
            });
        }

        Transform MakeSphere(string name, Vector3 pos, Color color, float intensity, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            Proto_Greybox3DUtil.DestroySafe(go.GetComponent<Collider>());
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Proto_Greybox3DMaterials.Glow(color, intensity);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static void FadeRenderer(Transform t, float a)
        {
            var r = t.GetComponent<MeshRenderer>();
            if (r == null || r.sharedMaterial == null) return;
            var m = r.sharedMaterial;
            var c = m.HasProperty("_Color") ? m.GetColor("_Color") : m.color;
            c.a = Mathf.Clamp01(a);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            m.color = c;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>Hit flash, knockback, tuck-roll, and bloodless death (fall + fade).</summary>
    public sealed class Proto_Greybox3DCombatFx : MonoBehaviour
    {
        public const float DeathDuration = 0.58f;
        const float FlashDuration = 0.14f;
        const float KnockbackDuration = 0.16f;

        readonly List<Renderer> _renderers = new List<Renderer>();
        readonly List<Color[]> _baseColors = new List<Color[]>();
        bool _bound;
        float _flashT;
        float _knockT;
        Vector3 _knockDir;
        float _deathT = -1f;
        bool _rolling;
        Transform _tuck;
        Transform _model;
        Vector3 _modelBaseScale = Vector3.one;
        bool _cachedModelScale;

        public bool IsDying => _deathT >= 0f && _deathT < DeathDuration;
        public bool IsGone => _deathT >= DeathDuration;
        public Vector3 KnockbackOffset =>
            _knockT > 0f ? _knockDir * (0.35f * (_knockT / KnockbackDuration)) : Vector3.zero;

        public void Bind()
        {
            if (_bound) return;
            _bound = true;
            _tuck = transform;
            _model = transform.Find("Model") ?? (transform.childCount > 0 ? transform.GetChild(0) : null);
            if (_model != null && !_cachedModelScale)
            {
                _modelBaseScale = _model.localScale;
                _cachedModelScale = true;
            }
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is TrailRenderer) continue;
                if (r.gameObject.name == "BlobShadow") continue;
                var shared = r.sharedMaterials;
                var inst = new Material[shared.Length];
                var cols = new Color[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                {
                    inst[i] = shared[i] != null ? new Material(shared[i]) : Proto_Greybox3DMaterials.Cel(Color.white);
                    cols[i] = inst[i].HasProperty("_Color") ? inst[i].GetColor("_Color") : inst[i].color;
                }
                r.sharedMaterials = inst;
                _renderers.Add(r);
                _baseColors.Add(cols);
            }
        }

        public void Tick(float dt, bool hit, bool down, bool rolling, Vector3 knockWorld)
        {
            Bind();
            if (hit && _deathT < 0f)
            {
                _flashT = FlashDuration;
                _knockT = KnockbackDuration;
                if (knockWorld.sqrMagnitude > 0.01f)
                    _knockDir = knockWorld.normalized;
                else
                    _knockDir = -transform.forward;
            }

            if (down && _deathT < 0f)
                _deathT = 0f;

            if (_flashT > 0f)
            {
                _flashT -= dt;
                float k = Mathf.Clamp01(_flashT / FlashDuration);
                ApplyFlash(k);
            }
            else if (_deathT < 0f)
                ApplyFlash(0f);

            if (_knockT > 0f)
                _knockT = Mathf.Max(0f, _knockT - dt);

            _rolling = rolling && _deathT < 0f;
            ApplyTuck(_rolling);

            if (_deathT >= 0f)
            {
                _deathT += dt;
                float t = Mathf.Clamp01(_deathT / DeathDuration);
                // Fall to the side and fade/squash into the ground — no blood.
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 82f, t * t * (3f - 2f * t)));
                float fade = 1f - t;
                ApplyDeathFade(fade);
                var s = transform.localScale;
                // keep XZ, squash Y slightly as they fall
                // scale is driven by presenter (height); only rotate + fade here.
                if (t >= 1f)
                    gameObject.SetActive(false);
            }
        }

        public void ResetState()
        {
            _flashT = 0f;
            _knockT = 0f;
            _deathT = -1f;
            _rolling = false;
            ApplyFlash(0f);
            ApplyTuck(false);
            transform.localRotation = Quaternion.identity;
        }

        void ApplyTuck(bool rolling)
        {
            if (_model == null) return;
            if (!_cachedModelScale)
            {
                _modelBaseScale = _model.localScale;
                _cachedModelScale = true;
            }
            _model.localScale = rolling
                ? new Vector3(_modelBaseScale.x * 1.15f, _modelBaseScale.y * 0.62f, _modelBaseScale.z * 1.15f)
                : _modelBaseScale;
        }

        void ApplyFlash(float amount)
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                var mats = r.sharedMaterials;
                var baseCols = _baseColors[i];
                for (int m = 0; m < mats.Length && m < baseCols.Length; m++)
                {
                    var c = Color.Lerp(baseCols[m], Color.white, amount);
                    if (mats[m].HasProperty("_Color")) mats[m].SetColor("_Color", c);
                    mats[m].color = c;
                }
            }
        }

        void ApplyDeathFade(float alpha)
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                var mats = r.sharedMaterials;
                var baseCols = _baseColors[i];
                for (int m = 0; m < mats.Length && m < baseCols.Length; m++)
                {
                    var c = baseCols[m];
                    c *= 0.55f + 0.45f * alpha;
                    c.a = alpha;
                    if (mats[m].HasProperty("_Color")) mats[m].SetColor("_Color", c);
                    mats[m].color = c;
                }
            }
        }
    }
}

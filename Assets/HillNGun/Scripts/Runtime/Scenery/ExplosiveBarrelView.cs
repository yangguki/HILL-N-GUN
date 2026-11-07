using UnityEngine;
using HillNGun.Core;
using HillNGun.Combat;
using HillNGun.Battlefield;
using HillNGun.Feel;

namespace HillNGun.Scenery
{
    /// <summary>
    /// 12 §6.1 danger barrel view: red barrel + yellow hazard glyph (distinct from the brown cover barrel),
    /// damage tint, and on arm a 0.4 s fast red blink with the blast radius ring closing in. View only.
    /// </summary>
    public class ExplosiveBarrelView : MonoBehaviour
    {
        private HazardExplosiveBarrel _barrel;
        private SpriteRenderer _sr;
        private LineRenderer _ring;
        private float _blink;
        private static Sprite _spr;
        private static Material _mat;
        private const int RingPts = 32;

        public void Bind(HazardExplosiveBarrel barrel)
        {
            _barrel = barrel;
            FieldView.Place(transform, barrel.Position, ScalePresets.VisualMul); // prop drawn with the actors (view only)
            _sr = gameObject.GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = BarrelSprite();
            _sr.sortingOrder = FieldView.DepthOrder(barrel.Position.y) + 2;
            _barrel.OnExploded += HandleExploded;
        }

        private void OnDestroy()
        {
            if (_barrel != null) _barrel.OnExploded -= HandleExploded;
        }

        private void Update()
        {
            if (_barrel == null || _barrel.Exploded) return;
            if (_barrel.Armed)
            {
                _blink += Time.deltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(_blink * Mathf.PI * 2f / 0.1f);
                _sr.color = Color.Lerp(Color.white, new Color(1f, 0.15f, 0.1f), pulse);
                float k = _barrel.WarningTotal > 0f ? Mathf.Clamp01(_barrel.WarningRemaining / _barrel.WarningTotal) : 0f;
                DrawRing(Mathf.Lerp(0.35f, 1f, 1f - k), pulse);
            }
            else
            {
                float t = Mathf.Clamp01(_barrel.Accumulated / (float)HazardExplosiveBarrel.TriggerDamage);
                _sr.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.45f), t);
                if (_ring != null) _ring.enabled = TerrainRules.CoverDebug;
                if (TerrainRules.CoverDebug) DrawRing(1f, 0f);
            }
        }

        private void DrawRing(float frac, float pulse)
        {
            if (_ring == null)
            {
                if (_mat == null) _mat = new Material(Shader.Find("Sprites/Default"));
                var go = new GameObject("blast_ring"); go.transform.SetParent(transform, false);
                _ring = go.AddComponent<LineRenderer>(); _ring.sharedMaterial = _mat; _ring.useWorldSpace = true;
                _ring.loop = true; _ring.positionCount = RingPts; _ring.sortingOrder = FieldView.OverlayOrder - 30;
            }
            _ring.enabled = true;
            _ring.startWidth = _ring.endWidth = 0.06f + 0.05f * pulse;
            _ring.startColor = _ring.endColor = new Color(1f, 0.2f, 0.1f, 0.6f + 0.4f * pulse);
            float r = TerrainRules.Data.barrelRadius * frac;
            for (int i = 0; i < RingPts; i++)
            {
                float a = i / (float)RingPts * Mathf.PI * 2f;
                _ring.SetPosition(i, FieldView.ToWorld(_barrel.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r));
            }
        }

        private void HandleExploded(HazardExplosiveBarrel b)
        {
            // explosion / debris VFX are spawned once by HazardExplosiveBarrel.Explode
            EffectsManager.Instance?.SpawnImpact(b.Position, false);
            if (_sr != null) _sr.enabled = false;
            if (_ring != null) _ring.enabled = false;
            Destroy(gameObject, 0.05f);
        }

        /// <summary>40 px / 40 ppu ≈ 0.8–1 unit: red-painted barrel, dark hoops, yellow warning triangle with "!".</summary>
        private static Sprite BarrelSprite()
        {
            if (_spr != null) return _spr;
            const int s = 40;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var outline = new Color(0.12f, 0.05f, 0.03f, 1f);
            var red = new Color(0.78f, 0.16f, 0.10f, 1f);
            var redHi = new Color(0.93f, 0.33f, 0.22f, 1f);
            var hoop = new Color(0.25f, 0.22f, 0.2f, 1f);
            var yellow = new Color(1f, 0.84f, 0.12f, 1f);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float nx = (x - 19.5f) / 14f; // barrel body: rounded rect bulging in the middle
                float bulge = 1f - 0.12f * Mathf.Pow((y - 19.5f) / 18f, 2f) * 4f;
                bool inside = Mathf.Abs(nx) < bulge && y >= 2 && y <= 37;
                bool edge = inside && (Mathf.Abs(nx) > bulge - 0.12f || y <= 3 || y >= 36);
                Color c = Color.clear;
                if (inside)
                {
                    c = nx < -0.35f ? redHi : red;
                    if ((y >= 8 && y <= 10) || (y >= 29 && y <= 31)) c = hoop;
                    if (edge) c = outline;
                }
                tex.SetPixel(x, y, c);
            }
            // warning triangle (apex up) centred on the body
            for (int y = 12; y <= 27; y++)
            {
                float half = (27 - y) * 0.55f;
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Abs(x - 19.5f);
                    if (dx <= half + 0.5f) tex.SetPixel(x, y, dx > half - 1f || y == 12 ? outline : yellow);
                }
            }
            for (int y = 17; y <= 23; y++) { tex.SetPixel(19, y, outline); tex.SetPixel(20, y, outline); }
            tex.SetPixel(19, 14, outline); tex.SetPixel(20, 14, outline); tex.SetPixel(19, 15, outline); tex.SetPixel(20, 15, outline);
            tex.Apply();
            _spr = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.15f), 40f);
            return _spr;
        }
    }
}

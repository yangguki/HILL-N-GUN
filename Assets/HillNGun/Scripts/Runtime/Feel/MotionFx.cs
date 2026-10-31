using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Feel
{
    /// <summary>
    /// View-only pooled motion effects: foot dust puffs and roll afterimages, plus the shared
    /// procedural soft-shadow / dust sprites (generated at runtime, no external art).
    /// One instance per scene, created on demand.
    /// </summary>
    public class MotionFx : MonoBehaviour
    {
        private static MotionFx _instance;
        private static Sprite _shadowSprite;
        private static Sprite _dustSprite;

        private struct Fx
        {
            public SpriteRenderer sr;
            public float age, life, startAlpha, startScale, endScale;
            public Vector3 drift;
            public Color color;
        }

        private readonly List<Fx> _active = new List<Fx>();
        private readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>();

        public static MotionViewData Data
        {
            get
            {
                var d = ServiceLocator.Get<MotionViewData>();
                if (d == null && RuntimeAssetManager.Instance != null) d = RuntimeAssetManager.Instance.MotionView;
                return d != null ? d : MotionViewData.Fallback;
            }
        }

        public static MotionFx Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[MotionFx]");
                    _instance = go.AddComponent<MotionFx>();
                }
                return _instance;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private static readonly Dictionary<Sprite, Vector2> _measureCache = new Dictionary<Sprite, Vector2>();

        /// <summary>
        /// Opaque-area foot line (local Y, sprite space) and opaque width, measured once per sprite.
        /// Falls back to the given defaults when the texture is not readable.
        /// </summary>
        public static void MeasureFeet(Sprite s, float fallbackFoot, float fallbackWidth, out float footY, out float width)
        {
            footY = fallbackFoot; width = fallbackWidth;
            if (s == null || !Data.autoFootFromSprite) return;
            if (_measureCache.TryGetValue(s, out var cached)) { footY = cached.x; width = cached.y; return; }
            try
            {
                var tex = s.texture;
                if (tex == null || !tex.isReadable) return;
                Rect r = s.textureRect;
                int x0 = Mathf.FloorToInt(r.x), y0 = Mathf.FloorToInt(r.y), w = Mathf.FloorToInt(r.width), h = Mathf.FloorToInt(r.height);
                var px = tex.GetPixels(x0, y0, w, h);
                int minRow = -1, maxRow = -1, minX = w, maxX = -1;
                for (int y = 0; y < h; y++)
                {
                    int cnt = 0;
                    for (int x = 0; x < w; x++)
                    {
                        if (px[y * w + x].a > 0.35f)
                        {
                            cnt++;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                        }
                    }
                    if (cnt > 2) { if (minRow < 0) minRow = y; maxRow = y; }
                }
                if (minRow < 0 || maxX < minX) return;
                float ppu = s.pixelsPerUnit;
                float opaqueH = maxRow - minRow;
                float pivotY = s.pivot.y; // pixels from rect bottom
                footY = (minRow + opaqueH * Data.footInset - pivotY) / ppu;
                width = (maxX - minX) / ppu;
                _measureCache[s] = new Vector2(footY, width);
            }
            catch (System.Exception) { }
        }

        /// <summary>Soft elliptical drop shadow (radial falloff), white so it can be tinted.</summary>
        public static Sprite ShadowSprite
        {
            get
            {
                if (_shadowSprite != null) return _shadowSprite;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - d);
                        a = a * a * (3f - 2f * a); // smooth falloff, darker core
                        px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a * 1.35f));
                    }
                tex.SetPixels(px);
                tex.Apply();
                _shadowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                _shadowSprite.name = "soft_shadow";
                return _shadowSprite;
            }
        }

        /// <summary>Fluffy dust puff (a few overlapping soft blobs).</summary>
        public static Sprite DustSprite
        {
            get
            {
                if (_dustSprite != null) return _dustSprite;
                const int n = 32;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var px = new Color[n * n];
                Vector2[] c = { new Vector2(0.5f, 0.45f), new Vector2(0.33f, 0.55f), new Vector2(0.66f, 0.56f), new Vector2(0.5f, 0.66f) };
                float[] r = { 0.32f, 0.22f, 0.22f, 0.2f };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        Vector2 p = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);
                        float a = 0f;
                        for (int i = 0; i < c.Length; i++)
                        {
                            float d = Vector2.Distance(p, c[i]) / r[i];
                            a = Mathf.Max(a, Mathf.Clamp01(1f - d * d));
                        }
                        float shade = Mathf.Lerp(0.85f, 1f, p.y);
                        px[y * n + x] = new Color(shade, shade, shade, a);
                    }
                tex.SetPixels(px);
                tex.Apply();
                _dustSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                _dustSprite.name = "dust_puff";
                return _dustSprite;
            }
        }

        private SpriteRenderer Get()
        {
            SpriteRenderer sr;
            if (_pool.Count > 0) { sr = _pool.Pop(); sr.gameObject.SetActive(true); }
            else
            {
                var go = new GameObject("fx");
                go.transform.SetParent(transform, false);
                sr = go.AddComponent<SpriteRenderer>();
            }
            sr.flipX = sr.flipY = false;
            return sr;
        }

        /// <summary>Dust puffs at a foot position. dir = rough push direction in world space.</summary>
        public void Dust(Vector3 worldPos, Vector2 dir, int count, float scale, int sortingOrder)
        {
            var d = Data;
            if (count <= 0 || d.dustLifetime <= 0f) return;
            for (int i = 0; i < count; i++)
            {
                var sr = Get();
                sr.sprite = DustSprite;
                sr.sortingOrder = sortingOrder;
                Vector3 jitter = new Vector3(Random.Range(-0.18f, 0.18f), Random.Range(-0.04f, 0.06f), 0f) * scale;
                sr.transform.position = worldPos + jitter;
                sr.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                float s = d.dustSize * scale * Random.Range(0.7f, 1.15f);
                Vector3 drift = new Vector3(dir.x * Random.Range(0.4f, 1.0f) + Random.Range(-0.25f, 0.25f), Random.Range(0.15f, 0.45f), 0f) * scale;
                _active.Add(new Fx { sr = sr, age = 0f, life = d.dustLifetime * Random.Range(0.8f, 1.2f), startAlpha = d.dustColor.a,
                    startScale = s * 0.6f, endScale = s * 1.6f, drift = drift, color = d.dustColor });
                sr.color = d.dustColor;
                sr.transform.localScale = new Vector3(s * 0.6f, s * 0.6f, 1f);
            }
        }

        /// <summary>Fading copy of a sprite renderer's current pose (roll afterimage).</summary>
        public void Afterimage(SpriteRenderer src, Color tint, float alpha, float life)
        {
            if (src == null || src.sprite == null || life <= 0f) return;
            var sr = Get();
            sr.sprite = src.sprite;
            sr.sortingOrder = src.sortingOrder - 1;
            var t = src.transform;
            sr.transform.position = t.position;
            sr.transform.rotation = t.rotation;
            sr.transform.localScale = t.lossyScale;
            var c = new Color(tint.r, tint.g, tint.b, alpha);
            sr.color = c;
            float s = t.lossyScale.y;
            _active.Add(new Fx { sr = sr, age = 0f, life = life, startAlpha = alpha, startScale = -1f, endScale = -1f, drift = Vector3.zero, color = c });
        }

        private void Update()
        {
            float dt = GameTime.DeltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var f = _active[i];
                f.age += dt;
                if (f.sr == null) { _active.RemoveAt(i); continue; }
                if (f.age >= f.life)
                {
                    f.sr.gameObject.SetActive(false);
                    _pool.Push(f.sr);
                    _active.RemoveAt(i);
                    continue;
                }
                float k = f.age / f.life;
                var c = f.color;
                c.a = f.startAlpha * (1f - k) * (1f - k);
                f.sr.color = c;
                if (f.startScale > 0f)
                {
                    float s = Mathf.Lerp(f.startScale, f.endScale, 1f - (1f - k) * (1f - k));
                    f.sr.transform.localScale = new Vector3(s, s * 0.8f, 1f);
                    f.sr.transform.position += f.drift * dt * (1f - k);
                }
                _active[i] = f;
            }
        }
    }

    /// <summary>
    /// View-only body rig for an actor: soft shadow + pivot (at the feet) + body sprite.
    /// Drives walk bounce, lean, squash/stretch spring, smooth left/right flip, hop and spin.
    /// Callers feed logic velocity/state; nothing here feeds back into gameplay.
    /// </summary>
    public sealed class MotionRig
    {
        public readonly Transform Root;
        public readonly Transform Pivot;
        public readonly Transform Body;
        public readonly SpriteRenderer Renderer;
        public readonly SpriteRenderer Shadow;

        private readonly float _foot;
        private readonly Vector2 _shadowSize;
        private readonly bool _artFacesRight;
        private float _face, _flip, _lean, _phase, _sq, _sqVel;
        private int _prevSign;
        private bool _moving;
        private float _shadowAlpha;

        public float FacingSign => _face;
        public int SortingOrder { get; set; }
        /// <summary>Extra multiplier for dust size (perspective / actor size).</summary>
        public float FxScale { get; set; } = 1f;

        public MotionRig(Transform root, float footOffset, Vector2 shadowSize, bool artFacesRight)
        {
            Root = root;
            _foot = footOffset;
            _shadowSize = shadowSize;
            _artFacesRight = artFacesRight;
            _face = _flip = artFacesRight ? 1f : -1f; // start in the art's native facing

            var d = MotionFx.Data;
            var shadowGo = new GameObject("SoftShadow");
            shadowGo.transform.SetParent(root, false);
            shadowGo.transform.localPosition = new Vector3(0f, footOffset, 0f);
            shadowGo.transform.localScale = new Vector3(shadowSize.x, shadowSize.y, 1f);
            Shadow = shadowGo.AddComponent<SpriteRenderer>();
            Shadow.sprite = MotionFx.ShadowSprite;
            // one shadow tone per stage theme for characters, props and decor
            var sc = HillNGun.Scenery.ThemeContext.GroundShadowOr(d.shadowColor);
            Shadow.color = sc;
            _shadowAlpha = sc.a;

            Pivot = new GameObject("BodyPivot").transform;
            Pivot.SetParent(root, false);
            Pivot.localPosition = new Vector3(0f, footOffset, 0f);
            Body = new GameObject("Body").transform;
            Body.SetParent(Pivot, false);
            Body.localPosition = new Vector3(0f, -footOffset, 0f);
            Renderer = Body.gameObject.AddComponent<SpriteRenderer>();
        }

        public void SetFacing(float sign)
        {
            if (Mathf.Abs(sign) > 0.01f) _face = Mathf.Sign(sign);
        }

        /// <summary>+ stretches, - squashes (spring returns to 1).</summary>
        public void Impulse(float amount) => _sq += amount;

        public Vector3 FootWorld => Root.TransformPoint(new Vector3(0f, _foot, 0f));

        public void Dust(Vector2 dir, int count)
        {
            MotionFx.Instance.Dust(FootWorld, dir, count, Root.lossyScale.y * FxScale, SortingOrder - 1);
        }

        public void Tick(float dt, float speedX, float maxSpeed, bool grounded, float bobHeight, float leanMax,
                         float hop, float spinDeg, bool walkFx)
        {
            var d = MotionFx.Data;
            float m01 = maxSpeed > 0.01f ? Mathf.Clamp01(Mathf.Abs(speedX) / maxSpeed) : 0f;

            // smooth flip (paper-turn through a thin sliver instead of a hard mirror)
            float flipRate = d.flipTime > 0f ? 2f / d.flipTime : 1000f;
            _flip = Mathf.MoveTowards(_flip, _face, flipRate * Mathf.Max(dt, 0.0001f));
            float minW = Mathf.Max(0.05f, d.flipMinWidth);
            float f = Mathf.Abs(_flip) < minW ? minW * (Mathf.Abs(_flip) < 0.001f ? _face : Mathf.Sign(_flip)) : _flip;
            float sx = f * (_artFacesRight ? 1f : -1f);

            // walk bounce + steps
            float bob = 0f, stretch = 0f;
            if (grounded && m01 > 0.05f)
            {
                float prev = _phase;
                _phase += Mathf.Abs(speedX) * d.stepsPerTile * Mathf.PI * dt;
                if (walkFx && d.dustOnSteps && Mathf.Floor(prev / Mathf.PI) != Mathf.Floor(_phase / Mathf.PI))
                    Dust(new Vector2(-Mathf.Sign(speedX), 0f), 1);
                float s = Mathf.Abs(Mathf.Sin(_phase));
                bob = s * bobHeight * m01;
                stretch = (s - 0.5f) * 2f * d.walkStretch * m01;
            }
            else
            {
                // settle the step phase to the nearest contact so the next start begins on a footfall
                _phase = Mathf.Round(_phase / Mathf.PI) * Mathf.PI;
            }

            // start / stop / turn accents
            bool movingNow = Mathf.Abs(speedX) > 0.35f;
            if (grounded && walkFx)
            {
                if (movingNow && !_moving) { _sq += d.startStretch; Dust(new Vector2(-Mathf.Sign(speedX), 0f), d.dustBurstCount); }
                else if (!movingNow && _moving) { _sq -= d.stopSquash; Dust(Vector2.zero, Mathf.Max(1, d.dustBurstCount - 1)); }
                int sign = speedX > 0.8f ? 1 : (speedX < -0.8f ? -1 : 0);
                if (sign != 0 && _prevSign != 0 && sign != _prevSign) { _sq -= d.stopSquash * 0.6f; Dust(new Vector2(-sign, 0f), d.dustBurstCount); }
                if (sign != 0) _prevSign = sign;
            }
            _moving = movingNow;

            // squash spring
            float h = Mathf.Min(dt, 1f / 30f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / h));
            float sdt = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float acc = -d.springStiffness * _sq - d.springDamping * _sqVel;
                _sqVel += acc * sdt;
                _sq += _sqVel * sdt;
            }
            _sq = Mathf.Clamp(_sq, -0.4f, 0.4f);

            // lean toward travel
            float leanTarget = grounded ? Mathf.Clamp(-(speedX / Mathf.Max(0.01f, maxSpeed)) * leanMax, -leanMax, leanMax) : 0f;
            _lean = Mathf.Lerp(_lean, leanTarget, 1f - Mathf.Exp(-d.leanSharpness * dt));

            float sq = _sq + stretch;
            Pivot.localScale = new Vector3(sx * (1f - sq * 0.6f), 1f + sq, 1f);
            Pivot.localRotation = Quaternion.Euler(0f, 0f, _lean);
            Body.localPosition = new Vector3(0f, -_foot + bob + hop, 0f);
            // spinDeg = forward tumble in the facing direction; compensate for the mirrored pivot
            Body.localRotation = Quaternion.Euler(0f, 0f, -_face * spinDeg * Mathf.Sign(sx));

            float lift = bob + hop;
            float k = Mathf.Clamp(1f - d.shadowShrinkPerUnit * lift, 0.5f, 1f);
            Shadow.transform.localScale = new Vector3(_shadowSize.x * k, _shadowSize.y * k, 1f);
            var c = HillNGun.Scenery.ThemeContext.GroundShadowOr(d.shadowColor); c.a = c.a * Mathf.Lerp(0.6f, 1f, k);
            Shadow.color = c;
            Shadow.sortingOrder = HillNGun.Battlefield.FieldView.GroundDecalOrder - 1;
        }

        /// <summary>Neutral pose (downed / eliminated).</summary>
        public void ResetPose()
        {
            _sq = _sqVel = _lean = 0f;
            _moving = false;
            float sx = _face * (_artFacesRight ? 1f : -1f);
            _flip = _face;
            Pivot.localScale = new Vector3(sx, 1f, 1f);
            Pivot.localRotation = Quaternion.identity;
            Body.localPosition = new Vector3(0f, -_foot, 0f);
            Body.localRotation = Quaternion.identity;
            Shadow.transform.localScale = new Vector3(_shadowSize.x, _shadowSize.y, 1f);
            Shadow.sortingOrder = HillNGun.Battlefield.FieldView.GroundDecalOrder - 1;
        }
    }
}

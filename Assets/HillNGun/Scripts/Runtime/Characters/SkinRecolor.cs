using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Characters
{
    /// <summary>
    /// Placeholder skins until real Spine skins exist: recolors a sprite toward the skin color while keeping shading
    /// (dark outlines stay dark). A plain SpriteRenderer.color multiply was too weak to see a gold skin on warm sprites.
    /// Results are cached per (sprite, color, strength). Source textures are imported readable by ArtGenerator.
    /// </summary>
    public static class SkinRecolor
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        private static float Ramp(float x, float a, float b) => x <= a ? 0f : x >= b ? 1f : (x - a) / (b - a);

        public static Sprite Get(Sprite src, Color tint, float strength)
        {
            if (src == null || strength <= 0.001f) return src;
            string key = $"{src.GetInstanceID()}_{ColorUtility.ToHtmlStringRGB(tint)}_{strength:F2}";
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var tex = src.texture;
            if (tex == null || !tex.isReadable) return src;
            Rect r = src.textureRect;
            int x = Mathf.FloorToInt(r.x), y = Mathf.FloorToInt(r.y), w = Mathf.FloorToInt(r.width), h = Mathf.FloorToInt(r.height);
            Color[] px;
            try { px = tex.GetPixels(x, y, w, h); }
            catch { return src; }

            Color.RGBToHSV(tint, out float th, out float ts, out float tv);
            float lift = tv * 0.95f;
            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i];
                if (c.a <= 0.01f) continue;
                Color.RGBToHSV(c, out float ph, out float s, out float v);
                // keep faces / hands: soft skin-tone mask (hue ~ orange, mid saturation, bright)
                float hd = Mathf.Min(Mathf.Abs(ph - 0.06f), 1f - Mathf.Abs(ph - 0.06f));
                float skin = (1f - Ramp(hd, 0.04f, 0.08f)) * Ramp(s, 0.12f, 0.22f) * (1f - Ramp(s, 0.6f, 0.72f)) * Ramp(v, 0.42f, 0.6f);
                float st = strength * (1f - 0.85f * skin);
                // hue -> skin hue, saturation toward the tint, value lifted for bright tints (gold reads as gold, not olive)
                float nv = Mathf.Min(1f, (1f - lift) * v + lift * (0.3f + 0.75f * Mathf.Pow(v, 0.5f)));
                float ns = s + (ts - s) * 0.9f;
                float nh = Mathf.Repeat(th - (1f - nv) * 0.07f * ts, 1f); // warmer shadows
                Color target = Color.HSVToRGB(nh, ns, nv);
                target.a = c.a;
                px[i] = Color.Lerp(c, target, st);
            }

            bool mips = tex.mipmapCount > 1;
            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, mips) { filterMode = tex.filterMode, wrapMode = TextureWrapMode.Clamp, name = src.name + "_skin" };
            outTex.SetPixels(px);
            outTex.Apply(mips, false);
            Vector2 pivot = new Vector2(src.pivot.x / r.width, src.pivot.y / r.height);
            var sprite = Sprite.Create(outTex, new Rect(0, 0, w, h), pivot, src.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = outTex.name;
            _cache[key] = sprite;
            return sprite;
        }
    }
}

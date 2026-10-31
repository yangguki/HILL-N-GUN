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

            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i];
                if (c.a <= 0.01f) continue;
                float lum = c.r * 0.30f + c.g * 0.59f + c.b * 0.11f;
                // brightness-preserving remap: shadows stay dark, highlights get a bright tint
                float k = Mathf.Clamp01(0.25f + lum * 1.15f);
                Color target = new Color(Mathf.Clamp01(tint.r * k + lum * 0.15f), Mathf.Clamp01(tint.g * k + lum * 0.15f), Mathf.Clamp01(tint.b * k + lum * 0.15f), c.a);
                px[i] = Color.Lerp(c, target, strength);
            }

            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = tex.filterMode, wrapMode = TextureWrapMode.Clamp, name = src.name + "_skin" };
            outTex.SetPixels(px);
            outTex.Apply(false, false);
            Vector2 pivot = new Vector2(src.pivot.x / r.width, src.pivot.y / r.height);
            var sprite = Sprite.Create(outTex, new Rect(0, 0, w, h), pivot, src.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = outTex.name;
            _cache[key] = sprite;
            return sprite;
        }
    }
}

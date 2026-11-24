using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Runtime 2x sky band #3c2a58 → #4a3466 (rows 0–268).</summary>
    public static class Proto_GreyboxPersp2DSkyGradient
    {
        static Sprite _cached;

        public static Sprite GetOrCreate()
        {
            if (_cached != null) return _cached;
            const int w = 384;
            int h = Mathf.Max(4, Mathf.RoundToInt(w * Proto_GreyboxPersp2DStageLayout.SkyGradientBottomY2x
                / Proto_GreyboxPersp2DStageLayout.RefW));
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var top = new Color32(0x3c, 0x2a, 0x58, 0xff);
            var bottom = new Color32(0x4a, 0x34, 0x66, 0xff);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                var c = Color.Lerp(top, bottom, t);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            }
            tex.Apply();
            _cached = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 1f),
                Proto_GreyboxWestPixelCatalog.ImportPpu);
            _cached.name = "P2D_SkyGradient_Runtime";
            return _cached;
        }

        /// <summary>Ref-space width/height after uniform scale to 3840 ref px wide (EditMode).</summary>
        public static void GetRefBoundsAfterScale(out float refWidth, out float refHeight)
        {
            var sky = GetOrCreate();
            float ppu = Proto_GreyboxWestPixelCatalog.ImportPpu;
            float uni = Proto_GreyboxPersp2DStageLayout.UniformScaleNative(sky, ppu, Proto_GreyboxPersp2DStageLayout.RefW);
            float wuPerRef = Proto_GreyboxPersp2DStageLayout.WorldUnitsPerRefPx;
            float worldW = sky.bounds.size.x * uni;
            float worldH = sky.bounds.size.y * uni;
            refWidth = worldW / wuPerRef;
            refHeight = worldH / wuPerRef;
        }
    }
}
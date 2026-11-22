using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxSpriteUtil
    {
        static Texture2D s_whiteTex;
        static Sprite s_whiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (s_whiteSprite != null) return s_whiteSprite;
                s_whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                s_whiteTex.SetPixel(0, 0, Color.white);
                s_whiteTex.Apply();
                s_whiteTex.hideFlags = HideFlags.HideAndDontSave;
                s_whiteSprite = Sprite.Create(
                    s_whiteTex,
                    new Rect(0, 0, 1, 1),
                    new Vector2(0.5f, 0f),
                    1f);
                s_whiteSprite.hideFlags = HideFlags.HideAndDontSave;
                return s_whiteSprite;
            }
        }

        public static Sprite GetCircleSprite(bool ring, int size = 128)
        {
            var key = ring ? 1 : 0;
            if (ring)
            {
                if (s_ringSprite != null) return s_ringSprite;
            }
            else if (s_circleSprite != null)
                return s_circleSprite;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            float r = size * 0.5f;
            float inner = ring ? r * 0.68f : 0f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = Color.clear;
                    if (ring)
                    {
                        if (d <= r && d >= inner)
                        {
                            float edge = Mathf.Clamp01((r - d) / Mathf.Max(r - inner, 1f));
                            float a = Mathf.Lerp(0.18f, 0.36f, edge);
                            c = new Color(1f, 1f, 1f, a);
                        }
                        else if (d < inner)
                        {
                            float fill = Mathf.Clamp01((inner - d) / (inner * 0.55f));
                            c = new Color(1f, 1f, 1f, fill * 0.12f);
                        }
                    }
                    else if (d <= r)
                        c = Color.white;

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            if (ring)
                s_ringSprite = sprite;
            else
                s_circleSprite = sprite;
            return sprite;
        }

        static Sprite s_circleSprite;
        static Sprite s_ringSprite;
        static Sprite s_heartSprite;

        public static Sprite GetHeartSprite(int size = 64)
        {
            if (s_heartSprite != null) return s_heartSprite;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            float cx = size * 0.5f;
            float cy = size * 0.38f;
            float scale = size * 0.22f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - cx) / scale;
                    float ny = (cy - y) / scale;
                    float a = nx * nx + ny * ny - 1f;
                    float heart = a * a * a - nx * nx * ny * ny * ny;
                    tex.SetPixel(x, y, heart <= 0f ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            s_heartSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.35f), size);
            s_heartSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_heartSprite;
        }

        public static Vector2 NativeWorldSize(Sprite sprite)
        {
            if (sprite == null) return Vector2.one;
            return new Vector2(sprite.rect.width / sprite.pixelsPerUnit, sprite.rect.height / sprite.pixelsPerUnit);
        }

        public static Vector3 ScaleForWorldSize(Sprite sprite, float worldWidth, float worldHeight)
        {
            var native = NativeWorldSize(sprite);
            return new Vector3(worldWidth / Mathf.Max(native.x, 1e-6f), worldHeight / Mathf.Max(native.y, 1e-6f), 1f);
        }

        /// <summary>화면 높이를 맞추고 스프라이트 종횡비를 유지(히트 폭과 독립).</summary>
        public static Vector3 ScaleForWorldHeightUniform(Sprite sprite, float worldHeight)
        {
            var native = NativeWorldSize(sprite);
            float sy = worldHeight / Mathf.Max(native.y, 1e-6f);
            return new Vector3(sy, sy, 1f);
        }

        public static GameObject CreateQuad(string name, Transform parent, Color color, float footX, float footY,
            float widthWorld, float heightWorld, int sortingOrder, string sortingLayerName = "Default")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(footX, footY, 0f);
            go.transform.localScale = new Vector3(widthWorld, heightWorld, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite;
            sr.color = color;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        public static GameObject AttachFootShadow(Transform parent, int sortingOrder, float widthWorld = 0.72f,
            float heightWorld = 0.14f)
        {
            var go = new GameObject(parent.name + "_Shadow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(widthWorld, heightWorld, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetFootShadowSprite();
            sr.color = Color.white;
            sr.sortingOrder = sortingOrder - 1;
            return go;
        }

        static Sprite FootPivotSprite(Texture2D tex, int pixelsPerUnit)
        {
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.Apply();
            int h = tex.height;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, h), new Vector2(0.5f, 0f), pixelsPerUnit);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        public static Sprite GetFootShadowSprite()
        {
            if (s_footShadow != null) return s_footShadow;
            const int w = 64;
            const int h = 24;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = w * 0.5f;
            float cy = h * 0.35f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x - cx) / (w * 0.42f);
                float ny = (y - cy) / (h * 0.55f);
                float d = nx * nx + ny * ny;
                float a = d <= 1f ? Mathf.Clamp01(1f - d) * Proto_GreyboxPalette.FootShadow.a : 0f;
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }

            s_footShadow = FootPivotSprite(tex, h);
            return s_footShadow;
        }

        static Sprite s_footShadow;

        public static Sprite GetVerticalGradientSprite(Color top, Color bottom, int w = 128, int h = 128)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                var c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            }

            var cloud = new Color(1f, 1f, 1f, 0.09f);
            StampEllipse(tex, w * 0.22f, h * 0.72f, w * 0.14f, h * 0.06f, cloud);
            StampEllipse(tex, w * 0.38f, h * 0.78f, w * 0.11f, h * 0.05f, cloud);
            StampEllipse(tex, w * 0.58f, h * 0.7f, w * 0.16f, h * 0.055f, cloud);
            StampEllipse(tex, w * 0.78f, h * 0.76f, w * 0.12f, h * 0.048f, cloud);

            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), w);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            tex.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        public static Sprite GetGroundFieldSprite(int w = 256, int h = 128)
        {
            if (s_groundField != null) return s_groundField;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                var baseC = Color.Lerp(Proto_GreyboxPalette.GroundFar, Proto_GreyboxPalette.GroundNear, t);
                float band = Mathf.Sin(y * 0.28f) * 0.012f;
                baseC = new Color(
                    Mathf.Clamp01(baseC.r + band),
                    Mathf.Clamp01(baseC.g + band * 0.85f),
                    Mathf.Clamp01(baseC.b + band * 0.7f),
                    1f);
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float streak = Mathf.Sin(nx * 9f + y * 0.04f) * 0.003f;
                    var c = new Color(
                        Mathf.Clamp01(baseC.r + streak),
                        Mathf.Clamp01(baseC.g + streak),
                        Mathf.Clamp01(baseC.b + streak),
                        1f);
                    tex.SetPixel(x, y, c);
                }
            }

            int lineStep = Mathf.Max(22, h / 4);
            var lineC = new Color(0f, 0f, 0f, 0.018f);
            for (int y = lineStep / 2; y < h; y += lineStep)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.Lerp(tex.GetPixel(x, y), lineC, 0.055f));

            s_groundField = FootPivotSprite(tex, w);
            return s_groundField;
        }

        static Sprite s_groundField;

        public static Sprite GetFenceSprite(int w = 1024, int h = 64)
        {
            if (s_fenceSprite != null) return s_fenceSprite;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.clear);

            int railH = Mathf.Max(4, h / 8);
            int topRail = h - railH - Mathf.Max(2, h / 16);
            int midRail = h / 2 - railH / 2;
            int botRail = Mathf.RoundToInt(h * 0.18f);
            var rail = Proto_GreyboxPalette.FenceWood;
            var railHi = Color.Lerp(rail, Color.white, 0.18f);
            FillRect(tex, 0, topRail, w, topRail + railH, railHi);
            FillRect(tex, 0, midRail, w, midRail + railH, rail);
            FillRect(tex, 0, botRail, w, botRail + railH, rail);

            int postW = Mathf.Max(5, w / 56);
            int gap = Mathf.Max(postW * 4, w / 10);
            for (int x = 0; x < w; x += gap)
            {
                FillRect(tex, x, 0, x + postW, h, Proto_GreyboxPalette.FencePost);
                var shade = Color.Lerp(Proto_GreyboxPalette.FencePost, Color.black, 0.15f);
                FillRect(tex, x, 0, x + 1, h, shade);
            }

            s_fenceSprite = FootPivotSprite(tex, h);
            return s_fenceSprite;
        }

        static Sprite s_fenceSprite;

        static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color c, int thickness)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                FillRect(tex, x - thickness, y - thickness, x + thickness + 1, y + thickness + 1, c);
            }
        }

        static void FillRect(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
        {
            x0 = Mathf.Clamp(x0, 0, tex.width - 1);
            x1 = Mathf.Clamp(x1, 0, tex.width);
            y0 = Mathf.Clamp(y0, 0, tex.height - 1);
            y1 = Mathf.Clamp(y1, 0, tex.height);
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                tex.SetPixel(x, y, c);
        }

        static void StampEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - rx);
            int x1 = Mathf.CeilToInt(cx + rx);
            int y0 = Mathf.FloorToInt(cy - ry);
            int y1 = Mathf.CeilToInt(cy + ry);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                float nx = (x - cx) / rx;
                float ny = (y - cy) / ry;
                if (nx * nx + ny * ny <= 1f)
                    tex.SetPixel(x, y, c);
            }
        }

        public static Sprite GetPlayerBackSprite(int w = 176, int h = 256)
        {
            if (s_playerBack != null) return s_playerBack;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.clear);

            int cx = w / 2;
            var boot = Color.Lerp(Proto_GreyboxPalette.PlayerCoat, Color.black, 0.35f);
            FillRect(tex, cx - 34, 0, cx - 6, 22, boot);
            FillRect(tex, cx + 6, 0, cx + 34, 22, boot);
            FillRect(tex, cx - 22, 18, cx - 8, 88, Proto_GreyboxPalette.PlayerCoat);
            FillRect(tex, cx + 8, 18, cx + 22, 88, Proto_GreyboxPalette.PlayerCoat);
            FillRect(tex, cx - 48, 82, cx + 48, 158, Proto_GreyboxPalette.PlayerCoat);
            FillRect(tex, cx - 58, 138, cx + 58, 168, Color.Lerp(Proto_GreyboxPalette.PlayerCoat, Color.white, 0.06f));
            FillRect(tex, cx - 18, 162, cx + 18, 196, new Color(194f / 255f, 166f / 255f, 148f / 255f));
            FillRect(tex, cx - 14, 192, cx + 14, 210, Proto_GreyboxPalette.PlayerHat);
            FillRect(tex, cx - 72, 206, cx + 72, 220, Proto_GreyboxPalette.PlayerHat);
            FillRect(tex, cx - 26, 210, cx + 26, 248, Proto_GreyboxPalette.PlayerHat);
            FillRect(tex, cx + 28, 128, cx + 40, 162, Color.Lerp(Proto_GreyboxPalette.PlayerCoat, Color.black, 0.1f));
            FillRect(tex, cx + 38, 148, cx + 96, 158, Color.Lerp(Proto_GreyboxPalette.PlayerHat, Color.black, 0.2f));
            FillRect(tex, cx + 90, 144, cx + 106, 162, new Color(48f / 255f, 46f / 255f, 52f / 255f));

            s_playerBack = FootPivotSprite(tex, h);
            return s_playerBack;
        }

        static Sprite s_playerBack;

        public static Sprite GetGruntSprite(int w = 152, int h = 176)
        {
            if (s_grunt != null) return s_grunt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.clear);

            int cx = w / 2;
            var boot = Color.Lerp(Proto_GreyboxPalette.GruntOutfit, Color.black, 0.32f);
            FillRect(tex, cx - 29, 0, cx - 6, 16, boot);
            FillRect(tex, cx + 6, 0, cx + 29, 16, boot);
            FillRect(tex, cx - 19, 14, cx - 6, 62, Proto_GreyboxPalette.GruntOutfit);
            FillRect(tex, cx + 6, 14, cx + 19, 62, Proto_GreyboxPalette.GruntOutfit);
            FillRect(tex, cx - 35, 58, cx + 35, 104, Proto_GreyboxPalette.GruntVest);
            FillRect(tex, cx - 26, 68, cx + 26, 98, Proto_GreyboxPalette.GruntOutfit);
            var skin = new Color(168f / 255f, 150f / 255f, 138f / 255f);
            var skinHi = new Color(176f / 255f, 158f / 255f, 146f / 255f);
            FillRect(tex, cx - 16, 100, cx + 16, 118, skin);
            FillRect(tex, cx - 22, 116, cx + 22, 164, skinHi);
            var band = Proto_GreyboxPalette.GruntBandana;
            FillRect(tex, cx - 50, 124, cx + 50, 138, band);
            FillRect(tex, cx - 14, 146, cx + 14, 164, skinHi);
            FillRect(tex, cx - 8, 164, cx + 8, 172, new Color(82f / 255f, 68f / 255f, 58f / 255f));
            FillRect(tex, cx - 48, 168, cx + 48, 176, new Color(72f / 255f, 58f / 255f, 50f / 255f));
            FillRect(tex, cx - 46, 172, cx + 46, 176, Color.Lerp(Proto_GreyboxPalette.GruntVest, Color.black, 0.15f));
            FillRect(tex, cx + 20, 76, cx + 42, 112, Proto_GreyboxPalette.GruntOutfit);
            FillRect(tex, cx + 34, 88, cx + 104, 102, Color.Lerp(Proto_GreyboxPalette.GruntVest, Color.black, 0.22f));
            FillRect(tex, cx + 92, 82, cx + 112, 108, new Color(52f / 255f, 50f / 255f, 56f / 255f));

            s_grunt = FootPivotSprite(tex, h);
            return s_grunt;
        }

        static Sprite s_grunt;

        public static Sprite GetCrateSprite(int w = 96, int h = 96)
        {
            if (s_crate != null) return s_crate;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.clear);

            var side = new Color(88f / 255f, 78f / 255f, 68f / 255f);
            var sideDark = new Color(62f / 255f, 54f / 255f, 46f / 255f);
            var plank = new Color(70f / 255f, 62f / 255f, 54f / 255f);
            var top = new Color(158f / 255f, 146f / 255f, 128f / 255f);
            var topHi = new Color(178f / 255f, 168f / 255f, 150f / 255f);
            int topH = h * 38 / 100;
            int frontH = h - topH;
            int faceW = w * 72 / 100;
            FillRect(tex, 4, 0, faceW, frontH, side);
            FillRect(tex, faceW - 1, 0, w - 2, frontH, sideDark);
            FillRect(tex, 0, frontH - 1, faceW, frontH + topH, top);
            FillRect(tex, faceW - 2, frontH, w - 4, frontH + topH - 2, Color.Lerp(top, sideDark, 0.35f));
            int step = Mathf.Max(5, frontH / 6);
            for (int y = step; y < frontH; y += step)
                FillRect(tex, 4, y, faceW, y + 1, plank);
            for (int y = step; y < frontH; y += step)
                FillRect(tex, faceW, y, w - 2, y + 1, Color.Lerp(plank, sideDark, 0.45f));
            FillRect(tex, 6, frontH, faceW - 4, frontH + topH - 2, topHi);
            FillRect(tex, 8, frontH + topH / 4, faceW - 8, frontH + topH / 4 + 1, plank);
            FillRect(tex, 8, frontH + topH / 2, faceW - 8, frontH + topH / 2 + 1, plank);
            FillRect(tex, 8, frontH + 3 * topH / 4, faceW - 8, frontH + 3 * topH / 4 + 1, plank);
            FillRect(tex, 0, 0, 4, frontH, Color.Lerp(side, Color.black, 0.2f));

            s_crate = FootPivotSprite(tex, h);
            return s_crate;
        }

        static Sprite s_crate;

        public enum HudIconKind
        {
            Roll,
            Reload,
            Grenade,
            Fire
        }

        public static Sprite GetHudIconSprite(HudIconKind kind, int size = 96)
        {
            switch (kind)
            {
                case HudIconKind.Roll:
                    if (s_hudRoll != null) return s_hudRoll;
                    break;
                case HudIconKind.Reload:
                    if (s_hudReload != null) return s_hudReload;
                    break;
                case HudIconKind.Fire:
                    if (s_hudFire != null) return s_hudFire;
                    break;
                default:
                    if (s_hudGrenade != null) return s_hudGrenade;
                    break;
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.clear);

            var icon = Color.white;
            int c = size / 2;
            switch (kind)
            {
                case HudIconKind.Roll:
                    for (int a = 200; a <= 340; a += 2)
                    {
                        float rad = a * Mathf.Deg2Rad;
                        int px = c + Mathf.RoundToInt(Mathf.Cos(rad) * size * 0.34f);
                        int py = c + Mathf.RoundToInt(Mathf.Sin(rad) * size * 0.34f);
                        FillRect(tex, px - 3, py - 3, px + 4, py + 4, icon);
                    }

                    FillRect(tex, c + size / 6, c - size / 7, c + size / 6 + 10, c - size / 7 + 8, icon);
                    s_hudRoll = CenteredSprite(tex, size);
                    return s_hudRoll;
                case HudIconKind.Reload:
                    for (int y0 = c - size / 4; y0 < c + size / 4; y0++)
                    for (int x0 = c - size / 4; x0 < c + size / 5; x0++)
                        tex.SetPixel(x0, y0, icon);
                    FillRect(tex, c + size / 5, c - size / 16, c + size / 3, c + size / 6, icon);
                    s_hudReload = CenteredSprite(tex, size);
                    return s_hudReload;
                case HudIconKind.Fire:
                    int cy = c - Mathf.RoundToInt(size * 0.1f);
                    StampEllipse(tex, c, cy, size * 0.09f, size * 0.09f, icon);
                    for (int a = 0; a < 360; a += 45)
                    {
                        float rad = a * Mathf.Deg2Rad;
                        int x0 = c + Mathf.RoundToInt(Mathf.Cos(rad) * size * 0.07f);
                        int y0 = cy + Mathf.RoundToInt(Mathf.Sin(rad) * size * 0.07f);
                        int x1 = c + Mathf.RoundToInt(Mathf.Cos(rad) * size * 0.26f);
                        int y1 = cy + Mathf.RoundToInt(Mathf.Sin(rad) * size * 0.26f);
                        DrawLine(tex, x0, y0, x1, y1, icon, 2);
                    }

                    s_hudFire = CenteredSprite(tex, size);
                    return s_hudFire;
                default:
                    StampEllipse(tex, c, c - size / 28, size * 0.28f, size * 0.3f, icon);
                    FillRect(tex, c - 4, c + size / 5, c + 4, c + Mathf.RoundToInt(size / 2.6f), icon);
                    FillRect(tex, c - size / 16, c + Mathf.RoundToInt(size / 2.6f), c + size / 16, c + Mathf.RoundToInt(size / 2.1f), icon);
                    s_hudGrenade = CenteredSprite(tex, size);
                    return s_hudGrenade;
            }
        }

        static Sprite s_hudRoll;
        static Sprite s_hudReload;
        static Sprite s_hudGrenade;
        static Sprite s_hudFire;

        static Sprite CenteredSprite(Texture2D tex, int pixelsPerUnit)
        {
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
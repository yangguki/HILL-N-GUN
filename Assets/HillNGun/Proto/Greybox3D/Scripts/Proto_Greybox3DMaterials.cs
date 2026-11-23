using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DMaterials
    {
        static Shader s_cel;
        static Shader s_glow;
        static Shader s_blobShader;
        static Shader s_skyShader;
        static Shader s_sprite;
        static Shader s_cutout;
        static readonly System.Collections.Generic.Dictionary<int, Material> s_envCache =
            new System.Collections.Generic.Dictionary<int, Material>();
        static Material s_celTemplate;
        static Material s_ground;
        static Material s_wood;
        static Material s_fabric;
        static Material s_fence;
        static Material s_allyBullet;
        static Material s_allyBulletTip;
        static Material s_allyBulletOutline;
        static Material s_allyBulletTrail;
        static Material s_enemyBullet;
        static Material s_enemyBulletCore;
        static Material s_enemyBulletTrail;
        static Material s_enemyGlow;
        static Material s_blob;
        static Material s_ring;
        static Material s_sky;
        static Texture2D s_sandNoise;
        static readonly System.Collections.Generic.Dictionary<int, Material> s_celCache =
            new System.Collections.Generic.Dictionary<int, Material>();

        public static void BindFromCatalog(Proto_Greybox3DCatalog cat)
        {
            if (cat == null) return;
            if (cat.CelShader != null) s_cel = cat.CelShader;
            if (cat.GlowShader != null) s_glow = cat.GlowShader;
            if (cat.BlobShader != null) s_blobShader = cat.BlobShader;
            if (cat.SkyShader != null) s_skyShader = cat.SkyShader;
            if (cat.SpriteShader != null) s_sprite = cat.SpriteShader;
            if (cat.CutoutShader != null) s_cutout = cat.CutoutShader;
            s_ground = null;
            s_envCache.Clear();
        }

        static Shader Resolve(ref Shader cache, string name, string fallback)
        {
            if (cache != null) return cache;
            var cat = Proto_Greybox3DCatalog.Load();
            BindFromCatalog(cat);
            if (cache != null) return cache;
            cache = Shader.Find(name) ?? Shader.Find(fallback) ?? Shader.Find("UI/Default") ??
                    Shader.Find("Sprites/Default");
            return cache;
        }

        static Shader CelShader => Resolve(ref s_cel, "HillNGun/Proto/Greybox3D/Cel", "VertexLit");
        static Shader GlowShader => Resolve(ref s_glow, "HillNGun/Proto/Greybox3D/Glow", "Unlit/Color");
        static Shader BlobShader => Resolve(ref s_blobShader, "HillNGun/Proto/Greybox3D/BlobShadow", "Unlit/Color");
        static Shader SkyShader => Resolve(ref s_skyShader, "HillNGun/Proto/Greybox3D/Sky", "Unlit/Color");

        static Material SafeMaterial(Shader shader)
        {
            if (shader != null) return new Material(shader);
            Debug.LogError("Greybox3D: no fallback shader available; custom shaders may have been stripped.");
            // Last resort: a material Unity can construct without a custom shader asset.
            return new Material(Shader.Find("UI/Default"));
        }

        public static Material Cel(Color baseColor, Texture tex = null, float outline = 0.016f)
        {
            if (s_celTemplate == null)
            {
                s_celTemplate = SafeMaterial(CelShader);
                s_celTemplate.enableInstancing = true;
            }

            var m = new Material(s_celTemplate);
            ApplyColor(m, baseColor);
            if (tex != null && m.HasProperty("_MainTex"))
                m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_Saturation"))
                m.SetFloat("_Saturation", 1.55f);
            if (m.HasProperty("_OutlineWidth"))
                m.SetFloat("_OutlineWidth", outline);
            if (m.HasProperty("_OutlineColor"))
                m.SetColor("_OutlineColor", new Color(0.10f, 0.05f, 0.03f, 1f));
            if (m.HasProperty("_RimStrength"))
                m.SetFloat("_RimStrength", 0.22f);
            if (m.HasProperty("_RimColor"))
                m.SetColor("_RimColor", new Color(1f, 0.95f, 0.85f, 1f));
            return m;
        }

        public static Material CelShared(Color baseColor, float outline = 0.016f, Texture tex = null)
        {
            int key = Quantize(baseColor) ^ ((int)(outline * 1000f) << 8) ^ (tex != null ? tex.GetInstanceID() : 0);
            if (s_celCache.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var m = Cel(baseColor, tex, outline);
            m.enableInstancing = true;
            s_celCache[key] = m;
            return m;
        }

        static int Quantize(Color c)
        {
            return ((int)(c.r * 64f) << 18) ^ ((int)(c.g * 64f) << 10) ^ ((int)(c.b * 64f) << 2) ^ (int)(c.a * 8f);
        }

        public static Texture2D SandNoise()
        {
            if (s_sandNoise != null) return s_sandNoise;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "Greybox3D_SandNoise"
            };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // Mid-range dirt, not a blown-out yellow. Concept is warm orange-brown.
                float v = 0.28f
                          + 0.40f * Mathf.PerlinNoise(x * 0.09f, y * 0.09f)
                          + 0.18f * Mathf.PerlinNoise(x * 0.27f + 8f, y * 0.27f);
                float speck = Mathf.PerlinNoise(x * 0.8f, y * 0.8f);
                var col = Color.Lerp(new Color(0.50f, 0.28f, 0.13f), new Color(0.76f, 0.46f, 0.22f), Mathf.Clamp01(v));
                if (speck > 0.82f)
                    col = Color.Lerp(col, new Color(0.42f, 0.22f, 0.10f), 0.45f);
                tex.SetPixel(x, y, col);
            }
            tex.Apply(false, true);
            s_sandNoise = tex;
            return s_sandNoise;
        }

        static void ApplyColor(Material m, Color c)
        {
            m.color = c;
            if (m.HasProperty("_Color"))
                m.SetColor("_Color", c);
        }

        public static Material SharedGround
        {
            get
            {
                if (s_ground == null)
                {
                    // Solid sand: the pack dirt texture is 128px and turns into a giant
                    // checker when stretched over the playfield.
                    // Solid sand only: the pack dirt texture has a visible "Dirt SandStone" label
                    // that tiles into a grid on the playfield.
                    var cat = Proto_Greybox3DCatalog.Load();
                    if (cat != null && cat.EnvGroundDirt != null)
                    {
                        s_ground = EnvTile(cat.EnvGroundDirt, new Vector2(6.5f, 12f), new Color(1.08f, 1.04f, 1.0f, 1f));
                    }
                    else
                    {
                        s_ground = Cel(new Color(0.92f, 0.78f, 0.58f), SandNoise(), 0.0f);
                        if (s_ground.HasProperty("_MainTex"))
                            s_ground.SetTextureScale("_MainTex", new Vector2(14f, 18f));
                    }
                }
                return s_ground;
            }
        }

        public static Material SharedWood
        {
            get
            {
                if (s_wood == null)
                {
                    var cat = Proto_Greybox3DCatalog.Load();
                    s_wood = Cel(new Color(0.72f, 0.48f, 0.26f), cat != null ? cat.WoodTex : null, 0.008f);
                }
                return s_wood;
            }
        }

        public static Material SharedFabric
        {
            get
            {
                if (s_fabric == null)
                {
                    var cat = Proto_Greybox3DCatalog.Load();
                    s_fabric = Cel(Color.white, cat != null ? cat.FabricTex : null, 0.01f);
                }
                return s_fabric;
            }
        }

        public static Material SharedFence
        {
            get
            {
                if (s_fence == null)
                    s_fence = Cel(new Color(0.48f, 0.32f, 0.16f), outline: 0.01f);
                return s_fence;
            }
        }

        public static Material SharedAllyBullet
        {
            get
            {
                if (s_allyBullet == null)
                    s_allyBullet = Cel(new Color(1f, 0.82f, 0.20f), outline: 0.028f);
                return s_allyBullet;
            }
        }

        public static Material SharedAllyBulletTip
        {
            get
            {
                if (s_allyBulletTip == null)
                    s_allyBulletTip = Cel(new Color(1f, 0.90f, 0.45f), outline: 0.022f);
                return s_allyBulletTip;
            }
        }

        public static Material SharedAllyBulletOutline
        {
            get
            {
                if (s_allyBulletOutline == null)
                    s_allyBulletOutline = Cel(new Color(0.18f, 0.10f, 0.04f), outline: 0.0f);
                return s_allyBulletOutline;
            }
        }

        public static Material SharedAllyBulletTrail
        {
            get
            {
                if (s_allyBulletTrail == null)
                    s_allyBulletTrail = Glow(new Color(1f, 0.92f, 0.25f, 0.85f), 2.2f);
                return s_allyBulletTrail;
            }
        }

        public static Material SharedEnemyBullet
        {
            get
            {
                if (s_enemyBullet == null)
                    s_enemyBullet = Glow(new Color(0.95f, 0.08f, 0.04f, 1f), 1.6f);
                return s_enemyBullet;
            }
        }

        public static Material SharedEnemyBulletCore
        {
            get
            {
                if (s_enemyBulletCore == null)
                    s_enemyBulletCore = Glow(new Color(1f, 0.85f, 0.25f, 1f), 2.2f);
                return s_enemyBulletCore;
            }
        }

        public static Material SharedEnemyBulletTrail
        {
            get
            {
                if (s_enemyBulletTrail == null)
                    s_enemyBulletTrail = Glow(new Color(1f, 0.18f, 0.04f, 0.9f), 2.6f);
                return s_enemyBulletTrail;
            }
        }

        public static Material SharedEnemyGlow
        {
            get
            {
                if (s_enemyGlow == null)
                    s_enemyGlow = Glow(new Color(1f, 0.45f, 0.08f, 0.55f), 1.2f);
                return s_enemyGlow;
            }
        }

        public static Material SharedBlob
        {
            get
            {
                if (s_blob == null)
                {
                    s_blob = SafeMaterial(BlobShader);
                    if (s_blob.HasProperty("_Color"))
                        s_blob.SetColor("_Color", new Color(0.12f, 0.07f, 0.03f, 0.42f));
                }
                return s_blob;
            }
        }

        public static Material SharedRing
        {
            get
            {
                if (s_ring == null)
                    s_ring = Glow(new Color(1f, 0.85f, 0.25f, 0.85f), 1.6f);
                return s_ring;
            }
        }

        public static Material SharedSky
        {
            get
            {
                if (s_sky == null)
                {
                    s_sky = SafeMaterial(SkyShader);
                    var cat = Proto_Greybox3DCatalog.Load();
                    if (s_sky.HasProperty("_Color"))
                        s_sky.SetColor("_Color", new Color(0.04f, 0.26f, 0.90f));
                    if (s_sky.HasProperty("_Horizon"))
                        s_sky.SetColor("_Horizon", new Color(0.36f, 0.66f, 0.98f));
                    if (cat != null && cat.SkyboxDay != null && s_sky.HasProperty("_MainTex"))
                    {
                        s_sky.SetTexture("_MainTex", cat.SkyboxDay);
                        if (s_sky.HasProperty("_UseTex"))
                            s_sky.SetFloat("_UseTex", 0.0f);
                    }
                    s_sky.color = new Color(0.04f, 0.26f, 0.90f);
                }
                return s_sky;
            }
        }

        public static Material Unlit(Color color)
        {
            var m = SafeMaterial(Shader.Find("Unlit/Color"));
            m.color = color;
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            return m;
        }

        public static Material Glow(Color color, float intensity)
        {
            var m = SafeMaterial(GlowShader);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", intensity);
            m.color = color;
            return m;
        }

        public static Material Awning(Color tint)
        {
            var cat = Proto_Greybox3DCatalog.Load();
            return Cel(tint, cat != null ? cat.FabricTex : null, 0.01f);
        }
        static Shader SpriteShader => Resolve(ref s_sprite, "HillNGun/Proto/Greybox3D/Sprite", "Sprites/Default");

        public static Material Sprite(Texture tex, Color color)
        {
            var m = SafeMaterial(SpriteShader);
            ApplyColor(m, color);
            if (tex != null && m.HasProperty("_MainTex"))
                m.SetTexture("_MainTex", tex);
            m.renderQueue = 3000;
            return m;
        }

        public static Material EnvTile(Texture tex, Vector2 tiling, Color tint)
        {
            int key = (tex != null ? tex.GetInstanceID() : 0)
                      ^ (Quantize(tint) << 1)
                      ^ ((int)(tiling.x * 10f) << 16)
                      ^ (int)(tiling.y * 10f);
            if (s_envCache.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var m = Cel(tint, tex, 0.004f);
            if (m.HasProperty("_MainTex"))
                m.SetTextureScale("_MainTex", tiling);
            if (m.HasProperty("_Saturation"))
                m.SetFloat("_Saturation", 1.15f);
            s_envCache[key] = m;
            return m;
        }

        public static Material Trail(Texture tex, Color color)
        {
            var m = Glow(color, 1.4f);
            if (tex != null && m.HasProperty("_MainTex"))
                m.SetTexture("_MainTex", tex);
            return m;
        }

        static Shader CutoutShader => Resolve(ref s_cutout, "HillNGun/Proto/Greybox3D/Cutout", "Transparent/Cutout/Diffuse");

        public static Material Cutout(Texture tex, Color tint, float cutoff, Vector2 tiling)
        {
            int key = (tex != null ? tex.GetInstanceID() : 0) ^ Quantize(tint) ^ ((int)(cutoff * 100f) << 10) ^ ((int)(tiling.x * 10f) << 16);
            if (s_envCache.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var m = SafeMaterial(CutoutShader);
            ApplyColor(m, tint);
            if (tex != null && m.HasProperty("_MainTex"))
            {
                m.SetTexture("_MainTex", tex);
                m.SetTextureScale("_MainTex", tiling);
            }
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", cutoff);
            s_envCache[key] = m;
            return m;
        }

    }
}

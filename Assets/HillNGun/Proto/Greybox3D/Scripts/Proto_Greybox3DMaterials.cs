using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DMaterials
    {
        static Shader s_cel;
        static Shader s_glow;
        static Shader s_blobShader;
        static Shader s_skyShader;
        static Material s_celTemplate;
        static Material s_ground;
        static Material s_wood;
        static Material s_fabric;
        static Material s_fence;
        static Material s_allyBullet;
        static Material s_enemyBullet;
        static Material s_enemyGlow;
        static Material s_blob;
        static Material s_ring;
        static Material s_sky;

        public static void BindFromCatalog(Proto_Greybox3DCatalog cat)
        {
            if (cat == null) return;
            if (cat.CelShader != null) s_cel = cat.CelShader;
            if (cat.GlowShader != null) s_glow = cat.GlowShader;
            if (cat.BlobShader != null) s_blobShader = cat.BlobShader;
            if (cat.SkyShader != null) s_skyShader = cat.SkyShader;
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
            return m;
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
                    s_ground = Cel(new Color(0.90f, 0.58f, 0.28f), null, 0.0f);
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
                    s_allyBullet = Glow(new Color(1f, 0.92f, 0.25f, 0.95f), 2.4f);
                return s_allyBullet;
            }
        }

        public static Material SharedEnemyBullet
        {
            get
            {
                if (s_enemyBullet == null)
                    s_enemyBullet = Glow(new Color(1f, 0.28f, 0.08f, 1f), 3.2f);
                return s_enemyBullet;
            }
        }

        public static Material SharedEnemyGlow
        {
            get
            {
                if (s_enemyGlow == null)
                    s_enemyGlow = Glow(new Color(1f, 0.55f, 0.12f, 0.7f), 1.8f);
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
                        s_sky.SetColor("_Color", new Color(0.16f, 0.42f, 0.92f));
                    if (s_sky.HasProperty("_Horizon"))
                        s_sky.SetColor("_Horizon", new Color(0.78f, 0.88f, 0.98f));
                    if (cat != null && cat.SkyboxDay != null && s_sky.HasProperty("_MainTex"))
                    {
                        s_sky.SetTexture("_MainTex", cat.SkyboxDay);
                        if (s_sky.HasProperty("_UseTex"))
                            s_sky.SetFloat("_UseTex", 0.55f);
                    }
                    s_sky.color = new Color(0.16f, 0.42f, 0.92f);
                }
                return s_sky;
            }
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
    }
}

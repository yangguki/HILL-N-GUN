using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DMaterials
    {
        static Material s_cel;
        static Material s_ground;
        static Material s_wood;
        static Material s_fence;
        static Material s_allyBullet;
        static Material s_enemyBullet;
        static Material s_sky;

        static Shader CelShader =>
            Shader.Find("HillNGun/Proto/Greybox3D/Cel") ?? Shader.Find("Unlit/Color");

        static Shader SkyShader =>
            Shader.Find("HillNGun/Proto/Greybox3D/Sky") ?? Shader.Find("Unlit/Color");

        public static Material Cel(Color baseColor)
        {
            if (s_cel == null)
            {
                s_cel = new Material(CelShader);
                s_cel.enableInstancing = true;
            }

            var m = new Material(s_cel);
            m.color = baseColor;
            if (m.HasProperty("_Color"))
                m.SetColor("_Color", baseColor);
            return m;
        }

        public static Material SharedGround
        {
            get
            {
                if (s_ground == null)
                    s_ground = Cel(new Color(0.82f, 0.7f, 0.48f));
                return s_ground;
            }
        }

        public static Material SharedWood
        {
            get
            {
                if (s_wood == null)
                    s_wood = Cel(new Color(0.55f, 0.36f, 0.2f));
                return s_wood;
            }
        }

        public static Material SharedFence
        {
            get
            {
                if (s_fence == null)
                    s_fence = Cel(new Color(0.45f, 0.32f, 0.18f));
                return s_fence;
            }
        }

        public static Material SharedAllyBullet
        {
            get
            {
                if (s_allyBullet == null)
                    s_allyBullet = Cel(new Color(1f, 0.92f, 0.22f));
                return s_allyBullet;
            }
        }

        public static Material SharedEnemyBullet
        {
            get
            {
                if (s_enemyBullet == null)
                    s_enemyBullet = Cel(new Color(1f, 0.35f, 0.12f));
                return s_enemyBullet;
            }
        }

        public static Material SharedSky
        {
            get
            {
                if (s_sky == null)
                {
                    s_sky = new Material(SkyShader);
                    if (s_sky.HasProperty("_Color"))
                        s_sky.SetColor("_Color", new Color(0.42f, 0.70f, 0.95f));
                    if (s_sky.HasProperty("_Horizon"))
                        s_sky.SetColor("_Horizon", new Color(0.90f, 0.80f, 0.62f));
                    s_sky.color = new Color(0.42f, 0.70f, 0.95f);
                }
                return s_sky;
            }
        }
    }
}
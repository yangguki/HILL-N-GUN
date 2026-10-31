using UnityEngine;
using UnityEditor;
using System.IO;

namespace HillNGun.Editor
{
    public static class ArtGenerator
    {
        private const string OUTPUT_PATH = "Assets/HillNGun/Art/Generated";
        private const int PPU = 32;

        [MenuItem("HillNGun/Art/Generate Placeholder Art", false, 50)]
        public static void GenerateAllArt()
        {
            EnsureDirectories();
            
            GenerateCharacterSprites();
            GenerateEnemySprites();
            GenerateBossSprites();
            GenerateCoverSprites();
            GenerateProjectileSprites();
            GenerateEffectSprites();
            GenerateBackgroundLayers();
            GenerateUISprites();
            
            AssetDatabase.Refresh();
            Debug.Log("[ArtGenerator] All placeholder art generated successfully!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = new string[]
            {
                OUTPUT_PATH,
                $"{OUTPUT_PATH}/Characters",
                $"{OUTPUT_PATH}/Enemies",
                $"{OUTPUT_PATH}/Boss",
                $"{OUTPUT_PATH}/Covers",
                $"{OUTPUT_PATH}/Projectiles",
                $"{OUTPUT_PATH}/Effects",
                $"{OUTPUT_PATH}/Background",
                $"{OUTPUT_PATH}/UI"
            };

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        #region Characters
        private static void GenerateCharacterSprites()
        {
            GenerateCharacter("buster", new Color(0.85f, 0.25f, 0.2f), CharacterStyle.Assault);
            GenerateCharacter("raven", new Color(0.5f, 0.25f, 0.7f), CharacterStyle.Sniper);
            GenerateCharacter("volt", new Color(0.2f, 0.6f, 0.85f), CharacterStyle.Support);
        }

        private enum CharacterStyle { Assault, Sniper, Support }

        private static void GenerateCharacter(string name, Color mainColor, CharacterStyle style)
        {
            int size = 64;
            Color outlineColor = new Color(0.15f, 0.1f, 0.08f);
            Color skinColor = new Color(0.9f, 0.75f, 0.6f);
            Color hatColor = new Color(0.4f, 0.25f, 0.15f);
            Color beltColor = new Color(0.5f, 0.35f, 0.2f);

            SaveSprite($"Characters/{name}_idle", DrawCharacterIdle(size, mainColor, outlineColor, skinColor, hatColor, beltColor, style));
            SaveSprite($"Characters/{name}_shoot", DrawCharacterShoot(size, mainColor, outlineColor, skinColor, hatColor, beltColor, style));
            SaveSprite($"Characters/{name}_roll", DrawCharacterRoll(size, mainColor, outlineColor, skinColor, hatColor, style));
            SaveSprite($"Characters/{name}_downed", DrawCharacterDowned(size, mainColor, outlineColor, skinColor, hatColor, style));
        }

        private static Texture2D DrawCharacterIdle(int size, Color main, Color outline, Color skin, Color hat, Color belt, CharacterStyle style)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int cx = size / 2;
            int cy = size / 2;

            DrawFilledEllipse(tex, cx, cy - 8, 10, 14, main);
            DrawFilledEllipse(tex, cx, cy - 8, 9, 13, Color.Lerp(main, Color.white, 0.15f));
            DrawFilledCircle(tex, cx, cy + 10, 8, skin);
            DrawFilledRect(tex, cx - 8, cy + 18, 16, 10, hat);
            DrawFilledRect(tex, cx - 10, cy + 17, 20, 3, Color.Lerp(hat, Color.black, 0.3f));
            DrawFilledRect(tex, cx - 4, cy - 4, 8, 3, belt);
            DrawFilledRect(tex, cx + 3, cy - 3, 4, 4, new Color(0.7f, 0.6f, 0.2f));
            DrawFilledEllipse(tex, cx - 8, cy - 18, 4, 6, main);
            DrawFilledEllipse(tex, cx + 8, cy - 18, 4, 6, main);
            DrawFilledRect(tex, cx - 6, cy - 24, 4, 4, new Color(0.3f, 0.2f, 0.15f));
            DrawFilledRect(tex, cx + 2, cy - 24, 4, 4, new Color(0.3f, 0.2f, 0.15f));

            if (style == CharacterStyle.Assault)
            {
                DrawFilledRect(tex, cx + 10, cy + 2, 14, 4, new Color(0.4f, 0.35f, 0.3f));
                DrawFilledRect(tex, cx + 6, cy, 6, 3, new Color(0.5f, 0.4f, 0.3f));
            }
            else if (style == CharacterStyle.Sniper)
            {
                DrawFilledRect(tex, cx + 8, cy + 4, 20, 3, new Color(0.35f, 0.3f, 0.25f));
                DrawFilledCircle(tex, cx + 26, cy + 5, 2, new Color(0.3f, 0.5f, 0.6f));
            }
            else
            {
                DrawFilledRect(tex, cx + 8, cy + 2, 10, 5, new Color(0.45f, 0.4f, 0.35f));
                DrawFilledCircle(tex, cx + 16, cy + 4, 3, new Color(0.6f, 0.6f, 0.2f));
            }

            DrawPixel(tex, cx - 2, cy + 13, Color.black);
            DrawPixel(tex, cx + 2, cy + 13, Color.black);

            tex.Apply();
            return tex;
        }

        private static Texture2D DrawCharacterShoot(int size, Color main, Color outline, Color skin, Color hat, Color belt, CharacterStyle style)
        {
            var tex = DrawCharacterIdle(size, main, outline, skin, hat, belt, style);
            
            int cx = size / 2;
            int cy = size / 2;
            DrawFilledCircle(tex, cx + 22, cy + 4, 4, new Color(1f, 0.9f, 0.3f, 0.9f));
            DrawFilledCircle(tex, cx + 22, cy + 4, 2, new Color(1f, 1f, 0.8f, 0.9f));

            tex.Apply();
            return tex;
        }

        private static Texture2D DrawCharacterRoll(int size, Color main, Color outline, Color skin, Color hat, CharacterStyle style)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int cx = size / 2;
            int cy = size / 2;

            DrawFilledCircle(tex, cx, cy, 12, main);
            DrawFilledCircle(tex, cx, cy, 10, Color.Lerp(main, Color.white, 0.1f));
            
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                int px = cx + Mathf.RoundToInt(Mathf.Cos(angle) * 6);
                int py = cy + Mathf.RoundToInt(Mathf.Sin(angle) * 6);
                DrawFilledCircle(tex, px, py, 2, Color.Lerp(main, Color.black, 0.3f));
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D DrawCharacterDowned(int size, Color main, Color outline, Color skin, Color hat, CharacterStyle style)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int cx = size / 2;
            int cy = size / 2;

            DrawFilledEllipse(tex, cx, cy - 4, 14, 8, main);
            DrawFilledEllipse(tex, cx, cy - 4, 12, 6, Color.Lerp(main, Color.gray, 0.3f));
            DrawFilledCircle(tex, cx - 10, cy, 6, skin);
            DrawFilledRect(tex, cx - 18, cy - 2, 8, 6, hat);

            Color starColor = new Color(1f, 0.9f, 0.2f);
            DrawStar(tex, cx + 6, cy + 10, 4, starColor);
            DrawStar(tex, cx - 2, cy + 14, 3, starColor);
            DrawStar(tex, cx + 10, cy + 6, 3, starColor);

            tex.Apply();
            return tex;
        }

        private static void DrawStar(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int i = 0; i < 5; i++)
            {
                float angle = (i * 72f - 90f) * Mathf.Deg2Rad;
                int px = cx + Mathf.RoundToInt(Mathf.Cos(angle) * radius);
                int py = cy + Mathf.RoundToInt(Mathf.Sin(angle) * radius);
                DrawLine(tex, cx, cy, px, py, color);
            }
            DrawPixel(tex, cx, cy, color);
        }
        #endregion

        #region Enemies
        private static void GenerateEnemySprites()
        {
            GeneratePistolMook();
            GenerateDynamiteBandit();
            GenerateMountedRaider();
        }

        private static void GeneratePistolMook()
        {
            int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int cx = size / 2;
            int cy = size / 2;

            Color clothColor = new Color(0.6f, 0.5f, 0.35f);
            Color skinColor = new Color(0.85f, 0.7f, 0.55f);

            DrawFilledEllipse(tex, cx, cy - 6, 8, 10, clothColor);
            DrawFilledCircle(tex, cx, cy + 6, 6, skinColor);
            DrawFilledRect(tex, cx - 5, cy + 12, 10, 6, new Color(0.3f, 0.25f, 0.2f));
            DrawFilledEllipse(tex, cx - 6, cy - 14, 3, 4, clothColor);
            DrawFilledEllipse(tex, cx + 6, cy - 14, 3, 4, clothColor);
            DrawFilledRect(tex, cx + 8, cy, 8, 3, new Color(0.4f, 0.35f, 0.3f));

            DrawPixel(tex, cx - 2, cy + 8, Color.black);
            DrawPixel(tex, cx + 2, cy + 8, Color.black);

            tex.Apply();
            SaveSprite("Enemies/pistol_mook", tex);
        }

        private static void GenerateDynamiteBandit()
        {
            int size = 56;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int cx = size / 2;
            int cy = size / 2;

            Color clothColor = new Color(0.5f, 0.3f, 0.2f);
            Color skinColor = new Color(0.8f, 0.65f, 0.5f);

            DrawFilledEllipse(tex, cx, cy - 6, 10, 12, clothColor);
            DrawFilledCircle(tex, cx, cy + 8, 7, skinColor);
            DrawFilledRect(tex, cx - 6, cy + 15, 12, 5, new Color(0.25f, 0.2f, 0.15f));
            DrawFilledEllipse(tex, cx - 8, cy - 16, 4, 5, clothColor);
            DrawFilledEllipse(tex, cx + 8, cy - 16, 4, 5, clothColor);

            DrawFilledRect(tex, cx - 16, cy - 2, 6, 12, new Color(0.7f, 0.2f, 0.15f));
            DrawFilledRect(tex, cx - 15, cy + 8, 4, 4, new Color(0.9f, 0.8f, 0.2f));
            DrawLine(tex, cx - 13, cy + 12, cx - 13, cy + 16, new Color(0.3f, 0.25f, 0.2f));

            DrawPixel(tex, cx - 2, cy + 10, Color.black);
            DrawPixel(tex, cx + 2, cy + 10, Color.black);

            tex.Apply();
            SaveSprite("Enemies/dynamite_bandit", tex);
        }

        private static void GenerateMountedRaider()
        {
            int size = 72;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int cx = size / 2;
            int cy = size / 2;

            Color horseColor = new Color(0.5f, 0.35f, 0.25f);
            Color riderColor = new Color(0.45f, 0.35f, 0.25f);
            Color skinColor = new Color(0.85f, 0.7f, 0.55f);

            DrawFilledEllipse(tex, cx, cy - 12, 16, 10, horseColor);
            DrawFilledEllipse(tex, cx + 16, cy - 8, 6, 8, horseColor);
            DrawFilledEllipse(tex, cx - 10, cy - 24, 3, 6, horseColor);
            DrawFilledEllipse(tex, cx - 4, cy - 24, 3, 6, horseColor);
            DrawFilledEllipse(tex, cx + 6, cy - 24, 3, 6, horseColor);
            DrawFilledEllipse(tex, cx + 12, cy - 24, 3, 6, horseColor);

            DrawFilledEllipse(tex, cx - 2, cy + 4, 6, 8, riderColor);
            DrawFilledCircle(tex, cx - 2, cy + 14, 5, skinColor);
            DrawFilledRect(tex, cx - 6, cy + 19, 8, 6, new Color(0.3f, 0.2f, 0.15f));

            DrawFilledRect(tex, cx + 6, cy + 8, 12, 3, new Color(0.4f, 0.35f, 0.3f));

            DrawPixel(tex, cx - 4, cy + 15, Color.black);
            DrawPixel(tex, cx, cy + 15, Color.black);
            DrawPixel(tex, cx + 18, cy - 6, Color.black);

            tex.Apply();
            SaveSprite("Enemies/mounted_raider", tex);
        }
        #endregion

        #region Boss
        private static void GenerateBossSprites()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int cx = size / 2;
            int cy = size / 2;

            Color bodyColor = new Color(0.45f, 0.5f, 0.35f);
            Color bellyColor = new Color(0.7f, 0.65f, 0.5f);
            Color patternColor = new Color(0.3f, 0.35f, 0.25f);

            for (int i = 0; i < 8; i++)
            {
                int tailX = cx - 30 - i * 4;
                int tailY = cy - 10 + Mathf.RoundToInt(Mathf.Sin(i * 0.8f) * 6);
                int tailR = 8 - i / 2;
                DrawFilledCircle(tex, tailX, tailY, tailR, bodyColor);
                DrawFilledCircle(tex, tailX, tailY - tailR / 2, tailR - 2, bellyColor);
            }

            DrawFilledEllipse(tex, cx, cy - 8, 24, 18, bodyColor);
            DrawFilledEllipse(tex, cx, cy - 14, 20, 10, bellyColor);

            DrawFilledEllipse(tex, cx + 30, cy, 14, 12, bodyColor);
            DrawFilledEllipse(tex, cx + 30, cy - 4, 10, 6, bellyColor);

            for (int i = 0; i < 5; i++)
            {
                int px = cx - 15 + i * 8;
                int py = cy - 2 + (i % 2) * 4;
                DrawFilledCircle(tex, px, py, 3, patternColor);
            }

            DrawFilledCircle(tex, cx + 36, cy + 4, 3, new Color(0.9f, 0.2f, 0.15f));
            DrawFilledCircle(tex, cx + 36, cy + 4, 1, Color.black);

            int fangX = cx + 40;
            DrawFilledRect(tex, fangX, cy - 4, 2, 6, new Color(0.95f, 0.95f, 0.9f));
            DrawFilledRect(tex, fangX + 4, cy - 4, 2, 6, new Color(0.95f, 0.95f, 0.9f));

            int rattleX = cx - 58;
            for (int i = 0; i < 4; i++)
            {
                DrawFilledCircle(tex, rattleX - i * 3, cy - 10, 3 - i / 2, new Color(0.8f, 0.75f, 0.6f));
            }

            tex.Apply();
            SaveSprite("Boss/rattlesnake_rex", tex);

            var warningTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(warningTex);
            DrawFilledCircle(warningTex, 32, 32, 28, new Color(1f, 0.3f, 0.2f, 0.6f));
            DrawFilledCircle(warningTex, 32, 32, 20, new Color(1f, 0.5f, 0.3f, 0.4f));
            DrawFilledRect(warningTex, 30, 20, 4, 16, Color.white);
            DrawFilledCircle(warningTex, 32, 42, 3, Color.white);
            warningTex.Apply();
            SaveSprite("Boss/warning_indicator", warningTex);
        }
        #endregion

        #region Covers
        private static void GenerateCoverSprites()
        {
            GenerateCover("barrel", 48, 56, DrawBarrel);
            GenerateCover("crate", 56, 48, DrawCrate);
            GenerateCover("wagon", 80, 56, DrawWagon);
        }

        private delegate void CoverDrawer(Texture2D tex, int stage);

        private static void GenerateCover(string name, int width, int height, CoverDrawer drawer)
        {
            string[] stages = { "intact", "cracked", "damaged", "destroyed" };
            for (int i = 0; i < 4; i++)
            {
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                ClearTexture(tex);
                drawer(tex, i);
                tex.Apply();
                SaveSprite($"Covers/{name}_{stages[i]}", tex);
            }
        }

        private static void DrawBarrel(Texture2D tex, int stage)
        {
            int cx = tex.width / 2;
            int cy = tex.height / 2;
            Color woodColor = new Color(0.55f, 0.4f, 0.25f);
            Color bandColor = new Color(0.3f, 0.25f, 0.2f);

            float damage = stage * 0.25f;
            woodColor = Color.Lerp(woodColor, new Color(0.4f, 0.3f, 0.2f), damage);

            DrawFilledEllipse(tex, cx, cy, 18, 24, woodColor);
            DrawFilledRect(tex, cx - 16, cy + 18, 32, 3, bandColor);
            DrawFilledRect(tex, cx - 16, cy - 20, 32, 3, bandColor);
            DrawFilledRect(tex, cx - 14, cy, 28, 2, bandColor);

            if (stage >= 1)
            {
                DrawLine(tex, cx - 8, cy + 20, cx - 4, cy - 10, Color.Lerp(woodColor, Color.black, 0.3f));
            }
            if (stage >= 2)
            {
                DrawLine(tex, cx + 6, cy + 15, cx + 10, cy - 15, Color.Lerp(woodColor, Color.black, 0.3f));
                DrawLine(tex, cx - 10, cy - 5, cx - 2, cy - 20, Color.Lerp(woodColor, Color.black, 0.3f));
            }
            if (stage >= 3)
            {
                DrawFilledEllipse(tex, cx, cy, 14, 20, Color.clear);
                for (int i = 0; i < 6; i++)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    int px = cx + Mathf.RoundToInt(Mathf.Cos(angle) * Random.Range(12f, 20f));
                    int py = cy + Mathf.RoundToInt(Mathf.Sin(angle) * Random.Range(16f, 26f));
                    DrawFilledRect(tex, px - 2, py - 3, 4, 6, woodColor);
                }
            }
        }

        private static void DrawCrate(Texture2D tex, int stage)
        {
            int cx = tex.width / 2;
            int cy = tex.height / 2;
            Color woodColor = new Color(0.6f, 0.45f, 0.3f);
            Color darkWood = new Color(0.4f, 0.3f, 0.2f);

            float damage = stage * 0.2f;
            woodColor = Color.Lerp(woodColor, new Color(0.45f, 0.35f, 0.25f), damage);

            DrawFilledRect(tex, cx - 24, cy - 20, 48, 40, woodColor);
            
            for (int i = 0; i < 5; i++)
            {
                int y = cy - 18 + i * 9;
                DrawLine(tex, cx - 22, y, cx + 22, y, darkWood);
            }
            
            DrawLine(tex, cx - 24, cy - 20, cx + 24, cy + 20, darkWood);
            DrawLine(tex, cx + 24, cy - 20, cx - 24, cy + 20, darkWood);

            if (stage >= 1)
            {
                DrawLine(tex, cx - 10, cy - 20, cx - 6, cy + 10, Color.Lerp(woodColor, Color.black, 0.4f));
            }
            if (stage >= 2)
            {
                DrawLine(tex, cx + 8, cy - 15, cx + 14, cy + 15, Color.Lerp(woodColor, Color.black, 0.4f));
            }
            if (stage >= 3)
            {
                DrawFilledRect(tex, cx - 12, cy - 10, 24, 20, Color.clear);
            }
        }

        private static void DrawWagon(Texture2D tex, int stage)
        {
            int cx = tex.width / 2;
            int cy = tex.height / 2;
            Color woodColor = new Color(0.5f, 0.38f, 0.25f);
            Color wheelColor = new Color(0.35f, 0.28f, 0.2f);
            Color metalColor = new Color(0.4f, 0.38f, 0.35f);

            float damage = stage * 0.2f;
            woodColor = Color.Lerp(woodColor, new Color(0.4f, 0.3f, 0.22f), damage);

            DrawFilledRect(tex, cx - 36, cy - 8, 72, 24, woodColor);
            DrawFilledRect(tex, cx - 38, cy - 10, 76, 4, Color.Lerp(woodColor, Color.black, 0.2f));

            DrawFilledCircle(tex, cx - 26, cy - 20, 10, wheelColor);
            DrawFilledCircle(tex, cx - 26, cy - 20, 6, Color.clear);
            DrawFilledCircle(tex, cx + 26, cy - 20, 10, wheelColor);
            DrawFilledCircle(tex, cx + 26, cy - 20, 6, Color.clear);

            for (int w = 0; w < 2; w++)
            {
                int wx = w == 0 ? cx - 26 : cx + 26;
                for (int s = 0; s < 4; s++)
                {
                    float angle = s * 45f * Mathf.Deg2Rad;
                    int x1 = wx + Mathf.RoundToInt(Mathf.Cos(angle) * 3);
                    int y1 = cy - 20 + Mathf.RoundToInt(Mathf.Sin(angle) * 3);
                    int x2 = wx + Mathf.RoundToInt(Mathf.Cos(angle) * 9);
                    int y2 = cy - 20 + Mathf.RoundToInt(Mathf.Sin(angle) * 9);
                    DrawLine(tex, x1, y1, x2, y2, wheelColor);
                }
            }

            if (stage >= 1)
            {
                DrawLine(tex, cx - 20, cy + 12, cx - 10, cy - 6, Color.Lerp(woodColor, Color.black, 0.3f));
            }
            if (stage >= 2)
            {
                DrawLine(tex, cx + 10, cy + 10, cx + 18, cy - 4, Color.Lerp(woodColor, Color.black, 0.3f));
                DrawFilledCircle(tex, cx + 26, cy - 20, 8, Color.clear);
            }
            if (stage >= 3)
            {
                DrawFilledRect(tex, cx - 20, cy - 6, 40, 16, Color.clear);
                DrawFilledCircle(tex, cx - 26, cy - 20, 8, Color.clear);
            }
        }
        #endregion

        #region Projectiles
        private static void GenerateProjectileSprites()
        {
            var bullet = new Texture2D(16, 8, TextureFormat.RGBA32, false);
            bullet.filterMode = FilterMode.Point;
            ClearTexture(bullet);
            DrawFilledEllipse(bullet, 8, 4, 6, 3, new Color(0.9f, 0.85f, 0.3f));
            DrawFilledEllipse(bullet, 10, 4, 3, 2, new Color(1f, 0.95f, 0.6f));
            bullet.Apply();
            SaveSprite("Projectiles/bullet", bullet);

            var heavyBullet = new Texture2D(24, 10, TextureFormat.RGBA32, false);
            heavyBullet.filterMode = FilterMode.Point;
            ClearTexture(heavyBullet);
            DrawFilledEllipse(heavyBullet, 12, 5, 10, 4, new Color(0.85f, 0.5f, 0.2f));
            DrawFilledEllipse(heavyBullet, 16, 5, 4, 3, new Color(1f, 0.7f, 0.4f));
            heavyBullet.Apply();
            SaveSprite("Projectiles/bullet_heavy", heavyBullet);

            var grenade = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            grenade.filterMode = FilterMode.Point;
            ClearTexture(grenade);
            DrawFilledCircle(grenade, 12, 10, 8, new Color(0.35f, 0.4f, 0.3f));
            DrawFilledRect(grenade, 10, 18, 4, 4, new Color(0.5f, 0.45f, 0.4f));
            DrawLine(grenade, 12, 22, 12, 24, new Color(0.7f, 0.5f, 0.3f));
            grenade.Apply();
            SaveSprite("Projectiles/grenade", grenade);
        }
        #endregion

        #region Effects
        private static void GenerateEffectSprites()
        {
            var muzzleFlash = new Texture2D(32, 24, TextureFormat.RGBA32, false);
            muzzleFlash.filterMode = FilterMode.Point;
            ClearTexture(muzzleFlash);
            DrawFilledEllipse(muzzleFlash, 8, 12, 6, 8, new Color(1f, 0.9f, 0.3f, 0.9f));
            DrawFilledEllipse(muzzleFlash, 18, 12, 10, 5, new Color(1f, 0.8f, 0.2f, 0.7f));
            DrawFilledEllipse(muzzleFlash, 26, 12, 4, 3, new Color(1f, 0.95f, 0.5f, 0.5f));
            muzzleFlash.Apply();
            SaveSprite("Effects/muzzle_flash", muzzleFlash);

            var hitSpark = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            hitSpark.filterMode = FilterMode.Point;
            ClearTexture(hitSpark);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                int x = 12 + Mathf.RoundToInt(Mathf.Cos(angle) * 8);
                int y = 12 + Mathf.RoundToInt(Mathf.Sin(angle) * 8);
                DrawLine(hitSpark, 12, 12, x, y, new Color(1f, 0.9f, 0.4f));
            }
            DrawFilledCircle(hitSpark, 12, 12, 3, new Color(1f, 1f, 0.8f));
            hitSpark.Apply();
            SaveSprite("Effects/hit_spark", hitSpark);

            var dustPuff = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            dustPuff.filterMode = FilterMode.Point;
            ClearTexture(dustPuff);
            DrawFilledCircle(dustPuff, 16, 16, 10, new Color(0.7f, 0.6f, 0.45f, 0.6f));
            DrawFilledCircle(dustPuff, 10, 18, 6, new Color(0.65f, 0.55f, 0.4f, 0.5f));
            DrawFilledCircle(dustPuff, 22, 14, 7, new Color(0.68f, 0.58f, 0.42f, 0.5f));
            dustPuff.Apply();
            SaveSprite("Effects/dust_puff", dustPuff);

            var explosion = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            explosion.filterMode = FilterMode.Point;
            ClearTexture(explosion);
            DrawFilledCircle(explosion, 32, 32, 28, new Color(1f, 0.5f, 0.1f, 0.7f));
            DrawFilledCircle(explosion, 32, 32, 20, new Color(1f, 0.7f, 0.2f, 0.8f));
            DrawFilledCircle(explosion, 32, 32, 12, new Color(1f, 0.9f, 0.5f, 0.9f));
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                int x = 32 + Mathf.RoundToInt(Mathf.Cos(angle) * 26);
                int y = 32 + Mathf.RoundToInt(Mathf.Sin(angle) * 26);
                DrawFilledCircle(explosion, x, y, 4, new Color(1f, 0.6f, 0.2f, 0.6f));
            }
            explosion.Apply();
            SaveSprite("Effects/explosion", explosion);
        }
        #endregion

        #region Background
        private static void GenerateBackgroundLayers()
        {
            GenerateSkyGradient();
            GenerateMesas();
            GenerateSaloonFacades();
            GenerateGround();
        }

        private static void GenerateSkyGradient()
        {
            int width = 512;
            int height = 256;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color topColor = new Color(0.3f, 0.5f, 0.75f);
            Color bottomColor = new Color(0.9f, 0.75f, 0.5f);

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                Color c = Color.Lerp(bottomColor, topColor, t);
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            SaveSprite("Background/sky_gradient", tex, FilterMode.Bilinear);
        }

        private static void GenerateMesas()
        {
            int width = 512;
            int height = 128;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            Color mesaColor = new Color(0.55f, 0.4f, 0.3f);
            Color mesaDark = new Color(0.4f, 0.3f, 0.22f);

            int[] mesaX = { 50, 180, 320, 420 };
            int[] mesaW = { 80, 100, 90, 70 };
            int[] mesaH = { 60, 80, 70, 55 };

            foreach (int i in new int[] { 0, 1, 2, 3 })
            {
                int x = mesaX[i];
                int w = mesaW[i];
                int h = mesaH[i];

                for (int px = x - w / 2; px < x + w / 2; px++)
                {
                    if (px < 0 || px >= width) continue;
                    int edgeDist = Mathf.Min(px - (x - w / 2), (x + w / 2) - px);
                    int colH = h - Mathf.Max(0, 8 - edgeDist) * 3;
                    
                    for (int py = 0; py < colH; py++)
                    {
                        if (py >= height) continue;
                        float shade = 1f - (float)edgeDist / (w / 2) * 0.3f;
                        tex.SetPixel(px, py, Color.Lerp(mesaColor, mesaDark, 1f - shade));
                    }
                }
            }

            tex.Apply();
            SaveSprite("Background/mesas", tex);
        }

        private static void GenerateSaloonFacades()
        {
            int width = 512;
            int height = 192;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            Color woodColor = new Color(0.5f, 0.38f, 0.28f);
            Color windowColor = new Color(0.25f, 0.35f, 0.45f, 0.8f);
            Color doorColor = new Color(0.35f, 0.28f, 0.2f);
            Color roofColor = new Color(0.4f, 0.32f, 0.25f);

            int[] buildingX = { 40, 160, 280, 400 };
            int[] buildingW = { 90, 100, 85, 95 };
            int[] buildingH = { 120, 140, 110, 130 };

            for (int b = 0; b < 4; b++)
            {
                int bx = buildingX[b];
                int bw = buildingW[b];
                int bh = buildingH[b];

                DrawFilledRect(tex, bx - bw / 2, 0, bw, bh, woodColor);
                
                DrawFilledRect(tex, bx - bw / 2, bh, bw, 12, roofColor);
                for (int rx = bx - bw / 2; rx < bx + bw / 2; rx += 6)
                {
                    DrawFilledRect(tex, rx, bh + 10, 4, 6, roofColor);
                }

                int doorW = 16;
                int doorH = 32;
                DrawFilledRect(tex, bx - doorW / 2, 0, doorW, doorH, doorColor);
                DrawLine(tex, bx, 0, bx, doorH - 2, Color.Lerp(doorColor, Color.black, 0.3f));

                int windowY = bh / 2 + 10;
                DrawFilledRect(tex, bx - bw / 3, windowY, 14, 18, windowColor);
                DrawFilledRect(tex, bx + bw / 3 - 14, windowY, 14, 18, windowColor);
                
                for (int i = 0; i < 3; i++)
                {
                    int y = bh - 15 + i * 4;
                    DrawLine(tex, bx - bw / 2, y, bx + bw / 2, y, Color.Lerp(woodColor, Color.black, 0.15f));
                }
            }

            tex.Apply();
            SaveSprite("Background/saloon_facades", tex);
        }

        private static void GenerateGround()
        {
            int width = 512;
            int height = 96;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color groundColor = new Color(0.65f, 0.5f, 0.35f);
            Color groundDark = new Color(0.5f, 0.38f, 0.28f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.1f) * 0.3f;
                    Color c = Color.Lerp(groundColor, groundDark, noise);
                    tex.SetPixel(x, y, c);
                }
            }

            for (int i = 0; i < 50; i++)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                int r = Random.Range(1, 3);
                Color pebble = Color.Lerp(groundDark, new Color(0.4f, 0.35f, 0.3f), Random.value);
                DrawFilledCircle(tex, x, y, r, pebble);
            }

            tex.Apply();
            SaveSprite("Background/ground", tex);
        }
        #endregion

        #region UI
        private static void GenerateUISprites()
        {
            var hpBarBg = new Texture2D(64, 12, TextureFormat.RGBA32, false);
            hpBarBg.filterMode = FilterMode.Point;
            ClearTexture(hpBarBg);
            DrawFilledRect(hpBarBg, 0, 0, 64, 12, new Color(0.2f, 0.15f, 0.1f));
            DrawFilledRect(hpBarBg, 1, 1, 62, 10, new Color(0.15f, 0.1f, 0.08f));
            hpBarBg.Apply();
            SaveSprite("UI/hp_bar_bg", hpBarBg);

            var hpBarFill = new Texture2D(60, 8, TextureFormat.RGBA32, false);
            hpBarFill.filterMode = FilterMode.Point;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 60; x++)
                {
                    float t = (float)y / 8;
                    Color c = Color.Lerp(new Color(0.2f, 0.7f, 0.3f), new Color(0.4f, 0.9f, 0.5f), t);
                    hpBarFill.SetPixel(x, y, c);
                }
            }
            hpBarFill.Apply();
            SaveSprite("UI/hp_bar_fill", hpBarFill);

            var ammoCell = new Texture2D(8, 16, TextureFormat.RGBA32, false);
            ammoCell.filterMode = FilterMode.Point;
            ClearTexture(ammoCell);
            DrawFilledRect(ammoCell, 0, 0, 8, 16, new Color(0.8f, 0.7f, 0.3f));
            DrawFilledRect(ammoCell, 1, 1, 6, 14, new Color(0.9f, 0.8f, 0.4f));
            ammoCell.Apply();
            SaveSprite("UI/ammo_cell", ammoCell);

            var ammoCellEmpty = new Texture2D(8, 16, TextureFormat.RGBA32, false);
            ammoCellEmpty.filterMode = FilterMode.Point;
            ClearTexture(ammoCellEmpty);
            DrawFilledRect(ammoCellEmpty, 0, 0, 8, 16, new Color(0.3f, 0.25f, 0.2f));
            DrawFilledRect(ammoCellEmpty, 1, 1, 6, 14, new Color(0.2f, 0.18f, 0.15f));
            ammoCellEmpty.Apply();
            SaveSprite("UI/ammo_cell_empty", ammoCellEmpty);

            var buttonBg = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            buttonBg.filterMode = FilterMode.Point;
            ClearTexture(buttonBg);
            DrawFilledRect(buttonBg, 0, 0, 64, 32, new Color(0.5f, 0.38f, 0.25f));
            DrawFilledRect(buttonBg, 2, 2, 60, 28, new Color(0.6f, 0.45f, 0.3f));
            DrawLine(buttonBg, 2, 28, 62, 28, new Color(0.7f, 0.55f, 0.4f));
            DrawLine(buttonBg, 2, 2, 2, 28, new Color(0.7f, 0.55f, 0.4f));
            buttonBg.Apply();
            SaveSprite("UI/button_bg", buttonBg);

            var joystickBg = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            joystickBg.filterMode = FilterMode.Point;
            ClearTexture(joystickBg);
            DrawFilledCircle(joystickBg, 48, 48, 44, new Color(0.2f, 0.18f, 0.15f, 0.6f));
            DrawFilledCircle(joystickBg, 48, 48, 40, new Color(0.25f, 0.22f, 0.18f, 0.5f));
            joystickBg.Apply();
            SaveSprite("UI/joystick_bg", joystickBg);

            var joystickKnob = new Texture2D(48, 48, TextureFormat.RGBA32, false);
            joystickKnob.filterMode = FilterMode.Point;
            ClearTexture(joystickKnob);
            DrawFilledCircle(joystickKnob, 24, 24, 20, new Color(0.5f, 0.45f, 0.35f, 0.8f));
            DrawFilledCircle(joystickKnob, 24, 24, 16, new Color(0.6f, 0.55f, 0.45f, 0.9f));
            joystickKnob.Apply();
            SaveSprite("UI/joystick_knob", joystickKnob);
        }
        #endregion

        #region Drawing Helpers
        private static void ClearTexture(Texture2D tex)
        {
            Color[] clear = new Color[tex.width * tex.height];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
        }

        private static void DrawPixel(Texture2D tex, int x, int y, Color c)
        {
            if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                tex.SetPixel(x, y, c);
        }

        private static void DrawFilledRect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            for (int py = y; py < y + h; py++)
            {
                for (int px = x; px < x + w; px++)
                {
                    DrawPixel(tex, px, py, c);
                }
            }
        }

        private static void DrawFilledCircle(Texture2D tex, int cx, int cy, int r, Color c)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    {
                        DrawPixel(tex, x, y, c);
                    }
                }
            }
        }

        private static void DrawFilledEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color c)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    float dx = (float)(x - cx) / rx;
                    float dy = (float)(y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        DrawPixel(tex, x, y, c);
                    }
                }
            }
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawPixel(tex, x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }
        #endregion

        #region Save Helpers
        private static void SaveSprite(string relativePath, Texture2D tex, FilterMode filterMode = FilterMode.Point)
        {
            string fullPath = $"{OUTPUT_PATH}/{relativePath}.png";
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            byte[] pngData = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngData);

            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
            
            var importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = PPU;
                importer.filterMode = filterMode;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
        #endregion
    }
}

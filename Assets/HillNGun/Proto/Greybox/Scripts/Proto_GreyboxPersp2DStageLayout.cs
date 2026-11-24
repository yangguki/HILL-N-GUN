using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>3840×2160 reference px → world (16:9 ortho). Origin top-left of screen.</summary>
    public static class Proto_GreyboxPersp2DStageLayout
    {
        /// <summary>Concept front row: feet at 1764/2160*100 (2x art), engine scale ~1.52.</summary>
        public const float HeroStartFootPercent = 81.7f;

        public const float RefW = Proto_GreyboxPersp2DMath.DesignWidth2x;
        public const float RefH = Proto_GreyboxPersp2DMath.DesignHeight2x;
        public const float SkyGradientBottomY2x = 268f;
        public const float BackdropTopLeftX2x = 0f;
        public const float BackdropTopLeftY2x = 268f;
        public const float BackdropWidthScale = 1.875f;
        public const float RoadTopLeftX2x = 0f;
        public const float RoadTopLeftY2x = 907f;

        public static float WorldUnitsPerRefPx => Proto_GreyboxLayout.WorldHeight / RefH;

        public static Vector3 RefPivotToWorld(float refX, float refY)
        {
            float xPct = refX / RefW * 100f;
            float yPct = refY / RefH * 100f;
            return new Vector3(
                Proto_GreyboxLayout.ScreenXPercentToWorldX(xPct),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(yPct),
                0f);
        }

        public static float FootPercentToFeetY2x(float footPercentFromTop) =>
            footPercentFromTop / 100f * RefH;

        public static float WorldFootYToFeetY2x(float footWorldY)
        {
            float pct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(footWorldY);
            return FootPercentToFeetY2x(pct);
        }

        public static float WorldFootXToRefX2x(float footWorldX)
        {
            float xPct = (footWorldX / Proto_GreyboxLayout.WorldWidth + 0.5f) * 100f;
            return xPct / 100f * RefW;
        }

        /// <summary>Standing hero 376px (2x art) at engine scale 1 → ~26% row at feet 1764.</summary>
        public const float StandingHeroPx = 376f;

        public static float CharacterUniformScaleAtFeetY2x(float feetY2x)
        {
            float e = Proto_GreyboxPersp2DMath.EngineScaleAtFeetY2x(feetY2x);
            float desiredWorldH = Proto_GreyboxLayout.WorldHeight * (StandingHeroPx * e / RefH);
            float nativeH = Proto_GreyboxPersp2DCatalog.CharacterCellPx / Proto_GreyboxPersp2DCatalog.ImportPpu;
            return desiredWorldH / Mathf.Max(nativeH, 0.001f);
        }

        public static float CharacterUniformScaleAtFootWorld(float footWorldY) =>
            CharacterUniformScaleAtFeetY2x(WorldFootYToFeetY2x(footWorldY));

        public static float PropUniformScaleAtFeetY2x(float feetY2x, Sprite prop)
        {
            if (prop == null) return CharacterUniformScaleAtFeetY2x(feetY2x);
            float e = Proto_GreyboxPersp2DMath.PropEngineScaleAtFeetY2x(feetY2x);
            float desiredWorldH = Proto_GreyboxLayout.WorldHeight * (prop.rect.height * e / RefH);
            float nativeH = prop.rect.height / Proto_GreyboxWestPixelCatalog.ImportPpu;
            return desiredWorldH / Mathf.Max(nativeH, 0.001f);
        }

        /// <summary>Screen height fraction for a 512px cell at feet row (tests).</summary>
        public static float HeroScreenHeightFractionAtFeetY2x(float feetY2x)
        {
            float scale = CharacterUniformScaleAtFeetY2x(feetY2x);
            float worldH = Proto_GreyboxPersp2DCatalog.CharacterCellPx / Proto_GreyboxPersp2DCatalog.ImportPpu * scale;
            return worldH / Proto_GreyboxLayout.WorldHeight;
        }

        public static Vector2 RefPivotFromTopLeft(float topLeftX, float topLeftY, Sprite sprite, float nativeW,
            float nativeH)
        {
            if (sprite == null) return new Vector2(topLeftX, topLeftY);
            float nx = sprite.pivot.x / Mathf.Max(sprite.rect.width, 1f);
            float nyBottom = sprite.pivot.y / Mathf.Max(sprite.rect.height, 1f);
            return new Vector2(topLeftX + nativeW * nx, topLeftY + nativeH * (1f - nyBottom));
        }

        public static void RoadRefBounds(float pivotRefX, float pivotRefY, float nativeW, float nativeH,
            Vector2 pivotNorm, out float leftX, out float rightX)
        {
            leftX = pivotRefX - nativeW * pivotNorm.x;
            rightX = leftX + nativeW;
        }

        public static SpriteRenderer PlaceSpritePivot(Transform parent, string goName, Sprite sprite, float ppu,
            float refPivotX, float refPivotY, float uniformScale, int sortOrder, bool flipX = false)
        {
            if (sprite == null || parent == null) return null;
            var go = new GameObject(string.IsNullOrEmpty(goName) ? sprite.name : goName);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortOrder;
            sr.flipX = flipX;
            go.transform.position = RefPivotToWorld(refPivotX, refPivotY);
            go.transform.localScale = Vector3.one * uniformScale;
            return sr;
        }

        public static SpriteRenderer PlaceSpriteTopLeft(Transform parent, string goName, Sprite sprite, float ppu,
            float topLeftX, float topLeftY, float nativeW, float nativeH, float uniformScale, int sortOrder,
            bool flipX = false)
        {
            var piv = RefPivotFromTopLeft(topLeftX, topLeftY, sprite, nativeW, nativeH);
            return PlaceSpritePivot(parent, goName, sprite, ppu, piv.x, piv.y, uniformScale, sortOrder, flipX);
        }

        public static float ScreenPercentYFromRefY2x(float refY2x) => refY2x / RefH * 100f;

        public static float ScreenPercentXFromRefX2x(float refX2x) => refX2x / RefW * 100f;

        public struct BuildingLayout
        {
            public string id;
            public float topLeftX;
            public float topLeftY;
            public float pivotX;
            public float pivotY;
            public float signOffsetX;
            public float signOffsetY;
            public float signScale;
            public float porchEdgeX;
            public float nearPorchZ;
        }

        public static BuildingLayout GetBuilding(string id)
        {
            var cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
            if (cat != null)
            {
                var baked = Proto_GreyboxPersp2DEnvLayout.FindBuilding(cat, id);
                if (baked != null && !string.IsNullOrEmpty(baked.id) && baked.pivotY > 0f)
                {
                    return new BuildingLayout
                    {
                        id = id,
                        pivotX = baked.pivotX,
                        pivotY = baked.pivotY,
                        signOffsetX = baked.signOffsetX,
                        signOffsetY = baked.signOffsetY,
                        signScale = baked.signScale,
                        porchEdgeX = baked.porchEdgeX,
                        nearPorchZ = baked.nearPorchZ
                    };
                }
            }

            return id switch
            {
                "sheriff_R" => new BuildingLayout
                {
                    id = id, pivotX = 2277.8f, pivotY = 1116.5f,
                    signOffsetX = 209.5f, signOffsetY = -221.9f, signScale = 0.5176f,
                    porchEdgeX = 4.1f, nearPorchZ = 29f
                },
                "store_L" => new BuildingLayout
                {
                    id = id, pivotX = 1570.9f, pivotY = 1116.5f,
                    signOffsetX = -255.5f, signOffsetY = -252.3f, signScale = 0.5176f,
                    porchEdgeX = -4.0f, nearPorchZ = 29f
                },
                "hotel_R" => new BuildingLayout
                {
                    id = id, pivotX = 2844.4f, pivotY = 1333.7f,
                    signOffsetX = 525.5f, signOffsetY = -753.1f, signScale = 1.0013f,
                    porchEdgeX = 5.2f, nearPorchZ = 15f
                },
                "saloon_L" => new BuildingLayout
                {
                    id = id, pivotX = 995.6f, pivotY = 1333.7f,
                    signOffsetX = -541.6f, signOffsetY = -701.1f, signScale = 1.0013f,
                    porchEdgeX = -5.2f, nearPorchZ = 15f
                },
                _ => default
            };
        }

        public static Vector2 RefTopLeftFromPivotNorm(float pivotRefX, float pivotRefY, Vector2 pivotNorm,
            float nativeW, float nativeH) => new(
            pivotRefX - pivotNorm.x * nativeW,
            pivotRefY - (1f - pivotNorm.y) * nativeH);

        /// <summary>Match design px width to sprite rect (handles maxTextureSize downscale).</summary>
        public static float UniformScaleNative(Sprite sprite, float ppu, float nativeW, float extraScale = 1f)
        {
            if (sprite == null || nativeW <= 0f) return 1f;
            float texW = sprite.rect.width / Mathf.Max(ppu, 0.01f);
            return nativeW * extraScale * WorldUnitsPerRefPx / Mathf.Max(texW, 0.0001f);
        }
    }
}
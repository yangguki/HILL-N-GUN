using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Slice K: screen-space perspective scale + porch depth test (2x design pixels).</summary>
    public static class Proto_GreyboxPersp2DMath
    {
        public const float DesignHeight2x = 2160f;
        public const float DesignWidth2x = 3840f;
        public const float HorizonY2x = 907f;
        public const float VanishingX2x = 1920f;
        public const float ScaleDenom = 564f;
        public const float PropScaleDenom = 846f;
        public const float MinEngineScale = 0.2f;
        public const float FocalPx = 2400f;
        public const float CameraHeightM = 2.4f;

        /// <summary>feet_y in 2x px from top; returns engine uniform scale (1.0 near hero row).</summary>
        public static float EngineScaleAtFeetY2x(float feetY2x)
        {
            float raw = (feetY2x - HorizonY2x) / ScaleDenom;
            return Mathf.Max(MinEngineScale, raw);
        }

        /// <summary>World foot y → screen % from top (1080p gameplay) → scale at 1x feet_y = 2 * percent/100 * 1080.</summary>
        public static float EngineScaleAtFootWorldY(float footWorldY)
        {
            float pct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(footWorldY);
            float feet1x = pct / 100f * 1080f;
            float feet2x = feet1x * 2f;
            return EngineScaleAtFeetY2x(feet2x);
        }

        public static float PropEngineScaleAtFeetY2x(float feetY2x)
        {
            float raw = (feetY2x - HorizonY2x) / PropScaleDenom;
            return Mathf.Max(MinEngineScale, raw);
        }

        /// <summary>Hero row feet_y 1764 → ~1.52 (512 cell / 564 denom).</summary>
        public static float EngineScaleAtFootPercent(float footPercentFromTop)
        {
            float feet2x = footPercentFromTop / 100f * DesignHeight2x;
            return EngineScaleAtFeetY2x(feet2x);
        }

        public static float WorldZFromFeetY2x(float feetY2x) =>
            FocalPx * CameraHeightM / Mathf.Max(feetY2x - HorizonY2x, 1f);

        public static float WorldXFromScreenX2x(float screenX2x, float feetY2x)
        {
            float z = WorldZFromFeetY2x(feetY2x);
            return (screenX2x - VanishingX2x) * z / FocalPx;
        }

        /// <summary>
        /// Building front draws over actor when actor is behind porch edge:
        /// |X| ≥ |X_porch_edge| and Z ≥ Z_near_porch (world from feet).
        /// </summary>
        public static bool PorchFrontDrawsOverActor(
            float actorWorldX, float actorWorldZ,
            float porchEdgeX, float nearPorchZ)
        {
            return Mathf.Abs(actorWorldX) >= Mathf.Abs(porchEdgeX) - 0.001f
                   && actorWorldZ >= nearPorchZ - 0.001f;
        }

        public static void ActorWorldFromFootScreen2x(float screenX2x, float feetY2x, out float worldX, out float worldZ)
        {
            worldZ = WorldZFromFeetY2x(feetY2x);
            worldX = WorldXFromScreenX2x(screenX2x, feetY2x);
        }
    }
}
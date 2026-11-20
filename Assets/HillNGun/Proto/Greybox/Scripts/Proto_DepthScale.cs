using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_DepthScale
    {
        public static float FarScale = Proto_GreyboxTuning.DepthFarScale;
        public static float NearScale = Proto_GreyboxTuning.DepthNearScale;

        public static float ScaleAt(float footY)
        {
            float t = Mathf.InverseLerp(
                Proto_GreyboxLayout.EnemyFarFootWorldY,
                Proto_GreyboxLayout.PlayerNearFootWorldY,
                footY);
            return Mathf.Lerp(FarScale, NearScale, t);
        }

        /// <summary>Same depth curve as <see cref="ScaleAt"/> but capped so near-player bullets stay at nominal world diameter.</summary>
        public static float BulletScaleAt(float footY) => Mathf.Min(ScaleAt(footY), Proto_GreyboxTuning.BulletDepthScaleCap);
    }
}
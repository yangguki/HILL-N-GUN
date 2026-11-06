using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>View-only grenade flight / explode presentation (12+: no blood/gore).</summary>
    [CreateAssetMenu(fileName = "GrenadeVfxData", menuName = "HillNGun/Data/Grenade VFX")]
    public class GrenadeVfxData : ScriptableObject
    {
        [Header("Flight")]
        [Tooltip("Bomb sprite world size (logic tiles at mid-field)")]
        public float bombSize = 0.55f;
        [Tooltip("Spin while flying (deg/sec)")]
        public float spinDegPerSec = 540f;
        [Tooltip("Smoke trail length in tiles")]
        public float trailLength = 1.1f;
        [Tooltip("Smoke trail width in tiles")]
        public float trailWidth = 0.22f;
        public Color trailColor = new Color(0.85f, 0.82f, 0.75f, 0.65f);
        public Color bombTint = new Color(1f, 0.95f, 0.85f, 1f);

        [Header("Cartoon explode (no gore)")]
        public float explodeDuration = 0.55f;
        public int starCount = 8;
        public int sparkCount = 12;
        public int smokePuffs = 5;
        public Color flameColor = new Color(1f, 0.55f, 0.15f, 1f);
        public Color sparkColor = new Color(1f, 0.95f, 0.45f, 1f);
        public Color starColor = new Color(1f, 1f, 0.85f, 1f);
        public Color smokeColor = new Color(0.7f, 0.68f, 0.62f, 0.7f);
        public float ringLife = 0.45f;

        private static GrenadeVfxData _builtin;
        public static GrenadeVfxData Builtin
        {
            get
            {
                if (_builtin == null)
                {
                    _builtin = CreateInstance<GrenadeVfxData>();
                    _builtin.hideFlags = HideFlags.DontSave;
                    _builtin.name = "grenade_vfx_builtin";
                }
                return _builtin;
            }
        }
    }
}

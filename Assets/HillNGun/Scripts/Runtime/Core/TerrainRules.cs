using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Core
{
    /// <summary>Access to the P0 terrain rule SO (Resources/Terrain/terrain_rules). Defaults when missing.</summary>
    public static class TerrainRules
    {
        private static TerrainRulesData _data;
        private static bool _loaded;

        public static TerrainRulesData Data
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    _data = Resources.Load<TerrainRulesData>(TerrainRulesData.ResourcePath);
                    if (_data == null) _data = ScriptableObject.CreateInstance<TerrainRulesData>();
                }
                return _data;
            }
        }

        public static void SetDataForTests(TerrainRulesData d) { _data = d; _loaded = d != null; }

        /// <summary>Debug view of safe widths / cover classes (F4, pause menu, -cover-debug).</summary>
        public static bool CoverDebug { get; set; }
    }
}

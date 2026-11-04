using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>All stage themes plus which one PvP and the menus use. Stages pick theirs via StageData.themeId.</summary>
    [CreateAssetMenu(fileName = "theme_library", menuName = "HillNGun/Stage Theme Library")]
    public class StageThemeLibrary : ScriptableObject
    {
        public StageThemeData[] themes = new StageThemeData[0];
        public string pvpThemeId = "town_street";
        public string menuThemeId = "frontier_dusk";
        public string defaultThemeId = "town_street";
        [Tooltip("Background bake resolution cap (longest side, px). Lower = less memory on low-end phones.")]
        public int maxBakeSize = 2560;

        public StageThemeData Get(string id)
        {
            if (themes == null) return null;
            if (!string.IsNullOrEmpty(id))
                foreach (var t in themes) if (t != null && t.id == id) return t;
            foreach (var t in themes) if (t != null && t.id == defaultThemeId) return t;
            return themes.Length > 0 ? themes[0] : null;
        }
    }
}

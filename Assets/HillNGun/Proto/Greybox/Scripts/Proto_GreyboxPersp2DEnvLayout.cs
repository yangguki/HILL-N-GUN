using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Baked env layout from hng_env_layout_2d.json (filled at editor rebuild).</summary>
    public static class Proto_GreyboxPersp2DEnvLayout
    {
        public static Proto_GreyboxPersp2DCatalog.BakedBuilding FindBuilding(Proto_GreyboxPersp2DCatalog cat, string id)
        {
            if (cat?.bakedBuildings == null || string.IsNullOrEmpty(id)) return null;
            foreach (var b in cat.bakedBuildings)
                if (b != null && b.id == id) return b;
            return null;
        }

        public static Proto_GreyboxPersp2DCatalog.BakedPropDef FindPropDef(Proto_GreyboxPersp2DCatalog cat, string name)
        {
            if (cat?.propDefs == null || string.IsNullOrEmpty(name)) return null;
            foreach (var p in cat.propDefs)
                if (p != null && p.name == name) return p;
            return null;
        }

        public static Sprite SpriteForProp(Proto_GreyboxPersp2DCatalog cat, string name)
        {
            var def = FindPropDef(cat, name);
            return def.sprite;
        }
    }
}
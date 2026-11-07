using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using HillNGun.Data;

namespace HillNGun.Editor
{
    /// <summary>
    /// Perf pass (2026-11-07): one Sprite Atlas (V2) for the painted cover / prop sprites referenced by SpriteLibrary.covers,
    /// so same-row covers (same sorting order) draw in one dynamic batch instead of one draw per texture.
    /// Budget: max 1024x1024 single page (well inside the BG/Props 2x2048 allowance); sprites keep their own rect / pivot /
    /// pixels-per-unit, so drawn size, hit geometry and style are unchanged. Characters (runtime-recoloured skins) are not packed.
    /// Run: -executeMethod HillNGun.Editor.PerfAtlasSetup.EnsureCoverAtlas
    /// </summary>
    public static class PerfAtlasSetup
    {
        public const string AtlasPath = "Assets/HillNGun/Art/Atlases/props_covers.spriteatlasv2";

        [MenuItem("HillNGun/Perf/Ensure Cover Atlas")]
        public static void EnsureCoverAtlas()
        {
            var lib = AssetDatabase.LoadAssetAtPath<SpriteLibrary>("Assets/HillNGun/Data/SpriteLibrary.asset");
            var sprites = new List<Object>();
            var seen = new HashSet<string>();
            if (lib != null && lib.covers != null)
                foreach (var c in lib.covers)
                {
                    if (c == null) continue;
                    foreach (var s in new[] { c.intact, c.cracked, c.damaged, c.destroyed })
                    {
                        if (s == null) continue;
                        string p = AssetDatabase.GetAssetPath(s);
                        if (string.IsNullOrEmpty(p) || !seen.Add(p)) continue;
                        sprites.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(p)); // pack the whole source texture (single-sprite PNGs)
                    }
                }
            System.IO.Directory.CreateDirectory("Assets/HillNGun/Art/Atlases");
            var asset = new SpriteAtlasAsset();
            asset.Add(sprites.ToArray());
            SpriteAtlasAsset.Save(asset, AtlasPath);
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(AtlasPath) as SpriteAtlasImporter;
            if (imp != null)
            {
                imp.includeInBuild = true;
                imp.packingSettings = new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4, blockOffset = 1, enableAlphaDilation = true };
                imp.textureSettings = new SpriteAtlasTextureSettings { readable = false, generateMipMaps = false, sRGB = true, filterMode = FilterMode.Bilinear };
                var plat = imp.GetPlatformSettings("DefaultTexturePlatform");
                plat.maxTextureSize = 1024;
                plat.format = TextureImporterFormat.Automatic;
                plat.textureCompression = TextureImporterCompression.Compressed;
                imp.SetPlatformSettings(plat);
                imp.SaveAndReimport();
            }
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
                EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            AssetDatabase.SaveAssets();
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            Debug.Log($"[PerfAtlas] {AtlasPath}: {sprites.Count} cover textures, packer={EditorSettings.spritePackerMode}, spriteCount={(atlas != null ? atlas.spriteCount : -1)}");
            foreach (var o in sprites) Debug.Log($"[PerfAtlas]   + {AssetDatabase.GetAssetPath(o)}");
        }
    }
}

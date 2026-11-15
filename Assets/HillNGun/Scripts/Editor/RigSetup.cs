using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;
using HillNGun.Rig;

namespace HillNGun.Editor
{
    /// <summary>
    /// v13: imports cutout-rig atlases (Resources/Rig/{id}_rig_atlas.png) as Multiple sprites from the rig JSON and writes
    /// Unity 2D Animation skinning data per part sprite (one bone named after the part, quad mesh, weight 1) so every part
    /// renders through SpriteSkin. Also sets the door sprite import (pivot = designer threshold anchor).
    /// Command line: -executeMethod HillNGun.Editor.RigSetup.ApplyFromCommandLine
    /// </summary>
    public static class RigSetup
    {
        public const string RigDir = "Assets/HillNGun/Resources/Rig/";
        public const string DoorDir = "Assets/HillNGun/Resources/Doors/";

        public static void ApplyFromCommandLine()
        {
            int n = Apply();
            Debug.Log($"[RigSetup] done: {n} rig atlases");
            EditorApplication.Exit(n > 0 ? 0 : 1);
        }

        [MenuItem("HillNGun/Setup/Apply Cutout Rigs + Doors")]
        public static int Apply()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("_rig t:TextAsset", new[] { RigDir.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("_rig.json")) continue;
                var def = JsonUtility.FromJson<RigDefinition>(AssetDatabase.LoadAssetAtPath<TextAsset>(path).text);
                if (def?.directions == null) continue;
                if (ImportAtlas(def)) n++;
            }
            ImportDoors();
            ImportDoorways();
            AssetDatabase.SaveAssets();
            return n;
        }

        private static bool ImportAtlas(RigDefinition def)
        {
            string tex = RigDir + def.atlas;
            var ti = AssetImporter.GetAtPath(tex) as TextureImporter;
            if (ti == null) { Debug.LogError("[RigSetup] missing atlas " + tex); return false; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = RigDefinition.AtlasPPU;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.Compressed;
            var tis = new TextureImporterSettings(); ti.ReadTextureSettings(tis);
            tis.spriteMeshType = SpriteMeshType.FullRect; tis.spriteGenerateFallbackPhysicsShape = false;
            ti.SetTextureSettings(tis);
            ti.SaveAndReimport();

            var factory = new SpriteDataProviderFactories(); factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
            dp.InitSpriteEditorDataProvider();
            var old = dp.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = new List<SpriteRect>();
            foreach (var v in def.directions)
                foreach (var p in v.parts)
                {
                    var r = new SpriteRect
                    {
                        name = p.sprite,
                        rect = new Rect(p.rect[0], p.rect[1], p.rect[2], p.rect[3]),
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(p.pivot[0], p.pivot[1]),
                        border = Vector4.zero,
                    };
                    r.spriteID = old.TryGetValue(p.sprite, out var id) ? id : GUID.Generate();
                    rects.Add(r);
                }
            dp.SetSpriteRects(rects.ToArray());
            var nameIds = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameIds?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            dp.Apply();
            ti.SaveAndReimport();

            // second pass: skinning (bones + mesh) per sprite
            dp = factory.GetSpriteEditorDataProviderFromObject(ti);
            dp.InitSpriteEditorDataProvider();
            var bonesDp = dp.GetDataProvider<ISpriteBoneDataProvider>();
            var meshDp = dp.GetDataProvider<ISpriteMeshDataProvider>();
            if (bonesDp == null || meshDp == null) { Debug.LogError("[RigSetup] 2D Animation skinning providers missing (package not installed?)"); return false; }
            foreach (var r in dp.GetSpriteRects())
            {
                float w = r.rect.width, h = r.rect.height;
                var piv = new Vector2(r.pivot.x * w, r.pivot.y * h); // rect-local px
                string boneName = r.name.Substring(r.name.IndexOf("__") + 2);
                bonesDp.SetBones(r.spriteID, new List<SpriteBone>
                {
                    new SpriteBone { name = boneName, guid = r.spriteID.ToString() + "_b", position = new Vector3(piv.x, piv.y, 0f), rotation = Quaternion.identity, length = Mathf.Max(8f, h * 0.5f), parentId = -1, color = Color.white }
                });
                var bw = new BoneWeight { boneIndex0 = 0, weight0 = 1f };
                meshDp.SetVertices(r.spriteID, new[]
                {
                    new Vertex2DMetaData { position = new Vector2(0f, 0f), boneWeight = bw },
                    new Vertex2DMetaData { position = new Vector2(w, 0f), boneWeight = bw },
                    new Vertex2DMetaData { position = new Vector2(w, h), boneWeight = bw },
                    new Vertex2DMetaData { position = new Vector2(0f, h), boneWeight = bw },
                });
                meshDp.SetIndices(r.spriteID, new[] { 0, 1, 2, 0, 2, 3 });
                meshDp.SetEdges(r.spriteID, new[] { new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 3), new Vector2Int(3, 0) });
            }
            dp.Apply();
            ti.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(tex).OfType<Sprite>().ToArray();
            int boned = sprites.Count(s => s.GetBones().Length == 1 && s.GetBindPoses().Length == 1);
            Debug.Log($"[RigSetup] {def.character}: atlas {tex} sprites={sprites.Length} withBones={boned}");
            return boned == rects.Count;
        }

        public const string DoorwayDir = "Assets/HillNGun/Resources/Doorways";
        /// <summary>v15 doorway kit import: per-door folder = manifest door id; PPU + pivot (floor anchor) from the designer manifest.</summary>
        public static int ImportDoorways()
        {
            if (!AssetDatabase.IsValidFolder(DoorwayDir)) return 0;
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { DoorwayDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                string id = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
                if (id == "bg") { if (ImportDoorwayBackground(path)) n++; continue; }
                var dd = HillNGun.PvE.EntranceDoors.KitDoor(id);
                if (dd == null || dd.pivot_normalized == null || dd.pivot_normalized.Length < 2) { Debug.LogWarning("[RigSetup] doorway: no manifest door for " + path); continue; }
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = dd.scale != null ? dd.scale.import_ppu : 54f;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.maxTextureSize = 512;
                ti.textureCompression = TextureImporterCompression.Uncompressed; // layer seams must register to the pixel
                ti.filterMode = FilterMode.Bilinear;
                var tis = new TextureImporterSettings(); ti.ReadTextureSettings(tis);
                tis.spriteAlignment = (int)SpriteAlignment.Custom;
                tis.spritePivot = new Vector2(dd.pivot_normalized[0], dd.pivot_normalized[1]);
                tis.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(tis);
                ti.SaveAndReimport(); n++;
            }
            Debug.Log($"[RigSetup] doorway kit sprites imported: {n}");
            return n;
        }

        /// <summary>Screen-space 1920x1080 doorway BG: PPU from doorways_engine.json (54 = 1:1 at 1080p), centred, full res.</summary>
        private static bool ImportDoorwayBackground(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) return false;
            var eng = HillNGun.PvE.EntranceDoors.Engine;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = eng != null ? eng.backgroundPpu : 54f;
            ti.mipmapEnabled = false; ti.alphaIsTransparency = false; ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 2048; ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.filterMode = FilterMode.Bilinear;
            var tis = new TextureImporterSettings(); ti.ReadTextureSettings(tis);
            tis.spriteAlignment = (int)SpriteAlignment.Center; tis.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(tis); ti.SaveAndReimport();
            return true;
        }

        private static void ImportDoors()
        {
            foreach (var guid in AssetDatabase.FindAssets("door_ t:Texture2D", new[] { DoorDir.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100f;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.maxTextureSize = 512; // 256x320 source: keep full res (anchor px exact)
                var tis = new TextureImporterSettings(); ti.ReadTextureSettings(tis);
                tis.spriteAlignment = (int)SpriteAlignment.Custom;
                tis.spritePivot = new Vector2(128f / 256f, 1f - 300f / 320f); // designer anchor (128,300) = threshold bottom centre
                tis.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(tis);
                ti.SaveAndReimport();
            }
        }
    }
}

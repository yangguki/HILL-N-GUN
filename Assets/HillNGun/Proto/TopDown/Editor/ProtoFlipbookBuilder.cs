using System.IO;
using UnityEditor;
using UnityEngine;

namespace HillNGun.Proto.TopDown.Editor
{
    /// <summary>Imports stage1 idle/run/shoot frames and builds ProtoFlipbookSet assets.</summary>
    public static class ProtoFlipbookBuilder
    {
        static readonly string[] Chars = { "buster", "raven", "volt", "grunt" };

        [MenuItem("HillNGun/Proto/Refresh Flipbook Sets")]
        public static void Refresh()
        {
            Directory.CreateDirectory(Abs(ProtoAssets.AnimArtDir));
            Directory.CreateDirectory(Abs(ProtoAssets.FlipbookDir));
            SyncFromArtSource();
            AssetDatabase.Refresh();
            ApplyImporterSettings();
            foreach (var id in Chars)
                BuildSet(id);
            WireIntoPixelArt();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProtoFlipbook] refreshed sets under " + ProtoAssets.FlipbookDir);
        }

        public static ProtoFlipbookSet Ensure(string characterId)
        {
            string path = SetPath(characterId);
            var set = AssetDatabase.LoadAssetAtPath<ProtoFlipbookSet>(path);
            return set != null ? set : BuildSet(characterId);
        }

        static void SyncFromArtSource()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            foreach (var id in Chars)
            {
                string srcRoot = Path.Combine(root, "ArtSource", "proto_v3", "v2", "anim", id, "stage1");
                string dstRoot = Abs(ProtoAssets.AnimArtDir + "/" + id + "/stage1");
                if (!Directory.Exists(srcRoot)) continue;
                Directory.CreateDirectory(dstRoot);
                string metaName = id + "_stage1_meta.json";
                string metaSrc = Path.Combine(srcRoot, metaName);
                if (File.Exists(metaSrc))
                    File.Copy(metaSrc, Path.Combine(dstRoot, metaName), true);

                foreach (var facing in new[] { "down", "up", "side" })
                {
                    foreach (var anim in new[] { "idle", "run", "shoot" })
                    {
                        string src = Path.Combine(srcRoot, "frames", facing, anim);
                        if (!Directory.Exists(src)) continue;
                        string dst = Path.Combine(dstRoot, "frames", facing, anim);
                        Directory.CreateDirectory(dst);
                        foreach (var file in Directory.GetFiles(src, "*.png"))
                            File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
                    }
                }
            }
        }

        static void ApplyImporterSettings()
        {
            string animRoot = ProtoAssets.AnimArtDir;
            if (!Directory.Exists(Abs(animRoot))) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { animRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
                if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    dirty = true;
                }
                if (Mathf.Abs(importer.spritePixelsPerUnit - ProtoFlipbookMeta.DefaultPpu) > 0.01f)
                {
                    importer.spritePixelsPerUnit = ProtoFlipbookMeta.DefaultPpu;
                    dirty = true;
                }
                // Custom pivot: feet at (0.5, 0.03125)
                var texSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(texSettings);
                var pivot = new Vector2(ProtoFlipbookMeta.PivotX, ProtoFlipbookMeta.PivotY);
                if (texSettings.spriteAlignment != (int)SpriteAlignment.Custom ||
                    Mathf.Abs(texSettings.spritePivot.x - pivot.x) > 1e-5f ||
                    Mathf.Abs(texSettings.spritePivot.y - pivot.y) > 1e-5f)
                {
                    texSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                    texSettings.spritePivot = pivot;
                    importer.SetTextureSettings(texSettings);
                    dirty = true;
                }
                var platform = importer.GetDefaultPlatformTextureSettings();
                if (platform.textureCompression != TextureImporterCompression.Uncompressed || platform.crunchedCompression)
                {
                    platform.textureCompression = TextureImporterCompression.Uncompressed;
                    platform.crunchedCompression = false;
                    importer.SetPlatformTextureSettings(platform);
                    dirty = true;
                }
                if (dirty) importer.SaveAndReimport();
            }
        }

        static ProtoFlipbookSet BuildSet(string characterId)
        {
            string metaPath = ProtoAssets.AnimArtDir + "/" + characterId + "/stage1/" + characterId + "_stage1_meta.json";
            string absMeta = Abs(metaPath);
            if (!File.Exists(absMeta))
            {
                Debug.LogWarning("[ProtoFlipbook] missing meta " + metaPath);
                return null;
            }

            string json = File.ReadAllText(absMeta);
            if (!ProtoFlipbookMeta.TryParse(json, out var stage))
            {
                Debug.LogWarning("[ProtoFlipbook] failed to parse " + metaPath);
                return null;
            }

            string assetPath = SetPath(characterId);
            var set = AssetDatabase.LoadAssetAtPath<ProtoFlipbookSet>(assetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<ProtoFlipbookSet>();
                AssetDatabase.CreateAsset(set, assetPath);
            }

            set.characterId = characterId;
            set.stage = stage.Stage;
            var clips = new ProtoFlipbookSet.Clip[stage.Anims.Count];
            string stageRoot = ProtoAssets.AnimArtDir + "/" + characterId + "/stage1/";
            for (int i = 0; i < stage.Anims.Count; i++)
            {
                var a = stage.Anims[i];
                var frames = new Sprite[a.Frames != null ? a.Frames.Length : 0];
                for (int f = 0; f < frames.Length; f++)
                    frames[f] = AssetDatabase.LoadAssetAtPath<Sprite>(stageRoot + a.Frames[f]);
                clips[i] = new ProtoFlipbookSet.Clip
                {
                    facing = a.Facing,
                    anim = a.Anim,
                    frames = frames,
                    fps = a.Fps,
                    loop = a.Loop,
                    flipXForLeft = a.FlipXForLeft,
                    pivotPx = a.PivotPx
                };
            }
            set.clips = clips;
            EditorUtility.SetDirty(set);
            return set;
        }

        static void WireIntoPixelArt()
        {
            WireOne(ProtoAssets.PixelArtTopDown);
            WireOne(ProtoAssets.PixelArtFrontline);
        }

        static void WireOne(string artPath)
        {
            var art = AssetDatabase.LoadAssetAtPath<ProtoPixelArt>(artPath);
            if (art == null) return;
            art.busterFlip = Ensure("buster");
            art.ravenFlip = Ensure("raven");
            art.voltFlip = Ensure("volt");
            art.gruntFlip = Ensure("grunt");
            EditorUtility.SetDirty(art);
        }

        static string SetPath(string characterId) =>
            ProtoAssets.FlipbookDir + "/ProtoFlipbook_" + characterId + "_stage1.asset";

        static string Abs(string projectRelative) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelative));
    }
}

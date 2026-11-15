using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Proto.TopDown.Editor
{
    public static class ProtoTopDownEditor
    {
        const string CaptureArg = "-proto-topdown-capture";
        const string SetupArg = "-proto-topdown-setup";

        [InitializeOnLoadMethod]
        static void Boot()
        {
            bool capture = Has(CaptureArg);
            bool setup = Has(SetupArg) || capture;
            if (!setup) return;
            EditorApplication.update += WatchPlay;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string done = Path.Combine(ProtoAssets.OutputDir, "capture.done");
            if (File.Exists(done))
            {
                EditorApplication.Exit(0);
                return;
            }
            EditorApplication.update += SetupOnce;
        }

        static void SetupOnce()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= SetupOnce;
            try
            {
                Setup();
                if (Has(CaptureArg))
                {
                    EditorSceneManager.OpenScene(ProtoAssets.Scene);
                    EditorApplication.EnterPlaymode();
                }
                else EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void WatchPlay()
        {
            if (!EditorApplication.isPlaying)
            {
                if (File.Exists(Path.Combine(ProtoAssets.OutputDir, "capture.done")))
                    EditorApplication.Exit(0);
                return;
            }
            string dir = ProtoAssets.OutputDir;
            string req = Path.Combine(dir, "perf_request.txt");
            if (File.Exists(req))
            {
                string skin = File.ReadAllText(req).Trim();
                File.Delete(req);
                string line = $"{skin} drawCalls={UnityStats.drawCalls} batches={UnityStats.batches} tris={UnityStats.triangles} setPass={UnityStats.setPassCalls}";
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "perf.txt"), line + "\n");
                Debug.Log("[ProtoTopDown] PERF " + line);
            }
            if (ProtoTopDownMatch.CaptureFinished) EditorApplication.isPlaying = false;
        }

        [MenuItem("HillNGun/Proto/Build TopDown Scene")]
        public static void Setup()
        {
            SliceSheet();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("ProtoTopDown");
            var match = go.AddComponent<ProtoTopDownMatch>();
            match.Buster = Load<CharacterData>(ProtoAssets.Buster);
            match.Pistol = Load<EnemyData>(ProtoAssets.Enemy);
            match.Rock = Load<CoverData>(ProtoAssets.Rock);
            match.Hay = Load<CoverData>(ProtoAssets.Hay);
            match.Crate = Load<CoverData>(ProtoAssets.Crate);
            match.Barrel = Load<CoverData>(ProtoAssets.Barrel);
            match.Rules = Load<MatchRulesData>(ProtoAssets.Rules);
            match.MoveFeel = Load<MovementFeelData>(ProtoAssets.FeelMove);
            match.Feel = Load<FeelData>(ProtoAssets.Feel);
            match.Downed = Load<DownedData>(ProtoAssets.Downed);
            match.SpriteDown = Load<Sprite>(ProtoAssets.SpriteDown);
            match.SpriteUp = Load<Sprite>(ProtoAssets.SpriteUp);
            match.SpriteSide = Load<Sprite>(ProtoAssets.SpriteSide);
            match.PixelArt = ProtoPixelArtSetup.EnsureTopDown();
            if (match.Buster == null || match.Pistol == null || match.Rules == null || match.SpriteDown == null)
                throw new InvalidOperationException("[ProtoTopDown] required assets failed to load");
            EditorSceneManager.SaveScene(scene, ProtoAssets.Scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProtoTopDown] scene saved " + ProtoAssets.Scene);
        }

        static void SliceSheet()
        {
            string project = Path.GetDirectoryName(Application.dataPath);
            string src = Path.Combine(project, ProtoAssets.SheetSource);
            if (!File.Exists(src)) throw new FileNotFoundException(src);
            var bytes = File.ReadAllBytes(src);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) throw new InvalidOperationException("sheet decode failed");
            var bg = tex.GetPixel(1, 1);
            var keyed = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            var pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                float dr = pixels[i].r - bg.r * 255f;
                float dg = pixels[i].g - bg.g * 255f;
                float db = pixels[i].b - bg.b * 255f;
                float dist = Mathf.Sqrt(dr * dr + dg * dg + db * db);
                pixels[i].a = dist < 42f ? (byte)0 : (byte)255;
            }
            keyed.SetPixels32(pixels);
            keyed.Apply();
            string dir = Path.Combine(project, ProtoAssets.ArtDir);
            Directory.CreateDirectory(dir);
            int third = keyed.width / 3;
            WriteSprite(keyed, 0, third, ProtoAssets.SpriteDown);
            WriteSprite(keyed, third, third * 2, ProtoAssets.SpriteUp);
            WriteSprite(keyed, third * 2, keyed.width, ProtoAssets.SpriteSide);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(keyed);
            AssetDatabase.Refresh();
        }

        static void WriteSprite(Texture2D src, int x0, int x1, string assetPath)
        {
            int minX = x1, minY = src.height, maxX = x0, maxY = -1;
            for (int y = 0; y < src.height; y++)
            for (int x = x0; x < x1; x++)
            {
                if (src.GetPixel(x, y).a < 0.5f) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            if (maxY < minY) throw new InvalidOperationException("empty sprite column " + assetPath);
            const int pad = 2;
            minX = Mathf.Max(x0, minX - pad);
            maxX = Mathf.Min(x1 - 1, maxX + pad);
            minY = Mathf.Max(0, minY - pad);
            maxY = Mathf.Min(src.height - 1, maxY + pad);
            int w = maxX - minX + 1;
            int h = maxY - minY + 1;
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            dst.SetPixels(src.GetPixels(minX, minY, w, h));
            dst.Apply();
            string abs = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
            File.WriteAllBytes(abs, dst.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(dst);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.spritePixelsPerUnit = 100;
            var platform = importer.GetDefaultPlatformTextureSettings();
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            platform.crunchedCompression = false;
            importer.SetPlatformTextureSettings(platform);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);

        static bool Has(string arg)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == arg) return true;
            return false;
        }
    }
}

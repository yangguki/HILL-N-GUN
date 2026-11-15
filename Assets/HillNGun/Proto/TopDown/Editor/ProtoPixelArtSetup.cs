using System.IO;
using UnityEditor;
using UnityEngine;

namespace HillNGun.Proto.TopDown.Editor
{
    /// <summary>Builds ProtoPixelArt assets from Assets/HillNGun/Proto/Art PNGs and wires match scenes.</summary>
    public static class ProtoPixelArtSetup
    {
        const string Art = ProtoAssets.ProtoArtDir;
        const string ArtV2 = Art + "/v2";

        [MenuItem("HillNGun/Proto/Refresh Pixel Art Assets")]
        public static void Refresh()
        {
            Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(Application.dataPath), Art));
            EnsureSpriteImportSettings();
            var top = BuildTopDown();
            var front = BuildFrontline();
            WireScenes(top, front);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProtoPixelArt] refreshed " + ProtoAssets.PixelArtTopDown + " and " + ProtoAssets.PixelArtFrontline);
        }

        public static ProtoPixelArt EnsureTopDown() =>
            AssetDatabase.LoadAssetAtPath<ProtoPixelArt>(ProtoAssets.PixelArtTopDown) ?? BuildTopDown();

        public static ProtoPixelArt EnsureFrontline() =>
            AssetDatabase.LoadAssetAtPath<ProtoPixelArt>(ProtoAssets.PixelArtFrontline) ?? BuildFrontline();

        static ProtoPixelArt BuildTopDown()
        {
            var art = LoadOrCreate(ProtoAssets.PixelArtTopDown);
            art.background = LoadSprite(ArtV2 + "/bg_arena_v2_1920.png");
            art.dirtUvMin = Vector2.zero;
            art.dirtUvMax = Vector2.zero;
            AssignSharedProps(art);
            // TopDown has no midline fence; keep vertical refs for reuse.
            art.fenceDivider = LoadSprite(Art + "/prop_fence_divider_v2.png");
            art.fenceCap = LoadSprite(Art + "/prop_fence_cap.png");
            art.fenceDividerH = LoadSprite(ArtV2 + "/prop_fence_h_tile_v2.png");
            art.fenceCapH = LoadSprite(ArtV2 + "/prop_fence_h_cap_v2.png");
            AssignPoses(art);
            EditorUtility.SetDirty(art);
            return art;
        }

        static ProtoPixelArt BuildFrontline()
        {
            var art = LoadOrCreate(ProtoAssets.PixelArtFrontline);
            art.background = LoadSprite(ArtV2 + "/bg_frontline_v2_1920.png");
            // Horizontal dirt band on v2 frontline plate (buildings top+bottom). Swap file + UV for v2.1.
            art.dirtUvMin = new Vector2(0.02f, 0.30f);
            art.dirtUvMax = new Vector2(0.98f, 0.70f);
            AssignSharedProps(art);
            art.fenceDivider = LoadSprite(Art + "/prop_fence_divider_v2.png");
            art.fenceCap = LoadSprite(Art + "/prop_fence_cap.png");
            art.fenceDividerH = LoadSprite(ArtV2 + "/prop_fence_h_tile_v2.png");
            art.fenceCapH = LoadSprite(ArtV2 + "/prop_fence_h_cap_v2.png");
            AssignPoses(art);
            EditorUtility.SetDirty(art);
            return art;
        }

        static void AssignSharedProps(ProtoPixelArt art)
        {
            art.brick = LoadSprite(ArtV2 + "/prop_wall_h_v2.png") ?? LoadSprite(Art + "/prop_brick_wall.png");
            art.brickVertical = LoadSprite(Art + "/prop_brick_wall_v.png");
            art.hay = LoadSprite(ArtV2 + "/prop_hay_v2_s0.png") ?? LoadSprite(ArtV2 + "/prop_hay_v2.png");
            art.hayDamaged = LoadSprite(ArtV2 + "/prop_hay_v2_s1.png");
            art.hayDestroyed = LoadSprite(ArtV2 + "/prop_hay_v2_s2.png");
            art.sandbag = LoadSprite(ArtV2 + "/prop_sandbags_v2.png") ?? LoadSprite(Art + "/prop_sandbags.png");
            art.crate = LoadSprite(ArtV2 + "/prop_crate_v2_s0.png") ?? LoadSprite(ArtV2 + "/prop_crate_v2.png");
            art.crateDamaged = LoadSprite(ArtV2 + "/prop_crate_v2_s1.png");
            art.crateDestroyed = LoadSprite(ArtV2 + "/prop_crate_v2_s2.png");
            art.barrel = LoadSprite(ArtV2 + "/prop_barrel_v2.png") ?? LoadSprite(Art + "/prop_barrel.png");
        }

        static void AssignPoses(ProtoPixelArt art)
        {
            art.buster = Pose("buster");
            art.raven = Pose("raven");
            art.volt = Pose("volt");
            art.grunt = Pose("grunt");
        }

        static ProtoPixelArt LoadOrCreate(string assetPath)
        {
            var art = AssetDatabase.LoadAssetAtPath<ProtoPixelArt>(assetPath);
            if (art != null) return art;
            art = ScriptableObject.CreateInstance<ProtoPixelArt>();
            AssetDatabase.CreateAsset(art, assetPath);
            return art;
        }

        static ProtoPixelArt.PoseSet Pose(string hero) => new ProtoPixelArt.PoseSet
        {
            front = LoadSprite(Art + "/" + hero + "_front.png"),
            back = LoadSprite(Art + "/" + hero + "_back.png"),
            side = LoadSprite(Art + "/" + hero + "_side.png")
        };

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static void EnsureSpriteImportSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Art }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool bg = path.Contains("/bg_");
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
                if (importer.filterMode != (bg ? FilterMode.Bilinear : FilterMode.Point))
                {
                    importer.filterMode = bg ? FilterMode.Bilinear : FilterMode.Point;
                    dirty = true;
                }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    dirty = true;
                }
                var platform = importer.GetDefaultPlatformTextureSettings();
                if (platform.textureCompression != TextureImporterCompression.Uncompressed || platform.crunchedCompression)
                {
                    platform.textureCompression = TextureImporterCompression.Uncompressed;
                    platform.crunchedCompression = false;
                    if (bg) platform.maxTextureSize = 2048;
                    importer.SetPlatformTextureSettings(platform);
                    dirty = true;
                }
                if (dirty) importer.SaveAndReimport();
            }
        }

        static void WireScenes(ProtoPixelArt top, ProtoPixelArt front)
        {
            WireMatch<ProtoTopDownMatch>(ProtoAssets.Scene, m => m.PixelArt = top);
            WireMatch<HillNGun.Proto.Frontline.FrontlineMatch>(ProtoAssets.FrontlineScene, m =>
            {
                m.PixelArt = front;
                m.SplitAxis = HillNGun.Proto.Frontline.FrontlineSplitAxis.TopBottom;
            });
        }

        static void WireMatch<T>(string scenePath, System.Action<T> assign) where T : Component
        {
            string abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", scenePath));
            if (!File.Exists(abs)) return;
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var match = Object.FindFirstObjectByType<T>();
            if (match == null) return;
            assign(match);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
    }
}

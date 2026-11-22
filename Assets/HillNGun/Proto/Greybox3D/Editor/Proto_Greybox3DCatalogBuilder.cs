using System.IO;
using UnityEditor;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D.Editor
{
    public static class Proto_Greybox3DCatalogBuilder
    {
        const string CatalogAsset = "Assets/HillNGun/Proto/Greybox3D/Resources/HillNGunGreybox3D/Catalog.asset";
        const string CowboyPath =
            "Assets/HillNGun/Proto/Greybox3D/ThirdParty/Quaternius/UltimateAnimatedCharacterPack/Cowboy_Male.fbx";

        [MenuItem("HillNGun/Proto/Greybox3D/Rebuild Catalog")]
        public static void Build()
        {
            ConfigureTextures();
            ConfigureCowboyImport();
            AssetDatabase.Refresh();

            var dir = Path.GetDirectoryName(CatalogAsset);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var catalog = AssetDatabase.LoadAssetAtPath<Proto_Greybox3DCatalog>(CatalogAsset);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<Proto_Greybox3DCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAsset);
            }

            catalog.CowboyMale = LoadGo(CowboyPath);
            AssignCowboyClips(catalog);

            catalog.CactusShort = LoadGo(Nature("cactus_short.fbx"));
            catalog.CactusTall = LoadGo(Nature("cactus_tall.fbx"));
            catalog.Grass = LoadGo(Nature("grass.fbx"));
            catalog.GrassLarge = LoadGo(Nature("grass_large.fbx"));
            catalog.GrassLeafs = LoadGo(Nature("grass_leafs.fbx"));
            catalog.RockSmallA = LoadGo(Nature("rock_smallA.fbx"));
            catalog.RockSmallB = LoadGo(Nature("rock_smallB.fbx"));
            catalog.RockSmallC = LoadGo(Nature("rock_smallC.fbx"));
            catalog.RockTallA = LoadGo(Nature("rock_tallA.fbx"));
            catalog.RockTallB = LoadGo(Nature("rock_tallB.fbx"));
            catalog.RockLargeA = LoadGo(Nature("rock_largeA.fbx"));
            catalog.CliffBlock = LoadGo(Nature("cliff_block_rock.fbx"));
            catalog.CliffLarge = LoadGo(Nature("cliff_large_rock.fbx"));
            catalog.CliffTop = LoadGo(Nature("cliff_top_rock.fbx"));

            catalog.Barrel = LoadGo(Survival("barrel.fbx"));
            catalog.Crate = LoadGo(Survival("box.fbx"));
            catalog.CrateLarge = LoadGo(Survival("box-large.fbx"));
            catalog.Fence = LoadGo(Survival("fence.fbx"));
            catalog.FenceDoorway = LoadGo(Survival("fence-doorway.fbx"));
            catalog.PatchGrass = LoadGo(Survival("patch-grass.fbx"));
            catalog.RockSandA = LoadGo(Survival("rock-sand-a.fbx"));
            catalog.RockSandB = LoadGo(Survival("rock-sand-b.fbx"));

            catalog.WesternObjects = LoadGo(
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/LowPolyWesternObjects.fbx");
            catalog.WoodTex = LoadTex(
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/Wood-Texture.png");
            catalog.DirtTex = LoadTex(
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/DirtSandStone-Texture.png");
            catalog.FabricTex = LoadTex(
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/Fabric_Texture.png");
            catalog.SkyboxDay = LoadTex(
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/Kenney/Skyboxes/skybox-day.png");

            var prefab = EnsureCowboyPrefab(catalog.CowboyMale);
            if (prefab != null)
                catalog.CowboyMale = prefab;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Proto_Greybox3DCatalog.Inject(catalog);
            Debug.Log(
                $"Greybox3D catalog rebuilt. cowboy={(catalog.CowboyMale != null)} cowboyName={(catalog.CowboyMale != null ? catalog.CowboyMale.name : "null")} idle={(catalog.CowboyIdle != null)} walk={(catalog.CowboyWalk != null)} shoot={(catalog.CowboyShoot != null)} hit={(catalog.CowboyHit != null)} cactus={(catalog.CactusTall != null)} sky={(catalog.SkyboxDay != null)}");
        }

        public static void BuildAndExit()
        {
            Build();
            EditorApplication.Exit(0);
        }

        static string Nature(string file) =>
            "Assets/HillNGun/Proto/Greybox3D/ThirdParty/Kenney/NatureKit/" + file;

        static string Survival(string file) =>
            "Assets/HillNGun/Proto/Greybox3D/ThirdParty/Kenney/SurvivalKit/" + file;

        static GameObject LoadGo(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        static Texture2D LoadTex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        static void AssignCowboyClips(Proto_Greybox3DCatalog catalog)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(CowboyPath))
            {
                if (obj is not AnimationClip clip || clip.name.StartsWith("__"))
                    continue;
                string n = clip.name.ToLowerInvariant();
                if (n.Contains("idle") && catalog.CowboyIdle == null) catalog.CowboyIdle = clip;
                else if (n.Contains("walk") && !n.Contains("carry") && catalog.CowboyWalk == null)
                    catalog.CowboyWalk = clip;
                else if (n.Contains("shoot") && catalog.CowboyShoot == null) catalog.CowboyShoot = clip;
                else if ((n.Contains("recievehit") || n.Contains("receivehit") || n.Contains("hit")) &&
                         catalog.CowboyHit == null)
                    catalog.CowboyHit = clip;
                else if (n.Contains("punch") && catalog.CowboyPunch == null) catalog.CowboyPunch = clip;
            }
        }

        static void ConfigureTextures()
        {
            ConfigureTex("Assets/HillNGun/Proto/Greybox3D/ThirdParty/Kenney/Skyboxes/skybox-day.png",
                tex =>
                {
                    tex.textureType = TextureImporterType.Default;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.sRGBTexture = true;
                    tex.mipmapEnabled = false;
                    tex.maxTextureSize = 2048;
                });
            string[] texes =
            {
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/Wood-Texture.png",
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/DirtSandStone-Texture.png",
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/LucianPavel/WesternObjects/Textures/Fabric_Texture.png",
                "Assets/HillNGun/Proto/Greybox3D/ThirdParty/Kenney/SurvivalKit/Textures/colormap.png"
            };
            foreach (var p in texes)
            {
                ConfigureTex(p, tex =>
                {
                    tex.textureType = TextureImporterType.Default;
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.sRGBTexture = true;
                    tex.maxTextureSize = 512;
                });
            }
        }

        static void ConfigureTex(string path, System.Action<TextureImporter> cfg)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            cfg(imp);
            imp.SaveAndReimport();
        }

        const string CowboyPrefabPath =
            "Assets/HillNGun/Proto/Greybox3D/Resources/HillNGunGreybox3D/CowboyMale.prefab";

        static GameObject EnsureCowboyPrefab(GameObject source)
        {
            if (source == null) return null;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CowboyPrefabPath);
            if (existing != null) return existing;
            var tmp = Object.Instantiate(source);
            tmp.name = "CowboyMale";
            var prefab = PrefabUtility.SaveAsPrefabAsset(tmp, CowboyPrefabPath);
            Object.DestroyImmediate(tmp);
            return prefab != null ? prefab : AssetDatabase.LoadAssetAtPath<GameObject>(CowboyPrefabPath);
        }

        static void ConfigureCowboyImport()
        {
            var imp = AssetImporter.GetAtPath(CowboyPath) as ModelImporter;
            if (imp == null) return;
            bool dirty = imp.animationType != ModelImporterAnimationType.Generic
                         || imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                         || !imp.importAnimation;
            if (!dirty) return;
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
            imp.SaveAndReimport();
        }
    }
}

using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>Runtime references to imported CC0 models, filled by the editor catalog builder.</summary>
    public sealed class Proto_Greybox3DCatalog : ScriptableObject
    {
        static Proto_Greybox3DCatalog()
        {
            Proto_Greybox3DViewBootstrap.Register();
        }

        public const string ResourcesPath = "HillNGunGreybox3D/Catalog";

        public GameObject CowboyMale;
        public AnimationClip CowboyIdle;
        public AnimationClip CowboyWalk;
        public AnimationClip CowboyShoot;
        public AnimationClip CowboyHit;
        public AnimationClip CowboyPunch;

        // Kenney Mini Characters 2.0 (CC0) — slice H replacements.
        public GameObject MiniHero;
        public GameObject MiniGrunt;
        public GameObject MiniGun;
        public AnimationClip MiniIdle;
        public AnimationClip MiniWalk;
        public AnimationClip MiniSprint;
        public AnimationClip MiniShoot;
        public AnimationClip MiniHit;
        public AnimationClip MiniDie;
        public AnimationClip MiniJump;
        public Texture2D MiniColormap;
        public Texture2D MiniColormapHero;
        public Texture2D MiniColormapGrunt;
        public Texture2D MiniColormapGruntB;
        public Texture2D MiniColormapGruntC;

        public GameObject CactusShort;
        public GameObject CactusTall;
        public GameObject Grass;
        public GameObject GrassLarge;
        public GameObject GrassLeafs;
        public GameObject RockSmallA;
        public GameObject RockSmallB;
        public GameObject RockSmallC;
        public GameObject RockTallA;
        public GameObject RockTallB;
        public GameObject RockLargeA;
        public GameObject CliffBlock;
        public GameObject CliffLarge;
        public GameObject CliffTop;

        public GameObject Barrel;
        public GameObject Crate;
        public GameObject CrateLarge;
        public GameObject Fence;
        public GameObject FenceDoorway;
        public GameObject PatchGrass;
        public GameObject RockSandA;
        public GameObject RockSandB;

        public GameObject WesternObjects;
        public Texture2D WoodTex;
        public Texture2D DirtTex;
        public Texture2D FabricTex;
        public Texture2D SkyboxDay;

        public Shader CelShader;
        public Shader GlowShader;
        public Shader BlobShader;
        public Shader SkyShader;
        public Shader VignetteShader;

        public bool HasCowboy => CowboyMale != null;
        public bool HasMini => MiniHero != null;
        public bool HasCelShader => CelShader != null;

        static Proto_Greybox3DCatalog _cached;

        public static void Inject(Proto_Greybox3DCatalog catalog)
        {
            if (catalog != null)
                _cached = catalog;
        }

        public static Proto_Greybox3DCatalog Load()
        {
            if (_cached != null && (_cached.MiniHero != null || _cached.CowboyMale != null))
                return _cached;
            var loaded = Resources.Load<Proto_Greybox3DCatalog>(ResourcesPath);
            if (loaded != null)
                _cached = loaded;
            if (_cached != null)
                Proto_Greybox3DMaterials.BindFromCatalog(_cached);
            return _cached;
        }
    }
}

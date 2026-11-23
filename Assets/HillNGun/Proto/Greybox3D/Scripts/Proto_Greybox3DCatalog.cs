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

        // Designer 3D skins (slice I)
        public Texture2D CharColormapPlayer;
        public Texture2D CharColormapEnemy;
        public Texture2D CharColormapEnemyFast;
        public Texture2D CharColormapEnemyStrong;
        public Texture2D CharColormapEnemyBomb;
        public Texture2D CharColormapEnemySniper;
        public Texture2D CharColormapPlayerMaleG;
        public Texture2D EnvWallWood;
        public Texture2D EnvDeckWood;
        public Texture2D EnvRoofShingle;
        public Texture2D EnvAwningBlue;
        public Texture2D EnvAwningRed;
        public Texture2D EnvAwningYellow;
        public Texture2D EnvAwningGreen;
        public Texture2D EnvGroundDirt;
        public Texture2D EnvRockMesa;
        public Texture2D EnvAtlas;
        public Font LilitaOne;
        public Sprite UiJoystickBase;
        public Sprite UiJoystickKnob;
        public Sprite UiJoystickKnobPressed;
        public Sprite UiBtnAttackNormal;
        public Sprite UiBtnAttackPressed;
        public Sprite UiBtnSkillNormal;
        public Sprite UiBtnSkillPressed;
        public Sprite UiBtnReloadNormal;
        public Sprite UiBtnReloadPressed;
        public Sprite UiBtnPauseNormal;
        public Sprite UiBtnPausePressed;
        public Sprite UiHpFrame;
        public Sprite UiHpTrack;
        public Sprite UiHpFill;
        public Sprite UiHpFillLow;
        public Sprite UiHpFillFull;
        public Sprite UiCrosshair;
        public Sprite UiCrosshairHit;
        public Sprite UiTextScore;
        public Sprite UiTextStage;
        public Sprite UiTextHp;
        public Texture2D FxBulletPlayer;
        public Texture2D FxBulletPlayerTrail;
        public Texture2D FxBulletPlayerStreak;
        public Texture2D FxBulletEnemy;
        public Texture2D FxBulletEnemyTrail;
        public Texture2D FxBulletEnemyStreak;
        public Sprite[] FxMuzzle;
        public Sprite[] FxHitSpark;
        public Sprite[] FxExplosion;
        public Sprite[] FxSmoke;
        public Sprite[] FxDust;
        public Sprite[] FxCoinSpin;
        public Sprite FxCoin;
        public Shader SpriteShader;
        public Texture2D CharColormapTurret;
        public Texture2D EnvAwningStall;
        public Texture2D EnvAwningStallEdge;
        public Texture2D EnvFenceWood;
        public Texture2D EnvPropWheel;
        public Texture2D EnvPropLamp;
        public Texture2D EnvPropLampGlow;
        public Texture2D EnvPropRockPile;
        public Texture2D EnvAtlasV2;
        public Sprite[] FxDizzyStars;
        public Sprite[] FxRollDustPuff;
        public Texture2D FxRollDustTrail;
        public Sprite[] FxShieldBubble;
        public Sprite[] FxShieldPop;
        public Texture2D FxShieldSphereAdd;
        public Texture2D FxShieldSphereAlpha;
        public Shader CutoutShader;

        public Shader CelShader;
        public Shader GlowShader;
        public Shader BlobShader;
        public Shader SkyShader;
        public Shader VignetteShader;

        public bool HasCowboy => CowboyMale != null;
        public bool HasMini => MiniHero != null;
        public bool HasDesignerUi => UiJoystickBase != null && UiBtnAttackNormal != null;
        public bool HasDesignerChars => CharColormapPlayer != null;
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

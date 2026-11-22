using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>fx_v2 sprites from Art/WestPixel/fx_v2; missing entries use procedural placeholders.</summary>
    [CreateAssetMenu(fileName = "Proto_GreyboxFxV2Catalog", menuName = "HillNGun/Greybox Fx V2 Catalog")]
    public class Proto_GreyboxFxV2Catalog : ScriptableObject
    {
        public const string ArtFolder = "Assets/HillNGun/Proto/Greybox/Art/WestPixel/fx_v2";
        public const string ArtFolderV2_1 = "Assets/HillNGun/Proto/Greybox/Art/WestPixel/fx_v2_1";
        public const string ResourcesName = "Proto_GreyboxFxV2Catalog";
        public const string EditorAssetPath = "Assets/HillNGun/Proto/Greybox/Resources/Proto_GreyboxFxV2Catalog.asset";
        /// <summary>Designer casings / aim_tip / shadow can read small on screen.</summary>
        public const float SmallFxWorldMul = 1.65f;

        public Sprite[] rollLeft;
        public Sprite[] rollRight;
        public Sprite[] rollForward;
        public Sprite[] rollBack;
        public Sprite rollLeftAfterimage;
        public Sprite rollRightAfterimage;
        public Sprite rollForwardAfterimage;
        public Sprite rollBackAfterimage;
        public Sprite[] rollDustTakeoff;
        public Sprite[] rollDustLand;

        public Sprite[] btnFireCyl;
        public Sprite[] btnFireRefill0to1;
        public Sprite[] btnFireRefill1to2;
        public Sprite[] btnFireRefill2to3;
        public Sprite[] btnFireRefill3to4;
        public Sprite[] btnFireRefill4to5;
        public Sprite[] btnFireEmptyBlink;

        public Sprite[][] reloadCasings;
        public Sprite casing;

        public Sprite[] dizzyLightBody;
        public Sprite[] dizzyLightFx;
        public Sprite[] dizzyLightInFx;
        public Sprite[] dizzySpinInBody;
        public Sprite[] dizzySpinInFx;
        public Sprite[] dizzySpinBody;
        public Sprite[] dizzySpinFx;
        public Sprite[] dizzySpinOutBody;
        public Sprite[] dizzySpinOutFx;
        public Sprite dizzyStar;
        public Sprite dizzyBird;

        public Sprite bulletAlly;
        public Sprite bulletAllyTracer;
        public Sprite bulletEnemy;
        public Sprite[] bulletEnemyFlipbook;
        public Sprite btnGrenadeGaugeBg;
        public Sprite btnGrenadeGaugeFill;
        public Sprite[] btnGrenadeGaugeFull;
        public Sprite[] btnGrenadeGaugeBaked;
        public Sprite[] muzzleFlash;
        public Sprite[] hitSparks;
        public Sprite[] dustGround;
        public Sprite[] dustCrate;
        public Sprite[] dynamiteSpin;
        public Sprite[] smokePuffs;
        public Sprite bombShadow;
        public Sprite[] explosion;
        public Sprite aimBand;
        public Sprite aimTip;
        public Sprite aimBombDot;
        public Sprite aimBombLand;

        public bool HasGameplayRoll => rollLeft != null && rollLeft.Length >= 8;

        public static Proto_GreyboxFxV2Catalog LoadRuntime()
        {
#if UNITY_EDITOR
            var ed = UnityEditor.AssetDatabase.LoadAssetAtPath<Proto_GreyboxFxV2Catalog>(EditorAssetPath);
            if (ed != null)
                return ed;
#endif
            return Resources.Load<Proto_GreyboxFxV2Catalog>(ResourcesName);
        }

        public Sprite[] RollFrames(Proto_GreyboxRollFacing facing)
        {
            switch (facing)
            {
                case Proto_GreyboxRollFacing.Left: return Prefer(rollLeft);
                case Proto_GreyboxRollFacing.Right: return Prefer(rollRight);
                case Proto_GreyboxRollFacing.Front: return Prefer(rollForward);
                case Proto_GreyboxRollFacing.Back: return Prefer(rollBack);
                default: return null;
            }
        }

        public Sprite RollAfterimage(Proto_GreyboxRollFacing facing)
        {
            switch (facing)
            {
                case Proto_GreyboxRollFacing.Left: return rollLeftAfterimage;
                case Proto_GreyboxRollFacing.Right: return rollRightAfterimage;
                case Proto_GreyboxRollFacing.Front: return rollForwardAfterimage;
                case Proto_GreyboxRollFacing.Back: return rollBackAfterimage;
                default: return null;
            }
        }

        public Sprite[] RefillClip(int fromAmmo)
        {
            switch (fromAmmo)
            {
                case 0: return Prefer(btnFireRefill0to1);
                case 1: return Prefer(btnFireRefill1to2);
                case 2: return Prefer(btnFireRefill2to3);
                case 3: return Prefer(btnFireRefill3to4);
                case 4: return Prefer(btnFireRefill4to5);
                default: return null;
            }
        }

        public Sprite[] ReloadCasingClip(int roundsFired)
        {
            if (reloadCasings == null || roundsFired < 1 || roundsFired > 5) return null;
            var clip = reloadCasings[roundsFired - 1];
            return clip != null && clip.Length > 0 ? clip : null;
        }

        public Sprite[] PreferEnemyBulletFrames() =>
            bulletEnemyFlipbook != null && bulletEnemyFlipbook.Length > 0 ? bulletEnemyFlipbook : null;

        static Sprite[] Prefer(Sprite[] fx)
        {
            if (fx != null && fx.Length > 0)
                return fx;
            return null;
        }
    }
}
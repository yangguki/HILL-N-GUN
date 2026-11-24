using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    [CreateAssetMenu(fileName = "Proto_GreyboxWestPixelCatalog", menuName = "HillNGun/Greybox West Pixel Catalog")]
    public class Proto_GreyboxWestPixelCatalog : ScriptableObject
    {
        public const string ResourcesName = "Proto_GreyboxWestPixelCatalog";
        public const string ArtFolder = "Assets/HillNGun/Proto/Greybox/Art/WestPixel";
        public const string PerspectiveArtSubfolder = "perspective_v1";
        public const float ImportPpu = 100f;

        // Slice J: same catalog type, second asset for the flat 2D chibi style.
        public const string ChibiResourcesName = "Proto_GreyboxChibiCatalog";
        public const string ChibiArtFolder = "Assets/HillNGun/Proto/Greybox/Art/Chibi";
        public const string ChibiEditorCatalogAssetPath =
            "Assets/HillNGun/Proto/Greybox/Resources/Proto_GreyboxChibiCatalog.asset";

        public bool HasPerspectiveBackground =>
            bgSkyFar != null && perspGround != null && bldLeft != null && bldRight != null && perspFence != null;

        public Sprite bgFar;
        public Sprite bgTown;
        public Sprite ground;
        public Sprite groundTile;
        public Sprite fence;
        public Sprite fencePost;
        public Sprite crateNear;
        public Sprite crateFar;

        public Sprite bgSkyFar;
        public Sprite bldLeft;
        public Sprite bldRight;
        public Sprite perspGround;
        public Sprite perspFence;
        public Sprite perspFencePost;
        public Sprite crateD40;
        public Sprite crateD46;
        public Sprite crateD52;

        public Sprite[] playerIdle;
        public Sprite[] playerShoot;
        public Sprite[] playerRun;
        public Sprite[] playerRoll;
        public Sprite[] playerHit;

        public Sprite[] gruntIdle;
        public Sprite[] gruntShoot;
        public Sprite[] gruntHit;
        public Sprite[] gruntDown;

        public Sprite bulletEnemy;
        public Sprite bulletAlly;
        public Sprite[] muzzleFlash;
        public Sprite[] hitSpark;
        public Sprite crosshair;

        public Sprite btnFire;
        public Sprite btnReload;
        public Sprite btnGrenade;
        public Sprite btnRoll;
        public Sprite btnPause;
        public Sprite joystickBase;
        public Sprite joystickKnob;
        public Sprite hpFrame;
        public Sprite hpFill;

        [Header("2D chibi only (null in the pixel catalog)")]
        public bool isChibi;
        /// <summary>One 1920×1080 painted plate: sky, mesas, street and the buildings lining it.</summary>
        public Sprite bgFullScene;
        public Sprite scorePlate;
        public Sprite stagePlate;
        public Sprite hpPlate;
        public Sprite heartFull;
        public Sprite heartEmpty;
        public Font uiFont;

        public bool IsGameplayReady =>
            bgFar != null && ground != null && fence != null && playerIdle != null && playerIdle.Length > 0
            && gruntIdle != null && gruntIdle.Length > 0 && bulletEnemy != null && bulletAlly != null
            && btnFire != null;

        public const string EditorCatalogAssetPath =
            "Assets/HillNGun/Proto/Greybox/Resources/Proto_GreyboxWestPixelCatalog.asset";

        public static Proto_GreyboxWestPixelCatalog LoadRuntime()
        {
#if UNITY_EDITOR
            var editorCat = UnityEditor.AssetDatabase.LoadAssetAtPath<Proto_GreyboxWestPixelCatalog>(EditorCatalogAssetPath);
            if (editorCat != null && editorCat.IsGameplayReady)
                return editorCat;
#endif
            var cat = Resources.Load<Proto_GreyboxWestPixelCatalog>(ResourcesName);
            return cat != null && cat.IsGameplayReady ? cat : null;
        }

        public static Proto_GreyboxWestPixelCatalog LoadChibiRuntime()
        {
#if UNITY_EDITOR
            var editorCat = UnityEditor.AssetDatabase.LoadAssetAtPath<Proto_GreyboxWestPixelCatalog>(ChibiEditorCatalogAssetPath);
            if (editorCat != null && editorCat.IsGameplayReady)
                return editorCat;
#endif
            var cat = Resources.Load<Proto_GreyboxWestPixelCatalog>(ChibiResourcesName);
            return cat != null && cat.IsGameplayReady ? cat : null;
        }

        /// <summary>Catalog for the current view style. Falls back to pixel when the chibi asset is missing.</summary>
        public static Proto_GreyboxWestPixelCatalog LoadForCurrentStyle()
        {
            if (Proto_GreyboxViewModeState.IsPersp2D)
                return null;
            if (Proto_GreyboxViewModeState.IsChibi)
            {
                var chibi = LoadChibiRuntime();
                if (chibi != null)
                    return chibi;
            }
            return LoadRuntime();
        }
    }
}
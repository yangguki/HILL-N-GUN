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
    }
}
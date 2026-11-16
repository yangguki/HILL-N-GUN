using System.Collections.Generic;
using UnityEngine;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.Proto.TopDown
{
    /// <summary>Symmetric arena in existing logic tiles. Cover footprints are the only colliders; buildings sit outside the walk polygon.</summary>
    public static class ProtoArena
    {
        public const float MinX = 1f;
        public const float MaxX = 17f;
        public const float MinY = 1.2f;
        public const float MaxY = 12.6f;
        public const float MidX = (MinX + MaxX) * 0.5f;
        /// <summary>Pixel plate squeezes logic Y so the flat map reads a little like a top-down view. Shared by both the pixel camera and pointer aiming.</summary>
        public const float PixelYCompress = 0.72f;
        public const float PostMargin = 0.8f;
        public static readonly Rect PostZone = new Rect(2.2f, 7.4f, 13.6f, 4.2f);

        public struct Piece
        {
            public string Kind;
            public Vector2 Center;
            public float Width;
            public float Depth;
            public bool Sized;
        }

        public static Vector2[] WalkPolygon() => new[]
        {
            new Vector2(MinX, MinY), new Vector2(MaxX, MinY), new Vector2(MaxX, MaxY), new Vector2(MinX, MaxY)
        };

        public static Vector2 PlayerSpawn => new Vector2(MidX, 2.35f);

        /// <summary>Top-edge storefront door. Timings match the existing street door (0.8s telegraph, 0.5s walk-out).</summary>
        public static StageSpawnPoint Door() => new StageSpawnPoint
        {
            id = "door_top",
            type = SpawnPointType.StorefrontDoor,
            side = SpawnSide.Top,
            entranceStart = new Vector2(MidX, 13.55f),
            position = new Vector2(MidX, 11.55f),
            direction = Vector2.down,
            telegraphTime = 0.8f,
            entranceTime = 0.5f
        };

        public static Piece[] Pieces()
        {
            var list = new List<Piece>(12);
            AddL(list, false);
            AddL(list, true);
            list.Add(Asset("sandbag", new Vector2(MidX, 9.35f)));
            list.Add(Asset("sandbag", new Vector2(6.3f, 4.15f)));
            list.Add(Asset("sandbag", new Vector2(Mirror(6.3f), 4.15f)));
            list.Add(Asset("crate", new Vector2(4.7f, 2.95f)));
            list.Add(Asset("crate", new Vector2(Mirror(4.7f), 2.95f)));
            list.Add(Asset("barrel", new Vector2(7.35f, 5.05f)));
            list.Add(Asset("barrel", new Vector2(Mirror(7.35f), 5.05f)));
            return list.ToArray();
        }

        public static float Mirror(float x) => MinX + MaxX - x;

        static void AddL(List<Piece> list, bool right)
        {
            var vert = new Vector2(3.55f, 7.55f);
            var horiz = new Vector2(4.95f, 6.2f);
            if (right) { vert.x = Mirror(vert.x); horiz.x = Mirror(horiz.x); }
            list.Add(new Piece { Kind = "brick", Center = vert, Width = 0.7f, Depth = 2.7f, Sized = true });
            list.Add(new Piece { Kind = "brick", Center = horiz, Width = 2.5f, Depth = 0.7f, Sized = true });
        }

        static Piece Asset(string kind, Vector2 center) => new Piece { Kind = kind, Center = center, Sized = false };

        public static Vector3 ToPixel(Vector2 logic) => new Vector3(logic.x, logic.y * PixelYCompress, 0f);

        public static Vector2 FromPixel(Vector3 world) => new Vector2(world.x, world.y / PixelYCompress);

        public static Vector3 ToWorld3D(Vector2 logic, float height = 0f) => new Vector3(logic.x, height, logic.y);
    }

    public static class ProtoAssets
    {
        public const string Buster = "Assets/HillNGun/Data/Characters/char_buster.asset";
        public const string Enemy = "Assets/HillNGun/Data/Enemies/enemy_pistol_mook.asset";
        public const string Rock = "Assets/HillNGun/Data/Cover/cover_rock.asset";
        public const string Hay = "Assets/HillNGun/Data/Cover/cover_hay.asset";
        public const string Crate = "Assets/HillNGun/Data/Cover/cover_crate.asset";
        public const string Barrel = "Assets/HillNGun/Data/Cover/cover_barrel.asset";
        public const string Rules = "Assets/HillNGun/Data/Match/match_rules_default.asset";
        public const string FeelMove = "Assets/HillNGun/Data/Feel/movement_feel.asset";
        public const string Feel = "Assets/HillNGun/Data/Feel/feel_default.asset";
        public const string Downed = "Assets/HillNGun/Data/Downed/downed_default.asset";
        public const string Scene = "Assets/HillNGun/Scenes/Proto_TopDown.unity";
        public const string MenuScene = "Assets/HillNGun/Scenes/Proto_Menu.unity";
        public const string FrontlineScene = "Assets/HillNGun/Scenes/Proto_Frontline.unity";
        public const string ArtDir = "Assets/HillNGun/Proto/TopDown/Art";
        public const string ProtoArtDir = "Assets/HillNGun/Proto/Art";
        public const string SheetSource = "ArtSource/proto_topdown/buster_pixel_sheet_v2.jpg";
        public const string SpriteDown = ArtDir + "/buster_down.png";
        public const string SpriteUp = ArtDir + "/buster_up.png";
        public const string SpriteSide = ArtDir + "/buster_side.png";
        public const string PixelArtTopDown = ProtoArtDir + "/ProtoPixelArt_TopDown.asset";
        public const string PixelArtFrontline = ProtoArtDir + "/ProtoPixelArt_Frontline.asset";
        public const string AnimArtDir = ProtoArtDir + "/v2/anim";
        public const string FlipbookDir = ProtoArtDir + "/Flipbooks";

        public static string OutputDir => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Builds/proto_topdown"));
    }
}

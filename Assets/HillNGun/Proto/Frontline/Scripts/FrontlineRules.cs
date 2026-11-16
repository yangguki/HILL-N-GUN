using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;

namespace HillNGun.Proto.Frontline
{
    public enum FrontlineOutcome
    {
        Playing,
        Player,
        Enemy,
        Draw
    }

    /// <summary>
    /// Frontline map split. Default is top/bottom (player bottom, enemy top).
    /// Left/right keeps the older bank layout when selected on the match.
    /// Field name on the match: <c>SplitAxis</c>.
    /// </summary>
    public enum FrontlineSplitAxis
    {
        /// <summary>Default. Player bottom / enemy top. Keep as 0 so unset scene fields default correctly.</summary>
        TopBottom = 0,
        LeftRight = 1
    }

    /// <summary>
    /// Plate = full BG frame for the camera. Walk Min/Max = dirt band (movement / spawn / clamp).
    /// A thin static obstacle is the invisible wall; bullets ignore it. ClampSide is the knockback backstop.
    /// Arch posts (top/bottom + v2.1) are extra static obstacles — BG draws them, no extra display mesh.
    /// Horizontal fence span is display-only (see FenceMinX/FenceMaxX); not an obstacle.
    /// </summary>
    public readonly struct FrontlineLayout
    {
        public readonly FrontlineSplitAxis SplitAxis;
        public readonly float PlateMinX;
        public readonly float PlateMaxX;
        public readonly float PlateMinY;
        public readonly float PlateMaxY;
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinY;
        public readonly float MaxY;
        public readonly float LineCoord;
        public readonly float WallHalf;
        public readonly float PlayerHold;
        public readonly float EnemyHold;

        public bool IsTopBottom => SplitAxis == FrontlineSplitAxis.TopBottom;
        public float LineX => IsTopBottom ? float.NaN : LineCoord;
        public float LineY => IsTopBottom ? LineCoord : float.NaN;
        public float MidX => (MinX + MaxX) * 0.5f;
        public float MidY => (MinY + MaxY) * 0.5f;

        /// <summary>Display-only H fence span (preview x 506–1425). Full dirt width in left/right mode.</summary>
        public float FenceMinX => IsTopBottom ? FrontlineRules.PlateXFromPx(506f) : MinX;
        public float FenceMaxX => IsTopBottom ? FrontlineRules.PlateXFromPx(1425f) : MaxX;

        public FrontlineLayout(
            FrontlineSplitAxis axis,
            float plateMinX, float plateMaxX, float plateMinY, float plateMaxY,
            float minX, float maxX, float minY, float maxY,
            float lineCoord, float wallHalf, float playerHold, float enemyHold)
        {
            SplitAxis = axis;
            PlateMinX = plateMinX;
            PlateMaxX = plateMaxX;
            PlateMinY = plateMinY;
            PlateMaxY = plateMaxY;
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            LineCoord = lineCoord;
            WallHalf = wallHalf;
            PlayerHold = playerHold;
            EnemyHold = enemyHold;
        }

        public Rect Wall => IsTopBottom
            ? new Rect(MinX - 2f, LineCoord - WallHalf, (MaxX - MinX) + 4f, WallHalf * 2f)
            : new Rect(LineCoord - WallHalf, PlateMinY - 2f, WallHalf * 2f, (PlateMaxY - PlateMinY) + 4f);

        /// <summary>
        /// Top entrance arch posts (BG-drawn). Prefer static obstacles over column clamp.
        /// Preview posts x 790–830 / 1085–1125, y 60–262 from top on 1920×1080.
        /// </summary>
        public Rect[] ArchPosts()
        {
            if (!IsTopBottom) return System.Array.Empty<Rect>();
            return new[]
            {
                FrontlineRules.ArchPostRect(790f, 830f, 60f, 262f),
                FrontlineRules.ArchPostRect(1085f, 1125f, 60f, 262f)
            };
        }

        public Vector2[] WalkPolygon() => new[]
        {
            new Vector2(MinX, MinY), new Vector2(MaxX, MinY), new Vector2(MaxX, MaxY), new Vector2(MinX, MaxY)
        };

        public Vector2 ClampSide(Vector2 p, float radius, Team team)
        {
            p.x = Mathf.Clamp(p.x, MinX + radius, MaxX - radius);
            p.y = Mathf.Clamp(p.y, MinY + radius, MaxY - radius);
            if (IsTopBottom)
            {
                float edge = team == Team.Player ? LineCoord - radius : LineCoord + radius;
                if (team == Team.Player && p.y > edge) p.y = edge;
                if (team != Team.Player && p.y < edge) p.y = edge;
            }
            else
            {
                float edge = team == Team.Player ? LineCoord - radius : LineCoord + radius;
                if (team == Team.Player && p.x > edge) p.x = edge;
                if (team != Team.Player && p.x < edge) p.x = edge;
            }
            return p;
        }

        public bool OnOwnSide(Vector2 p, float radius, Team team)
        {
            if (IsTopBottom)
            {
                if (team == Team.Player) return p.y + radius <= LineCoord + 0.02f;
                return p.y - radius >= LineCoord - 0.02f;
            }
            if (team == Team.Player) return p.x + radius <= LineCoord + 0.02f;
            return p.x - radius >= LineCoord - 0.02f;
        }

        public float DistToLine(Vector2 p) => IsTopBottom
            ? Mathf.Abs(p.y - LineCoord)
            : Mathf.Abs(p.x - LineCoord);

        public Vector2 TowardEnemy(Team team)
        {
            if (IsTopBottom) return team == Team.Player ? Vector2.up : Vector2.down;
            return team == Team.Player ? Vector2.right : Vector2.left;
        }

        public Vector2 Spawn(Team team, int slot)
        {
            if (IsTopBottom)
            {
                float y = team == Team.Player ? MinY + 1.15f : MaxY - 1.15f;
                float x = slot == 0 ? MidX : slot == 1 ? MidX - 3.1f : MidX + 3.1f;
                return new Vector2(x, y);
            }
            float xL = team == Team.Player ? MinX + 3.25f : MaxX - 3.25f;
            float yL = slot == 0 ? MidY : slot == 1 ? MinY + 3.15f : MaxY - 3.15f;
            return new Vector2(xL, yL);
        }

        public FrontlineRules.Piece[] Pieces()
        {
            var list = new List<FrontlineRules.Piece>(16);
            if (IsTopBottom) AddTopBottom(list);
            else
            {
                AddLeftRight(list, false);
                AddLeftRight(list, true);
            }
            return list.ToArray();
        }

        void AddTopBottom(List<FrontlineRules.Piece> list)
        {
            // Preview (v2_1_frontline_preview): 4 covers/side + 1 mirrored center-lane extra for 3v3 slots.
            // Foot ≈ lower edge of preview range; collision sizes unchanged (Sized brick only).
            // Enemy half (top)
            Add(list, "brick", FrontlineRules.PlatePx(520f, 240f), 2.4f, 0.7f, true, Team.Enemy);       // x420–620 / y155–240
            Add(list, "hay", FrontlineRules.PlatePx(1380f, 250f), 0f, 0f, false, Team.Enemy);          // x1335–1425 / y170–250
            Add(list, "sandbag", FrontlineRules.PlatePx(520f, 420f), 0f, 0f, false, Team.Enemy);      // front L
            Add(list, "crate", FrontlineRules.PlatePx(1420f, 430f), 0f, 0f, false, Team.Enemy);        // front R
            Add(list, "sandbag", FrontlineRules.PlatePx(960f, 400f), 0f, 0f, false, Team.Enemy);      // extra center front

            // Player half (bottom)
            Add(list, "sandbag", FrontlineRules.PlatePx(520f, 650f), 0f, 0f, false, Team.Player);     // front L
            Add(list, "barrel", FrontlineRules.PlatePx(1420f, 650f), 0f, 0f, false, Team.Player);     // front R
            Add(list, "hay", FrontlineRules.PlatePx(602.5f, 875f), 0f, 0f, false, Team.Player);       // back L
            Add(list, "crate", FrontlineRules.PlatePx(1340f, 875f), 0f, 0f, false, Team.Player);      // back R
            Add(list, "crate", FrontlineRules.PlatePx(960f, 630f), 0f, 0f, false, Team.Player);       // extra center front
        }

        void AddLeftRight(List<FrontlineRules.Piece> list, bool right)
        {
            // Legacy left/right cover bank — unchanged so SplitAxis.LeftRight still works.
            var team = right ? Team.Enemy : Team.Player;
            float minX = MinX, maxX = MaxX, midY = MidY, line = LineCoord;
            float mirror = minX + maxX;
            Add(list, "brick", MirrorX(new Vector2(minX + 2.9f, midY - 2.7f), right, mirror), 0.7f, 2.5f, true, team);
            Add(list, "brick", MirrorX(new Vector2(minX + 4.25f, midY - 3.85f), right, mirror), 2.4f, 0.7f, true, team);
            Add(list, "brick", MirrorX(new Vector2(minX + 2.9f, midY + 2.3f), right, mirror), 0.7f, 2.5f, true, team);
            Add(list, "brick", MirrorX(new Vector2(minX + 4.25f, midY + 1.15f), right, mirror), 2.4f, 0.7f, true, team);
            Add(list, "sandbag", MirrorX(new Vector2(line - 2.85f, midY - 3.3f), right, mirror), 0f, 0f, false, team);
            Add(list, "crate", MirrorX(new Vector2(line - 2.85f, midY - 1.7f), right, mirror), 0f, 0f, false, team);
            Add(list, "sandbag", MirrorX(new Vector2(line - 2.85f, midY + 2.5f), right, mirror), 0f, 0f, false, team);
        }

        static Vector2 MirrorX(Vector2 center, bool right, float mirrorSum)
        {
            if (right) center.x = mirrorSum - center.x;
            return center;
        }

        static void Add(List<FrontlineRules.Piece> list, string kind, Vector2 center, float width, float depth, bool sized, Team team)
        {
            list.Add(new FrontlineRules.Piece
            {
                Kind = kind,
                Center = center,
                Width = width,
                Depth = depth,
                Sized = sized,
                Team = team
            });
        }
    }

    /// <summary>
    /// Frontline rules + default layouts. RoundSeconds / Judge stay fixed.
    /// Geometry comes from <see cref="FrontlineLayout"/> so top/bottom and left/right can coexist.
    /// </summary>
    public static class FrontlineRules
    {
        public const float RoundSeconds = 90f;
        public const float WallHalf = 0.15f;

        public const float PlateMinX = 0.6f;
        public const float PlateMaxX = 23.4f;
        public const float PlateMinY = 1.2f;
        public const float PlateMaxY = 12.8f;

        /// <summary>bg_frontline_v2_1 dirt rect x 358–1563, y 128–916 (from top) on 1920×1080.</summary>
        public static readonly Vector2 V21DirtUvMin = new Vector2(358f / 1920f, 1f - 916f / 1080f);
        public static readonly Vector2 V21DirtUvMax = new Vector2(1563f / 1920f, 1f - 128f / 1080f);

        /// <summary>
        /// Default proto: top/bottom on bg_frontline_v2_1 (shops left/right, dirt center strip).
        /// </summary>
        public static FrontlineLayout TopBottomDefault { get; } = BuildTopBottom(
            V21DirtUvMin.x, V21DirtUvMax.x, V21DirtUvMin.y, V21DirtUvMax.y);

        /// <summary>Legacy left/right banks (player left, enemy right). Still selectable via SplitAxis.</summary>
        public static FrontlineLayout LeftRightDefault { get; } = BuildLeftRight(
            dirtUvMinX: 0.12f, dirtUvMaxX: 0.88f, dirtUvMinY: 0.08f, dirtUvMaxY: 0.92f);

        public static FrontlineLayout Default => TopBottomDefault;

        public static float MinX => Default.MinX;
        public static float MaxX => Default.MaxX;
        public static float MinY => Default.MinY;
        public static float MaxY => Default.MaxY;
        public static float LineX => Default.IsTopBottom ? Default.MidX : Default.LineCoord;
        public static float LineY => Default.IsTopBottom ? Default.LineCoord : Default.MidY;
        public static float LeftHoldX => LeftRightDefault.PlayerHold;
        public static float RightHoldX => LeftRightDefault.EnemyHold;
        public static Rect Wall => Default.Wall;

        public static FrontlineLayout For(FrontlineSplitAxis axis) =>
            axis == FrontlineSplitAxis.LeftRight ? LeftRightDefault : TopBottomDefault;

        /// <summary>
        /// Rebuild walk from dirt UV on the shared plate. Used when swapping BG files.
        /// </summary>
        public static FrontlineLayout FromDirtUv(
            FrontlineSplitAxis axis, Vector2 dirtUvMin, Vector2 dirtUvMax)
        {
            if (dirtUvMax.x <= dirtUvMin.x + 0.01f || dirtUvMax.y <= dirtUvMin.y + 0.01f)
                return For(axis);
            return axis == FrontlineSplitAxis.LeftRight
                ? BuildLeftRight(dirtUvMin.x, dirtUvMax.x, dirtUvMin.y, dirtUvMax.y)
                : BuildTopBottom(dirtUvMin.x, dirtUvMax.x, dirtUvMin.y, dirtUvMax.y);
        }

        public static float PlateXFromPx(float px) =>
            Mathf.Lerp(PlateMinX, PlateMaxX, px / 1920f);

        public static float PlateYFromTopPx(float pyFromTop) =>
            Mathf.Lerp(PlateMinY, PlateMaxY, 1f - pyFromTop / 1080f);

        public static Vector2 PlatePx(float px, float pyFromTop) =>
            new Vector2(PlateXFromPx(px), PlateYFromTopPx(pyFromTop));

        public static Rect ArchPostRect(float x0, float x1, float yTop, float yBotFromTop)
        {
            float xa = PlateXFromPx(Mathf.Min(x0, x1));
            float xb = PlateXFromPx(Mathf.Max(x0, x1));
            float yMin = PlateYFromTopPx(Mathf.Max(yTop, yBotFromTop));
            float yMax = PlateYFromTopPx(Mathf.Min(yTop, yBotFromTop));
            return new Rect(xa, yMin, xb - xa, yMax - yMin);
        }

        static FrontlineLayout BuildTopBottom(float dirtUvMinX, float dirtUvMaxX, float dirtUvMinY, float dirtUvMaxY)
        {
            float minX = Mathf.Lerp(PlateMinX, PlateMaxX, dirtUvMinX);
            float maxX = Mathf.Lerp(PlateMinX, PlateMaxX, dirtUvMaxX);
            float minY = Mathf.Lerp(PlateMinY, PlateMaxY, dirtUvMinY);
            float maxY = Mathf.Lerp(PlateMinY, PlateMaxY, dirtUvMaxY);
            float line = (minY + maxY) * 0.5f;
            return new FrontlineLayout(
                FrontlineSplitAxis.TopBottom,
                PlateMinX, PlateMaxX, PlateMinY, PlateMaxY,
                minX, maxX, minY, maxY,
                line, WallHalf,
                Mathf.Lerp(minY, line, 0.55f),
                Mathf.Lerp(line, maxY, 0.45f));
        }

        static FrontlineLayout BuildLeftRight(float dirtUvMinX, float dirtUvMaxX, float dirtUvMinY, float dirtUvMaxY)
        {
            float minX = Mathf.Lerp(PlateMinX, PlateMaxX, dirtUvMinX);
            float maxX = Mathf.Lerp(PlateMinX, PlateMaxX, dirtUvMaxX);
            float minY = Mathf.Lerp(PlateMinY, PlateMaxY, dirtUvMinY);
            float maxY = Mathf.Lerp(PlateMinY, PlateMaxY, dirtUvMaxY);
            float line = (minX + maxX) * 0.5f;
            return new FrontlineLayout(
                FrontlineSplitAxis.LeftRight,
                PlateMinX, PlateMaxX, PlateMinY, PlateMaxY,
                minX, maxX, minY, maxY,
                line, WallHalf,
                Mathf.Lerp(minX, line, 0.55f),
                Mathf.Lerp(line, maxX, 0.45f));
        }

        public struct Piece
        {
            public string Kind;
            public Vector2 Center;
            public float Width;
            public float Depth;
            public bool Sized;
            public Team Team;
        }

        public struct Unit
        {
            public Team Team;
            public bool Alive;
            public float HpRatio;
        }

        public static Vector2[] WalkPolygon() => Default.WalkPolygon();
        public static Vector2 ClampSide(Vector2 p, float radius, Team team) => Default.ClampSide(p, radius, team);
        public static bool OnOwnSide(Vector2 p, float radius, Team team) => Default.OnOwnSide(p, radius, team);
        public static Vector2 Spawn(Team team, int slot) => Default.Spawn(team, slot);
        public static Piece[] Pieces() => Default.Pieces();

        /// <summary>
        /// Wipe ends the round immediately. On timeout, more living units win; equal counts use the sum of
        /// current/max ratios, not the sum of raw HP. A remaining tie is a draw.
        /// </summary>
        public static FrontlineOutcome Judge(IList<Unit> units, bool timeUp)
        {
            int playerAlive = 0, enemyAlive = 0;
            float playerRatio = 0f, enemyRatio = 0f;
            if (units != null)
            {
                for (int i = 0; i < units.Count; i++)
                {
                    if (!units[i].Alive) continue;
                    if (units[i].Team == Team.Player) { playerAlive++; playerRatio += units[i].HpRatio; }
                    else { enemyAlive++; enemyRatio += units[i].HpRatio; }
                }
            }
            if (playerAlive == 0 && enemyAlive == 0) return FrontlineOutcome.Draw;
            if (playerAlive == 0) return FrontlineOutcome.Enemy;
            if (enemyAlive == 0) return FrontlineOutcome.Player;
            if (!timeUp) return FrontlineOutcome.Playing;
            if (playerAlive != enemyAlive) return playerAlive > enemyAlive ? FrontlineOutcome.Player : FrontlineOutcome.Enemy;
            if (Mathf.Abs(playerRatio - enemyRatio) <= 0.0001f) return FrontlineOutcome.Draw;
            return playerRatio > enemyRatio ? FrontlineOutcome.Player : FrontlineOutcome.Enemy;
        }
    }
}

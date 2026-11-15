using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Battlefield
{
    /// <summary>
    /// FreeMove PvE collision world (registered in ServiceLocator by the PvE match while pveMoveMode == FreeMove):
    /// walk polygon from the stage's StreetLayoutData + live (non-destroyed) cover footprints. PvP never registers it,
    /// so RowLane movement keeps its field clamp and cover pass-through.
    /// </summary>
    public sealed class FreeMoveWorld
    {
        public StreetLayoutData Layout { get; private set; }
        public IList<Vector2> Polygon => _poly;
        public System.Func<IReadOnlyList<HillNGun.Cover.CoverInstance>> CoverSource { get; set; }
        /// <summary>Extra fixed obstacles (tests / props). Logic rects.</summary>
        public readonly List<Rect> StaticObstacles = new List<Rect>();

        private Vector2[] _poly;
        private readonly List<Rect> _rects = new List<Rect>();

        public static FreeMoveWorld Active => ServiceLocator.Get<FreeMoveWorld>();

        public FreeMoveWorld(StreetLayoutData layout) { SetLayout(layout); }
        public FreeMoveWorld(Vector2[] polygon) { _poly = polygon; }

        public void SetLayout(StreetLayoutData layout)
        {
            Layout = layout;
            _poly = layout != null && layout.walkPolygon != null && layout.walkPolygon.Length >= 3 ? layout.walkPolygon : DefaultPolygon();
        }

        /// <summary>Fallback when a stage has no layout: the old logic field (0.5..23.5 x 1.3..13.9).</summary>
        public static Vector2[] DefaultPolygon() => new[]
        {
            new Vector2(0.5f, 1.3f), new Vector2(23.5f, 1.3f), new Vector2(23.5f, 13.9f), new Vector2(0.5f, 13.9f)
        };

        public IList<Rect> Obstacles
        {
            get
            {
                _rects.Clear();
                _rects.AddRange(StaticObstacles);
                var covers = CoverSource?.Invoke();
                if (covers != null)
                    for (int i = 0; i < covers.Count; i++)
                    {
                        var c = covers[i];
                        if (c == null || c.IsDestroyed || c.Data == null) continue;
                        _rects.Add(c.GetBounds());
                    }
                return _rects;
            }
        }

        public Vector2 Move(Vector2 from, Vector2 delta, float radius) => FreeMoveCollision.Move(from, delta, radius, _poly, Obstacles);
        public Vector2 Depenetrate(Vector2 p, float radius) => FreeMoveCollision.Depenetrate(p, radius, _poly, Obstacles);
        public bool IsFree(Vector2 p, float radius) => FreeMoveCollision.IsFree(p, radius, _poly, Obstacles);
        /// <summary>Inside the walk polygon only (ignores covers) — used to keep entrance end points honest.</summary>
        public bool InsideWalk(Vector2 p) => _poly != null && FreeMoveCollision.PointInPolygon(p, _poly);
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxCover
    {
        const float CrateFootprintDepth = 0.4f;

        readonly List<Rect> _crates;
        readonly List<Rect> _groundFootprints;

        public IReadOnlyList<Rect> Crates => _crates;

        public Proto_GreyboxCover(IEnumerable<Rect> crates)
        {
            _crates = new List<Rect>(crates);
            _groundFootprints = new List<Rect>(_crates.Count);
            foreach (var r in _crates)
                _groundFootprints.Add(new Rect(r.xMin, r.yMin, r.width, CrateFootprintDepth));
        }

        public Proto_GreyboxCover(IEnumerable<Rect> visualCrates, IEnumerable<Rect> groundFootprints)
        {
            _crates = new List<Rect>(visualCrates);
            _groundFootprints = new List<Rect>(groundFootprints);
        }

        public static Proto_GreyboxCover FromStageCrates()
        {
            var visuals = new[]
            {
                CrateWorldRect(52f, 56.5f, 30f, 36f),
                CrateWorldRect(12f, 21f, 52f, 63f),
                CrateWorldRect(58f, 69f, 62f, 82f),
            };
            var footprints = new List<Rect>(visuals.Length);
            foreach (var v in visuals)
                footprints.Add(CrateGroundFootprint(v));
            return new Proto_GreyboxCover(visuals, footprints);
        }

        public static Rect CrateWorldRect(float x0, float x1, float yTop, float yBottom)
        {
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(yBottom);
            float h = Proto_GreyboxLayout.ScreenPercentToWorldHeight(yBottom - yTop);
            float w = Proto_GreyboxLayout.WorldWidth * (x1 - x0) / 100f;
            float cx = (Proto_GreyboxLayout.ScreenXPercentToWorldX(x0) +
                        Proto_GreyboxLayout.ScreenXPercentToWorldX(x1)) * 0.5f;
            return new Rect(cx - w * 0.5f, footY, w, h);
        }

        public static Rect CrateGroundFootprint(Rect visualCrate) =>
            new Rect(visualCrate.xMin, visualCrate.yMin, visualCrate.width, CrateFootprintDepth);

        public static Rect CrateGroundFootprint(float x0, float x1, float yBottom)
        {
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(yBottom);
            float w = Proto_GreyboxLayout.WorldWidth * (x1 - x0) / 100f;
            float cx = (Proto_GreyboxLayout.ScreenXPercentToWorldX(x0) +
                        Proto_GreyboxLayout.ScreenXPercentToWorldX(x1)) * 0.5f;
            return new Rect(cx - w * 0.5f, footY, w, CrateFootprintDepth);
        }

        /// <summary>Player foot is behind cover when below the crate base (nearer camera) and x-overlapping.</summary>
        public bool IsFootBehindAnyCrate(Vector2 foot)
        {
            foreach (var f in _groundFootprints)
            {
                if (foot.x < f.xMin || foot.x > f.xMax)
                    continue;
                if (foot.y < f.yMin)
                    return true;
            }

            return false;
        }

        public void RemoveCrateMatching(float x0Percent, float x1Percent, float yBottomPercent)
        {
            for (int i = _groundFootprints.Count - 1; i >= 0; i--)
            {
                var g = _groundFootprints[i];
                float cx = (g.xMin + g.xMax) * 0.5f;
                float expectedCx = (Proto_GreyboxLayout.ScreenXPercentToWorldX(x0Percent) +
                                    Proto_GreyboxLayout.ScreenXPercentToWorldX(x1Percent)) * 0.5f;
                float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(yBottomPercent);
                if (Mathf.Abs(cx - expectedCx) < 0.05f && Mathf.Abs(g.yMin - footY) < 0.05f)
                {
                    _crates.RemoveAt(i);
                    _groundFootprints.RemoveAt(i);
                    break;
                }
            }
        }

        public void RemoveGroundFootprintAt(Vector2 center, float tolerance = 0.6f)
        {
            for (int i = _groundFootprints.Count - 1; i >= 0; i--)
            {
                var g = _groundFootprints[i];
                var c = new Vector2((g.xMin + g.xMax) * 0.5f, g.yMin);
                if ((c - center).sqrMagnitude <= tolerance * tolerance)
                {
                    _crates.RemoveAt(i);
                    _groundFootprints.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>Ground-plane segment (shooter foot → target foot); uses crate base footprints, not screen rects.</summary>
        public bool SegmentBlocked(Vector2 from, Vector2 to, out Vector2 hitPoint) =>
            SegmentBlocked(from, to, 0f, out hitPoint);

        public bool SegmentBlocked(Vector2 from, Vector2 to, float ignoreDistFromStart, out Vector2 hitPoint)
        {
            hitPoint = default;
            var bestT = float.MaxValue;
            var found = false;
            foreach (var r in _groundFootprints)
            {
                if (SegmentHitsRect(from, to, r, out var t, out var p) && t < bestT)
                {
                    if (ignoreDistFromStart > 0f && (p - from).magnitude < ignoreDistFromStart)
                        continue;
                    bestT = t;
                    hitPoint = p;
                    found = true;
                }
            }

            return found;
        }

        static bool SegmentHitsRect(Vector2 a, Vector2 b, Rect r, out float hitT, out Vector2 hit)
        {
            hitT = float.MaxValue;
            hit = default;
            var d = b - a;
            float tEnter = 0f;
            float tExit = 1f;
            if (!Clip1D(a.x, d.x, r.xMin, r.xMax, ref tEnter, ref tExit) ||
                !Clip1D(a.y, d.y, r.yMin, r.yMax, ref tEnter, ref tExit))
                return false;

            hitT = Mathf.Clamp01(tEnter);
            hit = a + d * hitT;
            return hitT <= 1f;
        }

        static bool Clip1D(float p, float dp, float min, float max, ref float tEnter, ref float tExit)
        {
            if (Mathf.Abs(dp) < 1e-8f)
                return p >= min && p <= max;

            float t0 = (min - p) / dp;
            float t1 = (max - p) / dp;
            if (t0 > t1)
            {
                var tmp = t0;
                t0 = t1;
                t1 = tmp;
            }

            tEnter = Mathf.Max(tEnter, t0);
            tExit = Mathf.Min(tExit, t1);
            return tEnter <= tExit;
        }
    }
}
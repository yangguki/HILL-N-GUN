using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Scenery
{
    /// <summary>
    /// Shared time-of-day multiply for characters and covers (view only). Driven by the active
    /// <see cref="StageVisualData.timeOfDay"/>; projectiles / muzzle flashes / name labels / outlines
    /// never read this. BG plate tint stays in <see cref="ParallaxBackdrop"/> so empty plates are not double-washed.
    /// </summary>
    public static class TimeOfDayGrade
    {
        public static StageVisualData Visual { get; private set; }
        public static TimeOfDay Time => Visual != null ? Visual.timeOfDay : TimeOfDay.Noon;

        public static void Set(StageVisualData visual) => Visual = visual;

        /// <summary>RGB multiply for actors and covers. Alpha is always 1 (callers keep their own alpha).</summary>
        public static Color ActorMul => MulFor(Time);

        public static Color MulFor(TimeOfDay t)
        {
            switch (t)
            {
                // ~0.8 brightness with a slight blue cast (기획 v4 night)
                case TimeOfDay.Night:
                    return new Color(0.72f, 0.78f, 0.92f, 1f);
                // Weak orange wash — does not re-tint the sunset BG plate
                case TimeOfDay.Sunset:
                    return new Color(1.00f, 0.88f, 0.74f, 1f);
                case TimeOfDay.Afternoon:
                    return new Color(1.00f, 0.96f, 0.88f, 1f);
                default:
                    return Color.white;
            }
        }

        /// <summary>Multiply RGB of <paramref name="c"/> by the current grade; keeps <paramref name="c"/>.a.</summary>
        public static Color Apply(Color c)
        {
            var g = ActorMul;
            if (g.r >= 0.999f && g.g >= 0.999f && g.b >= 0.999f) return c;
            return new Color(c.r * g.r, c.g * g.g, c.b * g.b, c.a);
        }
    }
}

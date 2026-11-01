using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.UI
{
    /// <summary>
    /// Screen areas currently used by the IMGUI HUDs (score, timer, team panels, boss bar, pause button). HUDs report
    /// their rects while drawing; the touch layout reads them (bottom-left origin, pixels) so the controls never cover
    /// the HUD. Entries expire if a HUD stops reporting them.
    /// </summary>
    public static class HudZones
    {
        private struct Entry { public Rect rect; public float time; }
        private static readonly Dictionary<string, Entry> _zones = new Dictionary<string, Entry>();
        private static readonly List<Rect> _scratch = new List<Rect>();
        private const float Expire = 1.0f;

        /// <summary>Changes whenever a zone appears, disappears or moves by more than a few pixels.</summary>
        public static int Version { get; private set; }

        /// <summary>Report a rect in the current IMGUI space (GUI.matrix applied, top-left origin). Call during Repaint.</summary>
        public static void ReportGui(string key, Rect guiRect)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint) return;
            Matrix4x4 m = GUI.matrix;
            Vector3 a = m.MultiplyPoint3x4(new Vector3(guiRect.xMin, guiRect.yMin, 0f));
            Vector3 b = m.MultiplyPoint3x4(new Vector3(guiRect.xMax, guiRect.yMax, 0f));
            float h = Screen.height;
            var screen = Rect.MinMaxRect(Mathf.Min(a.x, b.x), h - Mathf.Max(a.y, b.y), Mathf.Max(a.x, b.x), h - Mathf.Min(a.y, b.y));
            Report(key, screen);
        }

        /// <summary>Report a rect in screen pixels, bottom-left origin.</summary>
        public static void Report(string key, Rect screenRect)
        {
            float now = Time.unscaledTime;
            if (_zones.TryGetValue(key, out var e))
            {
                if (Mathf.Abs(e.rect.x - screenRect.x) > 4f || Mathf.Abs(e.rect.y - screenRect.y) > 4f ||
                    Mathf.Abs(e.rect.width - screenRect.width) > 4f || Mathf.Abs(e.rect.height - screenRect.height) > 4f)
                {
                    Version++;
                    e.rect = screenRect;
                }
                e.time = now;
                _zones[key] = e;
            }
            else
            {
                _zones[key] = new Entry { rect = screenRect, time = now };
                Version++;
            }
        }

        /// <summary>Current zones (bottom-left origin). Drops expired entries.</summary>
        public static IReadOnlyList<Rect> Collect()
        {
            _scratch.Clear();
            float now = Time.unscaledTime;
            List<string> dead = null;
            foreach (var kv in _zones)
            {
                if (now - kv.Value.time > Expire) { (dead ??= new List<string>()).Add(kv.Key); continue; }
                _scratch.Add(kv.Value.rect);
            }
            if (dead != null) { foreach (var k in dead) _zones.Remove(k); Version++; }
            return _scratch;
        }

        public static void Clear() { if (_zones.Count > 0) { _zones.Clear(); Version++; } }
    }
}

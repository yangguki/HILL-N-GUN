using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Scenery
{
    /// <summary>
    /// Baked Western backdrops for IMGUI menus: a wide panorama (lobby / title / team select / result) and small
    /// per-theme field thumbnails (stage select cards). Bakes happen in a hidden runner's Update (never inside
    /// OnGUI) and are cached for the session; drawing is a single GUI.DrawTexture.
    /// </summary>
    public static class MenuBackdrop
    {
        private static readonly Dictionary<string, Texture2D> _pano = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> _thumb = new Dictionary<string, Texture2D>();
        private static readonly List<System.Action> _queue = new List<System.Action>();
        private static readonly HashSet<string> _pending = new HashSet<string>();
        private static Runner _runner;

        private sealed class Runner : MonoBehaviour
        {
            private void Update()
            {
                if (_queue.Count == 0) return;
                var a = _queue[0];
                _queue.RemoveAt(0);
                try { a(); } catch (System.Exception e) { Debug.LogWarning("[MenuBackdrop] bake failed: " + e.Message); }
            }
        }

        private static void Enqueue(string key, System.Action a)
        {
            if (_pending.Contains(key)) return;
            _pending.Add(key);
            _queue.Add(a);
            if (_runner == null)
            {
                var go = new GameObject("MenuBackdropRunner") { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<Runner>();
            }
        }

        /// <summary>Draws the menu panorama into r (current GUI matrix space). Returns false until it is baked.</summary>
        public static bool Draw(Rect r, StageThemeData theme = null, float dim = 0f)
        {
            if (theme == null)
            {
                theme = ThemeContext.Menu;
                if (theme != null) ThemeContext.Set(theme);
            }
            if (theme == null) return false;
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            string key = theme.id + "@" + Mathf.RoundToInt(aspect * 10f) + "x" + (Screen.height / 240);
            if (!_pano.TryGetValue(key, out var tex) || tex == null)
            {
                int w = Screen.width, h = Screen.height;
                var th = theme;
                Enqueue("p:" + key, () =>
                {
                    var lib = ThemeContext.Library;
                    _pano[key] = BackdropBaker.BakePanorama(th, w, h, lib != null ? lib.maxBakeSize : 2048);
                    _pending.Remove("p:" + key);
                });
                return false;
            }
            if (Event.current == null || Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(r, tex, ScaleMode.ScaleAndCrop, false);
                if (dim > 0f)
                {
                    var old = GUI.color;
                    GUI.color = new Color(0.08f, 0.04f, 0.02f, dim);
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = old;
                }
            }
            return true;
        }

        /// <summary>Small field thumbnail (the actual stage backdrop) for a theme; null until baked.</summary>
        public static Texture2D Thumb(StageThemeData theme)
        {
            if (theme == null) return null;
            if (_thumb.TryGetValue(theme.id, out var tex) && tex != null) return tex;
            var th = theme;
            Enqueue("t:" + theme.id, () =>
            {
                var bf = RuntimeAssetManager.Instance != null ? RuntimeAssetManager.Instance.BattlefieldData : null;
                // top-left of the stage view: side scenery (buildings / cliffs / platform), horizon and the street
                var view = new Rect(-17.78f, -5.5f, 19.5f, 15.5f);
                _thumb[th.id] = BackdropBaker.BakeField(th, FieldMap.From(bf), view, 0f, 512, 300);
                _pending.Remove("t:" + th.id);
            });
            return null;
        }

        /// <summary>Theme for a stage (null-safe).</summary>
        public static StageThemeData ThemeFor(StageData stage) => ThemeContext.ForStage(stage);
    }
}

using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.UI
{
    /// <summary>
    /// Shared IMGUI helpers for menu screens. Colors / fonts come from UIThemeData tokens (05 §5.1).
    /// Screens draw in a virtual space of height = theme.referenceHeight; Begin() sets GUI.matrix to scale it.
    /// </summary>
    public static class UIKit
    {
        private static UIThemeData _fallback;
        public static UIThemeData Theme
        {
            get
            {
                var t = ServiceLocator.Get<UIThemeData>() ?? RuntimeAssetManager.Instance?.UITheme;
                if (t != null) return t;
                if (_fallback == null) _fallback = ScriptableObject.CreateInstance<UIThemeData>();
                return _fallback;
            }
        }

        public static float Scale { get; private set; } = 1f;
        public static float Width { get; private set; }
        public static float Height { get; private set; }

        private static Matrix4x4 _saved;

        /// <summary>Start scaled drawing. Returns virtual screen size.</summary>
        public static Vector2 Begin()
        {
            _saved = GUI.matrix;
            float refH = Mathf.Max(360f, Theme.referenceHeight);
            Scale = Screen.height / refH;
            Height = refH;
            Width = Screen.width / Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Scale, Scale, 1f));
            return new Vector2(Width, Height);
        }

        public static void End() => GUI.matrix = _saved;

        // ---------------- primitives ----------------
        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>Rectangle outline in ONE draw call (IMGUI border-width mode) instead of four filled strips.</summary>
        public static void Border(Rect r, Color c, float t = 2f)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, new Vector4(t, t, t, t), Vector4.zero);
        }

        public static void Panel(Rect r, bool dark = false)
        {
            Fill(r, dark ? Theme.panelDark : Theme.panel);
            Border(r, Theme.panelBorder, 2f);
        }

        public static void Background()
        {
            // baked Western panorama (dusk frontier town) under a light dim so panels stay readable
            if (HillNGun.Scenery.MenuBackdrop.Draw(new Rect(0, 0, Width, Height), null, 0.22f)) return;
            Fill(new Rect(0, 0, Width, Height), Theme.background);
            // subtle plank stripes (placeholder wood texture, self-made)
            for (float y = 0; y < Height; y += 48f) Fill(new Rect(0, y, Width, 2f), Theme.backgroundStripe);
        }

        public static void Bar(Rect r, float t, Color fill)
        {
            Fill(r, new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(r.x + 1, r.y + 1, Mathf.Max(0f, (r.width - 2) * Mathf.Clamp01(t)), r.height - 2), fill);
        }

        /// <summary>Draws a sprite fitted (aspect kept) inside r.</summary>
        public static void Sprite(Rect r, UnityEngine.Sprite s, Color? tint = null)
        {
            if (s == null || s.texture == null) return;
            Rect tr = s.textureRect;
            float aspect = tr.width / Mathf.Max(1f, tr.height);
            float w = r.width, h = r.width / aspect;
            if (h > r.height) { h = r.height; w = h * aspect; }
            Rect dst = new Rect(r.x + (r.width - w) / 2f, r.y + (r.height - h) / 2f, w, h);
            Rect uv = new Rect(tr.x / s.texture.width, tr.y / s.texture.height, tr.width / s.texture.width, tr.height / s.texture.height);
            var old = GUI.color;
            GUI.color = tint ?? Color.white;
            GUI.DrawTextureWithTexCoords(dst, s.texture, uv, true);
            GUI.color = old;
        }

        // ---------------- text ----------------
        private static GUIStyle _label;
        public static GUIStyle LabelStyle(int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false)
        {
            if (_label == null) _label = new GUIStyle(GUI.skin.label);
            _label.fontSize = size;
            _label.normal.textColor = color;
            _label.alignment = anchor;
            _label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            _label.wordWrap = wrap;
            _label.clipping = TextClipping.Clip;
            return _label;
        }

        public static void Label(Rect r, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false)
            => GUI.Label(r, text, LabelStyle(size, color, anchor, bold, wrap));

        public static void Title(Rect r, string text) => Label(r, text, Theme.fontTitle, Theme.accent, TextAnchor.MiddleCenter, true);
        public static void Header(Rect r, string text, TextAnchor a = TextAnchor.MiddleLeft) => Label(r, text, Theme.fontHeader, Theme.accent, a, true);
        public static void Body(Rect r, string text, TextAnchor a = TextAnchor.MiddleLeft) => Label(r, text, Theme.fontBody, Theme.text, a);
        public static void Small(Rect r, string text, TextAnchor a = TextAnchor.MiddleLeft) => Label(r, text, Theme.fontSmall, Theme.textDim, a, false, true);

        // ---------------- buttons ----------------
        public enum ButtonKind { Normal, Primary, Tab, TabActive }

        /// <summary>Theme button. Returns true when clicked (and enabled).</summary>
        public static bool Button(Rect r, string text, bool enabled = true, ButtonKind kind = ButtonKind.Normal, int fontSize = 0)
        {
            var t = Theme;
            bool hover = enabled && r.Contains(Event.current.mousePosition);
            Color bg = !enabled ? t.buttonDisabled
                : kind == ButtonKind.Primary ? (hover ? Color.Lerp(t.buttonPrimary, Color.white, 0.15f) : t.buttonPrimary)
                : kind == ButtonKind.TabActive ? t.tabActive
                : kind == ButtonKind.Tab ? (hover ? t.buttonHover : t.tab)
                : (hover ? t.buttonHover : t.button);
            Fill(r, bg);
            Border(r, kind == ButtonKind.TabActive ? t.accent : new Color(0f, 0f, 0f, 0.35f), kind == ButtonKind.TabActive ? 2f : 1f);
            Label(r, text, fontSize > 0 ? fontSize : t.fontButton, enabled ? t.text : t.textDim, TextAnchor.MiddleCenter, true);
            if (!enabled) return false;
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (clicked) HillNGun.Feel.PlaceholderAudio.Instance?.PlayUIClick();
            return clicked;
        }

        /// <summary>Small tag showing that a feature is an external stub (needs SDK / server / license).</summary>
        public static void StubTag(Rect r, string text)
        {
            Fill(r, Theme.stubBadge);
            Label(r, text, Theme.fontSmall - 2, Color.white, TextAnchor.MiddleCenter, true);
        }

        public static bool Toggle(Rect r, string label, bool value)
        {
            var t = Theme;
            Label(new Rect(r.x, r.y, r.width - 110f, r.height), label, t.fontBody, t.text);
            Rect sw = new Rect(r.xMax - 96f, r.y + (r.height - 34f) / 2f, 96f, 34f);
            if (Button(sw, value ? "ON" : "OFF", true, value ? ButtonKind.Primary : ButtonKind.Normal, t.fontSmall + 2)) return !value;
            return value;
        }

        public static float Slider(Rect r, string label, float value)
        {
            var t = Theme;
            Label(new Rect(r.x, r.y, 200f, r.height), label, t.fontBody, t.text);
            Rect bar = new Rect(r.x + 210f, r.y + r.height / 2f - 8f, r.width - 290f, 16f);
            Bar(bar, value, t.accent);
            Label(new Rect(r.xMax - 70f, r.y, 70f, r.height), Mathf.RoundToInt(value * 100f) + "%", t.fontBody, t.text, TextAnchor.MiddleRight);
            Rect hit = new Rect(bar.x - 8f, r.y, bar.width + 16f, r.height);
            var e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive, hit);
            if ((e.type == EventType.MouseDown && hit.Contains(e.mousePosition)) || (e.type == EventType.MouseDrag && GUIUtility.hotControl == id))
            {
                GUIUtility.hotControl = id;
                value = Mathf.Clamp01((e.mousePosition.x - bar.x) / bar.width);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }
            return value;
        }

        // ---------------- localization ----------------
        public static string T(string key, string fallback)
        {
            var loc = ServiceLocator.Get<LocalizationManager>();
            if (loc != null && loc.HasString(key)) return loc.GetString(key);
            return fallback;
        }

        public static string TF(string key, string fallback, params object[] args)
        {
            string f = T(key, fallback);
            try { return string.Format(f, args); } catch { return f; }
        }
    }
}

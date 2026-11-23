using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    /// <summary>
    /// Slice J: flat 2D chibi HUD dressing on top of the stock Greybox HUD. Hit areas and layout
    /// percentages are unchanged; only plates, heart sprites, fonts and colours are layered on.
    /// Game logic keeps 3 hearts — the 3-segment gauge is the existing hearts HUD reskinned.
    /// </summary>
    public static class Proto_GreyboxChibiHud
    {
        public const string OverlayName = "Greybox_Hud_ChibiOverlay";
        const string CylinderName = "Greybox_Hud_Cylinder";
        const string HeartsRootName = "Greybox_Hud_Hearts";

        static Transform _overlay;
        static bool _applied;
        static Proto_GreyboxWestPixelCatalog _cat;
        static readonly List<GameObject> _hiddenByUs = new();
        static readonly Dictionary<Text, TextState> _textStates = new();
        static readonly Dictionary<Image, Color> _heartBackColors = new();

        struct TextState
        {
            public Font Font;
            public Color Color;
            public int Size;
            public bool HadOutline;
        }

        public static bool IsApplied => _applied;

        public static void Apply(Transform hud, Proto_GreyboxWestPixelCatalog cat)
        {
            if (hud == null || cat == null) return;
            if (_applied && _overlay != null && _cat == cat)
            {
                SetOverlayVisible(true);
                Tick(cat);
                return;
            }

            Restore();
            _cat = cat;
            _applied = true;

            var go = new GameObject(OverlayName, typeof(RectTransform));
            go.transform.SetParent(hud, false);
            go.transform.SetAsFirstSibling(); // behind every stock widget (plates sit under the texts)
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _overlay = go.transform;

            // Plates (top-left score, top-centre stage, bottom-centre HP) — same spots as the 2D HUD texts.
            AddPlate("Chibi_ScorePlate", cat.scorePlate, 1.2f, 1.2f, 22.6f, 8.6f);
            AddPlate("Chibi_StagePlate", cat.stagePlate, 33.5f, 1.2f, 66.5f, 8.8f);
            AddPlate("Chibi_HpPlate", cat.hpPlate, 27.4f, 90.8f, 70.4f, 98.6f);

            StyleText(Proto_GreyboxHudBuilder.ScoreLabelName, cat.uiFont, Color.white, 0);
            StyleText(Proto_GreyboxHudBuilder.ScoreTextName, cat.uiFont, new Color(1f, 0.83f, 0.2f, 1f), 0);
            StyleText(Proto_GreyboxHudBuilder.WaveTextName, null, Color.white, 0); // keep OS font: "·" glyph
            StyleText(Proto_GreyboxHudBuilder.AmmoTextName, cat.uiFont, Color.white, 15);

            Tick(cat);
        }

        /// <summary>Keeps chibi-only state stable while stock HUD code runs every frame.</summary>
        public static void Tick(Proto_GreyboxWestPixelCatalog cat)
        {
            if (!_applied || cat == null) return;
            if (_overlay == null || !_overlay.gameObject.activeSelf) return;

            // Designer cylinder face is pixel art and covers the ATTACK button; ammo shows as text instead.
            HideOnce(GameObject.Find(CylinderName));
            var fire = GameObject.Find(Proto_GreyboxHudBuilder.FireButtonName);
            var fireImg = fire != null ? fire.GetComponent<Image>() : null;
            if (fireImg != null && fireImg.color.a < 0.99f)
                fireImg.color = Color.white;

            var hearts = GameObject.Find(HeartsRootName);
            if (hearts != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var back = hearts.transform.Find($"HeartBack_{i}")?.GetComponent<Image>();
                    var fill = hearts.transform.Find($"HeartFill_{i}")?.GetComponent<Image>();
                    if (back != null && cat.heartEmpty != null && back.sprite != cat.heartEmpty)
                    {
                        if (!_heartBackColors.ContainsKey(back))
                            _heartBackColors[back] = back.color;
                        back.sprite = cat.heartEmpty;
                        back.color = Color.white;
                    }
                    if (fill != null && cat.heartFull != null && fill.sprite != cat.heartFull)
                        fill.sprite = cat.heartFull;
                }
            }
        }

        public static void SetOverlayVisible(bool visible)
        {
            if (_overlay != null)
                _overlay.gameObject.SetActive(visible);
        }

        public static void Restore()
        {
            if (_overlay != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(_overlay.gameObject);
                else
#endif
                    Object.Destroy(_overlay.gameObject);
            }
            _overlay = null;

            foreach (var go in _hiddenByUs)
                if (go != null) go.SetActive(true);
            _hiddenByUs.Clear();

            foreach (var kv in _textStates)
            {
                var t = kv.Key;
                if (t == null) continue;
                t.font = kv.Value.Font;
                t.color = kv.Value.Color;
                t.fontSize = kv.Value.Size;
                if (!kv.Value.HadOutline)
                {
                    var o = t.GetComponent<Outline>();
                    if (o != null) DestroySafe(o);
                }
            }
            _textStates.Clear();

            var heartSprite = Proto_GreyboxSpriteUtil.GetHeartSprite();
            foreach (var kv in _heartBackColors)
            {
                if (kv.Key == null) continue;
                kv.Key.sprite = heartSprite;
                kv.Key.color = kv.Value;
            }
            _heartBackColors.Clear();
            var hearts = GameObject.Find(HeartsRootName);
            if (hearts != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var fill = hearts.transform.Find($"HeartFill_{i}")?.GetComponent<Image>();
                    var back = hearts.transform.Find($"HeartBack_{i}")?.GetComponent<Image>();
                    if (fill != null) fill.sprite = heartSprite;
                    if (back != null) back.sprite = heartSprite;
                }
            }

            _applied = false;
            _cat = null;
        }

        static void AddPlate(string name, Sprite sprite, float x0, float y0, float x1, float y1)
        {
            if (sprite == null || _overlay == null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_overlay, false);
            Proto_GreyboxHudBuilder.ApplyRect(go.GetComponent<RectTransform>(), x0, y0, x1, y1);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = false;
        }

        static void StyleText(string name, Font font, Color color, int size)
        {
            var go = GameObject.Find(name);
            if (go == null) return;
            var t = go.GetComponent<Text>();
            if (t == null) return;
            if (!_textStates.ContainsKey(t))
                _textStates[t] = new TextState
                {
                    Font = t.font, Color = t.color, Size = t.fontSize, HadOutline = t.GetComponent<Outline>() != null
                };
            if (font != null) t.font = font;
            t.color = color;
            if (size > 0) t.fontSize = size;
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.13f, 0.09f, 0.07f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        static void HideOnce(GameObject go)
        {
            if (go == null || !go.activeSelf) return;
            go.SetActive(false);
            if (!_hiddenByUs.Contains(go))
                _hiddenByUs.Add(go);
        }

        static void DestroySafe(Object o)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(o);
            else
#endif
                Object.Destroy(o);
        }
    }
}

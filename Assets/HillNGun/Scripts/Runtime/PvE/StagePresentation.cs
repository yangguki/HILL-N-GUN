using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using HillNGun.Data;
using HillNGun.Core;

namespace HillNGun.PvE
{
    /// <summary>Task I3: skippable intro pan, Rex WANTED card, WAVE/CLEAR banners. No blood/execution art.</summary>
    public class StagePresentation : MonoBehaviour
    {
        public static StagePresentation Instance { get; private set; }

        private bool _skip;
        private bool _busy;
        private string _banner;
        private float _bannerUntil;
        private bool _wanted;
        private float _wantedUntil;
        private string _wantedTitle = "WANTED";
        private string _wantedName;
        private string _wantedBounty;
        private Sprite _wantedPortrait;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!_busy && _bannerUntil <= 0f && !_wanted) return;
            bool tap = false;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) tap = true;
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) tap = true;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) tap = true;
            if (tap) _skip = true;
        }

        public IEnumerator PlayIntroPan(float duration = 1.5f)
        {
            _busy = true; _skip = false;
            var cam = Camera.main;
            if (cam == null) { _busy = false; yield break; }
            Vector3 end = cam.transform.position;
            Vector3 start = end + new Vector3(0f, 3.5f, 0f);
            cam.transform.position = start;
            float t = 0f;
            duration = Mathf.Clamp(duration, 1.2f, 1.8f);
            while (t < duration && !_skip)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / duration);
                cam.transform.position = Vector3.Lerp(start, end, u);
                yield return null;
            }
            cam.transform.position = end;
            _busy = false; _skip = false;
        }

        /// <summary>WANTED poster. Bounty always uses comma thousands ($1,000) regardless of locale.</summary>
        public IEnumerator PlayWantedCard(string bossName, int bounty, Sprite portrait = null, float duration = 2f)
        {
            _busy = true; _skip = false;
            _wanted = true;
            _wantedName = bossName;
            _wantedBounty = FormatBounty(bounty);
            Debug.Log($"[WANTED] bounty='{_wantedBounty}' codes={string.Join(",", Array.ConvertAll(_wantedBounty.ToCharArray(), c => ((int)c).ToString("x")))}");
            _wantedPortrait = portrait;
            _wantedUntil = Time.unscaledTime + duration;
            float t = 0f;
            while (t < duration && !_skip)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            _wanted = false;
            _busy = false; _skip = false;
        }

        /// <summary>Always "$1,000" style with ASCII comma — never locale period thousands ($1.000).</summary>
        public static string FormatBounty(int amount)
        {
            // InvariantCulture forces ',' thousands sep (ko-KR would emit '.').
            return string.Format(CultureInfo.InvariantCulture, "${0:N0}", amount);
        }

        static Font _latinFont;
        static Font LatinFont()
        {
            if (_latinFont != null) return _latinFont;
            try { _latinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { /* ignore */ }
            if (_latinFont == null) try { _latinFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { /* ignore */ }
            return _latinFont;
        }

        public void ShowBanner(string text, float seconds = 0.55f)
        {
            _banner = text;
            _bannerUntil = Time.unscaledTime + seconds;
        }

        public void ShowWave(int n) => ShowBanner("WAVE " + n, 0.6f);
        public void ShowClear() => ShowBanner("CLEAR", 0.5f);

        public bool IsWantedShowing => _wanted;

        private void OnGUI()
        {
            if (_wanted)
            {
                float sw = Screen.width, sh = Screen.height;
                float w = Mathf.Min(sw * 0.42f, 420f), h = w * 1.35f;
                var r = new Rect((sw - w) * 0.5f, (sh - h) * 0.5f, w, h);
                GUI.color = new Color(0.12f, 0.08f, 0.05f, 0.92f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = new Color(0.85f, 0.7f, 0.35f, 1f);
                DrawBorder(r, 4f);
                GUI.color = Color.white;
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
                style.fontSize = Mathf.RoundToInt(28 * (sw / 1280f));
                GUI.Label(new Rect(r.x, r.y + 18f, r.width, 36f), _wantedTitle, style);

                // Center portrait between title and name
                float artH = h * 0.42f;
                float artW = artH * 0.85f;
                var art = new Rect(r.x + (r.width - artW) * 0.5f, r.y + 58f, artW, artH);
                if (_wantedPortrait != null && _wantedPortrait.texture != null)
                {
                    GUI.color = Color.white;
                    Rect tr = _wantedPortrait.textureRect;
                    Rect uv = new Rect(tr.x / _wantedPortrait.texture.width, tr.y / _wantedPortrait.texture.height,
                        tr.width / _wantedPortrait.texture.width, tr.height / _wantedPortrait.texture.height);
                    float aspect = tr.width / Mathf.Max(1f, tr.height);
                    float aw = art.width, ah = art.width / aspect;
                    if (ah > art.height) { ah = art.height; aw = ah * aspect; }
                    var dst = new Rect(art.x + (art.width - aw) * 0.5f, art.y + (art.height - ah) * 0.5f, aw, ah);
                    GUI.DrawTextureWithTexCoords(dst, _wantedPortrait.texture, uv, true);
                }
                else
                {
                    GUI.color = new Color(0.25f, 0.18f, 0.12f, 0.9f);
                    GUI.DrawTexture(art, Texture2D.whiteTexture);
                    GUI.color = new Color(0.7f, 0.6f, 0.45f, 1f);
                    style.fontSize = Mathf.RoundToInt(18 * (sw / 1280f));
                    GUI.Label(art, "?", style);
                }

                GUI.color = Color.white;
                style.fontSize = Mathf.RoundToInt(30 * (sw / 1280f));
                GUI.Label(new Rect(r.x + 12f, art.yMax + 8f, r.width - 24f, 48f), _wantedName ?? "", style);
                // Bounty: Latin font + tall overflow rect so ',' descender is not clipped into a '.' lookalike.
                var bountyStyle = new GUIStyle(style)
                {
                    fontSize = Mathf.RoundToInt(26 * (sw / 1280f)),
                    clipping = TextClipping.Overflow,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                var latin = LatinFont();
                if (latin != null)
                {
                    bountyStyle.font = latin;
                    latin.RequestCharactersInTexture("$0123456789,", bountyStyle.fontSize, FontStyle.Bold);
                    latin.RequestCharactersInTexture("$0123456789,", bountyStyle.fontSize, FontStyle.Normal);
                }
                GUI.color = new Color(1f, 0.85f, 0.3f);
                GUI.Label(new Rect(r.x, r.yMax - 108f, r.width, 56f), _wantedBounty ?? "", bountyStyle);
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                style.fontSize = Mathf.RoundToInt(16 * (sw / 1280f));
                GUI.Label(new Rect(r.x, r.yMax - 44f, r.width, 28f), "TAP TO SKIP", style);
                GUI.color = Color.white;
            }
            if (!string.IsNullOrEmpty(_banner) && Time.unscaledTime < _bannerUntil)
            {
                float sw = Screen.width, sh = Screen.height;
                var r = new Rect(sw * 0.2f, sh * 0.18f, sw * 0.6f, sh * 0.1f);
                GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.65f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = Color.white;
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                style.fontSize = Mathf.RoundToInt(42 * (sw / 1280f));
                GUI.Label(r, _banner, style);
            }
        }

        private static void DrawBorder(Rect r, float t)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
        }
    }
}

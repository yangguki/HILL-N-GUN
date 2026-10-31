using UnityEngine;
using HillNGun.Battlefield;

namespace HillNGun.PvP
{
    /// <summary>Placeholder crate drawing (procedural box, no external art) + small HP bar.</summary>
    public class SupplyCrateView : MonoBehaviour
    {
        private SupplyCrate _crate;
        private SpriteRenderer _box, _barBg, _barFill;
        private float _dropAnim;

        public void Initialize(SupplyCrate crate)
        {
            _crate = crate;
            _box = MakeRenderer("Box", MakeCrateSprite());
            _barBg = MakeRenderer("BarBg", Pixel());
            _barFill = MakeRenderer("BarFill", Pixel());
            _barBg.color = new Color(0f, 0f, 0f, 0.7f);
            _barFill.color = new Color(1f, 0.85f, 0.3f);
        }

        private SpriteRenderer MakeRenderer(string name, Sprite s)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = s;
            return r;
        }

        private void Update()
        {
            bool show = _crate != null && _crate.IsActive && !_crate.IsOpened;
            _box.enabled = _barBg.enabled = _barFill.enabled = show;
            if (!show) { _dropAnim = 0f; return; }

            _dropAnim = Mathf.Min(1f, _dropAnim + Time.deltaTime * 2f);
            Vector3 world = FieldView.ToWorld(_crate.Position);
            float drop = (1f - _dropAnim) * 4f;
            _box.transform.position = world + Vector3.up * drop;
            _box.transform.localScale = Vector3.one * 1.1f;
            _box.sortingOrder = FieldView.ProjectileOrder - 10;

            const float w = 1.2f;
            _barBg.transform.position = world + new Vector3(-w * 0.5f, 0.9f + drop, 0f);
            _barBg.transform.localScale = new Vector3(w, 0.14f, 1f);
            _barFill.transform.position = _barBg.transform.position;
            _barFill.transform.localScale = new Vector3(w * _crate.HpRatio, 0.1f, 1f);
            _barBg.sortingOrder = FieldView.OverlayOrder - 6;
            _barFill.sortingOrder = FieldView.OverlayOrder - 5;
        }

        private static Sprite _pixel, _crateSprite;
        private static Sprite Pixel()
        {
            if (_pixel != null) return _pixel;
            var t = new Texture2D(2, 2) { filterMode = FilterMode.Point };
            t.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            t.Apply();
            _pixel = Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(0f, 0.5f), 2f);
            return _pixel;
        }

        private static Sprite MakeCrateSprite()
        {
            if (_crateSprite != null) return _crateSprite;
            const int n = 32;
            var t = new Texture2D(n, n) { filterMode = FilterMode.Point };
            var px = new Color[n * n];
            Color wood = new Color(0.62f, 0.42f, 0.22f), dark = new Color(0.35f, 0.22f, 0.1f), band = new Color(0.85f, 0.75f, 0.3f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool edge = x < 2 || y < 2 || x >= n - 2 || y >= n - 2;
                    bool diag = Mathf.Abs(x - y) < 2 || Mathf.Abs(x - (n - 1 - y)) < 2;
                    bool strap = y >= n / 2 - 2 && y < n / 2 + 2;
                    px[y * n + x] = edge ? dark : strap ? band : diag ? dark : wood;
                }
            t.SetPixels(px);
            t.Apply();
            _crateSprite = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return _crateSprite;
        }
    }
}

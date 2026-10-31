using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;

namespace HillNGun.Cover
{
    /// <summary>View for a CoverInstance: sprite per damage stage, placed via FieldView.</summary>
    public class CoverView : MonoBehaviour
    {
        private CoverInstance _cover;
        private SpriteRenderer _renderer;
        private CoverSprites _sprites;
        private float _baseScale = 1f;

        public CoverInstance Cover => _cover;
        public SpriteRenderer Renderer => _renderer;

        public void Initialize(CoverInstance cover)
        {
            _cover = cover;
            _renderer = gameObject.AddComponent<SpriteRenderer>();

            var lib = ServiceLocator.Get<SpriteLibrary>();
            _sprites = lib?.GetCoverSprites(cover.Data.id);

            Sprite s = _sprites?.intact;
            if (s == null) s = CreateFallbackSprite();
            _renderer.sprite = s;

            // Scale sprite so its width matches the logical cover width
            float spriteWidth = s.bounds.size.x;
            _baseScale = spriteWidth > 0.01f ? cover.Data.width / spriteWidth : 1f;
            _baseScale = Mathf.Clamp(_baseScale, 0.3f, 4f);

            FieldView.Place(transform, cover.Position, _baseScale);
            FieldView.ApplySorting(_renderer, cover.Position.y);

            cover.OnStageChanged += HandleStageChanged;
            cover.OnDamaged += HandleDamaged;
        }

        private void HandleStageChanged(CoverInstance c, CoverStage stage)
        {
            Refresh();
        }

        private void HandleDamaged(CoverInstance c, int dmg)
        {
            if (_renderer != null) StartCoroutine(Flash());
        }

        private System.Collections.IEnumerator Flash()
        {
            _renderer.color = new Color(1f, 0.85f, 0.7f);
            yield return new WaitForSeconds(0.05f);
            Refresh();
        }

        private void Update()
        {
            // Covers can be reset between rounds; keep visuals in sync cheaply
            if (_cover != null && Time.frameCount % 15 == 0) Refresh();
        }

        private void Refresh()
        {
            if (_cover == null || _renderer == null) return;
            Sprite s = _sprites?.GetSpriteForStage(_cover.Stage);
            if (s != null) _renderer.sprite = s;
            _renderer.color = _cover.IsDestroyed ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
        }

        private Sprite CreateFallbackSprite()
        {
            const int w = 32, h = 20;
            var tex = new Texture2D(w, h) { filterMode = FilterMode.Point };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                        ? new Color(0.3f, 0.2f, 0.1f) : new Color(0.6f, 0.4f, 0.2f);
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        }

        private void OnDestroy()
        {
            if (_cover != null)
            {
                _cover.OnStageChanged -= HandleStageChanged;
                _cover.OnDamaged -= HandleDamaged;
            }
        }
    }
}

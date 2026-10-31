using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;
using HillNGun.View3D;
using HillNGun.Feel;

namespace HillNGun.Cover
{
    /// <summary>View for a CoverInstance: sprite per damage stage, placed via FieldView.</summary>
    public class CoverView : MonoBehaviour
    {
        private CoverInstance _cover;
        private SpriteRenderer _renderer;
        private CoverSprites _sprites;
        private float _baseScale = 1f;
        private CoverProp3D _prop3D;
        private CoverStage _lastStage;
        public CoverProp3D Prop3D => _prop3D;

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

            if (Actor3DStage.Enabled && Actor3DStage.Data.useCover3D)
            {
                Actor3DStage.Ensure();
                _prop3D = new CoverProp3D(cover.Data.id, cover.Data.width, Actor3DStage.Data.actorLayer, Actor3DStage.Material);
                _prop3D.SetStage(cover.Stage);
                _lastStage = cover.Stage;
                _renderer.enabled = false;
                Place3D(0f);
            }

            cover.OnStageChanged += HandleStageChanged;
            cover.OnDamaged += HandleDamaged;
        }

        private void HandleStageChanged(CoverInstance c, CoverStage stage)
        {
            Refresh();
            if (_prop3D != null)
            {
                _prop3D.Hit(0.5f, 1f);
                var fx = MotionFx.Instance;
                if (fx != null && stage > _lastStage) fx.Dust(Base3D(), Vector2.up, stage == CoverStage.Destroyed ? 8 : 4, 1.2f, FieldView.DepthOrder(c.Position.y) + 1);
                _lastStage = stage;
            }
        }

        private Vector3 Base3D()
        {
            var w = FieldView.ToWorld(_cover.Position);
            float s = FieldView.PerspectiveScale(_cover.Position.y);
            return new Vector3(w.x, w.y - Actor3DStage.Data.coverFootOffset * s, 0f);
        }

        private void Place3D(float dt)
        {
            float s = FieldView.PerspectiveScale(_cover.Position.y) * Actor3DStage.Data.coverScale;
            _prop3D.Place(Base3D(), s, _cover.Position.y, dt);
        }

        private void HandleDamaged(CoverInstance c, int dmg)
        {
            if (_prop3D != null) { _prop3D.Hit(0.45f, 0.35f); return; }
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
            if (_prop3D != null && _cover != null)
            {
                _prop3D.SetStage(_cover.Stage);
                if (_cover.Stage < _lastStage) _lastStage = _cover.Stage; // round reset / repair
                Place3D(Time.deltaTime);
            }
        }

        private void Refresh()
        {
            if (_cover == null || _renderer == null) return;
            Sprite s = _sprites?.GetSpriteForStage(_cover.Stage);
            if (s != null) _renderer.sprite = s;
            _renderer.color = _cover.IsDestroyed ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            if (_prop3D != null) { _renderer.enabled = false; _prop3D.SetStage(_cover.Stage); }
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

        private void OnEnable() { _prop3D?.SetVisible(true); }
        private void OnDisable() { _prop3D?.SetVisible(false); }

        private void OnDestroy()
        {
            _prop3D?.Destroy();
            _prop3D = null;
            if (_cover != null)
            {
                _cover.OnStageChanged -= HandleStageChanged;
                _cover.OnDamaged -= HandleDamaged;
            }
        }
    }
}

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
        private bool _shadowRegistered;
        private CoverShadowLayer.Entry _shadowEntry;
        private int _themeVersion = -1;
        private static readonly Color DefaultShadow = new Color(0.05f, 0.03f, 0.02f, 0.38f);
        private SpriteLibrary _lib;
        // Painted cover art (Art/Covers/cover_*.png): bottom-centre pivot, sized to the scale-preset box (view only)
        private bool _artSprite;
        private bool _artOnActorLayer;
        private Vector2 _artScale = Vector2.one;
        private float _artDrawnWidth;
        private float _shake;
        public CoverProp3D Prop3D => _prop3D;

        public CoverInstance Cover => _cover;
        public SpriteRenderer Renderer => _renderer;

        public void Initialize(CoverInstance cover)
        {
            _cover = cover;
            _renderer = gameObject.AddComponent<SpriteRenderer>();

            var lib = ServiceLocator.Get<SpriteLibrary>();
            _lib = lib;
            _sprites = lib?.GetCoverSprites(cover.Data.id);
            if (_sprites != null && _sprites.coverId != cover.Data.id) _sprites = null; // no art for this id: never borrow another cover's

            Sprite s = _sprites?.intact;
            if (s != null)
            {
                InitArtSprite(s);
                ApplyTheme();
                cover.OnStageChanged += HandleStageChanged;
                cover.OnDamaged += HandleDamaged;
                return;
            }
            s = CreateFallbackSprite();
            _renderer.sprite = s;

            // Scale sprite so its width matches the logical cover width
            float spriteWidth = s.bounds.size.x;
            _baseScale = spriteWidth > 0.01f ? cover.Size.visualWidth / spriteWidth : 1f; // scale preset drawn width
            _baseScale = Mathf.Clamp(_baseScale, 0.3f, 4f);

            FieldView.Place(transform, cover.Position, _baseScale);
            FieldView.ApplySorting(_renderer, cover.Position.y);

            if (Actor3DStage.Enabled && Actor3DStage.Data.useCover3D)
            {
                Actor3DStage.Ensure();
                _prop3D = new CoverProp3D(cover.Data.id, cover.Data.width, Actor3DStage.Data.actorLayer, Actor3DStage.Material);
                var sz = cover.Size;
                _prop3D.SetTargetSize(sz.visualWidth, sz.visualHeight, ScalePresets.Data != null ? ScalePresets.Data.minCoverYScale : 0.25f);
                _prop3D.SetStage(cover.Stage);
                _lastStage = cover.Stage;
                _renderer.enabled = false;
                Place3D(0f);
            }

            ApplyTheme();

            cover.OnStageChanged += HandleStageChanged;
            cover.OnDamaged += HandleDamaged;
        }

        /// <summary>
        /// Painted cover art: the PNG is scaled to the scale preset's drawn box (height = heightH x H exactly, width as
        /// close to widthH x H as <see cref="SpriteLibrary.coverSpriteMaxStretch"/> allows without distorting the art).
        /// On the 3D actor layer it stands at the cover's depth with the same view tilt as the toon models, so a
        /// character behind it is hidden by depth test exactly like the old 3D props. Collision / protection never
        /// read any of this (CoverInstance footprint only).
        /// </summary>
        private void InitArtSprite(Sprite s)
        {
            _artSprite = true;
            _renderer.sprite = s;
            var sz = _cover.Size;
            float artW = Mathf.Max(0.001f, s.bounds.size.x), artH = Mathf.Max(0.001f, s.bounds.size.y);
            float targetW = sz.visualWidth > 0.01f ? sz.visualWidth : _cover.Data.width;
            float targetH = sz.visualHeight > 0.01f ? sz.visualHeight : targetW * artH / artW;
            float sy = targetH / artH;
            // SpriteLibrary.coverSpriteMaxStretch (default 1.35) caps non-wagon distortion.
            // Wagon: wide handoff art (cover_wagon_small.png) must hit ScalePreset width×height exactly — no stretch cap.
            bool exactBox = _cover != null && _cover.Data != null && _cover.Data.id == "cover_wagon";
            float maxStretch = exactBox ? 100f : (_lib != null ? Mathf.Max(1f, _lib.coverSpriteMaxStretch) : 1f);
            float sx = exactBox ? (targetW / artW) : Mathf.Clamp(targetW / artW, sy / maxStretch, sy * maxStretch);
            _artDrawnWidth = sx * artW;

            _artOnActorLayer = _lib != null && _lib.coverSpritesOnActorLayer && Actor3DStage.Enabled;
            if (_artOnActorLayer)
            {
                Actor3DStage.Ensure();
                gameObject.layer = Actor3DStage.Data.actorLayer;
                // ortho view of a plane tilted back by viewTiltDeg is cos(tilt) shorter: compensate so the drawn height is exact
                float cos = Mathf.Cos(Actor3DStage.Data.viewTiltDeg * Mathf.Deg2Rad);
                sy /= Mathf.Max(0.2f, cos);
            }
            _artScale = new Vector2(sx, sy);
            _baseScale = sx;
            FieldView.ApplySorting(_renderer, _cover.Position.y);
            PlaceArt(0f);
            Refresh();
        }

        private Vector3 ArtBase()
        {
            var w = FieldView.ToWorld(_cover.Position);
            float s = FieldView.PerspectiveScale(_cover.Position.y);
            return new Vector3(w.x, w.y - Actor3DStage.Data.coverFootOffset * s, 0f);
        }

        private void PlaceArt(float dt)
        {
            float s = FieldView.PerspectiveScale(_cover.Position.y);
            var b = ArtBase();
            _shake = Mathf.Max(0f, _shake - dt * 4f);
            float jx = _shake > 0f ? Mathf.Sin(Time.time * 70f) * _shake * 0.08f * s : 0f;
            if (_artOnActorLayer)
            {
                var d = Actor3DStage.Data;
                // Pull toward camera so a character tucked just south is behind the cover in the depth buffer.
                transform.position = new Vector3(b.x + jx, b.y, d.depthBase + _cover.Position.y * d.depthPerTile - d.coverDepthPull);
                transform.rotation = Quaternion.Euler(-d.viewTiltDeg, 0f, 0f);
            }
            else
            {
                transform.position = new Vector3(b.x + jx, b.y, 0f);
                transform.rotation = Quaternion.identity;
            }
            transform.localScale = new Vector3(_artScale.x * s, _artScale.y * s, 1f);
        }

        private void HandleStageChanged(CoverInstance c, CoverStage stage)
        {
            Refresh();
            if (_artSprite)
            {
                _shake = 1f;
                var fx = MotionFx.Instance;
                if (fx != null && stage > _lastStage) fx.Dust(ArtBase(), Vector2.up, stage == CoverStage.Destroyed ? 8 : 4, 1.2f, FieldView.DepthOrder(c.Position.y) + 1);
                _lastStage = stage;
            }
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
            if (_artSprite) _shake = Mathf.Max(_shake, 0.35f);
            if (_renderer != null) StartCoroutine(Flash());
        }

        private System.Collections.IEnumerator Flash()
        {
            var t = StageTint(_cover.Stage);
            _renderer.color = new Color(1f, 0.85f, 0.7f, t.a);
            yield return new WaitForSeconds(0.05f);
            Refresh();
        }

        /// <summary>
        /// Stage theme (view only): picks the Western prop look for this cover (barrel / crate / hay bale / rock /
        /// fence ...) and the shared ground-shadow tone. The cover's width, hit box and HP are untouched.
        /// </summary>
        private void ApplyTheme()
        {
            _themeVersion = HillNGun.Scenery.ThemeContext.Version;
            var theme = HillNGun.Scenery.ThemeContext.Current;
            if (_prop3D != null)
            {
                var v = HillNGun.Scenery.ThemeContext.CoverVisualFor(_cover.Data.id, _cover.Position);
                _prop3D.SetVisual(CoverProp3D.VisualKey(_cover.Data.id, v), v == CoverVisual.Rock && theme != null ? theme.rockTint : Color.white);
            }
            if (!_shadowRegistered && !HillNGun.Scenery.ThemeContext.ForceOff)
            {
                CoverShadowLayer.Register(this);
                _shadowRegistered = true;
            }
            PlaceShadow();
        }

        /// <summary>This cover's ground shadow (drawn by CoverShadowLayer as one mesh for all covers).</summary>
        public CoverShadowLayer.Entry ShadowEntry => _shadowEntry;

        private void PlaceShadow()
        {
            if (!_shadowRegistered || _cover == null) return;
            float s = FieldView.PerspectiveScale(_cover.Position.y);
            Vector3 basePos;
            if (_prop3D != null) basePos = Base3D();
            else if (_artSprite) basePos = ArtBase();
            else
            {
                var w = FieldView.ToWorld(_cover.Position);
                float half = _renderer != null && _renderer.sprite != null ? _renderer.sprite.bounds.extents.y * _baseScale * s : 0.4f * s;
                basePos = new Vector3(w.x, w.y - half + 0.05f * s, 0f);
            }
            var c = HillNGun.Scenery.ThemeContext.GroundShadowOr(DefaultShadow);
            if (_cover.IsDestroyed) c.a *= 0.45f;
            _shadowEntry = new CoverShadowLayer.Entry
            {
                center = basePos + new Vector3(0.1f * s, 0.03f * s, 0f),
                size = new Vector2((_artSprite ? _artDrawnWidth : _cover.Size.visualWidth) * s * 1.12f, 0.42f * s * ScalePresets.VisualMul),
                color = c,
                visible = true
            };
        }

        private void Update()
        {
            if (_cover != null && _themeVersion != HillNGun.Scenery.ThemeContext.Version) ApplyTheme();
            // Covers can be reset between rounds; keep visuals in sync cheaply
            if (_cover != null && Time.frameCount % 15 == 0) Refresh();
            if (_artSprite && _cover != null)
            {
                if (_cover.Stage < _lastStage) _lastStage = _cover.Stage; // round reset / repair
                PlaceArt(Time.deltaTime);
            }
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
            Sprite s = StageSprite(_cover.Stage);
            if (s != null) _renderer.sprite = s;
            _renderer.color = _artSprite
                ? StageTint(_cover.Stage)
                : HillNGun.Scenery.TimeOfDayGrade.Apply(_cover.IsDestroyed ? new Color(1f, 1f, 1f, 0.45f) : Color.white);
            PlaceShadow();
            if (_prop3D != null) { _renderer.enabled = false; _prop3D.SetStage(_cover.Stage); }
        }


        /// <summary>No dedicated damage art yet: one painted sprite, darkened / faded per damage stage (SpriteLibrary data).
        /// A stage that does have its own sprite keeps its colours (white).</summary>
        private Color StageTint(CoverStage stage)
        {
            if (_lib == null) return stage == CoverStage.Destroyed ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            bool dedicated = stage != CoverStage.Intact && _sprites != null && _sprites.GetSpriteForStage(stage) != null;
            var t = _lib.CoverStageTint(stage);
            var c = dedicated ? new Color(1f, 1f, 1f, t.a) : t;
            return HillNGun.Scenery.TimeOfDayGrade.Apply(c);
        }

        private Sprite StageSprite(CoverStage stage)
        {
            if (_sprites == null) return null;
            Sprite s = _sprites.GetSpriteForStage(stage);
            if (s != null) return s;
            // 3-state art may omit cracked: fall back along the damage ladder
            if (stage == CoverStage.Cracked) return _sprites.damaged != null ? _sprites.damaged : _sprites.intact;
            if (stage == CoverStage.Damaged) return _sprites.cracked != null ? _sprites.cracked : _sprites.intact;
            return _sprites.intact;
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
            if (_shadowRegistered) CoverShadowLayer.Unregister(this);
            if (_cover != null)
            {
                _cover.OnStageChanged -= HandleStageChanged;
                _cover.OnDamaged -= HandleDamaged;
            }
        }
    }
}

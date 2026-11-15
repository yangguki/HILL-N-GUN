using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;
using HillNGun.Feel;
using HillNGun.View3D;

namespace HillNGun.PvE
{
    public class EnemyView : MonoBehaviour
    {
        private EnemyInstance _enemy;
        private SpriteRenderer _spriteRenderer;
        private TextMesh _nameLabel;
        private BattlefieldSystem _battlefield;
        private SpriteLibrary _spriteLibrary;
        private GameObject _warningIndicator;
        private bool _isShowingWarning;
        private MeshRenderer _labelRenderer;
        private SpriteRenderer _warnRenderer;
        private MotionRig _rig;
        private Vector2 _lastPos;
        private bool _hasLastPos;
        public SpriteRenderer BodyRenderer => _spriteRenderer;
        private Actor3D _actor3D;
        private Vector2 _aimDir = Vector2.down;
        private float _aimTime = -99f;
        private float _foot3D;
        public Actor3D Actor3D => _actor3D;
        public EnemyInstance Enemy => _enemy;

        public void Initialize(EnemyInstance enemy)
        {
            _enemy = enemy;
            _battlefield = ServiceLocator.Get<BattlefieldSystem>();
            _spriteLibrary = ServiceLocator.Get<SpriteLibrary>();

            SetupVisuals();
            SubscribeEvents();
            UpdatePosition();
        }

        private void SetupVisuals()
        {
            Sprite enemySprite = null;
            if (_spriteLibrary != null && _enemy.Data != null)
            {
                var sprites = _spriteLibrary.GetEnemySprites(_enemy.Data.ArtId); // roles may reuse existing art (sniper = pistol art + tint)
                if (sprites != null)
                {
                    enemySprite = sprites.idle;
                }
            }

            // Body on a child rig (soft shadow + feet pivot) so bounce/lean never move the label/warning
            var md = MotionFx.Data;
            MotionFx.MeasureFeet(enemySprite, -0.5f, 0.9f, out float foot, out float width);
            float sw = width * md.enemyShadowWidth;
            _rig = new MotionRig(transform, foot, new Vector2(sw, sw * md.shadowAspect), md.enemyArtFacesRight);
            _rig.FxScale = 0.8f;
            _spriteRenderer = _rig.Renderer;

            if (enemySprite != null)
            {
                _spriteRenderer.sprite = enemySprite;
            }
            else
            {
                _spriteRenderer.sprite = CreatePlaceholderSprite();
            }

            _baseColor = HillNGun.Scenery.TimeOfDayGrade.Apply(enemySprite != null ? (_enemy.Data != null ? _enemy.Data.roleTint : Color.white) : (_enemy.Data?.baseColor ?? new Color(0.7f, 0.5f, 0.3f)));
            _spriteRenderer.color = _baseColor;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform);
            float headTop = MotionFx.MeasureTop(enemySprite, 0.6f);
            labelGo.transform.localPosition = new Vector3(0f, headTop + 0.22f, 0f); // above the enlarged head
            labelGo.transform.localScale = Vector3.one / Mathf.Max(0.1f, ScalePresets.VisualMul);
            _nameLabel = labelGo.AddComponent<TextMesh>();
            _nameLabel.text = _enemy.Data?.id ?? "Enemy";
            _nameLabel.fontSize = 28;
            _nameLabel.characterSize = 0.05f;
            _nameLabel.anchor = TextAnchor.MiddleCenter;
            _nameLabel.alignment = TextAlignment.Center;
            _nameLabel.color = new Color(1f, 0.55f, 0.45f);
            _labelRenderer = labelGo.GetComponent<MeshRenderer>();
            if (HillNGun.DebugTools.PerfDiag.NoLabels) labelGo.SetActive(false); // attribution runs only

            var warningGo = new GameObject("Warning");
            warningGo.transform.SetParent(transform);
            warningGo.transform.localPosition = new Vector3(0f, headTop + 0.6f, 0f);
            var warnSr = warningGo.AddComponent<SpriteRenderer>();
            warnSr.sprite = CreateWarningSprite();
            warnSr.sortingOrder = 15;
            warnSr.color = Color.yellow;
            _warningIndicator = warningGo;
            _warnRenderer = warnSr;
            _warningIndicator.SetActive(false);
            _headTop = headTop;
            if (_enemy.Data != null && _enemy.Data.throwsExplosive)
            {   // v17: dynamite thrower holds up a lit stick during its EXISTING warning (EnemyData.warningTime): view only
                var hd = new GameObject("HeldDynamite"); hd.transform.SetParent(transform, false);
                _heldDyn = hd.AddComponent<SpriteRenderer>(); _heldDyn.sprite = DynamiteSprite(); _heldDyn.enabled = false;
            }

            if (_enemy.FreeMove) SetupEntranceVisuals(warnSr.sprite);

            _foot3D = foot;
            if (_enemy.Data != null && Actor3DStage.Enabled)
            {
                _actor3D = Actor3DStage.Create(_enemy.Data.id, null, "Actor3D_" + _enemy.Data.id);
                if (_actor3D != null)
                {
                    _spriteRenderer.enabled = false;
                    var d = Actor3DStage.Data;
                    float top = foot + _actor3D.Rig.Height * d.modelScale * d.enemyScaleMul * Mathf.Cos(d.viewTiltDeg * Mathf.Deg2Rad);
                    labelGo.transform.localPosition = new Vector3(0f, top + 0.25f, 0f);
                    warningGo.transform.localPosition = new Vector3(0f, top + 0.65f, 0f);
                }
            }
        }

        // ---- FreeMove entrance / sniper visuals (view only) ----
        private Color _baseColor = Color.white;
        private GameObject _telegraph;
        private SpriteRenderer _telegraphSr;
        private LineRenderer _sightLine;
        private bool _entranceDustDone;
        private static Material _lineMat;

        private GameObject _doorway;
        private SpriteRenderer _doorwaySr;
        private static Sprite _doorwaySprite;

        /// <summary>Soft arched dark opening (white, tinted at runtime): the door / alley 'opens' during the telegraph.</summary>
        private static Sprite DoorwaySprite()
        {
            if (_doorwaySprite != null) return _doorwaySprite;
            const int w = 16, h = 32;
            var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w * 2f - 1f, ny = (y + 0.5f) / h;
                    float edge = 1f - Mathf.Abs(nx);
                    if (ny > 0.75f) { float ay = (ny - 0.75f) / 0.25f; edge = Mathf.Min(edge, 1f - Mathf.Sqrt(nx * nx + ay * ay)); }
                    float a = Mathf.Clamp01(edge * 4f) * Mathf.Clamp01(ny * 6f);
                    px[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px); tex.Apply();
            _doorwaySprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
            _doorwaySprite.name = "entrance_doorway";
            return _doorwaySprite;
        }

        private bool _designerDoor; private EntranceDoor _door; private bool _doorCrossed, _doorRatioLogged;
        /// <summary>v15 doorway kit: the enemy's door-relative visual state this frame (for tests / capture QA).</summary>
        public bool DoorInside { get; private set; }
        public int DoorSortOrder { get; private set; }
        public float DoorFeetY { get; private set; }
        public bool DoorInsideKnown => _door != null && _door.Layered;

        /// <summary>v15 doorway kit: 0.4 s VISUAL walk-out (logic entrance 0.5 s / invulnerability / telegraph / attack lockout untouched).
        /// inside start (opening centre, behind the patch) -> just past the floor threshold (cross fraction) -> logic post.</summary>
        /// <param name="footWorld">feet y relative to the actor centre in world units (negative): the image floor registration.</param>
        public static Vector3 DoorVisualPos(EntranceDoor d, Vector3 post, float footWorld, float elapsed)
        {
            var e = d.Engine; float wt = Mathf.Max(0.05f, e.visualWalkTime), cf = Mathf.Clamp(e.crossFraction, 0.05f, 0.95f);
            float v = Mathf.Clamp01(elapsed / wt);
            var a = d.InsideStartWorld - new Vector3(0f, footWorld, 0f); var c = d.CrossWorld - new Vector3(0f, footWorld, 0f);
            return v < cf ? Vector3.Lerp(a, c, v / cf) : Vector3.Lerp(c, post, (v - cf) / (1f - cf));
        }
        /// <summary>Threshold rule from the kit manifest: inside (z1, behind patch/door/frame) while the feet are above the floor anchor.</summary>
        public static bool DoorIsInside(EntranceDoor d, float feetY) => feetY > d.AnchorWorld.y + 1e-4f;
        /// <summary>v13 QA: entrance alpha ramp share of the walk. Designer doors: near-instant (a translucent body over the opaque
        /// dark 'open' frame read as a grey box); overlay doorway / alley: the old 0.35 fade out of the shadow.</summary>
        /// <summary>Tight visible height of the sprite mesh in world units (uniform scale).</summary>
        public static float VisualHeightWorld(SpriteRenderer sr)
        {
            var v = sr.sprite.vertices; if (v == null || v.Length == 0) return sr.bounds.size.y;
            float lo = float.MaxValue, hi = float.MinValue; foreach (var q in v) { lo = Mathf.Min(lo, q.y); hi = Mathf.Max(hi, q.y); }
            return (hi - lo) * Mathf.Abs(sr.transform.lossyScale.y);
        }
        public static float EntranceFadeShare(bool designerDoor) => designerDoor ? 0.08f : 0.35f;

        private void SetupEntranceVisuals(Sprite warnSprite)
        {
            var sp = _enemy.SpawnPoint;
            var door = sp != null && sp.type == SpawnPointType.StorefrontDoor ? EntranceDoors.For(sp) : null;
            if (door != null) { door.Register(_enemy); _designerDoor = true; _door = door; } // v13: designer door sprite animates instead of the dark overlay (no stacking)
            else if (sp != null && (sp.type == SpawnPointType.StorefrontDoor || sp.type == SpawnPointType.AlleyGap))
            {
                _doorway = new GameObject("DoorwayShadow");
                _doorway.transform.SetParent(transform.parent, false);
                _doorwaySr = _doorway.AddComponent<SpriteRenderer>();
                _doorwaySr.sprite = DoorwaySprite();
                _doorwaySr.color = new Color(0.07f, 0.04f, 0.03f, 0f);
                _doorwaySr.sortingOrder = FieldView.DepthOrder(sp.entranceStart.y + 1f); // over the painted facade, under actors / covers in front of it
                _doorway.transform.position = FieldView.ToWorld(sp.entranceStart) + new Vector3(0f, -0.15f, 0f);
                _doorway.SetActive(false);
            }
            if (sp != null && sp.telegraphTime > 0f)
            {
                _telegraph = new GameObject("EntranceTelegraph");
                _telegraph.transform.SetParent(transform.parent, false);
                _telegraphSr = _telegraph.AddComponent<SpriteRenderer>();
                _telegraphSr.sprite = warnSprite;
                _telegraphSr.color = new Color(1f, 0.55f, 0.1f);
                _telegraphSr.sortingOrder = FieldView.OverlayOrder + 2;
            }
            if (_enemy.Data != null && _enemy.Data.sightLineTelegraph)
            {
                var go = new GameObject("SightLine");
                go.transform.SetParent(transform, false);
                _sightLine = go.AddComponent<LineRenderer>();
                if (_lineMat == null) _lineMat = new Material(Shader.Find("Sprites/Default")) { name = "SniperSight_shared" };
                _sightLine.sharedMaterial = _lineMat;
                _sightLine.positionCount = 2;
                _sightLine.useWorldSpace = true;
                _sightLine.startWidth = 0.05f; _sightLine.endWidth = 0.02f;
                _sightLine.startColor = new Color(1f, 0.2f, 0.15f, 0.85f);
                _sightLine.endColor = new Color(1f, 0.2f, 0.15f, 0.15f);
                _sightLine.sortingOrder = FieldView.OverlayOrder - 5;
                _sightLine.enabled = false;
            }
        }

        /// <summary>Entrance pose: hidden during the telegraph, fade/hop/rise on the path, then normal.</summary>
        private void UpdateEntranceVisuals(out float yOffset, out float alpha)
        {
            yOffset = 0f; alpha = 1f;
            if (!_enemy.FreeMove) return;
            var sp = _enemy.SpawnPoint;
            bool telegraph = _enemy.Entrance == EntrancePhase.Telegraph;
            if (_telegraph != null)
            {
                _telegraph.SetActive(telegraph && _enemy.IsAlive);
                if (telegraph && sp != null)
                {
                    // '!' pops + shakes over the door / window (0.8 s door telegraph)
                    Vector3 w = FieldView.ToWorld(sp.TelegraphPos);
                    float t = _enemy.TelegraphProgress;
                    float shake = Mathf.Sin(Time.time * 55f) * 0.07f * (0.4f + t);
                    float pop = 1.1f + 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 9f));
                    _telegraph.transform.position = w + new Vector3(shake, 1.3f, 0f);
                    _telegraph.transform.localScale = Vector3.one * pop * 2.0f;
                }
            }
            if (_doorway != null)
            {
                // door-open cue: the opening widens + darkens during the telegraph, fades as the enemy steps out
                float open = telegraph ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_enemy.TelegraphProgress / 0.6f))
                           : _enemy.Entrance == EntrancePhase.Walking ? 1f - Mathf.Clamp01(_enemy.EntranceProgress) : 0f;
                bool on = open > 0.01f && _enemy.IsAlive;
                _doorway.SetActive(on);
                if (on)
                {
                    float jolt = telegraph ? Mathf.Sin(Time.time * 47f) * 0.04f * (1f - open) : 0f;
                    _doorway.transform.localScale = new Vector3(Mathf.Lerp(0.3f, 1.25f, open) * (1f + jolt), 1.1f, 1f);
                    var c = _doorwaySr.color; c.a = 0.78f * open; _doorwaySr.color = c;
                }
            }
            if (telegraph) { alpha = 0f; return; }
            if (_enemy.Entrance != EntrancePhase.Walking || sp == null) return;
            float p = _enemy.EntranceProgress;
            if (!_entranceDustDone && _rig != null) { _entranceDustDone = true; _rig.Dust(sp.direction, 3); }
            switch (sp.type)
            {
                case SpawnPointType.StorefrontDoor:
                case SpawnPointType.AlleyGap:
                    alpha = _door != null && _door.Layered ? 1f // kit: occlusion by patch/door/frame reveals the body, no alpha ramp
                          : Mathf.Clamp01(p / EntranceFadeShare(_designerDoor)); break; // steps out of the door / doorway shadow
                case SpawnPointType.FenceClimb:
                    yOffset = Mathf.Sin(Mathf.PI * Mathf.Clamp01(p)) * 0.7f; // climbs over the front fence
                    alpha = Mathf.Clamp01(p / 0.2f); break;
                case SpawnPointType.BalconySniper:
                    alpha = Mathf.Clamp01(p / 0.6f); break; // rises into the window
            }
        }

        private void UpdateSightLine()
        {
            if (_sightLine == null) return;
            bool on = _enemy.IsAlive && _enemy.IsWindingUp;
            _sightLine.enabled = on;
            if (!on) return;
            Vector2 a = _enemy.Position;
            Vector2 b = a + _enemy.AttackDirection * Mathf.Min(_enemy.Data.attackRange, 18f);
            _sightLine.SetPosition(0, FieldView.ToWorld(a));
            _sightLine.SetPosition(1, FieldView.ToWorld(b));
            float k = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 20f));
            _sightLine.startWidth = 0.06f * k;
        }

        private Sprite CreatePlaceholderSprite()
        {
            int size = 48;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Color mainColor = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < size / 2f - 2)
                        pixels[y * size + x] = mainColor;
                    else if (dist < size / 2f)
                        pixels[y * size + x] = Color.black;
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48f);
        }

        private static Sprite _warnSpriteShared;

        /// <summary>
        /// '!' glyph (bar on top, dot below; texture rows grow upward) with a dark outline so it reads on any BG.
        /// v11 fix: the old glyph was upside down (dot above the bar). Shared by all enemy views (one texture).
        /// </summary>
        private static Sprite CreateWarningSprite()
        {
            if (_warnSpriteShared != null) return _warnSpriteShared;
            const int size = 32;
            var texture = new Texture2D(size, size) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var mask = new bool[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size - 0.5f;
                    float ny = (y + 0.5f) / size - 0.5f;
                    float half = Mathf.Lerp(0.07f, 0.12f, Mathf.InverseLerp(-0.08f, 0.38f, ny)); // tapered bar, wider at the top
                    bool bar = Mathf.Abs(nx) < half && ny > -0.08f && ny < 0.38f;
                    bool dot = nx * nx + (ny + 0.27f) * (ny + 0.27f) < 0.085f * 0.085f;
                    mask[y * size + x] = bar || dot;
                }
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    if (mask[y * size + x]) { pixels[y * size + x] = Color.white; continue; }
                    bool edge = false;
                    for (int dy = -2; dy <= 2 && !edge; dy++)
                        for (int dx = -2; dx <= 2 && !edge; dx++)
                        {
                            int xx = x + dx, yy = y + dy;
                            if (xx >= 0 && yy >= 0 && xx < size && yy < size && mask[yy * size + xx] && dx * dx + dy * dy <= 5) edge = true;
                        }
                    pixels[y * size + x] = edge ? new Color(0.08f, 0.04f, 0.02f, 0.95f) : Color.clear;
                }
            texture.SetPixels(pixels);
            texture.Apply();
            _warnSpriteShared = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            _warnSpriteShared.name = "enemy_warning_bang";
            return _warnSpriteShared;
        }

        private void SubscribeEvents()
        {
            _enemy.OnWarningStart += HandleWarningStart;
            _enemy.OnDamageTaken += HandleDamage;
            _enemy.OnDefeated += HandleDefeated;
            _enemy.OnAttack += HandleAttack;
            _enemy.OnExplosiveThrown += HandleThrow;
        }

        private void HandleAttack(EnemyInstance e, Vector2 dir, int dmg)
        {
            if (dir.sqrMagnitude > 0.01f) _aimDir = dir.normalized;
            _aimTime = Time.time;
            _actor3D?.Fire();
        }

        private void HandleThrow(EnemyInstance e, Vector2 dir, int dmg)
        {
            if (dir.sqrMagnitude > 0.01f) _aimDir = dir.normalized;
            _actor3D?.Throw();
        }

        private void HandleWarningStart(EnemyInstance enemy)
        {
            _isShowingWarning = true;
            _warningIndicator?.SetActive(true);
        }

        public bool WarningVisible => _warningIndicator != null && _warningIndicator.activeSelf;

        private void HandleDamage(EnemyInstance enemy, int damage, Vector2 direction)
        {
            if (_spriteRenderer != null && _actor3D == null)
            {
                StartCoroutine(FlashWhite());
            }
            _actor3D?.Hit();
        }

        private bool _flashing;
        private System.Collections.IEnumerator FlashWhite()
        {
            Color original = _spriteRenderer.color;
            _flashing = true;
            _spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            _flashing = false;
            _spriteRenderer.color = original;
        }

        private void HandleDefeated(EnemyInstance enemy)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            _isShowingWarning = false;
            _warningIndicator?.SetActive(false);
            if (_telegraph != null) _telegraph.SetActive(false);
            if (_doorway != null) _doorway.SetActive(false);
        }

        private void Update()
        {
            if (_enemy == null) return;

            UpdatePosition();

            // '!' stays up for the whole wind-up (was hidden on the next frame = unreadable warning)
            bool warn = _enemy.IsAlive && _enemy.IsWindingUp;
            if (warn != _isShowingWarning)
            {
                _isShowingWarning = warn;
                _warningIndicator?.SetActive(warn);
            }
            UpdateSightLine();
            UpdateHeldDynamite(warn);

            if (!_enemy.IsAlive || _enemy.IsFleeing)
            {
                if (_enemy.IsOffScreen(25f))
                {
                    Destroy(gameObject);
                }
            }
        }

        // ---- v17 dynamite wind-up visual (no logic: the throw still happens at the end of EnemyData.warningTime) ----
        private SpriteRenderer _heldDyn; private float _headTop, _warnT;
        public bool HeldDynamiteVisible => _heldDyn != null && _heldDyn.enabled;
        public float HeldDynamiteAge => _warnT;
        private void UpdateHeldDynamite(bool warn)
        {
            if (_heldDyn == null) return;
            bool show = warn && _enemy.IsBodyVisible && !_enemy.IsDefeated;
            _warnT = show ? _warnT + GameTime.DeltaTime : 0f;
            if (_heldDyn.enabled != show) _heldDyn.enabled = show;
            if (!show) return;
            float face = _rig != null ? _rig.FacingSign : 1f;
            float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_warnT / 0.25f)); // raised behind the head over 0.25 s
            float wob = Mathf.Sin(_warnT * 30f) * 6f;
            _heldDyn.transform.localPosition = new Vector3(-face * Mathf.Lerp(0.15f, 0.32f, up), Mathf.Lerp(_headTop * 0.55f, _headTop + 0.05f, up), 0f);
            _heldDyn.transform.localRotation = Quaternion.Euler(0f, 0f, face * (25f + wob));
            _heldDyn.transform.localScale = Vector3.one * 0.9f;
            _heldDyn.sortingOrder = (_spriteRenderer != null ? _spriteRenderer.sortingOrder : 0) + 1;
            var c = Color.white; c.a = _spriteRenderer != null ? _spriteRenderer.color.a : 1f; _heldDyn.color = c;
        }
        private static Sprite _dynSprite;
        private static Sprite DynamiteSprite()
        {
            if (_dynSprite != null) return _dynSprite;
            const int W = 12, H = 28; var t = new Texture2D(W, H) { filterMode = FilterMode.Point }; var px = new Color[W * H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                Color c = Color.clear;
                bool stick = x >= 2 && x <= 9 && y <= 19;
                if (stick) c = (x == 2 || x == 9 || y == 0 || y == 19) ? new Color(0.25f, 0.05f, 0.03f) : (y > 6 && y < 10 ? new Color(0.95f, 0.85f, 0.55f) : new Color(0.85f, 0.15f, 0.1f));
                if (x == 6 && y > 19 && y < 24) c = new Color(0.2f, 0.18f, 0.15f);              // fuse
                if (y >= 23 && Mathf.Abs(x - 6) + Mathf.Abs(y - 25) <= 2) c = new Color(1f, 0.85f, 0.2f); // spark
                px[y * W + x] = c;
            }
            t.SetPixels(px); t.Apply(); _dynSprite = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.2f), 40f); _dynSprite.name = "held_dynamite_v17";
            return _dynSprite;
        }

        private void UpdatePosition()
        {
            FieldView.Place(transform, _enemy.Position, (_enemy.Data != null ? _enemy.Data.scale : 1f) * ScalePresets.VisualMul); // view only; HitRadius uses CharacterMul
            UpdateEntranceVisuals(out float yOff, out float alpha);
            if (_enemy.FreeMove && _enemy.IsDefeated)
            {
                // defeat: sniper ducks down / fleeing enemy fades out on its way back (no falling, no gore)
                float t = _enemy.DefeatTime;
                if (_enemy.Data != null && _enemy.Data.stationary) { yOff = -0.6f * Mathf.Clamp01(t / 0.6f); alpha = 1f - Mathf.Clamp01(t / 0.6f); }
                else alpha = 0.5f * (1f - Mathf.Clamp01(t / 1.8f));
            }
            if (yOff != 0f) transform.position += new Vector3(0f, yOff * transform.localScale.y, 0f);
            bool doorWalk = _door != null && _door.Layered && _enemy.FreeMove && !_enemy.IsDefeated && _enemy.SpawnPoint != null
                            && (_enemy.Entrance == EntrancePhase.Walking || _enemy.Entrance == EntrancePhase.Telegraph);
            DoorInside = false;
            if (doorWalk)
            {
                float el = _enemy.Entrance == EntrancePhase.Walking ? _enemy.EntranceProgress * Mathf.Max(0.05f, _enemy.SpawnPoint.entranceTime) : 0f;
                if (_rig != null) _rig.SnapFacing(_enemy.SpawnPoint.position.x - _enemy.SpawnPoint.entranceStart.x); // no flip squash in the doorway (uniform figure)
                float footW = (_rig != null ? _rig.FootOffset : 0f) * transform.localScale.y;
                var vp = DoorVisualPos(_door, FieldView.ToWorld(_enemy.SpawnPoint.position), footW, el);
                transform.position = new Vector3(vp.x, vp.y, transform.position.z); // uniform perspective scale kept (no flattening)
                DoorFeetY = vp.y + footW;
                if (!_doorCrossed && !DoorIsInside(_door, DoorFeetY)) _doorCrossed = true;
                DoorInside = !_doorCrossed;
                if (!_doorRatioLogged && _enemy.Entrance == EntrancePhase.Walking && _spriteRenderer != null && _spriteRenderer.sprite != null)
                {
                    _doorRatioLogged = true;
                    float h = VisualHeightWorld(_spriteRenderer);
                    Debug.Log($"[Doors] ratio {_door.Point.id} enemy={(_enemy.Data != null ? _enemy.Data.id : "?")} visualH={h * _door.PxPerWorld:F1}px opening={_door.OpeningHeightWorld * _door.PxPerWorld:F1}px ratio={(h > 0f ? _door.OpeningHeightWorld / h : 0f):F3} need>={_door.Engine.minOpeningToEnemy:F2} scale={transform.localScale.x:F3}/{transform.localScale.y:F3}");
                }
            }
            if (_enemy.FreeMove && _spriteRenderer != null && _actor3D == null)
            {
                var c = _enemy.IsDefeated ? new Color(0.5f, 0.5f, 0.5f, 1f) : _baseColor;
                if (!_flashing) { c.a = alpha * (_enemy.IsDefeated ? 1f : _baseColor.a); _spriteRenderer.color = c; }
                if (_rig != null && _rig.Shadow != null) _rig.Shadow.enabled = alpha > 0.05f;
                if (_labelRenderer != null) _labelRenderer.enabled = alpha > 0.05f && !_enemy.IsDefeated;
            }
            FieldView.ApplySorting(_spriteRenderer, _enemy.Position.y);
            UpdateMotion();
            if (_spriteRenderer != null)
            {
                if (DoorInside) _spriteRenderer.sortingOrder = _door.BaseOrder + _door.Engine.enemyInsideZ; // z1: interior < enemy < patch/door/frame/props
                DoorSortOrder = _spriteRenderer.sortingOrder;
                if (DoorInside && _labelRenderer != null) _labelRenderer.enabled = false; // no name label floating over the facade
                if (DoorInside && _rig != null && _rig.Shadow != null) _rig.Shadow.enabled = false;
            }
            if (_labelRenderer != null) _labelRenderer.sortingOrder = FieldView.OverlayOrder;
            if (_warnRenderer != null) _warnRenderer.sortingOrder = FieldView.OverlayOrder + 1;
        }

        private void UpdateMotion()
        {
            if (_rig == null) return;
            float dt = GameTime.DeltaTime;
            Vector2 p = _enemy.Position;
            Vector2 v = (_hasLastPos && dt > 0f) ? (p - _lastPos) / dt : Vector2.zero;
            if (v.magnitude > 30f) v = Vector2.zero; // teleport / spawn
            _lastPos = p; _hasLastPos = true;
            _rig.SortingOrder = FieldView.DepthOrder(p.y);
            if (Mathf.Abs(v.x) > 0.25f) _rig.SetFacing(v.x);
            var md = MotionFx.Data;
            float signed = v.magnitude * (v.x >= 0f ? 1f : -1f);
            float maxSpeed = _enemy.Data != null ? Mathf.Max(0.5f, _enemy.Data.moveSpeed) : 2f;
            bool grounded = _enemy.IsAlive || _enemy.IsFleeing;
            if (_enemy.FreeMove) _rig.Tick(dt, v.magnitude > 0.05f ? v : Vector2.zero, maxSpeed, grounded && _enemy.IsBodyVisible, md.enemyBobHeight, md.enemyLeanMaxDeg, 0f, 0f, _enemy.IsBodyVisible);
            else _rig.Tick(dt, Mathf.Abs(v.x) > 0.05f ? signed : 0f, maxSpeed, grounded, md.enemyBobHeight, md.enemyLeanMaxDeg, 0f, 0f, true);
            Update3D(dt, v, maxSpeed);
        }

        private void Update3D(float dt, Vector2 v, float maxSpeed)
        {
            if (_actor3D == null) return;
            var d = Actor3DStage.Data;
            if (_spriteRenderer.enabled) _spriteRenderer.enabled = false;
            _actor3D.Place(_rig.FootWorld, Mathf.Abs(transform.lossyScale.y) * d.enemyScaleMul, _enemy.Position.y);
            bool fleeing = _enemy.IsFleeing || _enemy.IsDefeated;
            bool warn = _isShowingWarning && _enemy.IsAlive;
            var pose = new ActorPose
            {
                velocity = v,
                maxSpeed = maxSpeed,
                aimDir = _aimDir,
                aiming = _enemy.IsAlive && (warn || Time.time - _aimTime < 0.35f),
                defaultYaw = d.enemyTeamYaw,
                torsoFollowMove = fleeing || _enemy.IsEntering ? 1f : Actor3DStage.Anim.torsoFollowMove,
                strideMul = fleeing ? Actor3DStage.Anim.fleeStrideMul : 1f,
            };
            // Body wash only — name label stays crisp; projectiles never read TimeOfDayGrade.
            _actor3D.SetTint(HillNGun.Scenery.TimeOfDayGrade.Apply(fleeing ? d.defeatedTint : Color.white));
            _actor3D.Tick(dt, pose);
        }

        private void OnDestroy()
        {
            if (_enemy != null)
            {
                _enemy.OnWarningStart -= HandleWarningStart;
                _enemy.OnDamageTaken -= HandleDamage;
                _enemy.OnDefeated -= HandleDefeated;
                _enemy.OnAttack -= HandleAttack;
                _enemy.OnExplosiveThrown -= HandleThrow;
            }
            _actor3D?.Destroy();
            _actor3D = null;
            if (_telegraph != null) Destroy(_telegraph);
            if (_doorway != null) Destroy(_doorway);
        }
    }
}

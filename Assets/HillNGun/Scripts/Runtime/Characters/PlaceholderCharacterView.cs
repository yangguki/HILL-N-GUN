using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;
using HillNGun.Feel;
using HillNGun.View3D;

namespace HillNGun.Characters
{
    public class PlaceholderCharacterView : MonoBehaviour, ICharacterView
    {
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private SpriteRenderer _shadowRenderer;
        [SerializeField] private Transform _weaponPivot;
        [SerializeField] private SpriteRenderer _hitFlashOverlay;
        [SerializeField] private Transform _damageNumberSpawnPoint;
        [SerializeField] private SpriteLibrary _spriteLibrary;

        private CharacterStateMachine _character;
        private BattlefieldSystem _battlefield;
        private CharacterSprites _sprites;
        private Color _originalColor;
        private float _hitFlashTimer;
        private bool _isFlashing;
        private LineRenderer _aimLine;
        private TextMesh _nameLabel;
        private Sprite _idleSprite;
        private Sprite _shootSprite;
        private Sprite _rollSprite;
        private Sprite _downedSprite;

        // Motion feel (view only; MotionViewData)
        private MotionRig _rig;
        private float _afterimageTimer;
        private float _rollDustTimer;
        public SpriteRenderer BodyRenderer => _bodyRenderer;
        private float _footY = -0.85f;
        private float _facingHold;

        // 3D actor view (Actor3DData.useActor3D); sprite body stays as hidden fallback
        private Actor3D _actor3D;
        private float _lastFireSeen = -1f;
        public Actor3D Actor3D => _actor3D;

        public void Initialize(CharacterStateMachine character)
        {
            _character = character;
            _battlefield = ServiceLocator.Get<BattlefieldSystem>();
            
            if (_spriteLibrary == null)
            {
                _spriteLibrary = ServiceLocator.Get<SpriteLibrary>();
            }

            LoadSprites(character);
            SetupVisuals(character);
            
            if (_bodyRenderer != null)
            {
                _originalColor = _originalTint;
                // Equipped skin (offline progression): player team only. Placeholder = sprite recolor (SkinRecolor).
                if (character.Team == Team.Player && character.Data != null)
                    _skin = HillNGun.Progression.ProgressionService.Instance.GetEquippedSkin(character.Data.id);
                _bodyRenderer.sprite = Skinned(_bodyRenderer.sprite);
                _bodyRenderer.color = _originalColor;
            }

            if (_hitFlashOverlay != null)
            {
                _hitFlashOverlay.enabled = false;
            }

            Setup3D(character);

            character.OnDamageTaken += HandleDamageTaken;
            character.OnRollStarted += HandleRollStarted;
            character.OnRollEnded += HandleRollEnded;

            UpdateView(character);
        }

        private MeshRenderer _labelRenderer;

        private void HandleDamageTaken(CharacterStateMachine c, int damage, Vector2 dir)
        {
            PlayHitFlash(0.08f);
            _actor3D?.Hit();
        }

        private void Setup3D(CharacterStateMachine character)
        {
            if (character?.Data == null || !Actor3DStage.Enabled) return;
            _actor3D = Actor3DStage.Create(character.Data.id, null, "Actor3D_" + character.Data.id, _skin);
            if (_actor3D == null) return;
            if (_bodyRenderer != null) _bodyRenderer.enabled = false;
            _lastFireSeen = character.LastFireTime;
            if (character.Team == Team.Enemy)
                _actor3D.SetTint(HillNGun.Scenery.TimeOfDayGrade.Apply(Actor3DStage.Data.pvpOpponentTint));
            float lh = LabelHeight3D();
            if (_nameLabel != null) _nameLabel.transform.localPosition = new Vector3(0f, lh, 0f);
        }

        /// <summary>Label height (local units) above the 3D model's head.</summary>
        private float LabelHeight3D()
        {
            var d = Actor3DStage.Data;
            return _footY + _actor3D.Rig.Height * d.modelScale * Mathf.Cos(d.viewTiltDeg * Mathf.Deg2Rad) + 0.3f;
        }

        private void Update3D(CharacterStateMachine c, bool shooting)
        {
            if (_actor3D == null) return;
            var d = Actor3DStage.Data;
            if (_bodyRenderer != null && _bodyRenderer.enabled) _bodyRenderer.enabled = false;
            Vector3 feet = _rig != null ? _rig.FootWorld : transform.position;
            _actor3D.Place(feet, Mathf.Abs(transform.lossyScale.y), c.Position.y);
            if (c.LastFireTime != _lastFireSeen) { _actor3D.Fire(); _lastFireSeen = c.LastFireTime; }
            bool rolling = c.MovementState == MovementState.Rolling;
            bool active = c.OverallState == CharacterOverallState.Active;
            var pose = new ActorPose
            {
                velocity = active ? c.Velocity : Vector2.zero,
                maxSpeed = c.Data != null ? c.Data.moveSpeed : 3f,
                aimDir = c.AimDirection,
                aiming = active && shooting,
                defaultYaw = c.Team == Team.Player ? d.playerTeamYaw : d.enemyTeamYaw,
                torsoFollowMove = Actor3DStage.Anim.torsoFollowMove,
                rolling = active && rolling,
                rollProgress = c.RollProgress,
                rollSpin01 = c.Feel != null ? c.Feel.RollEase(c.RollProgress) : c.RollProgress,
                rollDir = c.RollDirection,
                rowHop = active && c.MovementState == MovementState.RowChanging ? Mathf.Sin(Mathf.PI * c.RowChangeProgress) : 0f,
                downed = c.OverallState == CharacterOverallState.Downed,
                defeated = c.OverallState == CharacterOverallState.Eliminated,
            };
            Color tint = c.Team == Team.Enemy ? d.pvpOpponentTint : Color.white;
            if (c.OverallState == CharacterOverallState.Eliminated) tint = d.defeatedTint;
            // Time-of-day wash on the body only — name label / team marker / outline stay crisp.
            tint = HillNGun.Scenery.TimeOfDayGrade.Apply(tint);
            _actor3D.SetTint(tint);
            if (active && c.IsInvulnerable && !rolling) _actor3D.Flash(0.35f * Mathf.PingPong(Time.time * 8f, 1f));
            _actor3D.Tick(GameTime.DeltaTime, pose);
        }

        private void HandleRollStarted(CharacterStateMachine c)
        {
            if (_rig == null) return;
            var d = MotionFx.Data;
            _rig.Impulse(-d.rollSquash);
            _rig.Dust(new Vector2(-c.RollDirection.x, 0f), d.rollDustCount);
            _afterimageTimer = 0f;
            _rollDustTimer = d.rollDustInterval;
        }

        private void HandleRollEnded(CharacterStateMachine c)
        {
            if (_rig == null) return;
            var d = MotionFx.Data;
            _rig.Impulse(-d.landSquash);
            _rig.Dust(new Vector2(c.RollDirection.x, 0f), d.rollDustCount);
        }

        private void OnDestroy()
        {
            if (_character != null)
            {
                _character.OnDamageTaken -= HandleDamageTaken;
                _character.OnRollStarted -= HandleRollStarted;
                _character.OnRollEnded -= HandleRollEnded;
            }
            _actor3D?.Destroy();
            _actor3D = null;
        }

        private void LoadSprites(CharacterStateMachine character)
        {
            if (_spriteLibrary != null && character.Data != null)
            {
                _sprites = _spriteLibrary.GetCharacterSprites(character.Data.id);
                if (_sprites != null)
                {
                    _idleSprite = _sprites.idle;
                    _shootSprite = _sprites.shoot;
                    _rollSprite = _sprites.roll;
                    _downedSprite = _sprites.downed;
                }
            }
        }

        private void SetupVisuals(CharacterStateMachine character)
        {
            if (_bodyRenderer == null)
            {
                // Shadow + feet pivot + body child so bounce/lean/squash/flip never move labels or bars
                var md = MotionFx.Data;
                MotionFx.MeasureFeet(_idleSprite, md.heroFootOffset, md.fallbackShadowSize.x / Mathf.Max(0.01f, md.heroShadowWidth), out float foot, out float width);
                _footY = foot;
                float sw = width * md.heroShadowWidth;
                _rig = new MotionRig(transform, foot, new Vector2(sw, sw * md.shadowAspect), md.heroArtFacesRight);
                _bodyRenderer = _rig.Renderer;
            }
            
            if (_idleSprite != null)
            {
                _bodyRenderer.sprite = Skinned(_idleSprite);
            }
            else
            {
                _bodyRenderer.sprite = CreateBodySprite(character);
            }

            var aimLineGo = new GameObject("AimLine");
            aimLineGo.transform.SetParent(transform);
            aimLineGo.transform.localPosition = Vector3.zero;
            _aimLine = aimLineGo.AddComponent<LineRenderer>();
            _aimLine.startWidth = 0.05f;
            _aimLine.endWidth = 0.02f;
            _aimLine.positionCount = 2;
            _aimLine.sharedMaterial = AimLineMaterial; // shared: one material for every aim line (batchable)
            _aimLine.startColor = Color.white;
            _aimLine.endColor = new Color(1, 1, 1, 0.3f);
            _aimLine.sortingOrder = 5;

            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(transform);
            labelGo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            labelGo.transform.localScale = Vector3.one / Mathf.Max(0.1f, ScalePresets.CharacterMul); // keep name text readable at every scale preset
            _nameLabel = labelGo.AddComponent<TextMesh>();
            _nameLabel.text = character.Data?.id ?? "Player";
            _nameLabel.fontSize = 32;
            _nameLabel.characterSize = 0.06f;
            _nameLabel.anchor = TextAnchor.MiddleCenter;
            _nameLabel.alignment = TextAlignment.Center;
            _nameLabel.color = character.Team == Team.Player ? new Color(0.85f, 1f, 0.85f) : new Color(1f, 0.75f, 0.7f);
            _labelRenderer = labelGo.GetComponent<MeshRenderer>();

            // Team marker under the feet (blue = our team, red = opponents)
            var markerGo = new GameObject("TeamMarker");
            markerGo.transform.SetParent(transform, false);
            markerGo.transform.localPosition = new Vector3(0f, -0.85f, 0f);
            markerGo.transform.localScale = new Vector3(1.3f, 0.45f, 1f);
            _teamMarker = markerGo.AddComponent<SpriteRenderer>();
            _teamMarker.sprite = CreateMarkerSprite();
            _teamMarker.color = character.Team == Team.Player ? new Color(0.3f, 0.6f, 1f, 0.75f) : new Color(1f, 0.3f, 0.25f, 0.75f);
            _markerBaseColor = _teamMarker.color;
            _markerBaseScale = markerGo.transform.localScale;

            if (character.Team == Team.Enemy && _bodyRenderer != null)
            {
                // PvP opponents: warm tint so teams read at a glance
                _bodyRenderer.flipY = false;
                _originalTint = new Color(1f, 0.82f, 0.75f);
            }
            if (_teamMarker != null && _rig != null)
            {
                // marker ring sits on the soft shadow at the feet
                _teamMarker.transform.localPosition = new Vector3(0f, _footY, 0f);
            }
        }

        private static Material _aimLineMaterial;
        private static Material AimLineMaterial => _aimLineMaterial != null ? _aimLineMaterial : (_aimLineMaterial = new Material(Shader.Find("Sprites/Default")) { name = "AimLine_shared" });

        private Color _originalTint = Color.white;
        private HillNGun.Data.SkinDefinition _skin;

        private Sprite Skinned(Sprite s) => _skin != null ? SkinRecolor.Get(s, _skin.tint, _skin.recolorStrength) : s;
        private SpriteRenderer _teamMarker;

        private static Sprite _markerSprite;
        private static Sprite CreateMarkerSprite()
        {
            if (_markerSprite != null) return _markerSprite;
            const int n = 32;
            var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * n + x] = d <= 1f ? new Color(1f, 1f, 1f, d > 0.7f ? 1f : 0.45f) : Color.clear;
                }
            tex.SetPixels(px);
            tex.Apply();
            _markerSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return _markerSprite;
        }

        private Sprite CreateBodySprite(CharacterStateMachine character)
        {
            int size = 64;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Color mainColor = GetTeamColor(character.Team, character.Data?.role ?? Role.Assault);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < size / 2f - 3)
                        pixels[y * size + x] = mainColor;
                    else if (dist < size / 2f)
                        pixels[y * size + x] = new Color(0.2f, 0.2f, 0.2f, 1f);
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        private Color GetTeamColor(Team team, Role role)
        {
            if (team == Team.Player)
            {
                switch (role)
                {
                    case Role.Assault: return new Color(0.9f, 0.3f, 0.3f);
                    case Role.Sniper: return new Color(0.5f, 0.3f, 0.8f);
                    case Role.Support: return new Color(0.3f, 0.7f, 0.9f);
                    default: return Color.white;
                }
            }
            else
            {
                return new Color(0.8f, 0.6f, 0.2f);
            }
        }

        public void SetSpriteLibrary(SpriteLibrary library)
        {
            _spriteLibrary = library;
            if (_character != null)
            {
                LoadSprites(_character);
            }
        }

        public void UpdateView(CharacterStateMachine character)
        {
            if (character == null) return;

            FieldView.Place(transform, character.Position, ScalePresets.CharacterMul); // scale preset: body, shadow, foot ring
            Vector3 worldPos = transform.position;
            FieldView.ApplySorting(_bodyRenderer, character.Position.y);
            if (_teamMarker != null) _teamMarker.sortingOrder = FieldView.GroundDecalOrder;
            UpdateMotion(character);
            if (_aimLine != null) _aimLine.sortingOrder = FieldView.OverlayOrder - 10;
            if (_labelRenderer != null) _labelRenderer.sortingOrder = FieldView.OverlayOrder;

            if (_weaponPivot != null && character.AimDirection.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(character.AimDirection.y, character.AimDirection.x) * Mathf.Rad2Deg;
                _weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            UpdateAimLine(character, worldPos);
            UpdateFlash();
            UpdateStateVisuals(character);
            UpdateStatusBar(character);
        }

        /// <summary>View-only motion: facing (aim priority while shooting), walk bounce, lean, squash, hop, roll tumble, afterimages, dust.</summary>
        private void UpdateMotion(CharacterStateMachine c)
        {
            if (_rig == null) return;
            var d = MotionFx.Data;
            float dt = GameTime.DeltaTime;
            _rig.SortingOrder = FieldView.DepthOrder(c.Position.y);

            if (c.OverallState != CharacterOverallState.Active)
            {
                _rig.ResetPose();
                Update3D(c, false);
                return;
            }

            bool rolling = c.MovementState == MovementState.Rolling;
            float maxSpeed = c.Data != null ? c.Data.moveSpeed : 3f;
            float vx = c.Velocity.x;

            // facing: roll direction > aim (while shooting) > stick > velocity
            bool shooting = c.ActionState == ActionState.Firing || c.ActionState == ActionState.Charging ||
                            (c.IsAimingWeapon && c.ActionState != ActionState.Aiming) ||
                            Time.time - c.LastFireTime < d.aimFacingHold;
            float want = 0f; bool deliberate = false;
            if (rolling && !c.IsRowRoll && Mathf.Abs(c.RollDirection.x) > 0.05f) { want = Mathf.Sign(c.RollDirection.x); deliberate = true; }
            else if (shooting && Mathf.Abs(c.AimDirection.x) > d.aimFacingMinX) want = Mathf.Sign(c.AimDirection.x);
            else if (!shooting && Mathf.Abs(c.MoveInputX) > 0.1f) { want = Mathf.Sign(c.MoveInputX); deliberate = true; }
            else if (!shooting && Mathf.Abs(vx) > 0.3f) want = Mathf.Sign(vx);
            if (want != 0f && want != _rig.FacingSign)
            {
                _facingHold += dt;
                if (deliberate || _facingHold >= d.facingHoldTime) { _rig.SetFacing(want); _facingHold = 0f; }
            }
            else _facingHold = 0f;

            float hop = 0f, spin = 0f;
            if (rolling)
            {
                float p = c.RollProgress;
                spin = d.rollSpinDeg * c.Feel.RollEase(p);
                hop = c.IsRowRoll ? d.rowHopHeight * 1.3f * Mathf.Sin(Mathf.PI * p) : d.rowHopHeight * 0.35f * Mathf.Sin(Mathf.PI * p);

                _afterimageTimer -= dt;
                if (_afterimageTimer <= 0f && p < 0.92f && _actor3D == null)
                {
                    _afterimageTimer = d.afterimageInterval;
                    MotionFx.Instance.Afterimage(_bodyRenderer, d.afterimageTint, d.afterimageAlpha, d.afterimageLifetime);
                }
                _rollDustTimer -= dt;
                if (_rollDustTimer <= 0f)
                {
                    _rollDustTimer = d.rollDustInterval;
                    _rig.Dust(new Vector2(-c.RollDirection.x, 0f), 1);
                }
            }
            else if (c.MovementState == MovementState.RowChanging)
            {
                hop = d.rowHopHeight * Mathf.Sin(Mathf.PI * c.RowChangeProgress);
            }

            bool grounded = !rolling && c.MovementState != MovementState.RowChanging;
            _rig.Tick(dt, rolling ? 0f : vx, maxSpeed, grounded, d.bobHeight, d.leanMaxDeg, hop, spin, true);
            Update3D(c, shooting);
        }

        // Downed / revive bar + skill state marker tint (placeholder visuals, no external art)
        private Transform _barRoot;
        private SpriteRenderer _barBg, _barFill;
        private static Sprite _pixelSprite;

        private static Sprite PixelSprite()
        {
            if (_pixelSprite != null) return _pixelSprite;
            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            _pixelSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4f);
            return _pixelSprite;
        }

        private void UpdateStatusBar(CharacterStateMachine character)
        {
            bool downed = character.OverallState == CharacterOverallState.Downed;
            if (_barRoot == null)
            {
                if (!downed) { UpdateSkillMarker(character); return; }
                _barRoot = new GameObject("StatusBar").transform;
                _barRoot.SetParent(transform, false);
                _barBg = new GameObject("Bg").AddComponent<SpriteRenderer>();
                _barBg.transform.SetParent(_barRoot, false);
                _barBg.sprite = PixelSprite();
                _barBg.color = new Color(0f, 0f, 0f, 0.7f);
                _barFill = new GameObject("Fill").AddComponent<SpriteRenderer>();
                _barFill.transform.SetParent(_barRoot, false);
                _barFill.sprite = PixelSprite();
            }
            _barRoot.gameObject.SetActive(downed);
            UpdateSkillMarker(character);
            if (!downed) return;

            const float w = 1.2f, h = 0.16f;
            _barRoot.rotation = Quaternion.identity;
            _barRoot.position = transform.position + new Vector3(-w * 0.5f, 0.85f, 0f);
            _barBg.transform.localScale = new Vector3(w, h, 1f);
            float ratio;
            if (character.ReviveProgress > 0f)
            {
                ratio = character.ReviveProgress;
                _barFill.color = new Color(0.35f, 0.95f, 0.45f);
            }
            else
            {
                var d = ServiceLocator.Get<DownedData>();
                ratio = character.DownedTimeRemaining / Mathf.Max(0.1f, d != null ? d.downedDuration : 10f);
                _barFill.color = new Color(1f, 0.75f, 0.25f);
            }
            _barFill.transform.localScale = new Vector3(w * Mathf.Clamp01(ratio), h * 0.7f, 1f);
            _barBg.sortingOrder = FieldView.OverlayOrder - 5;
            _barFill.sortingOrder = FieldView.OverlayOrder - 4;
        }

        private void UpdateSkillMarker(CharacterStateMachine character)
        {
            if (_teamMarker == null) return;
            if (character.IsSpreadBoostActive || character.IsPierceArmed || character.IsHoldingSkill)
            {
                float p = 0.6f + 0.4f * Mathf.PingPong(Time.time * 4f, 1f);
                _teamMarker.color = character.IsPierceArmed ? new Color(0.4f, 1f, 1f, p) : new Color(1f, 0.9f, 0.3f, p);
                _teamMarker.transform.localScale = _markerBaseScale * 1.35f;
                _markerTinted = true;
            }
            else if (_markerTinted)
            {
                _teamMarker.color = _markerBaseColor;
                _teamMarker.transform.localScale = _markerBaseScale;
                _markerTinted = false;
            }
        }

        private bool _markerTinted;
        private Color _markerBaseColor = Color.white;
        private Vector3 _markerBaseScale = Vector3.one;

        private void UpdateAimLine(CharacterStateMachine character, Vector3 worldPos)
        {
            if (_aimLine == null) return;

            if (character.AimDirection.magnitude > 0.1f && character.OverallState == CharacterOverallState.Active)
            {
                _aimLine.enabled = true;
                var wpn = character.Data?.weapon;
                float range = wpn != null ? wpn.range : 5f;
                // Aim line shows the real weapon reach (tile space -> world)
                Vector2 tileEnd = character.Position + character.AimDirection.normalized * range;
                Vector3 endPos = FieldView.ToWorld(tileEnd);
                endPos.z = worldPos.z;
                _aimLine.SetPosition(0, worldPos);
                _aimLine.SetPosition(1, endPos);

                bool laser = wpn != null && wpn.aimLaser;
                Color aimColor = character.ActionState == ActionState.Charging ? Color.yellow
                    : (laser ? new Color(1f, 0.35f, 0.35f, 1f) : Color.white);
                _aimLine.startColor = aimColor;
                _aimLine.endColor = new Color(aimColor.r, aimColor.g, aimColor.b, laser ? 0.55f : 0.3f);
                _aimLine.startWidth = laser ? 0.08f : 0.05f;
                _aimLine.endWidth = laser ? 0.04f : 0.02f;
            }
            else
            {
                _aimLine.enabled = false;
            }
        }

        private void UpdateStateVisuals(CharacterStateMachine character)
        {
            if (_bodyRenderer == null) return;

            switch (character.OverallState)
            {
                case CharacterOverallState.Downed:
                    if (_downedSprite != null)
                    {
                        _bodyRenderer.sprite = Skinned(_downedSprite);
                        transform.rotation = Quaternion.identity;
                    }
                    else
                    {
                        transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                    _bodyRenderer.color = Color.Lerp(_originalColor, Color.gray, 0.3f);
                    break;

                case CharacterOverallState.Eliminated:
                    _bodyRenderer.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0.3f);
                    break;

                default:
                    transform.rotation = Quaternion.identity;
                    
                    if (character.MovementState == MovementState.Rolling && _rollSprite != null)
                    {
                        _bodyRenderer.sprite = Skinned(_rollSprite);
                    }
                    else if (character.ActionState == ActionState.Firing && _shootSprite != null)
                    {
                        _bodyRenderer.sprite = Skinned(_shootSprite);
                    }
                    else if (_idleSprite != null)
                    {
                        _bodyRenderer.sprite = Skinned(_idleSprite);
                    }
                    
                    if (!_isFlashing)
                    {
                        _bodyRenderer.color = _originalColor;
                    }
                    break;
            }

            if (character.IsInvulnerable)
            {
                float flash = Mathf.PingPong(Time.time * 20f, 1f);
                _bodyRenderer.color = Color.Lerp(_originalColor, Color.white, flash * 0.5f);
            }
        }

        private void UpdateFlash()
        {
            if (!_isFlashing) return;

            _hitFlashTimer -= Time.deltaTime;
            if (_hitFlashTimer <= 0f)
            {
                _isFlashing = false;
                if (_hitFlashOverlay != null)
                {
                    _hitFlashOverlay.enabled = false;
                }
                if (_bodyRenderer != null)
                {
                    _bodyRenderer.color = _originalColor;
                }
            }
        }

        public void PlayHitFlash(float duration)
        {
            _isFlashing = true;
            _hitFlashTimer = duration;

            if (_hitFlashOverlay != null)
            {
                _hitFlashOverlay.enabled = true;
            }
            else if (_bodyRenderer != null)
            {
                _bodyRenderer.color = Color.white;
            }
        }

        public void PlayDamageNumber(int damage, Vector2 position)
        {
            // Placeholder implementation - would spawn floating damage text
            Debug.Log($"Damage: {damage} at {position}");
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            _actor3D?.SetVisible(visible);
        }

        public void Cleanup()
        {
            // Cleanup resources if needed
        }
    }
}

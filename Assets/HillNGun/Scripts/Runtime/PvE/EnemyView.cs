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
                var sprites = _spriteLibrary.GetEnemySprites(_enemy.Data.id);
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

            _spriteRenderer.color = enemySprite != null ? Color.white : (_enemy.Data?.baseColor ?? new Color(0.7f, 0.5f, 0.3f));

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform);
            labelGo.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            labelGo.transform.localScale = Vector3.one / Mathf.Max(0.1f, ScalePresets.CharacterMul);
            _nameLabel = labelGo.AddComponent<TextMesh>();
            _nameLabel.text = _enemy.Data?.id ?? "Enemy";
            _nameLabel.fontSize = 28;
            _nameLabel.characterSize = 0.05f;
            _nameLabel.anchor = TextAnchor.MiddleCenter;
            _nameLabel.alignment = TextAlignment.Center;
            _nameLabel.color = new Color(1f, 0.55f, 0.45f);
            _labelRenderer = labelGo.GetComponent<MeshRenderer>();

            var warningGo = new GameObject("Warning");
            warningGo.transform.SetParent(transform);
            warningGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var warnSr = warningGo.AddComponent<SpriteRenderer>();
            warnSr.sprite = CreateWarningSprite();
            warnSr.sortingOrder = 15;
            warnSr.color = Color.yellow;
            _warningIndicator = warningGo;
            _warnRenderer = warnSr;
            _warningIndicator.SetActive(false);

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

        private Sprite CreateWarningSprite()
        {
            int size = 32;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (float)size - 0.5f;
                    float ny = y / (float)size - 0.5f;
                    
                    if (Mathf.Abs(nx) < 0.15f && ny > -0.3f && ny < 0.15f)
                        pixels[y * size + x] = Color.white;
                    else if (Mathf.Abs(nx) < 0.1f && ny > 0.2f && ny < 0.35f)
                        pixels[y * size + x] = Color.white;
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
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

        private void HandleDamage(EnemyInstance enemy, int damage, Vector2 direction)
        {
            if (_spriteRenderer != null && _actor3D == null)
            {
                StartCoroutine(FlashWhite());
            }
            _actor3D?.Hit();
        }

        private System.Collections.IEnumerator FlashWhite()
        {
            Color original = _spriteRenderer.color;
            _spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            _spriteRenderer.color = original;
        }

        private void HandleDefeated(EnemyInstance enemy)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }

        private void Update()
        {
            if (_enemy == null) return;

            UpdatePosition();

            if (_isShowingWarning && _enemy.IsAlive && !_enemy.IsFleeing)
            {
                _isShowingWarning = false;
                _warningIndicator?.SetActive(false);
            }

            if (!_enemy.IsAlive || _enemy.IsFleeing)
            {
                if (_enemy.IsOffScreen(25f))
                {
                    Destroy(gameObject);
                }
            }
        }

        private void UpdatePosition()
        {
            FieldView.Place(transform, _enemy.Position, (_enemy.Data != null ? _enemy.Data.scale : 1f) * ScalePresets.CharacterMul);
            FieldView.ApplySorting(_spriteRenderer, _enemy.Position.y);
            UpdateMotion();
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
            _rig.Tick(dt, Mathf.Abs(v.x) > 0.05f ? signed : 0f, maxSpeed, grounded, md.enemyBobHeight, md.enemyLeanMaxDeg, 0f, 0f, true);
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
            _actor3D.SetTint(fleeing ? d.defeatedTint : Color.white);
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
        }
    }
}

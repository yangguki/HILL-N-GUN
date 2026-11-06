using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;
using HillNGun.Feel;
using HillNGun.View3D;

namespace HillNGun.PvE
{
    public class BossView : MonoBehaviour
    {
        private BossInstance _boss;
        private SpriteRenderer _spriteRenderer;
        private SpriteRenderer _hpBarBg;
        private SpriteRenderer _hpBarFill;
        private TextMesh _nameLabel;
        private BattlefieldSystem _battlefield;
        private SpriteLibrary _spriteLibrary;
        private MotionRig _rig;
        private Vector2 _lastPos;
        private bool _hasLastPos;
        private float _t;
        public SpriteRenderer BodyRenderer => _spriteRenderer;
        private Actor3D _actor3D;
        private Vector2 _aimDir = Vector2.down;
        private float _aimTime = -99f;
        public Actor3D Actor3D => _actor3D;
        public TailSweepTelegraphView Telegraph { get; private set; }
        public BossInstance Boss => _boss;

        public void Initialize(BossInstance boss)
        {
            _boss = boss;
            _battlefield = ServiceLocator.Get<BattlefieldSystem>();
            _spriteLibrary = ServiceLocator.Get<SpriteLibrary>();

            SetupVisuals();
            SubscribeEvents();

            var telegraphGo = new GameObject("TailSweepTelegraph");
            // Root object (world-space mesh); destroyed together with this view
            Telegraph = telegraphGo.AddComponent<TailSweepTelegraphView>();
            Telegraph.Initialize(boss);
            UpdatePosition();
        }

        private void SetupVisuals()
        {
            Sprite bossSprite = null;
            if (_spriteLibrary != null && _spriteLibrary.boss != null)
            {
                bossSprite = _spriteLibrary.boss.idle;
            }

            var md = MotionFx.Data;
            MotionFx.MeasureFeet(bossSprite, -0.9f, 2f, out float foot, out float width);
            float sw = width * md.bossShadowWidth;
            _rig = new MotionRig(transform, foot, new Vector2(sw, sw * md.shadowAspect), md.enemyArtFacesRight);
            _rig.FxScale = 1.6f;
            _spriteRenderer = _rig.Renderer;

            if (bossSprite != null)
            {
                _spriteRenderer.sprite = bossSprite;
            }
            else
            {
                _spriteRenderer.sprite = CreateBossSprite();
            }

            _spriteRenderer.sortingOrder = 10;
            _spriteRenderer.color = HillNGun.Scenery.TimeOfDayGrade.Apply(Color.white); // style-4 sprite: TOD body wash

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform);
            labelGo.transform.localPosition = new Vector3(0f, 2f, 0f);
            _nameLabel = labelGo.AddComponent<TextMesh>();
            var loc = ServiceLocator.Get<HillNGun.UI.LocalizationManager>();
            string key = _boss.Data?.nameKey;
            _nameLabel.text = (loc != null && !string.IsNullOrEmpty(key) && loc.HasString(key)) ? loc.GetString(key) : (_boss.Data?.id ?? "BOSS");
            _nameLabel.fontSize = 48;
            _nameLabel.characterSize = 0.06f;
            _nameLabel.anchor = TextAnchor.MiddleCenter;
            _nameLabel.alignment = TextAlignment.Center;
            _nameLabel.color = new Color(1f, 0.3f, 0.3f);

            CreateHpBar();

            if (_boss.Data != null && Actor3DStage.Enabled)
            {
                _actor3D = Actor3DStage.Create(_boss.Data.id, null, "Actor3D_" + _boss.Data.id);
                if (_actor3D != null)
                {
                    _spriteRenderer.enabled = false;
                    var d = Actor3DStage.Data;
                    float top = foot + _actor3D.Rig.Height * d.modelScale * d.bossScaleMul * Mathf.Cos(d.viewTiltDeg * Mathf.Deg2Rad);
                    labelGo.transform.localPosition = new Vector3(0f, top + 0.2f, 0f);
                    if (_hpBarBg != null) _hpBarBg.transform.localPosition = new Vector3(0f, top + 0.5f, 0f);
                }
            }
        }

        private void CreateHpBar()
        {
            var bgGo = new GameObject("HpBarBg");
            bgGo.transform.SetParent(transform);
            bgGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            _hpBarBg = bgGo.AddComponent<SpriteRenderer>();
            _hpBarBg.sprite = CreateBarSprite();
            _hpBarBg.color = new Color(0.2f, 0.2f, 0.2f);
            _hpBarBg.sortingOrder = 20;
            bgGo.transform.localScale = new Vector3(3f, 0.3f, 1f);

            var fillGo = new GameObject("HpBarFill");
            fillGo.transform.SetParent(bgGo.transform);
            fillGo.transform.localPosition = Vector3.zero;
            fillGo.transform.localScale = Vector3.one;
            _hpBarFill = fillGo.AddComponent<SpriteRenderer>();
            _hpBarFill.sprite = CreateBarSprite();
            _hpBarFill.color = Color.red;
            _hpBarFill.sortingOrder = 21;
        }

        private Sprite CreateBarSprite()
        {
            var texture = new Texture2D(32, 8);
            var pixels = new Color[32 * 8];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 32, 8), new Vector2(0.5f, 0.5f), 32f);
        }

        private Sprite CreateBossSprite()
        {
            int size = 128;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Color mainColor = new Color(0.6f, 0.2f, 0.2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < size / 2f - 4)
                        pixels[y * size + x] = mainColor;
                    else if (dist < size / 2f)
                        pixels[y * size + x] = Color.black;
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        private void SubscribeEvents()
        {
            _boss.OnDamageTaken += HandleDamage;
            _boss.OnDefeated += HandleDefeated;
            _boss.OnFanShot += HandleFanShot;
            _boss.OnTailSweep += HandleTailSweep;
        }

        private void HandleFanShot(BossInstance b, Vector2[] dirs, int dmg)
        {
            if (dirs != null && dirs.Length > 0) _aimDir = dirs[dirs.Length / 2].normalized;
            _aimTime = Time.time;
            _actor3D?.FanShot();
        }

        private void HandleTailSweep(BossInstance b, float width)
        {
            float side = b.TailSweepArea.center.x - b.Position.x;
            _actor3D?.TailSweep(side >= 0f ? 1f : -1f);
        }

        private void HandleDamage(BossInstance boss, int damage, Vector2 direction)
        {
            if (_spriteRenderer != null && _actor3D == null)
            {
                StartCoroutine(FlashWhite());
            }
            _actor3D?.Hit();
            UpdateHpBar();
        }

        private System.Collections.IEnumerator FlashWhite()
        {
            Color original = _spriteRenderer.color;
            _spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            _spriteRenderer.color = original;
        }

        private void HandleDefeated(BossInstance boss)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }

        private void Update()
        {
            if (_boss == null) return;
            UpdatePosition();
            UpdateHpBar();
        }

        private void UpdatePosition()
        {
            FieldView.Place(transform, _boss.Position, 2f * ScalePresets.CharacterMul);
            FieldView.ApplySorting(_spriteRenderer, _boss.Position.y);
            UpdateMotion();
            if (_hpBarBg != null) _hpBarBg.sortingOrder = FieldView.OverlayOrder;
            if (_hpBarFill != null) _hpBarFill.sortingOrder = FieldView.OverlayOrder + 1;
            var lr = _nameLabel != null ? _nameLabel.GetComponent<MeshRenderer>() : null;
            if (lr != null) lr.sortingOrder = FieldView.OverlayOrder;
        }

        private void UpdateMotion()
        {
            if (_rig == null) return;
            float dt = GameTime.DeltaTime;
            _t += dt;
            Vector2 p = _boss.Position;
            Vector2 v = (_hasLastPos && dt > 0f) ? (p - _lastPos) / dt : Vector2.zero;
            if (v.magnitude > 30f) v = Vector2.zero;
            _lastPos = p; _hasLastPos = true;
            _rig.SortingOrder = FieldView.DepthOrder(p.y);
            var md = MotionFx.Data;
            // slow idle breathe/sway (snake coil) + lean into horizontal moves; no flip (boss faces the players)
            float breathe = _boss.IsAlive ? Mathf.Sin(_t * 2.1f) * md.bossBreathe * 2f : 0f;
            _rig.Tick(dt, Mathf.Abs(v.x) > 0.05f ? v.x : 0f, 3f, _boss.IsAlive, md.enemyBobHeight * 0.5f, md.enemyLeanMaxDeg * 0.5f, breathe, 0f, false);
            Update3D(dt, v);
        }

        private void Update3D(float dt, Vector2 v)
        {
            if (_actor3D == null) return;
            var d = Actor3DStage.Data;
            if (_spriteRenderer.enabled) _spriteRenderer.enabled = false;
            _actor3D.Place(_rig.FootWorld, Mathf.Abs(transform.lossyScale.y) * d.bossScaleMul, _boss.Position.y);
            bool warn = _boss.IsAlive && _boss.IsWarning;
            var wt = warn ? _boss.CurrentWarning : BossInstance.WarningType.None;
            _actor3D.Warn(wt == BossInstance.WarningType.TailSweep, wt == BossInstance.WarningType.FanShot);
            var pose = new ActorPose
            {
                velocity = v,
                maxSpeed = 3f,
                aimDir = _aimDir,
                aiming = _boss.IsAlive && Time.time - _aimTime < 0.5f,
                defaultYaw = d.enemyTeamYaw,
                torsoFollowMove = 0.2f,
                defeated = !_boss.IsAlive,
            };
            // Body gets time-of-day wash; name label / HP bar / outline stay untinted.
            _actor3D.SetTint(HillNGun.Scenery.TimeOfDayGrade.Apply(_boss.IsAlive ? Color.white : d.defeatedTint));
            _actor3D.Tick(dt, pose);
        }

        private void UpdateHpBar()
        {
            if (_hpBarFill == null || _boss == null) return;

            float hpPercent = _boss.HpRatio;
            _hpBarFill.transform.localScale = new Vector3(hpPercent, 1f, 1f);
            _hpBarFill.transform.localPosition = new Vector3((hpPercent - 1f) / 2f, 0f, 0f);
        }

        private void OnDestroy()
        {
            if (Telegraph != null) Destroy(Telegraph.gameObject);
            if (_boss != null)
            {
                _boss.OnDamageTaken -= HandleDamage;
                _boss.OnDefeated -= HandleDefeated;
                _boss.OnFanShot -= HandleFanShot;
                _boss.OnTailSweep -= HandleTailSweep;
            }
            _actor3D?.Destroy();
            _actor3D = null;
        }
    }
}

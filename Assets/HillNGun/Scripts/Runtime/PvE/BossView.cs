using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;

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
        public SpriteRenderer BodyRenderer => _spriteRenderer;
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
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            Sprite bossSprite = null;
            if (_spriteLibrary != null && _spriteLibrary.boss != null)
            {
                bossSprite = _spriteLibrary.boss.idle;
            }

            if (bossSprite != null)
            {
                _spriteRenderer.sprite = bossSprite;
            }
            else
            {
                _spriteRenderer.sprite = CreateBossSprite();
            }

            _spriteRenderer.sortingOrder = 10;

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
        }

        private void HandleDamage(BossInstance boss, int damage, Vector2 direction)
        {
            if (_spriteRenderer != null)
            {
                StartCoroutine(FlashWhite());
            }
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
            FieldView.Place(transform, _boss.Position, 2f);
            FieldView.ApplySorting(_spriteRenderer, _boss.Position.y);
            if (_hpBarBg != null) _hpBarBg.sortingOrder = FieldView.OverlayOrder;
            if (_hpBarFill != null) _hpBarFill.sortingOrder = FieldView.OverlayOrder + 1;
            var lr = _nameLabel != null ? _nameLabel.GetComponent<MeshRenderer>() : null;
            if (lr != null) lr.sortingOrder = FieldView.OverlayOrder;
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
            }
        }
    }
}

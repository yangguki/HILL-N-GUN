using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;

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
        public SpriteRenderer BodyRenderer => _spriteRenderer;
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
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            Sprite enemySprite = null;
            if (_spriteLibrary != null && _enemy.Data != null)
            {
                var sprites = _spriteLibrary.GetEnemySprites(_enemy.Data.id);
                if (sprites != null)
                {
                    enemySprite = sprites.idle;
                }
            }

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
        }

        private void HandleWarningStart(EnemyInstance enemy)
        {
            _isShowingWarning = true;
            _warningIndicator?.SetActive(true);
        }

        private void HandleDamage(EnemyInstance enemy, int damage, Vector2 direction)
        {
            if (_spriteRenderer != null)
            {
                StartCoroutine(FlashWhite());
            }
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
            FieldView.Place(transform, _enemy.Position, _enemy.Data != null ? _enemy.Data.scale : 1f);
            FieldView.ApplySorting(_spriteRenderer, _enemy.Position.y);
            if (_labelRenderer != null) _labelRenderer.sortingOrder = FieldView.OverlayOrder;
            if (_warnRenderer != null) _warnRenderer.sortingOrder = FieldView.OverlayOrder + 1;
        }

        private void OnDestroy()
        {
            if (_enemy != null)
            {
                _enemy.OnWarningStart -= HandleWarningStart;
                _enemy.OnDamageTaken -= HandleDamage;
                _enemy.OnDefeated -= HandleDefeated;
            }
        }
    }
}

using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;

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
                _bodyRenderer.color = _originalColor;
            }

            if (_hitFlashOverlay != null)
            {
                _hitFlashOverlay.enabled = false;
            }

            character.OnDamageTaken += HandleDamageTaken;

            UpdateView(character);
        }

        private MeshRenderer _labelRenderer;

        private void HandleDamageTaken(CharacterStateMachine c, int damage, Vector2 dir)
        {
            PlayHitFlash(0.08f);
        }

        private void OnDestroy()
        {
            if (_character != null) _character.OnDamageTaken -= HandleDamageTaken;
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
                _bodyRenderer = GetComponent<SpriteRenderer>();
                if (_bodyRenderer == null)
                {
                    _bodyRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }
            
            if (_idleSprite != null)
            {
                _bodyRenderer.sprite = _idleSprite;
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
            _aimLine.material = new Material(Shader.Find("Sprites/Default"));
            _aimLine.startColor = Color.white;
            _aimLine.endColor = new Color(1, 1, 1, 0.3f);
            _aimLine.sortingOrder = 5;

            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(transform);
            labelGo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
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

            if (character.Team == Team.Enemy && _bodyRenderer != null)
            {
                // PvP opponents: face the player and get a warm tint so teams read at a glance
                _bodyRenderer.flipY = false;
                _originalTint = new Color(1f, 0.82f, 0.75f);
            }
        }

        private Color _originalTint = Color.white;
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

            FieldView.Place(transform, character.Position);
            Vector3 worldPos = transform.position;
            FieldView.ApplySorting(_bodyRenderer, character.Position.y);
            FieldView.ApplySorting(_teamMarker, character.Position.y, -1);
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
        }

        private void UpdateAimLine(CharacterStateMachine character, Vector3 worldPos)
        {
            if (_aimLine == null) return;

            if (character.AimDirection.magnitude > 0.1f && character.OverallState == CharacterOverallState.Active)
            {
                _aimLine.enabled = true;
                float range = character.Data?.weapon?.range ?? 5f;
                Vector3 endPos = worldPos + (Vector3)(character.AimDirection.normalized * range * 0.3f);
                _aimLine.SetPosition(0, worldPos);
                _aimLine.SetPosition(1, endPos);

                Color aimColor = character.ActionState == ActionState.Charging ? Color.yellow : Color.white;
                _aimLine.startColor = aimColor;
                _aimLine.endColor = new Color(aimColor.r, aimColor.g, aimColor.b, 0.3f);
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
                        _bodyRenderer.sprite = _downedSprite;
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
                        _bodyRenderer.sprite = _rollSprite;
                    }
                    else if (character.ActionState == ActionState.Firing && _shootSprite != null)
                    {
                        _bodyRenderer.sprite = _shootSprite;
                    }
                    else if (_idleSprite != null)
                    {
                        _bodyRenderer.sprite = _idleSprite;
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
        }

        public void Cleanup()
        {
            // Cleanup resources if needed
        }
    }
}

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
                _originalColor = Color.white;
                _bodyRenderer.color = _originalColor;
            }

            if (_hitFlashOverlay != null)
            {
                _hitFlashOverlay.enabled = false;
            }

            UpdateView(character);
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
            _nameLabel.color = Color.white;
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

            Vector3 worldPos = _battlefield?.LogicToWorldPosition(character.Position) ?? 
                               new Vector3(character.Position.x, character.Position.y, 0f);
            transform.position = worldPos;

            float scale = _battlefield?.GetPerspectiveScale(character.Position.y) ?? 1f;
            transform.localScale = Vector3.one * scale;

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

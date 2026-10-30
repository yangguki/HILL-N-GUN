using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;

namespace HillNGun.Characters
{
    public class DummyAlly : MonoBehaviour
    {
        [SerializeField] private DownedData _downedData;

        private Vector2 _position;
        private int _currentRowIndex;
        private CharacterOverallState _state = CharacterOverallState.Active;
        private float _downedTimer;
        private float _reviveProgress;
        private bool _isBeingRevived;
        private TextMesh _nameLabel;

        private SpriteRenderer _renderer;
        private Transform _starsTransform;
        private float _starsRotation;

        public Vector2 Position => _position;
        public int CurrentRowIndex => _currentRowIndex;
        public CharacterOverallState State => _state;
        public float ReviveProgress => _reviveProgress;
        public bool IsBeingRevived => _isBeingRevived;
        public float DownedTimeRemaining => _downedData != null ? _downedData.downedDuration - _downedTimer : 0f;

        public event System.Action<DummyAlly> OnRevived;
        public event System.Action<DummyAlly> OnEliminated;
        public event System.Action<DummyAlly> OnDowned;

        public void Initialize(Vector2 position, int rowIndex, DownedData downedData)
        {
            _position = position;
            _currentRowIndex = rowIndex;
            _downedData = downedData;
            _state = CharacterOverallState.Active;
            _downedTimer = 0f;
            _reviveProgress = 0f;

            SetupVisuals();
        }

        private void SetupVisuals()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer == null)
            {
                var visualGo = new GameObject("Visual");
                visualGo.transform.SetParent(transform);
                visualGo.transform.localPosition = Vector3.zero;
                _renderer = visualGo.AddComponent<SpriteRenderer>();
                _renderer.sprite = CreatePlaceholderSprite();
                _renderer.color = new Color(0.3f, 0.8f, 0.4f, 1f);
            }

            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(transform);
            labelGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            _nameLabel = labelGo.AddComponent<TextMesh>();
            _nameLabel.text = "아군";
            _nameLabel.fontSize = 32;
            _nameLabel.characterSize = 0.08f;
            _nameLabel.anchor = TextAnchor.MiddleCenter;
            _nameLabel.alignment = TextAlignment.Center;
            _nameLabel.color = Color.white;

            var starsGo = new GameObject("Stars");
            starsGo.transform.SetParent(transform);
            starsGo.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            _starsTransform = starsGo.transform;
            starsGo.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                var starGo = new GameObject($"Star_{i}");
                starGo.transform.SetParent(_starsTransform);
                float angle = i * 120f * Mathf.Deg2Rad;
                starGo.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.3f, Mathf.Sin(angle) * 0.3f, 0f);
                starGo.transform.localScale = Vector3.one * 0.3f;

                var starRenderer = starGo.AddComponent<SpriteRenderer>();
                starRenderer.sprite = CreateStarSprite();
                starRenderer.color = Color.yellow;
                starRenderer.sortingOrder = 10;
            }

            transform.rotation = Quaternion.identity;
        }

        private Sprite CreatePlaceholderSprite()
        {
            int size = 64;
            var texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < size / 2f - 2)
                        pixels[y * size + x] = Color.white;
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

        private Sprite CreateStarSprite()
        {
            int size = 32;
            var texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerRadius = size / 2f - 2f;
            float innerRadius = outerRadius * 0.4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y) - center;
                    float angle = Mathf.Atan2(pos.y, pos.x);
                    float dist = pos.magnitude;

                    float starAngle = angle * 5f / (2f * Mathf.PI);
                    float starRadius = Mathf.Lerp(innerRadius, outerRadius, 
                        (Mathf.Cos(starAngle * 2f * Mathf.PI) + 1f) / 2f);

                    if (dist < starRadius)
                        pixels[y * size + x] = Color.white;
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        private void Update()
        {
            if (_state == CharacterOverallState.Eliminated) return;

            if (_state == CharacterOverallState.Active) return;

            float dt = GameTime.DeltaTime;

            if (_starsTransform != null)
            {
                _starsRotation += dt * 90f;
                _starsTransform.localRotation = Quaternion.Euler(0f, 0f, _starsRotation);
            }

            if (_isBeingRevived)
            {
                float reviveTime = _downedData?.baseReviveTime ?? 3f;
                _reviveProgress += dt / reviveTime;

                if (_reviveProgress >= 1f)
                {
                    CompleteRevive();
                }
            }
            else
            {
                _downedTimer += dt;

                float downedDuration = _downedData?.downedDuration ?? 10f;
                if (_downedTimer >= downedDuration)
                {
                    Eliminate();
                }
            }

            UpdateVisuals();
        }

        public void GoDown()
        {
            if (_state != CharacterOverallState.Active) return;

            _state = CharacterOverallState.Downed;
            _downedTimer = 0f;
            _reviveProgress = 0f;
            _isBeingRevived = false;

            transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            if (_starsTransform != null)
            {
                _starsTransform.gameObject.SetActive(true);
            }

            if (_renderer != null)
            {
                _renderer.color = new Color(0.5f, 0.8f, 0.5f, 0.7f);
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = "기절!";
                _nameLabel.color = Color.yellow;
            }

            Feel.PlaceholderAudio.Instance?.PlayDowned();
            OnDowned?.Invoke(this);
        }

        public void StartRevive()
        {
            if (_state != CharacterOverallState.Downed) return;
            _isBeingRevived = true;
        }

        public void StopRevive()
        {
            _isBeingRevived = false;
            _reviveProgress = Mathf.Max(0f, _reviveProgress - 0.1f);
        }

        private void CompleteRevive()
        {
            _state = CharacterOverallState.Active;
            _isBeingRevived = false;
            _reviveProgress = 0f;
            _downedTimer = 0f;

            transform.rotation = Quaternion.identity;

            if (_starsTransform != null)
            {
                _starsTransform.gameObject.SetActive(false);
            }

            if (_renderer != null)
            {
                _renderer.color = new Color(0.3f, 0.8f, 0.4f, 1f);
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = "아군";
                _nameLabel.color = Color.white;
            }

            Feel.PlaceholderAudio.Instance?.PlayRevive();
            OnRevived?.Invoke(this);
        }

        private void Eliminate()
        {
            _state = CharacterOverallState.Eliminated;

            if (_renderer != null)
            {
                _renderer.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }

            if (_starsTransform != null)
            {
                _starsTransform.gameObject.SetActive(false);
            }

            OnEliminated?.Invoke(this);
        }

        private void UpdateVisuals()
        {
            if (_renderer != null && _isBeingRevived)
            {
                float pulse = Mathf.PingPong(Time.time * 3f, 1f);
                _renderer.color = Color.Lerp(
                    new Color(0.5f, 0.5f, 0.8f, 0.7f),
                    new Color(0.7f, 0.9f, 0.7f, 0.9f),
                    pulse
                );
            }
        }

        public void ResetToActive()
        {
            _state = CharacterOverallState.Active;
            _downedTimer = 0f;
            _reviveProgress = 0f;
            _isBeingRevived = false;

            transform.rotation = Quaternion.identity;

            if (_starsTransform != null)
            {
                _starsTransform.gameObject.SetActive(false);
            }

            if (_renderer != null)
            {
                _renderer.color = new Color(0.3f, 0.8f, 0.4f, 1f);
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = "아군";
                _nameLabel.color = Color.white;
            }
        }

        public void ResetToDowned()
        {
            ResetToActive();
        }

        public void SetPosition(Vector2 position)
        {
            _position = position;
            var battlefield = ServiceLocator.Get<BattlefieldSystem>();
            if (battlefield != null)
            {
                transform.position = battlefield.LogicToWorldPosition(position);
            }
            else
            {
                transform.position = new Vector3(position.x, position.y, 0f);
            }
        }
    }
}

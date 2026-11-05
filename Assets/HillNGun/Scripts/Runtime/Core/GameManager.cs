using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Input;
using HillNGun.Battlefield;
using HillNGun.Combat;
using HillNGun.Characters;
using HillNGun.Cover;
using HillNGun.Logging;
using HillNGun.DebugTools;
using HillNGun.Feel;
using HillNGun.UI;
using System.Collections.Generic;

namespace HillNGun.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Data References")]
        [SerializeField] private BattlefieldData _battlefieldData;
        [SerializeField] private InputData _inputData;
        [SerializeField] private DownedData _downedData;
        [SerializeField] private FeelData _feelData;
        [SerializeField] private MatchRulesData _matchRules;

        [Header("Character Data")]
        [SerializeField] private CharacterData[] _characterDatas;
        [SerializeField] private int _startingCharacterIndex = 0;

        [Header("Cover Data")]
        [SerializeField] private CoverData[] _coverDatas;

        [Header("Target Data")]
        [SerializeField] private TargetData[] _targetDatas;

        [Header("Scene References")]
        [SerializeField] private BattlefieldSystem _battlefieldSystem;
        [SerializeField] private CombatSystem _combatSystem;
        [SerializeField] private TouchInputHandler _inputHandler;
        [SerializeField] private GameLogger _logger;
        [SerializeField] private DebugOverlay _debugOverlay;
        [SerializeField] private ScreenShake _screenShake;
        [SerializeField] private HUDController _hud;
        [SerializeField] private LocalizationManager _localization;
        [SerializeField] private PlaceholderAudio _audio;
        [SerializeField] private ReviveSystem _reviveSystem;
        [SerializeField] private SettingsPanel _settingsPanel;
        [SerializeField] private PlaceholderHUD _placeholderHUD;

        [Header("Prefabs")]
        [SerializeField] private GameObject _characterPrefab;
        [SerializeField] private GameObject _coverPrefab;
        [SerializeField] private GameObject _targetPrefab;
        [SerializeField] private GameObject _dummyAllyPrefab;

        private CharacterStateMachine _playerCharacter;
        private List<DummyAlly> _dummyAllies = new List<DummyAlly>();
        private List<CharacterStateMachine> _allCharacters = new List<CharacterStateMachine>();
        private List<CoverInstance> _allCovers = new List<CoverInstance>();
        private List<TargetInstance> _allTargets = new List<TargetInstance>();

        public CharacterStateMachine PlayerCharacter => _playerCharacter;

        private void Awake()
        {
            ServiceLocator.Register(_downedData);
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<DownedData>();
            ServiceLocator.Unregister<GameManager>();
        }

        private void Start()
        {
            InitializeSystems();
            SpawnPlayer();
            SpawnCovers();
            SpawnTargets();
            SpawnDummyAlly();
            
            _logger?.LogSessionStart(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void InitializeSystems()
        {
            if (_inputHandler != null)
            {
                _inputHandler.SetInputData(_inputData);
            }

            if (_screenShake != null)
            {
                _screenShake.SetFeelData(_feelData);
            }

            Vibration.SetFeelData(_feelData);
        }

        private void SpawnPlayer()
        {
            if (_characterDatas == null || _characterDatas.Length == 0) return;

            int index = Mathf.Clamp(_startingCharacterIndex, 0, _characterDatas.Length - 1);
            CharacterData data = _characterDatas[index];

            Vector2 startPos = _battlefieldSystem?.GetStartPosition(1, Team.Player) ?? 
                              new Vector2(12f, 4.5f);
            int startRow = _battlefieldData?.startRowIndex ?? 1;

            _playerCharacter = CreateCharacter(data, Team.Player, 1, startPos, startRow);

            if (_inputHandler != null)
            {
                _inputHandler.SetReceiver(_playerCharacter);
            }

            if (_hud != null)
            {
                _hud.SetCharacter(_playerCharacter);
            }

            _placeholderHUD?.SetCharacter(_playerCharacter);

            _logger?.LogCharacterSelect(_playerCharacter);
        }

        private CharacterStateMachine CreateCharacter(CharacterData data, Team team, int playerIndex, Vector2 position, int rowIndex)
        {
            GameObject go;
            if (_characterPrefab != null)
            {
                go = Instantiate(_characterPrefab);
            }
            else
            {
                go = new GameObject($"Character_{data.id}");
                go.AddComponent<CharacterStateMachine>();

                var viewGo = new GameObject("View");
                viewGo.transform.SetParent(go.transform);
                var sr = viewGo.AddComponent<SpriteRenderer>();
                sr.sprite = CreatePlaceholderSprite(1f);
                var view = viewGo.AddComponent<PlaceholderCharacterView>();
            }

            var character = go.GetComponent<CharacterStateMachine>();
            character.Initialize(data, team, playerIndex, position, rowIndex);

            var characterView = go.GetComponentInChildren<ICharacterView>();
            if (characterView != null)
            {
                characterView.Initialize(character);
                character.SetView(characterView);
            }

            _allCharacters.Add(character);
            _combatSystem?.RegisterCharacter(character);
            _debugOverlay?.RegisterCharacter(character);

            SubscribeToCharacterEvents(character);

            return character;
        }

        private void SubscribeToCharacterEvents(CharacterStateMachine character)
        {
            character.OnDamageTaken += HandleCharacterDamaged;
            character.OnDowned += HandleCharacterDowned;
            character.OnRevived += HandleCharacterRevived;
            character.OnEliminated += HandleCharacterEliminated;
            character.OnFired += HandleCharacterFired;
            character.OnRollStarted += HandleCharacterRolled;
            character.OnSkillUsed += HandleCharacterSkillUsed;
        }

        private void HandleCharacterDamaged(CharacterStateMachine character, int damage, Vector2 direction)
        {
            var view = character.GetComponentInChildren<ICharacterView>();
            view?.PlayHitFlash(_feelData?.hitFlashDuration ?? 0.06f);

            Feel.EffectsManager.Instance?.SpawnDamageNumber(character.Position, damage, damage >= 500);
            Feel.EffectsManager.Instance?.SpawnImpact(character.Position, false);

            _audio?.PlayHit(damage);

            if (damage >= 900)
            {
                _screenShake?.ShakeHeavyHit();
            }

            if (character == _playerCharacter)
            {
                Vibration.VibrateBigDamage(damage);
            }
        }

        private void HandleCharacterDowned(CharacterStateMachine character)
        {
            _logger?.LogDowned(character, "hp_zero");
            _audio?.PlayDowned();
        }

        private void HandleCharacterRevived(CharacterStateMachine character)
        {
            _logger?.LogRevived(character, 0f);
            _audio?.PlayRevive();
        }

        private void HandleCharacterEliminated(CharacterStateMachine character)
        {
            _logger?.LogOut(character);
        }

        private void HandleCharacterFired(CharacterStateMachine character, Vector2 direction, int damage)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bool isMoving = character.MovementState == MovementState.Moving || 
                           character.MovementState == MovementState.RowChanging;
            
            _logger?.LogShotFired(character, angle, 0f, isMoving);

            bool isShotgun = character.Data?.weapon?.projectilesPerShot > 1;
            bool isHeavy = character.Data?.weapon?.fireMode == FireMode.ChargeRelease;
            
            var wpn = character.Data?.weapon;
            Feel.EffectsManager.Instance?.SpawnMuzzleFlashPattern(character.Position, direction, isShotgun, wpn?.muzzleForwardOffset ?? 1f, wpn?.vfx);
            
            float range = character.Data?.weapon?.range ?? 7f;
            Vector2 endPos = character.Position + direction.normalized * range;
            Feel.EffectsManager.Instance?.SpawnTracer(character.Position, endPos, character.Team);
            
            _audio?.PlayShot(isHeavy, isShotgun);

            if (character.AmmoGauge?.IsEmpty == true)
            {
                _logger?.LogGaugeEmpty(character, 0f);
                _audio?.PlayReloadClick();
            }
        }

        private void HandleCharacterRolled(CharacterStateMachine character)
        {
            _logger?.LogRoll(character, character.AimDirection, 0);
            _audio?.PlayRoll();
        }

        private void HandleCharacterSkillUsed(CharacterStateMachine character)
        {
            _logger?.LogSkillUse(character);
            _audio?.PlaySkillActivate();
        }

        private void SpawnCovers()
        {
            if (_battlefieldData?.covers == null || _coverDatas == null) return;

            foreach (var placement in _battlefieldData.covers)
            {
                CoverData coverData = FindCoverData(placement.coverId);
                if (coverData == null) continue;

                Team team = placement.rowIndex < _battlefieldData.ourRowsY.Length ? Team.Player : Team.Enemy;
                float rowY = team == Team.Player 
                    ? _battlefieldData.GetOurRowY(placement.rowIndex)
                    : _battlefieldData.GetEnemyRowY(placement.rowIndex - _battlefieldData.ourRowsY.Length);

                float coverY = rowY + (_battlefieldData.coverOffsetMin + _battlefieldData.coverOffsetMax) / 2f;
                if (team == Team.Player)
                    coverY = rowY + (_battlefieldData.coverOffsetMin + _battlefieldData.coverOffsetMax) / 2f;

                Vector2 position = new Vector2(placement.xPosition, coverY);

                var cover = new CoverInstance();
                cover.Initialize(coverData, position, placement.rowIndex, team);
                
                _allCovers.Add(cover);
                _combatSystem?.RegisterCover(cover);

                CreateCoverVisual(cover);

                cover.OnStageChanged += (c, stage) => 
                {
                    _logger?.LogCoverDamaged(c.Data.id, c.CurrentHp, (int)stage);
                    if (stage != CoverStage.Intact)
                    {
                        _screenShake?.ShakeCover();
                        _audio?.PlayCoverHit();
                    }
                };

                cover.OnDestroyed += (c) =>
                {
                    _logger?.LogCoverDestroyed(c.Data.id);
                    _audio?.PlayCoverBreak();
                };
            }
        }

        private CoverData FindCoverData(string id)
        {
            if (_coverDatas == null) return null;
            foreach (var data in _coverDatas)
            {
                if (data != null && data.id == id)
                    return data;
            }
            return null;
        }

        private void CreateCoverVisual(CoverInstance cover)
        {
            GameObject go;
            if (_coverPrefab != null)
            {
                go = Instantiate(_coverPrefab);
            }
            else
            {
                go = new GameObject($"Cover_{cover.Data.id}");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreatePlaceholderSprite(cover.Data.width);
                sr.color = new Color(0.6f, 0.4f, 0.2f);
            }

            Vector3 worldPos = _battlefieldSystem?.LogicToWorldPosition(cover.Position) ?? 
                              new Vector3(cover.Position.x, cover.Position.y, 0f);
            go.transform.position = worldPos;
            go.transform.localScale = new Vector3(cover.Data.width, cover.Data.depth * 2f, 1f);
        }

        private void SpawnTargets()
        {
            if (_targetDatas == null) return;

            float[] targetXPositions = { 6f, 12f, 18f };
            float targetY = _battlefieldData?.enemyRowsY[0] ?? 11f;

            for (int i = 0; i < Mathf.Min(_targetDatas.Length, targetXPositions.Length); i++)
            {
                if (_targetDatas[i] == null) continue;

                Vector2 position = new Vector2(targetXPositions[i], targetY);
                var target = new TargetInstance();
                target.Initialize(_targetDatas[i], position, Team.Enemy);

                _allTargets.Add(target);
                _combatSystem?.RegisterTarget(target);

                CreateTargetVisual(target, i);
            }
        }

        private void CreateTargetVisual(TargetInstance target, int index)
        {
            GameObject go;
            if (_targetPrefab != null)
            {
                go = Instantiate(_targetPrefab);
            }
            else
            {
                go = new GameObject($"Target_{target.Data.id}_{index}");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreateTargetSprite();
                sr.color = new Color(1f, 0.8f, 0.3f);

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(go.transform);
                labelGo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                var label = labelGo.AddComponent<TextMesh>();
                string typeLabel = target.Data.shoots ? "사격" : (target.Data.moves ? "이동" : "고정");
                label.text = $"과녁 ({typeLabel})";
                label.fontSize = 32;
                label.characterSize = 0.06f;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.color = Color.white;
            }

            Vector3 worldPos = _battlefieldSystem?.LogicToWorldPosition(target.Position) ?? 
                              new Vector3(target.Position.x, target.Position.y, 0f);
            go.transform.position = worldPos;
        }

        private Sprite CreateTargetSprite()
        {
            int size = 64;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < 8)
                        pixels[y * size + x] = Color.white;
                    else if (dist < 12)
                        pixels[y * size + x] = new Color(1f, 0.8f, 0.3f);
                    else if (dist < 20)
                        pixels[y * size + x] = Color.white;
                    else if (dist < 24)
                        pixels[y * size + x] = new Color(1f, 0.8f, 0.3f);
                    else if (dist < size / 2f - 2)
                        pixels[y * size + x] = Color.white;
                    else if (dist < size / 2f)
                        pixels[y * size + x] = new Color(0.3f, 0.3f, 0.3f);
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        private Sprite CreatePlaceholderSprite(float size)
        {
            int texSize = Mathf.RoundToInt(64 * size);
            texSize = Mathf.Max(16, texSize);
            
            var texture = new Texture2D(texSize, texSize);
            Color[] pixels = new Color[texSize * texSize];
            
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dx = x - texSize / 2f;
                    float dy = y - texSize / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist < texSize / 2f - 2)
                    {
                        pixels[y * texSize + x] = Color.white;
                    }
                    else if (dist < texSize / 2f)
                    {
                        pixels[y * texSize + x] = Color.black;
                    }
                    else
                    {
                        pixels[y * texSize + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 64f);
        }

        public void SwitchCharacter(int index)
        {
            if (_characterDatas == null || index < 0 || index >= _characterDatas.Length) return;

            Vector2 currentPos = _playerCharacter?.Position ?? new Vector2(12f, 4.5f);
            int currentRow = _playerCharacter?.CurrentRowIndex ?? 1;

            if (_playerCharacter != null)
            {
                _combatSystem?.UnregisterCharacter(_playerCharacter);
                _debugOverlay?.UnregisterCharacter(_playerCharacter);
                Destroy(_playerCharacter.gameObject);
                _allCharacters.Remove(_playerCharacter);
            }

            CharacterData data = _characterDatas[index];
            _playerCharacter = CreateCharacter(data, Team.Player, 1, currentPos, currentRow);

            if (_inputHandler != null)
            {
                _inputHandler.SetReceiver(_playerCharacter);
            }

            if (_hud != null)
            {
                _hud.SetCharacter(_playerCharacter);
            }

            _placeholderHUD?.SetCharacter(_playerCharacter);

            _logger?.LogCharacterSelect(_playerCharacter);
        }

        public void ResetTargets()
        {
            foreach (var target in _allTargets)
            {
                target.Reset();
            }
        }

        public void ResetCovers()
        {
            foreach (var cover in _allCovers)
            {
                cover.Reset();
            }
        }

        private void SpawnDummyAlly()
        {
            Vector2 allyPos = new Vector2(18f, 2f);
            int allyRow = 0;

            GameObject go;
            if (_dummyAllyPrefab != null)
            {
                go = Instantiate(_dummyAllyPrefab);
            }
            else
            {
                go = new GameObject("DummyAlly");
                go.AddComponent<DummyAlly>();
            }

            var ally = go.GetComponent<DummyAlly>();
            ally.Initialize(allyPos, allyRow, _downedData);
            ally.SetPosition(allyPos);

            _dummyAllies.Add(ally);
            _reviveSystem?.RegisterDummyAlly(ally);

            ally.OnRevived += (a) => 
            {
                Debug.Log("Dummy ally revived!");
            };

            ally.OnEliminated += (a) =>
            {
                Debug.Log("Dummy ally eliminated! Press R to reset.");
            };
        }

        public void ResetDummyAllies()
        {
            foreach (var ally in _dummyAllies)
            {
                if (ally != null)
                {
                    ally.ResetToActive();
                }
            }
        }

        public void DownDummyAllies()
        {
            foreach (var ally in _dummyAllies)
            {
                if (ally != null && ally.State == CharacterOverallState.Active)
                {
                    ally.GoDown();
                }
            }
        }

        private void Update()
        {
            HandleDebugCharacterSwitch();
        }

        private void HandleDebugCharacterSwitch()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null) return;

            if (UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame)
                SwitchCharacter(0);
            if (UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame)
                SwitchCharacter(1);
            if (UnityEngine.InputSystem.Keyboard.current.digit3Key.wasPressedThisFrame)
                SwitchCharacter(2);
            if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetTargets();
                ResetCovers();
                ResetDummyAllies();
            }
            if (UnityEngine.InputSystem.Keyboard.current.kKey.wasPressedThisFrame)
            {
                DownDummyAllies();
            }
        }

        public void RegisterCharacterWithReviveSystem(CharacterStateMachine character)
        {
            _reviveSystem?.RegisterCharacter(character);
        }
    }
}

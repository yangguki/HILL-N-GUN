using UnityEngine;
using System.Collections.Generic;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private CharacterData[] _availableCharacters;

        private GUIStyle _titleStyle;
        private GUIStyle _charNameStyle;
        private GUIStyle _charStatsStyle;
        private GUIStyle _slotStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _backButtonStyle;
        private GUIStyle _selectedStyle;
        private bool _stylesInitialized;

        private LocalizationManager _localization;
        private SpriteLibrary _spriteLibrary;
        private Dictionary<string, Texture2D> _characterPortraits = new Dictionary<string, Texture2D>();
        private string[] _selectedIds = new string[3];
        private int _activeSlot = 0;
        private int _hoveredChar = -1;

        private void Awake()
        {
            _selectedIds[0] = "buster";
            _selectedIds[1] = "raven";
            _selectedIds[2] = "volt";
        }

        private void Start()
        {
            _localization = ServiceLocator.Get<LocalizationManager>();
            _spriteLibrary = ServiceLocator.Get<SpriteLibrary>();
            
            if (_availableCharacters == null || _availableCharacters.Length == 0)
            {
                var assetManager = RuntimeAssetManager.Instance;
                if (assetManager != null)
                {
                    _availableCharacters = assetManager.CharacterDatas;
                }
            }
            
            CacheCharacterPortraits();
        }

        private void CacheCharacterPortraits()
        {
            if (_spriteLibrary == null || _availableCharacters == null) return;
            
            foreach (var charData in _availableCharacters)
            {
                if (charData == null) continue;
                var sprites = _spriteLibrary.GetCharacterSprites(charData.id);
                var art = sprites?.portrait != null ? sprites.portrait : sprites?.idle;
                if (art != null)
                {
                    _characterPortraits[charData.id] = SpriteToTexture(art);
                }
            }
        }

        private Texture2D SpriteToTexture(Sprite sprite)
        {
            if (sprite == null) return null;
            if (!sprite.texture.isReadable) return sprite.texture; // single-sprite texture: use as-is
            
            var rect = sprite.textureRect;
            var tex = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
            var pixels = sprite.texture.GetPixels(
                (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = sprite.texture.filterMode;
            return tex;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 42;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.normal.textColor = new Color(1f, 0.9f, 0.7f);

            _charNameStyle = new GUIStyle(GUI.skin.label);
            _charNameStyle.fontSize = 24;
            _charNameStyle.fontStyle = FontStyle.Bold;
            _charNameStyle.alignment = TextAnchor.MiddleCenter;
            _charNameStyle.normal.textColor = Color.white;

            _charStatsStyle = new GUIStyle(GUI.skin.label);
            _charStatsStyle.fontSize = 14;
            _charStatsStyle.alignment = TextAnchor.MiddleLeft;
            _charStatsStyle.normal.textColor = new Color(0.8f, 0.75f, 0.65f);

            _slotStyle = new GUIStyle(GUI.skin.box);
            _slotStyle.normal.background = MakeTexture(1, 1, new Color(0.25f, 0.2f, 0.15f));
            _slotStyle.fontSize = 16;
            _slotStyle.alignment = TextAnchor.MiddleCenter;
            _slotStyle.normal.textColor = Color.white;

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 26;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = new Color(1f, 0.9f, 0.6f);
            _buttonStyle.normal.background = MakeTexture(1, 1, new Color(0.4f, 0.55f, 0.35f));
            _buttonStyle.hover.background = MakeTexture(1, 1, new Color(0.5f, 0.7f, 0.45f));
            _buttonStyle.padding = new RectOffset(25, 25, 15, 15);

            _backButtonStyle = new GUIStyle(GUI.skin.button);
            _backButtonStyle.fontSize = 20;
            _backButtonStyle.normal.textColor = new Color(0.9f, 0.8f, 0.7f);
            _backButtonStyle.hover.textColor = Color.white;
            _backButtonStyle.normal.background = MakeTexture(1, 1, new Color(0.3f, 0.25f, 0.2f));
            _backButtonStyle.hover.background = MakeTexture(1, 1, new Color(0.45f, 0.35f, 0.25f));
            _backButtonStyle.padding = new RectOffset(15, 15, 10, 10);

            _selectedStyle = new GUIStyle(GUI.skin.box);
            _selectedStyle.normal.background = MakeTexture(1, 1, new Color(0.5f, 0.4f, 0.25f));

            _stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            InitStyles();

            DrawBackground();
            DrawTitle();
            DrawTeamSlots();
            DrawCharacterGrid();
            DrawSelectedCharacterInfo();
            DrawStartButton();
            DrawBackButton();
        }

        private void DrawBackground()
        {
            Color bgColor = new Color(0.16f, 0.13f, 0.1f);
            GUI.color = bgColor;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawTitle()
        {
            string title = GetLocalizedString("ui.select_team", "팀 구성");
            GUI.Label(new Rect(0, Screen.height * 0.04f, Screen.width, 60f), title, _titleStyle);
        }

        private void DrawTeamSlots()
        {
            float slotWidth = 140f;
            float slotHeight = 160f;
            float spacing = 20f;
            float totalWidth = slotWidth * 3 + spacing * 2;
            float startX = (Screen.width - totalWidth) / 2f;
            float startY = Screen.height * 0.12f;

            for (int i = 0; i < 3; i++)
            {
                float x = startX + i * (slotWidth + spacing);
                bool isActive = _activeSlot == i;

                Color bgColor = isActive ? new Color(0.4f, 0.35f, 0.25f) : new Color(0.25f, 0.2f, 0.15f);
                GUI.color = bgColor;
                GUI.DrawTexture(new Rect(x, startY, slotWidth, slotHeight), Texture2D.whiteTexture);
                GUI.color = Color.white;

                if (isActive)
                {
                    DrawBorder(x, startY, slotWidth, slotHeight, new Color(0.9f, 0.7f, 0.4f), 3f);
                }

                string slotLabel = $"#{i + 1}";
                GUI.Label(new Rect(x, startY + 5f, slotWidth, 25f), slotLabel, _slotStyle);

                CharacterData charData = GetCharacterById(_selectedIds[i]);
                if (charData != null)
                {
                    DrawCharacterIcon(x + slotWidth / 2f - 37f, startY + 28f, 74f, charData);

                    string charName = GetLocalizedString(charData.nameKey, charData.id);
                    GUIStyle smallNameStyle = new GUIStyle(_charNameStyle);
                    smallNameStyle.fontSize = 16;
                    GUI.Label(new Rect(x, startY + 105f, slotWidth, 25f), charName, smallNameStyle);

                    string roleText = GetRoleText(charData.role);
                    GUIStyle roleStyle = new GUIStyle(_charStatsStyle);
                    roleStyle.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(new Rect(x, startY + 130f, slotWidth, 20f), roleText, roleStyle);
                }

                if (GUI.Button(new Rect(x, startY, slotWidth, slotHeight), "", GUIStyle.none))
                {
                    _activeSlot = i;
                }
            }
        }

        private void DrawCharacterGrid()
        {
            if (_availableCharacters == null || _availableCharacters.Length == 0) return;

            float cardWidth = 180f;
            float cardHeight = 200f;
            float spacing = 25f;
            float totalWidth = cardWidth * 3 + spacing * 2;
            float startX = (Screen.width - totalWidth) / 2f;
            float startY = Screen.height * 0.38f;

            for (int i = 0; i < _availableCharacters.Length && i < 3; i++)
            {
                CharacterData charData = _availableCharacters[i];
                if (charData == null) continue;

                float x = startX + i * (cardWidth + spacing);
                bool isSelected = IsCharacterSelected(charData.id);
                bool isHovered = _hoveredChar == i;

                Color bgColor = isSelected 
                    ? new Color(0.35f, 0.45f, 0.35f)
                    : (isHovered ? new Color(0.35f, 0.28f, 0.2f) : new Color(0.22f, 0.18f, 0.14f));

                GUI.color = bgColor;
                GUI.DrawTexture(new Rect(x, startY, cardWidth, cardHeight), Texture2D.whiteTexture);
                GUI.color = Color.white;

                if (isSelected)
                {
                    DrawBorder(x, startY, cardWidth, cardHeight, new Color(0.5f, 0.7f, 0.5f), 3f);
                }

                DrawCharacterIcon(x + cardWidth / 2f - 42f, startY + 8f, 86f, charData);

                string charName = GetLocalizedString(charData.nameKey, charData.id);
                GUI.Label(new Rect(x, startY + 95f, cardWidth, 30f), charName, _charNameStyle);

                float statsX = x + 15f;
                float statsY = startY + 125f;
                float lineHeight = 18f;

                string hpText = $"HP: {charData.maxHp}";
                GUI.Label(new Rect(statsX, statsY, cardWidth - 30f, lineHeight), hpText, _charStatsStyle);

                string roleText = GetRoleText(charData.role);
                GUI.Label(new Rect(statsX, statsY + lineHeight, cardWidth - 30f, lineHeight), roleText, _charStatsStyle);

                string weaponText = charData.weapon != null ? $"사거리: {charData.weapon.range}m" : "";
                GUI.Label(new Rect(statsX, statsY + lineHeight * 2, cardWidth - 30f, lineHeight), weaponText, _charStatsStyle);

                Rect cardRect = new Rect(x, startY, cardWidth, cardHeight);
                if (cardRect.Contains(Event.current.mousePosition))
                {
                    _hoveredChar = i;
                }
                else if (_hoveredChar == i)
                {
                    _hoveredChar = -1;
                }

                if (GUI.Button(cardRect, "", GUIStyle.none))
                {
                    SelectCharacter(charData.id);
                }
            }
        }

        private void DrawCharacterIcon(float x, float y, float size, CharacterData charData)
        {
            Texture2D portrait = null;
            if (charData != null && _characterPortraits.TryGetValue(charData.id, out portrait) && portrait != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(x, y, size, size), portrait, ScaleMode.ScaleToFit);
            }
            else
            {
                Color roleColor = charData?.role switch
                {
                    Role.Assault => new Color(0.7f, 0.4f, 0.3f),
                    Role.Sniper => new Color(0.4f, 0.5f, 0.7f),
                    Role.Support => new Color(0.4f, 0.6f, 0.45f),
                    _ => Color.gray
                };

                GUI.color = roleColor;
                GUI.DrawTexture(new Rect(x, y, size, size), GetCharacterTexture());
                GUI.color = Color.white;
            }
        }

        private Texture2D _characterTexture;
        private Texture2D GetCharacterTexture()
        {
            if (_characterTexture != null) return _characterTexture;

            int size = 64;
            _characterTexture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            float center = size / 2f;

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float dx = px - center;
                    float dy = py - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < size / 2f - 2)
                    {
                        pixels[py * size + px] = Color.white;
                    }
                    else
                    {
                        pixels[py * size + px] = Color.clear;
                    }
                }
            }

            _characterTexture.SetPixels(pixels);
            _characterTexture.Apply();
            return _characterTexture;
        }

        private void DrawSelectedCharacterInfo()
        {
            CharacterData charData = GetCharacterById(_selectedIds[_activeSlot]);
            if (charData == null) return;

            float boxWidth = 300f;
            float boxHeight = 120f;
            float x = Screen.width - boxWidth - 30f;
            float y = Screen.height * 0.65f;

            GUI.color = new Color(0.2f, 0.17f, 0.13f);
            GUI.DrawTexture(new Rect(x, y, boxWidth, boxHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawBorder(x, y, boxWidth, boxHeight, new Color(0.5f, 0.4f, 0.3f), 2f);

            string charName = GetLocalizedString(charData.nameKey, charData.id);
            GUI.Label(new Rect(x + 15f, y + 10f, boxWidth - 30f, 30f), charName, _charNameStyle);

            if (charData.skill != null)
            {
                string skillName = GetLocalizedString(charData.skill.nameKey, charData.skill.id);
                GUI.Label(new Rect(x + 15f, y + 45f, boxWidth - 30f, 20f), $"스킬: {skillName}", _charStatsStyle);
            }

            if (charData.passive != null)
            {
                string passiveName = GetLocalizedString(charData.passive.nameKey, charData.passive.id);
                GUI.Label(new Rect(x + 15f, y + 65f, boxWidth - 30f, 20f), $"패시브: {passiveName}", _charStatsStyle);
            }

            if (charData.weapon != null)
            {
                string dmgText = charData.weapon.projectilesPerShot > 1
                    ? $"공격력: {charData.weapon.damagePerProjectile}×{charData.weapon.projectilesPerShot}"
                    : $"공격력: {charData.weapon.damagePerProjectile}";
                GUI.Label(new Rect(x + 15f, y + 85f, boxWidth - 30f, 20f), dmgText, _charStatsStyle);
            }
        }

        private void DrawStartButton()
        {
            float buttonWidth = 220f;
            float buttonHeight = 55f;
            float x = (Screen.width - buttonWidth) / 2f;
            float y = Screen.height - buttonHeight - 40f;

            string startText = GetLocalizedString("ui.start_match", "전투 시작");
            if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), startText, _buttonStyle))
            {
                StartMatch();
            }
        }

        private void DrawBackButton()
        {
            float buttonWidth = 120f;
            float buttonHeight = 40f;
            float x = 30f;
            float y = Screen.height - buttonHeight - 30f;

            string backText = GetLocalizedString("ui.back", "뒤로");
            if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), backText, _backButtonStyle))
            {
                if (GameFlowManager.Instance?.CurrentSession?.mode == GameMode.PvE) LobbyUI.RequestedTab = LobbyTab.Stages;
                GameFlowManager.Instance?.GoToModeSelect();
            }
        }

        private void DrawBorder(float x, float y, float width, float height, Color color, float thickness)
        {
            HillNGun.UI.UIKit.Border(new Rect(x, y, width, height), color, thickness); // one draw call instead of four
        }

        private void SelectCharacter(string charId)
        {
            _selectedIds[_activeSlot] = charId;
        }

        private bool IsCharacterSelected(string charId)
        {
            for (int i = 0; i < _selectedIds.Length; i++)
            {
                if (_selectedIds[i] == charId) return true;
            }
            return false;
        }

        private CharacterData GetCharacterById(string id)
        {
            if (_availableCharacters == null || string.IsNullOrEmpty(id)) return null;

            foreach (var charData in _availableCharacters)
            {
                if (charData != null && charData.id == id)
                    return charData;
            }
            return null;
        }

        private string GetRoleText(Role role)
        {
            return role switch
            {
                Role.Assault => GetLocalizedString("role.assault", "돌격"),
                Role.Sniper => GetLocalizedString("role.sniper", "저격"),
                Role.Support => GetLocalizedString("role.support", "지원"),
                _ => ""
            };
        }

        private void StartMatch()
        {
            var flowManager = GameFlowManager.Instance;
            if (flowManager != null)
            {
                flowManager.SetAllCharacters(_selectedIds[0], _selectedIds[1], _selectedIds[2]);
                flowManager.StartMatch();
            }
        }

        private string GetLocalizedString(string key, string fallback)
        {
            if (_localization != null && _localization.HasString(key))
            {
                return _localization.GetString(key);
            }
            return fallback;
        }
    }
}

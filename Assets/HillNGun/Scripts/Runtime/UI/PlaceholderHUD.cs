using UnityEngine;
using HillNGun.Core;
using HillNGun.Characters;
using HillNGun.Combat;
using HillNGun.Input;

namespace HillNGun.UI
{
    public class PlaceholderHUD : MonoBehaviour
    {
        private CharacterStateMachine _character;
        private Texture2D _roundedBox;
        private Texture2D _circle;
        private Texture2D _pip;
        private bool _initialized;
        
        private int _hitCount;
        private int _totalDamage;
        private float _reloadFlash;
        private Rect _safeArea;
        private bool _isTouchDevice;

        public int HitCount => _hitCount;
        public int TotalDamage => _totalDamage;

        public void SetCharacter(CharacterStateMachine character)
        {
            _character = character;
            if (character != null)
            {
                character.OnFired += (c, d, dmg) => { };
            }
        }

        public void RecordHit(int damage)
        {
            _hitCount++;
            _totalDamage += damage;
        }

        public void ResetScore()
        {
            _hitCount = 0;
            _totalDamage = 0;
        }

        private void Init()
        {
            if (_initialized) return;
            _roundedBox = CreateRoundedBox(64, 64, 12, new Color(0.15f, 0.12f, 0.1f, 0.85f));
            _circle = CreateCircle(64, new Color(0.2f, 0.18f, 0.15f, 0.7f));
            _pip = CreatePip(16, 8);
            _initialized = true;
        }

        private Texture2D CreateRoundedBox(int w, int h, int r, Color color)
        {
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool inside = true;
                    if (x < r && y < r) inside = Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) <= r;
                    else if (x >= w - r && y < r) inside = Vector2.Distance(new Vector2(x, y), new Vector2(w - r - 1, r)) <= r;
                    else if (x < r && y >= h - r) inside = Vector2.Distance(new Vector2(x, y), new Vector2(r, h - r - 1)) <= r;
                    else if (x >= w - r && y >= h - r) inside = Vector2.Distance(new Vector2(x, y), new Vector2(w - r - 1, h - r - 1)) <= r;
                    
                    pixels[y * w + x] = inside ? color : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private Texture2D CreateCircle(int size, Color color)
        {
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01((center - dist) * 2f);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D CreatePip(int w, int h)
        {
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float r = 3f;
                    bool inside = x >= r && x < w - r;
                    if (x < r) inside = Vector2.Distance(new Vector2(x, y), new Vector2(r, h / 2f)) <= r;
                    if (x >= w - r) inside = Vector2.Distance(new Vector2(x, y), new Vector2(w - r - 1, h / 2f)) <= r;
                    pixels[y * w + x] = inside ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void Start()
        {
#if UNITY_ANDROID || UNITY_IOS
            _isTouchDevice = true;
#else
            _isTouchDevice = UnityEngine.InputSystem.Touchscreen.current != null;
#endif
        }

        private void Update()
        {
            if (_character?.AmmoGauge?.IsEmpty == true)
                _reloadFlash += Time.deltaTime * 6f;

            _safeArea = Screen.safeArea;
        }

        private void OnGUI()
        {
            if (_character == null) return;
            Init();

            DrawTopBar();
            DrawAmmoGauge();
            DrawAbilities();
            
            if (!_isTouchDevice)
            {
                DrawVirtualControls();
                DrawHints();
            }
            
            DrawScore();
        }

        private void DrawTopBar()
        {
            float panelW = 280;
            float panelH = 70;
            float x = _safeArea.x + (_safeArea.width - panelW) / 2;
            float y = _safeArea.y + 8;

            GUI.DrawTexture(new Rect(x, y, panelW, panelH), _roundedBox, ScaleMode.StretchToFill);

            var nameStyle = new GUIStyle { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            nameStyle.normal.textColor = GetRoleColor(_character.Data?.role ?? Role.Assault);
            
            string charName = _character.Data?.id?.ToUpper() ?? "PLAYER";
            GUI.Label(new Rect(x, y + 5, panelW, 25), charName, nameStyle);

            float hpBarX = x + 15;
            float hpBarY = y + 32;
            float hpBarW = panelW - 30;
            float hpBarH = 18;

            DrawRoundedBar(new Rect(hpBarX, hpBarY, hpBarW, hpBarH), _character.HpRatio, 
                new Color(0.2f, 0.6f, 0.3f), new Color(0.8f, 0.2f, 0.2f), new Color(0.1f, 0.1f, 0.1f, 0.8f));

            var hpStyle = new GUIStyle { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            hpStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(hpBarX, hpBarY, hpBarW, hpBarH), $"{_character.CurrentHp} / {_character.MaxHp}", hpStyle);

            var roleStyle = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            roleStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            GUI.Label(new Rect(x, y + 52, panelW, 15), GetRoleName(_character.Data?.role ?? Role.Assault), roleStyle);
        }

        private void DrawAmmoGauge()
        {
            var gauge = _character.AmmoGauge;
            if (gauge == null) return;

            float pipW = 45;
            float pipH = 22;
            float spacing = 6;
            float totalW = gauge.MaxCells * (pipW + spacing) - spacing;
            float x = (Screen.width - totalW) / 2;
            float y = 85;

            for (int i = 0; i < gauge.MaxCells; i++)
            {
                float pipX = x + i * (pipW + spacing);
                float fill = gauge.GetCellFillRatio(i);

                Color bgColor = new Color(0.15f, 0.12f, 0.1f, 0.8f);
                GUI.color = bgColor;
                GUI.DrawTexture(new Rect(pipX, y, pipW, pipH), _pip, ScaleMode.StretchToFill);

                if (fill > 0)
                {
                    Color fillColor = fill >= 1f ? new Color(1f, 0.85f, 0.2f) : new Color(0.6f, 0.5f, 0.2f);
                    GUI.color = fillColor;
                    GUI.DrawTexture(new Rect(pipX, y, pipW * fill, pipH), _pip, ScaleMode.StretchToFill);
                }
                GUI.color = Color.white;
            }

            if (gauge.IsEmpty)
            {
                float flash = Mathf.Abs(Mathf.Sin(_reloadFlash));
                var reloadStyle = new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                reloadStyle.normal.textColor = Color.Lerp(new Color(1f, 0.3f, 0.2f), new Color(1f, 0.8f, 0.2f), flash);
                GUI.Label(new Rect(x, y + pipH + 4, totalW, 20), "장전 중...", reloadStyle);
            }
        }

        private void DrawAbilities()
        {
            float btnSize = 55;
            float y = Screen.height - btnSize - 15;
            float spacing = 10;

            float rollCd = _character.RollCooldownRatio;
            float skillCd = _character.SkillCooldownRatio;
            int grenades = _character.GrenadeSystem?.CurrentGrenades ?? 0;

            DrawAbilityButton(new Rect(15, y, btnSize, btnSize), "구르기", 1f - rollCd, new Color(0.3f, 0.6f, 0.3f), "Space");
            DrawAbilityButton(new Rect(15 + btnSize + spacing, y, btnSize, btnSize), "수류탄", grenades > 0 ? 1f : 0f, new Color(0.6f, 0.5f, 0.2f), $"x{grenades}");
            DrawAbilityButton(new Rect(15 + (btnSize + spacing) * 2, y, btnSize, btnSize), "스킬", 1f - skillCd, new Color(0.5f, 0.3f, 0.6f), "E");
        }

        private void DrawAbilityButton(Rect rect, string name, float ready, Color color, string hint)
        {
            GUI.DrawTexture(rect, _roundedBox, ScaleMode.StretchToFill);

            if (ready < 1f && ready > 0f)
            {
                Color cdColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
                float cdH = rect.height * (1f - ready);
                GUI.color = cdColor;
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, cdH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            var nameStyle = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            nameStyle.normal.textColor = ready >= 1f ? color : new Color(0.5f, 0.5f, 0.5f);
            GUI.Label(new Rect(rect.x, rect.y + 8, rect.width, 20), name, nameStyle);

            var hintStyle = new GUIStyle { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            hintStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(rect.x, rect.y + rect.height - 18, rect.width, 15), hint, hintStyle);
        }

        private void DrawVirtualControls()
        {
            float size = 100;
            float margin = 25;

            GUI.DrawTexture(new Rect(margin, Screen.height - size - margin - 70, size, size), _circle, ScaleMode.StretchToFill);
            var moveStyle = new GUIStyle { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            moveStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(margin, Screen.height - size - margin - 70, size, size), "WASD\n이동", moveStyle);

            float rightX = Screen.width - size - margin;
            GUI.DrawTexture(new Rect(rightX, Screen.height - size - margin - 20, size, size), _circle, ScaleMode.StretchToFill);
            GUI.Label(new Rect(rightX, Screen.height - size - margin - 20, size, size), "마우스\n조준/사격", moveStyle);
        }

        private void DrawScore()
        {
            float w = 120;
            float h = 50;
            float x = Screen.width - w - 10;
            float y = Screen.height - h - 10;

            GUI.DrawTexture(new Rect(x, y, w, h), _roundedBox, ScaleMode.StretchToFill);

            var titleStyle = new GUIStyle { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            GUI.Label(new Rect(x, y + 5, w, 18), "연습장", titleStyle);

            var scoreStyle = new GUIStyle { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            scoreStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y + 25, w, 20), $"명중: {_hitCount}  DMG: {_totalDamage}", scoreStyle);
        }

        private void DrawHints()
        {
            var style = new GUIStyle { fontSize = 11, alignment = TextAnchor.UpperRight };
            style.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            
            string hints = "1/2/3 캐릭터\nR 리셋\nK 아군기절\nF5 디버그";
            GUI.Label(new Rect(Screen.width - 100, 85, 90, 80), hints, style);
        }

        private void DrawRoundedBar(Rect rect, float ratio, Color fullColor, Color emptyColor, Color bgColor)
        {
            GUI.color = bgColor;
            GUI.DrawTexture(rect, _pip, ScaleMode.StretchToFill);

            if (ratio > 0)
            {
                Color barColor = Color.Lerp(emptyColor, fullColor, ratio);
                GUI.color = barColor;
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * ratio, rect.height), _pip, ScaleMode.StretchToFill);
            }
            GUI.color = Color.white;
        }

        private Color GetRoleColor(Role role)
        {
            switch (role)
            {
                case Role.Assault: return new Color(1f, 0.5f, 0.3f);
                case Role.Sniper: return new Color(0.6f, 0.4f, 0.9f);
                case Role.Support: return new Color(0.4f, 0.8f, 1f);
                default: return Color.white;
            }
        }

        private string GetRoleName(Role role)
        {
            switch (role)
            {
                case Role.Assault: return "돌격";
                case Role.Sniper: return "저격";
                case Role.Support: return "지원";
                default: return "";
            }
        }
    }
}

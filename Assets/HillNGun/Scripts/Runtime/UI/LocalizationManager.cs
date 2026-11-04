using System.Collections.Generic;
using System.IO;
using UnityEngine;
using HillNGun.Core;

namespace HillNGun.UI
{
    public class LocalizationManager : MonoBehaviour
    {
        [SerializeField] private TextAsset _defaultStringsFile;

        private Dictionary<string, string> _strings = new Dictionary<string, string>();
        private string _currentLanguage = "ko";

        private void Awake()
        {
            _currentLanguage = HillNGun.Settings.SettingsService.Instance.Data.language ?? "ko";
            LoadStrings();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<LocalizationManager>();
        }

        [Tooltip("Extra language tables named strings_<code>.csv. Missing keys fall back to Korean (base).")]
        [SerializeField] private TextAsset[] _languageFiles;

        public string CurrentLanguage => _currentLanguage;

        public void LoadStrings()
        {
            _strings.Clear();

            if (_defaultStringsFile != null)
            {
                ParseCsv(_defaultStringsFile.text);
            }
            else
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Localization", "strings_ko.csv");
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    ParseCsv(content);
                }
                else
                {
                    LoadDefaultStrings();
                }
            }

            if (_currentLanguage != "ko" && _languageFiles != null)
            {
                foreach (var f in _languageFiles)
                    if (f != null && f.name == "strings_" + _currentLanguage) ParseCsv(f.text);
            }
        }

        /// <summary>RFC-4180-ish CSV: key,value with quoted values that may contain commas, "" and newlines.</summary>
        private void ParseCsv(string csvContent)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new System.Text.StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < csvContent.Length; i++)
            {
                char ch = csvContent[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < csvContent.Length && csvContent[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(ch);
                }
                else if (ch == '"') inQuotes = true;
                else if (ch == ',') { row.Add(field.ToString()); field.Clear(); }
                else if (ch == '\r') { }
                else if (ch == '\n') { row.Add(field.ToString()); field.Clear(); rows.Add(row); row = new List<string>(); }
                else field.Append(ch);
            }
            if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }

            for (int r = 1; r < rows.Count; r++) // row 0 = header
            {
                var cols = rows[r];
                if (cols.Count < 2) continue;
                string key = cols[0].Trim();
                if (string.IsNullOrEmpty(key) || key.StartsWith("#")) continue;
                string value = cols[1];
                for (int c = 2; c < cols.Count; c++) value += "," + cols[c]; // unquoted commas
                _strings[key] = value.Trim().Replace("\\n", "\n");
            }
        }

        private void LoadDefaultStrings()
        {
            _strings["char.buster.name"] = "버스터";
            _strings["char.raven.name"] = "레이븐";
            _strings["char.volt.name"] = "볼트";
            _strings["ui.reloading"] = "장전 중";
            _strings["ui.downed"] = "기절";
            _strings["ui.reviving"] = "소생 중";
            _strings["ui.skill_ready"] = "스킬 준비됨";
            _strings["ui.grenade"] = "수류탄";
            _strings["ui.roll"] = "구르기";
            _strings["skill.breach.name"] = "돌파 사격";
            _strings["skill.pierce.name"] = "관통탄";
            _strings["skill.repair.name"] = "긴급 보수";
            _strings["passive.roll_shot.name"] = "사격 집중";
            _strings["passive.weakpoint.name"] = "약점 사격";
            _strings["passive.fast_revive.name"] = "신속 소생";
        }

        public string GetString(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            return _strings.TryGetValue(key, out string value) ? value : key;
        }

        public string GetString(string key, params object[] args)
        {
            string template = GetString(key);
            return string.Format(template, args);
        }

        public bool HasString(string key)
        {
            return _strings.ContainsKey(key);
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode) || languageCode == _currentLanguage) return;
            _currentLanguage = languageCode;
            LoadStrings();
        }
    }
}

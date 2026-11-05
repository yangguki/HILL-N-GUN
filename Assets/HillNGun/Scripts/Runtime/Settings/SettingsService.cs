using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Feel;
using HillNGun.Progression;

namespace HillNGun.Settings
{
    /// <summary>Player settings saved locally (FP 1254 "설정·기록 로컬 저장"). Separate file from progression.</summary>
    [Serializable]
    public class SettingsData
    {
        public int version = 1;
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.7f;
        public bool soundOn = true;
        public bool vibrationOn = true;   // FP 1006 접근성: 진동 끄기
        public bool screenShakeOn = true; // FP 1006 접근성: 흔들림 끄기
        public int buttonSizeIndex = 1;   // 02 §1: 버튼 크기 3단계
        public string language = "ko";
        public bool tutorialDone;
        /// <summary>Scale A/B test (ScalePresetData option 1/2/3); 0 = data default.</summary>
        public int scalePreset;
    }

    /// <summary>Loads, applies and saves SettingsData. Applies to audio, feel (shake/vibration), input scale and language.</summary>
    public class SettingsService
    {
        public const string DefaultFile = "hillngun_settings.json";
        public static string SaveFileOverride;
        public static readonly string[] Languages = { "ko", "en" };

        private static SettingsService _instance;
        public static SettingsService Instance => _instance ??= new SettingsService(new FileSaveStorage(SaveFileOverride ?? DefaultFile));

        private readonly ISaveStorage _storage;
        public SettingsData Data { get; private set; }
        public event Action OnChanged;
        public event Action OnLanguageChanged;

        public SettingsService(ISaveStorage storage)
        {
            _storage = storage;
            Load();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var s = Instance;
            ApplyMobileResolution();
            SceneManager.sceneLoaded += (scene, mode) => s.Apply();
            s.Apply();
            Debug.Log($"[Settings] Loaded {s._storage.Location}: vol={s.Data.masterVolume:F2} sound={s.Data.soundOn} vib={s.Data.vibrationOn} shake={s.Data.screenShakeOn} btn={s.Data.buttonSizeIndex} lang={s.Data.language} tutorialDone={s.Data.tutorialDone}");
        }

        /// <summary>Mobile: render at a fixed height so menus stay readable and low-end phones keep 60fps (data: UIThemeData.mobileRenderHeight).</summary>
        private static void ApplyMobileResolution()
        {
            if (!Application.isMobilePlatform) return;
            var theme = ServiceLocator.Get<UIThemeData>() ?? RuntimeAssetManager.Instance?.UITheme;
            int h = theme != null ? theme.mobileRenderHeight : 0;
            if (h <= 0 || Screen.height <= h) return;
            int w = Mathf.RoundToInt(Screen.width * (h / (float)Screen.height));
            Screen.SetResolution(w, h, true);
            Debug.Log($"[Settings] Mobile render resolution {w}x{h}");
        }

        public void Load()
        {
            Data = null;
            if (_storage.TryRead(out string json))
            {
                try { Data = JsonUtility.FromJson<SettingsData>(json); }
                catch (Exception e) { Debug.LogWarning($"[Settings] Corrupt settings, using defaults: {e.Message}"); }
            }
            if (Data == null) Data = new SettingsData();
            if (Array.IndexOf(Languages, Data.language) < 0) Data.language = "ko";
            Data.buttonSizeIndex = Mathf.Clamp(Data.buttonSizeIndex, 0, 2);
        }

        public void Save()
        {
            _storage.Write(JsonUtility.ToJson(Data, true));
        }

        /// <summary>Call after changing Data fields from UI.</summary>
        public void Commit(bool languageChanged = false)
        {
            Save();
            Apply();
            OnChanged?.Invoke();
            if (languageChanged) OnLanguageChanged?.Invoke();
        }

        public void SetLanguage(string code)
        {
            if (Data.language == code) return;
            Data.language = code;
            Commit(true);
        }

        public void ResetTutorial()
        {
            Data.tutorialDone = false;
            Commit();
        }

        public void Apply()
        {
            var d = Data;
            AudioListener.volume = d.soundOn ? d.masterVolume : 0f;
            var audio = PlaceholderAudio.Instance;
            if (audio != null)
            {
                audio.SoundEnabled = d.soundOn;
                audio.MusicVolume = d.musicVolume;
                audio.SfxVolume = d.sfxVolume;
            }
            var feel = ServiceLocator.Get<FeelData>() ?? RuntimeAssetManager.Instance?.FeelData;
            if (feel != null)
            {
                // The player's choice lives in the settings file; the asset only holds defaults (in the Editor this also edits the asset in play mode).
                feel.shakeEnabled = d.screenShakeOn;
                feel.vibrationEnabled = d.vibrationOn;
            }
            var input = ServiceLocator.Get<InputData>() ?? RuntimeAssetManager.Instance?.InputData;
            if (input != null) input.currentButtonScaleIndex = d.buttonSizeIndex;
            foreach (var loc in UnityEngine.Object.FindObjectsByType<HillNGun.UI.LocalizationManager>(FindObjectsSortMode.None))
                loc.SetLanguage(d.language);
        }
    }
}

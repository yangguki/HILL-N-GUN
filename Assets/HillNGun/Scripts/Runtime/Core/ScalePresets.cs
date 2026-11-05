using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Data;

namespace HillNGun.Core
{
    /// <summary>Resolved size of one cover under a scale option (tiles / world units at perspective 1).</summary>
    public struct CoverSize
    {
        public CoverSizeType type;
        public bool fromPreset;
        /// <summary>Collision width (tiles): drawn width, never below the protection minimum.</summary>
        public float logicWidth;
        public float visualWidth;
        /// <summary>Drawn on-screen height (world units at perspective 1); &lt;= 0 = keep the model's own proportions.</summary>
        public float visualHeight;
    }

    /// <summary>
    /// Runtime access to <see cref="ScalePresetData"/> (기획 비율 1/2/3안 A/B test). The selection is saved in the
    /// settings file; <c>-scale-preset N</c> on the command line overrides it for automated runs (not saved).
    /// All maths is in the static *For(...) helpers so EditMode tests can check every option without a scene.
    /// Only view sizes and hit / collision sizes read this — ranges, bullet speed, rows, HP and damage do not.
    /// </summary>
    public static class ScalePresets
    {
        private static ScalePresetData _data;
        private static bool _loaded;
        private static int _current = -1;
        private static int _cliOverride = -2;

        public static event Action<int> OnChanged;

        public static ScalePresetData Data
        {
            get
            {
                if (!_loaded) { _loaded = true; if (_data == null) _data = Resources.Load<ScalePresetData>(ScalePresetData.ResourcePath); }
                return _data;
            }
        }

        /// <summary>Tests / tools: use this data and option without touching settings.</summary>
        public static void SetDataForTests(ScalePresetData data, int option)
        {
            _data = data;
            _loaded = data != null; // null = back to the Resources asset on next access
            _current = data != null && data.HasOption(option) ? option : (data != null ? data.defaultOption : -1);
            _mulFor = int.MinValue;
        }

        public static int CliOverride
        {
            get
            {
                if (_cliOverride != -2) return _cliOverride;
                _cliOverride = -1;
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                    if (args[i].Equals("-scale-preset", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[i + 1], out int n)) _cliOverride = n;
                return _cliOverride;
            }
        }

        /// <summary>Current option number (1/2/3). Default = ScalePresetData.defaultOption.</summary>
        public static int Current
        {
            get
            {
                if (_current > 0) return _current;
                var d = Data;
                if (d == null) return 0;
                int n = CliOverride;
                if (!d.HasOption(n)) n = SavedChoice();
                if (!d.HasOption(n)) n = d.defaultOption;
                _current = n;
                return _current;
            }
        }

        private static int SavedChoice()
        {
            try { return HillNGun.Settings.SettingsService.Instance.Data.scalePreset; }
            catch { return 0; }
        }

        public static ScalePresetOption CurrentOption => Data != null ? Data.GetOption(Current) : null;

        // ------------------------------------------------------------ derived values (current option)

        /// <summary>Uniform multiplier for every character / enemy / boss view and hit radius (1 = previous build).</summary>
        public static float CharacterMul
        {
            get
            {
                int cur = Current;
                if (_mulFor != cur) { _mul = CharacterMulFor(Data, CurrentOption); _mulFor = cur; }
                return _mul;
            }
        }
        private static float _mul = 1f;
        private static int _mulFor = int.MinValue;

        /// <summary>Character height H in world units (perspective 1).</summary>
        public static float CharacterHeightWorld => HeightWorldFor(Data, CurrentOption);

        public static CoverSize CoverSizeOf(CoverData cover) => CoverSizeFor(Data, CurrentOption, cover);

        /// <summary>Layout for a stage (StageData.id) or PvP key; fallback when the option has none.</summary>
        public static CoverPlacement[] LayoutFor(string key, CoverPlacement[] fallback)
        {
            var o = CurrentOption;
            var l = o != null ? o.GetLayout(key) : null;
            return l ?? fallback;
        }

        public static string PvpLayoutKey => Data != null ? Data.pvpLayoutKey : "pvp_town_street";

        public static CoverData FindExtraCover(string id)
        {
            var d = Data;
            if (d?.extraCovers == null) return null;
            foreach (var c in d.extraCovers) if (c != null && c.id == id) return c;
            return null;
        }

        // ------------------------------------------------------------ pure maths

        public static float CharacterMulFor(ScalePresetData d, ScalePresetOption o)
        {
            if (d == null || o == null || d.baselineCharacterHeightFrac <= 0f) return 1f;
            return o.characterHeightFrac / d.baselineCharacterHeightFrac;
        }

        public static float HeightWorldFor(ScalePresetData d, ScalePresetOption o)
            => d == null || o == null ? 0f : o.characterHeightFrac * d.screenHeightWorld;

        /// <summary>Hurt diameter (tiles) of a reference character (hitRadius = referenceHurtRadius) under the option.</summary>
        public static float HurtDiameterFor(ScalePresetData d, ScalePresetOption o)
            => d == null ? 1f : 2f * d.referenceHurtRadius * CharacterMulFor(d, o);

        public static CoverSize CoverSizeFor(ScalePresetData d, ScalePresetOption o, CoverData cover)
        {
            float dataWidth = cover != null ? cover.width : 1f;
            var r = new CoverSize { type = CoverSizeType.Crate, fromPreset = false, logicWidth = dataWidth, visualWidth = dataWidth, visualHeight = -1f };
            if (d == null || o == null || cover == null) return r;
            r.type = d.TypeOf(cover.id);
            var e = o.GetSize(r.type);
            float mul = CharacterMulFor(d, o);
            float minW = HurtDiameterFor(d, o) * d.coverMinWidthVsHurtDiameter;
            if (e == null)
            {
                // class not sized by this option: shrink with the characters, keep proportions
                r.visualWidth = dataWidth * mul;
                r.logicWidth = Mathf.Max(r.visualWidth, minW);
                return r;
            }
            float h = HeightWorldFor(d, o);
            r.fromPreset = true;
            r.visualWidth = e.widthH * h;
            r.visualHeight = e.heightH * h;
            r.logicWidth = Mathf.Max(r.visualWidth, minW);
            return r;
        }

        // ------------------------------------------------------------ selection

        /// <summary>Select an option and save it (settings file). Returns false for an unknown option.</summary>
        public static bool Select(int number, bool persist = true)
        {
            var d = Data;
            if (d == null || !d.HasOption(number)) return false;
            bool changed = number != Current;
            _current = number;
            _mulFor = int.MinValue;
            if (persist)
            {
                try
                {
                    var s = HillNGun.Settings.SettingsService.Instance;
                    if (s.Data.scalePreset != number) { s.Data.scalePreset = number; s.Save(); }
                }
                catch (Exception e) { Debug.LogWarning($"[ScalePreset] save failed: {e.Message}"); }
            }
            Debug.Log($"[ScalePreset] option {number} selected (H={o(number).characterHeightFrac:P1} mul={CharacterMulFor(d, o(number)):F3}){(changed ? "" : " (unchanged)")}");
            if (changed) OnChanged?.Invoke(number);
            return true;

            ScalePresetOption o(int n) => d.GetOption(n);
        }

        /// <summary>True in a match scene (PvE / PvP), where a change restarts the current stage.</summary>
        public static bool InMatchScene => SceneManager.GetActiveScene().name.EndsWith("_Match");

        /// <summary>Select + (in a match) restart the current stage with the new sizes and layout.</summary>
        public static void SelectAndRestart(int number)
        {
            if (!Select(number)) return;
            if (!InMatchScene) return;
            var flow = GameFlowManager.Instance;
            if (flow == null) return;
            var pve = UnityEngine.Object.FindFirstObjectByType<HillNGun.PvE.PvEMatchManager>();
            if (pve != null && flow.CurrentSession != null) flow.CurrentSession.startStageIndex = pve.CurrentStageIndex;
            Time.timeScale = 1f;
            Debug.Log($"[ScalePreset] restarting {(pve != null ? "stage " + pve.CurrentStageId : "match")} with option {number}");
            flow.RestartMatch();
        }
    }
}

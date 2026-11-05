using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.PvE;
using HillNGun.Combat;
using HillNGun.Cover;

namespace HillNGun.DebugTools
{
    /// <summary>
    /// -smoketest-scale (with -smoketest-fullpve -scale-preset N): stage 1-1 under one scale preset —
    /// combat screenshot at a fixed stage time, cover-protection proof on every live cover, cover / character
    /// hit counters, then the 1-1 clear time; quits after 1-1. Output: persistentDataPath/scale_smoke/.
    /// </summary>
    public class ScaleSmokeRunner : MonoBehaviour
    {
        private static bool _wanted;
        private static float _shotAt = 14f;
        private static int _switchTo;
        private static int _shotStage;
        private bool _switched;
        private string _dir;
        private PvEMatchManager _pve;
        private bool _shot, _done;
        private int _coverHits, _playerHits;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-smoketest-scale", StringComparison.OrdinalIgnoreCase)) _wanted = true;
                if (args[i].Equals("-scale-shot-at", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length &&
                    float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float t)) _shotAt = t;
                // -scale-switch-to N: after the shot, switch like the pause-menu toggle / F-key (restart current stage), verify, quit
                if (args[i].Equals("-scale-switch-to", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[i + 1], out int n)) _switchTo = n;
                // -scale-shot-stage N: let -smoketest-fullpve auto-play on to stage index N (0 = 1-1 ... 3 = Rex), shoot it, quit
                if (args[i].Equals("-scale-shot-stage", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[i + 1], out int st)) _shotStage = Mathf.Max(0, st);
            }
            if (!_wanted) return;
            var go = new GameObject("[ScaleSmokeRunner]");
            DontDestroyOnLoad(go);
            go.AddComponent<ScaleSmokeRunner>();
        }

        private void Awake()
        {
            _dir = Path.Combine(Application.persistentDataPath, "scale_smoke");
            Directory.CreateDirectory(_dir);
            Debug.Log($"[ScaleSmoke] option={ScalePresets.Current} mul={ScalePresets.CharacterMul:F3} H={ScalePresets.CharacterHeightWorld:F2}wu dir={_dir}");
        }

        private void Update()
        {
            if (_done) return;
            if (SceneManager.GetActiveScene().name.Contains("Result")) { Finish("result scene reached before 1-1 clear log"); return; }
            if (_pve == null)
            {
                _pve = FindFirstObjectByType<PvEMatchManager>();
                if (_pve == null) return;
                if (_switched)
                {
                    StartCoroutine(VerifySwitch());
                    _done = true;
                    return;
                }
                _pve.OnStageComplete += HandleStageComplete;
                var combat = ServiceLocator.Get<CombatSystem>();
                if (combat != null)
                {
                    combat.OnCoverHit += (p, c, d) => { if (_pve != null && _pve.CurrentStageIndex == 0 && c.Team == Team.Player) _coverHits++; };
                    combat.OnCharacterHit += (p, c, d) => { if (_pve != null && _pve.CurrentStageIndex == 0 && c.Team == Team.Player) _playerHits++; };
                }
            }
            if (!_shot && _pve.CurrentStageIndex == _shotStage && _pve.IsMatchActive && _pve.StageTimer >= _shotAt)
            {
                _shot = true;
                string path = Path.Combine(_dir, _shotStage > 0 ? $"stage{_shotStage}_{_pve.CurrentStageId}_opt{ScalePresets.Current}.png" : $"opt{ScalePresets.Current}.png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log($"[ScaleSmoke] SHOT {path} at stage t={_pve.StageTimer:F1}s");
                LogCovers();
                if (_shotStage > 0) { Finish($"stage {_pve.CurrentStageId} shot"); return; }
                if (_switchTo > 0)
                {
                    Debug.Log($"[ScaleSmoke] SWITCH {ScalePresets.Current} -> {_switchTo} mid-fight (stage {_pve.CurrentStageId} t={_pve.StageTimer:F1}s)");
                    _switched = true;
                    _pve.OnStageComplete -= HandleStageComplete;
                    _pve = null;
                    ScalePresets.SelectAndRestart(_switchTo);
                }
            }
        }

        private void LogCovers()
        {
            var combat = ServiceLocator.Get<CombatSystem>();
            var field = RuntimeAssetManager.Instance?.BattlefieldData ?? ServiceLocator.Get<BattlefieldData>();
            if (combat == null || field == null) return;
            float hurt = (RuntimeAssetManager.Instance?.GetCharacterById("buster")?.hitRadius ?? 0.5f) * ScalePresets.CharacterMul;
            float bullet = 0.15f;
            var mook = RuntimeAssetManager.Instance?.GetEnemyById("pistol_mook");
            if (mook != null) bullet = mook.bulletRadius;
            float shooterY = field.GetEnemyRowY(0);
            int ok = 0, total = 0;
            foreach (var c in combat.Covers)
            {
                if (c == null || c.Team != Team.Player) continue;
                float rowY = field.GetOurRowY(c.RowIndex);
                var r = c.GetBounds();
                bool center = CoverProtection.ProtectsStraightShot(r, rowY, shooterY, 0f, bullet, hurt);
                bool left = CoverProtection.ProtectsStraightShot(r, rowY, shooterY, -hurt, bullet, hurt);
                bool right = CoverProtection.ProtectsStraightShot(r, rowY, shooterY, hurt, bullet, hurt);
                total++; if (center && left && right) ok++;
                Debug.Log($"[ScaleSmoke] COVER {c.Data.id} row={c.RowIndex} x={c.Position.x:F1} logicW={c.Width:F2} drawW={c.Size.visualWidth:F2} drawH={c.Size.visualHeight:F2} type={c.Size.type} " +
                          $"protect(center={center} left={left} right={right}) hurtR={hurt:F2} bulletR={bullet:F2} destroyed={c.IsDestroyed}");
            }
            Debug.Log($"[ScaleSmoke] PROTECTION option={ScalePresets.Current} {ok}/{total} covers protect a tucked character (center+both hurt edges)");
        }

        private System.Collections.IEnumerator VerifySwitch()
        {
            yield return new WaitForSecondsRealtime(3f); // scene reload settles; old match objects are gone
            _pve = FindFirstObjectByType<PvEMatchManager>();
            var combat = ServiceLocator.Get<CombatSystem>();
            int covers = combat?.Covers?.Count ?? -1;
            float hr = 0f;
            var me = FindFirstObjectByType<HillNGun.Characters.CharacterStateMachine>();
            if (me != null) hr = me.HitRadius;
            int saved = HillNGun.Settings.SettingsService.Instance.Data.scalePreset;
            Debug.Log($"[ScaleSmoke] SWITCH RESULT option={ScalePresets.Current} saved={saved} stage={_pve?.CurrentStageId} t={_pve?.StageTimer:F1}s covers={covers} hitRadius={hr:F3} mul={ScalePresets.CharacterMul:F3} ok={(ScalePresets.Current == _switchTo && saved == _switchTo)}");
            LogCovers();
            string path = Path.Combine(_dir, $"switch_to_opt{ScalePresets.Current}.png");
            ScreenCapture.CaptureScreenshot(path);
            Finish("switch verified");
        }

        private void HandleStageComplete(PvERoundResult result)
        {
            if (_done || _pve == null || _pve.CurrentStageIndex != 0 || _shotStage > 0) return;
            Debug.Log($"[ScaleSmoke] CLEAR option={ScalePresets.Current} stage={_pve.CurrentStageId} result={result} time={_pve.StageTimer:F1}s coverHits={_coverHits} playerHits={_playerHits}");
            Finish("1-1 complete");
        }

        private void Finish(string why)
        {
            _done = true;
            Debug.Log($"[ScaleSmoke] DONE ({why})");
            StartCoroutine(QuitSoon());
        }

        private System.Collections.IEnumerator QuitSoon()
        {
            yield return new WaitForSecondsRealtime(1.0f);
            Application.Quit();
        }
    }
}

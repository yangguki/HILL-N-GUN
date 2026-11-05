using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.PvE;

namespace HillNGun.DebugTools
{
    /// <summary>
    /// -smoketest-rex-hp (with -smoketest-fullpve -smoketest-start-stage 3): observe Rex run —
    /// screenshot WANTED card, log scaled HP, log clear time, then quit once boss stage ends.
    /// Output: persistentDataPath/rex_hp_smoke/
    /// </summary>
    public class RexHpSmokeRunner : MonoBehaviour
    {
        private static bool _wanted;
        private string _dir;
        private PvEMatchManager _pve;
        private bool _wantedShot;
        private bool _hpLogged;
        private bool _done;
        private bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            foreach (var a in Environment.GetCommandLineArgs())
                if (a.Equals("-smoketest-rex-hp", StringComparison.OrdinalIgnoreCase)) _wanted = true;
            if (!_wanted) return;
            var go = new GameObject("[RexHpSmokeRunner]");
            DontDestroyOnLoad(go);
            go.AddComponent<RexHpSmokeRunner>();
        }

        private void Awake()
        {
            _dir = Path.Combine(Application.persistentDataPath, "rex_hp_smoke");
            Directory.CreateDirectory(_dir);
            Debug.Log($"[RexHpSmoke] observing dir={_dir}");
        }

        private void Update()
        {
            if (_done) return;
            if (_pve == null)
            {
                _pve = FindFirstObjectByType<PvEMatchManager>();
                if (_pve == null) return;
                if (!_hooked)
                {
                    _hooked = true;
                    _pve.OnStageComplete += HandleComplete;
                    Debug.Log($"[RexHpSmoke] hooked stage={_pve.CurrentStageId} index={_pve.CurrentStageIndex}");
                }
            }

            var pres = StagePresentation.Instance;
            if (!_wantedShot && pres != null && pres.IsWantedShowing)
            {
                _wantedShot = true;
                StartCoroutine(CaptureWanted());
            }

            if (!_hpLogged && _pve.ActiveBoss != null)
            {
                _hpLogged = true;
                var b = _pve.ActiveBoss;
                Debug.Log($"[RexHpSmoke] BOSS_HP id={b.Data?.id} max={b.MaxHp} current={b.CurrentHp} " +
                          $"humans={_pve.HumanPlayerCount} ai={_pve.AiAllyCount} aiAllyHpAdd={b.Data?.aiAllyHpAdd}");
                string combat = Path.Combine(_dir, "rex_combat.png");
                ScreenCapture.CaptureScreenshot(combat);
                Debug.Log($"[RexHpSmoke] COMBAT SHOT {combat}");
            }

            if (SceneManager.GetActiveScene().name.Contains("Result") && !_done)
            {
                Debug.Log($"[RexHpSmoke] CLEAR via Result time={_pve?.StageTimer:F1}s");
                Finish("result");
            }
        }

        private IEnumerator CaptureWanted()
        {
            float prev = Time.timeScale;
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(0.4f);
            string path = Path.Combine(_dir, "wanted_rex.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[RexHpSmoke] WANTED SHOT {path}");
            yield return new WaitForSecondsRealtime(0.6f);
            Time.timeScale = prev > 0.01f ? prev : 1f;
        }

        private void HandleComplete(PvERoundResult result)
        {
            if (_done || _pve == null) return;
            bool bossStage = _pve.CurrentStage != null && _pve.CurrentStage.isBossStage;
            if (!bossStage)
            {
                Debug.Log($"[RexHpSmoke] intermediate stage complete index={_pve.CurrentStageIndex} id={_pve.CurrentStageId} — keep going");
                return;
            }
            Debug.Log($"[RexHpSmoke] CLEAR result={result} time={_pve.StageTimer:F1}s maxHpLogged={_hpLogged}");
            Finish("boss stage complete");
        }

        private void Finish(string why)
        {
            if (_done) return;
            _done = true;
            Debug.Log($"[RexHpSmoke] DONE ({why})");
            StartCoroutine(QuitSoon());
        }

        private IEnumerator QuitSoon()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}

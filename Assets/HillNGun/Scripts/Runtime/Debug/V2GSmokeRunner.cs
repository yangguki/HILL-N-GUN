using System;
using System.Collections;
using System.IO;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Combat;
using HillNGun.Feel;
using HillNGun.DebugTools;
using HillNGun.UI;

namespace HillNGun.DebugTools
{
    /// <summary>
    /// -smoketest-v2g: short capture of v2 Task G shots (ranges, muzzles, barrel) then quit.
    /// Saves under persistentDataPath/smoketest_v2g/.
    /// </summary>
    public class V2GSmokeRunner : MonoBehaviour
    {
        private static bool _wanted;
        private string _dir;
        private int _shot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            foreach (var a in Environment.GetCommandLineArgs())
                if (a.Equals("-smoketest-v2g", StringComparison.OrdinalIgnoreCase)) _wanted = true;
            if (!_wanted) return;
            var go = new GameObject("V2GSmokeRunner");
            DontDestroyOnLoad(go);
            go.AddComponent<V2GSmokeRunner>();
        }

        private void Start()
        {
            if (!_wanted) { Destroy(gameObject); return; }
            _dir = Path.Combine(Application.persistentDataPath, "smoketest_v2g");
            Directory.CreateDirectory(_dir);
            foreach (var f in Directory.GetFiles(_dir, "*.png")) try { File.Delete(f); } catch { }
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            GameFlowManager.Instance?.GoToModeSelect();
            yield return new WaitForSecondsRealtime(1f);

            LobbyUI lobby = null;
            for (int i = 0; i < 30 && lobby == null; i++)
            {
                lobby = FindFirstObjectByType<LobbyUI>();
                yield return new WaitForSecondsRealtime(0.25f);
            }
            if (lobby != null) lobby.StartPvE(0);
            else GameFlowManager.Instance?.GoToCharacterSelect(GameMode.PvE);
            yield return new WaitForSecondsRealtime(1.5f);

            var flow = GameFlowManager.Instance;
            if (flow != null)
            {
                flow.SetAllCharacters("buster", "raven", "volt");
                flow.StartMatch();
            }
            yield return new WaitForSecondsRealtime(2.5f);

            DebugOverlay overlay = null;
            for (int i = 0; i < 40 && overlay == null; i++)
            {
                overlay = ServiceLocator.Get<DebugOverlay>();
                yield return new WaitForSecondsRealtime(0.2f);
            }
            overlay?.ForceShowRanges(true);
            Time.timeScale = 0.15f;
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("01_debug_ranges");

            var fx = EffectsManager.Instance;
            var am = RuntimeAssetManager.Instance;
            if (fx != null && am?.CharacterDatas != null)
            {
                foreach (var c in am.CharacterDatas)
                {
                    var w = c != null ? c.weapon : null;
                    if (w?.vfx == null) continue;
                    string tag = string.IsNullOrEmpty(w.vfx.patternId) ? w.id : w.vfx.patternId;
                    bool shotgun = (w.projectilesPerShot > 1) || w.vfx.isPellet;
                    fx.SpawnMuzzleFlashPattern(new Vector2(12f, 7f), Vector2.up, shotgun, w.muzzleForwardOffset, w.vfx);
                    yield return new WaitForSecondsRealtime(0.35f);
                    yield return Shot($"02_muzzle_{tag}");
                }
            }

            var combat = ServiceLocator.Get<CombatSystem>();
            if (combat != null)
            {
                var b = new HazardExplosiveBarrel();
                b.Initialize(new Vector2(12f, 9f), combat);
                b.TriggerByGrenade(); // arms (0.4 s warning, 12 §6.1)
                b.Tick(TerrainRules.Data.barrelWarningSec + 0.01f); // finish the warning even if timeScale is 0 here
                yield return new WaitForSecondsRealtime(0.45f);
                yield return Shot("03_barrel_explode");
            }

            Time.timeScale = 1f;
            UnityEngine.Debug.Log($"[V2G] done shots={_shot} dir={_dir}");
            yield return new WaitForSecondsRealtime(0.4f);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private IEnumerator Shot(string name)
        {
            string path = Path.Combine(_dir, $"v2g_{_shot:D2}_{name}.png");
            ScreenCapture.CaptureScreenshot(path);
            _shot++;
            UnityEngine.Debug.Log($"[V2G] shot {path}");
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
}

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.PvE;
using HillNGun.UI;
using HillNGun.DebugTools;

namespace HillNGun.DebugTools
{
    /// <summary>-smoketest-visual: snap each dust stage with temp BG + covers, optional WANTED, then quit.</summary>
    public class VisualSmokeRunner : MonoBehaviour
    {
        private static bool _wanted;
        private string _dir;
        private int _shot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            foreach (var a in Environment.GetCommandLineArgs())
                if (a.Equals("-smoketest-visual", StringComparison.OrdinalIgnoreCase)) _wanted = true;
            if (!_wanted) return;
            var go = new GameObject("VisualSmokeRunner");
            DontDestroyOnLoad(go);
            go.AddComponent<VisualSmokeRunner>();
        }

        private IEnumerator Start()
        {
            if (!_wanted) { Destroy(gameObject); yield break; }
            _dir = Path.Combine(Application.persistentDataPath, "smoketest_visual");
            Directory.CreateDirectory(_dir);
            foreach (var f in Directory.GetFiles(_dir, "*.png")) try { File.Delete(f); } catch { }

            yield return new WaitForSecondsRealtime(1.2f);
            GameFlowManager.Instance?.GoToModeSelect();
            yield return new WaitForSecondsRealtime(0.8f);

            for (int stage = 0; stage < 4; stage++)
            {
                var lobby = FindFirstObjectByType<LobbyUI>();
                if (lobby != null) lobby.StartPvE(stage);
                else GameFlowManager.Instance?.GoToCharacterSelect(GameMode.PvE);
                yield return new WaitForSecondsRealtime(1.0f);
                var flow = GameFlowManager.Instance;
                flow?.SetAllCharacters("buster", "raven", "volt");
                // startStageIndex for this run
                if (flow?.CurrentSession != null) flow.CurrentSession.startStageIndex = stage;
                flow?.StartMatch();
                yield return new WaitForSecondsRealtime(2.8f); // allow intro pan / wanted

                // Force-skip presentation
                var pres = StagePresentation.Instance;
                // wait for match actors
                for (int i = 0; i < 40; i++)
                {
                    var pve = FindFirstObjectByType<PvEMatchManager>();
                    if (pve != null && pve.CurrentStage != null) break;
                    yield return new WaitForSecondsRealtime(0.1f);
                }
                yield return Shot($"stage_{stage}_bg_covers");
                if (stage == 3) yield return Shot("wanted_or_boss");

                // return to lobby for next
                flow?.GoToModeSelect();
                yield return new WaitForSecondsRealtime(0.8f);
            }

            UnityEngine.Debug.Log($"[VisualSmoke] done shots={_shot} dir={_dir}");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private IEnumerator Shot(string name)
        {
            string path = Path.Combine(_dir, $"vis_{_shot:D2}_{name}.png");
            ScreenCapture.CaptureScreenshot(path);
            _shot++;
            UnityEngine.Debug.Log($"[VisualSmoke] {path}");
            yield return new WaitForSecondsRealtime(0.45f);
        }
    }
}

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using HillNGun.Core;
using HillNGun.PvE;
using HillNGun.UI;
using HillNGun.Combat;
using HillNGun.Cover;

namespace HillNGun.DebugTools
{
    /// <summary>-smoketest-visual: snap each dust stage with temp BG + covers, WANTED on boss, cover damage, then quit.</summary>
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
            yield return new WaitForSecondsRealtime(0.9f);

            for (int stage = 0; stage < 4; stage++)
            {
                var lobby = FindFirstObjectByType<LobbyUI>();
                if (lobby != null) lobby.StartPvE(stage);
                else
                {
                    var flow0 = GameFlowManager.Instance;
                    if (flow0?.CurrentSession != null) flow0.CurrentSession.startStageIndex = stage;
                    flow0?.GoToCharacterSelect(GameMode.PvE);
                }
                yield return new WaitForSecondsRealtime(1.0f);
                var flow = GameFlowManager.Instance;
                flow?.SetAllCharacters("buster", "raven", "volt");
                flow?.StartMatch();

                // Skip intro quickly by advancing realtime; presentation listens for taps — inject skip via timescale
                Time.timeScale = 8f;
                yield return new WaitForSecondsRealtime(0.6f);
                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(0.8f);

                yield return Shot($"stage_{stage}_bg_covers");

                if (stage == 0)
                {
                    // Damage first cover through stages for screenshot
                    var combat = ServiceLocator.Get<CombatSystem>();
                    if (combat?.Covers != null && combat.Covers.Count > 0)
                    {
                        var c = combat.Covers[0];
                        c.TakeDamage(c.Data.maxHp / 2);
                        yield return new WaitForSecondsRealtime(0.2f);
                        yield return Shot("cover_damage_mid");
                        c.TakeDamage(c.Data.maxHp);
                        yield return new WaitForSecondsRealtime(0.2f);
                        yield return Shot("cover_damage_destroyed");
                    }
                }
                if (stage == 3)
                    yield return Shot("boss_rex_or_wanted");

                flow?.GoToModeSelect();
                yield return new WaitForSecondsRealtime(0.7f);
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
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
}

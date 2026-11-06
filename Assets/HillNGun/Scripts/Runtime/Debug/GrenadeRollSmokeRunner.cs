using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.Combat;
using HillNGun.Data;
using HillNGun.PvE;
using HillNGun.Characters;
using HillNGun.Input;

namespace HillNGun.DebugTools
{
    /// <summary>
    /// -smoketest-grenade-roll (with -smoketest-fullpve): force one grenade throw for mid-flight + explode
    /// screenshots, then prove consecutive rolls with i-frame logs. Output: persistentDataPath/grenade_roll_smoke/
    /// </summary>
    public class GrenadeRollSmokeRunner : MonoBehaviour
    {
        private static bool _wanted;
        private string _dir;
        private PvEMatchManager _pve;
        private bool _done;
        private bool _started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            foreach (var a in Environment.GetCommandLineArgs())
                if (a.Equals("-smoketest-grenade-roll", StringComparison.OrdinalIgnoreCase)) _wanted = true;
            if (!_wanted) return;
            var go = new GameObject("[GrenadeRollSmokeRunner]");
            DontDestroyOnLoad(go);
            go.AddComponent<GrenadeRollSmokeRunner>();
        }

        private void Awake()
        {
            _dir = Path.Combine(Application.persistentDataPath, "grenade_roll_smoke");
            Directory.CreateDirectory(_dir);
            Debug.Log($"[GrenadeRollSmoke] dir={_dir}");
        }

        private void Update()
        {
            if (_done) return;
            if (_pve == null)
            {
                _pve = FindFirstObjectByType<PvEMatchManager>();
                if (_pve == null) return;
            }
            if (!_started && _pve.IsMatchActive && _pve.StageTimer >= 2.5f)
            {
                _started = true;
                StartCoroutine(RunSequence());
            }
            if (SceneManager.GetActiveScene().name.Contains("Result") && !_done)
                Finish("result");
        }

        private IEnumerator RunSequence()
        {
            GrenadeView.Ensure();
            var combat = ServiceLocator.Get<CombatSystem>();
            var me = Player();
            if (combat == null || me == null || me.Data?.grenade == null)
            {
                Debug.LogError("[GrenadeRollSmoke] missing combat/player/grenade");
                Finish("missing");
                yield break;
            }

            // ---- Grenade flight + explode ----
            Vector2 start = me.Position;
            Vector2 target = start + new Vector2(0f, 5.5f);
            var gdata = me.Data.grenade;
            // Force 1x during the throw so mid-flight / explode captures are not raced by -smoketest-timescale.
            float prev = Time.timeScale;
            Time.timeScale = 1f;
            combat.SpawnGrenade(start, target, gdata, me.Team);
            float flight = gdata.GetFlightTime(Vector2.Distance(start, target));
            Debug.Log($"[GrenadeRollSmoke] THROW flight={flight:F2}s start={start} target={target} scale={Time.timeScale}");

            yield return new WaitForSeconds(Mathf.Clamp(flight * 0.45f, 0.12f, flight * 0.7f));
            Time.timeScale = 0f;
            yield return null; // one paused frame so LateUpdate has drawn the bomb
            yield return null;
            string flightPath = Path.Combine(_dir, "grenade_flight.png");
            ScreenCapture.CaptureScreenshot(flightPath);
            int bombs = FindFirstObjectByType<GrenadeView>() != null ? FindFirstObjectByType<GrenadeView>().ActiveBombs : -1;
            int activeLogic = combat.ActiveGrenades != null ? combat.ActiveGrenades.Count : -1;
            Debug.Log($"[GrenadeRollSmoke] FLIGHT SHOT {flightPath} bombs={bombs} logicActive={activeLogic}");
            yield return new WaitForSecondsRealtime(0.3f);

            Time.timeScale = 1f;
            // wait for impact + a couple frames of cartoon burst
            float remain = Mathf.Max(0.05f, flight * 0.55f);
            yield return new WaitForSeconds(remain);
            yield return new WaitForSeconds(0.08f); // burst pop
            Time.timeScale = 0f;
            yield return null;
            string explodePath = Path.Combine(_dir, "grenade_explode.png");
            ScreenCapture.CaptureScreenshot(explodePath);
            Debug.Log($"[GrenadeRollSmoke] EXPLODE SHOT {explodePath}");
            yield return new WaitForSecondsRealtime(0.3f);
            Time.timeScale = prev > 0.01f ? prev : 1f;

            // ---- Consecutive rolls + i-frame proof ----
            me = Player();
            if (me == null) { Finish("no player for roll"); yield break; }
            var logPath = Path.Combine(_dir, "roll_chain.txt");
            using (var w = new StreamWriter(logPath, false))
            {
                w.WriteLine($"cooldown={me.Data.roll?.cooldown} duration={me.Data.roll?.duration} invuln={me.Data.roll?.invulnDuration}");
                me.OnRollCommand(new RollCommand(Vector2.right, Time.time));
                w.WriteLine($"t0 state={me.MovementState} inv={me.IsInvulnerable}");
                // buffer next roll near end
                float dur = me.Data.roll != null ? me.Data.roll.duration : 0.4f;
                float waited = 0f;
                while (waited < dur - 0.08f)
                {
                    waited += 0.02f;
                    yield return new WaitForSecondsRealtime(0.02f);
                }
                me.OnRollCommand(new RollCommand(Vector2.left, Time.time));
                w.WriteLine($"buffered near end state={me.MovementState} inv={me.IsInvulnerable}");
                // finish first, second should start
                yield return new WaitForSecondsRealtime(0.15f);
                w.WriteLine($"after boundary state={me.MovementState} inv={me.IsInvulnerable}");
                // let second finish, idle gap
                yield return new WaitForSecondsRealtime(dur + 0.05f);
                w.WriteLine($"gap state={me.MovementState} inv={me.IsInvulnerable}");
                bool gapOk = !me.IsInvulnerable && me.MovementState != MovementState.Rolling;
                // immediate third roll
                me.OnRollCommand(new RollCommand(Vector2.right, Time.time));
                w.WriteLine($"immediate chain state={me.MovementState} inv={me.IsInvulnerable}");
                bool chainOk = me.MovementState == MovementState.Rolling && me.IsInvulnerable;
                w.WriteLine($"PASS gapVulnerable={gapOk} chainStarts={chainOk}");
                Debug.Log($"[GrenadeRollSmoke] ROLL gapVulnerable={gapOk} chainStarts={chainOk}");
            }
            string rollShot = Path.Combine(_dir, "roll_chain.png");
            ScreenCapture.CaptureScreenshot(rollShot);
            Debug.Log($"[GrenadeRollSmoke] ROLL SHOT {rollShot}");

            yield return new WaitForSecondsRealtime(0.4f);
            Finish("sequence complete");
        }

        private CharacterStateMachine Player()
        {
            if (_pve == null || _pve.PlayerTeam == null || _pve.PlayerTeam.Count == 0) return null;
            int i = Mathf.Clamp(_pve.PlayerControlledIndex, 0, _pve.PlayerTeam.Count - 1);
            return _pve.PlayerTeam[i];
        }

        private void Finish(string why)
        {
            if (_done) return;
            _done = true;
            Debug.Log($"[GrenadeRollSmoke] DONE ({why})");
            StartCoroutine(QuitSoon());
        }

        private IEnumerator QuitSoon()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}

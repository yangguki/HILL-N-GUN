using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.PvE;
using HillNGun.PvP;
using HillNGun.Characters;
using HillNGun.Feel;

namespace HillNGun.DebugTools
{
    /// <summary>
    /// -smoketest-freemove (with -smoketest-fullpve / -smoketest-fullpvp): v11 FreeMove evidence.
    /// Event-driven screenshots into persistentDataPath/scale_smoke ("fm_{event}_{stage}.png"): free 2D movement,
    /// door "!" telegraph + enemy stepping out, street-end / fence entries, balcony sniper wind-up, Rex walk-in, PvP round 1.
    /// Per-stage FMSTAT lines: spawn director stats, fire-cap peak, dust peak/drops, collision overlap frames, stuck allies.
    /// -freemove-record-at S [-freemove-record-secs N] [-freemove-record-stage I]: JPG frames at a fixed 30 fps game clock
    /// (Time.captureFramerate) into scale_smoke/frames for an mp4 (desktop window recording, not a device).
    /// </summary>
    public class FreeMoveSmokeRunner : MonoBehaviour
    {
        private static bool _wanted;
        private static float _recAt = -1f, _recSecs = 8f;
        private static int _recStage = 0;
        private string _dir, _framesDir;
        private string _scene;
        private float _sceneT;
        private string _stageKey;
        private readonly HashSet<string> _shot = new HashSet<string>();
        private readonly Queue<string> _pending = new Queue<string>();
        private float _lastShotReal = -10f;
        private int _overlapFrames, _samples, _stuckEvents;
        private readonly Dictionary<CharacterStateMachine, (Vector2 pos, float t)> _still = new Dictionary<CharacterStateMachine, (Vector2, float)>();
        private readonly Dictionary<CharacterStateMachine, StillWin> _win = new Dictionary<CharacterStateMachine, StillWin>();
        private readonly Dictionary<string, int> _stillClass = new Dictionary<string, int>();

        private sealed class StillWin
        {
            public readonly int[] hist = new int[16]; public int frames, shots0, inRange; public float progSum, distSum, blockedMax; public int distN;
            public void Reset(int shots) { System.Array.Clear(hist, 0, hist.Length); frames = 0; inRange = 0; progSum = 0f; distSum = 0f; distN = 0; blockedMax = 0f; shots0 = shots; }
        }

        /// <summary>Classify a 6 s still window from the per-frame ally intents (evidence, not a guess).</summary>
        private static string Classify(StillWin w, int shots, out string detail)
        {
            float F(PvEMatchManager.AllyIntent i) => w.frames > 0 ? w.hist[(int)i] / (float)w.frames : 0f;
            float hold = F(PvEMatchManager.AllyIntent.HoldInRange) + F(PvEMatchManager.AllyIntent.BackOff);
            float move = F(PvEMatchManager.AllyIntent.Approach) + F(PvEMatchManager.AllyIntent.Sidestep) + F(PvEMatchManager.AllyIntent.Dodge);
            float idle = F(PvEMatchManager.AllyIntent.NoTarget), pilot = F(PvEMatchManager.AllyIntent.Pilot) + F(PvEMatchManager.AllyIntent.Rolling);
            float prog = w.frames > 0 ? w.progSum / w.frames : 1f;
            int fired = shots - w.shots0;
            detail = $"hold={hold:P0} move={move:P0} noTarget={idle:P0} pilot={pilot:P0} inRange={(w.frames > 0 ? w.inRange / (float)w.frames : 0f):P0} avgProgress={prog:F2} shots={fired} avgTargetDist={(w.distN > 0 ? w.distSum / w.distN : -1f):F1} blockedMax={w.blockedMax:F1}s";
            if (move >= 0.4f && (prog < 0.35f || w.blockedMax >= 1f)) return "BLOCKED_MOVE";
            if (hold >= 0.6f) return fired > 0 ? "HOLD_FIRING" : "HOLD_NO_SHOTS";
            if (idle >= 0.6f) return "IDLE_NO_TARGET";
            if (pilot >= 0.5f) return "ABILITY";
            return "MIXED";
        }
        private bool _recording, _recDone;
        private int _recFrames;
        private float _recStartT;
        private bool _pvpShot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-smoketest-freemove", StringComparison.OrdinalIgnoreCase)) _wanted = true;
                if (i + 1 >= args.Length) continue;
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var fl = System.Globalization.NumberStyles.Float;
                if (args[i].Equals("-freemove-record-at", StringComparison.OrdinalIgnoreCase) && float.TryParse(args[i + 1], fl, inv, out float a)) _recAt = a;
                if (args[i].Equals("-freemove-record-secs", StringComparison.OrdinalIgnoreCase) && float.TryParse(args[i + 1], fl, inv, out float s)) _recSecs = Mathf.Clamp(s, 1f, 20f);
                if (args[i].Equals("-freemove-record-stage", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[i + 1], out int st)) _recStage = st;
            }
            if (!_wanted) return;
            var go = new GameObject("[FreeMoveSmokeRunner]");
            DontDestroyOnLoad(go);
            go.AddComponent<FreeMoveSmokeRunner>();
        }

        private void Awake()
        {
            _dir = Path.Combine(Application.persistentDataPath, "scale_smoke");
            Directory.CreateDirectory(_dir);
            _framesDir = Path.Combine(_dir, "frames");
            if (_recAt >= 0f)
            {
                Directory.CreateDirectory(_framesDir);
                foreach (var f in Directory.GetFiles(_framesDir, "*.jpg")) File.Delete(f);
            }
            HillNGun.Settings.SettingsService.Instance.Data.tutorialDone = true;
            Debug.Log($"[FMSmoke] start screen={Screen.width}x{Screen.height} record={(_recAt >= 0f ? $"stage{_recStage}@{_recAt:F1}s x{_recSecs:F0}s" : "off")} (desktop window, not a device)");
        }

        private void Request(string key)
        {
            if (_shot.Contains(key)) return;
            _shot.Add(key);
            _pending.Enqueue(key);
        }

        private void Update()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene != _scene) { _scene = scene; _sceneT = 0f; }
            _sceneT += Time.deltaTime;
            FlushShots();
            if (!scene.EndsWith("_Match")) return;
            var pve = FindFirstObjectByType<PvEMatchManager>();
            if (pve != null) { TickPvE(pve); return; }
            var pvp = FindFirstObjectByType<PvPMatchManager>();
            if (pvp != null && pvp.IsMatchActive && pvp.CurrentRound == 1 && !_pvpShot && _sceneT > 7f)
            {
                _pvpShot = true;
                int free = 0;
                foreach (var c in FindObjectsByType<CharacterStateMachine>(FindObjectsSortMode.None)) if (c.IsFreeMove) free++;
                Debug.Log($"[FMSmoke] PVP mode check: freeMoveActors={free} (expect 0, RowLane)");
                Request("pvp_rowlane_r1");
            }
        }

        private void TickPvE(PvEMatchManager pve)
        {
            if (!pve.IsMatchActive) return;
            string key = pve.CurrentStageId ?? "pve";
            if (key != _stageKey)
            {
                _stageKey = key; _overlapFrames = 0; _samples = 0; _stuckEvents = 0; _still.Clear(); _win.Clear(); _stillClass.Clear();
                Debug.Log($"[FMSmoke] stage {key} mode={pve.MoveMode} layout={(pve.StreetLayout != null ? pve.StreetLayout.id : "none")}");
            }
            float t = pve.StageTimer;
            MaybeRecord(pve, t);
            if (!pve.IsFreeMove) return;

            // free 2D movement: the controlled hero moving with a clear vertical component
            var team = pve.PlayerTeam;
            for (int i = 0; i < team.Count; i++)
            {
                var c = team[i];
                if (c == null || c.OverallState != CharacterOverallState.Active) continue;
                if (pve.World != null)
                {
                    _samples++;
                    if (!pve.World.IsFree(c.Position, c.BodyRadius * 0.9f)) _overlapFrames++;
                }
                if (i == pve.PlayerControlledIndex && t > 3f && Mathf.Abs(c.FreeVelocity.y) > 1.5f && Mathf.Abs(c.FreeVelocity.x) > 1f) Request("free2d_" + key);
                if (i != pve.PlayerControlledIndex)
                {
                    bool has = pve.TryGetAllyIntent(c, out var info);
                    if (!_win.TryGetValue(c, out var w)) { w = new StillWin(); w.Reset(info.shots); _win[c] = w; }
                    if (!_still.TryGetValue(c, out var s) || (s.pos - c.Position).sqrMagnitude > 0.04f) { _still[c] = (c.Position, t); w.Reset(info.shots); }
                    else
                    {
                        if (has)
                        {
                            w.frames++; w.hist[(int)info.intent]++; if (info.inRange) w.inRange++;
                            w.progSum += info.cmdMag > 0f ? info.progress : 1f;
                            if (info.targetDist >= 0f) { w.distSum += info.targetDist; w.distN++; }
                            w.blockedMax = Mathf.Max(w.blockedMax, info.blockedTime);
                        }
                        if (t - s.t > 6f)
                        {
                            _stuckEvents++;
                            string cls = Classify(w, info.shots, out string det);
                            _stillClass[cls] = (_stillClass.TryGetValue(cls, out int k) ? k : 0) + 1;
                            Debug.Log($"[FMSmoke] ALLYSTILL {c.Data?.id} 6s at ({c.Position.x:F1},{c.Position.y:F1}) t={t:F1} class={cls} {det}");
                            _still[c] = (c.Position, t); w.Reset(info.shots);
                        }
                    }
                }
            }

            foreach (var e in pve.ActiveEnemies)
            {
                if (e == null || e.SpawnPoint == null) continue;
                var sp = e.SpawnPoint;
                switch (sp.type)
                {
                    case SpawnPointType.StorefrontDoor:
                        if (e.Entrance == EntrancePhase.Telegraph && e.TelegraphProgress >= 0.55f) Request("door_telegraph_" + key);
                        if (e.Entrance == EntrancePhase.Walking && e.EntranceProgress >= 0.45f) Request("door_exit_" + key);
                        break;
                    case SpawnPointType.StreetEnd:
                        if (e.Entrance == EntrancePhase.Walking && e.EntranceProgress >= 0.6f) Request("street_entry_" + key);
                        break;
                    case SpawnPointType.FenceClimb:
                        if (e.Entrance == EntrancePhase.Walking && e.EntranceProgress >= 0.4f) Request("fence_entry_" + key);
                        break;
                    case SpawnPointType.AlleyGap:
                        if (e.Entrance == EntrancePhase.Walking && e.EntranceProgress >= 0.5f) Request("alley_entry_" + key);
                        break;
                    case SpawnPointType.BalconySniper:
                        if (e.IsWindingUp) Request("sniper_aim_" + key);
                        break;
                }
            }
            var boss = pve.ActiveBoss;
            if (boss != null && boss.IsInEntryPhase() && _sceneT > 0f)
            {
                float d0 = (boss.EntryStart - boss.Position).magnitude;
                if (d0 > 2.5f) Request("rex_walkin_" + key);
            }
        }

        private void MaybeRecord(PvEMatchManager pve, float t)
        {
            if (_recAt < 0f || _recDone) return;
            if (!_recording && pve.CurrentStageIndex == _recStage && t >= _recAt)
            {
                _recording = true; _recStartT = t; _recFrames = 0;
                Time.captureFramerate = 30;
                StartCoroutine(RecordLoop(pve));
                Debug.Log($"[FMSmoke] REC start stage={pve.CurrentStageId} t={t:F1}s timeScale={Time.timeScale:F2}");
            }
        }

        private IEnumerator RecordLoop(PvEMatchManager pve)
        {
            int total = Mathf.RoundToInt(_recSecs * 30f);
            while (_recFrames < total && pve != null)
            {
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(_framesDir, $"fm_{_recFrames:D4}.jpg"), tex.EncodeToJPG(85));
                Destroy(tex);
                _recFrames++;
            }
            Time.captureFramerate = 0;
            _recording = false; _recDone = true;
            Debug.Log($"[FMSmoke] REC done frames={_recFrames} gameSpan={(pve != null ? pve.StageTimer - _recStartT : 0f):F2}s -> {_framesDir}");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(); // recording runs end here
        }

        private void FlushShots()
        {
            if (_pending.Count == 0 || Time.realtimeSinceStartup - _lastShotReal < 0.15f) return;
            string key = _pending.Dequeue();
            _lastShotReal = Time.realtimeSinceStartup;
            string path = Path.Combine(_dir, $"fm_{key}_{Screen.width}x{Screen.height}.png");
            ScreenCapture.CaptureScreenshot(path);
            SmokeTestRunner.MarkShotFrame();
            Debug.Log($"[FMSmoke] SHOT {path} sceneT={_sceneT:F1}");
        }

        private string StillClasses() { var sb = new System.Text.StringBuilder(); foreach (var kv in _stillClass) sb.Append(kv.Key).Append(':').Append(kv.Value).Append(' '); return sb.ToString().Trim(); }

        /// <summary>Called by the stage-complete log hook: per-stage FreeMove stats.</summary>
        private void OnEnable() { Application.logMessageReceived += OnLog; }
        private void OnDisable() { Application.logMessageReceived -= OnLog; }

        private void OnLog(string msg, string st, LogType type)
        {
            if (!msg.StartsWith("[PvE] Stage ") || !msg.Contains(" complete:")) return;
            var pve = FindFirstObjectByType<PvEMatchManager>();
            if (pve == null) return;
            var d = pve.Director; var fire = pve.FireTokens; var fx = MotionFx.Instance;
            string types = "";
            if (d != null) foreach (var kv in d.SpawnsByType) types += $"{kv.Key}:{kv.Value} ";
            Debug.Log($"[FMSmoke] FMSTAT {pve.CurrentStageId} mode={pve.MoveMode} spawned={d?.Spawned} deferrals={d?.Deferrals} relaxed={d?.Relaxed} skipped={d?.Skipped} byType=[{types.Trim()}] " +
                      $"fireCap={fire?.Capacity} peakAttackers={pve.PeakConcurrentAttackers} framesOverCap={pve.FramesOverFireCap} grants={fire?.Grants} denials={fire?.Denials} " +
                      $"dustPeak={fx?.DustPeak} dustDropped={fx?.DustDropped} overlapFrames={_overlapFrames}/{_samples} allyStill6s={_stuckEvents} stillClasses=[{StillClasses()}] allyBlockedEpisodes(>=1s)={pve.AllyBlockedEpisodes}");
        }
    }
}

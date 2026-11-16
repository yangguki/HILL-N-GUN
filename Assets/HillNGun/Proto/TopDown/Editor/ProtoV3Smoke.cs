using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Characters;
using HillNGun.Core;
using HillNGun.Input;
using HillNGun.Proto.Frontline;
using HillNGun.PvE;

namespace HillNGun.Proto.TopDown.Editor
{
    /// <summary>
    /// Bot-driven editor smoke: menu shot, then 난전 + Frontline 1v1×3 + 3v3×3.
    /// Launch with argv <c>-proto-v3-smoke</c>. State lives under Builds/proto_v3/ so it survives playmode reload.
    /// </summary>
    public static class ProtoV3Smoke
    {
        public const string Arg = "-proto-v3-smoke";
        public const string StateMenu = "menu";
        public const string StateMove8 = "move8";
        public const string StateTopDown = "topdown";
        public const string StateFront1 = "frontline_1v1";
        public const string StateFront3 = "frontline_3v3";
        public const string StateDone = "done";

        public static string OutputDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/proto_v3"));
        public static string StatePath => Path.Combine(OutputDir, "smoke_state.txt");
        public static string StageDonePath => Path.Combine(OutputDir, "stage_done.txt");
        public static string ResultsPath => Path.Combine(OutputDir, "results.txt");
        public static string FailPath => Path.Combine(OutputDir, "smoke_fail.txt");
        public static string RoundPath => Path.Combine(OutputDir, "smoke_round.txt");

        static string _launchedFor;

        [InitializeOnLoadMethod]
        static void Boot()
        {
            if (!Has(Arg)) return;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
            // Domain reload can enter play before playModeStateChanged subscribers are attached.
            if (EditorApplication.isPlaying) EnsurePlayWorker();
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (!Has(Arg)) return;
            if (change != PlayModeStateChange.EnteredPlayMode) return;
            EnsurePlayWorker();
        }

        static void EnsurePlayWorker()
        {
            if (UnityEngine.Object.FindFirstObjectByType<ProtoV3SmokePlay>() != null) return;
            var go = new GameObject("[ProtoV3Smoke]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ProtoV3SmokePlay>();
        }

        static void Tick()
        {
            if (!Has(Arg)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            Directory.CreateDirectory(OutputDir);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EnsurePlayWorker();
                    if (File.Exists(StageDonePath))
                        EditorApplication.isPlaying = false;
                }
                return;
            }

            if (File.Exists(FailPath))
            {
                Debug.LogError("[ProtoV3Smoke] failed: " + File.ReadAllText(FailPath));
                EditorApplication.Exit(1);
                return;
            }

            string state = ReadState();
            if (string.IsNullOrEmpty(state))
            {
                ClearOutputs();
                // Ensure flipbook SOs + sprite pivots exist before any playmode stage.
                try { ProtoFlipbookBuilder.Refresh(); }
                catch (Exception e) { Debug.LogWarning("[ProtoV3Smoke] flipbook refresh: " + e.Message); }
                WriteState(StateMenu);
                state = StateMenu;
            }

            if (state == StateDone)
            {
                bool ok = AllOutputsPresent();
                if (!ok) Debug.LogError("[ProtoV3Smoke] done but outputs missing under " + OutputDir);
                else Debug.Log("[ProtoV3Smoke] all outputs ready under " + OutputDir);
                EditorApplication.Exit(ok ? 0 : 1);
                return;
            }

            if (File.Exists(StageDonePath))
            {
                string finished = File.ReadAllText(StageDonePath).Trim();
                File.Delete(StageDonePath);
                WriteState(NextState(finished));
                WriteRound(1);
                _launchedFor = null;
                return;
            }

            try
            {
                Launch(state);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                File.WriteAllText(FailPath, e.Message);
                EditorApplication.Exit(1);
            }
        }

        static void Launch(string state)
        {
            if (_launchedFor == state) return;
            _launchedFor = state;
            switch (state)
            {
                case StateMenu:
                    EditorSceneManager.OpenScene(ProtoAssets.MenuScene);
                    break;
                case StateMove8:
                case StateTopDown:
                    EditorSceneManager.OpenScene(ProtoAssets.Scene);
                    break;
                case StateFront1:
                    Directory.CreateDirectory(FrontlineMatch.OutputDir);
                    File.WriteAllText(Path.Combine(FrontlineMatch.OutputDir, "mode.txt"), "1");
                    ProtoNav.SetFrontlineLaunch(false);
                    EditorSceneManager.OpenScene(ProtoAssets.FrontlineScene);
                    break;
                case StateFront3:
                    Directory.CreateDirectory(FrontlineMatch.OutputDir);
                    File.WriteAllText(Path.Combine(FrontlineMatch.OutputDir, "mode.txt"), "3");
                    ProtoNav.SetFrontlineLaunch(true);
                    EditorSceneManager.OpenScene(ProtoAssets.FrontlineScene);
                    break;
                default:
                    File.WriteAllText(FailPath, "unknown state " + state);
                    EditorApplication.Exit(1);
                    return;
            }
            Debug.Log("[ProtoV3Smoke] launch " + state);
            EditorApplication.EnterPlaymode();
        }

        static string NextState(string finished)
        {
            switch (finished)
            {
                case StateMenu: return StateMove8;
                case StateMove8: return StateTopDown;
                case StateTopDown: return StateFront1;
                case StateFront1: return StateFront3;
                case StateFront3: return StateDone;
                default: return StateDone;
            }
        }

        static void ClearOutputs()
        {
            Directory.CreateDirectory(OutputDir);
            foreach (var name in new[]
                     {
                         "menu.png", "topdown.png", "frontline_1v1.png", "frontline_3v3.png",
                         "menu_v2.png", "topdown_v2.png", "frontline_1v1_v2.png", "frontline_3v3_v2.png",
                         "menu_v4.png", "topdown_v4.png", "frontline_1v1_v4.png", "frontline_3v3_v4.png",
                         "menu_v5.png", "topdown_v5.png", "frontline_1v1_v5.png", "frontline_3v3_v5.png",
                         "backpedal_v5.png",
                         "results.txt", "stage_done.txt", "smoke_fail.txt", "smoke_round.txt"
                     })
            {
                string p = Path.Combine(OutputDir, name);
                if (File.Exists(p)) File.Delete(p);
            }
            // Clear prior move8 sequence frames.
            if (Directory.Exists(OutputDir))
            {
                foreach (var f in Directory.GetFiles(OutputDir, "move8_v5_*.png"))
                    File.Delete(f);
            }
        }

        public static bool AllOutputsPresent()
        {
            return File.Exists(Path.Combine(OutputDir, "menu_v5.png"))
                && File.Exists(Path.Combine(OutputDir, "topdown_v5.png"))
                && File.Exists(Path.Combine(OutputDir, "frontline_1v1_v5.png"))
                && File.Exists(Path.Combine(OutputDir, "frontline_3v3_v5.png"))
                && File.Exists(Path.Combine(OutputDir, "backpedal_v5.png"))
                && File.Exists(ResultsPath);
        }

        public static string ReadState()
        {
            return File.Exists(StatePath) ? File.ReadAllText(StatePath).Trim() : "";
        }

        public static void WriteState(string state)
        {
            Directory.CreateDirectory(OutputDir);
            File.WriteAllText(StatePath, state);
        }

        public static int ReadRound()
        {
            if (!File.Exists(RoundPath)) return 1;
            return int.TryParse(File.ReadAllText(RoundPath).Trim(), out int n) ? Mathf.Max(1, n) : 1;
        }

        public static void WriteRound(int round)
        {
            Directory.CreateDirectory(OutputDir);
            File.WriteAllText(RoundPath, round.ToString());
        }

        public static void AppendResult(string mode, string result, string reason, float endTimeSec)
        {
            Directory.CreateDirectory(OutputDir);
            var sb = new StringBuilder();
            if (File.Exists(ResultsPath)) sb.Append(File.ReadAllText(ResultsPath).TrimEnd()).Append("\n\n");
            sb.Append("mode=").Append(mode).Append('\n');
            sb.Append("result=").Append(result).Append('\n');
            sb.Append("reason=").Append(reason).Append('\n');
            sb.Append("end_time_sec=").Append(endTimeSec.ToString("0.###")).Append('\n');
            // Explicit per-round seconds for the bot report (same value as end_time_sec).
            sb.Append("round_seconds=").Append(endTimeSec.ToString("0.###")).Append('\n');
            File.WriteAllText(ResultsPath, sb.ToString());
        }

        public static void AppendAverage(string mode, IList<float> times)
        {
            if (times == null || times.Count == 0) return;
            float sum = 0f;
            for (int i = 0; i < times.Count; i++) sum += times[i];
            float avg = sum / times.Count;
            Directory.CreateDirectory(OutputDir);
            var sb = new StringBuilder();
            if (File.Exists(ResultsPath)) sb.Append(File.ReadAllText(ResultsPath).TrimEnd()).Append("\n\n");
            sb.Append("mode=").Append(mode).Append(" average\n");
            sb.Append("rounds=").Append(times.Count).Append('\n');
            sb.Append("end_time_sec=").Append(avg.ToString("0.###")).Append('\n');
            sb.Append("avg_round_seconds=").Append(avg.ToString("0.###")).Append('\n');
            for (int i = 0; i < times.Count; i++)
                sb.Append("round_").Append(i + 1).Append("_seconds=").Append(times[i].ToString("0.###")).Append('\n');
            File.WriteAllText(ResultsPath, sb.ToString());
        }

        public static void MarkStageDone(string stage)
        {
            Directory.CreateDirectory(OutputDir);
            File.WriteAllText(StageDonePath, stage);
            Debug.Log("[ProtoV3Smoke] stage done " + stage);
        }

        public static void Fail(string message)
        {
            Directory.CreateDirectory(OutputDir);
            File.WriteAllText(FailPath, message);
            Debug.LogError("[ProtoV3Smoke] " + message);
        }

        public static bool Has(string arg)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == arg) return true;
            return false;
        }
    }

    /// <summary>Playmode worker: autoplay + WaitForEndOfFrame ScreenCapture so overlay HUD is included.</summary>
    public sealed class ProtoV3SmokePlay : MonoBehaviour
    {
        const float TimeoutSec = 180f;
        const float MenuSettleSec = 1.2f;
        const int FrontlineRounds = 3;

        string _stage;
        bool _busy;
        bool _botReady;
        float _bombUntil = -1f;
        float _nextRoll = 4f;
        float _nextBomb = 6f;
        float _lastShot = -999f;
        bool _wasBombing;
        readonly List<float> _frontTimes = new List<float>(FrontlineRounds);

        void Awake()
        {
            _stage = ProtoV3Smoke.ReadState();
            Debug.Log("[ProtoV3Smoke] play worker stage=" + _stage + " round=" + ProtoV3Smoke.ReadRound());
        }

        void Update()
        {
            if (_busy) return;
            switch (_stage)
            {
                case ProtoV3Smoke.StateMenu:
                    _busy = true;
                    StartCoroutine(RunMenu());
                    break;
                case ProtoV3Smoke.StateMove8:
                    _busy = true;
                    StartCoroutine(RunMove8());
                    break;
                case ProtoV3Smoke.StateTopDown:
                    TickTopDown();
                    break;
                case ProtoV3Smoke.StateFront1:
                case ProtoV3Smoke.StateFront3:
                    TickFrontline();
                    break;
            }
        }

        IEnumerator RunMenu()
        {
            float t = 0f;
            while (t < MenuSettleSec)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            string path = Path.Combine(ProtoV3Smoke.OutputDir, "menu_v5.png");
            yield return CaptureHud(path);
            if (!File.Exists(path))
            {
                ProtoV3Smoke.Fail("menu_v5.png missing after capture");
                yield break;
            }
            ProtoV3Smoke.MarkStageDone(ProtoV3Smoke.StateMenu);
        }

        IEnumerator RunMove8()
        {
            float wait = 0f;
            ProtoTopDownMatch match = null;
            while (match == null && wait < 8f)
            {
                match = FindFirstObjectByType<ProtoTopDownMatch>();
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            if (match == null)
            {
                ProtoV3Smoke.Fail("move8: ProtoTopDownMatch missing");
                yield break;
            }
            match.SmokeAutoplay = true;
            match.SetDebugHud(false);
            // Pause waves so the 8-way walk is uncontested.
            match.SmokePauseWaves = true;

            // E, NE, N, NW, W, SW, S, SE
            var dirs = new[]
            {
                new Vector2(1f, 0f), new Vector2(1f, 1f).normalized, new Vector2(0f, 1f), new Vector2(-1f, 1f).normalized,
                new Vector2(-1f, 0f), new Vector2(-1f, -1f).normalized, new Vector2(0f, -1f), new Vector2(1f, -1f).normalized
            };
            const float hold = 0.55f;
            const float shotEvery = 0.12f;
            int frame = 0;
            for (int d = 0; d < dirs.Length; d++)
            {
                float t = 0f;
                float nextShot = 0f;
                while (t < hold)
                {
                    var player = match.Player;
                    if (player != null && player.OverallState == CharacterOverallState.Active)
                        player.OnMoveCommand(new MoveCommand(dirs[d], 1f, Time.time));
                    if (t + 1e-4f >= nextShot && frame < 48)
                    {
                        string path = Path.Combine(ProtoV3Smoke.OutputDir, "move8_v5_" + frame.ToString("000") + ".png");
                        yield return CaptureQuick(path);
                        frame++;
                        nextShot += shotEvery;
                    }
                    t += Time.deltaTime;
                    yield return null;
                }
            }

            // Backpedal shoot: move down, aim/shoot up.
            {
                var player = match.Player;
                if (player != null && player.OverallState == CharacterOverallState.Active)
                {
                    float now = Time.time;
                    player.OnMoveCommand(new MoveCommand(Vector2.down, 1f, now));
                    player.OnAimStartCommand(new AimStartCommand(Vector2.zero, now));
                    player.OnAimDragCommand(new AimDragCommand(Vector2.up, 1f, true, now));
                    player.OnAimReleaseCommand(new AimReleaseCommand(Vector2.up, true, now));
                }
                // Let the shoot frames start.
                float settle = 0f;
                while (settle < 0.12f)
                {
                    var p = match.Player;
                    if (p != null && p.OverallState == CharacterOverallState.Active)
                        p.OnMoveCommand(new MoveCommand(Vector2.down, 1f, Time.time));
                    settle += Time.deltaTime;
                    yield return null;
                }
                string back = Path.Combine(ProtoV3Smoke.OutputDir, "backpedal_v5.png");
                yield return CaptureHud(back);
                if (!File.Exists(back))
                {
                    ProtoV3Smoke.Fail("backpedal_v5.png missing after capture");
                    yield break;
                }
            }

            match.SmokePauseWaves = false;
            ProtoV3Smoke.MarkStageDone(ProtoV3Smoke.StateMove8);
        }

        void TickTopDown()
        {
            var match = FindFirstObjectByType<ProtoTopDownMatch>();
            if (match == null) return;
            match.SmokeAutoplay = true;
            match.SetDebugHud(false);
            if (!match.SmokeMatchEnded && match.MatchClock < TimeoutSec)
            {
                DriveTopDown(match);
                return;
            }
            _busy = true;
            StartCoroutine(FinishTopDown(match));
        }

        void DriveTopDown(ProtoTopDownMatch match)
        {
            var player = match.Player;
            if (player == null || player.OverallState != CharacterOverallState.Active) return;
            var enemies = match.Enemies;
            EnemyInstance nearest = null;
            float best = float.MaxValue;
            if (enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    var e = enemies[i];
                    if (e == null || !e.IsAlive) continue;
                    float d = (e.Position - player.Position).sqrMagnitude;
                    if (d < best) { best = d; nearest = e; }
                }
            }

            Vector2 aim = Vector2.up;
            Vector2 move = Vector2.zero;
            if (nearest != null)
            {
                Vector2 to = nearest.Position - player.Position;
                float dist = to.magnitude;
                if (dist > 0.01f) aim = to / dist;
                if (dist > 4.2f) move = aim;
                else if (dist < 2.2f) move = -aim;
                else move = new Vector2(aim.y, -aim.x);
            }
            else move = new Vector2(Mathf.Sin(match.MatchClock * 0.7f) * 0.4f, 0.25f);

            float now = Time.time;
            float mag = Mathf.Clamp01(move.magnitude);
            var dir = mag > 0.01f ? move.normalized : Vector2.zero;
            player.OnMoveCommand(new MoveCommand(dir, mag, now));

            float interval = player.Data != null && player.Data.weapon != null ? player.Data.weapon.fireInterval : 0.6f;
            if (nearest != null && now - _lastShot >= interval)
            {
                _lastShot = now;
                player.OnAimStartCommand(new AimStartCommand(Vector2.zero, now));
                player.OnAimDragCommand(new AimDragCommand(aim, 1f, true, now));
                player.OnAimReleaseCommand(new AimReleaseCommand(aim, true, now));
            }

            if (match.MatchClock >= _nextRoll)
            {
                _nextRoll = match.MatchClock + 5.5f;
                player.OnRollCommand(new RollCommand(dir.sqrMagnitude > 0.01f ? dir : aim, now));
            }

            if (match.MatchClock >= _nextBomb && player.HasGrenadeReady)
            {
                _nextBomb = match.MatchClock + 9f;
                _bombUntil = match.MatchClock + 0.55f;
                player.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, now));
                player.OnGrenadeAimDragCommand(new GrenadeAimDragCommand(aim, ThrowDistance(player), now));
                _wasBombing = true;
            }
            if (_wasBombing && match.MatchClock >= _bombUntil)
            {
                _wasBombing = false;
                Vector2 target = ClampTopDown(player.Position + aim * ThrowDistance(player));
                player.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(target, true, now));
            }
        }

        IEnumerator FinishTopDown(ProtoTopDownMatch match)
        {
            // Let ProtoControls end overlay paint.
            yield return null;
            yield return null;
            bool ended = match.SmokeMatchEnded;
            string result = ended ? match.SmokeResultLabel : "incomplete";
            string reason = ended
                ? (match.SmokeResultLabel == "클리어" ? "all-waves-cleared" : "player-eliminated")
                : "incomplete";
            float endTime = match.MatchClock;
            string path = Path.Combine(ProtoV3Smoke.OutputDir, "topdown_v5.png");
            yield return CaptureHud(path);
            if (!File.Exists(path))
            {
                ProtoV3Smoke.Fail("topdown_v5.png missing after capture");
                yield break;
            }
            ProtoV3Smoke.AppendResult("난전", result, reason, endTime);
            ProtoV3Smoke.MarkStageDone(ProtoV3Smoke.StateTopDown);
        }

        void TickFrontline()
        {
            var match = FindFirstObjectByType<FrontlineMatch>();
            if (match == null) return;
            if (!_botReady)
            {
                match.EnsureSmokePlayerBot();
                _botReady = true;
            }
            if (!match.SmokeMatchEnded && match.MatchClock < TimeoutSec) return;
            _busy = true;
            StartCoroutine(FinishFrontline(match));
        }

        IEnumerator FinishFrontline(FrontlineMatch match)
        {
            yield return null;
            yield return null;
            bool ended = match.SmokeMatchEnded;
            string result = ended ? match.SmokeResultLabel : "incomplete";
            string reason = ended ? match.SmokeDecisionReason : "incomplete";
            float endTime = match.MatchClock;
            int round = ProtoV3Smoke.ReadRound();
            bool three = _stage == ProtoV3Smoke.StateFront3;
            string mode = three ? "전선 3v3" : "전선 1v1";
            string png = three ? "frontline_3v3_v5.png" : "frontline_1v1_v5.png";

            if (round <= 1)
            {
                string path = Path.Combine(ProtoV3Smoke.OutputDir, png);
                yield return CaptureHud(path);
                if (!File.Exists(path))
                {
                    ProtoV3Smoke.Fail(png + " missing after capture");
                    yield break;
                }
                _frontTimes.Clear();
            }

            ProtoV3Smoke.AppendResult(mode + " #" + round, result, reason, endTime);
            _frontTimes.Add(endTime);

            if (round < FrontlineRounds)
            {
                ProtoV3Smoke.WriteRound(round + 1);
                _botReady = false;
                _busy = false;
                SceneManager.LoadScene(match.gameObject.scene.name);
                yield break;
            }

            ProtoV3Smoke.AppendAverage(mode, _frontTimes);
            _frontTimes.Clear();
            ProtoV3Smoke.WriteRound(1);
            ProtoV3Smoke.MarkStageDone(_stage);
        }

        IEnumerator CaptureQuick(string absolutePath)
        {
            Directory.CreateDirectory(ProtoV3Smoke.OutputDir);
            if (File.Exists(absolutePath)) File.Delete(absolutePath);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(absolutePath);
            float wait = 0f;
            while (!File.Exists(absolutePath) && wait < 2f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        IEnumerator CaptureHud(string absolutePath)
        {
            Directory.CreateDirectory(ProtoV3Smoke.OutputDir);
            if (File.Exists(absolutePath)) File.Delete(absolutePath);
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var restored = new List<(Canvas c, RenderMode mode, Camera cam)>(canvases.Length);
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                restored.Add((c, c.renderMode, c.worldCamera));
                if (cam != null)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = 1f;
                }
            }
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(absolutePath);
            float wait = 0f;
            while (!File.Exists(absolutePath) && wait < 5f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            for (int i = 0; i < restored.Count; i++)
            {
                var r = restored[i];
                if (r.c == null) continue;
                r.c.renderMode = r.mode;
                r.c.worldCamera = r.cam;
            }
        }

        static float ThrowDistance(CharacterStateMachine player)
        {
            var g = player != null && player.Data != null ? player.Data.grenade : null;
            return g != null ? Mathf.Lerp(g.minRange, g.maxRange, 0.45f) : 5f;
        }

        static Vector2 ClampTopDown(Vector2 p)
        {
            return new Vector2(
                Mathf.Clamp(p.x, ProtoArena.MinX + 0.4f, ProtoArena.MaxX - 0.4f),
                Mathf.Clamp(p.y, ProtoArena.MinY + 0.4f, ProtoArena.MaxY - 0.4f));
        }
    }
}

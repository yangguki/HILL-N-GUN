using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.Input;
using HillNGun.PvE;
using HillNGun.PvP;
using HillNGun.Characters;
using HillNGun.AI;
using HillNGun.Battlefield;
using HillNGun.Combat;
using HillNGun.Cover;
using HillNGun.Progression;
using HillNGun.Settings;
using HillNGun.External;
using HillNGun.UI;
using System;
using System.IO;
using System.Collections;

namespace HillNGun.DebugTools
{
    public class SmokeTestRunner : MonoBehaviour
    {
        /// <summary>Perf attribution switch (-perf-nogui): skips HUD/damage-number IMGUI so its draw-call share can be measured.</summary>
        private static readonly int SpikeThreshold = System.Array.Exists(Environment.GetCommandLineArgs(), a => a == "-perf-spike100") ? 100 : 150;
        public static bool NoGui = System.Array.Exists(Environment.GetCommandLineArgs(), a => a == "-perf-nogui");

        private static SmokeTestRunner _instance;
        private static bool _isSmokeTest;
        
        private string _screenshotFolder;
        private int _screenshotIndex;
        private float _matchTimer;
        private float _lastScreenshotTime;
        private float _autopilotTimer;
        private bool _pveCompleted;
        private bool _pvpCompleted;
        private SmokeTestPhase _phase = SmokeTestPhase.WaitingTitle;
        private bool _phaseScreenshotTaken;
        private AutoGunner _gunner;
        private CharacterStateMachine _gunnerOwner;
        private bool _advancedRow;

        // -smoketest-fullpve: play PvE through every stage + boss until the Result scene
        private static bool _fullPvE;
        /// <summary>-smoketest-start-stage N: PvE run begins at this stage index (0=1-1 … 3=Rex).</summary>
        private static int _startStage;
        // -smoketest-fullpvp: play a full best-of-3 PvP match (squad autopilot on both sides) until Result
        private static bool _fullPvP;
        // -smoketest-capture: after the PvE match warms up, record a 30fps PNG frame sequence of scripted
        // moves / rolls / row change / shooting (Time.captureFramerate keeps the game clock exact), then quit.
        private static bool _capture;
        private static float _captureSeconds = 7.0f;
        private int _captureFrame = -1;
        private string _framesFolder;
        private PvPMatchManager _hookedPvp;
        private bool _crateShot;
        private bool _crateOpenShot;
        private static float _timeScale = 2f;
        private float _realTimer;
        private int _lastStageIndex = -1;
        private int _lastWave = -1;
        private bool _bossSeen;
        private int _sweepShots;
        private bool _sweepShotThisWarning;
        private float _resultTimer = -1f;
        private int _lastBossPhase = -1;
        private const float FullRunRealTimeout = 900f;

        // milestone-A verification (skills / revive / phase-3 barrage)
        private AbilityPilot _pilot;
        private PvEMatchManager _hookedPve;
        private readonly System.Collections.Generic.HashSet<SkillEffect> _skillShots = new System.Collections.Generic.HashSet<SkillEffect>();
        private bool _barrageShot;
        private bool _forcedDown;
        private bool _reviveShot;
        private float _bossSpawnTime = -1f;
        private bool _bossDurationLogged;
        
        private enum SmokeTestPhase
        {
            WaitingTitle,
            Title,
            ModeSelect,
            CharacterSelect,
            PvEMatch,
            PvPModeSelect,
            PvPCharacterSelect,
            PvPMatch,
            Complete
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg.Equals("-smoketest", StringComparison.OrdinalIgnoreCase))
                {
                    _isSmokeTest = true;
                }
                else if (arg.Equals("-smoketest-fullpve", StringComparison.OrdinalIgnoreCase))
                {
                    _isSmokeTest = true;
                    _fullPvE = true;
                }
                else if (arg.Equals("-smoketest-capture", StringComparison.OrdinalIgnoreCase))
                {
                    _isSmokeTest = true;
                    _capture = true;
                }
                else if (arg.Equals("-smoketest-fullpvp", StringComparison.OrdinalIgnoreCase))
                {
                    _isSmokeTest = true;
                    _fullPvP = true;
                }
            }
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals("-smoketest-timescale", StringComparison.OrdinalIgnoreCase) &&
                    float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ts))
                {
                    _timeScale = Mathf.Clamp(ts, 0.25f, 8f);
                }
                if (args[i].Equals("-smoketest-capture-seconds", StringComparison.OrdinalIgnoreCase) &&
                    float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float cs))
                {
                    _captureSeconds = Mathf.Clamp(cs, 1f, 20f);
                }
                if (args[i].Equals("-smoketest-start-stage", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(args[i + 1], out int st))
                {
                    _startStage = Mathf.Max(0, st);
                }
            }

            if (_isSmokeTest)
            {
                // Never touch the player's real save during automated runs.
                ProgressionService.SaveFileOverride = "smoketest_save.json";
                SettingsService.SaveFileOverride = _capture ? "smoketest_capture_settings.json" : "smoketest_settings.json";
                ExternalServices.AnalyticsFileOverride = "smoketest_analytics.csv";
                if (!_fullPvE && !_fullPvP && !_capture)
                {
                    // regular smoke: start with fresh settings so the first-PvE tutorial shows
                    try { File.Delete(Path.Combine(Application.persistentDataPath, "smoketest_settings.json")); } catch { }
                }
                var go = new GameObject("[SmokeTestRunner]");
                _instance = go.AddComponent<SmokeTestRunner>();
                DontDestroyOnLoad(go);
                Debug.Log("[SmokeTest] Smoke test mode enabled");
            }
        }

        private void Awake()
        {
            if (!_isSmokeTest)
            {
                Destroy(gameObject);
                return;
            }

            _screenshotFolder = Path.Combine(Application.persistentDataPath, "smoketest");
            if (Directory.Exists(_screenshotFolder))
            {
                foreach (var file in Directory.GetFiles(_screenshotFolder, "*.png"))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(_screenshotFolder);
            }
            
            Debug.Log($"[SmokeTest] Screenshots will be saved to: {_screenshotFolder}");
            if (_capture)
            {
                _framesFolder = Path.Combine(_screenshotFolder, "frames");
                if (Directory.Exists(_framesFolder)) foreach (var f in Directory.GetFiles(_framesFolder, "*.png")) File.Delete(f);
                else Directory.CreateDirectory(_framesFolder);
                SettingsService.Instance.Data.tutorialDone = true; // clean frames: no tutorial overlay
                Debug.Log($"[SmokeTest] CAPTURE mode: {_captureSeconds:F1}s @30fps -> {_framesFolder}");
            }
            Application.logMessageReceived += OnLogMessage;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                Debug.Log($"[SmokeTest] Caught {type}: {condition}\n{stackTrace}");
            }
        }

        private void SetPhase(SmokeTestPhase newPhase)
        {
            _phase = newPhase;
            _phaseScreenshotTaken = false;
            _autopilotTimer = 0f;
            _gunner = null;
            _gunnerOwner = null;
            _pilot = null;
            _advancedRow = false;
        }

        private void HookPvE(PvEMatchManager pve)
        {
            if (_hookedPve == pve) return;
            _hookedPve = pve;
            pve.OnSkillUsed += (c, effect) =>
            {
                Timeline($"SKILL {c.Data?.skill?.id} ({effect}) by {c.Data?.id}");
                if (_skillShots.Add(effect)) StartCoroutine(ShotAfter($"skill_{effect}", 0.25f));
            };
            pve.OnAllyRevived += (r, c) => Timeline($"REVIVED {c.Data?.id} hp={c.CurrentHp}");
            pve.OnBossVolley += (boss, burstIndex) =>
            {
                if (boss.CurrentPhase != BossPhase.Phase3) return;
                if (burstIndex >= 2) Timeline($"BOSS P3 barrage volley {burstIndex}/{boss.CurrentPhaseData?.fanShotBurstCount} y={boss.Position.y:F1}");
                if (!_barrageShot && burstIndex >= 2)
                {
                    _barrageShot = true;
                    StartCoroutine(ShotAfter("boss_phase3_barrage", 0.35f));
                }
            };
        }

        /// <summary>Smoke-only test hook: down one AI ally once in the boss fight so the revive path is exercised.</summary>
        private void CheckReviveTest(PvEMatchManager pve, BossInstance boss)
        {
            if (!_forcedDown && boss != null && boss.CurrentPhase >= BossPhase.Phase2)
            {
                for (int i = 0; i < pve.PlayerTeam.Count; i++)
                {
                    var c = pve.PlayerTeam[i];
                    if (i == pve.PlayerControlledIndex || c == null || c.OverallState != CharacterOverallState.Active || c.IsInvulnerable) continue;
                    c.TakeDamage(c.CurrentHp + 1, Vector2.down);
                    if (c.OverallState != CharacterOverallState.Downed) continue;
                    _forcedDown = true;
                    Timeline($"TEST HOOK: forced {c.Data?.id} downed to exercise revive");
                    break;
                }
            }
            if (_forcedDown && !_reviveShot)
            {
                foreach (var c in pve.PlayerTeam)
                {
                    if (c != null && c.OverallState == CharacterOverallState.Downed && c.ReviveProgress >= 0.4f)
                    {
                        _reviveShot = true;
                        Timeline($"REVIVE in progress {c.Data?.id} {c.ReviveProgress:P0}");
                        StartCoroutine(TakeScreenshotDelayed("revive_progress"));
                        break;
                    }
                }
            }
        }

        // Performance sampling during matches (real-time frames only; capture mode uses a fixed frame rate)
        private int _fpsFrames, _fpsSlow;
        private double _fpsTime;
        private long _drawSum, _setPassSum, _drawMax;
        private int _drawSamples;
        private Unity.Profiling.ProfilerRecorder _drawRec, _setPassRec;

        private void SamplePerf()
        {
            if (Time.captureFramerate != 0) return;
            if (_phase != SmokeTestPhase.PvEMatch && _phase != SmokeTestPhase.PvPMatch) return;
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f || dt > 0.5f) return;
            if (!_drawRec.Valid)
            {
                _drawRec = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Draw Calls Count");
                _setPassRec = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "SetPass Calls Count");
            }
            _fpsFrames++;
            _fpsTime += dt;
            if (dt > 1f / 45f) _fpsSlow++;
            // Frames around a screenshot re-render the scene for the capture: excluded so they don't masquerade as gameplay peaks.
            bool shotFrame = Time.frameCount - _shotFrame <= 2;
            if (shotFrame && _drawRec.Valid) _drawShotMax = System.Math.Max(_drawShotMax, _drawRec.LastValue);
            if (_drawRec.Valid && _drawRec.LastValue > 0 && !shotFrame)
            {
                _drawHist[(int)System.Math.Min(_drawRec.LastValue / 10, _drawHist.Length - 1)]++;
                // Gameplay-only stats: match scenes while the simulation runs (menus / pause overlay excluded).
                if (Time.timeScale > 0f && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.EndsWith("_Match"))
                {
                    _playHist[(int)System.Math.Min(_drawRec.LastValue / 10, _playHist.Length - 1)]++;
                    _playMax = System.Math.Max(_playMax, _drawRec.LastValue);
                    _playSum += _drawRec.LastValue; _playSetPassSum += _setPassRec.Valid ? _setPassRec.LastValue : 0; _playN++;
                }
                if (_drawRec.LastValue > SpikeThreshold && Time.unscaledTime - _lastSpikeLog > 1f && _spikeLogs < 40)
                {
                    _lastSpikeLog = Time.unscaledTime; _spikeLogs++;
                    Debug.Log($"[SmokeTest] PERF spike draws={_drawRec.LastValue} setPass={_setPassRec.LastValue} t={Time.timeSinceLevelLoad:F1}s {Census()}");
                }
                _drawSum += _drawRec.LastValue; _drawSamples++;
                string sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (!_sceneMax.TryGetValue(sc, out long sm) || _drawRec.LastValue > sm) _sceneMax[sc] = _drawRec.LastValue;
                if (_drawRec.LastValue > _drawMax)
                {
                    if (_fpsFrames > 300 && _drawRec.LastValue > _drawMax + 15) _peakCensus = Census();
                    _drawMax = _drawRec.LastValue;
                }
                if (_setPassRec.Valid) _setPassSum += _setPassRec.LastValue;
            }
            if (_fpsFrames % 600 == 0) LogPerf("running");
        }

        private string _peakCensus = "";
        private static int _shotFrame = -100;
        private float _lastSpikeLog; private int _spikeLogs;
        private long _drawShotMax;
        private readonly int[] _drawHist = new int[100];
        private readonly int[] _playHist = new int[100];
        private long _playMax, _playSum, _playSetPassSum, _playN;

        private int DrawPercentile(double q) => Percentile(_drawHist, q);

        private static int Percentile(int[] hist, double q)
        {
            long total = 0; foreach (var c in hist) total += c;
            long need = (long)(total * q), acc = 0;
            for (int i = 0; i < hist.Length; i++) { acc += hist[i]; if (acc >= need) return (i + 1) * 10; }
            return hist.Length * 10;
        }
        private readonly System.Collections.Generic.Dictionary<string, long> _sceneMax = new System.Collections.Generic.Dictionary<string, long>();

        /// <summary>What was on screen at a draw-call peak (active renderers by type, distinct materials).</summary>
        private static string Census()
        {
            int sr = 0, mr = 0, smr = 0, lr = 0, tm = 0, other = 0;
            var mats = new System.Collections.Generic.HashSet<int>();
            var otherTypes = new System.Collections.Generic.HashSet<string>();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || !r.isVisible) continue;
                if (r is SpriteRenderer) sr++;
                else if (r is SkinnedMeshRenderer) smr++;
                else if (r is LineRenderer) lr++;
                else if (r is MeshRenderer) { if (r.GetComponent<TextMesh>() != null) tm++; else mr++; }
                else { other++; otherTypes.Add(r.GetType().Name + ":" + r.name); }
                if (r.sharedMaterial != null) mats.Add(r.sharedMaterial.GetInstanceID());
            }
            // Estimated 2D batches: transparent renderers in sort order, a new batch whenever material/texture changes.
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.enabled && r.gameObject.activeInHierarchy && r.isVisible) list.Add(r);
            list.Sort((a, b) => a.sortingLayerID != b.sortingLayerID ? SortingLayer.GetLayerValueFromID(a.sortingLayerID).CompareTo(SortingLayer.GetLayerValueFromID(b.sortingLayerID))
                : a.sortingOrder != b.sortingOrder ? a.sortingOrder.CompareTo(b.sortingOrder) : b.transform.position.z.CompareTo(a.transform.position.z));
            int batches = 0; string prevKey = null;
            var keyCount = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var r in list)
            {
                string key = r.GetType().Name.Replace("Renderer", "") + ":" + (r.sharedMaterial != null ? r.sharedMaterial.name : "null") + "/" + (r is SpriteRenderer s2 && s2.sprite != null ? s2.sprite.texture.name + "#" + s2.sprite.texture.GetInstanceID() % 1000 : "-");
                if (key != prevKey) { batches++; prevKey = key; }
                keyCount[key] = keyCount.TryGetValue(key, out int kc) ? kc + 1 : 1;
            }
            var top = new System.Collections.Generic.List<string>();
            foreach (var kv in keyCount) top.Add(kv.Key + "x" + kv.Value);
            return $"est2Dbatches={batches} keys[{string.Join(" ", top)}] scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} cams={Camera.allCamerasCount} sprites={sr} meshes={mr} skinned={smr} lines={lr} textMeshes={tm} other={other}({string.Join(",", otherTypes)}) materials={mats.Count} fx={HillNGun.Feel.EffectsManager.ActiveSummary}";
        }

        private void LogPerf(string tag)
        {
            if (_fpsFrames == 0) return;
            string draws = _drawSamples > 0 ? $"drawCalls avg={_drawSum / (double)_drawSamples:F0} max={_drawMax} p95={DrawPercentile(0.95)} p99={DrawPercentile(0.99)} (screenshot frames max={_drawShotMax}) setPass avg={_setPassSum / (double)_drawSamples:F0}" : "drawCalls n/a (release player)";
            Debug.Log($"[SmokeTest] PERF {tag}: avgFPS={_fpsFrames / _fpsTime:F1} frames={_fpsFrames} slow(<45fps)={100.0 * _fpsSlow / _fpsFrames:F1}% {draws} actor3D={HillNGun.View3D.Actor3DStage.Enabled} peak[{_peakCensus}]");
            if (tag == "final")
            {
                var sb = new System.Text.StringBuilder();
                foreach (var kv in _sceneMax) sb.Append(kv.Key).Append('=').Append(kv.Value).Append(' ');
                Debug.Log($"[SmokeTest] PERF sceneMax: {sb}");
                Debug.Log($"[SmokeTest] PERF vfx: impactParticlesPeak={HillNGun.Feel.VfxPool.PeakLiveParticles} (cap {HillNGun.Feel.VfxPool.MaxLiveParticles}) cappedSpawns={HillNGun.Feel.VfxPool.CappedSpawns} stageVisualApplies={HillNGun.Battlefield.BattlefieldRenderer.VisualApplyCount}");
                if (_playN > 0)
                    Debug.Log($"[SmokeTest] PERF gameplay: drawCalls avg={_playSum / (double)_playN:F0} p95<={Percentile(_playHist, 0.95)} p99<={Percentile(_playHist, 0.99)} max={_playMax} setPass avg={_playSetPassSum / (double)_playN:F0} frames={_playN}");
            }
        }

        private void Update()
        {
            if (!_isSmokeTest) return;
            SamplePerf();

            switch (_phase)
            {
                case SmokeTestPhase.WaitingTitle:
                    UpdateWaitingTitle();
                    break;
                case SmokeTestPhase.Title:
                    UpdateTitle();
                    break;
                case SmokeTestPhase.ModeSelect:
                    UpdateModeSelect();
                    break;
                case SmokeTestPhase.CharacterSelect:
                    UpdateCharacterSelect();
                    break;
                case SmokeTestPhase.PvEMatch:
                    UpdatePvEMatch();
                    break;
                case SmokeTestPhase.PvPModeSelect:
                    UpdatePvPModeSelect();
                    break;
                case SmokeTestPhase.PvPCharacterSelect:
                    UpdatePvPCharacterSelect();
                    break;
                case SmokeTestPhase.PvPMatch:
                    UpdatePvPMatch();
                    break;
                case SmokeTestPhase.Complete:
                    CompleteTest();
                    break;
            }
        }

        private void UpdateWaitingTitle()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Title" || sceneName.Contains("Title"))
            {
                SetPhase(SmokeTestPhase.Title);
            }
        }

        private void UpdateTitle()
        {
            _autopilotTimer += Time.deltaTime;
            
            if (!_phaseScreenshotTaken && _autopilotTimer > 0.5f)
            {
                _phaseScreenshotTaken = true;
                StartCoroutine(TakeScreenshotDelayed("title_screen"));
            }
            
            if (_autopilotTimer > 2f)
            {
                Debug.Log("[SmokeTest] Advancing from Title to ModeSelect");
                GameFlowManager.Instance?.GoToModeSelect();
                SetPhase(SmokeTestPhase.ModeSelect);
            }
        }

        private int _lobbyStep;
        private float _lobbyStepTime;

        private void UpdateModeSelect()
        {
            _autopilotTimer += Time.deltaTime;
            var lobby = FindFirstObjectByType<LobbyUI>();
            if (lobby == null) return;

            if (_fullPvE || _fullPvP)
            {
                if (_autopilotTimer < 1.5f) return;
                if (_fullPvP)
                {
                    Debug.Log("[SmokeTest] Lobby -> PvP (full PvP run)");
                    lobby.StartPvP();
                    SetPhase(SmokeTestPhase.PvPCharacterSelect);
                }
                else
                {
                    Debug.Log($"[SmokeTest] Lobby -> stage index {_startStage}");
                    lobby.StartPvE(_startStage);
                    SetPhase(SmokeTestPhase.CharacterSelect);
                }
                return;
            }

            // Regular smoke: tour the lobby tabs with a screenshot each, then start PvE from the stage tab.
            if (_autopilotTimer - _lobbyStepTime < 1.0f) return;
            _lobbyStepTime = _autopilotTimer;
            switch (_lobbyStep++)
            {
                case 0: lobby.SetTab(LobbyTab.Home); break;
                case 1: StartCoroutine(TakeScreenshotDelayed("lobby_home")); break;
                case 2: lobby.SetTab(LobbyTab.Stages); break;
                case 3: StartCoroutine(TakeScreenshotDelayed("lobby_stages")); break;
                case 4: lobby.SetTab(LobbyTab.Characters); break;
                case 5: StartCoroutine(TakeScreenshotDelayed("lobby_characters")); break;
                case 6: lobby.SetTab(LobbyTab.Shop); break;
                case 7: StartCoroutine(TakeScreenshotDelayed("lobby_shop")); break;
                case 8: lobby.SetTab(LobbyTab.Settings); break;
                case 9: StartCoroutine(TakeScreenshotDelayed("lobby_settings")); break;
                case 10:
                    var d = ProgressionService.Instance.Data;
                    Debug.Log($"[SmokeTest] Lobby tour done (bounty={d.bounty} badge={d.badgeLevel} regions={string.Join(",", d.unlockedRegions)}) -> stage index {_startStage}");
                    lobby.StartPvE(_startStage);
                    SetPhase(SmokeTestPhase.CharacterSelect);
                    break;
            }
        }

        private bool _tutorialShot;
        private bool _pauseShot;

        private IEnumerator PauseSettingsShot()
        {
            var pause = FindFirstObjectByType<PauseMenuController>();
            if (pause == null) yield break;
            pause.Pause();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return TakeScreenshotDelayed("pause_menu");
            pause.OpenSettings();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return TakeScreenshotDelayed("pause_settings");
            pause.Resume();
            Debug.Log("[SmokeTest] Pause menu + settings opened and resumed");
        }

        private void UpdateCharacterSelect()
        {
            _autopilotTimer += Time.deltaTime;
            
            if (!_phaseScreenshotTaken && _autopilotTimer > 0.5f)
            {
                _phaseScreenshotTaken = true;
                StartCoroutine(TakeScreenshotDelayed("character_select_pve"));
            }
            
            if (_autopilotTimer > 2f)
            {
                Debug.Log("[SmokeTest] Starting PvE match with default characters");
                var flowManager = GameFlowManager.Instance;
                if (flowManager != null)
                {
                    flowManager.SetAllCharacters("buster", "raven", "volt");
                    flowManager.StartMatch();
                }
                SetPhase(SmokeTestPhase.PvEMatch);
                _matchTimer = 0f;
                _lastScreenshotTime = -5f;
                if (_fullPvE)
                {
                    Time.timeScale = _timeScale;
                    _realTimer = 0f;
                    Debug.Log($"[SmokeTest] FULL PvE run started (timeScale={_timeScale})");
                }
            }
        }

        private void Timeline(string evt)
        {
            Debug.Log($"[SmokeTest][Timeline] game={_matchTimer:F1}s real={_realTimer:F1}s {evt}");
        }

        private void UpdateFullPvE()
        {
            _matchTimer += Time.deltaTime;
            _realTimer += Time.unscaledDeltaTime;
            _autopilotTimer += Time.deltaTime;

            if (_realTimer > FullRunRealTimeout)
            {
                Timeline("TIMEOUT - full run did not reach Result");
                Time.timeScale = 1f;
                SetPhase(SmokeTestPhase.Complete);
                return;
            }

            string scene = SceneManager.GetActiveScene().name;
            if (scene.Contains("Result"))
            {
                if (_resultTimer < 0f)
                {
                    _resultTimer = 0f;
                    Time.timeScale = 1f;
                    var session = GameFlowManager.Instance?.CurrentSession;
                    Timeline($"RESULT scene loaded (result={session?.result})");
                    LogProgression("after PvE");
                    TryProgressionPurchases();
                }
                _resultTimer += Time.unscaledDeltaTime;
                if (_resultTimer > 1.5f)
                {
                    _pveCompleted = true;
                    SetPhase(SmokeTestPhase.Complete);
                    _phase = SmokeTestPhase.Complete;
                    StartCoroutine(TakeScreenshotDelayed("result_screen"));
                }
                return;
            }

            var pve = FindFirstObjectByType<PvEMatchManager>();
            if (pve == null) return;

            HookPvE(pve);
            RunAutopilot();

            if (pve.CurrentStageIndex != _lastStageIndex)
            {
                _lastStageIndex = pve.CurrentStageIndex;
                _lastWave = -1;
                Timeline($"STAGE {pve.CurrentStageIndex + 1}/{pve.StageCount} start ({pve.CurrentStageId}) gaugeMax={pve.GaugeMax} waves={pve.TotalWaves}");
            }
            if (pve.IsMatchActive && pve.ActiveBoss == null && pve.CurrentWave != _lastWave && pve.CurrentWave >= 0)
            {
                _lastWave = pve.CurrentWave;
                Timeline($"stage {pve.CurrentStageIndex + 1} WAVE {pve.CurrentWave + 1}/{pve.TotalWaves} gauge={pve.CurrentGauge}/{pve.GaugeMax}");
                if (pve.CurrentStageIndex == 0 || pve.CurrentWave >= 2)
                    StartCoroutine(ShotAfter($"stage{pve.CurrentStageIndex + 1}_wave{pve.CurrentWave + 1}", 6f)); // after delayBeforeWave (3s) + walk-in
            }

            var boss = pve.ActiveBoss;
            if (boss != null)
            {
                if (!_bossSeen)
                {
                    _bossSeen = true;
                    _bossSpawnTime = _matchTimer;
                    Timeline($"BOSS spawned ({boss.Data?.id} hp={boss.CurrentHp}/{boss.MaxHp} party={pve.PartySize})");
                    StartCoroutine(ShotAfter("boss_entry", 2f));
                }
                if ((int)boss.CurrentPhase != _lastBossPhase)
                {
                    _lastBossPhase = (int)boss.CurrentPhase;
                    Timeline($"BOSS phase {boss.CurrentPhase} hp={boss.CurrentHp}");
                }
                bool sweepWarn = boss.CurrentWarning == BossInstance.WarningType.TailSweep;
                if (sweepWarn && !_sweepShotThisWarning && boss.WarningRemaining < (boss.CurrentPhaseData?.tailSweepWarning ?? 1f) * 0.6f)
                {
                    _sweepShotThisWarning = true;
                    Timeline($"TAIL SWEEP warning area={boss.TailSweepArea}");
                    if (_sweepShots < 3)
                    {
                        _sweepShots++;
                        StartCoroutine(TakeScreenshotDelayed($"boss_tailsweep_warning_{_sweepShots}"));
                    }
                }
                if (!sweepWarn) _sweepShotThisWarning = false;
                CheckReviveTest(pve, boss);
            }
            else if (_bossSeen && !_bossDurationLogged)
            {
                _bossDurationLogged = true;
                Timeline($"BOSS fight over after {_matchTimer - _bossSpawnTime:F1}s game time; skills={pve.SkillsUsed} grenades={pve.GrenadesThrown} revives={pve.RevivesCompleted} volleys={pve.BossVolleys} p3volleys={pve.BossPhase3Volleys}");
            }

            if (_matchTimer - _lastScreenshotTime >= 15f)
            {
                _lastScreenshotTime = _matchTimer;
                LogMatchState("PvE");
                Debug.Log($"[SmokeTest] TailSweeps={pve.TailSweepCount} hits={pve.TailSweepHits} skills={pve.SkillsUsed} grenades={pve.GrenadesThrown} revives={pve.RevivesCompleted}");
                StartCoroutine(TakeScreenshotDelayed($"pve_full_{Mathf.FloorToInt(_matchTimer)}s"));
            }
        }

        private IEnumerator ShotAfter(string name, float gameSeconds)
        {
            yield return new WaitForSeconds(gameSeconds);
            if (SceneManager.GetActiveScene().name.Contains("Result")) yield break;
            LogMatchState("PvE");
            yield return TakeScreenshotDelayed(name);
        }

        private void UpdatePvEMatch()
        {
            if (_fullPvE)
            {
                UpdateFullPvE();
                return;
            }
            if (_capture)
            {
                UpdateCapture();
                return;
            }

            _matchTimer += Time.deltaTime;
            _autopilotTimer += Time.deltaTime;
            
            RunAutopilot();

            var tut = FindFirstObjectByType<TutorialOverlay>();
            if (!_tutorialShot && tut != null && tut.IsActive && tut.StepIndex >= 0)
            {
                _tutorialShot = true;
                Debug.Log($"[SmokeTest] Tutorial active step {tut.StepIndex + 1}/{tut.StepCount}");
                StartCoroutine(TakeScreenshotDelayed("tutorial_overlay"));
            }
            if (!_pauseShot && _matchTimer > 12f)
            {
                _pauseShot = true;
                StartCoroutine(PauseSettingsShot());
            }
            
            if (_matchTimer - _lastScreenshotTime >= 5f)
            {
                _lastScreenshotTime = _matchTimer;
                LogMatchState("PvE");
                StartCoroutine(TakeScreenshotDelayed($"pve_match_{Mathf.FloorToInt(_matchTimer)}s"));
            }

            if (_matchTimer >= 20f)
            {
                Debug.Log("[SmokeTest] PvE match time complete (20s). Moving to PvP.");
                _pveCompleted = true;
                GameFlowManager.Instance?.GoToModeSelect();
                SetPhase(SmokeTestPhase.PvPModeSelect);
            }
        }

        private CharacterStateMachine CaptureSubject()
        {
            var pve = FindFirstObjectByType<PvEMatchManager>();
            if (pve == null || pve.PlayerTeam == null || pve.PlayerTeam.Count == 0) return null;
            return pve.PlayerTeam[Mathf.Clamp(pve.PlayerControlledIndex, 0, pve.PlayerTeam.Count - 1)];
        }

        private void UpdateCapture()
        {
            _matchTimer += Time.deltaTime;
            _autopilotTimer += Time.deltaTime;
            var me = CaptureSubject();
            if (_captureFrame < 0)
            {
                // warm-up: let enemies enter and the squad settle (regular autopilot), then start recording
                RunAutopilot();
                if (_matchTimer >= 3.5f && me != null && me.OverallState == CharacterOverallState.Active &&
                    me.MovementState != MovementState.Rolling && me.MovementState != MovementState.RowChanging)
                {
                    me.OnMoveCommand(MoveCommand.Stop(Time.time));
                    me.OnAimReleaseCommand(new AimReleaseCommand(me.AimDirection, false, Time.time)); // warm-up gunner off
                    _gunner = null; _gunnerOwner = null;
                    _captureCenterDir = me.Position.x < 12f ? 1f : -1f;
                    _captureFrame = 0;
                    _captureRolls = 0;
                    Time.timeScale = 1f;
                    Time.captureFramerate = 30;
                    Debug.Log($"[SmokeTest] CAPTURE start at match t={_matchTimer:F1}s subject={me.Data?.id} row={me.CurrentRowIndex} x={me.Position.x:F2}");
                }
                return;
            }

            float t = _captureFrame / 30f;
            if (me != null && me.OverallState == CharacterOverallState.Active) ScriptedMotion(me, t);
            _shotFrame = Time.frameCount;
            ScreenCapture.CaptureScreenshot(Path.Combine(_framesFolder, $"frame_{_captureFrame:D4}.png"), 2); // 2x supersample for zoom crops
            if (_captureFrame % 15 == 0 && me != null)
                Debug.Log($"[SmokeTest] CAPTURE f={_captureFrame} t={t:F2} state={me.MovementState} x={me.Position.x:F2} y={me.Position.y:F2} vx={me.Velocity.x:F2} inv={me.IsInvulnerable} rec={me.IsRecovering} rollCd={me.RollCooldownRatio:F2}");
            _captureFrame++;
            if (t >= _captureSeconds)
            {
                Time.captureFramerate = 0;
                Debug.Log($"[SmokeTest] CAPTURE done: {_captureFrame} frames, rolls={_captureRolls}");
                _captureFrame = int.MinValue / 2;
                SetPhase(SmokeTestPhase.Complete);
                CompleteTest();
            }
        }

        private int _captureRolls;
        private float _captureCenterDir = 1f;
        private float _lastScriptRoll = -10f;
        private int _scriptStage = -1;

        /// <summary>Deterministic demo: run, stop, roll, slow analog walk, row change, strafe-shoot, row roll, run.</summary>
        private void ScriptedMotion(CharacterStateMachine me, float t)
        {
            float now = Time.time;
            float centerDir = _captureCenterDir;
            int stage = t < 0.9f ? 0 : t < 1.35f ? 1 : t < 1.9f ? 2 : t < 2.7f ? 3 : t < 3.3f ? 4 : t < 4.6f ? 5 : t < 5.45f ? 6 : 7;
            bool enter = stage != _scriptStage;
            _scriptStage = stage;
            if (enter) Debug.Log($"[SmokeTest] CAPTURE stage {stage} at t={t:F2}");
            switch (stage)
            {
                case 0: me.OnMoveCommand(new MoveCommand(new Vector2(centerDir, 0f), 1f, now)); break;          // accel + run
                case 1: me.OnMoveCommand(MoveCommand.Stop(now)); break;                                          // decel stop
                case 2:
                    if (enter) { me.OnRollCommand(new RollCommand(new Vector2(-centerDir, 0f), now)); _captureRolls++; } // eased roll
                    break;
                case 3: me.OnMoveCommand(new MoveCommand(new Vector2(centerDir, 0f), 0.3f, now)); break;          // analog slow walk
                case 4:
                    if (enter) { me.OnMoveCommand(MoveCommand.Stop(now)); me.OnRowChangeCommand(new RowChangeCommand(me.CurrentRowIndex > 0 ? -1 : 1, now)); }
                    break;                                                                                          // eased row change
                case 5:
                    me.OnMoveCommand(new MoveCommand(new Vector2(-centerDir, 0f), 1f, now));                        // strafe while shooting (aim facing)
                    if (_gunner == null || _gunnerOwner != me) {
                        _gunnerOwner = me;
                        // Pacing mul is PvE-only (06 stage time bands). PvP smoke keeps full fire rate.
                        float mul = 1f;
                        if (_fullPvE)
                        {
                            var bot = HillNGun.Core.RuntimeAssetManager.Instance?.BotBehavior;
                            if (bot != null) mul = bot.pveAllyShotCooldownMul;
                        }
                        _gunner = new AutoGunner(me, 0.3f, mul);
                    }
                    var pve = FindFirstObjectByType<PvEMatchManager>();
                    _gunner.Tick(Time.deltaTime, pve != null ? pve.FindTargetFor(me.Position, 40f) : (Vector2?)null);
                    break;
                case 6:
                    if (enter)
                    {
                        me.OnAimReleaseCommand(new AimReleaseCommand(me.AimDirection, false, now));
                        me.OnMoveCommand(MoveCommand.Stop(now));
                    }
                    break;
                default:
                    // row roll forward (or a side roll if no row that way), then run
                    if (enter || (_captureRolls < 2 && me.RollCooldownRatio <= 0f))
                    {
                        if (me.RollCooldownRatio <= 0f && me.MovementState != MovementState.Rolling)
                        {
                            int dir = me.CurrentRowIndex < 2 ? 1 : -1;
                            me.OnRollCommand(new RollCommand(new Vector2(0f, dir), now));
                            _captureRolls++;
                        }
                    }
                    if (t > 6.0f) me.OnMoveCommand(new MoveCommand(new Vector2(centerDir, 0f), 1f, now));
                    break;
            }
        }

        private void UpdatePvPModeSelect()
        {
            _autopilotTimer += Time.deltaTime;
            
            if (!_phaseScreenshotTaken && _autopilotTimer > 0.5f)
            {
                _phaseScreenshotTaken = true;
                StartCoroutine(TakeScreenshotDelayed("pvp_mode_select"));
            }
            
            if (_autopilotTimer > 2f)
            {
                Debug.Log("[SmokeTest] Selecting PvP mode");
                GameFlowManager.Instance?.GoToCharacterSelect(GameMode.PvP);
                SetPhase(SmokeTestPhase.PvPCharacterSelect);
            }
        }

        private void UpdatePvPCharacterSelect()
        {
            _autopilotTimer += Time.deltaTime;
            
            if (!_phaseScreenshotTaken && _autopilotTimer > 0.5f)
            {
                _phaseScreenshotTaken = true;
                StartCoroutine(TakeScreenshotDelayed("character_select_pvp"));
            }
            
            if (_autopilotTimer > 2f)
            {
                Debug.Log("[SmokeTest] Starting PvP match");
                var flowManager = GameFlowManager.Instance;
                if (flowManager != null)
                {
                    flowManager.SetAllCharacters("buster", "raven", "volt");
                    if (_fullPvP)
                    {
                        var rules = RuntimeAssetManager.Instance?.MatchRules;
                        // 12 §8 P0: optional PvP systems stay at their data default (OFF) unless explicitly asked
                        bool forceOptional = Array.Exists(Environment.GetCommandLineArgs(), a => a.Equals("-smoketest-pvp-optional", StringComparison.OrdinalIgnoreCase));
                        if (!forceOptional) Debug.Log($"[SmokeTest] PvP optional systems at data default: supply={rules?.supplyCrateEnabled} lateDecay={rules?.lateCoverDecayEnabled} (pass -smoketest-pvp-optional to force on)");
                        if (rules != null && forceOptional)
                        {
                            // TEST: proposals (04 B-4) default off; force on so the run verifies them.
                            rules.supplyCrateEnabled = true;
                            rules.lateCoverDecayEnabled = true;
                            rules.supplyCrateTime = 20f;
                            Debug.Log("[SmokeTest] TEST: forcing supplyCrateEnabled/lateCoverDecayEnabled on, crate at 20s (runtime only)");
                        }
                    }
                    flowManager.StartMatch();
                }
                SetPhase(SmokeTestPhase.PvPMatch);
                _matchTimer = 0f;
                _lastScreenshotTime = -5f;
                if (_fullPvP)
                {
                    Time.timeScale = _timeScale;
                    _realTimer = 0f;
                    Debug.Log($"[SmokeTest] FULL PvP run started (timeScale={_timeScale})");
                }
            }
        }

        private void HookPvP(PvPMatchManager pvp)
        {
            if (pvp == null || _hookedPvp == pvp) return;
            if (pvp.PlayerTeam == null || pvp.PlayerTeam.Count == 0) return;
            _hookedPvp = pvp;
            // Player squad leader is driven by the same BotBrain + abilities as the enemy squad (not a pistol-only gunner).
            pvp.SetPlayerAutopilot(true);
            Timeline("PvP player autopilot ON (BotBrain + abilities)");
            pvp.OnRoundStarted += r =>
            {
                Timeline($"PvP ROUND {r} countdown (score {pvp.PlayerRoundsWon}-{pvp.EnemyRoundsWon})");
                if (_fullPvP) StartCoroutine(ShotAfterAny($"pvp_round{r}_countdown", 1.2f));
            };
            pvp.OnRoundEnded += res =>
            {
                Timeline($"PvP ROUND {pvp.CurrentRound} END {res} reason={pvp.LastRoundReason} score {pvp.PlayerRoundsWon}-{pvp.EnemyRoundsWon} kills={pvp.PlayerKills} deaths={pvp.PlayerDeaths}");
                if (_fullPvP) StartCoroutine(ShotAfterAny($"pvp_round{pvp.CurrentRound}_result", 0.8f));
            };
            pvp.OnSupplyCrateDropped += c =>
            {
                Timeline("PvP SUPPLY CRATE dropped");
                if (!_crateShot) { _crateShot = true; StartCoroutine(ShotAfterAny("pvp_supply_crate", 0.25f)); }
            };
            pvp.OnSupplyCrateOpened += (c, team) =>
            {
                Timeline($"PvP SUPPLY CRATE opened by {team}");
                if (!_crateOpenShot) { _crateOpenShot = true; StartCoroutine(ShotAfterAny("pvp_supply_crate_opened", 0.3f)); }
            };
            pvp.OnMatchEnded += m => Timeline($"PvP MATCH END {m} rounds {pvp.PlayerRoundsWon}-{pvp.EnemyRoundsWon}");
        }

        private IEnumerator ShotAfterAny(string name, float gameSeconds)
        {
            yield return new WaitForSeconds(gameSeconds);
            if (SceneManager.GetActiveScene().name.Contains("Result")) yield break;
            yield return TakeScreenshotDelayed(name);
        }

        private void LogProgression(string when)
        {
            var ps = ProgressionService.Instance;
            var d = ps?.Data;
            if (d == null) { Debug.Log($"[SmokeTest] Progression ({when}): none"); return; }
            string chars = "";
            foreach (var c in d.characters) chars += $" {c.id}:Lv{c.masteryLevel}/{c.masteryXp}xp skin={c.equippedSkin}";
            Debug.Log($"[SmokeTest] Progression ({when}): bounty={d.bounty} badge=Lv{d.badgeLevel} ({d.badgeXp}xp) cleared=[{string.Join(",", d.clearedStages)}] regions=[{string.Join(",", d.unlockedRegions)}] skins=[{string.Join(",", d.ownedSkins)}] pve={d.pveRuns} boss={d.bossKills} pvp W{d.pvpWins}/L{d.pvpLosses}/D{d.pvpDraws}{chars}");
        }

        private void TryProgressionPurchases()
        {
            var ps = ProgressionService.Instance;
            if (ps?.Data == null) return;
            if (!ps.OwnsSkin("buster_gold"))
            {
                bool bought = ps.TryPurchaseSkin("buster_gold");
                Debug.Log($"[SmokeTest] TEST purchase buster_gold -> {bought} (bounty now {ps.Data.bounty})");
            }
            if (ps.OwnsSkin("buster_gold")) Debug.Log($"[SmokeTest] TEST equip buster_gold -> {ps.EquipSkin("buster", "buster_gold")}");
            if (ps.OwnsSkin("raven_dusty")) Debug.Log($"[SmokeTest] TEST equip raven_dusty -> {ps.EquipSkin("raven", "raven_dusty")}");
            LogProgression("after purchases");
        }

        private void UpdateFullPvP()
        {
            _matchTimer += Time.deltaTime;
            _realTimer += Time.unscaledDeltaTime;
            if (_realTimer > FullRunRealTimeout)
            {
                Timeline("TIMEOUT - full PvP run did not reach Result");
                Time.timeScale = 1f;
                SetPhase(SmokeTestPhase.Complete);
                return;
            }
            if (SceneManager.GetActiveScene().name.Contains("Result"))
            {
                if (_resultTimer < 0f)
                {
                    _resultTimer = 0f;
                    Time.timeScale = 1f;
                    var s = GameFlowManager.Instance?.CurrentSession;
                    Timeline($"PvP RESULT scene loaded (result={s?.result} rounds={s?.playerRoundsWon}-{s?.enemyRoundsWon} kills={s?.playerKills} dealt={s?.damageDealt} taken={s?.damageTaken})");
                    LogProgression("after PvP");
                }
                _resultTimer += Time.unscaledDeltaTime;
                if (_resultTimer > 1.5f)
                {
                    _pvpCompleted = true;
                    StartCoroutine(TakeScreenshotDelayed("pvp_result_screen"));
                    SetPhase(SmokeTestPhase.Complete);
                }
                return;
            }
            var pvp = FindFirstObjectByType<PvPMatchManager>();
            HookPvP(pvp);
            if (_matchTimer - _lastScreenshotTime >= 20f)
            {
                _lastScreenshotTime = _matchTimer;
                LogMatchState("PvP");
            }
        }

        private void UpdatePvPMatch()
        {
            if (_fullPvP)
            {
                UpdateFullPvP();
                return;
            }
            _matchTimer += Time.deltaTime;
            _autopilotTimer += Time.deltaTime;
            
            HookPvP(FindFirstObjectByType<PvPMatchManager>());
            if (_hookedPvp == null) RunAutopilot();
            
            if (_matchTimer - _lastScreenshotTime >= 5f)
            {
                _lastScreenshotTime = _matchTimer;
                LogMatchState("PvP");
                StartCoroutine(TakeScreenshotDelayed($"pvp_match_{Mathf.FloorToInt(_matchTimer)}s"));
            }

            if (_matchTimer >= 15f)
            {
                Debug.Log("[SmokeTest] PvP match time complete (15s). Test finished.");
                _pvpCompleted = true;
                SetPhase(SmokeTestPhase.Complete);
            }
        }

        private void LogMatchState(string matchType)
        {
            Debug.Log($"[SmokeTest] === {matchType} Match State at {_matchTimer:F1}s ===");
            
            var camera = Camera.main;
            if (camera != null)
            {
                Debug.Log($"[SmokeTest] Camera: pos={camera.transform.position}, rect={camera.rect}, orthoSize={camera.orthographicSize}");
            }
            else
            {
                Debug.LogWarning("[SmokeTest] WARNING: No main camera found!");
            }

            int playerCount = 0;
            int enemyCount = 0;
            int coverCount = 0;

            var pveManager = FindFirstObjectByType<PvEMatchManager>();
            if (pveManager != null)
            {
                var playerTeam = pveManager.PlayerTeam;
                playerCount = playerTeam?.Count ?? 0;
                
                if (playerTeam != null)
                {
                    foreach (var p in playerTeam)
                    {
                        if (p != null)
                        {
                            Debug.Log($"[SmokeTest] Player: {p.Data?.id}, pos={p.Position}, state={p.OverallState}, hp={p.CurrentHp}");
                        }
                    }
                }

                var enemies = pveManager.ActiveEnemies;
                enemyCount = enemies?.Count ?? 0;
                
                if (enemies != null)
                {
                    foreach (var e in enemies)
                    {
                        if (e != null)
                        {
                            Debug.Log($"[SmokeTest] Enemy: {e.Data?.id}, pos={e.Position}, alive={e.IsAlive}, hp={e.CurrentHp}, entering={e.IsEntering}");
                        }
                    }
                }

                var boss = pveManager.ActiveBoss;
                if (boss != null)
                {
                    Debug.Log($"[SmokeTest] Boss: {boss.Data?.id}, pos={boss.Position}, hp={boss.CurrentHp}/{boss.MaxHp}");
                }
            }

            var pvpManager = FindFirstObjectByType<PvPMatchManager>();
            if (pvpManager != null)
            {
                var playerTeam = pvpManager.PlayerTeam;
                playerCount = playerTeam?.Count ?? 0;
                
                if (playerTeam != null)
                {
                    foreach (var p in playerTeam)
                    {
                        if (p != null)
                        {
                            Debug.Log($"[SmokeTest] PlayerTeam: {p.Data?.id}, pos={p.Position}, state={p.OverallState}, hp={p.CurrentHp}");
                        }
                    }
                }

                var enemyTeam = pvpManager.EnemyTeam;
                enemyCount = enemyTeam?.Count ?? 0;
                
                if (enemyTeam != null)
                {
                    foreach (var e in enemyTeam)
                    {
                        if (e != null)
                        {
                            Debug.Log($"[SmokeTest] EnemyTeam: {e.Data?.id}, pos={e.Position}, state={e.OverallState}, hp={e.CurrentHp}");
                        }
                    }
                }
            }

            var characters = FindObjectsByType<CharacterStateMachine>(FindObjectsSortMode.None);
            var enemyViews = FindObjectsByType<EnemyView>(FindObjectsSortMode.None);
            var bossViews = FindObjectsByType<BossView>(FindObjectsSortMode.None);

            Debug.Log($"[SmokeTest] Found GameObjects: CharacterStateMachine={characters.Length}, EnemyView={enemyViews.Length}, BossView={bossViews.Length}");

            if (pveManager != null)
            {
                Debug.Log($"[SmokeTest] PvE progress: wave={pveManager.CurrentWave + 1}/{pveManager.TotalWaves} gauge={pveManager.CurrentGauge}/{pveManager.GaugeMax} enemyDamage={pveManager.TotalEnemyDamage} kills={pveManager.EnemiesDefeated} playerDamageTaken={pveManager.PlayerDamageTaken}");
            }
            if (pvpManager != null)
            {
                Debug.Log($"[SmokeTest] PvP progress: round={pvpManager.CurrentRound} phase={pvpManager.CurrentPhase} score={pvpManager.PlayerRoundsWon}-{pvpManager.EnemyRoundsWon} dmgToEnemy={pvpManager.TotalDamageToEnemyTeam} dmgToPlayer={pvpManager.TotalDamageToPlayerTeam}");
            }

            LogViews();

            if (playerCount == 0)
            {
                Debug.LogWarning("[SmokeTest] WARNING: Player count is 0!");
            }
            if (matchType == "PvE" && enemyCount == 0 && pveManager?.ActiveBoss == null)
            {
                Debug.LogWarning("[SmokeTest] WARNING: No enemies or boss spawned in PvE!");
            }
            if (matchType == "PvP" && enemyCount == 0)
            {
                Debug.LogWarning("[SmokeTest] WARNING: Enemy team count is 0 in PvP!");
            }
        }

        private void RunAutopilot()
        {
            CharacterStateMachine me = null;
            Vector2? target = null;

            var pve = FindFirstObjectByType<PvEMatchManager>();
            var pvp = FindFirstObjectByType<PvPMatchManager>();

            if (pve != null && pve.PlayerTeam != null && pve.PlayerTeam.Count > 0)
            {
                me = pve.PlayerTeam[Mathf.Clamp(pve.PlayerControlledIndex, 0, pve.PlayerTeam.Count - 1)];
                if (me != null) target = pve.FindTargetFor(me.Position, 40f);
            }
            else if (pvp != null && pvp.PlayerTeam != null && pvp.PlayerTeam.Count > 0)
            {
                me = pvp.PlayerTeam[0];
                if (me != null && pvp.EnemyTeam != null)
                {
                    float best = float.MaxValue;
                    foreach (var e in pvp.EnemyTeam)
                    {
                        if (e == null || e.OverallState != CharacterOverallState.Active) continue;
                        float d = Vector2.Distance(me.Position, e.Position);
                        if (d < best) { best = d; target = e.Position; }
                    }
                }
            }

            if (me == null) return;

            if (_gunnerOwner != me)
            {
                _gunnerOwner = me;
                _gunner = new AutoGunner(me, 0.3f);
                _advancedRow = false;
            }

            if (pve != null && me != null && _fullPvE)
            {
                float range = me.Data?.weapon?.range ?? 7f;
                Vector2? inRange = pve.FindTargetFor(me.Position, range * 1.1f);
                if (_pilot == null || _pilot.Character != me)
                    _pilot = new AbilityPilot(me, RuntimeAssetManager.Instance?.BotBehavior, 1f);
                bool pilotMoving = me.OverallState == CharacterOverallState.Active && pve.TickPilot(_pilot, Time.deltaTime, inRange);
                if (!pilotMoving && _autopilotTimer > 0.5f && me.OverallState == CharacterOverallState.Active) pve.DriveAllyMovement(me, inRange);
                if (!_pilot.IsHolding) _gunner.Tick(Time.deltaTime, inRange ?? target);
                return;
            }

            // Move up to the front row once so short-range weapons can reach the enemy line
            if (!_advancedRow && _autopilotTimer > 0.5f)
            {
                if (me.CurrentRowIndex >= 2) _advancedRow = true;
                else me.OnRowChangeCommand(new RowChangeCommand(1, Time.time));
            }
            else if (me.MovementState != MovementState.RowChanging && me.MovementState != MovementState.Rolling)
            {
                // Strafe gently toward the target's X
                float dx = target.HasValue ? target.Value.x - me.Position.x : Mathf.Sin(_autopilotTimer * 0.7f);
                float mag = Mathf.Abs(dx) > 0.5f ? 0.6f : 0f;
                me.OnMoveCommand(new MoveCommand(new Vector2(Mathf.Sign(dx), 0f), mag, Time.time));
            }

            _gunner.Tick(Time.deltaTime, target);
        }

        private void LogViews()
        {
            var cam = Camera.main;
            Bounds camBounds = FieldView.CameraBounds(cam);
            camBounds.Expand(new Vector3(0f, 0f, 1000f));
            int visible = 0, offCam = 0, noSprite = 0;

            void Check(string kind, string name, SpriteRenderer sr, Vector2 logic)
            {
                if (sr == null)
                {
                    Debug.LogWarning($"[SmokeTest] VIEW WARNING {kind} {name}: no SpriteRenderer");
                    noSprite++;
                    return;
                }
                Bounds b = sr.bounds;
                bool hasSprite = sr.sprite != null;
                bool onCam = camBounds.Intersects(new Bounds(new Vector3(b.center.x, b.center.y, 0f), new Vector3(b.size.x, b.size.y, 1f)));
                Debug.Log($"[SmokeTest] View {kind} {name}: logic={logic} world={sr.transform.position} sprite={(hasSprite ? sr.sprite.name : "NULL")} order={sr.sortingOrder} bounds=({b.min.x:F1},{b.min.y:F1})-({b.max.x:F1},{b.max.y:F1}) enabled={sr.enabled && sr.gameObject.activeInHierarchy} onCam={onCam}");
                if (!hasSprite) { noSprite++; Debug.LogWarning($"[SmokeTest] VIEW WARNING {kind} {name}: sprite is NULL"); }
                if (!onCam) { offCam++; Debug.LogWarning($"[SmokeTest] VIEW WARNING {kind} {name}: OFF CAMERA (cam={camBounds.min}-{camBounds.max})"); }
                if (hasSprite && onCam) visible++;
            }

            foreach (var v in FindObjectsByType<PlaceholderCharacterView>(FindObjectsSortMode.None))
            {
                var c = v.GetComponentInParent<CharacterStateMachine>();
                Check("Character", v.transform.parent != null ? v.transform.parent.name : v.name, v.BodyRenderer, c != null ? c.Position : Vector2.zero);
            }
            foreach (var v in FindObjectsByType<EnemyView>(FindObjectsSortMode.None))
            {
                if (v.Enemy != null && v.Enemy.IsEntering) { Debug.Log($"[SmokeTest] View Enemy {v.name}: entering at logic={v.Enemy.Position}"); continue; }
                if (v.Enemy != null && v.Enemy.IsFleeing) { Debug.Log($"[SmokeTest] View Enemy {v.name}: defeated/fleeing at logic={v.Enemy.Position}"); continue; }
                Check("Enemy", v.name, v.BodyRenderer, v.Enemy != null ? v.Enemy.Position : Vector2.zero);
            }
            foreach (var v in FindObjectsByType<BossView>(FindObjectsSortMode.None))
                Check("Boss", v.name, v.BodyRenderer, Vector2.zero);
            int coverCount = 0;
            foreach (var v in FindObjectsByType<CoverView>(FindObjectsSortMode.None))
            {
                coverCount++;
                Check("Cover", v.name, v.Renderer, v.Cover != null ? v.Cover.Position : Vector2.zero);
            }

            var combat = ServiceLocator.Get<CombatSystem>();
            Debug.Log($"[SmokeTest] Views summary: visible={visible} offCamera={offCam} noSprite={noSprite} covers={coverCount} activeProjectiles={combat?.ProjectilePool?.ActiveCount ?? -1} characters={combat?.Characters?.Count ?? -1}");
        }

        private void CompleteTest()
        {
            LogPerf("final");
            if (_drawRec.Valid) _drawRec.Dispose();
            if (_setPassRec.Valid) _setPassRec.Dispose();
            StartCoroutine(TakeScreenshotDelayed("test_complete"));
            StartCoroutine(QuitAfterDelay());
            _phase = (SmokeTestPhase)(-1);
        }

        private IEnumerator QuitAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            Debug.Log("[SmokeTest] Smoke test completed successfully!");
            Debug.Log($"[SmokeTest] Screenshots saved to: {_screenshotFolder}");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private IEnumerator TakeScreenshotDelayed(string name)
        {
            yield return new WaitForEndOfFrame();
            
            string filename = $"smoketest_{_screenshotIndex:D3}_{name}.png";
            string fullPath = Path.Combine(_screenshotFolder, filename);
            
            _shotFrame = Time.frameCount;
            ScreenCapture.CaptureScreenshot(fullPath);
            _screenshotIndex++;
            
            Debug.Log($"[SmokeTest] Screenshot saved: {fullPath}");
        }
    }
}

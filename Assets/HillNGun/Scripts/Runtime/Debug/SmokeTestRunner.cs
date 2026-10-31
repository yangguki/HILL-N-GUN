using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.Input;
using HillNGun.PvE;
using HillNGun.PvP;
using HillNGun.Characters;
using System;
using System.IO;
using System.Collections;

namespace HillNGun.DebugTools
{
    public class SmokeTestRunner : MonoBehaviour
    {
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
                    break;
                }
            }

            if (_isSmokeTest)
            {
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
        }

        private void Update()
        {
            if (!_isSmokeTest) return;

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

        private void UpdateModeSelect()
        {
            _autopilotTimer += Time.deltaTime;
            
            if (!_phaseScreenshotTaken && _autopilotTimer > 0.5f)
            {
                _phaseScreenshotTaken = true;
                StartCoroutine(TakeScreenshotDelayed("mode_select"));
            }
            
            if (_autopilotTimer > 2f)
            {
                Debug.Log("[SmokeTest] Selecting PvE mode");
                GameFlowManager.Instance?.GoToCharacterSelect(GameMode.PvE);
                SetPhase(SmokeTestPhase.CharacterSelect);
            }
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
            }
        }

        private void UpdatePvEMatch()
        {
            _matchTimer += Time.deltaTime;
            _autopilotTimer += Time.deltaTime;
            
            RunAutopilot();
            
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
                    flowManager.StartMatch();
                }
                SetPhase(SmokeTestPhase.PvPMatch);
                _matchTimer = 0f;
                _lastScreenshotTime = -5f;
            }
        }

        private void UpdatePvPMatch()
        {
            _matchTimer += Time.deltaTime;
            _autopilotTimer += Time.deltaTime;
            
            RunAutopilot();
            
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
                            Debug.Log($"[SmokeTest] Player: {p.Data?.id}, pos={p.Position}, state={p.OverallState}");
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
                            Debug.Log($"[SmokeTest] Enemy: {e.Data?.id}, pos={e.Position}, alive={e.IsAlive}");
                        }
                    }
                }

                var boss = pveManager.ActiveBoss;
                if (boss != null)
                {
                    Debug.Log($"[SmokeTest] Boss: {boss.Data?.id}, pos={boss.Position}, hp={boss.CurrentHp}/{boss.Data?.maxHp}");
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
                            Debug.Log($"[SmokeTest] PlayerTeam: {p.Data?.id}, pos={p.Position}, state={p.OverallState}");
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
                            Debug.Log($"[SmokeTest] EnemyTeam: {e.Data?.id}, pos={e.Position}, state={e.OverallState}");
                        }
                    }
                }
            }

            var characters = FindObjectsByType<CharacterStateMachine>(FindObjectsSortMode.None);
            var enemyViews = FindObjectsByType<EnemyView>(FindObjectsSortMode.None);
            var bossViews = FindObjectsByType<BossView>(FindObjectsSortMode.None);

            Debug.Log($"[SmokeTest] Found GameObjects: CharacterStateMachine={characters.Length}, EnemyView={enemyViews.Length}, BossView={bossViews.Length}");

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
            var inputHandler = FindFirstObjectByType<TouchInputHandler>();
            if (inputHandler == null) return;

            float moveAngle = _autopilotTimer * 0.5f;
            Vector2 moveDir = new Vector2(Mathf.Cos(moveAngle), Mathf.Sin(moveAngle) * 0.3f);
            
            inputHandler.InjectMoveCommand(moveDir, 0.7f);
            
            if (Time.frameCount % 60 == 0)
            {
                Vector2 aimDir = new Vector2(1f, Mathf.Sin(_autopilotTimer) * 0.5f).normalized;
                inputHandler.InjectFireCommand(aimDir);
            }
        }

        private void CompleteTest()
        {
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
            
            ScreenCapture.CaptureScreenshot(fullPath);
            _screenshotIndex++;
            
            Debug.Log($"[SmokeTest] Screenshot saved: {fullPath}");
        }
    }
}

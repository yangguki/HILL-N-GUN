using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.Input;
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
            if (!Directory.Exists(_screenshotFolder))
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
                _phase = SmokeTestPhase.Title;
                StartCoroutine(TakeScreenshotDelayed("title_screen"));
            }
        }

        private void UpdateTitle()
        {
            _autopilotTimer += Time.deltaTime;
            if (_autopilotTimer > 2f)
            {
                Debug.Log("[SmokeTest] Advancing from Title to ModeSelect");
                GameFlowManager.Instance?.GoToModeSelect();
                _phase = SmokeTestPhase.ModeSelect;
                _autopilotTimer = 0f;
            }
        }

        private void UpdateModeSelect()
        {
            _autopilotTimer += Time.deltaTime;
            if (_autopilotTimer > 0.5f)
            {
                StartCoroutine(TakeScreenshotDelayed("mode_select"));
            }
            if (_autopilotTimer > 2f)
            {
                Debug.Log("[SmokeTest] Selecting PvE mode");
                GameFlowManager.Instance?.GoToCharacterSelect(GameMode.PvE);
                _phase = SmokeTestPhase.CharacterSelect;
                _autopilotTimer = 0f;
            }
        }

        private void UpdateCharacterSelect()
        {
            _autopilotTimer += Time.deltaTime;
            if (_autopilotTimer > 0.5f && _autopilotTimer < 1f)
            {
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
                _phase = SmokeTestPhase.PvEMatch;
                _autopilotTimer = 0f;
                _matchTimer = 0f;
                _lastScreenshotTime = 0f;
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
                StartCoroutine(TakeScreenshotDelayed($"pve_match_{Mathf.FloorToInt(_matchTimer)}s"));
            }

            if (_matchTimer >= 20f)
            {
                Debug.Log("[SmokeTest] PvE match time complete (20s). Moving to PvP.");
                _pveCompleted = true;
                GameFlowManager.Instance?.GoToModeSelect();
                _phase = SmokeTestPhase.PvPModeSelect;
                _autopilotTimer = 0f;
            }
        }

        private void UpdatePvPModeSelect()
        {
            _autopilotTimer += Time.deltaTime;
            if (_autopilotTimer > 2f)
            {
                Debug.Log("[SmokeTest] Selecting PvP mode");
                GameFlowManager.Instance?.GoToCharacterSelect(GameMode.PvP);
                _phase = SmokeTestPhase.PvPCharacterSelect;
                _autopilotTimer = 0f;
            }
        }

        private void UpdatePvPCharacterSelect()
        {
            _autopilotTimer += Time.deltaTime;
            if (_autopilotTimer > 0.5f && _autopilotTimer < 1f)
            {
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
                _phase = SmokeTestPhase.PvPMatch;
                _autopilotTimer = 0f;
                _matchTimer = 0f;
                _lastScreenshotTime = 0f;
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
                StartCoroutine(TakeScreenshotDelayed($"pvp_match_{Mathf.FloorToInt(_matchTimer)}s"));
            }

            if (_matchTimer >= 15f)
            {
                Debug.Log("[SmokeTest] PvP match time complete (15s). Test finished.");
                _pvpCompleted = true;
                _phase = SmokeTestPhase.Complete;
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

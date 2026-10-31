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
        private AutoGunner _gunner;
        private CharacterStateMachine _gunnerOwner;
        private bool _advancedRow;
        
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
            _gunner = null;
            _gunnerOwner = null;
            _advancedRow = false;
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
                Check("Character", v.transform.parent != null ? v.transform.parent.name : v.name, v.GetComponent<SpriteRenderer>(), c != null ? c.Position : Vector2.zero);
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

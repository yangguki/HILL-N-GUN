using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using HillNGun.Battlefield;
using HillNGun.Characters;
using HillNGun.Combat;
using HillNGun.Core;
using HillNGun.Cover;
using HillNGun.Data;
using HillNGun.Input;
using HillNGun.PvE;

namespace HillNGun.Proto.TopDown
{
    /// <summary>
    /// Editor prototype. Drives the existing FreeMove / combat / enemy pieces and only swaps views.
    /// Cover blocks both teams: a runtime clone of the match rules sets ownCoverBlocksOwnShots, because the
    /// shared arena has no "our side" the way the row-lane field does. The asset on disk is not modified.
    /// </summary>
    public class ProtoTopDownMatch : MonoBehaviour, IProtoBoard
    {
        public CharacterData Buster;
        public EnemyData Pistol;
        public CoverData Rock;
        public CoverData Hay;
        public CoverData Crate;
        public CoverData Barrel;
        public MatchRulesData Rules;
        public MovementFeelData MoveFeel;
        public FeelData Feel;
        public DownedData Downed;
        public Sprite SpriteDown;
        public Sprite SpriteUp;
        public Sprite SpriteSide;
        public ProtoPixelArt PixelArt;

        public CharacterStateMachine Player => _player;
        public CombatSystem Combat => _combat;
        public ProtoEnemyDriver Driver => _driver;
        public IReadOnlyList<CoverInstance> Covers => _covers;
        public Vector2 AimDirection => _capture ? _demoAim : (_controls != null ? _controls.Facing : Vector2.up);
        public bool ShowGrenadeAim => _capture ? _demoBomb : (_controls != null && _controls.GrenadePreview);
        public Vector2 GrenadeAimPoint => _capture ? _demoGrenade : (_controls != null ? _controls.GrenadeTarget : Vector2.zero);
        public IReadOnlyList<EnemyInstance> Enemies => _driver != null ? _driver.Enemies : System.Array.Empty<EnemyInstance>();
        public IReadOnlyList<CharacterStateMachine> Fighters => null;
        public float DividerX => float.NaN;
        public float DividerY => float.NaN;
        public float DividerMinX => 0f;
        public float DividerMaxX => 0f;
        public float DividerMinY => 0f;
        public float DividerMaxY => 0f;
        public static bool CaptureFinished { get; private set; }

#if UNITY_EDITOR
        /// <summary>Smoke-only: editor driver sets this so autoplay runs while ProtoControls stays enabled (HUD visible).</summary>
        public bool SmokeAutoplay { get; set; }
        /// <summary>Smoke-only: freeze wave spawn/advance during the move8 facing capture.</summary>
        public bool SmokePauseWaves { get; set; }
        public bool SmokeMatchEnded => _ended;
        public string SmokeResultLabel => _result;
        public float MatchClock => _clock;
#endif

        readonly List<CoverInstance> _covers = new List<CoverInstance>(12);
        CharacterStateMachine _player;
        CombatSystem _combat;
        FreeMoveWorld _world;
        ProtoEnemyDriver _driver;
        ProtoControls _controls;
        ProtoPresentation _views;
        HillNGun.UI.ScalePresetHud _scaleHud;
        MatchRulesData _rulesClone;
        bool _capture;
        bool _ended;
        bool _pixel = true;
        string _result = "";
        float _clock;
        Vector2 _demoAim = Vector2.up;
        Vector2 _demoGrenade;
        bool _demoFire, _demoBomb, _demoRoll;
        bool _shotPixel, _shotDio;
        bool _perfPixel, _perfDio;
        float _frameAcc, _frameMax;
        int _frameN;
        GUIStyle _label, _big;
#if UNITY_EDITOR
        bool _debugHud = true;
#endif

        void Awake()
        {
#if !UNITY_EDITOR
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
#endif
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-proto-topdown-capture") _capture = true;
            CaptureFinished = false;
            if (_capture) Time.captureFramerate = 24;
            GameTime.IsPaused = false;
            GameTime.ResetTimeScale();
            if (Buster == null || Pistol == null || Rules == null || MoveFeel == null)
            {
                Debug.LogError("[ProtoTopDown] scene is missing data refs. Run HillNGun/Proto/Build TopDown Scene.");
                return;
            }

            if (GetComponent<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            ServiceLocator.Register(MoveFeel);
            if (Downed != null) ServiceLocator.Register(Downed);

            _rulesClone = Instantiate(Rules);
            _rulesClone.ownCoverBlocksOwnShots = true;

            var layout = ScriptableObject.CreateInstance<StreetLayoutData>();
            layout.id = "proto_topdown";
            layout.walkPolygon = ProtoArena.WalkPolygon();
            layout.enemyPostZone = ProtoArena.PostZone;
            layout.enemyPostEdgeMargin = ProtoArena.PostMargin;
            layout.spawnPoints = new[] { ProtoArena.Door() };
            _world = new FreeMoveWorld(layout);
            _world.CoverSource = () => _covers;
            ServiceLocator.Register(_world);

            var combatGo = new GameObject("Combat");
            _combat = combatGo.AddComponent<CombatSystem>();
            SetField(_combat, "_matchRules", _rulesClone);
            SetField(_combat, "_feelData", Feel);

            BuildCovers();

            var hero = new GameObject("Buster");
            _player = hero.AddComponent<CharacterStateMachine>();
            ResetPlayer(_player, Buster);
            _combat.RegisterCharacter(_player);

            _driver = new ProtoEnemyDriver(_world, layout, _combat, _rulesClone, Pistol, () => _player.Position);

            _views = gameObject.AddComponent<ProtoPresentation>();
            _views.Init(this, SpriteDown, SpriteUp, SpriteSide, ResolvePixelArt());

            _controls = gameObject.AddComponent<ProtoControls>();
            _controls.Bind(_player, Buster.grenade, PointerLogic);
            _controls.BuildUi(ToggleSkin, Restart, true, null, null, ProtoNav.OpenMenu);
            _controls.enabled = !_capture;
#if UNITY_EDITOR
            if (HasArg("-proto-v3-smoke")) _debugHud = false;
#endif
        }

        ProtoPixelArt ResolvePixelArt()
        {
            if (PixelArt != null) return PixelArt;
#if UNITY_EDITOR
            PixelArt = UnityEditor.AssetDatabase.LoadAssetAtPath<ProtoPixelArt>(ProtoAssets.PixelArtTopDown);
#endif
            return PixelArt;
        }

#if UNITY_EDITOR
        public void SetDebugHud(bool on) => _debugHud = on;

        static bool HasArg(string arg)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++) if (args[i] == arg) return true;
            return false;
        }
#endif

        public static void ResetPlayer(CharacterStateMachine player, CharacterData data)
        {
            if (player == null || data == null) return;
            player.Initialize(data, Team.Player, 0, ProtoArena.PlayerSpawn, 0);
            player.SetMoveMode(MoveMode.FreeMove);
        }

        public void Restart()
        {
            if (_capture) return;
            SceneManager.LoadScene(gameObject.scene.name);
        }

        void Start()
        {
            var hud = FindFirstObjectByType<HillNGun.UI.ScalePresetHud>();
            if (hud != null) { _scaleHud = hud; _scaleHud.enabled = false; }
            if (_capture && _player != null) StartCoroutine(CaptureRoutine());
        }

        void OnDestroy()
        {
            if (_scaleHud != null) _scaleHud.enabled = true;
            if (_world != null && FreeMoveWorld.Active == _world) ServiceLocator.Unregister<FreeMoveWorld>();
        }

        void Update()
        {
            if (_player == null || _driver == null) return;
            float sim = GameTime.DeltaTime;
            if (_capture)
            {
                float step = Time.deltaTime;
                if (step > 0.05f) step = 1f / 24f;
                _clock += step;
            }
            else _clock += sim;
            if (_capture) TickDemo();
#if UNITY_EDITOR
            bool pauseWaves = SmokePauseWaves;
#else
            bool pauseWaves = false;
#endif
            if ((!_ended || _capture) && !pauseWaves) _driver.Tick(sim, Time.time, _player.Position);
            if (!_ended)
            {
                if (_player.OverallState == CharacterOverallState.Eliminated) { _ended = true; _result = "패배"; }
                else if (_driver.AllWavesCleared) { _ended = true; _result = "클리어"; }
            }
            if (_controls != null && !_capture)
            {
#if UNITY_EDITOR
                // Smoke autoplay drives the character directly; keep the component enabled so overlay HUD stays up.
                _controls.Blocked = _ended || SmokeAutoplay;
#else
                _controls.Blocked = _ended;
#endif
                _controls.SetHud(_player.CurrentHp, _player.MaxHp, _clock);
                _controls.SetEnded(_ended ? _result : null);
            }
#if UNITY_EDITOR
            var keys = UnityEngine.InputSystem.Keyboard.current;
            if (keys != null && keys.f2Key.wasPressedThisFrame) _debugHud = !_debugHud;
#endif
        }

        public void ToggleSkin()
        {
            if (_player == null) return;
            var before = ProtoLogicStamp.Capture(_player, _driver.Enemies);
            _pixel = !_pixel;
            _views.SetPixel(_pixel);
            var after = ProtoLogicStamp.Capture(_player, _driver.Enemies);
            if (!before.Same(after))
                Debug.LogError("[ProtoTopDown] skin toggle changed logic" + before.Diff(after));
            else
                Debug.Log("[ProtoTopDown] skin=" + (_pixel ? "pixel" : "diorama") + " logic unchanged");
        }

        Vector2 PointerLogic()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || _views == null || _views.ActiveCamera == null) return _player.Position;
            return ScreenToLogic(_views.ActiveCamera, mouse.position.ReadValue(), _views.PixelOn);
        }

        public static Vector2 ScreenToLogic(Camera cam, Vector2 screen, bool pixel)
        {
            var ray = cam.ScreenPointToRay(screen);
            if (pixel)
            {
                if (Mathf.Abs(ray.direction.z) < 1e-5f) return Vector2.zero;
                float t = -ray.origin.z / ray.direction.z;
                return ProtoArena.FromPixel(ray.origin + ray.direction * t);
            }
            if (Mathf.Abs(ray.direction.y) < 1e-5f) return Vector2.zero;
            float td = -ray.origin.y / ray.direction.y;
            var p = ray.origin + ray.direction * td;
            return new Vector2(p.x, p.z);
        }

        void BuildCovers()
        {
            var pieces = ProtoArena.Pieces();
            for (int i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                var src = piece.Kind == "brick" ? Rock : piece.Kind == "sandbag" ? Hay : piece.Kind == "barrel" ? Barrel : Crate;
                if (src == null) continue;
                var data = src;
                if (piece.Sized)
                {
                    data = Instantiate(src);
                    data.width = piece.Width;
                    data.depth = piece.Depth;
                }
                var cover = new CoverInstance();
                cover.InstanceId = "proto_" + i;
                cover.Initialize(data, piece.Center, 0, Team.Player, new CoverSize
                {
                    logicWidth = data.width,
                    visualWidth = data.width,
                    visualHeight = -1f
                });
                _covers.Add(cover);
                _combat.RegisterCover(cover);
            }
        }

        void TickDemo()
        {
            float t = _clock;
            Vector2 move = Vector2.zero;
            _demoAim = Vector2.up;
            bool fire = false, bomb = false, roll = false;
            if (t < 1.7f) { move = new Vector2(0.15f, 1f); _demoAim = move.normalized; }
            else if (t < 3.4f)
            {
                _demoAim = Vector2.up;
                fire = (t > 2.05f && t < 2.16f) || (t > 2.9f && t < 3.02f);
            }
            else if (t < 4.6f)
            {
                move = Vector2.left;
                _demoAim = new Vector2(0.2f, 1f);
                bomb = t > 3.55f && t < 4.3f;
            }
            else if (t < 6.8f)
            {
                move = new Vector2(-0.35f, -1f);
                _demoAim = Vector2.up;
                roll = t > 5.15f && t < 5.28f;
                fire = t > 6.05f && t < 6.18f;
            }
            else
            {
                move = new Vector2(0.9f, 0.1f);
                _demoAim = Vector2.up;
                fire = t > 7.3f && t < 7.42f;
            }

            float mag = Mathf.Clamp01(move.magnitude);
            var dir = mag > 0.01f ? move.normalized : Vector2.zero;
            _player.OnMoveCommand(new MoveCommand(dir, mag, Time.time));
            if (fire && !_demoFire) FireOnce(_demoAim);
            _demoFire = fire;
            _demoGrenade = ClampThrow(_player.Position + _demoAim * ThrowDistance());
            if (bomb && !_demoBomb)
                _player.OnGrenadeAimStartCommand(new GrenadeAimStartCommand(Vector2.zero, Time.time));
            if (!bomb && _demoBomb)
                _player.OnGrenadeReleaseCommand(new GrenadeReleaseCommand(_demoGrenade, true, Time.time));
            _demoBomb = bomb;
            if (roll && !_demoRoll)
                _player.OnRollCommand(new RollCommand(dir.sqrMagnitude > 0.01f ? dir : _demoAim, Time.time));
            _demoRoll = roll;
        }

        void FireOnce(Vector2 aim)
        {
            float now = Time.time;
            _player.OnAimStartCommand(new AimStartCommand(Vector2.zero, now));
            _player.OnAimDragCommand(new AimDragCommand(aim, 1f, true, now));
            _player.OnAimReleaseCommand(new AimReleaseCommand(aim, true, now));
        }

        float ThrowDistance()
        {
            var g = Buster != null ? Buster.grenade : null;
            return g != null ? Mathf.Lerp(g.minRange, g.maxRange, 0.45f) : 5f;
        }

        static Vector2 ClampThrow(Vector2 p)
        {
            return new Vector2(Mathf.Clamp(p.x, ProtoArena.MinX + 0.4f, ProtoArena.MaxX - 0.4f),
                Mathf.Clamp(p.y, ProtoArena.MinY + 0.4f, ProtoArena.MaxY - 0.4f));
        }

        IEnumerator CaptureRoutine()
        {
            yield return null;
            var dir = ProtoAssets.OutputDir;
            Directory.CreateDirectory(dir);
            var pixelDir = Path.Combine(dir, "frames_pixel");
            var dioDir = Path.Combine(dir, "frames_diorama");
            Directory.CreateDirectory(pixelDir);
            Directory.CreateDirectory(dioDir);
            var rt = new RenderTexture(1280, 720, 24);
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            _views.FitTo(rt);
            var perfPath = Path.Combine(dir, "perf.txt");
            if (File.Exists(perfPath)) File.Delete(perfPath);
            int pixelFrames = 0, dioFrames = 0, spins = 0;
            float grabStep = 0f;
            while (_clock < 8.7f && spins < 8000)
            {
                yield return null;
                spins++;
                grabStep += Time.deltaTime;
                if (_clock >= 4.6f && _pixel) ToggleSkin();
                bool pixelPhase = _clock < 4.6f;
                bool record = (_clock >= 1.05f && _clock <= 4.15f) || (_clock >= 5.05f && _clock <= 8.15f);
                bool shot = (!_shotPixel && _clock >= 3.2f) || (!_shotDio && !pixelPhase && _clock >= 6.6f);
                bool grab = shot || (record && grabStep >= 1f / 24f);
                if (record && grabStep >= 1f / 24f) grabStep -= 1f / 24f;
                if (record)
                {
                    float ms = Time.unscaledDeltaTime * 1000f;
                    _frameAcc += ms;
                    _frameN++;
                    if (ms > _frameMax) _frameMax = ms;
                }
                if (grab)
                {
                    var cam = _views.ActiveCamera;
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    tex.Apply();
                    cam.targetTexture = null;
                    RenderTexture.active = null;
                    if (shot && pixelPhase && !_shotPixel)
                    {
                        File.WriteAllBytes(Path.Combine(dir, "pixel.png"), tex.EncodeToPNG());
                        _shotPixel = true;
                        Debug.Log("[ProtoTopDown] SHOT pixel");
                    }
                    else if (shot && !pixelPhase && !_shotDio)
                    {
                        File.WriteAllBytes(Path.Combine(dir, "diorama.png"), tex.EncodeToPNG());
                        _shotDio = true;
                        Debug.Log("[ProtoTopDown] SHOT diorama");
                    }
                    if (record && pixelPhase)
                        File.WriteAllBytes(Path.Combine(pixelDir, $"f_{pixelFrames++:D4}.jpg"), tex.EncodeToJPG(80));
                    else if (record && !pixelPhase)
                        File.WriteAllBytes(Path.Combine(dioDir, $"f_{dioFrames++:D4}.jpg"), tex.EncodeToJPG(80));
                }
                if (!_perfPixel && _clock >= 3.4f && pixelPhase) { RequestDrawCalls("pixel"); _perfPixel = true; }
                if (_clock >= 4.2f && _clock < 5f && _frameN > 0) FlushPerf("pixel");
                if (!_perfDio && _clock >= 6.8f) { RequestDrawCalls("diorama"); _perfDio = true; }
                if (_clock >= 8.2f && _frameN > 0) FlushPerf("diorama");
            }
            if (_frameN > 0) FlushPerf("diorama");
            File.WriteAllText(Path.Combine(dir, "frames.txt"), $"pixel={pixelFrames}\ndiorama={dioFrames}\nclock={_clock:F2}\nspins={spins}\n");
            File.WriteAllText(Path.Combine(dir, "capture.done"), "ok");
            CaptureFinished = true;
            Debug.Log($"[ProtoTopDown] CAPTURE done pixelFrames={pixelFrames} dioramaFrames={dioFrames}");
        }

        void FlushPerf(string skin)
        {
            if (_frameN <= 0) return;
            int renderers = 0;
            var found = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
                if (found[i] != null && found[i].enabled && found[i].gameObject.activeInHierarchy) renderers++;
            var line = $"{skin} frameMsAvg={_frameAcc / _frameN:F2} frameMsMax={_frameMax:F2} samples={_frameN} renderers={renderers}";
            Debug.Log("[ProtoTopDown] PERF " + line);
            Directory.CreateDirectory(ProtoAssets.OutputDir);
            File.AppendAllText(Path.Combine(ProtoAssets.OutputDir, "perf.txt"), line + "\n");
            _frameAcc = 0f;
            _frameMax = 0f;
            _frameN = 0;
        }

        static void RequestDrawCalls(string skin)
        {
            Directory.CreateDirectory(ProtoAssets.OutputDir);
            File.WriteAllText(Path.Combine(ProtoAssets.OutputDir, "perf_request.txt"), skin);
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (!_debugHud || SmokeAutoplay || _player == null) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                _label.normal.textColor = Color.white;
                _big = new GUIStyle(_label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
            }
            float gren = _player.GrenadeSystem != null ? _player.GrenadeSystem.CurrentGrenades : 0;
            float ammo = _player.AmmoGauge != null ? _player.AmmoGauge.CurrentCells : 0;
            float ammoMax = _player.AmmoGauge != null ? _player.AmmoGauge.MaxCells : 0;
            string skin = _pixel ? "픽셀" : "3D";
            GUI.Label(new Rect(24, 18, 900, 36), $"HP {_player.CurrentHp}/{_player.MaxHp}   탄 {ammo:0}/{ammoMax:0}   폭탄 {gren:0}   웨이브 {_driver.WaveNumber}/{_driver.WaveCount}   {_clock:0}s   {skin}", _label);
            if (_player.OverallState == CharacterOverallState.Downed)
                GUI.Label(new Rect(24, 52, 400, 30), "다운", _label);
            if (_ended)
                GUI.Label(new Rect(Screen.width * 0.5f - 200, Screen.height * 0.28f, 400, 80), _result, _big);
            GUI.Label(new Rect(24, Screen.height - 36, 1100, 28), "WASD 이동 · 마우스 조준 · 사격 J · 폭탄 K · 구르기 Space · 스킨 F1 · 디버그 F2   (오토에임 없음)", _label);
        }
#endif

        static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) Debug.LogError("[ProtoTopDown] missing " + name);
            else field.SetValue(target, value);
        }
    }
}

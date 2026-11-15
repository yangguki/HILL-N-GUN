using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using HillNGun.Battlefield;
using HillNGun.Characters;
using HillNGun.Combat;
using HillNGun.Core;
using HillNGun.Cover;
using HillNGun.Data;
using HillNGun.PvE;
using HillNGun.Proto.TopDown;

namespace HillNGun.Proto.Frontline
{
    /// <summary>
    /// Local front-line bout. One shared FreeMove world, a midline obstacle the bullets ignore, and a clamp after knockback.
    /// Split direction is <see cref="SplitAxis"/> (default TopBottom).
    /// </summary>
    public class FrontlineMatch : MonoBehaviour, IProtoBoard
    {
        public CharacterData Buster;
        public CoverData Rock;
        public CoverData Hay;
        public CoverData Crate;
        public MatchRulesData Rules;
        public MovementFeelData MoveFeel;
        public FeelData Feel;
        public DownedData Downed;
        public Sprite SpriteDown;
        public Sprite SpriteUp;
        public Sprite SpriteSide;
        public ProtoPixelArt PixelArt;

        [Tooltip("Default TopBottom (player bottom / enemy top). LeftRight keeps the older bank layout.")]
        public FrontlineSplitAxis SplitAxis = FrontlineSplitAxis.TopBottom;

        public CharacterStateMachine Player => _fighters.Count > 0 ? _fighters[0] : null;
        public CombatSystem Combat => _combat;
        public IReadOnlyList<CoverInstance> Covers => _covers;
        public Vector2 AimDirection => _controls != null ? _controls.Facing : (_layout.TowardEnemy(Team.Player));
        public bool ShowGrenadeAim => _controls != null && _controls.GrenadePreview;
        public Vector2 GrenadeAimPoint => _controls != null ? _controls.GrenadeTarget : Vector2.zero;
        public IReadOnlyList<EnemyInstance> Enemies => null;
        public IReadOnlyList<CharacterStateMachine> Fighters => _fighters;
        public float DividerX => _layout.IsTopBottom ? float.NaN : _layout.LineCoord;
        public float DividerY => _layout.IsTopBottom ? _layout.LineCoord : float.NaN;
        public float DividerMinX => _layout.MinX;
        public float DividerMaxX => _layout.MaxX;
        public float DividerMinY => _layout.MinY;
        public float DividerMaxY => _layout.MaxY;
        public FrontlineLayout Layout => _layout;
        public static bool AutoFinished { get; private set; }

#if UNITY_EDITOR
        public bool SmokeMatchEnded => _ended;
        public string SmokeResultLabel => _result;
        public float MatchClock => _clock;
        public string SmokeDecisionReason => _smokeReason;
        string _smokeReason = "";
        bool _smokePlayerBot;
#endif

        readonly List<CoverInstance> _covers = new List<CoverInstance>(16);
        readonly List<CharacterStateMachine> _fighters = new List<CharacterStateMachine>(6);
        readonly List<FrontlineBot> _bots = new List<FrontlineBot>(6);
        readonly List<CharacterStateMachine> _foes = new List<CharacterStateMachine>(4);
        readonly HashSet<CoverInstance> _claimed = new HashSet<CoverInstance>();
        CombatSystem _combat;
        FreeMoveWorld _world;
        ProtoControls _controls;
        ProtoPresentation _views;
        HillNGun.UI.ScalePresetHud _scaleHud;
        FrontlineLayout _layout;
        bool _three;
        bool _auto;
        bool _ended;
        bool _advanced;
        float _clock;
        string _result = "";

        public static string OutputDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/proto_frontline"));

        void Awake()
        {
#if !UNITY_EDITOR
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
#endif
            AutoFinished = false;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-proto-frontline-auto") _auto = true;
            if (_auto)
            {
                string stage = ReadFile("auto_stage.txt");
                _three = stage == "3";
                GameTime.TimeScale = 2f;
            }
            else if (ProtoNav.TryTakeFrontlineThree(out bool three)) _three = three;
            else _three = ReadFile("mode.txt") == "3";

            _layout = ResolveLayout();
            GameTime.IsPaused = false;
            if (!_auto) GameTime.ResetTimeScale();
            if (Buster == null || Rules == null || MoveFeel == null)
            {
                Debug.LogError("[Frontline] scene is missing data refs. Run HillNGun/Proto/Build Frontline Scene.");
                return;
            }
            if (GetComponent<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            ServiceLocator.Register(MoveFeel);
            if (Downed != null) ServiceLocator.Register(Downed);

            var rules = Instantiate(Rules);
            rules.ownCoverBlocksOwnShots = true;
            var layoutData = ScriptableObject.CreateInstance<StreetLayoutData>();
            layoutData.id = "proto_frontline";
            layoutData.walkPolygon = _layout.WalkPolygon();
            _world = new FreeMoveWorld(layoutData);
            _world.CoverSource = () => _covers;
            _world.StaticObstacles.Add(_layout.Wall);
            ServiceLocator.Register(_world);

            var combatGo = new GameObject("Combat");
            _combat = combatGo.AddComponent<CombatSystem>();
            SetField(_combat, "_matchRules", rules);
            SetField(_combat, "_feelData", Feel);
            BuildCovers();
            SpawnSide(Team.Player, _three ? 3 : 1);
            SpawnSide(Team.Enemy, _three ? 3 : 1);

            _views = gameObject.AddComponent<ProtoPresentation>();
            // Frame first so Init's scenery background can use walk bounds; aspect re-fit continues in LateUpdate.
            // Frame the full BG plate so dirt UV aligns with walk; edge crop of buildings is OK.
            _views.SetFrame(_layout.PlateMinX - 1.2f, _layout.PlateMaxX + 1.2f, _layout.PlateMinY - 0.9f, _layout.PlateMaxY + 1.0f);
            _views.Init(this, SpriteDown, SpriteUp, SpriteSide, ResolvePixelArt());
            _views.SetPixel(true);

            if (!_auto && Player != null)
            {
                _controls = gameObject.AddComponent<ProtoControls>();
                _controls.Bind(Player, Buster.grenade, PointerLogic);
                // Mode toggle stays in the HUD corner (skin slot); keyboard 1/3 still works.
                _controls.BuildUi(null, Restart, false, _three ? "1v1" : "3v3", ToggleMode, ProtoNav.OpenMenu);
            }
        }

        FrontlineLayout ResolveLayout()
        {
            var art = ResolvePixelArt();
            if (art != null && art.HasDirtUv)
                return FrontlineRules.FromDirtUv(SplitAxis, art.dirtUvMin, art.dirtUvMax);
            return FrontlineRules.For(SplitAxis);
        }

        void Start()
        {
            var hud = FindFirstObjectByType<HillNGun.UI.ScalePresetHud>();
            if (hud != null) { _scaleHud = hud; _scaleHud.enabled = false; }
            if (_views != null)
            {
                float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                _views.Fit(aspect);
            }
            Debug.Log("[Frontline] start " + (_three ? "3v3" : "1v1") + " split=" + SplitAxis + (_auto ? " auto" : ""));
        }

        ProtoPixelArt ResolvePixelArt()
        {
            if (PixelArt != null) return PixelArt;
#if UNITY_EDITOR
            PixelArt = UnityEditor.AssetDatabase.LoadAssetAtPath<ProtoPixelArt>(ProtoAssets.PixelArtFrontline);
#endif
            return PixelArt;
        }

        void OnDestroy()
        {
            if (_scaleHud != null) _scaleHud.enabled = true;
            if (_world != null && FreeMoveWorld.Active == _world) ServiceLocator.Unregister<FreeMoveWorld>();
        }

        void Update()
        {
            if (_fighters.Count == 0) return;
            _clock += GameTime.DeltaTime;
            if (!_ended)
            {
                float now = Time.time;
                RefreshClaims();
                for (int i = 0; i < _bots.Count; i++)
                {
                    _foes.Clear();
                    FillFoes(_bots[i].Self, _foes);
                    _bots[i].Tick(GameTime.DeltaTime, now, _foes, _covers, _claimed);
                }
                var outcome = FrontlineRules.Judge(Snapshot(), _clock >= FrontlineRules.RoundSeconds);
                if (outcome != FrontlineOutcome.Playing) Finish(outcome);
            }
            if (_controls != null)
            {
#if UNITY_EDITOR
                _controls.Blocked = _ended || _smokePlayerBot;
#else
                _controls.Blocked = _ended;
#endif
                _controls.SetStatus(Status());
                _controls.SetEnded(_ended ? _result : null);
                var kb = Keyboard.current;
                if (kb != null && kb[Key.Digit1].wasPressedThisFrame) SetMode(false);
                if (kb != null && kb[Key.Digit3].wasPressedThisFrame) SetMode(true);
            }
        }

        void RefreshClaims()
        {
            _claimed.Clear();
            for (int i = 0; i < _bots.Count; i++)
            {
                var c = _bots[i] != null ? _bots[i].ClaimedCover : null;
                if (c != null && !c.IsDestroyed) _claimed.Add(c);
            }
        }

#if UNITY_EDITOR
        /// <summary>Smoke-only: keep ProtoControls HUD, but drive slot-0 with FrontlineBot (not -proto-frontline-auto).</summary>
        public void EnsureSmokePlayerBot()
        {
            if (_smokePlayerBot || Player == null) return;
            for (int i = 0; i < _bots.Count; i++)
                if (_bots[i] != null && _bots[i].Self == Player) { _smokePlayerBot = true; return; }
            _bots.Add(new FrontlineBot(Player, 0, _layout));
            _smokePlayerBot = true;
            Debug.Log("[Frontline] smoke player bot attached");
        }

        string ClassifySmokeReason()
        {
            var units = Snapshot();
            int playerAlive = 0, enemyAlive = 0;
            float playerRatio = 0f, enemyRatio = 0f;
            for (int i = 0; i < units.Count; i++)
            {
                if (!units[i].Alive) continue;
                if (units[i].Team == Team.Player) { playerAlive++; playerRatio += units[i].HpRatio; }
                else { enemyAlive++; enemyRatio += units[i].HpRatio; }
            }
            bool timeUp = _clock >= FrontlineRules.RoundSeconds;
            if (playerAlive == 0 || enemyAlive == 0) return "wipe-out";
            if (!timeUp) return "incomplete";
            if (playerAlive != enemyAlive) return "timeout-survivors";
            if (Mathf.Abs(playerRatio - enemyRatio) <= 0.0001f) return "timeout-draw";
            return "timeout-hp-ratio";
        }
#endif

        void LateUpdate()
        {
            for (int i = 0; i < _fighters.Count; i++)
            {
                var f = _fighters[i];
                if (f == null || f.OverallState == CharacterOverallState.Eliminated) continue;
                var clamped = _layout.ClampSide(f.Position, f.BodyRadius, f.Team);
                if ((clamped - f.Position).sqrMagnitude > 1e-6f) f.SetPosition(clamped);
            }
        }

        public void Restart()
        {
            if (_auto) return;
            SceneManager.LoadScene(gameObject.scene.name);
        }

        void ToggleMode() => SetMode(!_three);

        void SetMode(bool three)
        {
            if (_auto || three == _three) return;
            Directory.CreateDirectory(OutputDir);
            File.WriteAllText(Path.Combine(OutputDir, "mode.txt"), three ? "3" : "1");
            SceneManager.LoadScene(gameObject.scene.name);
        }

        void Finish(FrontlineOutcome outcome)
        {
            if (_advanced) return;
            _advanced = true;
            _ended = true;
            _result = outcome == FrontlineOutcome.Draw ? "무승부" : outcome == FrontlineOutcome.Player ? "승리" : "패배";
#if UNITY_EDITOR
            _smokeReason = ClassifySmokeReason();
#endif
            Debug.Log("[Frontline] " + (_three ? "3v3" : "1v1") + " " + outcome + " t=" + _clock.ToString("F1"));
            if (!_auto) return;
            Directory.CreateDirectory(OutputDir);
            File.AppendAllText(Path.Combine(OutputDir, "auto.txt"), (_three ? "3v3 " : "1v1 ") + outcome + " t=" + _clock.ToString("F1") + "\n");
            if (!_three)
            {
                File.WriteAllText(Path.Combine(OutputDir, "auto_stage.txt"), "3");
                SceneManager.LoadScene(gameObject.scene.name);
            }
            else
            {
                File.WriteAllText(Path.Combine(OutputDir, "auto.done"), "ok");
                AutoFinished = true;
            }
        }

        string Status()
        {
            int t = Mathf.Max(0, Mathf.FloorToInt(_clock));
            int left = 90 - Mathf.Min(90, t);
            return "아군 " + Sum(Team.Player) + "    " + (left / 60) + ":" + (left % 60).ToString("00") + "    적 " + Sum(Team.Enemy);
        }

        int Sum(Team team)
        {
            int hp = 0;
            for (int i = 0; i < _fighters.Count; i++)
            {
                var f = _fighters[i];
                if (f != null && f.Team == team && f.OverallState != CharacterOverallState.Eliminated) hp += f.CurrentHp;
            }
            return hp;
        }

        List<FrontlineRules.Unit> Snapshot()
        {
            var list = new List<FrontlineRules.Unit>(_fighters.Count);
            for (int i = 0; i < _fighters.Count; i++)
            {
                var f = _fighters[i];
                if (f == null) continue;
                bool alive = f.OverallState != CharacterOverallState.Eliminated;
                list.Add(new FrontlineRules.Unit { Team = f.Team, Alive = alive, HpRatio = alive ? f.HpRatio : 0f });
            }
            return list;
        }

        void FillFoes(CharacterStateMachine self, List<CharacterStateMachine> foes)
        {
            if (self == null) return;
            for (int i = 0; i < _fighters.Count; i++)
            {
                var f = _fighters[i];
                if (f == null || f == self || f.Team == self.Team) continue;
                if (f.OverallState == CharacterOverallState.Eliminated) continue;
                foes.Add(f);
            }
        }

        void SpawnSide(Team team, int count)
        {
            for (int slot = 0; slot < count; slot++)
            {
                var go = new GameObject(team + "_" + slot);
                var actor = go.AddComponent<CharacterStateMachine>();
                actor.Initialize(Buster, team, team == Team.Player ? 0 : 1, _layout.Spawn(team, slot), 0);
                actor.SetMoveMode(MoveMode.FreeMove);
                _combat.RegisterCharacter(actor);
                _fighters.Add(actor);
                bool driven = _auto || !(team == Team.Player && slot == 0);
                if (driven) _bots.Add(new FrontlineBot(actor, slot, _layout));
            }
        }

        void BuildCovers()
        {
            var pieces = _layout.Pieces();
            for (int i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                var src = piece.Kind == "brick" ? Rock : piece.Kind == "sandbag" ? Hay : Crate;
                if (src == null) continue;
                var data = src;
                if (piece.Sized)
                {
                    data = Instantiate(src);
                    data.width = piece.Width;
                    data.depth = piece.Depth;
                }
                var cover = new CoverInstance();
                cover.InstanceId = "front:" + piece.Kind + ":" + i;
                cover.Initialize(data, piece.Center, 0, piece.Team, new CoverSize
                {
                    logicWidth = data.width,
                    visualWidth = data.width,
                    visualHeight = -1f
                });
                _covers.Add(cover);
                _combat.RegisterCover(cover);
            }
        }

        Vector2 PointerLogic()
        {
            var mouse = Mouse.current;
            var cam = _views != null ? _views.ActiveCamera : null;
            if (mouse == null || cam == null || Player == null) return Player != null ? Player.Position : Vector2.zero;
            return ProtoTopDownMatch.ScreenToLogic(cam, mouse.position.ReadValue(), true);
        }

        static string ReadFile(string name)
        {
            string path = Path.Combine(OutputDir, name);
            return File.Exists(path) ? File.ReadAllText(path).Trim() : "";
        }

        static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) Debug.LogError("[Frontline] missing " + name);
            else field.SetValue(target, value);
        }
    }
}

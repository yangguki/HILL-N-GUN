using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    public enum Proto_GreyboxRunResult
    {
        Playing,
        Clear,
        Down
    }

    [ExecuteAlways]
    public class Proto_GreyboxPlayerController : MonoBehaviour
    {
        Proto_GreyboxPlayerMotor _motor;
        Proto_GreyboxRoll _roll;
        Proto_GreyboxCover _cover;
        Proto_GreyboxAim _aim;
        Proto_GreyboxGun _gun;
        Proto_GreyboxGrenade _grenade;
        readonly List<Proto_GreyboxAllyBulletSim> _allyBullets = new List<Proto_GreyboxAllyBulletSim>();
        readonly List<Proto_GreyboxBulletSim> _enemyBullets = new List<Proto_GreyboxBulletSim>();
        readonly List<Proto_GreyboxTarget> _targets = new List<Proto_GreyboxTarget>();
        readonly List<Proto_GreyboxTargetView> _targetViews = new List<Proto_GreyboxTargetView>();
        readonly Dictionary<Proto_GreyboxGrunt, Proto_GreyboxTargetView> _gruntViews =
            new Dictionary<Proto_GreyboxGrunt, Proto_GreyboxTargetView>();

        readonly Proto_GreyboxRuntimeTuning _tuning = new Proto_GreyboxRuntimeTuning();
        readonly Proto_GreyboxWaveDirector _waveDirector = new Proto_GreyboxWaveDirector();
        readonly System.Random _coverRng = new System.Random(77);

        SpriteRenderer _sprite;
        Transform _crosshair;
        Text _hpText;
        Text _scoreText;
        Text _ammoText;
        Text _waveText;
        Image _hpBarFill;
        readonly List<Image> _ammoPips = new List<Image>();
        Image _reloadFill;
        Image _reloadDisc;
        Image _reloadIcon;
        Text _reloadLabel;
        RectTransform _reloadLabelRt;
        const int ReloadLabelFontDefault = 11;
        const int ReloadLabelFontEmpty = 18;
        RectTransform _joystickKnob;
        Transform _aimLine;
        Transform _aimCone;
        Transform _grenadeArcLine;
        Transform _grenadeLandingMarker;
        GameObject _explosionVis;
        readonly Proto_GreyboxResultUi _resultUi = new Proto_GreyboxResultUi();
        readonly Proto_GreyboxPauseUi _pauseUi = new Proto_GreyboxPauseUi();
        readonly Proto_GreyboxSettingsUi _settingsUi = new Proto_GreyboxSettingsUi();
        readonly Proto_GreyboxPlayerHearts _hearts = new Proto_GreyboxPlayerHearts();
        readonly Proto_GreyboxHeartsHud _heartsHud = new Proto_GreyboxHeartsHud();
        readonly Proto_GreyboxRollGaugeHud _rollGaugeHud = new Proto_GreyboxRollGaugeHud();
        Proto_GreyboxRuntimeDebugUi _debugUi;
        bool _settingsFromGear;
        Proto_GreyboxWestPixelSkin _westSkin;
        readonly Proto_GreyboxRollDizzy _rollDizzy = new Proto_GreyboxRollDizzy();
        readonly Proto_GreyboxCylinderHud _cylinderHud = new Proto_GreyboxCylinderHud();
        readonly Proto_GreyboxGrenadeGaugeHud _grenadeGaugeHud = new Proto_GreyboxGrenadeGaugeHud();
        bool _grenadeBlockedTapPrev;
        bool _wasRolling;
        Transform _aimTipRing;
        readonly List<Transform> _grenadeArcDots = new List<Transform>();
        Transform _grenadeFlyVis;
        Transform _grenadeFlyShadow;
        Transform _grenadeFlySmoke;
        readonly List<Transform> _dizzyStars = new List<Transform>();

        Color _baseColor;
        int _hp;
        int _score;
        bool _rollPressedPrev;
        bool _reloadPressedPrev;
        float _simTime;
        bool _fireStickTracking;
        Vector2 _fireStickStartScreen;
        int _fireStickFingerId = -2;
        int _grenadeStickFingerId = -2;
        bool _grenadeStickTracking;
        Vector2 _grenadeStickStartScreen;
        float _grenadePeakScreenMag;
        bool _grenadeStickActive;
        Vector2 _grenadePreviewLanding;
        int _joyFingerId = -2;
        Proto_GreyboxBulletSim _debugBullet;
        bool _mouseAimTracking;
        Vector2 _mousePressWorld;
        Vector2? _aimOverride;
        Transform _allyBulletRoot;
        Transform _enemyBulletRoot;
        Transform _gruntRoot;
        readonly List<SpriteRenderer> _allyBulletViews = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _allyTracerViews = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _allyBulletHotspotViews = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _enemyBulletViews = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _enemyBulletGlowViews = new List<SpriteRenderer>();

        bool _runStarted;
        Proto_GreyboxRunResult _runResult = Proto_GreyboxRunResult.Playing;
        float _hitFlashTimer;
        bool _debugTogglePrev;
        bool _viewModeApplied;

        public Proto_GreyboxPlayerMotor Motor => _motor;
        public Proto_GreyboxRoll Roll => _roll;
        public Proto_GreyboxAim Aim => _aim;
        public Proto_GreyboxGun Gun => _gun;
        public Proto_GreyboxRollDizzy RollDizzy => _rollDizzy;
        public Proto_GreyboxPlayerHearts Hearts => _hearts;
        public Proto_GreyboxGrenade Grenade => _grenade;
        public bool GrenadeAimPreviewActive => _grenadeStickActive && _grenade != null && _grenade.CanThrow(_simTime);
        public Vector2 GrenadePreviewLanding => _grenadePreviewLanding;
        public Proto_GreyboxCover Cover => _cover;
        public Proto_GreyboxRuntimeTuning RuntimeTuning => _tuning;
        public Proto_GreyboxWaveDirector WaveDirector => _waveDirector;
        public IReadOnlyList<Proto_GreyboxTarget> Targets => _targets;
        public IReadOnlyList<Proto_GreyboxAllyBulletSim> AllyBullets => _allyBullets;
        public IReadOnlyList<SpriteRenderer> AllyBulletViewRenderers => _allyBulletViews;
        public IReadOnlyList<SpriteRenderer> AllyTracerViewRenderers => _allyTracerViews;
        public IReadOnlyList<Proto_GreyboxBulletSim> EnemyBullets => _enemyBullets;
        public int Hp => _hp;
        public int Score => _score;
        public float SimTime => _simTime;
        public Proto_GreyboxRunResult RunResult => _runResult;
        public bool IsPaused { get; private set; }
        public bool PlayerHitFlashActive => _hitFlashTimer > 0f;
        public Proto_GreyboxViewMode ViewMode => Proto_GreyboxViewModeState.Current;

        public void NotifyMoveZoneChanged() => _westSkin?.RebuildMoveBoundary();

        public void SetViewMode(Proto_GreyboxViewMode mode, bool persist = true)
        {
            EnsureLogic();
            Proto_GreyboxViewModeState.SetMode(mode, persist);
            _tuning.View3D = mode == Proto_GreyboxViewMode.View3D;
            ApplyViewMode();
        }

        void ApplyViewMode()
        {
            bool threeD = Proto_GreyboxViewModeState.Is3D;
            var presenter = Proto_GreyboxViewPresenterRegistry.Ensure();
            Proto_GreyboxWorld2DVisibility.SetWorldVisible(!threeD);
            var cam2d = Proto_GreyboxWorld2DVisibility.Find2DWorldCamera();
            if (cam2d != null)
            {
                cam2d.enabled = !threeD;
                if (!threeD)
                {
                    cam2d.tag = "MainCamera";
                    cam2d.GetComponent<Proto_GreyboxCameraSetup>()?.Apply();
                }
                else
                    cam2d.tag = "Untagged";
            }

            presenter?.SetActive(threeD, this);
            var hud = GameObject.Find("Greybox_Hud");
            var canvas = hud != null ? hud.GetComponent<Canvas>() : null;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                var worldCam = threeD && presenter?.WorldCamera != null ? presenter.WorldCamera : cam2d;
                if (worldCam != null)
                    canvas.worldCamera = worldCam;
            }

            _viewModeApplied = true;
            if (threeD)
                presenter?.Sync(this);
        }

        void SyncViewPresenter()
        {
            if (!Proto_GreyboxViewModeState.Is3D)
                return;
            Proto_GreyboxViewPresenterRegistry.Ensure()?.Sync(this);
        }

        public void SetWestPixelSkin(bool enabled)
        {
            EnsureLogic();
            EnsureWestSkin();
            _westSkin.SetEnabled(enabled);
        }

        public void SetPlayerPerspectiveRuntime()
        {
            EnsureLogic();
            _tuning.ApplyPlayerPerspective();
        }

        public void SetPerspectiveDepth(bool enabled)
        {
            EnsureLogic();
            _tuning.PerspectiveDepth = enabled;
            Proto_GreyboxPerspectiveDepth.Enabled = enabled;
            Proto_GreyboxMoveZone.ResetToDefaults();
            var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            builder?.RebuildEnvironment();
            NotifyMoveZoneChanged();
            EnsureWestSkin();
            if (_westSkin != null && _westSkin.IsActive)
                _westSkin.RebuildStageLayout();
            else if (_tuning.WestPixelSkin)
                _westSkin?.SetEnabled(true);
            ApplyCrosshairPosition();
        }

        void ApplyCrosshairPosition()
        {
            var ch = GameObject.Find("Greybox_Crosshair");
            if (ch == null) return;
            var aim = Proto_GreyboxAim.DefaultAimGround;
            ch.transform.position = new Vector3(aim.x, aim.y, 0f);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            if (IsPaused)
            {
                Time.timeScale = 0f;
                EnsurePausePanel();
                _pauseUi.SetVisible(true);
            }
            else
            {
                Time.timeScale = 1f;
                _pauseUi.SetVisible(false);
            }
        }

        public static Proto_GreyboxInput FilterCombatInputWhenPaused(Proto_GreyboxInput input, bool paused) =>
            paused ? Proto_GreyboxCombatInputGate.StripCombatWhenPaused(input) : input;

        void Awake() => EnsureLogic();

        void EnsureLogic()
        {
            if (_motor != null) return;
            _sprite = GetComponent<SpriteRenderer>();
            _baseColor = _sprite != null ? _sprite.color : Color.white;
            var foot = new Vector2(transform.position.x, transform.position.y);
            Proto_GreyboxBulletSpeedPrefs.LoadInto(_tuning);
            Proto_GreyboxViewModeState.EnsureLoaded();
            _tuning.View3D = Proto_GreyboxViewModeState.Is3D;
            _hearts.Configure(_tuning.PlayerMaxHp, _tuning.HeartRegenSec);
            _rollDizzy.ApplyRuntime(_tuning);
            _hp = _hearts.Hearts;
            _motor = new Proto_GreyboxPlayerMotor(foot, _tuning.MoveSpeed);
            _roll = new Proto_GreyboxRoll();
            _cover = Proto_GreyboxCover.FromStageCrates();
            _aim = new Proto_GreyboxAim();
            _gun = new Proto_GreyboxGun();
            _grenade = new Proto_GreyboxGrenade();
            SyncTargetsFromGrunts();
            CacheHudRefs();
            CacheCrosshair();
            EnsureDebugUi();
            if (!_viewModeApplied)
                ApplyViewMode();
        }

        public void SetAimOverride(Vector2 groundPos) => _aimOverride = groundPos;

        public void ClearAimOverride() => _aimOverride = null;

        public void PoseAimStickDrag(Vector2 dragOffsetWorld)
        {
            EnsureLogic();
            _aim.PoseInProgressDrag(_motor.FootPosition, CollectAimTargets(), dragOffsetWorld);
        }

        public void RefreshTargets()
        {
            SyncTargetsFromGrunts();
            EnsureViewRefs();
        }

        void SyncTargetsFromGrunts()
        {
            _targets.Clear();
            _targetViews.Clear();
            foreach (var pair in _gruntViews)
            {
                if (pair.Key == null || pair.Value == null) continue;
                _targets.Add(pair.Key.Target);
                _targetViews.Add(pair.Value);
            }
        }

        void EnsureViewRefs()
        {
            if (_ammoText == null || _scoreText == null || _hpText == null || _waveText == null)
                CacheHudRefs();
            if (_crosshair == null)
                CacheCrosshair();
            else
            {
                var ch = GameObject.Find("Greybox_Crosshair");
                if (ch != null && ch.transform != _crosshair)
                    _crosshair = ch.transform;
            }
        }

        void CacheHudRefs()
        {
            var hp = GameObject.Find(Proto_GreyboxHudBuilder.HpTextName);
            if (hp != null)
                _hpText = hp.GetComponent<Text>();
            var score = GameObject.Find(Proto_GreyboxHudBuilder.ScoreTextName);
            if (score != null)
                _scoreText = score.GetComponent<Text>();
            var ammo = GameObject.Find(Proto_GreyboxHudBuilder.AmmoTextName);
            if (ammo != null)
                _ammoText = ammo.GetComponent<Text>();
            var wave = GameObject.Find(Proto_GreyboxHudBuilder.WaveTextName);
            if (wave != null)
                _waveText = wave.GetComponent<Text>();
            var knob = GameObject.Find(Proto_GreyboxHudBuilder.JoystickKnobName);
            if (knob != null)
                _joystickKnob = knob.GetComponent<RectTransform>();
            var hpFill = GameObject.Find(Proto_GreyboxHudBuilder.HpBarFillName);
            if (hpFill != null)
                _hpBarFill = hpFill.GetComponent<Image>();
            if (_ammoPips.Count == 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    var pip = GameObject.Find(Proto_GreyboxHudBuilder.AmmoPipPrefix + i);
                    if (pip != null)
                        _ammoPips.Add(pip.GetComponent<Image>());
                }
            }

            if (_reloadFill == null)
            {
                var fill = GameObject.Find(Proto_GreyboxHudBuilder.ReloadFillName);
                if (fill != null)
                    _reloadFill = fill.GetComponent<Image>();
            }

            if (_reloadDisc == null)
            {
                var reload = GameObject.Find(Proto_GreyboxHudBuilder.ReloadButtonName);
                if (reload != null)
                    _reloadDisc = reload.GetComponent<Image>();
            }

            if (_reloadLabel == null)
            {
                var label = GameObject.Find(Proto_GreyboxHudBuilder.ReloadLabelName);
                if (label != null)
                {
                    _reloadLabel = label.GetComponent<Text>();
                    _reloadLabelRt = label.GetComponent<RectTransform>();
                }
            }

            if (_reloadIcon == null)
            {
                var icon = GameObject.Find(Proto_GreyboxHudBuilder.ReloadButtonName + "_Icon");
                if (icon != null)
                    _reloadIcon = icon.GetComponent<Image>();
            }
        }

        void CacheCrosshair()
        {
            var ch = GameObject.Find("Greybox_Crosshair");
            if (ch != null)
                _crosshair = ch.transform;
        }

        void EnsureDebugUi()
        {
            var hud = GameObject.Find("Greybox_Hud");
            if (hud == null) return;
            var canvas = hud.GetComponent<Canvas>();
            if (canvas == null) return;
            _debugUi = Proto_GreyboxRuntimeDebugUi.Ensure(canvas, _tuning, this);
        }

        void EnsureWestSkin()
        {
            if (_westSkin == null)
                _westSkin = new Proto_GreyboxWestPixelSkin(this);
            _westSkin.EnsureBound();
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            EnsureLogic();
            var input = PollLegacyInput();
            if (IsPaused && _runResult == Proto_GreyboxRunResult.Playing)
                return;
            Simulate(input, Time.deltaTime);
        }

        public void Simulate(Proto_GreyboxInput input, float dt)
        {
            EnsureLogic();
            EnsureViewRefs();
            ApplyRuntimeTuning();
            input = FilterCombatInputWhenPaused(input, IsPaused);

            if (_runResult != Proto_GreyboxRunResult.Playing)
            {
                if (input.RestartPressed)
                    FullRestart();
                UpdateResultPanel();
                return;
            }

            if (!_runStarted)
            {
                Proto_GreyboxPerspectiveDepth.Enabled = _tuning.PerspectiveDepth;
                _tuning.ApplyPlayerPerspective();
                Proto_GreyboxMoveZone.ResetToDefaults();
                _waveDirector.StartRun(_tuning);
                _runStarted = true;
                Proto_GreyboxWestPixelSkinState.Enabled = _tuning.WestPixelSkin;
                EnsureWestSkin();
                _westSkin?.SetEnabled(_tuning.WestPixelSkin);
            }

            _simTime += dt;
            _rollDizzy.Tick(_simTime);
            EnsureWestSkin();
            _westSkin?.SetSimDt(dt);
            if (_rollDizzy.BlocksMovement)
                input = new Proto_GreyboxInput
                {
                    DebugToggle = input.DebugToggle,
                    RestartPressed = input.RestartPressed
                };
            _roll.RememberMoveDirection(input.Move);

            if (input.DebugToggle && !_debugTogglePrev)
                _debugUi?.Toggle();
            _debugTogglePrev = input.DebugToggle;

            if (input.ReloadPressed && !_reloadPressedPrev && !_rollDizzy.BlocksFire)
            {
                if (_gun.Ammo < _gun.MagazineSize)
                {
                    _gun.StartReload(_simTime);
                    if (_gun.ReloadCasingCount > 0)
                        _westSkin?.OnReloadStartCasing(_gun.ReloadCasingCount);
                }
            }

            _reloadPressedPrev = input.ReloadPressed;

            _wasRolling = _roll.IsRolling;
            if (_roll.IsRolling)
                _motor.ApplyDelta(_roll.Tick(dt));
            else
            {
                _roll.Tick(dt);
                var move = input.Move;
                float footPct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(_motor.FootPosition.y);
                float vMul = Proto_GreyboxPerspectiveDepth.PlayerVerticalMoveSpeedFactorAtFootPercent(footPct);
                move.y *= vMul;
                _motor.TickMove(move, dt);
            }

            if (_wasRolling && !_roll.IsRolling)
            {
                _rollDizzy.NotifyRollEnded(_simTime);
                _westSkin?.OnRollEnded(_roll.Facing, _rollDizzy.Phase);
                if (_rollDizzy.Phase == Proto_GreyboxRollDizzyPhase.Stun)
                    _gun.CancelReload();
            }

            if (input.RollPressed && _rollDizzy.ChainLocked(_simTime))
                _rollGaugeHud.TriggerLockedNudge();

            if (input.RollPressed && !_rollDizzy.BlocksRoll && _rollDizzy.TryAcceptRollStart(_simTime)
                && _roll.TryStart(input.Move))
            {
                _gun.CancelReload();
                _rollDizzy.NotifyRollStarted(_simTime);
                _westSkin?.OnRollStarted(_roll.Facing);
            }

            _rollPressedPrev = input.RollPressed;

            var foot = _motor.FootPosition;
            var aimTargets = CollectAimTargets();
            if (input.FireStickHeld)
            {
                if (!_aim.IsDragging)
                {
                    _aim.ConfigureScreenStick(_tuning.AimStickRadiusHeightPercent, _tuning.AimStickSmoothTime,
                        Proto_GreyboxPointerMath.AimStickDeadZonePortion,
                        Proto_GreyboxPointerMath.AimStickDragThresholdPortion,
                        _tuning.AimAngularCapDegPerSec, _tuning.AimFineAimExponent);
                    _aim.BeginDrag(foot, aimTargets);
                }

                _aim.SetAimStickScreenDelta(input.FireStickDragWorld, Screen.width, Screen.height, dt);
            }
            else if (input.MouseAimHeld)
                _aim.TickKeyboardDrag(foot, aimTargets, input.MouseAimWorld, input.MousePressWorld);
            else if (!_aim.IsDragging)
                _aim.TickIdle(foot, aimTargets);

            _gun.Tick(_simTime);

            Proto_GreyboxAimReleaseIntent fireIntent = Proto_GreyboxAimReleaseIntent.None;
            if (input.FireStickReleased)
                fireIntent = _aim.EndDrag();
            else if (input.MouseAimReleased)
                fireIntent = _aim.EndKeyboardDrag(input.MouseAimWorld, input.MousePressWorld);
            else if (input.FireQuickAuto)
            {
                _aim.TickIdle(foot, aimTargets);
                fireIntent = Proto_GreyboxAimReleaseIntent.AutoAim;
            }

            if (!_rollDizzy.BlocksFire && (fireIntent == Proto_GreyboxAimReleaseIntent.AutoAim
                || fireIntent == Proto_GreyboxAimReleaseIntent.ManualDirection))
                TryFireFromIntent(fireIntent);

            TickGrenadeInput(input, foot, aimTargets);

            _grenade.Tick(_simTime, dt, CollectHittableTargets(), _cover, DeactivateCratePercent);
            TickGrunts(dt, foot);
            _waveDirector.Tick(dt, _tuning);
            TickAllyBullets(dt);
            TickEnemyBullets(dt);
            SyncAllyBulletViews();
            SyncEnemyBulletViews();
            SyncGruntViews();
            ApplyTransform(dt);
            _westSkin?.TickFrame(dt, input);
            UpdateCrosshair();
            UpdateAimLineVisual();
            UpdateGrenadePreviewVisual();
            TickGrenadeFlightVisual();
            UpdateHud(dt);
            UpdateDizzyVisual();
            TickExplosionVis();
            TickDebugBullet(dt);
            _hearts.Tick(dt, _tuning.HeartRegenSec);
            _hp = _hearts.Hearts;
            CheckRunEnd();
            SyncViewPresenter();
            if (Proto_GreyboxViewModeState.Is3D)
                Proto_GreyboxWorld2DVisibility.SetWorldVisible(false);

            if (Input.GetKeyDown(KeyCode.T))
                FireDebugBullet();
        }

        void ApplyRuntimeTuning()
        {
            _tuning.ApplyDepthScales();
            _tuning.ApplyPlayerPerspective();
            _motor.SetMoveSpeed(_tuning.MoveSpeed);
            _roll.ApplyRuntime(_tuning);
            _rollDizzy.ApplyRuntime(_tuning);
            _gun.ApplyRuntime(_tuning);
            _grenade.ApplyRuntime(_tuning);
        }

        List<Proto_GreyboxTarget> CollectAimTargets()
        {
            var list = new List<Proto_GreyboxTarget>();
            foreach (var g in _waveDirector.Grunts)
            {
                if (g.Target.IsAlive)
                    list.Add(g.Target);
            }

            return list;
        }

        List<Proto_GreyboxTarget> CollectHittableTargets()
        {
            var list = new List<Proto_GreyboxTarget>();
            foreach (var g in _waveDirector.Grunts)
            {
                if (g.Target.IsAlive && !g.IsHidden)
                    list.Add(g.Target);
            }

            return list;
        }

        Vector2 CombatAimGround() => _aimOverride ?? _aim.AimGroundPosition();

        void TryFireFromIntent(Proto_GreyboxAimReleaseIntent intent)
        {
            if (_rollDizzy.BlocksFire) return;
            if (!_gun.TryFire(_simTime)) return;
            Vector2 aim = intent == Proto_GreyboxAimReleaseIntent.AutoAim
                ? _aim.AutoFireAimPoint()
                : _aim.ManualFireAimPoint();
            if (_aimOverride.HasValue)
                aim = _aimOverride.Value;
            SpawnAllyBulletAt(aim);
        }

        void SpawnAllyBulletAt(Vector2 aimGround)
        {
            var foot = _motor.FootPosition;
            _allyBullets.Add(new Proto_GreyboxAllyBulletSim(foot, aimGround, _cover, 25f, _tuning.AllyBulletSpeed,
                _tuning.AllyBulletDiameter, _tuning.AllyBulletDamage));
            _westSkin?.OnPlayerFired();
        }

        void EnsureAllyBulletRoot()
        {
            if (_allyBulletRoot != null) return;
            var parent = transform.parent != null ? transform.parent : transform;
            var go = new GameObject("Greybox_AllyBullets");
            go.transform.SetParent(parent, false);
            _allyBulletRoot = go.transform;
        }

        void EnsureEnemyBulletRoot()
        {
            if (_enemyBulletRoot != null) return;
            var parent = transform.parent != null ? transform.parent : transform;
            var go = new GameObject("Greybox_EnemyBullets");
            go.transform.SetParent(parent, false);
            _enemyBulletRoot = go.transform;
        }

        void EnsureGruntRoot()
        {
            if (_gruntRoot != null) return;
            var parent = transform.parent != null ? transform.parent : transform;
            var go = new GameObject("Greybox_Grunts");
            go.transform.SetParent(parent, false);
            _gruntRoot = go.transform;
        }

        void SyncAllyBulletViews()
        {
            EnsureWestSkin();
            EnsureAllyBulletRoot();
            if (_westSkin != null && _westSkin.SyncAllyBullets(_allyBullets, _tuning.AllyBulletDiameter, _allyBulletViews,
                    _allyTracerViews, _allyBulletHotspotViews, _allyBulletRoot))
                return;
            while (_allyBulletViews.Count < _allyBullets.Count)
            {
                var go = new GameObject("Greybox_AllyBullet");
                go.transform.SetParent(_allyBulletRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                sr.color = new Color(1f, 0.98f, 0.55f, 1f);
                _allyBulletViews.Add(sr);

                var hotGo = new GameObject("Greybox_AllyBullet_Hotspot");
                hotGo.transform.SetParent(go.transform, false);
                var hotSr = hotGo.AddComponent<SpriteRenderer>();
                hotSr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                hotSr.color = Color.white;
                _allyBulletHotspotViews.Add(hotSr);

                var tail = new GameObject("Greybox_AllyTracer");
                tail.transform.SetParent(_allyBulletRoot, false);
                var tsr = tail.AddComponent<SpriteRenderer>();
                tsr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
                tsr.color = Proto_GreyboxPalette.AllyTracer;
                _allyTracerViews.Add(tsr);
            }

            for (int i = 0; i < _allyBulletViews.Count; i++)
            {
                var sr = _allyBulletViews[i];
                var tracer = i < _allyTracerViews.Count ? _allyTracerViews[i] : null;
                var hot = i < _allyBulletHotspotViews.Count ? _allyBulletHotspotViews[i] : null;
                if (i < _allyBullets.Count && _allyBullets[i].Alive)
                {
                    var b = _allyBullets[i];
                    var vel = b.GroundVelocity;
                    var dir = vel.sqrMagnitude > 1e-6f ? vel.normalized : Vector2.right;
                    var vis = b.VisualPositionAtGround();
                    float scale = b.VisualScale;
                    float diam = Mathf.Max(Proto_GreyboxTuning.AllyBulletVisualDiameter * scale,
                        Proto_GreyboxLayout.MinAllyBulletWorldDiameter);
                    vis += dir * diam * 0.18f;
                    int order = Proto_GreyboxLayout.CombatDrawSortingOrder(b.GroundPosition.y);
                    sr.gameObject.SetActive(true);
                    sr.transform.position = new Vector3(vis.x, vis.y, 0f);
                    sr.transform.localScale = new Vector3(diam, diam, 1f);
                    sr.color = new Color(1f, 0.98f, 0.55f, 1f);
                    sr.sortingOrder = order;

                    if (hot != null)
                    {
                        hot.gameObject.SetActive(true);
                        float hotD = diam * 0.32f;
                        hot.transform.localScale = new Vector3(hotD, hotD, 1f);
                        hot.sortingOrder = order + 1;
                    }

                    if (tracer != null)
                    {
                        float tailLen = Proto_GreyboxTuning.AllyBulletVisualTracerLength;
                        var center = vis - dir * tailLen * 0.5f;
                        tracer.gameObject.SetActive(true);
                        tracer.transform.position = new Vector3(center.x, center.y + 0.02f * scale, 0f);
                        tracer.transform.localScale = new Vector3(tailLen, diam * 0.3f, 1f);
                        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        tracer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                        tracer.sortingOrder = order - 1;
                    }
                }
                else
                {
                    sr.gameObject.SetActive(false);
                    if (tracer != null) tracer.gameObject.SetActive(false);
                    if (hot != null) hot.gameObject.SetActive(false);
                }
            }
        }

        void SyncEnemyBulletViews()
        {
            EnsureWestSkin();
            EnsureEnemyBulletRoot();
            if (_westSkin != null && _westSkin.SyncEnemyBullets(_enemyBullets, _tuning.EnemyBulletDiameter,
                    _enemyBulletViews, _enemyBulletGlowViews, _enemyBulletRoot, _simTime))
                return;
            while (_enemyBulletViews.Count < _enemyBullets.Count)
            {
                var glow = new GameObject("Greybox_EnemyBulletGlow");
                glow.transform.SetParent(_enemyBulletRoot, false);
                var gsr = glow.AddComponent<SpriteRenderer>();
                gsr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                gsr.color = Proto_GreyboxPalette.EnemyBulletGlow;
                _enemyBulletGlowViews.Add(gsr);

                var go = new GameObject("Greybox_EnemyBullet");
                go.transform.SetParent(_enemyBulletRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                sr.color = Proto_GreyboxPalette.EnemyBulletCore;
                _enemyBulletViews.Add(sr);
            }

            for (int i = 0; i < _enemyBulletViews.Count; i++)
            {
                var sr = _enemyBulletViews[i];
                var glow = i < _enemyBulletGlowViews.Count ? _enemyBulletGlowViews[i] : null;
                if (i < _enemyBullets.Count && _enemyBullets[i].Alive)
                {
                    var b = _enemyBullets[i];
                    float diam = b.VisualDiameter(_tuning.EnemyBulletDiameter) *
                                 Proto_GreyboxTuning.EnemyBulletVisualDiameterMul;
                    int order = Proto_GreyboxLayout.CombatDrawSortingOrder(b.GroundPosition.y);
                    var pos = new Vector3(b.GroundPosition.x, b.GroundPosition.y, 0f);
                    sr.gameObject.SetActive(true);
                    sr.transform.position = pos;
                    sr.transform.localScale = new Vector3(diam, diam, 1f);
                    sr.sortingOrder = order;

                    if (glow != null)
                    {
                        float halo = diam * Proto_GreyboxTuning.EnemyBulletVisualGlowMul;
                        glow.gameObject.SetActive(true);
                        glow.transform.position = pos;
                        glow.transform.localScale = new Vector3(halo, halo, 1f);
                        glow.sortingOrder = order - 1;
                    }
                }
                else
                {
                    sr.gameObject.SetActive(false);
                    if (glow != null) glow.gameObject.SetActive(false);
                }
            }
        }

        void TickAllyBullets(float dt)
        {
            var hittable = CollectHittableTargets();
            for (int i = _allyBullets.Count - 1; i >= 0; i--)
            {
                var b = _allyBullets[i];
                if (!b.Alive)
                {
                    _allyBullets.RemoveAt(i);
                    continue;
                }

                var hit = b.Tick(dt, hittable);
                if (hit != null)
                {
                    foreach (var v in _targetViews)
                    {
                        if (v?.Logic != hit) continue;
                        v.FlashHit();
                        _westSkin?.OnGruntHitFlash(v);
                        _westSkin?.SpawnHitSpark(hit.FootPosition);
                        break;
                    }
                }

                if (!b.Alive)
                {
                    if (hit == null)
                        _westSkin?.SpawnBulletDust(b.GroundPosition);
                    _allyBullets.RemoveAt(i);
                }
            }
        }

        void TickEnemyBullets(float dt)
        {
            var footprint = Proto_GreyboxBulletSim.PlayerGroundFootprint(_motor.FootPosition);
            for (int i = _enemyBullets.Count - 1; i >= 0; i--)
            {
                var b = _enemyBullets[i];
                if (!b.Alive)
                {
                    _enemyBullets.RemoveAt(i);
                    continue;
                }

                int hpBefore = _hearts.Hearts;
                int hpScratch = hpBefore;
                b.Tick(dt, footprint, IsInvulnerable, ref hpScratch, _tuning.CoverDamageReduction01, _coverRng,
                    Proto_GreyboxTuning.EnemyBulletDamage);
                if (hpScratch < hpBefore)
                {
                    if (_hearts.TakeHit())
                    {
                        _hp = _hearts.Hearts;
                        _heartsHud.TriggerPop(_hearts.Hearts);
                        _hitFlashTimer = 0.12f;
                        _westSkin?.OnPlayerHitFlash();
                    }
                }
                else
                    _hp = _hearts.Hearts;

                if (!b.Alive)
                {
                    _westSkin?.SpawnHitSpark(b.GroundPosition);
                    _enemyBullets.RemoveAt(i);
                }
            }
        }

        void TickGrunts(float dt, Vector2 playerFoot)
        {
            EnsureGruntRoot();
            foreach (var grunt in _waveDirector.Grunts)
            {
                if (!grunt.Target.IsAlive) continue;
                grunt.Tick(dt, Proto_GreyboxTuning.GruntWalkSpeed, Proto_GreyboxTuning.GruntStrafeRange,
                    _tuning.EnemyFireInterval, _tuning.EnemyBulletSpeed, playerFoot);
                if (!grunt.FiredThisTick) continue;
                _enemyBullets.Add(new Proto_GreyboxBulletSim(grunt.LastFireGround, grunt.LastFireVelocity, _cover,
                    _tuning.EnemyBulletDiameter));
            }

            for (int i = _waveDirector.Grunts.Count - 1; i >= 0; i--)
            {
                var grunt = _waveDirector.Grunts[i];
                if (_gruntViews.ContainsKey(grunt)) continue;
                CreateGruntView(grunt);
            }

            SyncTargetsFromGrunts();
        }

        void CreateGruntView(Proto_GreyboxGrunt grunt)
        {
            EnsureWestSkin();
            EnsureGruntRoot();
            var go = new GameObject("Greybox_Grunt");
            go.transform.SetParent(_gruntRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Proto_GreyboxSpriteUtil.GetGruntSprite();
            sr.color = Color.white;
            var view = go.AddComponent<Proto_GreyboxTargetView>();
            view.Init(grunt.Target);
            view.SyncFromLogic();
            _westSkin?.EnsureGruntSprite(view);
            var gruntOrder = sr.sortingOrder;
            Proto_GreyboxSpriteUtil.AttachFootShadow(go.transform, gruntOrder, 0.85f, 0.16f);
            _gruntViews[grunt] = view;
        }

        void SyncGruntViews()
        {
            foreach (var pair in _gruntViews)
            {
                var grunt = pair.Key;
                var view = pair.Value;
                if (grunt == null || view == null) continue;
                if (!grunt.Target.IsAlive)
                {
                    if (_westSkin != null && _westSkin.TryPlayGruntDeath(grunt, view, ref _score))
                        continue;
                    if (view.gameObject.activeSelf)
                    {
                        view.MarkDefeated();
                        view.gameObject.SetActive(false);
                        _score += Proto_GreyboxTuning.ScorePerKill;
                    }

                    continue;
                }

                view.gameObject.SetActive(true);
                if (_westSkin != null && _westSkin.IsActive)
                {
                    _westSkin.SyncGruntLiving(grunt, view, grunt.IsHidden);
                    view.SyncPositionOnly();
                }
                else
                {
                    view.SetHiddenVisual(grunt.IsHidden);
                    view.SyncFromLogic();
                }
            }
        }

        void CheckRunEnd()
        {
            if (_hp <= 0)
            {
                _hp = 0;
                _runResult = Proto_GreyboxRunResult.Down;
                EnterResultScreen();
            }
            else if (_waveDirector.State == Proto_GreyboxWaveDirectorState.Cleared)
            {
                _runResult = Proto_GreyboxRunResult.Clear;
                EnterResultScreen();
            }
        }

        void EnterResultScreen()
        {
            SetPaused(false);
            Time.timeScale = 1f;
            EnsureResultPanel();
            UpdateResultPanel();
        }

        void EnsurePausePanel()
        {
            if (_pauseUi.IsBuilt) return;
            var hud = GameObject.Find("Greybox_Hud");
            if (hud == null) return;
            _pauseUi.Ensure(hud.transform);
        }

        void EnsureSettingsPanel()
        {
            var hud = GameObject.Find("Greybox_Hud");
            if (hud == null) return;
            _settingsUi.Ensure(hud.transform, _tuning, CloseSettings);
        }

        /// <summary>Editor screenshot / smoke: open 설정 overlay without menu taps.</summary>
        public void ShowSettingsForCapture() => OpenSettings(true);

        void OpenSettings(bool fromGear)
        {
            EnsureSettingsPanel();
            _settingsFromGear = fromGear;
            if (fromGear)
            {
                SetPaused(true);
                _pauseUi.SetVisible(false);
            }
            else
                _pauseUi.SetVisible(false);

            _settingsUi.SetVisible(true);
        }

        void CloseSettings()
        {
            _settingsUi.SetVisible(false);
            if (_settingsFromGear)
            {
                SetPaused(false);
                return;
            }

            if (IsPaused)
                _pauseUi.SetVisible(true);
        }

        void EnsureResultPanel()
        {
            if (_resultUi.IsBuilt) return;
            var hud = GameObject.Find("Greybox_Hud");
            if (hud == null) return;
            _resultUi.Ensure(hud.transform);
        }

        void UpdateResultPanel()
        {
            EnsureResultPanel();
            if (!_resultUi.IsBuilt) return;
            int wave = Mathf.Max(1, _waveDirector.CurrentWave);
            if (_runResult == Proto_GreyboxRunResult.Clear)
                wave = _waveDirector.TotalWaves;
            int kills = _score / Mathf.Max(1, Proto_GreyboxTuning.ScorePerKill);
            _resultUi.Refresh(_runResult, _score, wave, _waveDirector.TotalWaves, _simTime, kills);
        }

        public void FullRestart()
        {
            SetPaused(false);
            Time.timeScale = 1f;
            var builder = FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            if (builder != null) { builder.Build(); return; } // Build() replaces this player with a fresh controller
            _allyBullets.Clear();
            _enemyBullets.Clear();
            _gruntViews.Clear();
            _targets.Clear();
            _targetViews.Clear();
            _simTime = 0f;
            _score = 0;
            _hearts.Configure(_tuning.PlayerMaxHp, _tuning.HeartRegenSec);
            _rollDizzy.ApplyRuntime(_tuning);
            _hp = _hearts.Hearts;
            _runResult = Proto_GreyboxRunResult.Playing;
            _runStarted = false;
            _grenade = new Proto_GreyboxGrenade();
            _gun = new Proto_GreyboxGun();
            _aim = new Proto_GreyboxAim();
            _cover = Proto_GreyboxCover.FromStageCrates();
            _waveDirector.ResetRun();
            _explosionVis = null;
            _resultUi.Clear();
            _pauseUi.Clear();
            _settingsUi.Clear();
            _motor = null;
            EnsureLogic();
            transform.position = new Vector3(
                Proto_GreyboxLayout.ScreenXPercentToWorldX(49f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(88f), 0f);
        }

        public void SetDebugPanelOpen(bool open)
        {
            EnsureLogic();
            EnsureDebugUi();
            _debugUi?.SetOpen(open);
        }

        void DeactivateCratePercent(float x0, float x1, float yBottom)
        {
            if (Mathf.Approximately(x0, 52f))
                DeactivateCrate(Proto_GreyboxStageBuilder.CrateFarName);
            else if (Mathf.Approximately(x0, 12f))
                DeactivateCrate(Proto_GreyboxStageBuilder.CrateMidName);
            else if (Mathf.Approximately(x0, 58f))
                DeactivateCrate(Proto_GreyboxStageBuilder.CrateNearName);
        }

        static void DeactivateCrate(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                go.SetActive(false);
        }

        void UpdateCrosshair()
        {
            if (_crosshair == null)
            {
                CacheCrosshair();
                if (_crosshair == null) return;
            }

            Vector2 aim;
            if (_aim != null && (_aim.IsDragging || _aimOverride.HasValue))
                aim = ResolveAimVisualEnd();
            else
                aim = Proto_GreyboxAim.DefaultAimGround;
            _crosshair.position = new Vector3(aim.x, aim.y, 0f);
        }

        Vector2 ResolveAimVisualEnd()
        {
            var foot = _motor.FootPosition;
            var candidate = _aimOverride ?? _aim.AimLineEnd;
            var delta = candidate - foot;
            float len = delta.magnitude;
            if (len < 1e-5f) return candidate;
            return Proto_GreyboxAim.ClipAimEnd(foot, delta / len, Mathf.Min(len, 25f));
        }

        void UpdateHud(float dt)
        {
            if (_ammoText == null)
                CacheHudRefs();
            bool westHud = _westSkin != null && _westSkin.IsActive;
            if (_ammoText != null)
            {
                if (westHud)
                    _ammoText.gameObject.SetActive(false);
                else
                {
                    _ammoText.gameObject.SetActive(true);
                    _ammoText.text = _gun.IsReloading ? "R..." : $"{_gun.Ammo}/{_gun.MagazineSize}";
                }
            }
            if (_scoreText != null)
                _scoreText.text = _score.ToString();
            if (_waveText != null)
            {
                int w = Mathf.Max(1, _waveDirector.CurrentWave);
                if (_runResult != Proto_GreyboxRunResult.Playing && _waveDirector.State == Proto_GreyboxWaveDirectorState.Cleared)
                    w = _waveDirector.TotalWaves;
                _waveText.text = $"STAGE 1 · WAVE {w}/{_waveDirector.TotalWaves}";
            }

            var hud = GameObject.Find("Greybox_Hud");
            if (hud != null)
                _heartsHud.EnsureBound(hud.transform);
            _heartsHud.Update(_hearts, _simTime, dt);
            _cylinderHud.Update(_gun, _simTime, dt);
            _grenadeGaugeHud.EnsureBound();
            _grenadeGaugeHud.Update(_grenade, _simTime, dt);
            _rollGaugeHud.EnsureBound();
            _rollGaugeHud.Update(_rollDizzy, _simTime, dt);
            for (int i = 0; i < _ammoPips.Count; i++)
            {
                var pip = _ammoPips[i];
                if (pip == null) continue;
                bool visible = false;
                pip.gameObject.SetActive(visible);
                if (!visible) continue;
                bool on = i < _gun.Ammo;
                pip.color = on ? Proto_GreyboxPalette.HudAmmoPipOn : Proto_GreyboxPalette.HudAmmoPipOff;
            }

            if (_reloadFill != null)
                _reloadFill.fillAmount = _gun.ReloadProgress01(_simTime);
            bool pulseReload = _gun.Ammo <= 0 && !_gun.IsReloading;
            if (_reloadLabel != null)
            {
                if (westHud)
                    _reloadLabel.gameObject.SetActive(pulseReload);
                else
                    _reloadLabel.gameObject.SetActive(true);

                if (!westHud || pulseReload)
                {
                    _reloadLabel.text = pulseReload ? "장전!" : "RELOAD";
                    if (pulseReload)
                    {
                        bool hangul;
                        var font = Proto_GreyboxHudBuilder.GetUiFont(out hangul);
                        if (font != null)
                            _reloadLabel.font = font;
                        _reloadLabel.fontSize = ReloadLabelFontEmpty;
                        _reloadLabel.fontStyle = FontStyle.Bold;
                        _reloadLabel.color = new Color(1f, 0.94f, 0.28f, 1f);
                        if (_reloadLabelRt != null)
                            Proto_GreyboxHudBuilder.ApplyRect(_reloadLabelRt,
                                Proto_GreyboxHudBuilder.ReloadCenterX - Proto_GreyboxHudBuilder.ReloadEmptyLabelPadPercent,
                                Proto_GreyboxHudBuilder.ReloadCenterY - Proto_GreyboxHudBuilder.ReloadEmptyLabelPadPercent,
                                Proto_GreyboxHudBuilder.ReloadCenterX + Proto_GreyboxHudBuilder.ReloadEmptyLabelPadPercent,
                                Proto_GreyboxHudBuilder.ReloadCenterY + Proto_GreyboxHudBuilder.ReloadEmptyLabelPadPercent);
                    }
                    else if (!westHud)
                    {
                        _reloadLabel.fontSize = ReloadLabelFontDefault;
                        _reloadLabel.fontStyle = FontStyle.Normal;
                        _reloadLabel.color = new Color(1f, 1f, 1f, 0.7f);
                        if (_reloadLabelRt != null)
                            Proto_GreyboxHudBuilder.ApplyRect(_reloadLabelRt,
                                Proto_GreyboxHudBuilder.ReloadCenterX - Proto_GreyboxHudBuilder.ReloadLabelPadPercent,
                                Proto_GreyboxHudBuilder.ReloadCenterY - Proto_GreyboxHudBuilder.ReloadLabelPadPercent,
                                Proto_GreyboxHudBuilder.ReloadCenterX + Proto_GreyboxHudBuilder.ReloadLabelPadPercent,
                                Proto_GreyboxHudBuilder.ReloadCenterY + Proto_GreyboxHudBuilder.ReloadLabelPadPercent);
                    }
                }
            }

            if (_reloadIcon != null)
            {
                var ic = _reloadIcon.color;
                ic.a = pulseReload ? 0.08f : 0.92f;
                _reloadIcon.color = ic;
            }

            if (_reloadDisc != null && pulseReload)
            {
                float pulse = 0.62f + 0.38f * Mathf.Sin(_simTime * 8f);
                _reloadDisc.color = new Color(
                    Proto_GreyboxPalette.HudActionDisc.r * pulse,
                    Proto_GreyboxPalette.HudActionDisc.g * pulse,
                    Proto_GreyboxPalette.HudActionDisc.b * pulse,
                    1f);
            }
            else if (_reloadDisc != null)
                _reloadDisc.color = Proto_GreyboxPalette.HudActionDisc;
        }

        void UpdateAimLineVisual()
        {
            bool show = _aim.IsDragging || _aimOverride.HasValue;
            if (!show)
            {
                if (_aimLine != null) _aimLine.gameObject.SetActive(false);
                if (_aimCone != null) _aimCone.gameObject.SetActive(false);
                if (_aimTipRing != null) _aimTipRing.gameObject.SetActive(false);
                return;
            }

            EnsureAimLineVisual();
            var foot = _motor.FootPosition;
            var end = ResolveAimVisualEnd();
            var delta = end - foot;
            float len = delta.magnitude;
            if (len < 0.05f)
            {
                _aimLine.gameObject.SetActive(false);
                _aimCone.gameObject.SetActive(false);
                return;
            }

            var dir = delta / len;
            int order = Proto_GreyboxLayout.CombatDrawSortingOrder(foot.y) + 2;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var fx = Proto_GreyboxFxV2Catalog.LoadRuntime();
            var lineSr = _aimLine.GetComponent<SpriteRenderer>();
            if (fx != null && fx.aimBand != null)
            {
                lineSr.sprite = fx.aimBand;
                lineSr.color = Color.white;
                _aimLine.gameObject.SetActive(true);
                _aimLine.position = new Vector3(foot.x, foot.y, 0f);
                float bandH = fx.aimBand.rect.height / Proto_GreyboxWestPixelCatalog.ImportPpu;
                float scaleY = len / Mathf.Max(bandH, 0.001f);
                float scaleX = (fx.aimBand.rect.width / Proto_GreyboxWestPixelCatalog.ImportPpu) * Proto_GreyboxFxV2Catalog.SmallFxWorldMul;
                _aimLine.localScale = new Vector3(scaleX, scaleY, 1f);
                _aimLine.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
                _aimCone.gameObject.SetActive(false);
            }
            else
            {
                var mid = foot + dir * (len * 0.5f);
                lineSr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
                _aimLine.gameObject.SetActive(true);
                _aimLine.position = new Vector3(mid.x, mid.y, 0f);
                _aimLine.localScale = new Vector3(len, 0.16f, 1f);
                _aimLine.rotation = Quaternion.Euler(0f, 0f, angle);
                lineSr.color = new Color(1f, 1f, 1f, 0.22f);
                _aimCone.gameObject.SetActive(true);
                _aimCone.position = new Vector3(foot.x, foot.y, 0f);
                _aimCone.localScale = new Vector3(0.42f, 0.26f, 1f);
                _aimCone.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }

            if (lineSr != null)
                lineSr.sortingOrder = order;
            EnsureAimTipRing();
            _aimTipRing.gameObject.SetActive(true);
            _aimTipRing.position = new Vector3(end.x, end.y, 0f);
            var tipSr = _aimTipRing.GetComponent<SpriteRenderer>();
            if (fx != null && fx.aimTip != null)
            {
                tipSr.sprite = fx.aimTip;
                tipSr.color = Color.white;
                float tipScale = (fx.aimTip.rect.width / Proto_GreyboxWestPixelCatalog.ImportPpu) * Proto_GreyboxFxV2Catalog.SmallFxWorldMul;
                _aimTipRing.localScale = new Vector3(tipScale, tipScale, 1f);
            }
            else
            {
                float tipScale = 0.22f + 0.08f * Proto_DepthScale.ScaleAt(end.y);
                _aimTipRing.localScale = new Vector3(tipScale, tipScale, 1f);
            }

            if (tipSr != null)
                tipSr.sortingOrder = order + 1;
        }

        void EnsureAimTipRing()
        {
            if (_aimTipRing != null) return;
            var parent = transform.parent != null ? transform.parent : transform;
            var go = new GameObject("Greybox_AimTip");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
            sr.color = new Color(1f, 1f, 1f, 0.45f);
            _aimTipRing = go.transform;
        }

        void TickGrenadeInput(Proto_GreyboxInput input, Vector2 foot, IList<Proto_GreyboxTarget> aimTargets)
        {
            bool blockedTap = (input.GrenadePressed || input.GrenadeStickHeld) && !_grenade.CanThrow(_simTime);
            if (blockedTap && !_grenadeBlockedTapPrev)
                _grenadeGaugeHud.TriggerChargeShake();
            _grenadeBlockedTapPrev = blockedTap;

            _grenadeStickActive = input.GrenadeStickHeld && _grenade.CanThrow(_simTime);
            if (input.GrenadeStickHeld)
            {
                float stickR = Proto_GreyboxPointerMath.AimStickRadiusPx(Screen.height,
                    _tuning.AimStickRadiusHeightPercent);
                _grenadePeakScreenMag = Mathf.Max(_grenadePeakScreenMag, input.GrenadeStickDragWorld.magnitude);
                _grenadePreviewLanding = Proto_GreyboxGrenadeAim.LandingFromScreenDrag(foot,
                    input.GrenadeStickDragWorld, Screen.width, Screen.height, stickR);
            }
            else if (!input.GrenadeStickReleased)
                _grenadePeakScreenMag = 0f;

            if (input.GrenadeStickReleased && _grenade.CanThrow(_simTime))
            {
                float stickR = Proto_GreyboxPointerMath.AimStickRadiusPx(Screen.height,
                    _tuning.AimStickRadiusHeightPercent);
                var intent = Proto_GreyboxPointerMath.AimStickReleaseIntent(input.GrenadeStickDragWorld.magnitude,
                    _grenadePeakScreenMag, stickR, Proto_GreyboxPointerMath.AimStickDeadZonePortion,
                    Proto_GreyboxPointerMath.AimStickDragThresholdPortion);
                _grenadePeakScreenMag = 0f;
                if (intent != Proto_GreyboxAimReleaseIntent.Cancel)
                {
                    var landing = intent == Proto_GreyboxAimReleaseIntent.AutoAim
                        ? Proto_GreyboxGrenadeAim.AutoThrowLanding(foot, aimTargets)
                        : _grenadePreviewLanding;
                    _grenade.TryThrow(_simTime, foot, landing);
                }
            }

            if (input.GrenadePressed && _grenade.CanThrow(_simTime))
            {
                var landing = Proto_GreyboxGrenadeAim.AutoThrowLanding(foot, aimTargets);
                _grenade.TryThrow(_simTime, foot, landing);
            }
        }

        void UpdateGrenadePreviewVisual()
        {
            bool show = _grenadeStickActive && _grenade.CanThrow(_simTime);
            if (!show)
            {
                HideGrenadeArcDots();
                if (_grenadeArcLine != null) _grenadeArcLine.gameObject.SetActive(false);
                if (_grenadeLandingMarker != null) _grenadeLandingMarker.gameObject.SetActive(false);
                return;
            }

            EnsureGrenadePreviewVisual();
            var foot = _motor.FootPosition;
            var end = _grenadePreviewLanding;
            var delta = end - foot;
            float len = delta.magnitude;
            if (len < 0.08f)
            {
                HideGrenadeArcDots();
                _grenadeArcLine.gameObject.SetActive(false);
                _grenadeLandingMarker.gameObject.SetActive(false);
                return;
            }

            int order = Proto_GreyboxLayout.CombatDrawSortingOrder(foot.y) + 4;
            var fx = Proto_GreyboxFxV2Catalog.LoadRuntime();
            _grenadeArcLine.gameObject.SetActive(false);
            EnsureGrenadeArcDots(12);
            const int dotCount = 12;
            for (int i = 0; i < dotCount; i++)
            {
                float t = (i + 0.5f) / dotCount;
                var flat = Vector2.Lerp(foot, end, t);
                float lift = Proto_GreyboxTuning.GrenadeArcVisualHeight * 4f * t * (1f - t);
                var pos = flat + new Vector2(0f, lift);
                var dot = _grenadeArcDots[i];
                dot.gameObject.SetActive(true);
                dot.position = new Vector3(pos.x, pos.y, 0f);
                var sr = dot.GetComponent<SpriteRenderer>();
                if (fx != null && fx.aimBombDot != null)
                {
                    sr.sprite = fx.aimBombDot;
                    sr.color = new Color(1f, 1f, 1f, 0.45f + 0.35f * t);
                    float s = (fx.aimBombDot.rect.width / Proto_GreyboxWestPixelCatalog.ImportPpu) * Proto_GreyboxFxV2Catalog.SmallFxWorldMul;
                    dot.localScale = new Vector3(s, s, 1f);
                }
                else
                {
                    float s = 0.05f + 0.03f * t;
                    dot.localScale = new Vector3(s, s, 1f);
                    sr.color = new Color(0.95f, 0.62f, 0.2f, 0.35f + 0.25f * t);
                }

                if (sr != null)
                    sr.sortingOrder = order;
            }

            float landR = Proto_GreyboxTuning.GrenadeRadius * 1.15f;
            float depth = Proto_GreyboxPerspectiveDepth.Enabled
                ? Proto_GreyboxPerspectiveDepth.EnemyVisualScaleAtFootWorldY(end.y)
                : Proto_DepthScale.ScaleAt(end.y);
            _grenadeLandingMarker.gameObject.SetActive(true);
            _grenadeLandingMarker.position = new Vector3(end.x, end.y, 0f);
            var landSr = _grenadeLandingMarker.GetComponent<SpriteRenderer>();
            if (fx != null && fx.aimBombLand != null)
            {
                landSr.sprite = fx.aimBombLand;
                float w = fx.aimBombLand.rect.width / Proto_GreyboxWestPixelCatalog.ImportPpu * depth * Proto_GreyboxFxV2Catalog.SmallFxWorldMul;
                float h = fx.aimBombLand.rect.height / Proto_GreyboxWestPixelCatalog.ImportPpu * depth * Proto_GreyboxFxV2Catalog.SmallFxWorldMul;
                _grenadeLandingMarker.localScale = new Vector3(w, h, 1f);
                landSr.color = new Color(1f, 1f, 1f, 0.72f);
            }
            else
            {
                float perspY = landR * 2f * 0.45f;
                _grenadeLandingMarker.localScale = new Vector3(landR * 2f, perspY, 1f);
                landSr.color = new Color(0.95f, 0.45f, 0.12f, 0.55f);
            }

            if (landSr != null)
                landSr.sortingOrder = order + 1;
        }

        void EnsureGrenadeArcDots(int count)
        {
            while (_grenadeArcDots.Count < count)
            {
                var parent = transform.parent != null ? transform.parent : transform;
                var go = new GameObject("Greybox_GrenadeArcDot");
                go.transform.SetParent(parent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                _grenadeArcDots.Add(go.transform);
            }
        }

        void HideGrenadeArcDots()
        {
            foreach (var d in _grenadeArcDots)
            {
                if (d != null)
                    d.gameObject.SetActive(false);
            }
        }

        void TickGrenadeFlightVisual()
        {
            if (!_grenade.IsFlying)
            {
                if (_grenadeFlyVis != null) _grenadeFlyVis.gameObject.SetActive(false);
                if (_grenadeFlyShadow != null) _grenadeFlyShadow.gameObject.SetActive(false);
                if (_grenadeFlySmoke != null) _grenadeFlySmoke.gameObject.SetActive(false);
                return;
            }

            EnsureGrenadeFlyVisual();
            float t = _grenade.FlightProgress01(_simTime);
            var arcPos = _grenade.ArcVisualGround(_simTime);
            var shadowPos = Vector2.Lerp(_grenade.StartGround, _grenade.AimGround, t);
            int order = Proto_GreyboxLayout.CombatDrawSortingOrder(arcPos.y) + 6;
            float depth = Proto_GreyboxPerspectiveDepth.Enabled
                ? Proto_GreyboxPerspectiveDepth.EnemyVisualScaleAtFootWorldY(arcPos.y)
                : Proto_DepthScale.ScaleAt(arcPos.y);
            var fx = Proto_GreyboxFxV2Catalog.LoadRuntime();
            var dyn = fx?.dynamiteSpin;
            _grenadeFlyVis.gameObject.SetActive(true);
            _grenadeFlyVis.position = new Vector3(arcPos.x, arcPos.y, 0f);
            var dynSr = _grenadeFlyVis.GetComponent<SpriteRenderer>();
            if (dyn != null && dyn.Length > 0)
            {
                int f = Mathf.FloorToInt(_simTime * 16f) % dyn.Length;
                dynSr.sprite = dyn[f];
                dynSr.color = Color.white;
                // Sprite already has PPU 100; scale is a depth multiplier, with a floor so mid-arc stays readable.
                float mul = Mathf.Max(depth * 1.2f, 0.85f);
                _grenadeFlyVis.localScale = new Vector3(mul, mul, 1f);
                _grenadeFlyVis.rotation = Quaternion.identity;
            }
            else
            {
                float spin = _simTime * 720f;
                _grenadeFlyVis.localScale = new Vector3(0.22f * depth, 0.22f * depth, 1f);
                _grenadeFlyVis.rotation = Quaternion.Euler(0f, 0f, spin);
            }

            if (dynSr != null)
                dynSr.sortingOrder = order;

            float shadowA = 0.2f + 0.45f * t;
            float shadowS = 0.18f + 0.22f * t;
            _grenadeFlyShadow.gameObject.SetActive(true);
            _grenadeFlyShadow.position = new Vector3(shadowPos.x, shadowPos.y, 0f);
            var shSr = _grenadeFlyShadow.GetComponent<SpriteRenderer>();
            if (fx != null && fx.bombShadow != null)
            {
                shSr.sprite = fx.bombShadow;
                float sw = (fx.bombShadow.rect.width / Proto_GreyboxWestPixelCatalog.ImportPpu) * (0.6f + 0.4f * t) * depth * Proto_GreyboxFxV2Catalog.SmallFxWorldMul;
                float sh = (fx.bombShadow.rect.height / Proto_GreyboxWestPixelCatalog.ImportPpu) * (0.6f + 0.4f * t) * depth * Proto_GreyboxFxV2Catalog.SmallFxWorldMul;
                _grenadeFlyShadow.localScale = new Vector3(sw, sh, 1f);
                shSr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.5f, 1f, t));
            }
            else
            {
                _grenadeFlyShadow.localScale = new Vector3(shadowS, shadowS * 0.45f, 1f);
                shSr.color = new Color(0f, 0f, 0f, shadowA);
            }

            shSr.sortingOrder = order - 2;
            var puffs = fx?.smokePuffs;
            _grenadeFlySmoke.gameObject.SetActive(puffs != null && puffs.Length > 0);
            if (puffs != null && puffs.Length > 0)
            {
                int puffIdx = Mathf.FloorToInt(_simTime * 20f) % puffs.Length;
                var smSr = _grenadeFlySmoke.GetComponent<SpriteRenderer>();
                smSr.sprite = puffs[puffIdx];
                float grow = 1f + 0.15f * Mathf.Sin(_simTime * 6f);
                float psz = (puffs[puffIdx].rect.width / Proto_GreyboxWestPixelCatalog.ImportPpu) * grow * depth;
                _grenadeFlySmoke.position = _grenadeFlyVis.position;
                _grenadeFlySmoke.localScale = new Vector3(psz, psz, 1f);
                smSr.sortingOrder = order - 1;
                smSr.color = new Color(1f, 1f, 1f, 0.55f);
            }
        }

        void EnsureGrenadeFlyVisual()
        {
            if (_grenadeFlyVis != null) return;
            var parent = transform.parent != null ? transform.parent : transform;
            _grenadeFlyVis = CreateFxSprite(parent, "Greybox_GrenadeFly", new Color(0.45f, 0.22f, 0.12f, 1f));
            _grenadeFlyShadow = CreateFxSprite(parent, "Greybox_GrenadeShadow", new Color(0f, 0f, 0f, 0.4f));
            _grenadeFlySmoke = CreateFxSprite(parent, "Greybox_GrenadeSmoke", new Color(0.6f, 0.58f, 0.55f, 0.4f));
        }

        static Transform CreateFxSprite(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            sr.color = color;
            return go.transform;
        }

        void UpdateDizzyVisual()
        {
            var phase = _rollDizzy.Phase;
            if (phase == Proto_GreyboxRollDizzyPhase.None)
            {
                foreach (var s in _dizzyStars)
                {
                    if (s != null)
                        s.gameObject.SetActive(false);
                }

                _westSkin?.SyncDizzyPhase(phase, 0f);
                return;
            }

            int want = phase == Proto_GreyboxRollDizzyPhase.Stun ? 5 : 2;
            while (_dizzyStars.Count < want)
            {
                var go = new GameObject("Greybox_DizzyStar");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                sr.color = new Color(1f, 0.92f, 0.35f, 0.9f);
                _dizzyStars.Add(go.transform);
            }

            _westSkin?.SyncDizzyPhase(phase, _rollDizzy.PhaseElapsed(_simTime));
            var fx = Proto_GreyboxFxV2Catalog.LoadRuntime();
            bool designerOrbit = fx != null && (fx.dizzyStar != null || fx.dizzyBird != null);
            var foot = _motor.FootPosition;
            float bodyH = Proto_GreyboxPerspectiveDepth.PlayerBodyHeightWorld(foot.y);
            float fxScale = Proto_GreyboxPerspectiveDepth.PlayerVisualScaleAtFootWorldY(foot.y);
            for (int i = 0; i < _dizzyStars.Count; i++)
            {
                var star = _dizzyStars[i];
                if (i >= want)
                {
                    star.gameObject.SetActive(false);
                    continue;
                }

                star.gameObject.SetActive(true);
                float ang = _simTime * (phase == Proto_GreyboxRollDizzyPhase.Stun ? 420f : 180f) + i * 72f;
                float rad = designerOrbit ? 0.14f + (i % 2) * 0.05f : 0.12f + (i % 2) * 0.06f;
                var off = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * rad;
                star.position = new Vector3(foot.x + off.x, foot.y + bodyH * (designerOrbit ? 0.95f : 0.92f) + off.y * 0.3f, 0f);
                float s = designerOrbit
                    ? 0.12f * Proto_GreyboxFxV2Catalog.SmallFxWorldMul
                    : (phase == Proto_GreyboxRollDizzyPhase.Stun ? 0.09f : 0.07f);
                star.localScale = new Vector3(s * fxScale, s * fxScale, 1f);
                star.rotation = designerOrbit ? Quaternion.identity : Quaternion.Euler(0f, 0f, ang);
                var sr = star.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = Proto_GreyboxLayout.WorldSpriteSortingOrder(foot.y) + 8;
                    if (designerOrbit)
                    {
                        sr.sprite = (i % 2 == 0 ? fx.dizzyStar : fx.dizzyBird) ?? fx.dizzyStar ??
                                    Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                        sr.color = Color.white;
                        sr.flipX = off.x < 0f && fx.dizzyBird != null && i % 2 == 1;
                    }
                }
            }
        }

        void EnsureGrenadePreviewVisual()
        {
            if (_grenadeArcLine != null) return;
            var parent = transform.parent != null ? transform.parent : transform;
            var arcGo = new GameObject("Greybox_GrenadeArc");
            arcGo.transform.SetParent(parent, false);
            var arcSr = arcGo.AddComponent<SpriteRenderer>();
            arcSr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            arcSr.color = new Color(0.95f, 0.55f, 0.15f, 0.62f);
            _grenadeArcLine = arcGo.transform;

            var landGo = new GameObject("Greybox_GrenadeLanding");
            landGo.transform.SetParent(parent, false);
            var landSr = landGo.AddComponent<SpriteRenderer>();
            landSr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
            landSr.color = new Color(0.95f, 0.45f, 0.12f, 0.72f);
            _grenadeLandingMarker = landGo.transform;
        }

        void EnsureAimLineVisual()
        {
            if (_aimLine != null) return;
            var parent = transform.parent != null ? transform.parent : transform;
            var lineGo = new GameObject("Greybox_AimLine");
            lineGo.transform.SetParent(parent, false);
            var lineSr = lineGo.AddComponent<SpriteRenderer>();
            lineSr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            lineSr.color = new Color(1f, 0.92f, 0.45f, 0.55f);
            _aimLine = lineGo.transform;

            var coneGo = new GameObject("Greybox_AimCone");
            coneGo.transform.SetParent(parent, false);
            var coneSr = coneGo.AddComponent<SpriteRenderer>();
            coneSr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            coneSr.color = new Color(1f, 0.92f, 0.45f, 0.2f);
            _aimCone = coneGo.transform;
        }

        void TickExplosionVis()
        {
            if (_grenade.ExplosionVisible(_simTime))
            {
                if (_explosionVis == null)
                {
                    var parent = transform.parent != null ? transform.parent : transform;
                    _explosionVis = new GameObject("Greybox_GrenadeExplosion");
                    _explosionVis.transform.SetParent(parent, false);
                    _explosionVis.AddComponent<SpriteRenderer>();
                }

                float t = _grenade.ExplosionFadeT(_simTime);
                var pos = _grenade.VisualGround;
                float depth = Proto_GreyboxPerspectiveDepth.Enabled
                    ? Proto_GreyboxPerspectiveDepth.EnemyVisualScaleAtFootWorldY(pos.y)
                    : Proto_DepthScale.ScaleAt(pos.y);
                float perspMul = Proto_GreyboxPerspectiveDepth.Enabled
                    ? Proto_GreyboxPerspectiveDepth.EnemyBodyHeightWorld(pos.y) /
                      Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.TargetHeightPercent)
                    : 1f;
                int order = Proto_GreyboxLayout.CombatDrawSortingOrder(pos.y);
                _explosionVis.transform.position = new Vector3(pos.x, pos.y, 0f);
                var expSr = _explosionVis.GetComponent<SpriteRenderer>();
                var fx = Proto_GreyboxFxV2Catalog.LoadRuntime();
                if (fx != null && fx.explosion != null && fx.explosion.Length > 0)
                {
                    float elapsed = (1f - t) * (fx.explosion.Length / 16f);
                    int idx = Mathf.Clamp(Mathf.FloorToInt(elapsed * 16f), 0, fx.explosion.Length - 1);
                    expSr.sprite = fx.explosion[idx];
                    float h = fx.explosion[idx].rect.height / Proto_GreyboxWestPixelCatalog.ImportPpu;
                    float scale = (perspMul * 1.2f * depth * Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.TargetHeightPercent))
                        / Mathf.Max(h, 0.001f);
                    _explosionVis.transform.localScale = new Vector3(scale, scale, 1f);
                    expSr.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t));
                }
                else
                {
                    float r = _grenade.ExplosionRadius * (0.35f + t * 0.65f);
                    expSr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                    float diam = Mathf.Max(r * 2f * depth, Proto_GreyboxLayout.MinAllyBulletWorldDiameter * 2f);
                    _explosionVis.transform.localScale = new Vector3(diam, diam, 1f);
                    expSr.color = new Color(1f, 0.55f, 0.1f, Mathf.Lerp(0.75f, 0f, t));
                }

                if (expSr != null)
                    expSr.sortingOrder = order;
                _explosionVis.SetActive(true);
            }
            else if (_explosionVis != null)
                _explosionVis.SetActive(false);
        }

        void ApplyTransform(float dt)
        {
            var foot = _motor.FootPosition;
            float wob = 0f;
            bool fxDizzy = _westSkin != null && _westSkin.IsActive && _westSkin.UsesFxV2DizzyBody;
            if (!fxDizzy)
            {
                if (_rollDizzy.Phase == Proto_GreyboxRollDizzyPhase.Light)
                    wob = Mathf.Sin(_simTime * 6f) * Proto_GreyboxTuning.RollDizzyWobbleDeg * Mathf.Deg2Rad;
                else if (_rollDizzy.Phase == Proto_GreyboxRollDizzyPhase.Stun)
                    wob = Mathf.Sin(_simTime * 9f) * Proto_GreyboxTuning.RollDizzyStunWobbleDeg * Mathf.Deg2Rad;
            }
            float cx = foot.x + (wob != 0f ? Mathf.Sin(wob) * 0.04f : 0f);
            transform.position = new Vector3(cx, foot.y, 0f);
            if (wob != 0f)
                transform.rotation = Quaternion.Euler(0f, 0f, wob * Mathf.Rad2Deg);
            else
                transform.rotation = Quaternion.identity;
            if (_westSkin != null && _westSkin.IsActive)
            {
                if (_sprite != null)
                {
                    _sprite.sortingOrder = Proto_GreyboxLayout.WorldSpriteSortingOrder(foot.y);
                    if (_hitFlashTimer > 0f)
                    {
                        _hitFlashTimer -= dt;
                        _sprite.color = Color.white;
                    }
                    else if (_hearts.IsInvulnerable && !_roll.IsInvulnerable)
                    {
                        bool blinkOn = Mathf.PingPong(_simTime * 14f, 1f) > 0.45f;
                        _sprite.color = blinkOn
                            ? _baseColor
                            : new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.42f);
                    }
                    else
                    {
                        _sprite.color = _roll.IsRolling
                            ? new Color(_baseColor.r + 0.12f, _baseColor.g + 0.12f, _baseColor.b + 0.12f, 0.78f)
                            : _baseColor;
                    }
                }

                return;
            }

            float targetH = Proto_GreyboxPerspectiveDepth.PlayerBodyHeightWorld(foot.y);
            var rollVisual = _roll.IsRolling;
            float sy = rollVisual ? 0.6f : 1f;
            if (_sprite != null && _sprite.sprite != null)
            {
                var uniform = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(_sprite.sprite, targetH);
                transform.localScale = new Vector3(uniform.x, uniform.y * sy, 1f);
            }
            else
            {
                float scale = Proto_GreyboxPerspectiveDepth.PlayerVisualScaleAtFootWorldY(foot.y);
                float targetW = Proto_GreyboxLayout.WorldWidth * (Proto_GreyboxTuning.PlayerCapsuleWidthPercent / 100f) * scale;
                float baseH = targetH / scale;
                float baseW = targetW / scale;
                transform.localScale = new Vector3(baseW * scale, baseH * scale * sy, 1f);
            }

            if (_sprite != null)
            {
                _sprite.sortingOrder = Proto_GreyboxLayout.WorldSpriteSortingOrder(foot.y);
                if (_hitFlashTimer > 0f)
                {
                    _hitFlashTimer -= dt;
                    _sprite.color = Color.white;
                }
                else if (_hearts.IsInvulnerable && !_roll.IsInvulnerable)
                {
                    bool blinkOn = Mathf.PingPong(_simTime * 14f, 1f) > 0.45f;
                    _sprite.color = blinkOn
                        ? _baseColor
                        : new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.42f);
                }
                else
                {
                    _sprite.color = rollVisual
                        ? new Color(_baseColor.r + 0.12f, _baseColor.g + 0.12f, _baseColor.b + 0.12f, 0.78f)
                        : _baseColor;
                }
            }
        }

        Proto_GreyboxInput PollLegacyInput()
        {
            if (_runResult != Proto_GreyboxRunResult.Playing)
                return PollResultInput();

            if (IsPaused)
                return PollPausedInput();

            bool pauseToggled = PollPauseToggleTap();
            if (pauseToggled)
            {
                SetPaused(true);
                Proto_GreyboxHudBuilder.SetJoystickKnob(_joystickKnob, Vector2.zero);
                return FilterCombatInputWhenPaused(default, true);
            }

            if (PollSettingsGearTap())
            {
                OpenSettings(true);
                Proto_GreyboxHudBuilder.SetJoystickKnob(_joystickKnob, Vector2.zero);
                return FilterCombatInputWhenPaused(default, true);
            }

            var move = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1f;
            if (move.sqrMagnitude > 1f)
                move.Normalize();

            var touchMove = PollTouchJoystick();
            if (touchMove.sqrMagnitude > move.sqrMagnitude)
                move = touchMove;

            bool roll = Input.GetKey(KeyCode.Space) || PollTouchButton(Proto_GreyboxHudBuilder.RollButtonName);
            bool reload = Input.GetKeyDown(KeyCode.R) || PollTouchButton(Proto_GreyboxHudBuilder.ReloadButtonName);
            bool grenadeTap = Input.GetKeyDown(KeyCode.G);
            bool debugToggle = Input.GetKeyDown(KeyCode.F1) || PollTouchDbgButton();
            bool fireQuick = Input.GetKeyDown(KeyCode.J);

            var fireStick = PollTouchFireStick();
            var grenadeStick = PollTouchGrenadeStick();
            var mouseAim = PollMouseAim();

            Proto_GreyboxHudBuilder.SetJoystickKnob(_joystickKnob, move);
            return new Proto_GreyboxInput
            {
                Move = move,
                RollPressed = roll,
                FireQuickAuto = fireQuick,
                FireStickHeld = fireStick.Held,
                FireStickReleased = fireStick.Released,
                FireStickDragWorld = fireStick.DragWorld,
                MouseAimHeld = mouseAim.Held,
                MouseAimReleased = mouseAim.Released,
                MouseAimWorld = mouseAim.PointerWorld,
                MousePressWorld = mouseAim.PressWorld,
                ReloadPressed = reload,
                GrenadePressed = grenadeTap,
                GrenadeStickHeld = grenadeStick.Held,
                GrenadeStickReleased = grenadeStick.Released,
                GrenadeStickDragWorld = grenadeStick.DragWorld,
                DebugToggle = debugToggle,
                RestartPressed = false
            };
        }

        Proto_GreyboxInput PollResultInput()
        {
            bool restart = Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return)
                           || Input.GetKeyDown(KeyCode.KeypadEnter) || PollResultRestartTap();
            Proto_GreyboxHudBuilder.SetJoystickKnob(_joystickKnob, Vector2.zero);
            return new Proto_GreyboxInput { RestartPressed = restart };
        }

        Proto_GreyboxInput PollPausedInput()
        {
            if (_settingsUi.IsVisible)
            {
                if (PollSettingsCloseTap())
                    CloseSettings();
            }
            else if (PollPauseOverlayInput(out bool resume, out bool restart, out bool settings))
            {
                if (resume)
                    SetPaused(false);
                else if (settings)
                    OpenSettings(false);
                else if (restart)
                    FullRestart();
            }
            else if (PollPauseToggleTap())
                SetPaused(false);

            Proto_GreyboxHudBuilder.SetJoystickKnob(_joystickKnob, Vector2.zero);
            return FilterCombatInputWhenPaused(default, true);
        }

        bool PollPauseToggleTap()
        {
            int n = GatherPointers(_pointerBuf);
            for (int i = 0; i < n; i++)
            {
                if (!_pointerBuf[i].Began) continue;
                PointerPercent(_pointerBuf[i].Pos, out float xPct, out float yPct);
                if (Proto_GreyboxPauseUi.IsPauseToggleHit(xPct, yPct)) return true;
            }

            return false;
        }

        bool PollPauseOverlayInput(out bool resume, out bool restart, out bool settings)
        {
            resume = false;
            restart = false;
            settings = false;
            int n = GatherPointers(_pointerBuf);
            for (int i = 0; i < n; i++)
            {
                if (!_pointerBuf[i].Began) continue;
                PointerPercent(_pointerBuf[i].Pos, out float xPct, out float yPct);
                if (Proto_GreyboxPauseUi.IsResumeHit(xPct, yPct))
                {
                    resume = true;
                    return true;
                }

                if (Proto_GreyboxPauseUi.IsPauseSettingsHit(xPct, yPct))
                {
                    settings = true;
                    return true;
                }

                if (Proto_GreyboxPauseUi.IsPauseRestartHit(xPct, yPct))
                {
                    restart = true;
                    return true;
                }
            }

            return false;
        }

        bool PollSettingsCloseTap()
        {
            int n = GatherPointers(_pointerBuf);
            for (int i = 0; i < n; i++)
            {
                if (!_pointerBuf[i].Began) continue;
                PointerPercent(_pointerBuf[i].Pos, out float xPct, out float yPct);
                if (_settingsUi.TryCloseFromTap(xPct, yPct))
                    return true;
            }

            return false;
        }

        bool PollSettingsGearTap()
        {
            int n = GatherPointers(_pointerBuf);
            for (int i = 0; i < n; i++)
            {
                if (!_pointerBuf[i].Began) continue;
                PointerPercent(_pointerBuf[i].Pos, out float xPct, out float yPct);
                if (Proto_GreyboxHudBuilder.CircleHitTest(xPct, yPct, Proto_GreyboxHudBuilder.SettingsGearCenterX,
                        Proto_GreyboxHudBuilder.SettingsGearCenterY, Proto_GreyboxHudBuilder.SettingsGearRadiusPercent))
                    return true;
            }

            return false;
        }

        bool PollResultRestartTap()
        {
            int n = GatherPointers(_pointerBuf);
            for (int i = 0; i < n; i++)
            {
                if (!_pointerBuf[i].Began) continue;
                PointerPercent(_pointerBuf[i].Pos, out float xPct, out float yPct);
                if (Proto_GreyboxResultUi.IsRestartHit(xPct, yPct)) return true;
            }

            return false;
        }

        bool PollTouchDbgButton()
        {
            int n = GatherPointers(_pointerBuf);
            for (int i = 0; i < n; i++)
            {
                if (!_pointerBuf[i].Began) continue;
                PointerPercent(_pointerBuf[i].Pos, out float xPct, out float yPct);
                if (IsDbgHit(xPct, yPct)) return true;
            }

            return false;
        }

        struct FireStickPoll
        {
            public bool Held;
            public bool Released;
            public Vector2 DragWorld;
        }

        struct MouseAimPoll
        {
            public bool Held;
            public bool Released;
            public Vector2 PointerWorld;
            public Vector2 PressWorld;
        }

        struct PointerEvent
        {
            public int Id;
            public Vector2 Pos;
            public bool Began;
            public bool Held;
            public bool Ended;
        }

        readonly PointerEvent[] _pointerBuf = new PointerEvent[16];
        static readonly PointerEvent[] SharedPointerBuf = new PointerEvent[16];

        static int GatherPointers(PointerEvent[] buf)
        {
            if (Input.touchCount > 0)
            {
                int n = Mathf.Min(Input.touchCount, buf.Length);
                for (int i = 0; i < n; i++)
                {
                    var t = Input.GetTouch(i);
                    buf[i].Id = t.fingerId;
                    buf[i].Pos = t.position;
                    buf[i].Began = t.phase == TouchPhase.Began;
                    buf[i].Held = t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved ||
                                  t.phase == TouchPhase.Stationary;
                    buf[i].Ended = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                }

                return n;
            }

            bool down = Input.GetMouseButtonDown(0);
            bool held = Input.GetMouseButton(0);
            bool up = Input.GetMouseButtonUp(0);
            if (!down && !held && !up) return 0;
            buf[0].Id = -1;
            buf[0].Pos = Input.mousePosition;
            buf[0].Began = down;
            buf[0].Held = held;
            buf[0].Ended = up;
            return 1;
        }

        static void PointerPercent(Vector2 pos, out float xPct, out float yPct) =>
            Proto_GreyboxPointerMath.PointerPercentFromScreen(pos, Screen.width, Screen.height, out xPct, out yPct);

        static bool IsDbgHit(float xPct, float yPct) =>
            xPct >= 3.2f && xPct <= 8.6f && yPct >= 8.6f && yPct <= 11.4f;

        static bool IsJoystickHit(float xPct, float yPct) =>
            xPct >= Proto_GreyboxHudBuilder.JoystickHitX0 && xPct <= Proto_GreyboxHudBuilder.JoystickHitX1
            && yPct >= Proto_GreyboxHudBuilder.JoystickHitY0 && yPct <= Proto_GreyboxHudBuilder.JoystickHitY1;

        static bool IsFireHit(float xPct, float yPct) => Proto_GreyboxCombatInputGate.FireCircleHit(xPct, yPct);

        FireStickPoll PollTouchFireStick()
        {
            var result = new FireStickPoll();
            int n = GatherPointers(_pointerBuf);
            PointerEvent? tracked = null;
            for (int i = 0; i < n; i++)
            {
                var p = _pointerBuf[i];
                PointerPercent(p.Pos, out float xPct, out float yPct);
                if (_fireStickFingerId == -2 && p.Began && IsFireHit(xPct, yPct))
                {
                    _fireStickTracking = true;
                    _fireStickFingerId = p.Id;
                    _fireStickStartScreen = p.Pos;
                    tracked = p;
                    break;
                }

                if (_fireStickTracking && p.Id == _fireStickFingerId)
                {
                    tracked = p;
                    break;
                }
            }

            if (!_fireStickTracking) return result;
            if (tracked == null)
            {
                _fireStickTracking = false;
                _fireStickFingerId = -2;
                return result;
            }

            var ev = tracked.Value;
            if (ev.Held)
            {
                result.Held = true;
                result.DragWorld = ev.Pos - _fireStickStartScreen;
            }

            if (ev.Ended)
            {
                result.Released = true;
                result.DragWorld = ev.Pos - _fireStickStartScreen;
                _fireStickTracking = false;
                _fireStickFingerId = -2;
            }

            return result;
        }

        FireStickPoll PollTouchGrenadeStick()
        {
            var result = new FireStickPoll();
            int n = GatherPointers(_pointerBuf);
            PointerEvent? tracked = null;
            for (int i = 0; i < n; i++)
            {
                var p = _pointerBuf[i];
                PointerPercent(p.Pos, out float xPct, out float yPct);
                if (_grenadeStickFingerId == -2 && p.Began
                    && Proto_GreyboxCombatInputGate.GrenadeCircleHit(xPct, yPct))
                {
                    _grenadeStickTracking = true;
                    _grenadeStickFingerId = p.Id;
                    _grenadeStickStartScreen = p.Pos;
                    tracked = p;
                    break;
                }

                if (_grenadeStickTracking && p.Id == _grenadeStickFingerId)
                {
                    tracked = p;
                    break;
                }
            }

            if (!_grenadeStickTracking) return result;
            if (tracked == null)
            {
                _grenadeStickTracking = false;
                _grenadeStickFingerId = -2;
                return result;
            }

            var ev = tracked.Value;
            if (ev.Held)
            {
                result.Held = true;
                result.DragWorld = ev.Pos - _grenadeStickStartScreen;
            }

            if (ev.Ended)
            {
                result.Released = true;
                result.DragWorld = ev.Pos - _grenadeStickStartScreen;
                _grenadeStickTracking = false;
                _grenadeStickFingerId = -2;
            }

            return result;
        }

        MouseAimPoll PollMouseAim()
        {
            var result = new MouseAimPoll();
            if (Input.touchCount > 0) return result;

            Vector2 pos = Input.mousePosition;
            if (Input.GetMouseButtonDown(0) && !IsPointerOnHudButton(pos))
            {
                _mouseAimTracking = true;
                _mousePressWorld = ScreenToGroundWorld(pos);
            }

            if (_mouseAimTracking && Input.GetMouseButton(0))
            {
                result.Held = true;
                result.PointerWorld = ScreenToGroundWorld(pos);
                result.PressWorld = _mousePressWorld;
            }

            if (_mouseAimTracking && Input.GetMouseButtonUp(0))
            {
                result.Released = true;
                result.PointerWorld = ScreenToGroundWorld(pos);
                result.PressWorld = _mousePressWorld;
                _mouseAimTracking = false;
            }

            return result;
        }

        static Vector2 ScreenDeltaToWorldDrag(Vector2 deltaScreen) =>
            Proto_GreyboxPointerMath.ScreenDeltaToWorldDrag(deltaScreen, Screen.width, Screen.height);

        static Vector2 ScreenToGroundWorld(Vector2 screenPos) =>
            Proto_GreyboxPointerMath.ScreenToGroundWorld(screenPos, Screen.width, Screen.height);

        static bool IsPointerOnHudButton(Vector2? posOverride = null)
        {
            Vector2 pos = posOverride ?? (Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition);
            PointerPercent(pos, out float xPct, out float yPct);
            if (Proto_GreyboxCombatInputGate.RollCircleHit(xPct, yPct)) return true;
            if (IsFireHit(xPct, yPct)) return true;
            if (Proto_GreyboxCombatInputGate.ReloadCircleHit(xPct, yPct)) return true;
            if (Proto_GreyboxCombatInputGate.GrenadeCircleHit(xPct, yPct)) return true;
            if (IsJoystickHit(xPct, yPct)) return true;
            if (Proto_GreyboxPauseUi.IsPauseToggleHit(xPct, yPct)) return true;
            if (IsDbgHit(xPct, yPct)) return true;
            return false;
        }

        Vector2 PollTouchJoystick()
        {
            var analog = Vector2.zero;
            int n = GatherPointers(_pointerBuf);
            PointerEvent? tracked = null;
            for (int i = 0; i < n; i++)
            {
                var p = _pointerBuf[i];
                PointerPercent(p.Pos, out float xPct, out float yPct);
                if (_joyFingerId == -2 && Proto_GreyboxCombatInputGate.JoystickCaptureOnBegan(xPct, yPct, p.Began))
                {
                    _joyFingerId = p.Id;
                    tracked = p;
                    break;
                }

                if (_joyFingerId != -2 && p.Id == _joyFingerId)
                {
                    tracked = p;
                    break;
                }
            }

            if (_joyFingerId != -2 && tracked == null)
            {
                _joyFingerId = -2;
                return analog;
            }

            if (tracked == null) return analog;
            var ev = tracked.Value;
            if (ev.Ended)
            {
                _joyFingerId = -2;
                return analog;
            }

            analog = Proto_GreyboxPointerMath.TouchJoystickAnalog(ev.Pos, Screen.width, Screen.height);
            return analog;
        }

        static bool PollTouchButton(string goName)
        {
            int n = GatherPointers(SharedPointerBuf);
            for (int i = 0; i < n; i++)
            {
                if (!SharedPointerBuf[i].Began) continue;
                PointerPercent(SharedPointerBuf[i].Pos, out float xPct, out float yPct);
                if (goName == Proto_GreyboxHudBuilder.RollButtonName)
                {
                    if (Proto_GreyboxCombatInputGate.RollCircleHit(xPct, yPct)) return true;
                }
                else if (goName == Proto_GreyboxHudBuilder.FireButtonName)
                {
                    if (IsFireHit(xPct, yPct)) return true;
                }
                else if (goName == Proto_GreyboxHudBuilder.ReloadButtonName)
                {
                    if (Proto_GreyboxCombatInputGate.ReloadCircleHit(xPct, yPct)) return true;
                }
                else if (goName == Proto_GreyboxHudBuilder.GrenadeButtonName)
                {
                    if (Proto_GreyboxCombatInputGate.GrenadeCircleHit(xPct, yPct)) return true;
                }
            }

            return false;
        }

        void FireDebugBullet()
        {
            var foot = _motor.FootPosition;
            var startGround = new Vector2(foot.x, Proto_GreyboxLayout.ScreenYPercentToWorldY(30f));
            var toPlayer = (foot - startGround).normalized;
            _debugBullet = new Proto_GreyboxBulletSim(startGround, toPlayer * _tuning.EnemyBulletSpeed, _cover);
        }

        void TickDebugBullet(float dt)
        {
            if (_debugBullet == null || !_debugBullet.Alive)
                return;
            var footprint = Proto_GreyboxBulletSim.PlayerGroundFootprint(_motor.FootPosition);
            int hpBefore = _hearts.Hearts;
            int hpScratch = hpBefore;
            _debugBullet.Tick(dt, footprint, IsInvulnerable, ref hpScratch);
            if (hpScratch < hpBefore && _hearts.TakeHit())
            {
                _hp = _hearts.Hearts;
                _heartsHud.TriggerPop(_hearts.Hearts);
                _hitFlashTimer = 0.12f;
                _westSkin?.OnPlayerHitFlash();
            }
            else
                _hp = _hearts.Hearts;
        }

        public bool IsInvulnerable => (_roll != null && _roll.IsInvulnerable) || _hearts.IsInvulnerable;

        public void ResetHp()
        {
            _hearts.ResetFull();
            _hp = _hearts.Hearts;
        }

        public void ApplyTestPlayerHit()
        {
            if (!_hearts.TakeHit()) return;
            _hp = _hearts.Hearts;
            _heartsHud.TriggerPop(_hearts.Hearts);
            _hitFlashTimer = 0.12f;
            _westSkin?.OnPlayerHitFlash();
        }
    }
}
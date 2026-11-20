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
        GameObject _explosionVis;
        readonly Proto_GreyboxResultUi _resultUi = new Proto_GreyboxResultUi();
        Proto_GreyboxRuntimeDebugUi _debugUi;

        Color _baseColor;
        int _hp;
        int _score;
        bool _rollPressedPrev;
        bool _reloadPressedPrev;
        float _simTime;
        bool _fireStickTracking;
        Vector2 _fireStickStartScreen;
        int _fireStickFingerId = -2;
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
        readonly List<SpriteRenderer> _enemyBulletViews = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _enemyBulletGlowViews = new List<SpriteRenderer>();

        bool _runStarted;
        Proto_GreyboxRunResult _runResult = Proto_GreyboxRunResult.Playing;
        float _hitFlashTimer;
        bool _debugTogglePrev;

        public Proto_GreyboxPlayerMotor Motor => _motor;
        public Proto_GreyboxRoll Roll => _roll;
        public Proto_GreyboxAim Aim => _aim;
        public Proto_GreyboxGun Gun => _gun;
        public Proto_GreyboxGrenade Grenade => _grenade;
        public Proto_GreyboxCover Cover => _cover;
        public Proto_GreyboxRuntimeTuning RuntimeTuning => _tuning;
        public Proto_GreyboxWaveDirector WaveDirector => _waveDirector;
        public IReadOnlyList<Proto_GreyboxTarget> Targets => _targets;
        public IReadOnlyList<Proto_GreyboxAllyBulletSim> AllyBullets => _allyBullets;
        public IReadOnlyList<Proto_GreyboxBulletSim> EnemyBullets => _enemyBullets;
        public int Hp => _hp;
        public int Score => _score;
        public float SimTime => _simTime;
        public Proto_GreyboxRunResult RunResult => _runResult;

        void Awake() => EnsureLogic();

        void EnsureLogic()
        {
            if (_motor != null) return;
            _sprite = GetComponent<SpriteRenderer>();
            _baseColor = _sprite != null ? _sprite.color : Color.white;
            var foot = new Vector2(transform.position.x, transform.position.y);
            _hp = _tuning.PlayerMaxHp;
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

        void Update()
        {
            if (!Application.isPlaying) return;
            EnsureLogic();
            var input = PollLegacyInput();
            Simulate(input, Time.deltaTime);
        }

        public void Simulate(Proto_GreyboxInput input, float dt)
        {
            EnsureLogic();
            EnsureViewRefs();
            ApplyRuntimeTuning();

            if (_runResult != Proto_GreyboxRunResult.Playing)
            {
                if (input.RestartPressed)
                    FullRestart();
                UpdateResultPanel();
                return;
            }

            if (!_runStarted)
            {
                _waveDirector.StartRun(_tuning);
                _runStarted = true;
            }

            _simTime += dt;
            _roll.RememberMoveDirection(input.Move);

            if (input.DebugToggle && !_debugTogglePrev)
                _debugUi?.Toggle();
            _debugTogglePrev = input.DebugToggle;

            if (input.RollPressed && !_rollPressedPrev)
            {
                if (_roll.TryStart(input.Move))
                    _gun.CancelReload();
            }
            _rollPressedPrev = input.RollPressed;

            if (input.ReloadPressed && !_reloadPressedPrev)
                _gun.StartReload(_simTime);
            _reloadPressedPrev = input.ReloadPressed;

            if (_roll.IsRolling)
                _motor.ApplyDelta(_roll.Tick(dt));
            else
            {
                _roll.Tick(dt);
                _motor.TickMove(input.Move, dt);
            }

            var foot = _motor.FootPosition;
            var aimTargets = CollectAimTargets();
            if (input.FireStickHeld)
            {
                if (!_aim.IsDragging)
                    _aim.BeginDrag(foot, aimTargets);
                _aim.SetAimDirection(input.FireStickDragWorld);
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

            if (fireIntent == Proto_GreyboxAimReleaseIntent.AutoAim
                || fireIntent == Proto_GreyboxAimReleaseIntent.ManualDirection)
                TryFireFromIntent(fireIntent);

            if (input.GrenadePressed)
                _grenade.TryThrow(_simTime, foot, CombatAimGround());

            _grenade.Tick(_simTime, dt, CollectHittableTargets(), _cover, DeactivateCratePercent);
            TickGrunts(dt, foot);
            _waveDirector.Tick(dt, _tuning);
            TickAllyBullets(dt);
            TickEnemyBullets(dt);
            SyncAllyBulletViews();
            SyncEnemyBulletViews();
            SyncGruntViews();
            ApplyTransform(dt);
            UpdateCrosshair();
            UpdateAimLineVisual();
            UpdateHud();
            TickExplosionVis();
            TickDebugBullet(dt);
            CheckRunEnd();

            if (Input.GetKeyDown(KeyCode.T))
                FireDebugBullet();
        }

        void ApplyRuntimeTuning()
        {
            _tuning.ApplyDepthScales();
            _motor.SetMoveSpeed(_tuning.MoveSpeed);
            _roll.ApplyRuntime(_tuning);
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
            EnsureAllyBulletRoot();
            while (_allyBulletViews.Count < _allyBullets.Count)
            {
                var go = new GameObject("Greybox_AllyBullet");
                go.transform.SetParent(_allyBulletRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                sr.color = Proto_GreyboxPalette.AllyBulletCore;
                _allyBulletViews.Add(sr);

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
                if (i < _allyBullets.Count && _allyBullets[i].Alive)
                {
                    var b = _allyBullets[i];
                    var vis = b.VisualPositionAtGround();
                    float scale = b.VisualScale;
                    float diam = Mathf.Max(_tuning.AllyBulletDiameter * scale, Proto_GreyboxLayout.MinAllyBulletWorldDiameter);
                    int order = Proto_GreyboxLayout.CombatDrawSortingOrder(b.GroundPosition.y);
                    sr.gameObject.SetActive(true);
                    sr.transform.position = new Vector3(vis.x, vis.y, 0f);
                    sr.transform.localScale = new Vector3(diam, diam, 1f);
                    sr.sortingOrder = order;

                    if (tracer != null)
                    {
                        var vel = b.GroundVelocity;
                        var dir = vel.sqrMagnitude > 1e-6f ? vel.normalized : Vector2.right;
                        float tailLen = Proto_GreyboxTuning.AllyBulletTracerLength;
                        var center = b.GroundPosition - dir * tailLen * 0.5f;
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
                }
            }
        }

        void SyncEnemyBulletViews()
        {
            EnsureEnemyBulletRoot();
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
                    float diam = b.VisualDiameter(_tuning.EnemyBulletDiameter);
                    int order = Proto_GreyboxLayout.CombatDrawSortingOrder(b.GroundPosition.y);
                    var pos = new Vector3(b.GroundPosition.x, b.GroundPosition.y, 0f);
                    sr.gameObject.SetActive(true);
                    sr.transform.position = pos;
                    sr.transform.localScale = new Vector3(diam, diam, 1f);
                    sr.sortingOrder = order;

                    if (glow != null)
                    {
                        float halo = diam * Proto_GreyboxTuning.EnemyBulletGlowDiameterMul;
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
                        break;
                    }
                }

                if (!b.Alive)
                    _allyBullets.RemoveAt(i);
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

                int hpBefore = _hp;
                b.Tick(dt, footprint, IsInvulnerable, ref _hp, _tuning.CoverDamageReduction01, _coverRng,
                    Proto_GreyboxTuning.EnemyBulletDamage);
                if (_hp < hpBefore)
                    _hitFlashTimer = 0.12f;
                if (!b.Alive)
                    _enemyBullets.RemoveAt(i);
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
            EnsureGruntRoot();
            var go = new GameObject("Greybox_Grunt");
            go.transform.SetParent(_gruntRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Proto_GreyboxSpriteUtil.GetGruntSprite();
            sr.color = Color.white;
            var view = go.AddComponent<Proto_GreyboxTargetView>();
            view.Init(grunt.Target);
            view.SyncFromLogic();
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
                    if (view.gameObject.activeSelf)
                    {
                        view.MarkDefeated();
                        view.gameObject.SetActive(false);
                        _score += Proto_GreyboxTuning.ScorePerKill;
                    }

                    continue;
                }

                view.gameObject.SetActive(true);
                view.SetHiddenVisual(grunt.IsHidden);
                view.SyncFromLogic();
            }
        }

        void CheckRunEnd()
        {
            if (_hp <= 0)
            {
                _hp = 0;
                _runResult = Proto_GreyboxRunResult.Down;
                EnsureResultPanel();
                UpdateResultPanel();
            }
            else if (_waveDirector.State == Proto_GreyboxWaveDirectorState.Cleared)
            {
                _runResult = Proto_GreyboxRunResult.Clear;
                EnsureResultPanel();
                UpdateResultPanel();
            }
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
            var builder = FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            if (builder != null) { builder.Build(); return; } // Build() replaces this player with a fresh controller
            _allyBullets.Clear();
            _enemyBullets.Clear();
            _gruntViews.Clear();
            _targets.Clear();
            _targetViews.Clear();
            _simTime = 0f;
            _score = 0;
            _hp = _tuning.PlayerMaxHp;
            _runResult = Proto_GreyboxRunResult.Playing;
            _runStarted = false;
            _grenade = new Proto_GreyboxGrenade();
            _gun = new Proto_GreyboxGun();
            _aim = new Proto_GreyboxAim();
            _cover = Proto_GreyboxCover.FromStageCrates();
            _waveDirector.ResetRun();
            _explosionVis = null;
            _resultUi.Clear();
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
                aim = CombatAimGround();
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

        void UpdateHud()
        {
            if (_ammoText == null)
                CacheHudRefs();
            if (_ammoText != null)
                _ammoText.text = _gun.IsReloading ? "R..." : $"{_gun.Ammo}/{_gun.MagazineSize}";
            if (_scoreText != null)
                _scoreText.text = _score.ToString();
            if (_waveText != null)
            {
                int w = Mathf.Max(1, _waveDirector.CurrentWave);
                if (_runResult != Proto_GreyboxRunResult.Playing && _waveDirector.State == Proto_GreyboxWaveDirectorState.Cleared)
                    w = _waveDirector.TotalWaves;
                _waveText.text = $"STAGE 1 · WAVE {w}/{_waveDirector.TotalWaves}";
            }

            if (_hpBarFill != null)
                _hpBarFill.fillAmount = Mathf.Clamp01((float)_hp / _tuning.PlayerMaxHp);
            for (int i = 0; i < _ammoPips.Count; i++)
            {
                var pip = _ammoPips[i];
                if (pip == null) continue;
                bool visible = i < _gun.MagazineSize;
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
                            Proto_GreyboxHudBuilder.ReloadEmptyLabelX0, Proto_GreyboxHudBuilder.ReloadEmptyLabelY0,
                            Proto_GreyboxHudBuilder.ReloadEmptyLabelX1, Proto_GreyboxHudBuilder.ReloadEmptyLabelY1);
                }
                else
                {
                    _reloadLabel.fontSize = ReloadLabelFontDefault;
                    _reloadLabel.fontStyle = FontStyle.Normal;
                    _reloadLabel.color = new Color(1f, 1f, 1f, 0.7f);
                    if (_reloadLabelRt != null)
                        Proto_GreyboxHudBuilder.ApplyRect(_reloadLabelRt, Proto_GreyboxHudBuilder.ReloadHitX0,
                            Proto_GreyboxHudBuilder.ReloadHitY0, Proto_GreyboxHudBuilder.ReloadHitX1,
                            Proto_GreyboxHudBuilder.ReloadHitY1);
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
            var mid = foot + dir * (len * 0.5f);
            _aimLine.gameObject.SetActive(true);
            _aimLine.position = new Vector3(mid.x, mid.y, 0f);
            _aimLine.localScale = new Vector3(len, 0.04f, 1f);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _aimLine.rotation = Quaternion.Euler(0f, 0f, angle);
            var lineSr = _aimLine.GetComponent<SpriteRenderer>();
            if (lineSr != null)
                lineSr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(foot.y) + 2;

            _aimCone.gameObject.SetActive(true);
            _aimCone.position = new Vector3(foot.x, foot.y, 0f);
            _aimCone.localScale = new Vector3(0.35f, 0.22f, 1f);
            _aimCone.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
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
                    var sr = _explosionVis.AddComponent<SpriteRenderer>();
                    sr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                    sr.color = new Color(1f, 0.55f, 0.1f, 0.65f);
                }

                float t = _grenade.ExplosionFadeT(_simTime);
                float r = _grenade.ExplosionRadius * (0.35f + t * 0.65f);
                var pos = _grenade.VisualGround;
                float depth = Proto_DepthScale.ScaleAt(pos.y);
                float diam = Mathf.Max(r * 2f * depth, Proto_GreyboxLayout.MinAllyBulletWorldDiameter * 2f);
                int order = Proto_GreyboxLayout.CombatDrawSortingOrder(pos.y);
                _explosionVis.transform.position = new Vector3(pos.x, pos.y, 0f);
                _explosionVis.transform.localScale = new Vector3(diam, diam, 1f);
                var expSr = _explosionVis.GetComponent<SpriteRenderer>();
                if (expSr != null)
                {
                    expSr.sortingOrder = order;
                    expSr.color = new Color(1f, 0.55f, 0.1f, Mathf.Lerp(0.75f, 0f, t));
                }

                _explosionVis.SetActive(true);
            }
            else if (_explosionVis != null)
                _explosionVis.SetActive(false);
        }

        void ApplyTransform(float dt)
        {
            var foot = _motor.FootPosition;
            transform.position = new Vector3(foot.x, foot.y, 0f);
            float targetH = Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.PlayerCapsuleHeightPercent);
            var rollVisual = _roll.IsRolling;
            float sy = rollVisual ? 0.6f : 1f;
            if (_sprite != null && _sprite.sprite != null)
            {
                var uniform = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(_sprite.sprite, targetH);
                transform.localScale = new Vector3(uniform.x, uniform.y * sy, 1f);
            }
            else
            {
                float scale = Proto_DepthScale.ScaleAt(foot.y);
                float targetW = Proto_GreyboxLayout.WorldWidth * (Proto_GreyboxTuning.PlayerCapsuleWidthPercent / 100f);
                float baseH = targetH / scale;
                float baseW = targetW / scale;
                transform.localScale = new Vector3(baseW * scale, baseH * scale * sy, 1f);
            }

            if (_sprite != null)
            {
                _sprite.sortingOrder = Proto_GreyboxLayout.SortingOrderAtFootY(foot.y);
                if (_hitFlashTimer > 0f)
                {
                    _hitFlashTimer -= dt;
                    _sprite.color = Color.white;
                }
                else
                {
                    _sprite.color = rollVisual
                        ? new Color(_baseColor.r + 0.12f, _baseColor.g + 0.12f, _baseColor.b + 0.12f, 0.78f)
                        : _baseColor;
                }
            }

            if (_hpText != null)
                _hpText.text = $"HP  {_hp}/{_tuning.PlayerMaxHp}";
        }

        Proto_GreyboxInput PollLegacyInput()
        {
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
            bool grenade = Input.GetKeyDown(KeyCode.G) || PollTouchButton(Proto_GreyboxHudBuilder.GrenadeButtonName);
            bool debugToggle = Input.GetKeyDown(KeyCode.F1) || PollTouchDbgButton();
            bool fireQuick = Input.GetKeyDown(KeyCode.J);

            var fireStick = PollTouchFireStick();
            var mouseAim = PollMouseAim();

            bool restart = false;
            if (_runResult != Proto_GreyboxRunResult.Playing)
            {
                restart = Input.GetKeyDown(KeyCode.R) || PollScreenTapForRestart();
                reload = false;
            }

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
                GrenadePressed = grenade,
                DebugToggle = debugToggle,
                RestartPressed = restart
            };
        }

        bool PollScreenTapForRestart()
        {
            int n = GatherPointers(_pointerBuf);
            for (int i = 0; i < n; i++)
            {
                if (!_pointerBuf[i].Began) continue;
                PointerPercent(_pointerBuf[i].Pos, out float xPct, out float yPct);
                if (IsDbgHit(xPct, yPct)) continue;
                return true;
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

        static void PointerPercent(Vector2 pos, out float xPct, out float yPct)
        {
            xPct = pos.x / Screen.width * 100f;
            yPct = (Screen.height - pos.y) / Screen.height * 100f;
        }

        static bool IsDbgHit(float xPct, float yPct) =>
            xPct >= 3.2f && xPct <= 8.6f && yPct >= 8.6f && yPct <= 11.4f;

        static bool IsJoystickHit(float xPct, float yPct) =>
            xPct >= 4f && xPct <= 19f && yPct >= 66f && yPct <= 91f;

        static bool IsFireHit(float xPct, float yPct) =>
            xPct >= Proto_GreyboxHudBuilder.FireHitX0 && xPct <= Proto_GreyboxHudBuilder.FireHitX1
            && yPct >= Proto_GreyboxHudBuilder.FireHitY0 && yPct <= Proto_GreyboxHudBuilder.FireHitY1;

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
                result.DragWorld = ScreenDeltaToWorldDrag(ev.Pos - _fireStickStartScreen);
            }

            if (ev.Ended)
            {
                result.Released = true;
                result.DragWorld = ScreenDeltaToWorldDrag(ev.Pos - _fireStickStartScreen);
                _fireStickTracking = false;
                _fireStickFingerId = -2;
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

        static Vector2 ScreenDeltaToWorldDrag(Vector2 deltaScreen)
        {
            float worldPerPx = Proto_GreyboxLayout.WorldWidth / Screen.width;
            float worldPerPy = Proto_GreyboxLayout.WorldHeight / Screen.height;
            return new Vector2(deltaScreen.x * worldPerPx, -deltaScreen.y * worldPerPy);
        }

        static Vector2 ScreenToGroundWorld(Vector2 screenPos)
        {
            float xPct = screenPos.x / Screen.width * 100f;
            float yPct = (Screen.height - screenPos.y) / Screen.height * 100f;
            return new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(xPct),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(yPct));
        }

        static bool IsPointerOnHudButton(Vector2? posOverride = null)
        {
            Vector2 pos = posOverride ?? (Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition);
            PointerPercent(pos, out float xPct, out float yPct);
            if (xPct >= Proto_GreyboxHudBuilder.RollHitX0 && xPct <= Proto_GreyboxHudBuilder.RollHitX1
                && yPct >= Proto_GreyboxHudBuilder.RollHitY0 && yPct <= Proto_GreyboxHudBuilder.RollHitY1)
                return true;
            if (IsFireHit(xPct, yPct)) return true;
            if (xPct >= Proto_GreyboxHudBuilder.ReloadHitX0 && xPct <= Proto_GreyboxHudBuilder.ReloadHitX1
                && yPct >= Proto_GreyboxHudBuilder.ReloadHitY0 && yPct <= Proto_GreyboxHudBuilder.ReloadHitY1)
                return true;
            if (xPct >= 88f && xPct <= 98f && yPct >= 84f && yPct <= 95f) return true;
            if (xPct <= 19f && yPct >= 66f) return true;
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
                if (_joyFingerId == -2 && (p.Began || p.Held) && IsJoystickHit(xPct, yPct))
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

            Vector2 pos = ev.Pos;
            float cx = Proto_GreyboxHudBuilder.RefWidth * 0.115f;
            float cy = Proto_GreyboxHudBuilder.RefHeight * (1f - 0.78f);
            float sx = pos.x / Screen.width * Proto_GreyboxHudBuilder.RefWidth;
            float sy = (Screen.height - pos.y) / Screen.height * Proto_GreyboxHudBuilder.RefHeight;
            var delta = new Vector2(sx - cx, sy - cy);
            float maxR = Proto_GreyboxHudBuilder.RefWidth * 0.075f;
            if (delta.magnitude > maxR)
                delta = delta.normalized * maxR;
            analog = delta / maxR;
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
                    if (xPct >= Proto_GreyboxHudBuilder.RollHitX0 && xPct <= Proto_GreyboxHudBuilder.RollHitX1
                        && yPct >= Proto_GreyboxHudBuilder.RollHitY0 && yPct <= Proto_GreyboxHudBuilder.RollHitY1)
                        return true;
                }
                else if (goName == Proto_GreyboxHudBuilder.FireButtonName)
                {
                    if (IsFireHit(xPct, yPct)) return true;
                }
                else if (goName == Proto_GreyboxHudBuilder.ReloadButtonName)
                {
                    if (xPct >= Proto_GreyboxHudBuilder.ReloadHitX0 && xPct <= Proto_GreyboxHudBuilder.ReloadHitX1
                        && yPct >= Proto_GreyboxHudBuilder.ReloadHitY0 && yPct <= Proto_GreyboxHudBuilder.ReloadHitY1)
                        return true;
                }
                else if (goName == Proto_GreyboxHudBuilder.GrenadeButtonName)
                {
                    if (xPct >= 88f && xPct <= 98f && yPct >= 84f && yPct <= 95f) return true;
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
            _debugBullet.Tick(dt, footprint, IsInvulnerable, ref _hp);
        }

        public bool IsInvulnerable => _roll != null && _roll.IsInvulnerable;

        public void ResetHp() => _hp = _tuning.PlayerMaxHp;
    }
}
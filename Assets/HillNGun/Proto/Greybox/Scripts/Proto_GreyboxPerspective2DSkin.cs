using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Slice K: perspective 2D environment + character sprites over shared 2D gameplay.</summary>
    public sealed class Proto_GreyboxPerspective2DSkin
    {
        public const string LayerRootName = "Persp2D_Stage";
        const float DesignW = Proto_GreyboxPersp2DMath.DesignWidth2x;
        const float DesignH = Proto_GreyboxPersp2DMath.DesignHeight2x;

        readonly Proto_GreyboxPlayerController _host;
        Proto_GreyboxPersp2DCatalog _catalog;
        Transform _layerRoot;
        bool _active;
        bool _bound;
        float _animT;
        string _playerDir = Proto_GreyboxPersp2DDirection.South;
        int _playerFrame;
        string _playerAnim = "idle";
        public bool IsActive => _active && _bound;

        public Proto_GreyboxPerspective2DSkin(Proto_GreyboxPlayerController host) => _host = host;

        public void SetEnabled(bool on)
        {
            _active = on && Proto_GreyboxViewModeState.IsPersp2D;
            if (_active) EnsureBound();
            else if (_bound) Unbind();
        }

        public void SyncFromViewMode() => SetEnabled(Proto_GreyboxViewModeState.IsPersp2D);

        public void EnsureBound()
        {
            if (!_active) return;
            _catalog = Proto_GreyboxPersp2DCatalog.LoadRuntime();
            if (_catalog == null) return;
            if (!_bound) BindStage();
        }

        float _simDt;

        public void SetSimDt(float dt) => _simDt = dt;

        public void Tick(float dt, Proto_GreyboxInput input)
        {
            if (!IsActive || _catalog == null) return;
            _simDt = dt;
            TickPlayer(dt, input);
            TickMuzzle(dt);
            _combatFx?.Tick(dt);
            SyncCaptureBandits();
            HideProceduralClutter();
        }

        public void SyncGruntLiving(Proto_GreyboxGrunt grunt, Proto_GreyboxTargetView view, bool hidden)
        {
            if (!IsActive || view == null || _catalog == null) return;
            var foot = grunt.Target.FootPosition;
            var sr = view.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            sr.enabled = true;
            string dir = Proto_GreyboxPersp2DDirection.EnemyFacePlayer(foot, _host.Motor.FootPosition);
            string anim = grunt.FiredThisTick ? "shoot" : "idle";
            Proto_GreyboxPersp2DDirection.Resolve("bandit", dir, out var spriteDir, out var flipX);
            var frames = _catalog.FramesFor("bandit", anim, spriteDir);
            if (frames == null || frames.Length == 0) return;
            var use = _catalog.FramesFor("bandit", anim, spriteDir) ?? frames;
            sr.sprite = use[0];
            sr.flipX = flipX;
            float footPct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(foot.y);
            float scale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(foot.y)
                * Proto_GreyboxPersp2DVisualTuning.EnemySpriteScaleMul;
            sr.transform.position = new Vector3(foot.x, foot.y, 0f);
            sr.transform.localScale = Vector3.one * scale;
            int sort = Proto_GreyboxLayout.SortingOrderAtFootY(foot.y);
            float x2x = (foot.x / Proto_GreyboxLayout.WorldWidth + 0.5f) * DesignW;
            float feet2x = footPct / 100f * DesignH;
            foreach (var b in _catalog.sceneBuildings)
            {
                if (b == null) continue;
                Proto_GreyboxPersp2DMath.ActorWorldFromFootScreen2x(x2x, feet2x, out var wx, out var wz);
                if (Proto_GreyboxPersp2DMath.PorchFrontDrawsOverActor(wx, wz, b.porchEdgeX, b.nearPorchZ))
                    sort = Mathf.Min(sort, -440);
            }
            sr.sortingOrder = sort;
            if (hidden) sr.color = new Color(1f, 1f, 1f, 0.25f);
            else sr.color = Color.white;
            if (grunt.FiredThisTick)
                _combatFx?.OnGruntMuzzle(foot, _host.Motor.FootPosition);
        }

        void BindStage()
        {
            HideProceduralStage();
            BuildStage();
            Proto_GreyboxPersp2DHud.EnsureOnHud(GameObject.Find("Greybox_Hud")?.transform);
            _combatFx = new Proto_GreyboxPersp2DCombatFx(_host, () => _layerRoot);
            _bound = true;
        }

        void Unbind()
        {
            ClearCaptureBandits();
            _combatFx?.Clear();
            _combatFx = null;
            Proto_GreyboxPersp2DHud.Restore();
            RestoreProceduralStage();
            if (_layerRoot != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) Object.DestroyImmediate(_layerRoot.gameObject);
                else
#endif
                    Object.Destroy(_layerRoot.gameObject);
                _layerRoot = null;
            }
            _bound = false;
        }

        static readonly string[] BuildingFarToNear = { "sheriff_R", "store_L", "hotel_R", "saloon_L" };

        SpriteRenderer _muzzleSr;
        float _muzzleT;
        Proto_GreyboxFxV2Catalog _fxV2;
        Proto_GreyboxPersp2DCombatFx _combatFx;
        readonly List<SpriteRenderer> _captureBandits = new List<SpriteRenderer>();

        void BuildStage()
        {
            var stage = GameObject.Find(Proto_GreyboxStageBuilder.RootName);
            var parent = stage != null ? stage.transform : _host.transform.parent;
            var layerGo = new GameObject(LayerRootName);
            layerGo.transform.SetParent(parent, false);
            _layerRoot = layerGo.transform;
            float envPpu = Proto_GreyboxWestPixelCatalog.ImportPpu;

            var roadPlane = _catalog.roadPlane;
            if (roadPlane.nativeW <= 0f)
                roadPlane = new Proto_GreyboxPersp2DCatalog.BakedPlane
                    { nativeW = 3840f, nativeH = 1253f, topLeftX = 0f, topLeftY = 907f };
            var skyPlane = _catalog.skyPlane;
            if (skyPlane.nativeW <= 0f)
                skyPlane = new Proto_GreyboxPersp2DCatalog.BakedPlane
                    { nativeW = 3840f, nativeH = 907f, topLeftX = 0f, topLeftY = 0f };

            float roadUni = Proto_GreyboxPersp2DStageLayout.UniformScaleNative(_catalog.road, envPpu, roadPlane.nativeW);
            if (_catalog.sky != null)
            {
                float skyUni = Proto_GreyboxPersp2DStageLayout.UniformScaleNative(_catalog.sky, envPpu, skyPlane.nativeW);
                Proto_GreyboxPersp2DStageLayout.PlaceSpriteTopLeft(_layerRoot, "P2D_Sky", _catalog.sky, envPpu,
                    skyPlane.topLeftX, skyPlane.topLeftY, skyPlane.nativeW, skyPlane.nativeH, skyUni, -520);
            }
            if (_catalog.road != null)
                Proto_GreyboxPersp2DStageLayout.PlaceSpriteTopLeft(_layerRoot, "P2D_Road", _catalog.road, envPpu,
                    roadPlane.topLeftX, roadPlane.topLeftY, roadPlane.nativeW, roadPlane.nativeH, roadUni, -490);

            int backOrder = -480;
            foreach (var id in BuildingFarToNear)
            {
                var b = FindBuilding(id);
                if (b?.back == null) continue;
                var baked = Proto_GreyboxPersp2DEnvLayout.FindBuilding(_catalog, id);
                var lay = Proto_GreyboxPersp2DStageLayout.GetBuilding(id);
                float nativeW = baked != null && baked.nativeW > 0f ? baked.nativeW : 800f;
                float bUni = Proto_GreyboxPersp2DStageLayout.UniformScaleNative(b.back, envPpu, nativeW);
                Proto_GreyboxPersp2DStageLayout.PlaceSpritePivot(_layerRoot, "P2D_" + id + "_back", b.back, envPpu,
                    lay.pivotX, lay.pivotY, bUni, backOrder++);
                if (b.sign != null)
                {
                    float signNative = baked != null && baked.signNativeW > 0f ? baked.signNativeW : b.sign.rect.width;
                    float signUni = Proto_GreyboxPersp2DStageLayout.UniformScaleNative(b.sign, envPpu, signNative,
                        lay.signScale);
                    Proto_GreyboxPersp2DStageLayout.PlaceSpritePivot(_layerRoot, "P2D_" + id + "_sign", b.sign, envPpu,
                        lay.pivotX + lay.signOffsetX, lay.pivotY + lay.signOffsetY, signUni, backOrder++);
                }
            }

            int frontOrder = -395;
            foreach (var id in BuildingFarToNear)
            {
                var b = FindBuilding(id);
                if (b?.front == null) continue;
                var baked = Proto_GreyboxPersp2DEnvLayout.FindBuilding(_catalog, id);
                var lay = Proto_GreyboxPersp2DStageLayout.GetBuilding(id);
                float nativeW = baked != null && baked.nativeW > 0f ? baked.nativeW : 800f;
                float bUni = Proto_GreyboxPersp2DStageLayout.UniformScaleNative(b.front, envPpu, nativeW);
                Proto_GreyboxPersp2DStageLayout.PlaceSpritePivot(_layerRoot, "P2D_" + id + "_front", b.front, envPpu,
                    lay.pivotX, lay.pivotY, bUni, frontOrder++);
            }

            PlaceSceneProps(envPpu);
            var coverSprite = Proto_GreyboxPersp2DEnvLayout.SpriteForProp(_catalog, "crate") ?? _catalog.props?[0];
            if (coverSprite != null)
            {
                PlaceCoverPropRef("P2D_CoverL", coverSprite, 960f, 1640f);
                PlaceCoverPropRef("P2D_CoverR", coverSprite, 2880f, 1640f);
            }
        }

        void PlaceSceneProps(float envPpu)
        {
            if (_catalog.scenePropPlacements == null || _layerRoot == null) return;
            foreach (var p in _catalog.scenePropPlacements)
            {
                if (p == null || string.IsNullOrEmpty(p.name)) continue;
                var def = Proto_GreyboxPersp2DEnvLayout.FindPropDef(_catalog, p.name);
                if (def == null || def.sprite == null) continue;
                var sprite = def.sprite;
                float nativeW = def.nativeW > 0f ? def.nativeW : sprite.rect.width;
                float uni = Proto_GreyboxPersp2DStageLayout.UniformScaleNative(sprite, envPpu, nativeW, p.jsonScale);
                float footPct = Proto_GreyboxPersp2DStageLayout.ScreenPercentYFromRefY2x(p.pivotY);
                int sort = Proto_GreyboxLayout.SortingOrderAtFootY(
                    Proto_GreyboxLayout.ScreenYPercentToWorldY(footPct));
                Proto_GreyboxPersp2DStageLayout.PlaceSpritePivot(_layerRoot, "P2D_Prop_" + p.name, sprite, envPpu,
                    p.pivotX, p.pivotY, uni, sort, p.flipX);
                if (_catalog.propShadow != null)
                {
                    float shUni = uni * 0.65f;
                    Proto_GreyboxPersp2DStageLayout.PlaceSpritePivot(_layerRoot, "P2D_Sh_" + p.name, _catalog.propShadow,
                        envPpu, p.pivotX, p.pivotY + 6f, shUni, sort - 1);
                }
            }
        }

        Proto_GreyboxPersp2DCatalog.BuildingSet FindBuilding(string id)
        {
            if (_catalog.sceneBuildings == null) return null;
            foreach (var b in _catalog.sceneBuildings)
                if (b != null && b.id == id) return b;
            return null;
        }

        public void OnPlayerFired()
        {
            if (!IsActive || _catalog == null) return;
            _fxV2 ??= Proto_GreyboxFxV2Catalog.LoadRuntime();
            var muzzle = _catalog.fxMuzzleFlash ?? _fxV2?.muzzleFlash;
            if (muzzle == null || muzzle.Length == 0) return;
            EnsureMuzzleSr();
            _muzzleSr.sprite = muzzle[0];
            _muzzleSr.gameObject.SetActive(true);
            _muzzleT = Proto_GreyboxTuning.PlayerMuzzleFlashDuration;
            var foot = _host.Motor.FootPosition;
            Proto_GreyboxPersp2DDirection.Resolve("hero", _playerDir, out _, out var flipX);
            float visScale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(foot.y);
            var muzPos = Proto_GreyboxPersp2DAnimJson.MuzzleWorldFromFoot(foot, visScale, "hero", _playerDir, flipX, 0);
            _muzzleSr.transform.position = new Vector3(muzPos.x, muzPos.y, 0f);
            float diam = Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.PlayerMuzzleFlashHeightPercent) *
                         visScale;
            _muzzleSr.transform.localScale =
                Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(muzzle[0], diam);
            _muzzleSr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(foot.y) + 3;
        }

        void EnsureMuzzleSr()
        {
            if (_muzzleSr != null) return;
            var go = new GameObject("P2D_Muzzle");
            go.transform.SetParent(_layerRoot != null ? _layerRoot : _host.transform, false);
            _muzzleSr = go.AddComponent<SpriteRenderer>();
            _muzzleSr.enabled = true;
            go.SetActive(false);
        }

        void TickMuzzle(float dt)
        {
            if (_muzzleSr == null || !_muzzleSr.gameObject.activeSelf) return;
            _muzzleT -= dt;
            var seq = _fxV2?.muzzleFlash;
            if (seq != null && seq.Length > 0)
            {
                int frame = Mathf.Min(seq.Length - 1, Mathf.FloorToInt((1f - _muzzleT / Mathf.Max(0.001f, Proto_GreyboxTuning.PlayerMuzzleFlashDuration)) * seq.Length));
                _muzzleSr.sprite = seq[frame];
            }

            if (_muzzleT <= 0f) _muzzleSr.gameObject.SetActive(false);
        }

        void PlaceCoverPropRef(string name, Sprite sprite, float feetX2x, float feetY2x)
        {
            if (sprite == null || _layerRoot == null) return;
            float scale = Proto_GreyboxPersp2DStageLayout.PropUniformScaleAtFeetY2x(feetY2x, sprite);
            float xPct = Proto_GreyboxPersp2DStageLayout.ScreenPercentXFromRefX2x(feetX2x);
            float footPct = Proto_GreyboxPersp2DStageLayout.ScreenPercentYFromRefY2x(feetY2x);
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(footPct);
            float x = Proto_GreyboxLayout.ScreenXPercentToWorldX(xPct);
            var go = new GameObject(name);
            go.transform.SetParent(_layerRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Proto_GreyboxLayout.SortingOrderAtFootY(footY);
            go.transform.position = new Vector3(x, footY, 0f);
            go.transform.localScale = Vector3.one * scale;
        }

        void TickPlayer(float dt, Proto_GreyboxInput input)
        {
            var sr = _host.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            var motor = _host.Motor;
            var roll = _host.Roll;
            var aimGround = _host.Aim.AimGroundPosition();
            var aimV = aimGround - motor.FootPosition;
            if (aimV.sqrMagnitude < 0.01f) aimV = input.Move;
            if (aimV.sqrMagnitude < 0.01f) aimV = Vector2.up;
            _playerDir = Proto_GreyboxPersp2DDirection.PickFromVector(aimV);
            _playerAnim = roll.IsRolling ? "roll" : input.Move.sqrMagnitude > 0.04f && !roll.IsRolling ? "run"
                : input.FireQuickAuto ? "shoot" : "idle";
            Proto_GreyboxPersp2DDirection.Resolve("hero", _playerDir, out var spriteDir, out var flipX);
            var frames = _catalog.FramesFor("hero", _playerAnim, spriteDir);
            if (frames == null || frames.Length == 0) return;
            float fps = _playerAnim switch
            {
                "run" => 12f,
                "shoot" => 20f,
                "roll" => 13f / Mathf.Max(Proto_GreyboxTuning.RollDuration, 0.01f),
                _ => 8f
            };
            _animT += dt;
            int idx = _playerAnim is "idle" or "run"
                ? Mathf.FloorToInt(_animT * fps) % frames.Length
                : Mathf.Min(Mathf.FloorToInt(_animT * fps), frames.Length - 1);
            sr.sprite = frames[idx];
            sr.flipX = flipX;
            var foot = motor.FootPosition;
            float footPct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(foot.y);
            float scale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(foot.y);
            sr.transform.localScale = Vector3.one * scale;
            sr.sortingOrder = Proto_GreyboxLayout.SortingOrderAtFootY(foot.y);
            Proto_GreyboxPersp2DHud.Tick(_host, input);
        }

        public bool SyncAllyBullets(IReadOnlyList<Proto_GreyboxAllyBulletSim> bullets, float diameter,
            List<SpriteRenderer> cores, List<SpriteRenderer> tracers, List<SpriteRenderer> hotspots, Transform parent) =>
            IsActive && _combatFx != null &&
            _combatFx.SyncAllyBullets(bullets, diameter, cores, tracers, hotspots, parent);

        public bool SyncEnemyBullets(IReadOnlyList<Proto_GreyboxBulletSim> bullets, float diameter,
            List<SpriteRenderer> cores, List<SpriteRenderer> glows, Transform parent, float simTime) =>
            IsActive && _combatFx != null &&
            _combatFx.SyncEnemyBullets(bullets, diameter, cores, glows, parent, simTime);

        public void OnRollStarted(Proto_GreyboxRollFacing facing) => _combatFx?.OnRollStarted(facing);
        public void OnRollEnded() => _combatFx?.OnRollEnded();
        public void SpawnBulletDust(Vector2 groundPos) => _combatFx?.SpawnBulletDust(groundPos);
        public void SpawnHitSpark(Vector2 groundPos) => _combatFx?.SpawnHitSpark(groundPos);

        public void SyncCaptureDecor()
        {
            if (!IsActive) EnsureBound();
            if (Proto_GreyboxCapturePersp2D.DecorStreetBandits)
                EnsureCaptureBandits();
            else
                ClearCaptureBandits();
        }

        void EnsureCaptureBandits()
        {
            if (_captureBandits.Count > 0) return;
            var frames = _catalog?.FramesFor("bandit", "idle", "S");
            if (frames == null || frames.Length == 0) return;
            float[] xPct = { 43f, 48f, 54f, 60f };
            float[] yPct = { 55f, 56f, 54f, 57f };
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("P2D_CaptureBandit_" + i);
                go.transform.SetParent(_layerRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                float x = Proto_GreyboxLayout.ScreenXPercentToWorldX(xPct[i]);
                float y = Proto_GreyboxLayout.ScreenYPercentToWorldY(yPct[i]);
                float scale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(y)
                    * Proto_GreyboxPersp2DVisualTuning.CaptureBanditScaleMul;
                go.transform.position = new Vector3(x, y, 0f);
                go.transform.localScale = Vector3.one * scale;
                sr.sortingOrder = Proto_GreyboxLayout.SortingOrderAtFootY(y);
                _captureBandits.Add(sr);
            }
        }

        void SyncCaptureBandits()
        {
            if (Proto_GreyboxCapturePersp2D.DecorStreetBandits)
                EnsureCaptureBandits();
        }

        void ClearCaptureBandits()
        {
            foreach (var sr in _captureBandits)
            {
                if (sr == null) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying) Object.DestroyImmediate(sr.gameObject);
                else
#endif
                    Object.Destroy(sr.gameObject);
            }
            _captureBandits.Clear();
        }

        static void HideProceduralStage()
        {
            var stage = GameObject.Find(Proto_GreyboxStageBuilder.RootName);
            if (stage == null) return;
            foreach (Transform t in stage.transform)
            {
                if (t.name.StartsWith("Greybox_") && !t.name.Contains("Player"))
                    t.gameObject.SetActive(false);
            }
        }

        static void RestoreProceduralStage()
        {
            var stage = GameObject.Find(Proto_GreyboxStageBuilder.RootName);
            if (stage == null) return;
            foreach (Transform t in stage.transform)
                t.gameObject.SetActive(true);
        }

        void HideProceduralClutter()
        {
            var fence = GameObject.Find("Greybox_Fence");
            if (fence != null) fence.SetActive(false);
        }

        GameObject _debugActor;

        public bool TryGetDecorBanditChestScreen(int index, out float xPct, out float yPct)
        {
            xPct = yPct = 0f;
            if (index < 0 || index >= _captureBandits.Count) return false;
            var sr = _captureBandits[index];
            return TryGetSpriteChestScreen(sr, out xPct, out yPct);
        }

        static bool TryGetSpriteChestScreen(SpriteRenderer sr, out float xPct, out float yPct)
        {
            xPct = yPct = 0f;
            if (sr == null || !sr.gameObject.activeSelf || !sr.enabled) return false;
            Proto_GreyboxScreenMetrics.FromBounds(sr.bounds, out var top, out var bottom, out _, out _, out _,
                out xPct);
            yPct = Mathf.Lerp(top, bottom, 0.42f);
            return true;
        }

        public bool TryGetCrosshairScreenPercent(Proto_GreyboxPlayerController host, out float xPct, out float yPct)
        {
            xPct = yPct = 0f;
            var aim = host.Aim;
            var selected = aim?.SelectedTarget;

            if (Proto_GreyboxCapturePersp2D.DecorStreetBandits && _captureBandits.Count > 0)
            {
                int aimBandit = selected != null && selected.IsAlive ? -1 : 1;
                if (aimBandit >= 0 && aimBandit < _captureBandits.Count
                    && TryGetDecorBanditChestScreen(aimBandit, out xPct, out yPct))
                    return true;

                var foot = host.Motor.FootPosition;
                int best = -1;
                float bestDist = float.MaxValue;
                for (int i = 0; i < _captureBandits.Count; i++)
                {
                    var sr = _captureBandits[i];
                    if (sr == null || !sr.gameObject.activeSelf) continue;
                    float d = Mathf.Abs(sr.transform.position.x - foot.x);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = i;
                    }
                }

                if (best >= 0)
                    return TryGetDecorBanditChestScreen(best, out xPct, out yPct);
            }

            if (selected != null && selected.IsAlive)
            {
                var sr = host.TryGetGruntSprite(selected);
                if (sr != null && TryGetSpriteChestScreen(sr, out xPct, out yPct))
                    return true;
            }

            if (selected != null && selected.IsAlive)
            {
                var foot = selected.FootPosition;
                float bodyH = Proto_GreyboxPerspectiveDepth.EnemyBodyHeightWorld(foot.y)
                    * Proto_GreyboxPersp2DVisualTuning.EnemySpriteScaleMul;
                xPct = (foot.x / Proto_GreyboxLayout.WorldWidth + 0.5f) * 100f;
                yPct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(foot.y + bodyH * 0.52f);
                return true;
            }

            return false;
        }

        public void PumpCaptureCombatMoment()
        {
            EnsureCaptureBandits();
            _combatFx?.SpawnCaptureCombatVolley(_captureBandits, _host.Motor.FootPosition);
        }

        public void WriteFxCaptureCheck(Camera cam, System.Text.StringBuilder sb) =>
            _combatFx?.WriteFxCaptureCheck(cam, _host.Motor.FootPosition, sb);

        public void WriteDecorBanditHeightReport(Camera cam, System.Text.StringBuilder sb, bool includeBandits = true,
            bool includeHero = true, string heroLabel = "hero")
        {
            if (includeBandits)
            {
                float sum = 0f;
                int n = 0;
                for (int i = 0; i < _captureBandits.Count; i++)
                {
                    var sr = _captureBandits[i];
                    if (sr == null || !sr.gameObject.activeSelf) continue;
                    Proto_GreyboxScreenMetrics.FromBounds(sr.bounds, out _, out _, out var h, out _, out _, out var cx);
                    float footPct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(sr.transform.position.y);
                    sb.AppendLine($"bandit_{i}: x={cx:F1}% feet_y={footPct:F1}% height={h:F2}%");
                    Debug.Log($"Persp K bandit {i}: x={cx:F1}% feet={footPct:F1}% h={h:F2}%");
                    sum += h;
                    n++;
                }

                if (n > 0)
                    sb.AppendLine($"mean_height={sum / n:F2}%");
            }

            if (includeHero)
                AppendHeroHeightLine(sb, heroLabel);
        }

        void AppendHeroHeightLine(System.Text.StringBuilder sb, string heroLabel)
        {
            if (_host == null) return;
            var playerSr = _host.GetComponent<SpriteRenderer>();
            if (playerSr == null || playerSr.sprite == null) return;
            Proto_GreyboxScreenMetrics.FromBounds(playerSr.bounds, out _, out _, out var h, out _, out _, out var cx);
            float footPct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(_host.Motor.FootPosition.y);
            sb.AppendLine($"{heroLabel}: x={cx:F1}% feet_y={footPct:F1}% height={h:F2}%");
        }

        public void ClearPorchDebugActor()
        {
            if (_debugActor == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) Object.DestroyImmediate(_debugActor);
            else
#endif
                Object.Destroy(_debugActor);
            _debugActor = null;
        }

        /// <summary>Capture helper: place an actor relative to saloon porch for sort demos.</summary>
        public void DebugPlaceActorAtScreen2x(string who, float x2x, float feetY2x, bool behindPorch,
            string buildingId = "saloon_L")
        {
            if (!IsActive) EnsureBound();
            ClearPorchDebugActor();
            var anim = who == "hero" ? "idle" : "idle";
            var frames = _catalog.FramesFor(who, anim, "S");
            if (frames == null || frames.Length == 0) return;
            float xPct = x2x / DesignW * 100f;
            float footPct = feetY2x / DesignH * 100f;
            float x = Proto_GreyboxLayout.ScreenXPercentToWorldX(xPct);
            float y = Proto_GreyboxLayout.ScreenYPercentToWorldY(footPct);
            _debugActor = new GameObject("P2D_DebugActor");
            _debugActor.transform.SetParent(_layerRoot, false);
            var sr = _debugActor.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            float scale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFeetY2x(feetY2x)
                * Proto_GreyboxPersp2DVisualTuning.PorchDebugScaleMul;
            _debugActor.transform.position = new Vector3(x, y, 0f);
            _debugActor.transform.localScale = Vector3.one * scale;
            var bld = FindBuilding(buildingId);
            if (bld != null)
            {
                Proto_GreyboxPersp2DMath.ActorWorldFromFootScreen2x(x2x, feetY2x, out var wx, out var wz);
                bool frontOver = Proto_GreyboxPersp2DMath.PorchFrontDrawsOverActor(wx, wz, bld.porchEdgeX, bld.nearPorchZ);
                int frontOrder = FindBuildingFrontSort(buildingId);
                sr.sortingOrder = behindPorch
                    ? (frontOver ? frontOrder - 12 : frontOrder - 4)
                    : (frontOver ? frontOrder + 8 : Proto_GreyboxLayout.SortingOrderAtFootY(y));
            }
        }

        int FindBuildingFrontSort(string buildingId)
        {
            if (_layerRoot == null) return -392;
            var front = _layerRoot.Find("P2D_" + buildingId + "_front");
            if (front != null)
            {
                var fsr = front.GetComponent<SpriteRenderer>();
                if (fsr != null) return fsr.sortingOrder;
            }

            return -392;
        }

        public void WritePorchBehindCheck(Camera cam, System.Text.StringBuilder sb)
        {
            sb.AppendLine("porch_behind_capture_check");
            if (_debugActor == null)
            {
                sb.AppendLine("debug_actor=null");
                return;
            }

            var asr = _debugActor.GetComponent<SpriteRenderer>();
            if (asr == null)
            {
                sb.AppendLine("sprite=null");
                return;
            }

            Proto_GreyboxScreenMetrics.FromBoundsViaCamera(cam, asr.bounds, out var top, out var bottom, out var h,
                out var left, out var right, out var cx);
            sb.AppendLine($"bandit_bbox_pct: left={left:F1} right={right:F1} top={top:F1} bottom={bottom:F1} h={h:F2} cx={cx:F1}");
            var front = _layerRoot != null ? _layerRoot.Find("P2D_saloon_L_front") : null;
            var fsr = front != null ? front.GetComponent<SpriteRenderer>() : null;
            if (fsr == null)
            {
                sb.AppendLine("saloon_front=null");
                return;
            }

            sb.AppendLine($"saloon_front_sort={fsr.sortingOrder} actor_sort={asr.sortingOrder}");
            sb.AppendLine($"actor_behind_front_layer={asr.sortingOrder < fsr.sortingOrder}");
            float midY = (top + bottom) * 0.5f;
            float legTop = midY;
            float legBottom = bottom;
            Proto_GreyboxScreenMetrics.FromBoundsViaCamera(cam, fsr.bounds, out var fTop, out var fBottom, out _, out var fLeft,
                out var fRight, out _);
            bool overlapX = right >= fLeft && left <= fRight;
            bool overlapLegs = overlapX && legBottom < fBottom && legTop > fTop;
            sb.AppendLine($"front_bbox_pct: left={fLeft:F1} right={fRight:F1} top={fTop:F1} bottom={fBottom:F1}");
            sb.AppendLine($"lower_half_overlap_railing={overlapLegs && asr.sortingOrder < fsr.sortingOrder}");
        }

        readonly List<SpriteRenderer> _bandDebug = new();

        public void SetDepthBandDebug(bool on)
        {
            foreach (var sr in _bandDebug)
            {
                if (sr != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) Object.DestroyImmediate(sr.gameObject);
                    else
#endif
                        Object.Destroy(sr.gameObject);
                }
            }
            _bandDebug.Clear();
            if (!on || _layerRoot == null) return;
            MakeBandLine(Proto_GreyboxPersp2DMath.HorizonY2x / DesignH * 100f, new Color(1f, 0.2f, 0.2f, 0.85f));
            MakeBandLine(Proto_GreyboxPerspectiveDepth.Persp2DEnemyFootMinPercent, new Color(1f, 0.85f, 0.2f, 0.7f));
            MakeBandLine(Proto_GreyboxPerspectiveDepth.Persp2DEnemyFootMaxPercent, new Color(1f, 0.85f, 0.2f, 0.7f));
            MakeBandLine(Proto_GreyboxPerspectiveDepth.Persp2DMoveFootMinPercent, new Color(0.2f, 1f, 0.35f, 0.7f));
            MakeBandLine(Proto_GreyboxPerspectiveDepth.Persp2DMoveFootMaxPercent, new Color(0.2f, 1f, 0.35f, 0.7f));
        }

        void MakeBandLine(float yPctFromTop, Color color)
        {
            var go = new GameObject("P2D_Band");
            go.transform.SetParent(_layerRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            var tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sr.color = color;
            sr.sortingOrder = 500;
            float y = Proto_GreyboxLayout.ScreenYPercentToWorldY(yPctFromTop);
            float w = Proto_GreyboxLayout.WorldWidth;
            go.transform.position = new Vector3(0f, y, 0f);
            go.transform.localScale = new Vector3(w, 0.02f, 1f);
            _bandDebug.Add(sr);
        }
    }
}
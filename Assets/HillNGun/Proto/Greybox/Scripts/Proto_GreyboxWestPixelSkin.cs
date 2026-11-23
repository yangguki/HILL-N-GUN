using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    /// <summary>절차적 그레이박스 위에 웨스턴 픽셀 스프라이트 레이어(기본 ON). 게임플레이 좌표/히트는 건드리지 않음.</summary>
    public class Proto_GreyboxWestPixelSkin
    {
        const float PlayerBodyPx = 204f;
        const float PlayerTotalPx = 213f;
        const float GruntBodyPx = 120f;
        const float GruntTotalPx = 147f;
        const float BgFarTopPct = 0f;
        const float BgFarHeightPct = 420f / 1080f * 100f;
        const float BgTownTopPct = 120f / 1080f * 100f;
        const float GroundTopPct = 360f / 1080f * 100f;
        const string LayerRootName = "Greybox_WestPixel_Layer";

        readonly Proto_GreyboxPlayerController _host;
        Proto_GreyboxWestPixelCatalog _catalog;
        Transform _layerRoot;
        bool _stageBound;

        float _playerAnimT;
        int _playerFrame;
        PlayerAnimState _playerState;
        float _playerShootT;
        float _playerIdleFlip;
        bool _playerFlipX;

        readonly Dictionary<Proto_GreyboxGrunt, GruntVisualState> _gruntVisuals = new();
        readonly HashSet<Proto_GreyboxGrunt> _scoredDeaths = new();
        float _simDt;

        readonly List<SpriteRenderer> _sparkPool = new();
        readonly List<SparkFx> _sparks = new();
        SpriteRenderer _muzzle;
        float _muzzleT;

        Transform _moveBoundaryRoot;
        float _moveBoundaryTopPct = -1f;
        readonly List<SpriteRenderer> _moveBoundaryTopSegs = new();
        SpriteRenderer _moveBoundaryLeft;
        SpriteRenderer _moveBoundaryRight;
        const float MoveBoundaryHighlightDist = 0.4f;

        readonly List<SpriteRenderer> _gruntMuzzlePool = new();
        readonly List<GruntMuzzleFx> _gruntMuzzles = new();
        Proto_GreyboxFxV2Catalog _fxV2;
        Proto_GreyboxRollFacing _rollFacing;
        readonly List<SpriteRenderer> _casingPool = new();
        SpriteRenderer _dizzyFxOverlay;
        SpriteRenderer _reloadCasingSr;
        float _reloadCasingT;
        Sprite[] _reloadCasingClip;
        Proto_GreyboxRollDizzyPhase _dizzyPhase = Proto_GreyboxRollDizzyPhase.None;
        float _dizzyElapsed;
        bool _rollVisualActive;
        bool _skipRollFrame10;
        float _rollVisualT;
        int _muzzleFxFrame;
        float _muzzleFxT;

        Image _hpFrameOverlay;
        Image _hpFillOverlay;
        readonly List<GameObject> _hiddenGrey = new();

        class GruntVisualState
        {
            public bool Dying;
            public bool Finished;
            public float DeathElapsed;
            public int IdleFrame;
            public float ShootT;
            public float IdleT;
        }

        class SparkFx
        {
            public SpriteRenderer Sr;
            public float T;
            public int Frame;
            public Sprite[] Seq;
            public float SeqFps;
        }

        class GruntMuzzleFx
        {
            public SpriteRenderer Sr;
            public float T;
        }

        enum PlayerAnimState
        {
            Idle,
            Run,
            Shoot,
            Roll,
            Hit
        }

        public bool IsActive => Proto_GreyboxWestPixelSkinState.Enabled && _catalog != null && _catalog.IsGameplayReady;

        /// <summary>Slice J: the flat 2D chibi catalog is bound (same 2D gameplay, different sprites).</summary>
        public bool IsChibiStyle => IsActive && _catalog.isChibi;

        public bool UsesFxV2DizzyBody =>
            !IsChibiStyle && _fxV2 != null && _fxV2.dizzyLightBody != null && _fxV2.dizzyLightBody.Length > 0
            && _dizzyPhase != Proto_GreyboxRollDizzyPhase.None;

        public Proto_GreyboxWestPixelSkin(Proto_GreyboxPlayerController host) => _host = host;

        public void EnsureCatalog()
        {
            if (_catalog == null)
                _catalog = Proto_GreyboxWestPixelCatalog.LoadForCurrentStyle();
            if (_fxV2 == null)
                _fxV2 = Proto_GreyboxFxV2Catalog.LoadRuntime();
        }

        public void SetEnabled(bool on)
        {
            Proto_GreyboxWestPixelSkinState.Enabled = on;
            if (IsActive)
                BindStage();
            else
                UnbindStage();
        }

        public void EnsureBound()
        {
            EnsureCatalog();
            if (IsActive)
                BindStage();
            else if (_stageBound)
                UnbindStage();
        }

        /// <summary>
        /// Swap pixel / chibi catalogs to match the view mode. In 3D the 2D world is hidden, so the
        /// catalog is left alone (rebinding would overwrite the 3D HUD skin on shared HUD objects).
        /// </summary>
        public void SyncStyle()
        {
            if (Proto_GreyboxViewModeState.Is3D)
            {
                Proto_GreyboxChibiHud.SetOverlayVisible(false);
                return;
            }

            var want = Proto_GreyboxWestPixelCatalog.LoadForCurrentStyle();
            if (want == null)
                return;
            if (_catalog != want)
            {
                if (_stageBound)
                    UnbindStage();
                _catalog = want;
                if (IsActive)
                    BindStage();
                return;
            }

            // Same style (e.g. back from 3D, whose HUD restore re-shows every stock widget).
            if (IsActive && _stageBound)
            {
                ApplyHudSkin();
                Proto_GreyboxChibiHud.SetOverlayVisible(_catalog.isChibi);
            }
        }

        void BindStage()
        {
            if (_stageBound && _layerRoot != null) return;
            HideProceduralStage();
            BuildWestPixelStage();
            ApplyHudSkin();
            ApplyCrosshairSkin();
            ApplyCrateSkins();
            _stageBound = true;
        }

        void UnbindStage()
        {
            RestoreProceduralStage();
            RestoreHudSkin();
            RestoreCrosshairSkin();
            RestoreCrateSkins();
            if (_layerRoot != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(_layerRoot.gameObject);
                else
#endif
                    Object.Destroy(_layerRoot.gameObject);
                _layerRoot = null;
            }

            _stageBound = false;
            _gruntVisuals.Clear();
            _scoredDeaths.Clear();
            _sparks.Clear();
            _muzzleT = 0f;
        }

        void HideProceduralStage()
        {
            _hiddenGrey.Clear();
            HideIfExists(Proto_GreyboxStageBuilder.SkyName);
            HideIfExists(Proto_GreyboxStageBuilder.GroundName);
            HideIfExists(Proto_GreyboxStageBuilder.FenceName);
            var root = GameObject.Find(Proto_GreyboxStageBuilder.RootName);
            if (root == null) return;
            foreach (Transform child in root.transform)
            {
                var n = child.name;
                if (n.StartsWith("Greybox_Building"))
                    HideGo(child.gameObject);
            }
        }

        void RestoreProceduralStage()
        {
            foreach (var go in _hiddenGrey)
            {
                if (go != null)
                    go.SetActive(true);
            }

            _hiddenGrey.Clear();
        }

        void HideIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                HideGo(go);
        }

        void HideGo(GameObject go)
        {
            if (go == null || !go.activeSelf) return;
            go.SetActive(false);
            _hiddenGrey.Add(go);
        }

        void BuildWestPixelStage()
        {
            var stage = GameObject.Find(Proto_GreyboxStageBuilder.RootName);
            var parent = stage != null ? stage.transform : _host.transform.parent;
            if (_layerRoot == null)
            {
                var layerGo = new GameObject(LayerRootName);
                layerGo.transform.SetParent(parent, false);
                _layerRoot = layerGo.transform;
            }

            for (int i = _layerRoot.childCount - 1; i >= 0; i--)
            {
                var c = _layerRoot.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(c.gameObject);
                else
#endif
                    Object.Destroy(c.gameObject);
            }

            float bgUniform = ComputeBgUniformScale();
            if (_catalog.bgFullScene != null)
            {
                // Chibi: one painted plate (sky + street + buildings lining it), fence on top.
                PlaceTopLeftSprite("WP_FullScene", _catalog.bgFullScene, 0f, bgUniform, Proto_GreyboxLayout.SortingSky);
                if (Proto_GreyboxPerspectiveDepth.Enabled && _catalog.perspFence != null)
                    BuildPerspectiveFenceTiles(bgUniform);
                else
                    BuildFlatFenceTiles(bgUniform);
            }
            else if (Proto_GreyboxPerspectiveDepth.Enabled && _catalog.HasPerspectiveBackground)
                BuildPerspectiveBackground(bgUniform);
            else
                BuildFlatBackground(bgUniform);
            BuildMoveBoundary();
        }

        void BuildFlatBackground(float bgUniform)
        {
            PlaceTopLeftSprite("WP_BgFar", _catalog.bgFar, BgFarTopPct, bgUniform, Proto_GreyboxLayout.SortingSky);
            if (_catalog.ground != null)
                PlaceTopLeftSprite("WP_Ground", _catalog.ground, GroundTopPct, bgUniform, Proto_GreyboxLayout.SortingGround);
            PlaceTopLeftSprite("WP_BgTown", _catalog.bgTown, BgTownTopPct, bgUniform, Proto_GreyboxLayout.SortingBuildingBase);
            BuildFlatFenceTiles(bgUniform);
        }

        void BuildPerspectiveBackground(float bgUniform)
        {
            const float invH = 100f / 1080f;
            PlaceTopLeftSprite("WP_BgSkyFar", _catalog.bgSkyFar, 0f, bgUniform, Proto_GreyboxLayout.SortingSky);
            PlaceTopLeftSprite("WP_PerspGround", _catalog.perspGround, 348f * invH, bgUniform, Proto_GreyboxLayout.SortingGround);
            PlaceTopLeftSprite("WP_BldLeft", _catalog.bldLeft, 270f * invH, bgUniform, Proto_GreyboxLayout.SortingBuildingBase);
            PlaceTopLeftSprite("WP_BldRight", _catalog.bldRight, 273f * invH, bgUniform, Proto_GreyboxLayout.SortingBuildingBase,
                50f);
            BuildPerspectiveFenceTiles(bgUniform);
        }

        void BuildPerspectiveFenceTiles(float bgUniform)
        {
            if (_catalog.perspFence == null) return;
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.FenceBottomPercent);
            float tileWpx = _catalog.perspFence.rect.width;
            var fenceRoot = new GameObject("WP_Fence");
            fenceRoot.transform.SetParent(_layerRoot, false);
            float xPx = 0f;
            while (xPx < 860f - tileWpx + 1f)
            {
                PlaceFenceBottomLeftPx("tile_l_" + xPx, _catalog.perspFence, xPx, footY, bgUniform, fenceRoot.transform);
                xPx += tileWpx;
            }

            if (_catalog.perspFencePost != null)
                PlaceFenceBottomLeftPx("post_gap", _catalog.perspFencePost, xPx, footY, bgUniform, fenceRoot.transform);

            float xRight = 1920f;
            while (xRight > 1060f)
            {
                xRight -= tileWpx;
                PlaceFenceBottomLeftPx("tile_r_" + xRight, _catalog.perspFence, xRight, footY, bgUniform, fenceRoot.transform);
            }
        }

        void PlaceFenceBottomLeftPx(string name, Sprite sprite, float xPx, float footY, float uniformScale, Transform parent)
        {
            if (sprite == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Proto_GreyboxLayout.SortingFence;
            float x = Proto_GreyboxLayout.ScreenXPercentToWorldX(xPx / 1920f * 100f);
            go.transform.position = new Vector3(x, footY, 0f);
            go.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }

        void BuildPerspectiveCrates(float bgUniform)
        {
            PlaceCrateBottomCenter("WP_CrateD40", _catalog.crateD40, 0.51f, 0.40f, bgUniform);
            PlaceCrateBottomCenter("WP_CrateD46", _catalog.crateD46, 0.40f, 0.46f, bgUniform);
            PlaceCrateBottomCenter("WP_CrateD52_A", _catalog.crateD52, 0.72f, 0.52f, bgUniform);
            PlaceCrateBottomCenter("WP_CrateD52_B", _catalog.crateD52, 0.24f, 0.52f, bgUniform);
        }

        void PlaceCrateBottomCenter(string name, Sprite sprite, float xFrac, float yFrac, float uniformScale)
        {
            if (sprite == null || _layerRoot == null) return;
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(yFrac * 100f);
            var go = new GameObject(name);
            go.transform.SetParent(_layerRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Proto_GreyboxLayout.WorldSpriteSortingOrder(footY);
            float x = Proto_GreyboxLayout.ScreenXPercentToWorldX(xFrac * 100f);
            go.transform.position = new Vector3(x, footY, 0f);
            go.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }

        float ComputeBgUniformScale()
        {
            Sprite basis = null;
            if (_catalog.bgFullScene != null)
                basis = _catalog.bgFullScene;
            else if (Proto_GreyboxPerspectiveDepth.Enabled && _catalog.bgSkyFar != null)
                basis = _catalog.bgSkyFar;
            else if (_catalog.bgFar != null)
                basis = _catalog.bgFar;
            if (basis == null) return 1f;
            float spriteW = basis.rect.width / Proto_GreyboxWestPixelCatalog.ImportPpu;
            return Proto_GreyboxLayout.WorldWidth / Mathf.Max(spriteW, 0.01f);
        }

        void PlaceTopLeftSprite(string name, Sprite sprite, float topPct, float uniformScale, int sortOrder,
            float leftEdgePercent = 0f)
        {
            if (sprite == null || _layerRoot == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(_layerRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortOrder;
            float w = sprite.rect.width / Proto_GreyboxWestPixelCatalog.ImportPpu * uniformScale;
            float h = sprite.rect.height / Proto_GreyboxWestPixelCatalog.ImportPpu * uniformScale;
            float left = Proto_GreyboxLayout.ScreenXPercentToWorldX(leftEdgePercent);
            float topY = Proto_GreyboxLayout.ScreenYPercentToWorldY(topPct);
            go.transform.position = new Vector3(left + w * 0.5f, topY - h * 0.5f, 0f);
            go.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }

        void BuildFlatFenceTiles(float bgUniform)
        {
            if (_catalog.fence == null) return;
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.FenceBottomPercent);
            float topY = Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.FenceTopPercent);
            float bandH = topY - footY;
            float tileW = _catalog.fence.rect.width / Proto_GreyboxWestPixelCatalog.ImportPpu * bgUniform;
            float scaleY = bandH / Mathf.Max(_catalog.fence.rect.height / Proto_GreyboxWestPixelCatalog.ImportPpu * bgUniform, 0.001f);
            float left = -Proto_GreyboxLayout.WorldWidth * 0.5f;
            float right = Proto_GreyboxLayout.WorldWidth * 0.5f;
            float x = left;
            int i = 0;
            var fenceRoot = new GameObject("WP_Fence");
            fenceRoot.transform.SetParent(_layerRoot, false);
            while (x < right - tileW * 0.25f)
            {
                var go = new GameObject("tile_" + i++);
                go.transform.SetParent(fenceRoot.transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _catalog.fence;
                sr.sortingOrder = Proto_GreyboxLayout.SortingFence;
                go.transform.position = new Vector3(x, footY, 0f);
                go.transform.localScale = new Vector3(bgUniform, bgUniform * scaleY, 1f);
                x += tileW;
            }

            if (_catalog.fencePost != null)
            {
                var post = new GameObject("WP_FencePost");
                post.transform.SetParent(fenceRoot.transform, false);
                var psr = post.AddComponent<SpriteRenderer>();
                psr.sprite = _catalog.fencePost;
                psr.sortingOrder = Proto_GreyboxLayout.SortingFence + 1;
                float postW = _catalog.fencePost.rect.width / Proto_GreyboxWestPixelCatalog.ImportPpu * bgUniform;
                post.transform.position = new Vector3(right - postW * 0.5f, footY, 0f);
                post.transform.localScale = new Vector3(bgUniform, bgUniform * scaleY, 1f);
            }
        }

        void ApplyCrateSkins()
        {
            if (Proto_GreyboxPerspectiveDepth.Enabled && _catalog.crateD40 != null)
            {
                HideIfExists(Proto_GreyboxStageBuilder.CrateFarName);
                HideIfExists(Proto_GreyboxStageBuilder.CrateMidName);
                BuildPerspectiveCrates(ComputeBgUniformScale());
                SkinCrate(Proto_GreyboxStageBuilder.CrateNearName, _catalog.crateNear);
            }
            else
            {
                SkinCrate(Proto_GreyboxStageBuilder.CrateFarName, _catalog.crateFar);
                SkinCrate(Proto_GreyboxStageBuilder.CrateMidName, _catalog.crateNear);
                SkinCrate(Proto_GreyboxStageBuilder.CrateNearName, _catalog.crateNear);
            }
        }

        void RestoreCrateSkins()
        {
            RestoreCrate(Proto_GreyboxStageBuilder.CrateFarName);
            RestoreCrate(Proto_GreyboxStageBuilder.CrateMidName);
            RestoreCrate(Proto_GreyboxStageBuilder.CrateNearName);
        }

        static void SkinCrate(string name, Sprite sprite)
        {
            if (sprite == null) return;
            var go = GameObject.Find(name);
            if (go == null) return;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            var bounds = sr.bounds;
            sr.sprite = sprite;
            sr.color = Color.white;
            float worldW = bounds.size.x;
            float worldH = bounds.size.y;
            go.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(sprite, worldW, worldH);
        }

        static void RestoreCrate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) return;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            sr.sprite = Proto_GreyboxSpriteUtil.GetCrateSprite();
        }

        void ApplyHudSkin()
        {
            var hud = GameObject.Find("Greybox_Hud");
            if (hud == null) return;
            SkinHudButton(Proto_GreyboxHudBuilder.FireButtonName, _catalog.btnFire, true);
            SkinHudButton(Proto_GreyboxHudBuilder.ReloadButtonName, _catalog.btnReload, true);
            SkinHudButton(Proto_GreyboxHudBuilder.GrenadeButtonName, _catalog.btnGrenade, true);
            SkinHudButton(Proto_GreyboxHudBuilder.RollButtonName, _catalog.btnRoll, true);
            SkinHudButton("Greybox_Hud_Pause", _catalog.btnPause, true);
            SkinHudButton(Proto_GreyboxHudBuilder.JoystickRingName, _catalog.joystickBase, false);
            SkinHudButton(Proto_GreyboxHudBuilder.JoystickKnobName, _catalog.joystickKnob, false);
            HideChildIcon(Proto_GreyboxHudBuilder.FireButtonName);
            HideChildIcon(Proto_GreyboxHudBuilder.ReloadButtonName);
            HideChildIcon(Proto_GreyboxHudBuilder.GrenadeButtonName);
            HideChildIcon(Proto_GreyboxHudBuilder.RollButtonName);
            HideHudTextLabel("Greybox_Hud_FireLabel");
            HideHudTextLabel("Greybox_Hud_RollLabel");
            HideHudTextLabel("Greybox_Hud_GrenadeLabel");
            // ReloadLabel stays active so UpdateHud can show 장전! (GameObject.Find skips inactive).
            HideHudTextLabel(Proto_GreyboxHudBuilder.HpTextName);
            ApplyHpBarSkin(hud.transform);
            ApplyScoreOutline(hud.transform, Proto_GreyboxHudBuilder.ScoreTextName);
            ApplyScoreOutline(hud.transform, Proto_GreyboxHudBuilder.WaveTextName);
            var pauseLabel = GameObject.Find("Greybox_Hud_PauseLabel");
            if (pauseLabel != null)
                pauseLabel.SetActive(false);
            if (_catalog.isChibi)
                Proto_GreyboxChibiHud.Apply(hud.transform, _catalog);
            else
                Proto_GreyboxChibiHud.Restore();
        }

        void RestoreHudSkin()
        {
            Proto_GreyboxChibiHud.Restore();
            ShowHudTextLabel("Greybox_Hud_FireLabel");
            ShowHudTextLabel("Greybox_Hud_RollLabel");
            ShowHudTextLabel("Greybox_Hud_GrenadeLabel");
            ShowHudTextLabel("Greybox_Hud_ReloadLabel");
            ShowHudTextLabel(Proto_GreyboxHudBuilder.HpTextName);
            RestoreHudButton(Proto_GreyboxHudBuilder.FireButtonName, Proto_GreyboxPalette.HudFire);
            RestoreHudButton(Proto_GreyboxHudBuilder.ReloadButtonName, Proto_GreyboxPalette.HudActionDisc);
            RestoreHudButton(Proto_GreyboxHudBuilder.GrenadeButtonName, Proto_GreyboxPalette.HudActionDisc);
            RestoreHudButton(Proto_GreyboxHudBuilder.RollButtonName, Proto_GreyboxPalette.HudActionDisc);
            RestoreHudButton("Greybox_Hud_Pause", Proto_GreyboxPalette.HudGlass);
            RestoreHudButton(Proto_GreyboxHudBuilder.JoystickRingName, Color.white, true);
            RestoreHudButton(Proto_GreyboxHudBuilder.JoystickKnobName, new Color(1f, 1f, 1f, 0.96f), false);
            if (_hpFrameOverlay != null) Object.Destroy(_hpFrameOverlay.gameObject);
            if (_hpFillOverlay != null) Object.Destroy(_hpFillOverlay.gameObject);
            _hpFrameOverlay = null;
            _hpFillOverlay = null;
        }

        static void SkinHudButton(string name, Sprite sprite, bool preserveRaycast)
        {
            if (sprite == null) return;
            var go = GameObject.Find(name);
            if (go == null) return;
            var img = go.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sprite;
            img.color = Color.white;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = preserveRaycast;
        }

        static void RestoreHudButton(string name, Color color, bool ring = false)
        {
            var go = GameObject.Find(name);
            if (go == null) return;
            var img = go.GetComponent<Image>();
            if (img == null) return;
            img.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(ring);
            img.color = color;
        }

        static void HideChildIcon(string buttonName)
        {
            var icon = GameObject.Find(buttonName + "_Icon");
            if (icon != null)
                icon.SetActive(false);
        }

        static void HideHudTextLabel(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                go.SetActive(false);
        }

        static void ShowHudTextLabel(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                go.SetActive(true);
        }

        void ApplyHpBarSkin(Transform hud)
        {
            var track = hud.Find("Greybox_Hud_HpBarTrack");
            if (track != null)
                track.gameObject.SetActive(false);
            var legacyFill = GameObject.Find(Proto_GreyboxHudBuilder.HpBarFillName);
            if (legacyFill != null)
                legacyFill.SetActive(false);
        }

        static void ApplyScoreOutline(Transform hud, string textName)
        {
            var go = GameObject.Find(textName);
            if (go == null) return;
            var text = go.GetComponent<Text>();
            if (text == null) return;
            text.fontStyle = FontStyle.Bold;
            var outline = go.GetComponent<Outline>();
            if (outline == null)
                outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.1f, 0.08f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        void ApplyCrosshairSkin()
        {
            var ch = GameObject.Find("Greybox_Crosshair");
            if (ch == null || _catalog.crosshair == null) return;
            var sr = ch.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            sr.sprite = _catalog.crosshair;
            sr.color = Color.white;
            float h = Proto_GreyboxLayout.ScreenPercentToWorldHeight(5f);
            ch.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(_catalog.crosshair, h);
            var aim = Proto_GreyboxAim.DefaultAimGround;
            ch.transform.position = new Vector3(aim.x, aim.y, 0f);
        }

        void RestoreCrosshairSkin()
        {
            var ch = GameObject.Find("Greybox_Crosshair");
            if (ch == null) return;
            var sr = ch.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            float w = Proto_GreyboxLayout.WorldWidth * 0.03f;
            float h = Proto_GreyboxLayout.ScreenPercentToWorldHeight(5f);
            sr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            sr.color = new Color(0.82f, 0.72f, 0.38f, 0.85f);
            ch.transform.localScale = new Vector3(w, h, 1f);
        }

        public void SetSimDt(float dt) => _simDt = dt;

        public void TickFrame(float dt, Proto_GreyboxInput input)
        {
            _simDt = dt;
            EnsureBound();
            if (!IsActive) return;
            TickPlayerAnim(dt, input);
            TickMuzzle(dt);
            TickGruntMuzzles(dt);
            TickSparks(dt);
            if (_catalog.isChibi)
                Proto_GreyboxChibiHud.Tick(_catalog);
            EnsureMoveBoundaryUpToDate();
            UpdateMoveBoundaryHighlight(_host.Motor.FootPosition);
        }

        public void RebuildMoveBoundary()
        {
            if (!IsActive || _layerRoot == null) return;
            BuildMoveBoundary();
            UpdateMoveBoundaryHighlight(_host.Motor.FootPosition);
        }

        public void RebuildStageLayout()
        {
            if (!IsActive || _layerRoot == null) return;
            HideProceduralStage();
            for (int i = _layerRoot.childCount - 1; i >= 0; i--)
            {
                var c = _layerRoot.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(c.gameObject);
                else
#endif
                    Object.Destroy(c.gameObject);
            }

            BuildWestPixelStage();
            ApplyCrateSkins();
            ApplyCrosshairSkin();
            BuildMoveBoundary();
        }

        public void UpdateHpFill(float fill01)
        {
            if (_hpFillOverlay != null)
                _hpFillOverlay.fillAmount = Mathf.Clamp01(fill01);
        }

        void TickPlayerAnim(float dt, Proto_GreyboxInput input)
        {
            var sr = _host.GetComponent<SpriteRenderer>();
            if (sr == null || _catalog.playerIdle == null || _catalog.playerIdle.Length == 0) return;

            var motor = _host.Motor;
            var roll = _host.Roll;
            bool moving = input.Move.sqrMagnitude > 0.04f && !roll.IsRolling;

            var aimX = _host.Aim.AimGroundPosition().x - motor.FootPosition.x;
            _playerFlipX = aimX < -0.02f;

            PlayerAnimState next;
            if (_host.PlayerHitFlashActive && _catalog.playerHit != null && _catalog.playerHit.Length > 0)
                next = PlayerAnimState.Hit;
            else if (roll.IsRolling || _rollVisualActive || UsesFxV2DizzyBody)
                next = PlayerAnimState.Roll;
            else if (_playerShootT > 0f)
                next = PlayerAnimState.Shoot;
            else if (moving)
                next = PlayerAnimState.Run;
            else
                next = PlayerAnimState.Idle;

            if (_playerState != next)
            {
                _playerState = next;
                _playerFrame = 0;
                _playerAnimT = 0f;
            }

            _playerShootT = Mathf.Max(0f, _playerShootT - dt);
            _playerAnimT += dt;

            Sprite frame = PickPlayerFrame();
            if (frame != null)
            {
                sr.sprite = frame;
                sr.flipX = _playerFlipX;
            }

            var footPos = motor.FootPosition;
            float targetBodyH = Proto_GreyboxPerspectiveDepth.PlayerBodyHeightWorld(footPos.y);
            float sy = targetBodyH / (PlayerBodyPx / Proto_GreyboxWestPixelCatalog.ImportPpu);
            bool fxRoll = IsChibiStyle || (_fxV2 != null && _fxV2.HasGameplayRoll);
            float rollSy = roll.IsRolling && !fxRoll ? 0.6f : 1f;
            _host.transform.localScale = new Vector3(sy, sy * rollSy, 1f);
            if (roll.IsRolling || _rollVisualActive)
                _rollVisualT += dt;
            TickReloadCasingAnim(dt);
            var foot = motor.FootPosition;
            sr.sortingOrder = Proto_GreyboxLayout.WorldSpriteSortingOrder(foot.y);
        }

        Sprite PickPlayerFrame()
        {
            switch (_playerState)
            {
                case PlayerAnimState.Shoot:
                    if (_catalog.playerShoot != null && _catalog.playerShoot.Length > 0)
                        return _catalog.playerShoot[Mathf.Clamp(_playerFrame, 0, _catalog.playerShoot.Length - 1)];
                    break;
                case PlayerAnimState.Roll:
                    if (UsesFxV2DizzyBody)
                    {
                        var dizzyBody = PickDizzyBodySprite();
                        if (dizzyBody != null)
                            return dizzyBody;
                    }

                    var rollFrames = !IsChibiStyle && _fxV2 != null ? _fxV2.RollFrames(_rollFacing) : null;
                    if (rollFrames == null || rollFrames.Length == 0)
                        rollFrames = _catalog.playerRoll;
                    if (rollFrames != null && rollFrames.Length > 0)
                    {
                        const float rollFps = 22f;
                        int maxFrame = _skipRollFrame10 ? 9 : 10;
                        int frame = Mathf.Clamp(Mathf.FloorToInt(_rollVisualT * rollFps), 0, maxFrame);
                        if (!_host.Roll.IsRolling && frame >= maxFrame)
                            _rollVisualActive = false;
                        return rollFrames[Mathf.Clamp(frame, 0, rollFrames.Length - 1)];
                    }

                    break;
                case PlayerAnimState.Run:
                    if (_catalog.playerRun != null && _catalog.playerRun.Length > 0)
                    {
                        if (_playerAnimT > 0.08f) { _playerAnimT = 0f; _playerFrame++; }
                        return _catalog.playerRun[_playerFrame % _catalog.playerRun.Length];
                    }

                    break;
                case PlayerAnimState.Hit:
                    if (_catalog.playerHit != null && _catalog.playerHit.Length > 0)
                        return _catalog.playerHit[0];
                    break;
                default:
                    if (_catalog.playerIdle != null && _catalog.playerIdle.Length > 0)
                    {
                        if (_playerAnimT > 0.45f) { _playerAnimT = 0f; _playerFrame ^= 1; }
                        return _catalog.playerIdle[_playerFrame % _catalog.playerIdle.Length];
                    }

                    break;
            }

            return _catalog.playerIdle != null && _catalog.playerIdle.Length > 0 ? _catalog.playerIdle[0] : null;
        }

        public void OnPlayerFired()
        {
            if (!IsActive) return;
            _playerShootT = 0.22f;
            _playerState = PlayerAnimState.Shoot;
            _playerFrame = 0;
            TriggerMuzzle();
        }

        public void OnReloadStartCasing(int count)
        {
            if (count <= 0) return;
            EnsureCatalog();
            var clip = _fxV2?.ReloadCasingClip(count);
            if (clip != null && clip.Length > 0)
            {
                EnsureReloadCasingSr();
                _reloadCasingClip = clip;
                _reloadCasingT = 0f;
                var foot = _host.Motor.FootPosition;
                var aim = _host.Aim.AimGroundPosition();
                var dir = (aim - foot).normalized;
                if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
                float gunY = foot.y + Proto_GreyboxPerspectiveDepth.PlayerBodyHeightWorld(foot.y) * 0.55f;
                _reloadCasingSr.transform.position = new Vector3(foot.x + dir.x * 0.35f, gunY, 0f);
                _reloadCasingSr.transform.localScale =
                    Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(clip[0],
                        0.35f * Proto_GreyboxFxV2Catalog.SmallFxWorldMul);
                _reloadCasingSr.sprite = clip[0];
                _reloadCasingSr.gameObject.SetActive(true);
                return;
            }

            var footFallback = _host.Motor.FootPosition;
            int spawn = Mathf.Min(count, 6);
            for (int i = 0; i < spawn; i++)
                SpawnCasingFx(footFallback + new Vector2(-0.08f + i * 0.03f, 0.05f));
        }

        public void OnRollStarted(Proto_GreyboxRollFacing facing)
        {
            _rollFacing = facing;
            _rollVisualT = 0f;
            _rollVisualActive = true;
            _skipRollFrame10 = false;
            var foot = _host.Motor.FootPosition;
            var rollDir = _host.Roll.ActiveDirection;
            if (rollDir.sqrMagnitude < 1e-4f) rollDir = Vector2.right;
            SpawnRollDust(foot - rollDir.normalized * 0.08f, true);
        }

        public void OnRollEnded(Proto_GreyboxRollFacing facing, Proto_GreyboxRollDizzyPhase dizzyAfter)
        {
            _rollFacing = facing;
            _skipRollFrame10 = dizzyAfter != Proto_GreyboxRollDizzyPhase.None;
            SpawnRollDust(_host.Motor.FootPosition, false);
        }

        public void SyncDizzyPhase(Proto_GreyboxRollDizzyPhase phase, float elapsed)
        {
            _dizzyPhase = phase;
            _dizzyElapsed = elapsed;
            EnsureDizzyOverlay();
            if (_dizzyFxOverlay == null) return;
            if (phase == Proto_GreyboxRollDizzyPhase.None)
            {
                _dizzyFxOverlay.gameObject.SetActive(false);
                return;
            }

            var fxSprite = PickDizzyFxSprite();
            if (fxSprite == null)
            {
                _dizzyFxOverlay.gameObject.SetActive(false);
                return;
            }

            _dizzyFxOverlay.sprite = fxSprite;
            _dizzyFxOverlay.gameObject.SetActive(true);
            var foot = _host.Motor.FootPosition;
            _dizzyFxOverlay.transform.localPosition = Vector3.zero;
            _dizzyFxOverlay.sortingOrder = Proto_GreyboxLayout.WorldSpriteSortingOrder(foot.y) + 3;
        }

        public void SpawnBulletDust(Vector2 groundPos) => SpawnDustPuff(groundPos, false);

        void SpawnCasingFx(Vector2 groundPos)
        {
            var sr = RentSpark(_casingPool);
            sr.color = new Color(0.85f, 0.72f, 0.28f, 0.95f);
            sr.transform.position = new Vector3(groundPos.x, groundPos.y + 0.2f, 0f);
            sr.transform.localScale = Vector3.one * 0.08f;
            sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(groundPos.y) + 3;
            sr.gameObject.SetActive(true);
        }

        void SpawnRollDust(Vector2 groundPos, bool takeoff)
        {
            float fxScale = Proto_GreyboxPerspectiveDepth.PlayerVisualScaleAtFootWorldY(groundPos.y);
            var frames = takeoff ? _fxV2?.rollDustTakeoff : _fxV2?.rollDustLand;
            if (frames != null && frames.Length > 0)
            {
                var sr = RentSpark(_sparkPool);
                sr.sprite = frames[0];
                sr.color = Color.white;
                sr.transform.position = new Vector3(groundPos.x, groundPos.y, 0f);
                sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(frames[0], 0.2f * fxScale);
                sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(groundPos.y) + 1;
                sr.gameObject.SetActive(true);
                _sparks.Add(new SparkFx { Sr = sr, T = frames.Length / 16f, Frame = 0, Seq = frames, SeqFps = 16f });
                return;
            }

            SpawnDustPuff(groundPos, takeoff);
        }

        void SpawnDustPuff(Vector2 groundPos, bool start)
        {
            float fxScale = Proto_GreyboxPerspectiveDepth.PlayerVisualScaleAtFootWorldY(groundPos.y);
            var sr = RentSpark(_sparkPool);
            var groundFx = _fxV2?.dustGround;
            if (groundFx != null && groundFx.Length > 0)
            {
                sr.sprite = groundFx[0];
                sr.color = Color.white;
                sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(groundFx[0], 0.22f * fxScale);
                _sparks.Add(new SparkFx { Sr = sr, T = groundFx.Length / 16f, Frame = 0, Seq = groundFx, SeqFps = 16f });
            }
            else
            {
                sr.color = new Color(0.72f, 0.62f, 0.48f, start ? 0.55f : 0.42f);
                sr.transform.localScale = Vector3.one * ((start ? 0.22f : 0.18f) * fxScale);
                _sparks.Add(new SparkFx { Sr = sr, T = 0.35f, Frame = 0 });
            }

            sr.transform.position = new Vector3(groundPos.x, groundPos.y, 0f);
            sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(groundPos.y) + 1;
            sr.gameObject.SetActive(true);
        }

        SpriteRenderer RentSpark(List<SpriteRenderer> pool)
        {
            foreach (var sr in pool)
            {
                if (sr != null && !sr.gameObject.activeSelf)
                    return sr;
            }

            var go = new GameObject("Greybox_WestPixel_Fx");
            go.transform.SetParent(_layerRoot != null ? _layerRoot : _host.transform, false);
            var created = go.AddComponent<SpriteRenderer>();
            created.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
            pool.Add(created);
            return created;
        }

        public void OnPlayerHitFlash()
        {
            if (!IsActive) return;
            _playerState = PlayerAnimState.Hit;
            _playerFrame = 0;
            _playerAnimT = 0f;
        }

        void TriggerMuzzle()
        {
            var muzzle = _fxV2?.muzzleFlash != null && _fxV2.muzzleFlash.Length > 0
                ? _fxV2.muzzleFlash
                : _catalog.muzzleFlash;
            if (muzzle == null || muzzle.Length == 0) return;
            EnsureMuzzle();
            _muzzle.sprite = muzzle[0];
            _muzzleFxFrame = 0;
            _muzzleFxT = muzzle.Length / 30f;
            _muzzleT = Mathf.Max(Proto_GreyboxTuning.PlayerMuzzleFlashDuration, _muzzleFxT);
            var foot = _host.Motor.FootPosition;
            var aim = _host.Aim.AimGroundPosition();
            var dir = (aim - foot).normalized;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
            float bodyH = Proto_GreyboxPerspectiveDepth.PlayerBodyHeightWorld(foot.y);
            float visScale = Proto_GreyboxPerspectiveDepth.PlayerVisualScaleAtFootWorldY(foot.y);
            float dist = 0.42f * visScale;
            _muzzle.transform.position = new Vector3(foot.x + dir.x * dist, foot.y + dir.y * dist + bodyH * 0.55f, 0f);
            float diam = Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.PlayerMuzzleFlashHeightPercent) * visScale;
            _muzzle.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(muzzle[0], diam);
            _muzzle.flipX = _playerFlipX;
            _muzzle.gameObject.SetActive(true);
        }

        void EnsureMuzzle()
        {
            if (_muzzle != null) return;
            var go = new GameObject("Greybox_WestPixel_Muzzle");
            go.transform.SetParent(_layerRoot != null ? _layerRoot : _host.transform, false);
            _muzzle = go.AddComponent<SpriteRenderer>();
            _muzzle.sortingOrder = Proto_GreyboxLayout.SortingCombatOverlay + 50;
        }

        void TickMuzzle(float dt)
        {
            if (_muzzle == null || _muzzleT <= 0f) return;
            _muzzleT -= dt;
            var muzzle = _fxV2?.muzzleFlash != null && _fxV2.muzzleFlash.Length > 0
                ? _fxV2.muzzleFlash
                : _catalog.muzzleFlash;
            if (muzzle != null && muzzle.Length > 1)
            {
                _muzzleFxT -= dt;
                if (_muzzleFxT > 0f)
                {
                    int idx = Mathf.Clamp(Mathf.FloorToInt((muzzle.Length / 30f - _muzzleFxT) * 30f), 0, muzzle.Length - 1);
                    _muzzle.sprite = muzzle[idx];
                }
            }

            if (_muzzleT <= 0f)
                _muzzle.gameObject.SetActive(false);
        }

        void TriggerGruntMuzzle(Vector2 foot, Vector2 velocity)
        {
            if (_catalog.muzzleFlash == null || _catalog.muzzleFlash.Length == 0) return;
            var sr = RentGruntMuzzle();
            sr.sprite = _catalog.muzzleFlash[0];
            var dir = velocity.sqrMagnitude > 1e-6f ? velocity.normalized : Vector2.right;
            float chest = Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.TargetHeightPercent) * 0.28f;
            sr.transform.position = new Vector3(foot.x + dir.x * 0.15f, foot.y + chest, 0f);
            float diam = Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.GruntMuzzleFlashHeightPercent);
            sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(_catalog.muzzleFlash[0], diam);
            sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(foot.y) + 2;
            sr.gameObject.SetActive(true);
            _gruntMuzzles.Add(new GruntMuzzleFx { Sr = sr, T = Proto_GreyboxTuning.GruntMuzzleFlashDuration });
        }

        SpriteRenderer RentGruntMuzzle()
        {
            foreach (var s in _gruntMuzzlePool)
            {
                if (s != null && !s.gameObject.activeSelf)
                    return s;
            }

            var go = new GameObject("Greybox_WestPixel_GruntMuzzle");
            go.transform.SetParent(_layerRoot != null ? _layerRoot : _host.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            _gruntMuzzlePool.Add(sr);
            return sr;
        }

        void TickGruntMuzzles(float dt)
        {
            for (int i = _gruntMuzzles.Count - 1; i >= 0; i--)
            {
                var fx = _gruntMuzzles[i];
                fx.T -= dt;
                if (fx.Sr != null && _catalog.muzzleFlash != null && _catalog.muzzleFlash.Length > 1 &&
                    fx.T < Proto_GreyboxTuning.GruntMuzzleFlashDuration * 0.5f)
                    fx.Sr.sprite = _catalog.muzzleFlash[1];
                if (fx.T <= 0f)
                {
                    if (fx.Sr != null)
                        fx.Sr.gameObject.SetActive(false);
                    _gruntMuzzles.RemoveAt(i);
                }
            }
        }

        void EnsureMoveBoundaryUpToDate()
        {
            float pct = Proto_GreyboxMoveZone.FootYPercentMinCurrent;
            if (Mathf.Abs(pct - _moveBoundaryTopPct) > 0.01f)
                BuildMoveBoundary();
        }

        void BuildMoveBoundary()
        {
            if (_layerRoot == null) return;
            _moveBoundaryTopPct = Proto_GreyboxMoveZone.FootYPercentMinCurrent;

            if (_moveBoundaryRoot != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(_moveBoundaryRoot.gameObject);
                else
#endif
                    Object.Destroy(_moveBoundaryRoot.gameObject);
            }

            _moveBoundaryTopSegs.Clear();
            _moveBoundaryLeft = null;
            _moveBoundaryRight = null;

            var root = new GameObject("WP_MoveBoundary");
            root.transform.SetParent(_layerRoot, false);
            _moveBoundaryRoot = root.transform;

            float yTop = Proto_GreyboxMoveZone.MaxFootWorldY;
            float yBot = Proto_GreyboxMoveZone.MinFootWorldY;
            float xL = Proto_GreyboxMoveZone.MinFootWorldX;
            float xR = Proto_GreyboxMoveZone.MaxFootWorldX;
            int sortBase = Proto_GreyboxLayout.WorldSpriteSortingOrder(yTop) - 1;
            var wood = new Color(0.62f, 0.48f, 0.32f, 0.92f);
            var rope = new Color(0.78f, 0.66f, 0.42f, 0.85f);

            const float dashLen = 0.28f;
            const float gapLen = 0.14f;
            float x = xL;
            int seg = 0;
            while (x < xR - dashLen * 0.25f)
            {
                float cx = x + dashLen * 0.5f;
                if (cx > xR) break;
                var go = new GameObject("top_dash_" + seg++);
                go.transform.SetParent(_moveBoundaryRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
                sr.color = seg % 3 == 0 ? wood : rope;
                go.transform.position = new Vector3(Mathf.Min(cx, xR - dashLen * 0.5f), yTop, 0f);
                go.transform.localScale = new Vector3(dashLen, 0.09f, 1f);
                sr.sortingOrder = sortBase;
                _moveBoundaryTopSegs.Add(sr);
                x += dashLen + gapLen;
            }

            for (int stake = 0; stake <= 6; stake++)
            {
                float t = stake / 6f;
                float sx = Mathf.Lerp(xL, xR, t);
                var go = new GameObject("top_stake_" + stake);
                go.transform.SetParent(_moveBoundaryRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
                sr.color = wood;
                go.transform.position = new Vector3(sx, yTop + 0.06f, 0f);
                go.transform.localScale = new Vector3(0.06f, 0.22f, 1f);
                sr.sortingOrder = sortBase + 1;
                _moveBoundaryTopSegs.Add(sr);
            }

            _moveBoundaryLeft = CreateMoveSideEdge("edge_left", xL, yBot, yTop, sortBase);
            _moveBoundaryRight = CreateMoveSideEdge("edge_right", xR, yBot, yTop, sortBase);
        }

        SpriteRenderer CreateMoveSideEdge(string name, float x, float yBot, float yTop, int sortBase)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_moveBoundaryRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            sr.color = new Color(0.58f, 0.44f, 0.30f, 0.55f);
            float h = Mathf.Max(yTop - yBot, 0.05f);
            go.transform.position = new Vector3(x, yBot + h * 0.5f, 0f);
            go.transform.localScale = new Vector3(0.04f, h, 1f);
            sr.sortingOrder = sortBase;
            return sr;
        }

        void UpdateMoveBoundaryHighlight(Vector2 playerFoot)
        {
            if (_moveBoundaryRoot == null) return;
            float topDist = Proto_GreyboxMoveZone.MaxFootWorldY - playerFoot.y;
            float leftDist = playerFoot.x - Proto_GreyboxMoveZone.MinFootWorldX;
            float rightDist = Proto_GreyboxMoveZone.MaxFootWorldX - playerFoot.x;
            bool nearTop = topDist >= 0f && topDist < MoveBoundaryHighlightDist;
            bool nearLeft = leftDist >= 0f && leftDist < MoveBoundaryHighlightDist;
            bool nearRight = rightDist >= 0f && rightDist < MoveBoundaryHighlightDist;

            var wood = new Color(0.62f, 0.48f, 0.32f, 0.92f);
            var rope = new Color(0.78f, 0.66f, 0.42f, 0.85f);
            var woodHi = new Color(0.92f, 0.78f, 0.42f, 0.98f);
            var ropeHi = new Color(1f, 0.92f, 0.55f, 0.98f);

            for (int i = 0; i < _moveBoundaryTopSegs.Count; i++)
            {
                var sr = _moveBoundaryTopSegs[i];
                if (sr == null) continue;
                bool stake = sr.gameObject.name.StartsWith("top_stake");
                if (nearTop)
                    sr.color = stake ? woodHi : (i % 3 == 0 ? woodHi : ropeHi);
                else
                    sr.color = stake ? wood : (i % 3 == 0 ? wood : rope);
            }

            ApplySideEdgeHighlight(_moveBoundaryLeft, nearLeft);
            ApplySideEdgeHighlight(_moveBoundaryRight, nearRight);
        }

        static void ApplySideEdgeHighlight(SpriteRenderer sr, bool near)
        {
            if (sr == null) return;
            sr.color = near
                ? new Color(0.95f, 0.82f, 0.48f, 0.92f)
                : new Color(0.58f, 0.44f, 0.30f, 0.55f);
        }

        public void SpawnHitSpark(Vector2 groundPos)
        {
            if (!IsActive) return;
            var sparks = _fxV2?.hitSparks != null && _fxV2.hitSparks.Length > 0 ? _fxV2.hitSparks : _catalog.hitSpark;
            if (sparks == null || sparks.Length == 0) return;
            var sr = RentSpark();
            sr.sprite = sparks[0];
            sr.transform.position = new Vector3(groundPos.x, groundPos.y, 0f);
            float diam = Proto_GreyboxLayout.ScreenPercentToWorldHeight(2.2f);
            sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(sparks[0], diam);
            sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(groundPos.y) + 3;
            float dur = sparks == _fxV2?.hitSparks ? sparks.Length / 24f : 0.12f;
            _sparks.Add(new SparkFx { Sr = sr, T = dur, Frame = 0 });
        }

        SpriteRenderer RentSpark()
        {
            foreach (var s in _sparkPool)
            {
                if (s != null && !s.gameObject.activeSelf)
                    return s;
            }

            var go = new GameObject("Greybox_WestPixel_Spark");
            go.transform.SetParent(_layerRoot != null ? _layerRoot : _host.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            _sparkPool.Add(sr);
            return sr;
        }

        void TickSparks(float dt)
        {
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                var fx = _sparks[i];
                fx.T -= dt;
                var sparks = _fxV2?.hitSparks != null && _fxV2.hitSparks.Length > 0 ? _fxV2.hitSparks : _catalog.hitSpark;
                if (sparks != null && sparks.Length > 1 && fx.Sr != null && fx.Seq == null)
                {
                    int idx = Mathf.Clamp(Mathf.FloorToInt((sparks.Length / 24f - fx.T) * 24f), 0, sparks.Length - 1);
                    fx.Sr.sprite = sparks[idx];
                }
                else if (fx.Seq != null && fx.Seq.Length > 1 && fx.Sr != null && fx.SeqFps > 0f)
                {
                    float total = fx.Seq.Length / fx.SeqFps;
                    int idx = Mathf.Clamp(Mathf.FloorToInt((total - fx.T) * fx.SeqFps), 0, fx.Seq.Length - 1);
                    fx.Sr.sprite = fx.Seq[idx];
                }

                if (fx.T <= 0f)
                {
                    if (fx.Sr != null)
                        fx.Sr.gameObject.SetActive(false);
                    _sparks.RemoveAt(i);
                }
            }
        }

        public bool TryPlayGruntDeath(Proto_GreyboxGrunt grunt, Proto_GreyboxTargetView view, ref int score)
        {
            if (!IsActive || view == null) return false;
            if (!_gruntVisuals.TryGetValue(grunt, out var st))
            {
                st = new GruntVisualState();
                _gruntVisuals[grunt] = st;
            }

            if (st.Finished)
            {
                if (view.gameObject.activeSelf)
                    view.gameObject.SetActive(false);
                return true;
            }

            if (!st.Dying)
            {
                st.Dying = true;
                st.DeathElapsed = 0f;
                view.gameObject.SetActive(true);
                if (!_scoredDeaths.Contains(grunt))
                {
                    score += Proto_GreyboxTuning.ScorePerKill;
                    _scoredDeaths.Add(grunt);
                }
            }

            st.DeathElapsed += Mathf.Max(_simDt, 0.0001f);
            int frame = Proto_GreyboxGruntDeathVisual.FrameIndex(st.DeathElapsed);
            float alpha = Proto_GreyboxGruntDeathVisual.AlphaAt(st.DeathElapsed, st.Finished);
            var sr = view.GetComponent<SpriteRenderer>();
            if (sr != null && _catalog.gruntDown != null && _catalog.gruntDown.Length > 0)
            {
                sr.enabled = true;
                sr.sprite = _catalog.gruntDown[Mathf.Clamp(frame, 0, _catalog.gruntDown.Length - 1)];
                sr.color = new Color(1f, 1f, 1f, alpha);
                if (view.Logic != null)
                    sr.sortingOrder = Proto_GreyboxLayout.WorldSpriteSortingOrder(view.Logic.FootPosition.y);
            }

            if (Proto_GreyboxGruntDeathVisual.IsFinished(st.DeathElapsed, st.Finished))
            {
                st.Finished = true;
                view.gameObject.SetActive(false);
            }
            else
            {
                view.gameObject.SetActive(Proto_GreyboxGruntDeathVisual.ViewShouldBeActive(st.DeathElapsed, st.Finished));
            }

            return true;
        }

        public void SyncGruntLiving(Proto_GreyboxGrunt grunt, Proto_GreyboxTargetView view, bool hidden)
        {
            if (!IsActive || view == null) return;
            if (_gruntVisuals.TryGetValue(grunt, out var st) && (st.Dying || st.Finished)) return;
            var sr = view.GetComponent<SpriteRenderer>();
            if (sr == null || _catalog.gruntIdle == null || _catalog.gruntIdle.Length == 0) return;
            sr.enabled = true;

            if (!_gruntVisuals.TryGetValue(grunt, out st))
            {
                st = new GruntVisualState();
                _gruntVisuals[grunt] = st;
            }

            st.IdleT += Mathf.Max(_simDt, 0.0001f);
            if (grunt.FiredThisTick)
            {
                st.ShootT = 0.18f;
                if (_catalog.gruntShoot != null && _catalog.gruntShoot.Length > 0)
                    sr.sprite = _catalog.gruntShoot[0];
                TriggerGruntMuzzle(grunt.Target.FootPosition, grunt.LastFireVelocity);
            }

            st.ShootT = Mathf.Max(0f, st.ShootT - Mathf.Max(_simDt, 0.0001f));
            if (st.ShootT > 0f && _catalog.gruntShoot != null && _catalog.gruntShoot.Length > 0)
                sr.sprite = _catalog.gruntShoot[st.ShootT > 0.09f ? 0 : 1];
            else if (hidden)
            {
                sr.color = new Color(1f, 1f, 1f, 0.25f);
                sr.sprite = _catalog.gruntIdle[0];
            }
            else
            {
                sr.color = Color.white;
                if (st.IdleT > 0.5f) { st.IdleT = 0f; st.IdleFrame ^= 1; }
                sr.sprite = _catalog.gruntIdle[st.IdleFrame % _catalog.gruntIdle.Length];
            }

            float targetBodyH = Proto_GreyboxPerspectiveDepth.Enabled && grunt.Target != null
                ? Proto_GreyboxPerspectiveDepth.EnemyBodyHeightWorld(grunt.Target.FootPosition.y)
                : Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.TargetHeightPercent);
            float sy = targetBodyH / (GruntBodyPx / Proto_GreyboxWestPixelCatalog.ImportPpu);
            view.transform.localScale = new Vector3(sy, sy, 1f);
            if (grunt.Target != null)
                sr.sortingOrder = Proto_GreyboxLayout.WorldSpriteSortingOrder(grunt.Target.FootPosition.y);
        }

        public void OnGruntHitFlash(Proto_GreyboxTargetView view)
        {
            if (!IsActive || view == null) return;
            var sr = view.GetComponent<SpriteRenderer>();
            if (sr == null || _catalog.gruntHit == null || _catalog.gruntHit.Length == 0) return;
            sr.sprite = _catalog.gruntHit[0];
        }

        public bool SyncAllyBullets(IReadOnlyList<Proto_GreyboxAllyBulletSim> bullets, float diameter, List<SpriteRenderer> cores,
            List<SpriteRenderer> tracers, List<SpriteRenderer> hotspots, Transform parent)
        {
            var allyCore = _fxV2?.bulletAlly ?? _catalog.bulletAlly;
            var allyTracer = _fxV2?.bulletAllyTracer;
            if (!IsActive || allyCore == null) return false;
            EnsureWestAllyBulletViews(bullets.Count, parent, cores, tracers, hotspots);
            for (int i = 0; i < cores.Count; i++)
            {
                var sr = cores[i];
                var tr = i < tracers.Count ? tracers[i] : null;
                var hot = i < hotspots.Count ? hotspots[i] : null;
                if (i < bullets.Count && bullets[i].Alive)
                {
                    var b = bullets[i];
                    var vel = b.GroundVelocity;
                    var dir = vel.sqrMagnitude > 1e-6f ? vel.normalized : Vector2.right;
                    sr.gameObject.SetActive(true);
                    var pos = b.VisualPositionAtGround();
                    float scale = b.VisualScale;
                    float diam = Mathf.Max(Proto_GreyboxTuning.AllyBulletVisualDiameter * scale,
                        Proto_GreyboxLayout.MinAllyBulletWorldDiameter);
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    int order = Proto_GreyboxLayout.CombatDrawSortingOrder(b.GroundPosition.y);
                    if (allyTracer != null)
                    {
                        sr.sprite = allyTracer;
                        sr.color = Color.white;
                        sr.transform.position = new Vector3(pos.x, pos.y, 0f);
                        float trH = allyTracer.rect.height / Proto_GreyboxWestPixelCatalog.ImportPpu;
                        float trScale = Mathf.Max(diam * 0.55f, trH) / trH;
                        sr.transform.localScale = new Vector3(trScale, trScale, 1f);
                        sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                        sr.sortingOrder = order;
                        if (hot != null)
                            hot.gameObject.SetActive(false);
                        if (tr != null)
                        {
                            tr.gameObject.SetActive(allyCore != null);
                            if (allyCore != null)
                            {
                                tr.sprite = allyCore;
                                tr.color = Color.white;
                                var headPos = pos + dir * (diam * 0.22f);
                                tr.transform.position = new Vector3(headPos.x, headPos.y, 0f);
                                tr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(allyCore, diam * 0.35f, diam * 0.28f);
                                tr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                                tr.sortingOrder = order + 1;
                            }
                        }
                    }
                    else
                    {
                        sr.sprite = allyCore;
                        sr.color = new Color(1f, 0.98f, 0.55f, 1f);
                        pos += dir * diam * 0.18f;
                        sr.transform.position = new Vector3(pos.x, pos.y, 0f);
                        sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(allyCore, diam, diam * 0.65f);
                        sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                        sr.sortingOrder = order;
                        if (hot != null)
                        {
                            hot.gameObject.SetActive(true);
                            hot.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                            hot.color = Color.white;
                            float hotD = diam * 0.32f;
                            hot.transform.localScale = new Vector3(hotD, hotD, 1f);
                            hot.sortingOrder = order + 1;
                        }

                        if (tr != null)
                        {
                            tr.gameObject.SetActive(true);
                            tr.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
                            tr.color = new Color(1f, 0.96f, 0.42f, 0.9f);
                            float tailLen = Proto_GreyboxTuning.AllyBulletVisualTracerLength;
                            var tailCenter = pos - dir * tailLen * 0.5f;
                            tr.transform.position = new Vector3(tailCenter.x, tailCenter.y + 0.02f * scale, 0f);
                            tr.transform.localScale = new Vector3(tailLen, diam * 0.3f, 1f);
                            tr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                            tr.sortingOrder = order - 1;
                        }
                    }
                }
                else
                {
                    sr.gameObject.SetActive(false);
                    if (tr != null) tr.gameObject.SetActive(false);
                    if (hot != null) hot.gameObject.SetActive(false);
                }
            }

            return true;
        }

        static void EnsureWestAllyBulletViews(int bulletCount, Transform parent, List<SpriteRenderer> cores,
            List<SpriteRenderer> tracers, List<SpriteRenderer> hotspots)
        {
            while (cores.Count < bulletCount)
            {
                var go = new GameObject("Greybox_AllyBullet");
                go.transform.SetParent(parent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                cores.Add(sr);

                var hotGo = new GameObject("Greybox_AllyBullet_Hotspot");
                hotGo.transform.SetParent(go.transform, false);
                var hotSr = hotGo.AddComponent<SpriteRenderer>();
                hotspots.Add(hotSr);

                var tail = new GameObject("Greybox_AllyTracer");
                tail.transform.SetParent(parent, false);
                var tsr = tail.AddComponent<SpriteRenderer>();
                tracers.Add(tsr);
            }
        }

        static void EnsureWestEnemyBulletViews(int bulletCount, Transform parent, List<SpriteRenderer> cores,
            List<SpriteRenderer> glows)
        {
            while (cores.Count < bulletCount)
            {
                var glow = new GameObject("Greybox_EnemyBulletGlow");
                glow.transform.SetParent(parent, false);
                var gsr = glow.AddComponent<SpriteRenderer>();
                glows.Add(gsr);

                var go = new GameObject("Greybox_EnemyBullet");
                go.transform.SetParent(parent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                cores.Add(sr);
            }
        }

        public bool SyncEnemyBullets(IReadOnlyList<Proto_GreyboxBulletSim> bullets, float diameter,
            List<SpriteRenderer> cores, List<SpriteRenderer> glows, Transform parent, float simTime)
        {
            var frames = _fxV2?.PreferEnemyBulletFrames();
            var enemySprite = frames != null && frames.Length > 0
                ? frames[Mathf.FloorToInt(simTime * 12f) % frames.Length]
                : _fxV2?.bulletEnemy ?? _catalog.bulletEnemy;
            if (!IsActive || enemySprite == null)
            {
                if (!IsActive) return false;
                enemySprite = _fxV2?.bulletAlly ?? _catalog.bulletAlly;
                if (enemySprite == null) return false;
            }
            EnsureWestEnemyBulletViews(bullets.Count, parent, cores, glows);
            for (int i = 0; i < cores.Count; i++)
            {
                var sr = cores[i];
                var glow = i < glows.Count ? glows[i] : null;
                if (i < bullets.Count && bullets[i].Alive)
                {
                    var b = bullets[i];
                    var vel = b.GroundVelocity;
                    var dir = vel.sqrMagnitude > 1e-6f ? vel.normalized : Vector2.right;
                    sr.gameObject.SetActive(true);
                    var useFlip = frames != null && frames.Length > 0;
                    sr.sprite = useFlip
                        ? frames[Mathf.FloorToInt(simTime * 12f) % frames.Length]
                        : enemySprite;
                    sr.color = useFlip ? Color.white : new Color(1f, 0.45f, 0.2f, 1f);
                    var pos = b.GroundPosition;
                    sr.transform.position = new Vector3(pos.x, pos.y, 0f);
                    float diam = b.VisualDiameter(diameter) * Proto_GreyboxTuning.EnemyBulletVisualDiameterMul;
                    sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(enemySprite, diam, diam * 0.75f);
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(b.GroundPosition.y);
                    if (glow != null)
                    {
                        if (useFlip)
                            glow.gameObject.SetActive(false);
                        else
                        {
                            glow.gameObject.SetActive(true);
                            glow.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                            glow.color = new Color(1f, 0.42f, 0.12f, 0.7f);
                            float halo = diam * Proto_GreyboxTuning.EnemyBulletVisualGlowMul;
                            glow.transform.position = sr.transform.position;
                            glow.transform.localScale = new Vector3(halo, halo, 1f);
                            glow.sortingOrder = sr.sortingOrder - 1;
                        }
                    }
                }
                else
                {
                    sr.gameObject.SetActive(false);
                    if (glow != null)
                        glow.gameObject.SetActive(false);
                }
            }

            return true;
        }

        public void EnsureGruntSprite(Proto_GreyboxTargetView view)
        {
            if (!IsActive || view == null) return;
            var sr = view.GetComponent<SpriteRenderer>();
            if (sr == null || _catalog.gruntIdle == null || _catalog.gruntIdle.Length == 0) return;
            sr.enabled = true;
            sr.sprite = _catalog.gruntIdle[0];
            sr.color = Color.white;
            if (view.Logic != null)
                sr.sortingOrder = Proto_GreyboxLayout.WorldSpriteSortingOrder(view.Logic.FootPosition.y);
        }

        Sprite PickDizzyBodySprite()
        {
            if (_fxV2 == null) return null;
            if (_dizzyPhase == Proto_GreyboxRollDizzyPhase.Light)
            {
                if (_dizzyElapsed < 2f / 12f)
                    return LastRollFrameOrIdle();
                int idx = Mathf.FloorToInt((_dizzyElapsed - 2f / 12f) * 8f) % Mathf.Max(1, _fxV2.dizzyLightBody?.Length ?? 1);
                return _fxV2.dizzyLightBody != null && _fxV2.dizzyLightBody.Length > 0
                    ? _fxV2.dizzyLightBody[idx % _fxV2.dizzyLightBody.Length]
                    : null;
            }

            if (_dizzyPhase != Proto_GreyboxRollDizzyPhase.Stun) return null;
            float t = _dizzyElapsed;
            if (t < Proto_GreyboxRollDizzy.SpinInSec)
            {
                int idx = Mathf.FloorToInt(t * 12f);
                return _fxV2.dizzySpinInBody != null && _fxV2.dizzySpinInBody.Length > 0
                    ? _fxV2.dizzySpinInBody[Mathf.Clamp(idx, 0, _fxV2.dizzySpinInBody.Length - 1)]
                    : null;
            }

            t -= Proto_GreyboxRollDizzy.SpinInSec;
            if (t < Proto_GreyboxRollDizzy.SpinBodySec)
            {
                int idx = Mathf.FloorToInt(t * 12f);
                return _fxV2.dizzySpinBody != null && _fxV2.dizzySpinBody.Length > 0
                    ? _fxV2.dizzySpinBody[Mathf.Clamp(idx, 0, _fxV2.dizzySpinBody.Length - 1)]
                    : null;
            }

            t -= Proto_GreyboxRollDizzy.SpinBodySec;
            int outIdx = Mathf.FloorToInt(t * 12f);
            return _fxV2.dizzySpinOutBody != null && _fxV2.dizzySpinOutBody.Length > 0
                ? _fxV2.dizzySpinOutBody[Mathf.Clamp(outIdx, 0, _fxV2.dizzySpinOutBody.Length - 1)]
                : null;
        }

        Sprite LastRollFrameOrIdle()
        {
            var rollFrames = _fxV2?.RollFrames(_rollFacing);
            if (rollFrames != null && rollFrames.Length > 8)
                return rollFrames[8];
            return _catalog.playerIdle != null && _catalog.playerIdle.Length > 0 ? _catalog.playerIdle[0] : null;
        }

        Sprite PickDizzyFxSprite()
        {
            if (_fxV2 == null) return null;
            if (_dizzyPhase == Proto_GreyboxRollDizzyPhase.Light)
            {
                if (_dizzyElapsed < 2f / 12f)
                {
                    int idx = Mathf.FloorToInt(_dizzyElapsed * 12f);
                    return _fxV2.dizzyLightInFx != null && _fxV2.dizzyLightInFx.Length > 0
                        ? _fxV2.dizzyLightInFx[Mathf.Clamp(idx, 0, _fxV2.dizzyLightInFx.Length - 1)]
                        : null;
                }

                int bodyIdx = Mathf.FloorToInt((_dizzyElapsed - 2f / 12f) * 8f);
                return _fxV2.dizzyLightFx != null && _fxV2.dizzyLightFx.Length > 0
                    ? _fxV2.dizzyLightFx[bodyIdx % _fxV2.dizzyLightFx.Length]
                    : null;
            }

            if (_dizzyPhase != Proto_GreyboxRollDizzyPhase.Stun) return null;
            float t = _dizzyElapsed;
            if (t < Proto_GreyboxRollDizzy.SpinInSec)
            {
                int idx = Mathf.FloorToInt(t * 12f);
                return _fxV2.dizzySpinInFx != null && _fxV2.dizzySpinInFx.Length > 0
                    ? _fxV2.dizzySpinInFx[Mathf.Clamp(idx, 0, _fxV2.dizzySpinInFx.Length - 1)]
                    : null;
            }

            t -= Proto_GreyboxRollDizzy.SpinInSec;
            if (t < Proto_GreyboxRollDizzy.SpinBodySec)
            {
                int idx = Mathf.FloorToInt(t * 12f);
                return _fxV2.dizzySpinFx != null && _fxV2.dizzySpinFx.Length > 0
                    ? _fxV2.dizzySpinFx[Mathf.Clamp(idx, 0, _fxV2.dizzySpinFx.Length - 1)]
                    : null;
            }

            t -= Proto_GreyboxRollDizzy.SpinBodySec;
            int outIdx = Mathf.FloorToInt(t * 12f);
            return _fxV2.dizzySpinOutFx != null && _fxV2.dizzySpinOutFx.Length > 0
                ? _fxV2.dizzySpinOutFx[Mathf.Clamp(outIdx, 0, _fxV2.dizzySpinOutFx.Length - 1)]
                : null;
        }

        void EnsureDizzyOverlay()
        {
            if (_dizzyFxOverlay != null) return;
            var go = new GameObject("Greybox_WestPixel_DizzyFx");
            go.transform.SetParent(_host.transform, false);
            _dizzyFxOverlay = go.AddComponent<SpriteRenderer>();
            _dizzyFxOverlay.color = Color.white;
        }

        void EnsureReloadCasingSr()
        {
            if (_reloadCasingSr != null) return;
            var go = new GameObject("Greybox_WestPixel_ReloadCasings");
            go.transform.SetParent(_layerRoot != null ? _layerRoot : _host.transform, false);
            _reloadCasingSr = go.AddComponent<SpriteRenderer>();
            _reloadCasingSr.sortingOrder = Proto_GreyboxLayout.SortingCombatOverlay + 20;
        }

        void TickReloadCasingAnim(float dt)
        {
            if (_reloadCasingSr == null || _reloadCasingClip == null || !_reloadCasingSr.gameObject.activeSelf) return;
            _reloadCasingT += dt;
            int idx = Mathf.FloorToInt(_reloadCasingT * 20f);
            if (idx >= _reloadCasingClip.Length)
            {
                _reloadCasingSr.gameObject.SetActive(false);
                return;
            }

            _reloadCasingSr.sprite = _reloadCasingClip[idx];
        }
    }
}
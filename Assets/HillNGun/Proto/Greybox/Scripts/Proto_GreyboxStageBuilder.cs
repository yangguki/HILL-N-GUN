using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    [ExecuteAlways]
    public class Proto_GreyboxStageBuilder : MonoBehaviour
    {
        public const string RootName = "Greybox_StageRoot";
        public const string PlayerName = "Greybox_Player";
        public const string SkyName = "Greybox_Sky";
        public const string GroundName = "Greybox_Ground";
        public const string FenceName = "Greybox_Fence";
        public const string EnemyMarkerPrefix = "Greybox_EnemyMarker_";
        public const string CrateFarName = "Greybox_Crate_Far";
        public const string CrateMidName = "Greybox_Crate_Mid";
        public const string CrateNearName = "Greybox_Crate_Near";
        public const string ScenePath = "Assets/HillNGun/Proto/Greybox/Scenes/Proto_GreyboxStage.unity";

        Transform _root;

        void Awake() => Build();

        public void Build()
        {
            ClearRoot();
            _root = new GameObject(RootName).transform;
            _root.SetParent(transform, false);

            BuildBackground();
            BuildFence();
            BuildCrates();
            BuildPlayer();
            BuildCrosshair();
            BuildHud();
        }

        /// <summary>Rebuild sky/buildings/ground/fence/crates only. Keeps player, HUD, and west-skin layer.</summary>
        public void RebuildEnvironment()
        {
            if (_root == null)
            {
                var existing = GameObject.Find(RootName);
                _root = existing != null ? existing.transform : null;
            }

            if (_root == null)
            {
                Build();
                return;
            }

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (!IsEnvironmentChild(child.name))
                    continue;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
#endif
                    Destroy(child.gameObject);
            }

            BuildBackground();
            BuildFence();
            BuildCrates();
        }

        static bool IsEnvironmentChild(string name)
        {
            if (name == SkyName || name == GroundName || name == FenceName)
                return true;
            if (name == CrateFarName || name == CrateMidName || name == CrateNearName)
                return true;
            if (name.StartsWith("Greybox_Building"))
                return true;
            return false;
        }

        void ClearRoot()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
#endif
                    Destroy(child.gameObject);
            }
        }

        void BuildBackground()
        {
            float skyH = Proto_GreyboxLayout.ScreenPercentToWorldHeight(16f);
            float skyFoot = Proto_GreyboxLayout.ScreenYPercentToWorldY(16f);
            var skyGo = Proto_GreyboxSpriteUtil.CreateQuad(SkyName, _root, Color.white,
                0f, skyFoot, Proto_GreyboxLayout.WorldWidth, skyH, Proto_GreyboxLayout.SortingSky);
            var skySr = skyGo.GetComponent<SpriteRenderer>();
            var skySprite = Proto_GreyboxSpriteUtil.GetVerticalGradientSprite(
                Proto_GreyboxPalette.SkyTop, Proto_GreyboxPalette.SkyBottom);
            skySr.sprite = skySprite;
            skyGo.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(skySprite, Proto_GreyboxLayout.WorldWidth, skyH);

            float bldgTop = 16f;
            float bldgBottom = 32f;
            float bldgH = Proto_GreyboxLayout.ScreenPercentToWorldHeight(bldgBottom - bldgTop);
            float bldgFoot = Proto_GreyboxLayout.ScreenYPercentToWorldY(bldgBottom);
            int blocks = 5;
            float blockW = Proto_GreyboxLayout.WorldWidth / blocks;
            for (int i = 0; i < blocks; i++)
            {
                float cx = -Proto_GreyboxLayout.WorldWidth * 0.5f + blockW * (i + 0.5f);
                var faceW = blockW * 0.92f;
                Proto_GreyboxSpriteUtil.CreateQuad($"Greybox_Building_{i}", _root, Proto_GreyboxPalette.Building, cx,
                    bldgFoot, faceW, bldgH, Proto_GreyboxLayout.SortingBuildingBase + i);
                float awningH = bldgH * 0.12f;
                Proto_GreyboxSpriteUtil.CreateQuad($"Greybox_BuildingAwning_{i}", _root, Proto_GreyboxPalette.BuildingTrim,
                    cx, bldgFoot + bldgH - awningH, faceW, awningH, Proto_GreyboxLayout.SortingBuildingBase + i + 1);
                float doorW = faceW * 0.28f;
                float doorH = bldgH * 0.42f;
                Proto_GreyboxSpriteUtil.CreateQuad($"Greybox_BuildingDoor_{i}", _root, Proto_GreyboxPalette.BuildingDoor,
                    cx, bldgFoot, doorW, doorH, Proto_GreyboxLayout.SortingBuildingBase + i + 2);
                float winW = faceW * 0.24f;
                float winH = bldgH * 0.2f;
                float winFoot = bldgFoot + bldgH * 0.52f;
                Proto_GreyboxSpriteUtil.CreateQuad($"Greybox_BuildingWinL_{i}", _root, Proto_GreyboxPalette.BuildingWindow,
                    cx - faceW * 0.26f, winFoot, winW, winH, Proto_GreyboxLayout.SortingBuildingBase + i + 3);
                Proto_GreyboxSpriteUtil.CreateQuad($"Greybox_BuildingWinR_{i}", _root, Proto_GreyboxPalette.BuildingWindow,
                    cx + faceW * 0.26f, winFoot, winW, winH, Proto_GreyboxLayout.SortingBuildingBase + i + 3);
            }

            float groundTop = 32f;
            float groundH = Proto_GreyboxLayout.ScreenPercentToWorldHeight(100f - groundTop);
            var groundGo = Proto_GreyboxSpriteUtil.CreateQuad(GroundName, _root, Color.white,
                0f, 0f, Proto_GreyboxLayout.WorldWidth, groundH, Proto_GreyboxLayout.SortingGround);
            var groundSr = groundGo.GetComponent<SpriteRenderer>();
            var groundSprite = Proto_GreyboxSpriteUtil.GetGroundFieldSprite();
            groundSr.sprite = groundSprite;
            groundGo.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(groundSprite, Proto_GreyboxLayout.WorldWidth, groundH);
        }

        void BuildFence()
        {
            float foot = Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.FenceBottomPercent);
            float top = Proto_GreyboxLayout.ScreenYPercentToWorldY(Proto_GreyboxPerspectiveDepth.FenceTopPercent);
            float h = top - foot;
            var fenceGo = Proto_GreyboxSpriteUtil.CreateQuad(FenceName, _root, Color.white,
                0f, foot, Proto_GreyboxLayout.WorldWidth, h, Proto_GreyboxLayout.SortingFence);
            var fenceSr = fenceGo.GetComponent<SpriteRenderer>();
            var fenceSprite = Proto_GreyboxSpriteUtil.GetFenceSprite();
            fenceSr.sprite = fenceSprite;
            fenceGo.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(fenceSprite, Proto_GreyboxLayout.WorldWidth, h);
        }

        void BuildScaledBox(string name, float x0, float x1, float yTop, float yBottom, Color color,
            int? sortingOrderOverride = null)
        {
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(yBottom);
            float scale = Proto_DepthScale.ScaleAt(footY);
            float targetH = Proto_GreyboxLayout.ScreenPercentToWorldHeight(yBottom - yTop);
            float targetW = Proto_GreyboxLayout.WorldWidth * (x1 - x0) / 100f;
            float baseH = targetH / scale;
            float baseW = targetW / scale;
            float cx = (Proto_GreyboxLayout.ScreenXPercentToWorldX(x0) + Proto_GreyboxLayout.ScreenXPercentToWorldX(x1)) * 0.5f;
            int order = sortingOrderOverride ?? Proto_GreyboxLayout.WorldSpriteSortingOrder(footY);
            var go = Proto_GreyboxSpriteUtil.CreateQuad(name, _root, color, cx, footY, baseW, baseH, order);
            float worldW = baseW * scale;
            float worldH = baseH * scale;
            var crateSprite = Proto_GreyboxSpriteUtil.GetCrateSprite();
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = crateSprite;
            sr.color = Color.white;
            go.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(crateSprite, worldW, worldH);
        }

        void BuildCrates()
        {
            // Heights scaled with the hero (legacy 6/11/20% * 12/20 → 3.6/6.6/12), feet unchanged.
            BuildScaledBox(CrateFarName, 52f, 56.5f, 32.4f, 36f, Proto_GreyboxPalette.Crate);
            BuildScaledBox(CrateMidName, 12f, 21f, 56.4f, 63f, Proto_GreyboxPalette.Crate);
            BuildScaledBox(CrateNearName, 58f, 69f, 70f, 82f, Proto_GreyboxPalette.Crate);
        }

        void BuildEnemyMarkers()
        {
            const int count = 8;
            float headY = 26f;
            float footY = 37f;
            float hPercent = footY - headY;
            float wPercent = 3.5f;
            float worldFoot = Proto_GreyboxLayout.ScreenYPercentToWorldY(footY);
            float scale = Proto_DepthScale.ScaleAt(worldFoot);
            float targetH = Proto_GreyboxLayout.ScreenPercentToWorldHeight(hPercent);
            float targetW = Proto_GreyboxLayout.WorldWidth * (wPercent / 100f);
            float baseH = targetH / scale;
            float baseW = targetW / scale;
            int order = Proto_GreyboxLayout.SortingEnemyMarker;
            var color = new Color(1f, 0.15f, 0.15f, 0.45f);

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float xPct = Mathf.Lerp(21f, 79f, t);
                float cx = Proto_GreyboxLayout.ScreenXPercentToWorldX(xPct);
                var go = Proto_GreyboxSpriteUtil.CreateQuad(EnemyMarkerPrefix + i, _root, color, cx, worldFoot,
                    baseW, baseH, order + i);
                go.transform.localScale = new Vector3(baseW * scale, baseH * scale, 1f);
                var logic = new Proto_GreyboxTarget(new Vector2(cx, worldFoot));
                go.AddComponent<Proto_GreyboxTargetView>().Init(logic);
            }
        }

        void BuildPlayer()
        {
            float foot = 88f;
            float hPct = Proto_GreyboxTuning.PlayerCapsuleHeightPercent;
            float head = foot - hPct;
            float wPct = Proto_GreyboxTuning.PlayerCapsuleWidthPercent;
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(foot);
            float scale = Proto_DepthScale.ScaleAt(footY);
            float targetH = Proto_GreyboxLayout.ScreenPercentToWorldHeight(hPct);
            float targetW = Proto_GreyboxLayout.WorldWidth * (wPct / 100f);
            float baseH = targetH / scale;
            float baseW = targetW / scale;
            float cx = Proto_GreyboxLayout.ScreenXPercentToWorldX(49f);
            int order = Proto_GreyboxLayout.WorldSpriteSortingOrder(footY);
            var go = Proto_GreyboxSpriteUtil.CreateQuad(PlayerName, _root, Color.white, cx, footY, baseW, baseH, order);
            var playerSprite = Proto_GreyboxSpriteUtil.GetPlayerBackSprite();
            var playerSr = go.GetComponent<SpriteRenderer>();
            playerSr.sprite = playerSprite;
            float worldH = targetH;
            go.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(playerSprite, worldH);
            Proto_GreyboxSpriteUtil.AttachFootShadow(go.transform, order, 0.95f, 0.18f);
            go.AddComponent<Proto_GreyboxPlayerController>();
        }

        void BuildCrosshair()
        {
            var aim = Proto_GreyboxAim.DefaultAimGround;
            float cx = aim.x;
            float cy = aim.y;
            float w = Proto_GreyboxLayout.WorldWidth * 0.03f;
            float h = Proto_GreyboxLayout.ScreenPercentToWorldHeight(5f);
            Proto_GreyboxSpriteUtil.CreateQuad("Greybox_Crosshair", _root, new Color(0.82f, 0.72f, 0.38f, 0.85f),
                cx, cy - h * 0.5f, w, h, 100);
        }

        void BuildHud()
        {
            var cam = Camera.main;
            if (cam == null)
                cam = FindFirstObjectByType<Camera>();
            if (cam != null)
                Proto_GreyboxHudBuilder.Build(cam, transform);

            Proto_GreyboxViewPresenterRegistry.Ensure();
        }
    }
}
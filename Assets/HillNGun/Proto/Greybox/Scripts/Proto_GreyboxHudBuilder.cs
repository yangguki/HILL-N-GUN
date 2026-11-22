using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxHudBuilder
    {
        public const int RefWidth = 1280;
        public const int RefHeight = 720;
        public const float JoystickCenterXPercent = 11.5f;
        public const float JoystickCenterYPercentFromTop = 78f;
        public const string JoystickRingName = "Greybox_Hud_JoystickRing";
        public const string JoystickKnobName = "Greybox_Hud_JoystickKnob";
        public const string RollButtonName = "Greybox_Hud_Roll";
        public const string FireButtonName = "Greybox_Hud_Fire";
        public const string ReloadButtonName = "Greybox_Hud_Reload";
        public const string ReloadFillName = "Greybox_Hud_ReloadFill";
        public const string ReloadLabelName = "Greybox_Hud_ReloadLabel";
        public const string GrenadeButtonName = "Greybox_Hud_Grenade";

        public const float JoystickHitX0 = 4f;
        public const float JoystickHitX1 = 19f;
        public const float JoystickHitY0 = 66f;
        public const float JoystickHitY1 = 91f;
        public const float FireCenterX = 93f;
        public const float FireCenterY = 72.5f;
        public const float FirePlaceRadiusPercent = 6.8f;
        public const float ReloadCenterX = 87.2f;
        public const float ReloadCenterY = 62.2f;
        public const float GrenadeCenterX = 84.8f;
        public const float GrenadeCenterY = 72.5f;
        public const float RollCenterX = 93f;
        public const float RollCenterY = 57.9f;
        public const float SmallActionPlaceRadiusPercent = 4.2f;
        public const float ActionClusterEdgeGapWidthPercent = 2f;
        public const float ActionClusterHitPaddingPx = 9f;
        public const float ReloadLabelPadPercent = 3.8f;
        public const float ReloadEmptyLabelPadPercent = 4.4f;
        public const string PauseButtonName = "Greybox_Hud_Pause";
        public const string SettingsGearButtonName = "Greybox_Hud_SettingsGear";
        public const float SettingsGearCenterX = 89.5f;
        public const float SettingsGearCenterY = 6.2f;
        public const float SettingsGearRadiusPercent = 2.7f;
        public const float HeartsRootX0 = 31f;
        public const float HeartsRootY0 = 92.2f;
        public const float HeartsRootX1 = 69f;
        public const float HeartsRootY1 = 97.2f;
        public const float HeartsLegacyTopY1 = 8.5f;
        public const string HpTextName = "Greybox_Hud_HpText";
        public const string HpBarFillName = "Greybox_Hud_HpBarFill";
        public const string ScoreTextName = "Greybox_Hud_Score";
        public const string ScoreLabelName = "Greybox_Hud_ScoreLabel";
        public const string AmmoTextName = "Greybox_Hud_AmmoText";
        public const string WaveTextName = "Greybox_Hud_Wave";
        public const string DbgButtonName = "Greybox_Hud_Dbg";
        public const string AmmoPipPrefix = "Greybox_Hud_AmmoPip_";

        public static Canvas Build(Camera worldCamera, Transform parent)
        {
            var canvasGo = new GameObject("Greybox_Hud");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = worldCamera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = Proto_GreyboxLayout.SortingHudCanvas;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            Proto_GreyboxUiEventSystem.Ensure();

            PlaceFloatingText(canvasGo.transform, ScoreLabelName, "SCORE", 2.5f, 2f, 10f, 7.5f, TextAnchor.MiddleLeft,
                16, FontStyle.Normal);
            PlaceFloatingText(canvasGo.transform, ScoreTextName, "0", 9f, 2f, 22f, 7.5f, TextAnchor.MiddleLeft, 22,
                FontStyle.Bold);
            PlaceFloatingText(canvasGo.transform, WaveTextName, "STAGE 1 · WAVE 1/3", 35f, 2f, 65f, 8f,
                TextAnchor.MiddleCenter, 16, FontStyle.Bold);
            PlaceDbgChip(canvasGo.transform);
            PlaceCircle(canvasGo.transform, PauseButtonName, 95.5f, 6.2f, 3.0f, Proto_GreyboxPalette.HudGlass, false);
            PlaceText(canvasGo.transform, "Greybox_Hud_PauseLabel", "||", 93.8f, 4.6f, 97.2f, 8.2f,
                TextAnchor.MiddleCenter, 13);
            PlaceCircle(canvasGo.transform, SettingsGearButtonName, SettingsGearCenterX, SettingsGearCenterY,
                SettingsGearRadiusPercent, new Color(0.18f, 0.2f, 0.28f, 0.82f), false);
            PlaceText(canvasGo.transform, "Greybox_Hud_SettingsGearLabel", "⚙", SettingsGearCenterX - 2.4f,
                SettingsGearCenterY - 2.4f, SettingsGearCenterX + 2.4f, SettingsGearCenterY + 2.4f,
                TextAnchor.MiddleCenter, 14);

            PlaceHpBar(canvasGo.transform);

            PlaceJoystick(canvasGo.transform);
            PlaceClusterActionButton(canvasGo.transform, RollButtonName, "Greybox_Hud_RollLabel", "ROLL",
                Proto_GreyboxSpriteUtil.HudIconKind.Roll, RollCenterX, RollCenterY);
            PlaceCircle(canvasGo.transform, FireButtonName, FireCenterX, FireCenterY, FirePlaceRadiusPercent,
                Proto_GreyboxPalette.HudFire, false);
            PlaceHudIcon(canvasGo.transform, FireButtonName + "_Icon", Proto_GreyboxSpriteUtil.HudIconKind.Fire,
                FireCenterX, FireCenterY, FirePlaceRadiusPercent * 0.5f, new Color(0.12f, 0.1f, 0.08f, 0.92f));
            PlaceLabelAroundCenter(canvasGo.transform, "Greybox_Hud_FireLabel", "FIRE", FireCenterX, FireCenterY,
                FirePlaceRadiusPercent);
            PlaceAmmoPips(canvasGo.transform);
            PlaceText(canvasGo.transform, AmmoTextName, "5/5", 90f, 81.5f, 98f, 85.5f, TextAnchor.MiddleCenter, 11);
            PlaceClusterActionButton(canvasGo.transform, ReloadButtonName, ReloadLabelName, "RELOAD",
                Proto_GreyboxSpriteUtil.HudIconKind.Reload, ReloadCenterX, ReloadCenterY);
            PlaceReloadRadialFill(canvasGo.transform);
            PlaceClusterActionButton(canvasGo.transform, GrenadeButtonName, "Greybox_Hud_GrenadeLabel", "GRENADE",
                Proto_GreyboxSpriteUtil.HudIconKind.Grenade, GrenadeCenterX, GrenadeCenterY);

            return canvas;
        }

        static void PlaceHpBar(Transform parent)
        {
            PlaceRect(parent, "Greybox_Hud_HpBarTrack", 35f, 92.2f, 64f, 95.8f, Proto_GreyboxPalette.HudHpTrack);
            var fillGo = new GameObject(HpBarFillName);
            fillGo.transform.SetParent(parent, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            ApplyRect(fillRt, 35f, 92.2f, 64f, 95.8f);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            fill.color = Proto_GreyboxPalette.HudHpFill;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            PlaceText(parent, HpTextName, "HP  25/25", 35f, 92.2f, 64f, 95.8f, TextAnchor.MiddleCenter, 13);
        }

        static void PlaceAmmoPips(Transform parent)
        {
            int mag = Proto_GreyboxTuning.MagazineSize;
            float span = Mathf.Min(7f, mag * 1.55f);
            float start = 91.5f - span * 0.5f;
            for (int i = 0; i < mag; i++)
            {
                float cx = mag == 1 ? 92f : start + i * (span / Mathf.Max(1, mag - 1));
                PlaceCircle(parent, AmmoPipPrefix + i, cx, 80.6f, 0.55f, Proto_GreyboxPalette.HudAmmoPipOn, false);
            }
        }

        static void PlaceReloadRadialFill(Transform parent)
        {
            var go = new GameObject(ReloadFillName);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            ApplyRect(rt, ReloadCenterX - SmallActionPlaceRadiusPercent, ReloadCenterY - SmallActionPlaceRadiusPercent,
                ReloadCenterX + SmallActionPlaceRadiusPercent, ReloadCenterY + SmallActionPlaceRadiusPercent);
            var img = go.AddComponent<Image>();
            img.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
            img.color = new Color(1f, 0.86f, 0.2f, 0.55f);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
            img.fillAmount = 0f;
            img.raycastTarget = false;
        }

        static void PlaceDbgChip(Transform parent)
        {
            PlaceRect(parent, DbgButtonName, 3.2f, 8.6f, 8.6f, 11.4f, Proto_GreyboxPalette.HudDbgChip);
            PlaceText(parent, DbgButtonName + "_Label", "DBG", 3.2f, 8.6f, 8.6f, 11.4f, TextAnchor.MiddleCenter, 11);
        }

        const float JoystickRadiusPercent = 12f;
        const float JoystickKnobRadiusPercent = 4f;

        static void PlaceJoystick(Transform parent)
        {
            PlaceCircle(parent, JoystickRingName, JoystickCenterXPercent, JoystickCenterYPercentFromTop,
                JoystickRadiusPercent, Color.white, true);
            PlaceCircle(parent, JoystickKnobName, JoystickCenterXPercent, JoystickCenterYPercentFromTop,
                JoystickKnobRadiusPercent, new Color(1f, 1f, 1f, 0.96f), false);
        }

        static void PlaceClusterActionButton(Transform parent, string buttonName, string labelName, string label,
            Proto_GreyboxSpriteUtil.HudIconKind icon, float cx, float cy)
        {
            PlaceCircle(parent, buttonName, cx, cy, SmallActionPlaceRadiusPercent, Proto_GreyboxPalette.HudActionDisc,
                false);
            PlaceHudIcon(parent, buttonName + "_Icon", icon, cx, cy, SmallActionPlaceRadiusPercent * 0.68f);
            PlaceLabelAroundCenter(parent, labelName, label, cx, cy, SmallActionPlaceRadiusPercent);
        }

        static void PlaceLabelAroundCenter(Transform parent, string name, string text, float cx, float cy,
            float placeRadiusPercent)
        {
            float pad = placeRadiusPercent * 0.92f;
            PlaceLabel(parent, name, text, cx - pad, cy - pad, cx + pad, cy + pad);
        }

        public static float CircleVisualRadiusPx(float placeRadiusPercent) =>
            RefHeight * (placeRadiusPercent / 100f);

        public static float CircleHitRadiusPx(float placeRadiusPercent,
            float paddingPx = ActionClusterHitPaddingPx) =>
            CircleVisualRadiusPx(placeRadiusPercent) + paddingPx;

        public static bool CircleHitTest(float xPct, float yPct, float cx, float cy, float placeRadiusPercent,
            float paddingPx = ActionClusterHitPaddingPx)
        {
            float hitR = CircleHitRadiusPx(placeRadiusPercent, paddingPx);
            float x = xPct / 100f * RefWidth;
            float y = yPct / 100f * RefHeight;
            float cxPx = cx / 100f * RefWidth;
            float cyPx = cy / 100f * RefHeight;
            float dx = x - cxPx;
            float dy = y - cyPx;
            return dx * dx + dy * dy <= hitR * hitR;
        }

        public static float ClusterCenterDistancePx(float ax, float ay, float bx, float by)
        {
            float dx = (ax - bx) / 100f * RefWidth;
            float dy = (ay - by) / 100f * RefHeight;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static float ExpectedClusterCenterDistancePx(float firePlaceRadius, float smallPlaceRadius,
            float edgeGapWidthPercent = ActionClusterEdgeGapWidthPercent)
        {
            float gapPx = RefWidth * (edgeGapWidthPercent / 100f);
            return CircleVisualRadiusPx(firePlaceRadius) + gapPx + CircleVisualRadiusPx(smallPlaceRadius);
        }

        static void PlaceHudIcon(Transform parent, string name, Proto_GreyboxSpriteUtil.HudIconKind kind, float cx,
            float cy, float halfSizePercent, Color? tint = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            ApplyRect(rt, cx - halfSizePercent, cy - halfSizePercent, cx + halfSizePercent, cy + halfSizePercent);
            var img = go.AddComponent<Image>();
            img.sprite = Proto_GreyboxSpriteUtil.GetHudIconSprite(kind);
            img.color = tint ?? new Color(1f, 1f, 1f, 0.92f);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }

        static void PlaceFloatingText(Transform parent, string name, string text, float x0, float y0, float x1, float y1,
            TextAnchor alignment, int size, FontStyle style = FontStyle.Normal)
        {
            var label = PlaceText(parent, name, text, x0, y0, x1, y1, alignment, size);
            label.fontStyle = style;
            label.color = new Color(0.96f, 0.97f, 0.99f, 0.95f);
        }

        static void PlaceCircle(Transform parent, string name, float centerX, float centerY, float radiusPercent,
            Color color, bool ring)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            ApplyRect(rt, centerX - radiusPercent, centerY - radiusPercent, centerX + radiusPercent,
                centerY + radiusPercent);
            var img = go.AddComponent<Image>();
            img.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(ring);
            img.color = color;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }

        static void PlaceLabel(Transform parent, string name, string text, float x0, float y0, float x1, float y1)
        {
            var t = PlaceText(parent, name, text, x0, y0, x1, y1, TextAnchor.MiddleCenter, 11);
            t.color = new Color(1f, 1f, 1f, 0.7f);
        }

        static void PlaceRect(Transform parent, string name, float x0, float y0, float x1, float y1, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            ApplyRect(rt, x0, y0, x1, y1);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
        }

        static Text PlaceText(Transform parent, string name, string text, float x0, float y0, float x1, float y1,
            TextAnchor alignment, int size = 18)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            ApplyRect(rt, x0, y0, x1, y1);
            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        public static void ApplyRect(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0 / 100f, 1f - y1 / 100f);
            rt.anchorMax = new Vector2(x1 / 100f, 1f - y0 / 100f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void ApplyHeartsRootRect(RectTransform rt) =>
            ApplyRect(rt, HeartsRootX0, HeartsRootY0, HeartsRootX1, HeartsRootY1);

        public static void SetJoystickKnob(RectTransform knob, Vector2 analog)
        {
            if (knob == null) return;
            var parent = knob.parent as RectTransform;
            if (parent == null) return;
            float maxPx = RefHeight * (JoystickRadiusPercent / 100f) * 0.52f;
            var offset = analog * maxPx;
            knob.anchoredPosition = offset;
        }

        public static Font GetUiFont(out bool hangul)
        {
            hangul = false;
            var builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtin == null)
                builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var os = Font.CreateDynamicFontFromOSFont(
                new[] { "Gothic A1", "Malgun Gothic", "NanumGothic", "Noto Sans CJK KR", "Arial" }, 32);
            if (os != null && SupportsHangul(os))
            {
                hangul = true;
                return os;
            }

            if (builtin != null)
                return builtin;
            return os != null ? os : builtin;
        }

        static bool SupportsHangul(Font font)
        {
            if (font == null) return false;
            font.RequestCharactersInTexture("생존처치", 24, FontStyle.Normal);
            foreach (var c in font.characterInfo)
            {
                if (c.index == '생')
                    return true;
            }

            return false;
        }
    }
}
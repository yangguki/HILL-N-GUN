using HillNGun.Proto.Greybox;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_TEXTMESHPRO || true
using TMPro;
#endif

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>Applies designer 3D-view HUD skins over the existing Greybox HUD. 2D is restored on exit.</summary>
    public static class Proto_Greybox3DHudSkin
    {
        const string OverlayName = "Greybox3D_HudOverlay";
        const string CrosshairName = "Greybox3D_Hud_Crosshair";
        const string HpRootName = "Greybox3D_Hud_Hp";
        static bool _applied;
        static Transform _overlay;
        static Image _crosshair;
        static Image[] _hpFills = new Image[3];
        static Image[] _hpTracks = new Image[3];
        static Text _scoreText;
        static Text _stageText;
        static TMP_Text _scoreTmp;
        static TMP_Text _stageTmp;
        static float _hitT;
        static int _lastScore = int.MinValue;

        public static bool IsApplied => _applied;

        public static void Apply()
        {
            var hud = GameObject.Find("Greybox_Hud");
            if (hud == null) return;
            var cat = Proto_Greybox3DCatalog.Load();
            if (cat == null || !cat.HasDesignerUi) return;

            RestoreInternal(keepFlag: false);
            Proto_Greybox3DHudLayout.Enabled = true;
            _applied = true;

            SkinImage(hud.transform, Proto_GreyboxHudBuilder.JoystickRingName, cat.UiJoystickBase, L.JoyX, L.JoyY, L.JoyBaseRadius * 2f, L.JoyBaseRadius * 2f);
            SkinImage(hud.transform, Proto_GreyboxHudBuilder.JoystickKnobName, cat.UiJoystickKnob, L.JoyX, L.JoyY, L.JoyKnobRadius * 2f, L.JoyKnobRadius * 2f);
            SkinButton(hud.transform, Proto_GreyboxHudBuilder.FireButtonName, cat.UiBtnAttackNormal, L.AttackX, L.AttackY, L.AttackRadius * 2f);
            SkinButton(hud.transform, Proto_GreyboxHudBuilder.ReloadButtonName, cat.UiBtnReloadNormal, L.ReloadX, L.ReloadY, L.ReloadRadius * 2f);
            SkinButton(hud.transform, Proto_GreyboxHudBuilder.GrenadeButtonName, cat.UiBtnSkillNormal, L.SkillX, L.SkillY, L.SkillRadius * 2f);
            SkinButton(hud.transform, Proto_GreyboxHudBuilder.RollButtonName, null, L.RollX, L.RollY, L.RollRadius * 2f, keepExistingSprite: true);
            SkinButton(hud.transform, Proto_GreyboxHudBuilder.PauseButtonName, cat.UiBtnPauseNormal, L.PauseX, L.PauseY, L.PauseRadius * 2f);

            Hide(hud.transform, "Greybox_Hud_FireLabel");
            Hide(hud.transform, "Greybox_Hud_Fire_Icon");
            Hide(hud.transform, Proto_GreyboxHudBuilder.FireButtonName + "_Icon");
            Hide(hud.transform, "Greybox_Hud_ReloadLabel");
            Hide(hud.transform, Proto_GreyboxHudBuilder.ReloadButtonName + "_Icon");
            Hide(hud.transform, "Greybox_Hud_GrenadeLabel");
            Hide(hud.transform, Proto_GreyboxHudBuilder.GrenadeButtonName + "_Icon");
            Hide(hud.transform, "Greybox_Hud_RollLabel");
            Hide(hud.transform, Proto_GreyboxHudBuilder.RollButtonName + "_Icon");
            Hide(hud.transform, "Greybox_Hud_PauseLabel");
            Hide(hud.transform, Proto_GreyboxHudBuilder.SettingsGearButtonName);
            Hide(hud.transform, Proto_GreyboxHudBuilder.DbgButtonName);
            Hide(hud.transform, "Greybox_Hud_DbgLabel");
            Hide(hud.transform, "Greybox_Hud_SettingsGearLabel");
            Hide(hud.transform, Proto_GreyboxHudBuilder.ScoreLabelName);
            Hide(hud.transform, Proto_GreyboxHudBuilder.ScoreTextName);
            Hide(hud.transform, Proto_GreyboxHudBuilder.WaveTextName);
            Hide(hud.transform, "Greybox_Hud_Hearts");
            Hide(hud.transform, "Greybox_Hud_HpBarTrack");
            Hide(hud.transform, Proto_GreyboxHudBuilder.HpBarFillName);
            Hide(hud.transform, Proto_GreyboxHudBuilder.HpTextName);
            Hide(hud.transform, Proto_GreyboxHudBuilder.ReloadFillName);
            Hide(hud.transform, Proto_GreyboxHudBuilder.AmmoTextName);

            BuildOverlay(hud.transform, cat);
        }

        public static void Restore()
        {
            RestoreInternal(keepFlag: false);
        }

        static void RestoreInternal(bool keepFlag)
        {
            Proto_Greybox3DHudLayout.Enabled = false;
            if (_overlay != null)
                Proto_Greybox3DUtil.DestroySafe(_overlay.gameObject);
            _overlay = null;
            _crosshair = null;
            _scoreText = null;
            _stageText = null;
            _scoreTmp = null;
            _stageTmp = null;
            for (int i = 0; i < 3; i++) { _hpFills[i] = null; _hpTracks[i] = null; }
            if (!keepFlag) _applied = false;

            var hud = GameObject.Find("Greybox_Hud");
            if (hud == null) return;
            // Re-show stock widgets; positions are rebuilt on the next 2D frame / EnsureBound.
            ShowAll(hud.transform);
        }

        public static void Tick(Proto_GreyboxPlayerController host, Camera worldCam)
        {
            if (!_applied)
            {
                if (Proto_GreyboxViewModeState.Is3D) Apply();
                if (!_applied) return;
            }
            if (host == null) return;

            if (host.Score != _lastScore)
            {
                if (_lastScore != int.MinValue && host.Score > _lastScore)
                    NotifyHit();
                _lastScore = host.Score;
            }

            if (_hitT > 0f) _hitT -= Application.isPlaying ? Time.deltaTime : 0.02f;
            var cat = Proto_Greybox3DCatalog.Load();
            if (_crosshair != null && cat != null)
            {
                _crosshair.sprite = _hitT > 0f && cat.UiCrosshairHit != null ? cat.UiCrosshairHit : cat.UiCrosshair;
                if (worldCam != null)
                {
                    var aim = Proto_Greybox3DFootMap.GroundPointToWorld(host.Aim.AimGroundPosition()) + Vector3.up * 0.35f;
                    var vp = worldCam.WorldToViewportPoint(aim);
                    var rt = _crosshair.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(vp.x), Mathf.Clamp01(vp.y));
                    rt.anchoredPosition = Vector2.zero;
                    _crosshair.enabled = vp.z > 0.1f;
                }
            }

            if (host.Hearts != null)
            {
                float total = 0f;
                for (int i = 0; i < 3; i++)
                {
                    float fill = host.Hearts.FillForHeart(i);
                    total += fill;
                    if (_hpFills[i] != null)
                    {
                        _hpFills[i].fillAmount = fill;
                        bool low = fill <= 0.30f;
                        if (cat != null)
                        {
                            var sprite = low && cat.UiHpFillLow != null ? cat.UiHpFillLow : cat.UiHpFill;
                            if (sprite != null) _hpFills[i].sprite = sprite;
                        }
                    }
                }
            }

            string score = host.Score.ToString();
            int wave = 1;
            int totalWaves = 3;
            if (host.WaveDirector != null)
            {
                wave = Mathf.Max(1, host.WaveDirector.CurrentWave);
                totalWaves = host.WaveDirector.TotalWaves;
            }
            string stage = $"1-{wave}";
            if (_scoreTmp != null) _scoreTmp.text = score;
            else if (_scoreText != null) _scoreText.text = score;
            if (_stageTmp != null) _stageTmp.text = stage;
            else if (_stageText != null) _stageText.text = stage;

            // Pressed sprites for fire / skill / reload / pause / knob.
            if (cat != null)
            {
                SwapIf(host, Proto_GreyboxHudBuilder.JoystickKnobName, cat.UiJoystickKnob, cat.UiJoystickKnobPressed, host.Motor != null && host.Motor.LastMoveDirection.sqrMagnitude > 0.04f);
            }
        }

        public static void NotifyHit() => _hitT = 0.16f;

        static void SwapIf(Proto_GreyboxPlayerController host, string name, Sprite normal, Sprite pressed, bool on)
        {
            var hud = GameObject.Find("Greybox_Hud");
            if (hud == null) return;
            var t = hud.transform.Find(name);
            var img = t != null ? t.GetComponent<Image>() : null;
            if (img == null) return;
            var s = on && pressed != null ? pressed : normal;
            if (s != null) img.sprite = s;
        }

        static void BuildOverlay(Transform hud, Proto_Greybox3DCatalog cat)
        {
            var go = new GameObject(OverlayName, typeof(RectTransform));
            go.transform.SetParent(hud, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector3.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _overlay = go.transform;

            // SCORE label + value
            if (cat.UiTextScore != null)
                PlaceSprite(_overlay, "ScoreLabel", cat.UiTextScore, L.ScoreX, L.ScoreY, 240f / 1920f * 100f, 100f / 1080f * 100f, new Vector2(0f, 0.5f));
            PlaceValue(_overlay, "ScoreValue", L.ScoreX + 9.5f, L.ScoreY, 14f, 6f, out _scoreText, out _scoreTmp, cat, TextAnchor.MiddleLeft);

            // STAGE label + value
            if (cat.UiTextStage != null)
                PlaceSprite(_overlay, "StageLabel", cat.UiTextStage, L.StageX, L.StageY, 240f / 1920f * 100f, 100f / 1080f * 100f, new Vector2(0.5f, 0.5f));
            PlaceValue(_overlay, "StageValue", L.StageX, L.StageY + 4.6f, 12f, 5f, out _stageText, out _stageTmp, cat, TextAnchor.MiddleCenter);

            BuildHp(cat);
            var ch = PlaceSprite(_overlay, CrosshairName, cat.UiCrosshair, 50f, 50f, 64f / 1080f * 100f, 64f / 1080f * 100f, new Vector2(0.5f, 0.5f));
            _crosshair = ch;
        }

        static void BuildHp(Proto_Greybox3DCatalog cat)
        {
            var root = new GameObject(HpRootName, typeof(RectTransform));
            root.transform.SetParent(_overlay, false);
            var rt = root.GetComponent<RectTransform>();
            float w = L.HpPanelW;
            float h = L.HpPanelH;
            ApplyCenter(rt, L.HpCenterX, L.HpCenterY, w, h);

            if (cat.UiHpFrame != null)
            {
                var frame = ChildImage(root.transform, "Frame", cat.UiHpFrame, Image.Type.Sliced);
                Stretch(frame.rectTransform);
            }
            if (cat.UiTextHp != null)
            {
                var label = ChildImage(root.transform, "HpLabel", cat.UiTextHp, Image.Type.Simple);
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0.02f, 0.08f);
                lrt.anchorMax = new Vector2(0.16f, 0.92f);
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                label.preserveAspect = true;
            }

            // Three equal segments = the three hearts, with regen partial fill.
            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.18f + i * 0.27f;
                float x1 = x0 + 0.25f;
                var trackGo = new GameObject("SegTrack_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                trackGo.transform.SetParent(root.transform, false);
                var trt = trackGo.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(x0, 0.22f);
                trt.anchorMax = new Vector2(x1, 0.78f);
                trt.offsetMin = trt.offsetMax = Vector2.zero;
                var track = trackGo.GetComponent<Image>();
                track.sprite = cat.UiHpTrack;
                track.type = Image.Type.Sliced;
                track.raycastTarget = false;
                _hpTracks[i] = track;

                var fillGo = new GameObject("SegFill_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillGo.transform.SetParent(trackGo.transform, false);
                var frt = fillGo.GetComponent<RectTransform>();
                Stretch(frt);
                var fill = fillGo.GetComponent<Image>();
                fill.sprite = cat.UiHpFillFull != null ? cat.UiHpFillFull : cat.UiHpFill;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.fillAmount = 1f;
                fill.raycastTarget = false;
                fill.preserveAspect = false;
                _hpFills[i] = fill;
            }
        }

        static void PlaceValue(Transform parent, string name, float cx, float cy, float w, float h,
            out Text uitext, out TMP_Text tmp, Proto_Greybox3DCatalog cat, TextAnchor align)
        {
            uitext = null;
            tmp = null;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            ApplyCenter(rt, cx, cy, w, h);
            // Prefer TMP; if it fails (batchmode / missing TMP resources), drop the
            // TMP component and fall back to UI.Text + Outline using the TTF directly.
            TextMeshProUGUI tmpComp = null;
            try
            {
                tmpComp = go.AddComponent<TextMeshProUGUI>();
                tmpComp.alignment = align == TextAnchor.MiddleLeft ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Midline;
                tmpComp.fontSize = 36;
                tmpComp.color = Color.white;
                tmpComp.outlineWidth = 0.3f;
                tmpComp.outlineColor = new Color(0.14f, 0.09f, 0.11f, 1f);
                tmpComp.raycastTarget = false;
                if (cat != null && cat.LilitaOne != null)
                {
                    var asset = TMP_FontAsset.CreateFontAsset(cat.LilitaOne);
                    if (asset != null) tmpComp.font = asset;
                }
                if (tmpComp.font == null)
                    throw new System.InvalidOperationException("TMP font missing");
                tmp = tmpComp;
                return;
            }
            catch (System.Exception)
            {
                if (tmpComp != null)
                    Object.DestroyImmediate(tmpComp);
                tmp = null;
            }
            var t = go.AddComponent<Text>();
            if (t == null) return;
            t.alignment = align;
            t.fontSize = 22;
            t.fontStyle = FontStyle.Bold;
            t.color = Color.white;
            t.raycastTarget = false;
            if (cat != null && cat.LilitaOne != null) t.font = cat.LilitaOne;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.14f, 0.09f, 0.11f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0.08f, 0.05f, 0.06f, 0.8f);
            sh.effectDistance = new Vector2(0f, -2f);
            uitext = t;
        }

        static Image PlaceSprite(Transform parent, string name, Sprite sprite, float cx, float cy, float w, float h, Vector2 pivot)
        {
            var img = ChildImage(parent, name, sprite, Image.Type.Simple);
            var rt = img.rectTransform;
            rt.pivot = pivot;
            ApplyCenter(rt, cx, cy, w, h);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        static Image ChildImage(Transform parent, string name, Sprite sprite, Image.Type type)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            img.color = Color.white;
            img.raycastTarget = false;
            return img;
        }

        static void SkinImage(Transform hud, string name, Sprite sprite, float cx, float cy, float w, float h)
        {
            var t = hud.Find(name);
            if (t == null) return;
            var img = t.GetComponent<Image>();
            if (img != null && sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
            var rt = t.GetComponent<RectTransform>();
            if (rt != null) ApplyCenter(rt, cx, cy, w, h);
        }

        static void SkinButton(Transform hud, string name, Sprite sprite, float cx, float cy, float size, bool keepExistingSprite = false)
        {
            SkinImage(hud, name, keepExistingSprite ? null : sprite, cx, cy, size, size);
            var icon = hud.Find(name + "_Icon");
            if (icon != null) icon.gameObject.SetActive(false);
        }

        static void Hide(Transform hud, string name)
        {
            var t = hud.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }

        static void ShowAll(Transform hud)
        {
            foreach (var t in hud.GetComponentsInChildren<Transform>(true))
            {
                if (t == hud) continue;
                if (t.name == OverlayName) continue;
                t.gameObject.SetActive(true);
            }
        }

        static void ApplyCenter(RectTransform rt, float cx, float cy, float w, float h)
        {
            // Same percent space as HudBuilder.ApplyRect (x/y from top, 0-100).
            float x0 = cx - w * 0.5f;
            float x1 = cx + w * 0.5f;
            float y0 = cy - h * 0.5f;
            float y1 = cy + h * 0.5f;
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // Short alias so the layout constants stay readable.
        static class L
        {
            public const float JoyX = Proto_Greybox3DHudLayout.JoyX;
            public const float JoyY = Proto_Greybox3DHudLayout.JoyY;
            public const float JoyBaseRadius = Proto_Greybox3DHudLayout.JoyBaseRadius;
            public const float JoyKnobRadius = Proto_Greybox3DHudLayout.JoyKnobRadius;
            public const float AttackX = Proto_Greybox3DHudLayout.AttackX;
            public const float AttackY = Proto_Greybox3DHudLayout.AttackY;
            public const float AttackRadius = Proto_Greybox3DHudLayout.AttackRadius;
            public const float SkillX = Proto_Greybox3DHudLayout.SkillX;
            public const float SkillY = Proto_Greybox3DHudLayout.SkillY;
            public const float SkillRadius = Proto_Greybox3DHudLayout.SkillRadius;
            public const float ReloadX = Proto_Greybox3DHudLayout.ReloadX;
            public const float ReloadY = Proto_Greybox3DHudLayout.ReloadY;
            public const float ReloadRadius = Proto_Greybox3DHudLayout.ReloadRadius;
            public const float RollX = Proto_Greybox3DHudLayout.RollX;
            public const float RollY = Proto_Greybox3DHudLayout.RollY;
            public const float RollRadius = Proto_Greybox3DHudLayout.RollRadius;
            public const float PauseX = Proto_Greybox3DHudLayout.PauseX;
            public const float PauseY = Proto_Greybox3DHudLayout.PauseY;
            public const float PauseRadius = Proto_Greybox3DHudLayout.PauseRadius;
            public const float HpCenterX = Proto_Greybox3DHudLayout.HpCenterX;
            public const float HpCenterY = Proto_Greybox3DHudLayout.HpCenterY;
            public const float HpPanelW = Proto_Greybox3DHudLayout.HpPanelW;
            public const float HpPanelH = Proto_Greybox3DHudLayout.HpPanelH;
            public const float ScoreX = Proto_Greybox3DHudLayout.ScoreX;
            public const float ScoreY = Proto_Greybox3DHudLayout.ScoreY;
            public const float StageX = Proto_Greybox3DHudLayout.StageX;
            public const float StageY = Proto_Greybox3DHudLayout.StageY;
        }
    }
}

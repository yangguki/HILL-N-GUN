using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Designer HUD for 2D 원근 (concept layout; hit areas unchanged).</summary>
    public static class Proto_GreyboxPersp2DHud
    {
        public const string OverlayName = "Greybox_Hud_Persp2DOverlay";
        const float HeartScreenHeightPercent = 6.5f;
        static bool _applied;
        static Transform _overlay;
        static Image[] _hpHearts = new Image[3];
        static Image _crosshair;
        static float _crosshairOverrideX = -1f;
        static float _crosshairOverrideY;
        static Cluster _attack, _roll, _skill, _reload, _pause;
        static Image _joyBase, _joyKnob;
        static Sprite _joyBaseNormal, _joyKnobNormal, _joyBasePressed, _joyKnobPressed;

        struct Cluster
        {
            public Image Btn;
            public Image Icon;
            public Image Label;
            public Image Cooldown;
            public Sprite Normal;
            public Sprite Pressed;
        }

        public static bool IsApplied => _applied;

        public static void EnsureOnHud(Transform hud)
        {
            if (hud == null || !Proto_GreyboxViewModeState.IsPersp2D) return;
            if (_applied && (_overlay == null || _overlay.parent != hud))
                _applied = false;
            if (!_applied) Apply(hud);
        }

        public static void Apply(Transform hud)
        {
            if (hud == null) return;
            var cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
            if (cat == null)
            {
                Debug.LogWarning("Persp2D HUD not applied: catalog missing.");
                return;
            }

            if (!cat.HudGameplayReady(out var missing))
            {
                Debug.LogWarning("Persp2D HUD not applied; missing catalog sprites: " + missing);
                return;
            }

#if UNITY_EDITOR
            cat.EnsureEditorUiResolved();
#endif
            Restore();
            _applied = true;
            HideLegacyWidgets(hud);

            SkinJoystick(hud, cat);
            float btnSize = Proto_GreyboxPersp2DHudLayout.BtnDiameterPercent;
            _attack = SkinCluster(hud, Proto_GreyboxHudBuilder.FireButtonName, cat.uiBtnAttack,
                cat.uiBtnAttackPressed, cat.uiIconAttack, null,
                Proto_GreyboxPersp2DHudLayout.AttackX, Proto_GreyboxPersp2DHudLayout.BtnRowY, btnSize);
            _roll = SkinCluster(hud, Proto_GreyboxHudBuilder.RollButtonName, cat.uiBtnRoll,
                cat.uiBtnRollPressed, cat.uiIconRoll, null,
                Proto_GreyboxPersp2DHudLayout.RollX, Proto_GreyboxPersp2DHudLayout.BtnRowY, btnSize);
            _skill = SkinCluster(hud, Proto_GreyboxHudBuilder.GrenadeButtonName, cat.uiBtnSkill,
                cat.uiBtnSkillPressed, cat.uiIconSkill, null,
                Proto_GreyboxPersp2DHudLayout.SkillX, Proto_GreyboxPersp2DHudLayout.BtnRowY, btnSize);
            _reload = SkinCluster(hud, Proto_GreyboxHudBuilder.ReloadButtonName, cat.uiBtnReload,
                cat.uiBtnReloadPressed, cat.uiIconReload, null,
                Proto_GreyboxPersp2DHudLayout.ReloadX, Proto_GreyboxPersp2DHudLayout.BtnRowY, btnSize);
            _pause = SkinCluster(hud, Proto_GreyboxHudBuilder.PauseButtonName, cat.UiBtnPauseOrEditor,
                cat.UiBtnPausePressedOrEditor, cat.UiIconPauseOrEditor, null,
                Proto_GreyboxPersp2DHudLayout.PauseX, Proto_GreyboxPersp2DHudLayout.PauseY,
                Proto_GreyboxPersp2DHudLayout.PauseRadius * 2f);
            EnsureClusterOpaque(_pause);

            var go = new GameObject(OverlayName, typeof(RectTransform));
            go.transform.SetParent(hud, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _overlay = go.transform;

            float heartSize = HeartScreenHeightPercent;
            float spacing = heartSize * 1.12f;
            float cx = Proto_GreyboxPersp2DHudLayout.HpCenterX;
            float cy = Proto_GreyboxPersp2DHudLayout.HpCenterY;
            for (int i = 0; i < 3; i++)
            {
                float x = cx - spacing + i * spacing;
                _hpHearts[i] = Plate(_overlay, $"Heart{i}", cat.uiHeartFull, x, cy, heartSize, heartSize);
            }

            Plate(_overlay, "P2D_Label_Attack", cat.uiLabelAttack, Proto_GreyboxPersp2DHudLayout.AttackX,
                Proto_GreyboxPersp2DHudLayout.LabelRowY, Proto_GreyboxPersp2DHudLayout.LabelWidthPercent,
                Proto_GreyboxPersp2DHudLayout.LabelHeightPercent);
            Plate(_overlay, "P2D_Label_Roll", cat.uiLabelRoll, Proto_GreyboxPersp2DHudLayout.RollX,
                Proto_GreyboxPersp2DHudLayout.LabelRowY, Proto_GreyboxPersp2DHudLayout.LabelWidthPercent,
                Proto_GreyboxPersp2DHudLayout.LabelHeightPercent);
            Plate(_overlay, "P2D_Label_Skill", cat.uiLabelSkill, Proto_GreyboxPersp2DHudLayout.SkillX,
                Proto_GreyboxPersp2DHudLayout.LabelRowY, Proto_GreyboxPersp2DHudLayout.LabelWidthPercent,
                Proto_GreyboxPersp2DHudLayout.LabelHeightPercent);
            Plate(_overlay, "P2D_Label_Reload", cat.uiLabelReload, Proto_GreyboxPersp2DHudLayout.ReloadX,
                Proto_GreyboxPersp2DHudLayout.LabelRowY, Proto_GreyboxPersp2DHudLayout.LabelWidthPercent,
                Proto_GreyboxPersp2DHudLayout.LabelHeightPercent);

            if (cat.uiCrosshair != null)
                _crosshair = Plate(_overlay, "P2D_Crosshair", cat.uiCrosshair, 50f, 50f, 3.5f, 3.5f);
        }

        static void SkinJoystick(Transform hud, Proto_GreyboxPersp2DCatalog cat)
        {
            _joyBaseNormal = cat.uiJoystickBase;
            _joyKnobNormal = cat.uiJoystickKnob;
            _joyBasePressed = cat.uiJoystickBasePressed ?? cat.uiJoystickBase;
            _joyKnobPressed = cat.uiJoystickKnobPressed ?? cat.uiJoystickKnob;
            float baseSize = Proto_GreyboxPersp2DHudLayout.JoyBaseRadius * 2f;
            float knobSize = Proto_GreyboxPersp2DHudLayout.JoyKnobRadius * 2f;
            SkinButton(hud, Proto_GreyboxHudBuilder.JoystickRingName, _joyBaseNormal,
                Proto_GreyboxPersp2DHudLayout.JoyX, Proto_GreyboxPersp2DHudLayout.JoyY, baseSize);
            SkinButton(hud, Proto_GreyboxHudBuilder.JoystickKnobName, _joyKnobNormal,
                Proto_GreyboxPersp2DHudLayout.JoyX, Proto_GreyboxPersp2DHudLayout.JoyY, knobSize);
            var ring = hud.Find(Proto_GreyboxHudBuilder.JoystickRingName);
            var knob = hud.Find(Proto_GreyboxHudBuilder.JoystickKnobName);
            _joyBase = ring != null ? ring.GetComponent<Image>() : null;
            _joyKnob = knob != null ? knob.GetComponent<Image>() : null;
        }

        static Cluster SkinCluster(Transform hud, string btnName, Sprite normal, Sprite pressed, Sprite icon,
            Sprite label, float cx, float cy, float size)
        {
            var cluster = new Cluster { Normal = normal, Pressed = pressed };
            SkinButton(hud, btnName, normal, cx, cy, size);
            var t = hud.Find(btnName);
            if (t == null) return cluster;
            ClearPerspClusterChildren(t);
            cluster.Btn = t.GetComponent<Image>();
            var cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
            if (icon != null)
                cluster.Icon = ChildPlate(t, "P2D_Icon", icon, 0f, 0.1f, size * 0.38f);
            if (cat?.uiBtnCooldownOverlay != null)
            {
                cluster.Cooldown = ChildPlate(t, "P2D_Cooldown", cat.uiBtnCooldownOverlay, 0f, 0f, size, size);
                cluster.Cooldown.type = Image.Type.Filled;
                cluster.Cooldown.fillMethod = Image.FillMethod.Radial360;
                cluster.Cooldown.fillAmount = 0f;
                cluster.Cooldown.gameObject.SetActive(false);
            }
            return cluster;
        }

        static void HideLegacyWidgets(Transform hud)
        {
            Hide(hud, Proto_GreyboxHudBuilder.FireButtonName + "_Icon");
            Hide(hud, Proto_GreyboxHudBuilder.ReloadButtonName + "_Icon");
            Hide(hud, Proto_GreyboxHudBuilder.GrenadeButtonName + "_Icon");
            Hide(hud, Proto_GreyboxHudBuilder.RollButtonName + "_Icon");
            Hide(hud, "Greybox_Hud_FireLabel");
            Hide(hud, "Greybox_Hud_ReloadLabel");
            Hide(hud, "Greybox_Hud_GrenadeLabel");
            Hide(hud, "Greybox_Hud_RollLabel");
            Hide(hud, Proto_GreyboxHudBuilder.PauseButtonName + "_Label");
            Hide(hud, Proto_GreyboxHudBuilder.SettingsGearButtonName);
            Hide(hud, "Greybox_Hud_SettingsGearLabel");
            Hide(hud, Proto_GreyboxHudBuilder.DbgButtonName);
            Hide(hud, Proto_GreyboxHudBuilder.DbgButtonName + "_Label");
            Hide(hud, Proto_GreyboxHudBuilder.AmmoTextName);
            for (int i = 0; i < Proto_GreyboxTuning.MagazineSize; i++)
                Hide(hud, Proto_GreyboxHudBuilder.AmmoPipPrefix + i);
            Hide(hud, Proto_GreyboxHudBuilder.ScoreLabelName);
            Hide(hud, Proto_GreyboxHudBuilder.ScoreTextName);
            Hide(hud, Proto_GreyboxHudBuilder.WaveTextName);
            Hide(hud, Proto_GreyboxHudBuilder.PauseButtonName + "_Label");
            Hide(hud, "Greybox_Hud_PauseLabel");
            var pauseRt = hud.Find(Proto_GreyboxHudBuilder.PauseButtonName);
            if (pauseRt != null)
            {
                var legacyLabel = pauseRt.parent.Find("Greybox_Hud_PauseLabel");
                if (legacyLabel != null)
                    legacyLabel.gameObject.SetActive(false);
            }
            Hide(hud, "Greybox_Hud_Hearts");
            Hide(hud, Proto_GreyboxHudBuilder.HpBarFillName);
            Hide(hud, "Greybox_Hud_HpBarTrack");
            Hide(hud, Proto_GreyboxHudBuilder.HpTextName);
            Hide(hud, Proto_GreyboxHudBuilder.ReloadFillName);
            var cyl = hud.Find("Greybox_Hud_Cylinder");
            if (cyl != null) cyl.gameObject.SetActive(false);
            cyl = hud.Find(Proto_GreyboxHudBuilder.FireButtonName + "/Greybox_Hud_Cylinder");
            if (cyl != null) cyl.gameObject.SetActive(false);
        }

        public static void Tick(Proto_GreyboxPlayerController host, Proto_GreyboxInput input)
        {
            if (!_applied || host == null) return;
            var cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
            if (cat == null) return;
            for (int i = 0; i < 3; i++)
            {
                if (_hpHearts[i] == null) continue;
                float fill = host.Hearts.FillForHeart(i);
                var sp = fill > 0.01f ? cat.uiHeartFull : cat.uiHeartEmpty;
                if (sp != null)
                {
                    _hpHearts[i].sprite = sp;
                    _hpHearts[i].color = Color.white;
                }
            }

            var tuning = host.RuntimeTuning;
            bool fire = input.FireQuickAuto || input.FireStickHeld;
            SetCluster(_attack, fire, 0f);
            float rollDur = Mathf.Max(tuning.RollDuration, 0.01f);
            float rollCd = host.Roll.IsRolling
                ? Mathf.Clamp01(host.Roll.Elapsed / rollDur)
                : tuning.RollCooldown > 0.01f
                    ? host.Roll.CooldownRemaining / tuning.RollCooldown
                    : 0f;
            SetCluster(_roll, input.RollPressed || host.Roll.IsRolling, rollCd);
            float grenadeProg = host.Grenade.RechargeProgress01(host.SimTime);
            float skillCd = grenadeProg > 0.01f && !host.Grenade.CanThrow(host.SimTime) ? 1f - grenadeProg : 0f;
            SetCluster(_skill, input.GrenadePressed, skillCd);
            float reloadCd = host.Gun.IsReloading ? host.Gun.ReloadProgress01(host.SimTime) : 0f;
            SetCluster(_reload, input.ReloadPressed || host.Gun.IsReloading, reloadCd);

            if (_crosshair != null)
            {
                float xPct;
                float yPct;
                bool show;
                if (_crosshairOverrideX >= 0f)
                {
                    xPct = _crosshairOverrideX;
                    yPct = _crosshairOverrideY;
                    show = true;
                }
                else
                    show = host.TryGetPersp2DCrosshairScreen(out xPct, out yPct);

                _crosshair.enabled = show;
                if (show)
                    Center(_crosshair.rectTransform, xPct, yPct, 3.5f, 3.5f);
            }

            bool stick = input.Move.sqrMagnitude > 0.04f;
            if (_joyBase != null && _joyBaseNormal != null)
                _joyBase.sprite = stick ? _joyBasePressed : _joyBaseNormal;
            if (_joyKnob != null && _joyKnobNormal != null)
            {
                _joyKnob.sprite = stick ? _joyKnobPressed : _joyKnobNormal;
                var knobRt = _joyKnob.rectTransform;
                float knobOff = Proto_GreyboxPersp2DHudLayout.JoyKnobRadius * 0.85f;
                knobRt.anchoredPosition = stick
                    ? new Vector2(input.Move.x * knobOff / 100f * 1280f, -input.Move.y * knobOff / 100f * 720f)
                    : Vector2.zero;
            }
        }

        static void SetCluster(Cluster c, bool pressed, float cooldown01)
        {
            if (c.Btn == null) return;
            c.Btn.sprite = pressed && c.Pressed != null ? c.Pressed : c.Normal;
            if (c.Cooldown != null)
            {
                bool on = cooldown01 > 0.01f;
                c.Cooldown.gameObject.SetActive(on);
                if (on) c.Cooldown.fillAmount = Mathf.Clamp01(cooldown01);
            }
        }

        public static void SetCrosshairCaptureOverride(float xPct, float yPct)
        {
            _crosshairOverrideX = xPct;
            _crosshairOverrideY = yPct;
        }

        public static void ClearCrosshairCaptureOverride() => _crosshairOverrideX = -1f;

        public static void SetPausePressedForCapture(bool pressed)
        {
            if (_pause.Btn != null)
                SetCluster(_pause, pressed, 0f);
        }

        public static void Restore()
        {
            _applied = false;
            _crosshairOverrideX = -1f;
            _crosshair = null;
            _attack = _roll = _skill = _reload = _pause = default;
            if (_overlay != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) Object.DestroyImmediate(_overlay.gameObject);
                else
#endif
                    Object.Destroy(_overlay.gameObject);
            }
            _overlay = null;
            for (int i = 0; i < 3; i++) _hpHearts[i] = null;
            var hud = GameObject.Find("Greybox_Hud");
            if (hud == null) return;
            ShowAll(hud.transform);
        }

        static void ClearPerspClusterChildren(Transform btn)
        {
            for (int i = btn.childCount - 1; i >= 0; i--)
            {
                var c = btn.GetChild(i);
                if (c.name.StartsWith("P2D_"))
                {
#if UNITY_EDITOR
                    if (!UnityEngine.Application.isPlaying)
                        Object.DestroyImmediate(c.gameObject);
                    else
#endif
                        Object.Destroy(c.gameObject);
                }
            }
        }

        static void EnsureClusterOpaque(Cluster c)
        {
            if (c.Btn != null)
            {
                c.Btn.color = Color.white;
                var cg = c.Btn.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }

            if (c.Icon != null)
            {
                c.Icon.color = Color.white;
                c.Icon.gameObject.SetActive(true);
                var icg = c.Icon.GetComponent<CanvasGroup>();
                if (icg != null) icg.alpha = 1f;
            }

            if (c.Btn != null)
            {
                c.Btn.transform.SetAsLastSibling();
                c.Icon?.transform.SetAsLastSibling();
            }
        }

        static void SkinButton(Transform hud, string name, Sprite sprite, float cx, float cy, float size)
        {
            if (sprite == null) return;
            var t = hud.Find(name);
            if (t == null) return;
            var img = t.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
                img.type = Image.Type.Simple;
            }

            var cg = t.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            var rt = t.GetComponent<RectTransform>();
            if (rt != null) Center(rt, cx, cy, size, size);
        }

        static Image ChildPlate(Transform parent, string name, Sprite sprite, float offsetYPercent, float offsetXPercent,
            float w, float h = -1f)
        {
            if (h < 0f) h = w;
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = parent.GetComponent<RectTransform>().sizeDelta * (w / Mathf.Max(parent.GetComponent<RectTransform>().sizeDelta.x, 1f));
            if (rt.sizeDelta.sqrMagnitude < 1f)
                rt.sizeDelta = new Vector2(w / 100f * 1280f, h / 100f * 720f);
            rt.anchoredPosition = new Vector2(offsetXPercent * rt.sizeDelta.x, offsetYPercent * rt.sizeDelta.y);
            return img;
        }

        static Image Plate(Transform parent, string name, Sprite sprite, float cx, float cy, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
            Center(img.rectTransform, cx, cy, w, h);
            return img;
        }

        static void Center(RectTransform rt, float cx, float cy, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(cx / 100f, 1f - cy / 100f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w / 100f * 1280f, h / 100f * 720f);
            rt.anchoredPosition = Vector2.zero;
        }

        static void Hide(Transform hud, string name)
        {
            var t = hud.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }

        static void ShowAll(Transform hud)
        {
            for (int i = 0; i < hud.childCount; i++)
                hud.GetChild(i).gameObject.SetActive(true);
        }
    }
}
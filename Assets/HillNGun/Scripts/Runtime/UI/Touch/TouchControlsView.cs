using UnityEngine;
using UnityEngine.UI;
using HillNGun.Data;
using HillNGun.Input;
using HillNGun.Characters;

namespace HillNGun.UI.Touch
{
    /// <summary>
    /// uGUI view of the touch controls (view only: reads the router + character every frame, never handles input;
    /// all Images have raycastTarget off, touches go through TouchControlRouter). One atlas texture → ~1 batch.
    /// </summary>
    public class TouchControlsView : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _root;

        private class Stick { public Image Base, Ring, Knob, Icon, Cooldown; public Image[] Pips; }
        private const int MaxAmmoPips = 8;
        private class Button { public Image Bg, Icon, Cooldown, Ring, Knob; public Image[] Pips; }

        private Stick _move, _attack;
        private Button _roll, _skill, _grenade;
        private bool _built;

        public static TouchControlsView Create(Transform parent)
        {
            var go = new GameObject("[TouchControlsView]");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<TouchControlsView>();
            v.Build();
            return v;
        }

        private void Build()
        {
            if (_built) return;
            _built = true;
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 40;       // above the field, below IMGUI HUD/menus (IMGUI always draws last)
            _canvas.pixelPerfect = false;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; // layout is computed in screen pixels
            scaler.scaleFactor = 1f;
            _root = (RectTransform)transform;

            _move = MakeStick("Move", TouchIconAtlas.Cell.Arrow);
            _attack = MakeStick("Attack", TouchIconAtlas.Cell.Crosshair);
            _attack.Cooldown = MakeImage("AttackCock", TouchIconAtlas.Cell.RingThin);
            _attack.Cooldown.type = Image.Type.Filled;
            _attack.Cooldown.fillMethod = Image.FillMethod.Radial360;
            _attack.Cooldown.fillOrigin = (int)Image.Origin360.Top;
            _attack.Cooldown.fillClockwise = true;
            _attack.Pips = new Image[MaxAmmoPips];
            for (int i = 0; i < MaxAmmoPips; i++) _attack.Pips[i] = MakeImage("AmmoPip" + i, TouchIconAtlas.Cell.Pip);
            _roll = MakeButton("Roll", TouchIconAtlas.Cell.Roll, 0);
            _skill = MakeButton("Skill", TouchIconAtlas.Cell.Star, 0);
            _grenade = MakeButton("Grenade", TouchIconAtlas.Cell.Dynamite, 3);
        }

        private Image MakeImage(string name, TouchIconAtlas.Cell cell, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : _root, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = TouchIconAtlas.Get(cell);
            img.raycastTarget = false;
            return img;
        }

        private Stick MakeStick(string name, TouchIconAtlas.Cell icon)
        {
            return new Stick
            {
                Base = MakeImage(name + "Base", TouchIconAtlas.Cell.Disc),
                Ring = MakeImage(name + "Ring", TouchIconAtlas.Cell.Ring),
                Knob = MakeImage(name + "Knob", TouchIconAtlas.Cell.Knob),
                Icon = MakeImage(name + "Icon", icon),
            };
        }

        private Button MakeButton(string name, TouchIconAtlas.Cell icon, int pips)
        {
            var b = new Button
            {
                Bg = MakeImage(name + "Bg", TouchIconAtlas.Cell.Disc),
                Icon = MakeImage(name + "Icon", icon),
                Cooldown = MakeImage(name + "Cooldown", TouchIconAtlas.Cell.Disc),
                Ring = MakeImage(name + "Ring", TouchIconAtlas.Cell.RingThin),
                Knob = MakeImage(name + "DragKnob", TouchIconAtlas.Cell.Knob),
                Pips = new Image[pips]
            };
            b.Cooldown.type = Image.Type.Filled;
            b.Cooldown.fillMethod = Image.FillMethod.Radial360;
            b.Cooldown.fillOrigin = (int)Image.Origin360.Top;
            b.Cooldown.fillClockwise = false;
            for (int i = 0; i < pips; i++) b.Pips[i] = MakeImage(name + "Pip" + i, TouchIconAtlas.Cell.Pip);
            return b;
        }

        public void SetVisible(bool on)
        {
            if (_canvas != null && _canvas.enabled != on) _canvas.enabled = on;
        }

        private static void Place(Image img, Vector2 center, float radius, Color color, bool show = true)
        {
            if (img.gameObject.activeSelf != show) img.gameObject.SetActive(show);
            if (!show) return;
            var rt = img.rectTransform;
            rt.anchoredPosition = center;
            rt.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            if (img.color != color) img.color = color;
        }

        private static Color A(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);

        /// <summary>Sync every frame from the router state (screen px, bottom-left) and the controlled character.</summary>
        public void Sync(TouchControlRouter router, CharacterStateMachine c, TouchControlsData d)
        {
            if (router == null || d == null) { SetVisible(false); return; }
            var L = router.Layout;
            if (!L.Valid) { SetVisible(false); return; }
            SetVisible(true);
            float op = d.opacity;

            // ---- move stick: floating (shown where the thumb is), faint hint at its rest spot when idle ----
            bool mv = router.MoveActive;
            Vector2 mOrigin = mv ? router.MoveOrigin : L.MoveRest;
            Vector2 mKnob = mv ? router.MoveKnob : L.MoveRest;
            float mA = (mv ? 1f : d.idleMoveHintAlpha) * op;
            Place(_move.Base, mOrigin, L.MoveRadius, A(d.baseColor, mA));
            Place(_move.Ring, mOrigin, L.MoveRadius, A(d.ringColor, mA));
            Place(_move.Knob, mKnob, L.MoveKnobRadius, A(d.knobColor, mA));
            Place(_move.Icon, mKnob, L.MoveKnobRadius * 0.55f, A(new Color(0.25f, 0.2f, 0.16f, 0.8f), mA), !mv);

            // ---- attack stick ----
            bool atk = router.AttackActive;
            bool charging = c != null && c.IsCharging;
            Color atkCol = charging ? d.chargeLineColor : d.attackColor;
            Vector2 knob = L.Attack + router.AttackDelta;
            Place(_attack.Base, L.Attack, L.AttackRadius, A(d.baseColor, op * (atk ? 1.15f : 1f)));
            Place(_attack.Ring, L.Attack, L.AttackRadius, A(router.AttackAiming ? atkCol : d.ringColor, op));
            Place(_attack.Knob, knob, L.AttackKnobRadius * (atk ? 1.06f : 1f), A(atkCol, op));
            // fire-interval ring + hammer-cock kick (revolver / lever action): one shot, then the hammer comes back
            float cd = router.ShotCooldown01;
            var wpn = c != null && c.Data != null ? c.Data.weapon : null;
            bool cock = wpn != null && wpn.cockAnimation && cd > 0f;
            float kick = cock ? Mathf.Sin((1f - cd) * Mathf.PI) : 0f;
            Place(_attack.Icon, knob, L.AttackKnobRadius * 0.7f * (1f + 0.12f * kick), A(cd > 0f ? new Color(1f, 0.9f, 0.75f) : Color.white, op));
            _attack.Icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, cock ? 28f * kick : 0f);
            Place(_attack.Cooldown, L.Attack, L.AttackRadius * 1.08f, A(new Color(1f, 0.95f, 0.8f, 0.95f), op), cd > 0.001f);
            if (cd > 0.001f) _attack.Cooldown.fillAmount = 1f - cd;

            // ammo gauge (03: 총알 게이지 칸) as pips on an arc under the attack stick; the recharging cell fills up
            var gauge = c != null ? c.AmmoGauge : null;
            int cells = gauge != null ? Mathf.Min(MaxAmmoPips, gauge.MaxCells) : 0;
            float apr = Mathf.Max(6f, L.AttackRadius * 0.085f);
            for (int i = 0; i < MaxAmmoPips; i++)
            {
                bool show = i < cells;
                if (!show) { Place(_attack.Pips[i], Vector2.zero, 1f, Color.clear, false); continue; }
                float span = 70f;                                      // degrees covered by the pips
                float ang = (cells == 1 ? 0f : Mathf.Lerp(span * 0.5f, -span * 0.5f, i / (float)(cells - 1))) + 90f + 180f;
                Vector2 pp = L.Attack + new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * (L.AttackRadius + apr * 2.2f);
                float fill = gauge.GetCellFillRatio(i);
                Color pc = fill >= 0.999f ? new Color(1f, 0.86f, 0.35f, 1f) : Color.Lerp(new Color(0.18f, 0.14f, 0.1f, 0.85f), new Color(1f, 0.7f, 0.3f, 0.9f), fill * 0.8f);
                Place(_attack.Pips[i], pp, apr * (fill >= 0.999f ? 1f : 0.8f + 0.2f * fill), A(pc, op));
            }

            // ---- roll / skill / grenade (cooldown ring = remaining time, counter-clockwise from the top) ----
            float rollCd = c != null ? c.RollCooldownRatio : 0f;
            SyncButton(_roll, L.Roll, L.RollRadius, d.rollColor, rollCd, router.RollPressed, d, op, Vector2.zero, false);

            bool hasSkill = c != null && c.Data != null && c.Data.skill != null;
            float skillCd = hasSkill ? c.SkillCooldownRatio : 1f;
            bool skillReady = hasSkill && skillCd <= 0f;
            Color skillCol = d.skillColor;
            if (skillReady) skillCol = Color.Lerp(skillCol, Color.white, 0.18f + 0.18f * Mathf.Sin(Time.unscaledTime * 6f));
            Vector2 skDrag = router.SkillAiming ? Vector2.ClampMagnitude(router.SkillTarget - (c != null ? c.Position : Vector2.zero), 1f) * L.SkillRadius : Vector2.zero;
            SyncButton(_skill, L.Skill, L.SkillRadius, skillCol, skillCd, router.SkillActive, d, op, skDrag, router.SkillAiming);

            int gCount = c != null && c.GrenadeSystem != null ? c.GrenadeSystem.CurrentGrenades : 0;
            int gMax = c != null && c.GrenadeSystem != null ? c.GrenadeSystem.MaxGrenades : 0;
            float gCd = gCount > 0 ? 0f : 1f - Mathf.Clamp01(c != null && c.GrenadeSystem != null ? c.GrenadeSystem.RechargeProgress : 0f);
            Vector2 grDrag = router.GrenadeAiming ? router.GrenadeDelta / Mathf.Max(1f, L.GrenadeDragFullPx) * L.GrenadeRadius : Vector2.zero;
            SyncButton(_grenade, L.Grenade, L.GrenadeRadius, d.grenadeColor, gCd, router.GrenadeActive, d, op, grDrag, router.GrenadeAiming);
            for (int i = 0; i < _grenade.Pips.Length; i++)
            {
                bool show = i < gMax;
                float pr = L.GrenadeRadius * 0.16f;
                float span = (gMax - 1) * pr * 2.6f;
                Vector2 pp = L.Grenade + new Vector2(-span * 0.5f + i * pr * 2.6f, -L.GrenadeRadius * 0.98f);
                Place(_grenade.Pips[i], pp, pr, i < gCount ? A(new Color(1f, 0.85f, 0.35f, 1f), op) : A(new Color(0.15f, 0.12f, 0.1f, 0.8f), op), show);
            }
        }

        private static void SyncButton(Button b, Vector2 pos, float r, Color col, float cooldown, bool pressed,
                                       TouchControlsData d, float op, Vector2 drag, bool dragging)
        {
            float k = pressed ? 0.94f : 1f;
            float dim = cooldown > 0f ? 0.55f : 1f;
            Place(b.Bg, pos, r * k, A(new Color(col.r * dim, col.g * dim, col.b * dim, col.a), op));
            Place(b.Icon, pos + drag * 0.35f, r * 0.62f * k, A(Color.white, op * (cooldown > 0f ? 0.6f : 1f)));
            bool cd = cooldown > 0.001f;
            Place(b.Cooldown, pos, r * k, A(d.cooldownColor, op), cd);
            if (cd) b.Cooldown.fillAmount = Mathf.Clamp01(cooldown);
            Place(b.Ring, pos, r * k * 1.04f, A(pressed ? Color.white : d.ringColor, op));
            Place(b.Knob, pos + drag, r * 0.42f, A(d.knobColor, op), dragging);
        }
    }
}

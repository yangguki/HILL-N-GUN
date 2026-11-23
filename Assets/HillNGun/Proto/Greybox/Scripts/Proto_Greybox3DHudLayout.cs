using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>
    /// 3D-view HUD layout in the same 1280x720 percent space as <see cref="Proto_GreyboxHudBuilder"/>.
    /// Coordinates come from the confirmed concept (1920x1080, y from the top). Enabled only while
    /// the 3D HUD skin is applied so 2D hit tests stay unchanged.
    /// </summary>
    public static class Proto_Greybox3DHudLayout
    {
        public const float ConceptW = 1920f;
        public const float ConceptH = 1080f;

        public static bool Enabled;

        public static float Cx(float conceptX) => conceptX / ConceptW * 100f;
        public static float Cy(float conceptYFromTop) => conceptYFromTop / ConceptH * 100f;
        public static float RadiusFromDiameter(float conceptDiameter) =>
            conceptDiameter * 0.5f / ConceptH * 100f;

        // Joystick centre (225,868) base 260 / knob 116.
        public const float JoyX = 225f / ConceptW * 100f;
        public const float JoyY = 868f / ConceptH * 100f;
        public const float JoyBaseRadius = 260f * 0.5f / ConceptH * 100f;
        public const float JoyKnobRadius = 116f * 0.5f / ConceptH * 100f;
        public const float JoyHitPad = 2.5f;

        // Aim (1597,816) 150 — no current aim-button function; hidden, not hittable.
        public const float AimX = 1597f / ConceptW * 100f;
        public const float AimY = 816f / ConceptH * 100f;
        public const float AimRadius = 150f * 0.5f / ConceptH * 100f;

        // Attack (1796,816) 150 → fire.
        public const float AttackX = 1796f / ConceptW * 100f;
        public const float AttackY = 816f / ConceptH * 100f;
        public const float AttackRadius = 150f * 0.5f / ConceptH * 100f;

        // Skill (1597,975) 128 → grenade (bomb).
        public const float SkillX = 1597f / ConceptW * 100f;
        public const float SkillY = 975f / ConceptH * 100f;
        public const float SkillRadius = 128f * 0.5f / ConceptH * 100f;

        // Reload (1796,975) 128.
        public const float ReloadX = 1796f / ConceptW * 100f;
        public const float ReloadY = 975f / ConceptH * 100f;
        public const float ReloadRadius = 128f * 0.5f / ConceptH * 100f;

        // Roll stays (existing control, no concept button). Left of skill.
        public const float RollX = 1450f / ConceptW * 100f;
        public const float RollY = 975f / ConceptH * 100f;
        public const float RollRadius = 110f * 0.5f / ConceptH * 100f;

        // Pause (1834,66) 88.
        public const float PauseX = 1834f / ConceptW * 100f;
        public const float PauseY = 66f / ConceptH * 100f;
        public const float PauseRadius = 88f * 0.5f / ConceptH * 100f;

        // HP panel centre (961,1009) 536x44, track 340x24.
        public const float HpCenterX = 961f / ConceptW * 100f;
        public const float HpCenterY = 1009f / ConceptH * 100f;
        public const float HpPanelW = 536f / ConceptW * 100f;
        public const float HpPanelH = 44f / ConceptH * 100f;

        // SCORE (127,58), STAGE (960,60).
        public const float ScoreX = 127f / ConceptW * 100f;
        public const float ScoreY = 58f / ConceptH * 100f;
        public const float StageX = 960f / ConceptW * 100f;
        public const float StageY = 60f / ConceptH * 100f;

        public static bool CircleHit(float xPct, float yPct, float cx, float cy, float radiusPct)
        {
            return Proto_GreyboxHudBuilder.CircleHitTest(xPct, yPct, cx, cy, radiusPct, 10f);
        }

        public static bool JoystickHit(float xPct, float yPct)
        {
            return xPct >= JoyX - JoyBaseRadius - JoyHitPad && xPct <= JoyX + JoyBaseRadius + JoyHitPad
                   && yPct >= JoyY - JoyBaseRadius - JoyHitPad && yPct <= JoyY + JoyBaseRadius + JoyHitPad;
        }

        public static bool PauseHit(float xPct, float yPct) =>
            CircleHit(xPct, yPct, PauseX, PauseY, PauseRadius);
    }
}

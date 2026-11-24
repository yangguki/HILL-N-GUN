using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Confirmed concept HUD percents for 2D 원근 only (1920×1080 reference).</summary>
    public static class Proto_GreyboxPersp2DHudLayout
    {
        public const float ConceptW = 1920f;
        public const float ConceptH = 1080f;

        public static float Cx(float conceptX) => conceptX / ConceptW * 100f;
        public static float Cy(float conceptYFromTop) => conceptYFromTop / ConceptH * 100f;

        // Joystick centre ≈ (10 %, 80 %), base ≈ 25 % screen height.
        public const float JoyX = 10f;
        public const float JoyY = 80f;
        public const float JoyBaseRadius = 12.5f;
        public const float JoyKnobRadius = 5.5f;
        public const float JoyHitPad = 2.5f;

        // One row: ATTACK, ROLL, SKILL, RELOAD left→right at y ≈ 84 %, diameter ≈ 14 %.
        public const float BtnRowY = 84f;
        public const float BtnDiameterPercent = 14f;
        public const float BtnRadius = BtnDiameterPercent * 0.5f;
        public const float AttackX = 68f;
        public const float RollX = 76f;
        public const float SkillX = 85f;
        public const float ReloadX = 94f;
        public const float LabelRowY = 93f;
        public const float LabelHeightPercent = 3.2f;
        public const float LabelWidthPercent = 11f;

        // Pause (unchanged vs slice-I concept).
        public const float PauseX = 1834f / ConceptW * 100f;
        public const float PauseY = 66f / ConceptH * 100f;
        public const float PauseRadius = 88f * 0.5f / ConceptH * 100f;

        public const float HpCenterX = 50f;
        public const float HpCenterY = 92f;

        public static bool CircleHit(float xPct, float yPct, float cx, float cy, float radiusPct) =>
            Proto_GreyboxHudBuilder.CircleHitTest(xPct, yPct, cx, cy, radiusPct, 10f);

        public static bool JoystickHit(float xPct, float yPct) =>
            xPct >= JoyX - JoyBaseRadius - JoyHitPad && xPct <= JoyX + JoyBaseRadius + JoyHitPad
            && yPct >= JoyY - JoyBaseRadius - JoyHitPad && yPct <= JoyY + JoyBaseRadius + JoyHitPad;

        public static bool PauseHit(float xPct, float yPct) =>
            CircleHit(xPct, yPct, PauseX, PauseY, PauseRadius);
    }
}
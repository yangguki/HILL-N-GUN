using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>
    /// UI theme tokens (05 §5.1 "색·폰트는 토큰으로"). Every new menu screen reads colors / font sizes from here,
    /// so swapping this asset re-skins the menus. Final 9-slice art is external (Stub-external).
    /// </summary>
    [CreateAssetMenu(fileName = "ui_theme_default", menuName = "HillNGun/UI Theme")]
    public class UIThemeData : ScriptableObject
    {
        [Header("Colors")]
        public Color background = new Color(0.16f, 0.12f, 0.09f);
        public Color backgroundStripe = new Color(0.20f, 0.15f, 0.11f);
        public Color panel = new Color(0.25f, 0.19f, 0.14f, 0.97f);
        public Color panelDark = new Color(0.18f, 0.14f, 0.10f, 0.97f);
        public Color panelBorder = new Color(0.58f, 0.44f, 0.29f);
        public Color accent = new Color(1f, 0.82f, 0.35f);
        public Color text = new Color(0.96f, 0.91f, 0.83f);
        public Color textDim = new Color(0.74f, 0.66f, 0.56f);
        public Color good = new Color(0.55f, 0.92f, 0.55f);
        public Color bad = new Color(1f, 0.48f, 0.42f);
        public Color button = new Color(0.45f, 0.32f, 0.20f);
        public Color buttonHover = new Color(0.62f, 0.46f, 0.30f);
        public Color buttonDisabled = new Color(0.30f, 0.27f, 0.24f);
        public Color buttonPrimary = new Color(0.72f, 0.42f, 0.16f);
        public Color tab = new Color(0.30f, 0.23f, 0.16f);
        public Color tabActive = new Color(0.66f, 0.47f, 0.25f);
        public Color locked = new Color(0.36f, 0.33f, 0.31f);
        public Color stubBadge = new Color(0.42f, 0.48f, 0.66f);
        public Color overlay = new Color(0f, 0f, 0f, 0.6f);

        [Header("Font sizes (at reference height)")]
        public int fontTitle = 40;
        public int fontHeader = 26;
        public int fontBody = 20;
        public int fontSmall = 16;
        public int fontButton = 21;

        [Header("Layout")]
        [Tooltip("Menus are laid out for this screen height and scaled to the real one (phones / big monitors).")]
        public float referenceHeight = 720f;
        [Tooltip("Mobile only: render resolution height (0 = native). Keeps IMGUI readable and helps low-end devices (02 §7).")]
        public int mobileRenderHeight = 720;
    }
}

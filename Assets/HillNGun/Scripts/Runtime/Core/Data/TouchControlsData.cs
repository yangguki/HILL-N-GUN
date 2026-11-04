using UnityEngine;

namespace HillNGun.Data
{
    /// <summary>
    /// On-screen touch controls (Brawl-Stars-style feel): floating move stick on the left half, a large attack stick
    /// bottom-right with roll / skill / grenade on an arc around it. All sizes are reference pixels at a 1080 px
    /// short side; TouchControlLayout scales them to the device (smaller on tablets so they stay under the thumb).
    /// </summary>
    [CreateAssetMenu(fileName = "TouchControlsData", menuName = "HillNGun/Data/TouchControls")]
    public class TouchControlsData : ScriptableObject
    {
        [Header("Scale (reference px at referenceShortSide)")]
        public float referenceShortSide = 1080f;
        [Tooltip("Aspect (long/short) at or below this is treated as a tablet")]
        public float tabletAspectMax = 1.62f;
        [Tooltip("Extra scale on tablets: the screen is physically bigger, so controls take a smaller share of it")]
        public float tabletScale = 0.82f;
        public float minScale = 0.55f;
        public float maxScale = 2.4f;

        [Header("Safe area / HUD")]
        [Tooltip("Inset from the device safe area (ref px)")]
        public float safeAreaPadding = 24f;
        [Tooltip("Top band kept free for the HUD (gauge, wave, timer, pause) (ref px)")]
        public float topReserved = 170f;
        [Tooltip("Minimum gap between a control and a HUD element (ref px)")]
        public float hudGap = 12f;

        [Header("Move stick (floating, spawns where the left thumb lands)")]
        [Range(0.3f, 0.6f)] public float moveZoneWidth = 0.5f;
        public float moveRadius = 120f;
        public float moveKnobRadius = 50f;
        [Range(0.02f, 0.4f)] public float moveDeadzone = 0.12f;
        [Tooltip("Base follows the thumb when it is dragged past the radius")]
        public bool moveStickFollow = true;
        [Tooltip("Idle hint position from the safe-area bottom-left (ref px)")]
        public Vector2 moveRestOffset = new Vector2(250f, 230f);

        [Header("Attack stick (bottom-right)")]
        [Tooltip("Centre offset from the safe-area bottom-right corner (ref px)")]
        public Vector2 attackOffset = new Vector2(250f, 230f);
        public float attackRadius = 118f;
        public float attackKnobRadius = 54f;
        [Tooltip("Touch-down hit radius = attackRadius x this")]
        public float attackHitScale = 1.35f;
        [Tooltip("Aim deadzone as a ratio of attackRadius, measured from the touch-down point")]
        [Range(0.05f, 0.5f)] public float attackDeadzone = 0.2f;
        [Tooltip("Short tap inside the deadzone auto-aims at the nearest enemy and fires (user request 2026-11-01; design 01 said no auto-aim, so it is a toggle)")]
        public bool tapAutoAim = true;
        public float tapMaxTime = 0.45f;
        [Tooltip("Auto-aim prefers enemies within weapon range x this, else the nearest one")]
        public float autoAimRangeMul = 1.6f;
        [Tooltip("A release that comes before the weapon's fireInterval is queued this long instead of dropped")]
        public float fireQueueWindow = 0.3f;

        [Header("Arc buttons around the attack stick (angles: 0 = right, 90 = up)")]
        public float arcRadius = 215f;
        public float rollAngle = 150f;
        public float skillAngle = 106f;
        public float grenadeAngle = 194f;
        public float rollRadius = 60f;
        public float skillRadius = 66f;
        public float grenadeRadius = 62f;
        public float buttonHitScale = 1.25f;

        [Header("Grenade / drag skill aim (drag from the button)")]
        [Tooltip("Drag below this (ref px) and release = cancel (01 state table)")]
        public float grenadeDeadzonePx = 26f;
        [Tooltip("Drag length (ref px) that reaches GrenadeData.maxRange")]
        public float grenadeDragFull = 240f;
        [Tooltip("Drag length (ref px) that reaches SkillData.targetRange")]
        public float skillDragFull = 240f;

        [Header("Row change from the move stick (01 §3: up/down 0.6 for 0.15 s)")]
        public float rowChangeThreshold = 0.6f;
        public float rowChangeHoldTime = 0.15f;

        [Header("Look")]
        [Range(0.3f, 1f)] public float opacity = 0.9f;
        [Range(0f, 1f)] public float idleMoveHintAlpha = 0.35f;
        public Color baseColor = new Color(0.12f, 0.09f, 0.07f, 0.42f);
        public Color ringColor = new Color(1f, 0.93f, 0.8f, 0.75f);
        public Color knobColor = new Color(1f, 0.96f, 0.88f, 0.95f);
        public Color attackColor = new Color(0.95f, 0.42f, 0.22f, 0.92f);
        public Color rollColor = new Color(0.36f, 0.66f, 0.36f, 0.92f);
        public Color skillColor = new Color(0.98f, 0.76f, 0.2f, 0.95f);
        public Color grenadeColor = new Color(0.86f, 0.28f, 0.2f, 0.92f);
        public Color cooldownColor = new Color(0f, 0f, 0f, 0.55f);
        public Color aimLineColor = new Color(1f, 0.97f, 0.85f, 0.55f);
        public Color chargeLineColor = new Color(1f, 0.85f, 0.25f, 0.7f);
        public Color grenadeAreaColor = new Color(1f, 0.45f, 0.18f, 0.32f);
        [Tooltip("World width of the aim strip (tiles at the near row)")]
        public float aimLineWidth = 0.42f;
    }
}

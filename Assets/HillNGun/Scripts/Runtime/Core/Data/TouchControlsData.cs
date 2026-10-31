using UnityEngine;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "TouchControlsData", menuName = "HillNGun/Data/TouchControls")]
    public class TouchControlsData : ScriptableObject
    {
        [Header("Move Joystick")]
        [Tooltip("Position relative to safe area (0-1)")]
        public Vector2 moveJoystickAnchor = new Vector2(0.15f, 0.25f);
        public float moveJoystickSize = 150f;
        public float moveKnobSize = 60f;
        public float moveDeadzone = 0.1f;

        [Header("Aim/Fire Joystick")]
        public Vector2 aimJoystickAnchor = new Vector2(0.85f, 0.25f);
        public float aimJoystickSize = 150f;
        public float aimKnobSize = 60f;
        public float aimDeadzone = 0.15f;

        [Header("Roll Button")]
        public Vector2 rollButtonAnchor = new Vector2(0.25f, 0.55f);
        public float rollButtonSize = 70f;

        [Header("Grenade Button")]
        public Vector2 grenadeButtonAnchor = new Vector2(0.35f, 0.45f);
        public float grenadeButtonSize = 70f;

        [Header("Skill Button")]
        public Vector2 skillButtonAnchor = new Vector2(0.75f, 0.55f);
        public float skillButtonSize = 80f;

        [Header("Pause Button")]
        public Vector2 pauseButtonAnchor = new Vector2(0.5f, 0.95f);
        public float pauseButtonSize = 50f;

        [Header("Appearance")]
        [Range(0.3f, 1f)]
        public float opacity = 0.6f;
        public Color joystickBgColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        public Color joystickKnobColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        public Color buttonColor = new Color(0.4f, 0.35f, 0.3f, 0.7f);
        public Color buttonActiveColor = new Color(0.6f, 0.5f, 0.4f, 0.9f);
        public Color buttonCooldownColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("Safe Area")]
        public float safeAreaPadding = 20f;

        public Rect GetControlRect(Vector2 anchor, float size, Rect safeArea)
        {
            float x = safeArea.x + anchor.x * safeArea.width - size / 2f;
            float y = safeArea.y + (1f - anchor.y) * safeArea.height - size / 2f;
            return new Rect(x, y, size, size);
        }

        public Rect GetMoveJoystickRect(Rect safeArea)
        {
            return GetControlRect(moveJoystickAnchor, moveJoystickSize, safeArea);
        }

        public Rect GetAimJoystickRect(Rect safeArea)
        {
            return GetControlRect(aimJoystickAnchor, aimJoystickSize, safeArea);
        }

        public Rect GetRollButtonRect(Rect safeArea)
        {
            return GetControlRect(rollButtonAnchor, rollButtonSize, safeArea);
        }

        public Rect GetGrenadeButtonRect(Rect safeArea)
        {
            return GetControlRect(grenadeButtonAnchor, grenadeButtonSize, safeArea);
        }

        public Rect GetSkillButtonRect(Rect safeArea)
        {
            return GetControlRect(skillButtonAnchor, skillButtonSize, safeArea);
        }

        public Rect GetPauseButtonRect(Rect safeArea)
        {
            return GetControlRect(pauseButtonAnchor, pauseButtonSize, safeArea);
        }
    }
}

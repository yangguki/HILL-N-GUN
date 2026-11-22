using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public struct Proto_GreyboxInput
    {
        public Vector2 Move;
        public bool RollPressed;
        public bool FireQuickAuto;
        public bool FireStickHeld;
        public bool FireStickReleased;
        public Vector2 FireStickDragWorld;
        public bool MouseAimHeld;
        public bool MouseAimReleased;
        public Vector2 MouseAimWorld;
        public Vector2 MousePressWorld;
        public bool ReloadPressed;
        public bool GrenadePressed;
        public bool GrenadeStickHeld;
        public bool GrenadeStickReleased;
        public Vector2 GrenadeStickDragWorld;
        public bool DebugToggle;
        public bool RestartPressed;
    }

    public interface IProto_GreyboxInputSource
    {
        Proto_GreyboxInput Poll();
    }
}
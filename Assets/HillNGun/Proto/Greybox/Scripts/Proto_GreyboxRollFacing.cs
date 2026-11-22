using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public enum Proto_GreyboxRollFacing
    {
        Left,
        Right,
        Front,
        Back
    }

    public static class Proto_GreyboxRollFacingUtil
    {
        public static Proto_GreyboxRollFacing Classify(Vector2 dir)
        {
            if (dir.sqrMagnitude < 1e-8f)
                return Proto_GreyboxRollFacing.Right;
            dir.Normalize();
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                return dir.x >= 0f ? Proto_GreyboxRollFacing.Right : Proto_GreyboxRollFacing.Left;
            return dir.y >= 0f ? Proto_GreyboxRollFacing.Front : Proto_GreyboxRollFacing.Back;
        }
    }
}
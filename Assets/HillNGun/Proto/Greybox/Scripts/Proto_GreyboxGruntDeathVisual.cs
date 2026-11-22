namespace HillNGun.Proto.Greybox
{
    /// <summary>Grunt down-frame timing → hold → fade; used by west skin and EditMode tests.</summary>
    public static class Proto_GreyboxGruntDeathVisual
    {
        public const float Frame0End = 0.2f;
        public const float LastFrameTime = 0.45f;
        public const float HoldDuration = 0.4f;
        public const float FadeDuration = 0.3f;
        public const float TotalDuration = LastFrameTime + HoldDuration + FadeDuration;

        public static int FrameIndex(float elapsed) =>
            elapsed < Frame0End ? 0 : elapsed < LastFrameTime ? 1 : 2;

        public static float AlphaAt(float elapsed, bool alreadyFinished)
        {
            if (alreadyFinished)
                return 0f;
            if (elapsed < LastFrameTime + HoldDuration)
                return 1f;
            float fadeT = elapsed - LastFrameTime - HoldDuration;
            return UnityEngine.Mathf.Clamp01(1f - fadeT / FadeDuration);
        }

        public static bool IsFinished(float elapsed, bool alreadyFinished) =>
            alreadyFinished || elapsed >= TotalDuration;

        public static bool ViewShouldBeActive(float elapsed, bool alreadyFinished) =>
            !IsFinished(elapsed, alreadyFinished);
    }
}
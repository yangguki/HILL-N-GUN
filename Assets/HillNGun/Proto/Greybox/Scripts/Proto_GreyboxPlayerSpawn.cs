namespace HillNGun.Proto.Greybox
{
  public static class Proto_GreyboxPlayerSpawn
  {
    const float LegacyStartFootPercent = 88f;

    /// <summary>Test-only: force spawn foot world Y (balance sim A/B). Cleared after use.</summary>
    public static float? StartFootWorldYOverride;

    public static float PlayerStartFootPercentFromTop(Proto_GreyboxViewMode mode)
    {
      if (mode == Proto_GreyboxViewMode.View2DPerspective)
        return Proto_GreyboxPersp2DStageLayout.HeroStartFootPercent;
      return LegacyStartFootPercent;
    }

    public static float PlayerStartFootWorldY(Proto_GreyboxViewMode mode)
    {
      if (StartFootWorldYOverride.HasValue)
        return StartFootWorldYOverride.Value;
      return Proto_GreyboxLayout.ScreenYPercentToWorldY(PlayerStartFootPercentFromTop(mode));
    }

    public static float PlayerStartFootWorldX() => Proto_GreyboxLayout.ScreenXPercentToWorldX(49f);
  }
}
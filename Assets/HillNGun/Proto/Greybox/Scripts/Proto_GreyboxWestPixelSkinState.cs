namespace HillNGun.Proto.Greybox
{
    /// <summary>런타임 웨스턴 픽셀 스킨 ON/OFF. 기본 ON(플레이/APK). EditMode <see cref="Proto_GreyboxStageTests"/> 는 Build() 절차물만 검사.</summary>
    public static class Proto_GreyboxWestPixelSkinState
    {
        public const bool DefaultEnabled = true;

        static bool s_enabled = DefaultEnabled;

        public static bool Enabled
        {
            get => s_enabled;
            set => s_enabled = value;
        }

        public static void ResetToDefault() => s_enabled = DefaultEnabled;
    }
}
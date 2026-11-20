namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxRuntimeTuning
    {
        public float MoveSpeed = Proto_GreyboxTuning.MoveSpeed;
        public float RollDistance = Proto_GreyboxTuning.RollDistance;
        public float RollDuration = Proto_GreyboxTuning.RollDuration;
        public float RollInvulnDuration = Proto_GreyboxTuning.RollInvulnDuration;
        public float RollCooldown = Proto_GreyboxTuning.RollCooldown;
        public float EnemyBulletSpeed = Proto_GreyboxTuning.EnemyBulletSpeed;
        public float EnemyBulletDiameter = Proto_GreyboxTuning.EnemyBulletDiameter;
        public float AllyBulletSpeed = Proto_GreyboxTuning.AllyBulletSpeed;
        public float AllyBulletDiameter = Proto_GreyboxTuning.AllyBulletDiameter;
        public int AllyBulletDamage = Proto_GreyboxTuning.AllyBulletDamage;
        public float FireInterval = Proto_GreyboxTuning.FireInterval;
        public int MagazineSize = Proto_GreyboxTuning.MagazineSize;
        public float ReloadDuration = Proto_GreyboxTuning.ReloadDuration;
        public int Wave1Count = Proto_GreyboxTuning.Wave1EnemyCount;
        public int Wave2Count = Proto_GreyboxTuning.Wave2EnemyCount;
        public int Wave3Count = Proto_GreyboxTuning.Wave3EnemyCount;
        public float EnemyFireInterval = Proto_GreyboxTuning.EnemyFireInterval;
        public int EnemyHp = Proto_GreyboxTuning.TargetHp;
        public int PlayerMaxHp = Proto_GreyboxTuning.PlayerMaxHp;
        public float GrenadeCooldown = Proto_GreyboxTuning.GrenadeCooldown;
        public float CoverDamageReductionPercent = Proto_GreyboxTuning.CoverDamageReduction * 100f;
        public float DepthFarScale = Proto_GreyboxTuning.DepthFarScale;
        public float DepthNearScale = Proto_GreyboxTuning.DepthNearScale;

        public float CoverDamageReduction01 => CoverDamageReductionPercent / 100f;

        public int WaveEnemyCount(int wave) => wave switch
        {
            1 => Wave1Count,
            2 => Wave2Count,
            3 => Wave3Count,
            _ => 0
        };

        public void ApplyDepthScales()
        {
            Proto_DepthScale.FarScale = DepthFarScale;
            Proto_DepthScale.NearScale = DepthNearScale;
        }
    }
}
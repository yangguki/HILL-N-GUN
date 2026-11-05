using UnityEngine;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "chapter_", menuName = "HillNGun/Chapter Data")]
    public class ChapterData : ScriptableObject
    {
        public string chapterId = "dust_town";
        public int index = 1;
        public string displayNameKey = "region.dusty_town";
        public string bossId = "boss_rex";
        public int bountyReward = 1000;
        public string unlockAfterBossId;
        public string paletteId = "sand_saloon";
        public string[] signatureHazards = { "explosive_barrel", "saloon_sign", "tumbleweed" };
        public StageData[] stages;
    }
}

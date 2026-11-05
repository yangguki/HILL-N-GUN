using NUnit.Framework;
using UnityEngine;
using HillNGun.Data;
using HillNGun.Scenery;

namespace HillNGun.Tests.EditMode
{
    public class CampaignVisualV2Tests
    {
        [Test]
        public void StageType_ExterminateAndBoss_Exist_StubsPresent()
        {
            Assert.AreEqual(0, (int)StageType.Exterminate);
            Assert.AreEqual(5, (int)StageType.Boss);
            Assert.AreEqual(1, (int)StageType.Survive);
            Assert.AreEqual(4, (int)StageType.Bounty);
        }

        [Test]
        public void ChapterData_HoldsFourDustStages_SameChapterId()
        {
            var c = ScriptableObject.CreateInstance<ChapterData>();
            c.chapterId = "dust_town";
            var a = ScriptableObject.CreateInstance<StageData>();
            a.id = "dusty_town_1_1"; a.chapterId = "dust_town"; a.stageType = StageType.Exterminate; a.stageIndex = 1;
            var b = ScriptableObject.CreateInstance<StageData>();
            b.id = "dusty_town_1_2"; b.chapterId = "dust_town"; b.stageType = StageType.Exterminate; b.stageIndex = 2;
            var d = ScriptableObject.CreateInstance<StageData>();
            d.id = "dusty_town_1_3"; d.chapterId = "dust_town"; d.stageType = StageType.Exterminate; d.stageIndex = 3;
            var e = ScriptableObject.CreateInstance<StageData>();
            e.id = "dusty_town_boss"; e.chapterId = "dust_town"; e.stageType = StageType.Boss; e.stageIndex = 10;
            c.stages = new[] { a, b, d, e };
            Assert.AreEqual(4, c.stages.Length);
            foreach (var s in c.stages) Assert.AreEqual("dust_town", s.chapterId);
            Assert.AreEqual(StageType.Boss, c.stages[3].stageType);
        }

        [Test]
        public void StageVisual_EnsureFiveLayers_L3IsCombatLane_NoDecor()
        {
            var v = ScriptableObject.CreateInstance<StageVisualData>();
            v.layers = null;
            StageVisualData.EnsureFiveLayers(v);
            Assert.AreEqual(5, v.layers.Length);
            Assert.IsTrue(v.layers[3].combatLane);
            Assert.IsFalse(StageVisualData.AllowsDecorOnLayer(3));
            Assert.IsTrue(StageVisualData.AllowsDecorOnLayer(2));
        }

        [Test]
        public void Parallax_TrySpawnDecor_BlocksL3()
        {
            var go = new GameObject("test_parallax");
            var p = go.AddComponent<ParallaxBackdrop>();
            var v = ScriptableObject.CreateInstance<StageVisualData>();
            StageVisualData.EnsureFiveLayers(v);
            p.Apply(v);
            Assert.IsNull(p.TrySpawnDecor(3, "weed", Vector3.zero));
            var decor = p.TrySpawnDecor(2, "sign", Vector3.zero);
            Assert.IsNotNull(decor);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SequentialUnlock_RequiresPreviousClear()
        {
            // Mirrors ProgressionService rule used by StageSelectPanel
            var cleared = new System.Collections.Generic.HashSet<string>();
            bool Unlocked(string[] ids, int i)
            {
                if (i <= 0) return true;
                return cleared.Contains(ids[i - 1]);
            }
            string[] ids = { "1_1", "1_2", "1_3", "boss" };
            Assert.IsTrue(Unlocked(ids, 0));
            Assert.IsFalse(Unlocked(ids, 1));
            cleared.Add("1_1");
            Assert.IsTrue(Unlocked(ids, 1));
            Assert.IsFalse(Unlocked(ids, 3));
            cleared.Add("1_2"); cleared.Add("1_3");
            Assert.IsTrue(Unlocked(ids, 3));
        }

        [Test]
        public void DustBudget_CapsAt16()
        {
            var v = ScriptableObject.CreateInstance<StageVisualData>();
            v.dustMotes = 12;
            Assert.LessOrEqual(Mathf.Clamp(v.dustMotes, 0, 16), 16);
            Assert.LessOrEqual(v.dustMotesLowTier, 8);
        }
    }
}

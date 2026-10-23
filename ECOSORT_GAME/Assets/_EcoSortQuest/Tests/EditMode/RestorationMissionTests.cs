using NUnit.Framework;
using UnityEngine;

namespace EcoSortQuest.Tests
{
    public sealed class RestorationMissionTests
    {
        [Test]
        public void RewardsRequireActualCleanupAndStopAtTwelve()
        {
            var mission=new RestorationMission();
            Assert.AreEqual(0,mission.Stars(12));
            for(int i=0;i<3;i++)mission.CleanOne();
            Assert.AreEqual(0,mission.Stage);
            mission.CleanOne();Assert.AreEqual(1,mission.Stage);
            for(int i=0;i<4;i++)mission.CleanOne();
            Assert.AreEqual(2,mission.Stage);
            for(int i=0;i<4;i++)mission.CleanOne();
            Assert.IsTrue(mission.Complete);Assert.AreEqual(3,mission.Stage);
            Assert.IsFalse(mission.CleanOne());Assert.AreEqual(12,mission.Cleaned);
            Assert.AreEqual(3,mission.Stars(12));Assert.AreEqual(2,mission.Stars(9));Assert.AreEqual(1,mission.Stars(8));
        }
        [Test]
        public void PreparationDecisionUsesConditionRatherThanMaterialAlone()
        {
            var mission=new RestorationMission();
            Assert.IsFalse(mission.Prepare("water-bottle",PreparationAction.Ready));
            Assert.IsTrue(mission.Prepare("water-bottle",PreparationAction.EmptyContents));
            Assert.IsTrue(mission.Prepare("cardboard",PreparationAction.FlattenDryPaper));
            Assert.IsTrue(mission.Prepare("apple-core",PreparationAction.Ready));
            Assert.IsFalse(mission.Prepare("paper-bag",PreparationAction.EmptyContents));
            Assert.AreEqual(2,mission.PreparationMistakes);
            Assert.AreEqual(0,mission.Cleaned,"Preparation must not grant cleanup progress.");
            for(int i=0;i<12;i++)mission.CleanOne();
            Assert.AreEqual(2,mission.Stars(12),"A perfect material score alone does not award the preparation star.");
        }
        [Test]
        public void SavedRewardIsIdempotentAndKeepsBestResultPerContext()
        {
            const string player="test-restoration-save";
            PlayerPrefs.DeleteKey(RestorationSave.Key(player,LearningContext.Park));
            try
            {
                RestorationSave.Complete(player,LearningContext.Park,"first",3);
                var duplicate=RestorationSave.Complete(player,LearningContext.Park,"first",3);
                Assert.AreEqual(1,duplicate.completions);
                RestorationSave.Complete(player,LearningContext.Park,"second",1);
                var record=RestorationSave.Load(player,LearningContext.Park);
                Assert.AreEqual(3,record.bestStars);Assert.AreEqual(2,record.completions);
                Assert.AreEqual(0,RestorationSave.Load(player,LearningContext.Home).bestStars);
            }
            finally { PlayerPrefs.DeleteKey(RestorationSave.Key(player,LearningContext.Park));PlayerPrefs.Save(); }
        }
    }
}

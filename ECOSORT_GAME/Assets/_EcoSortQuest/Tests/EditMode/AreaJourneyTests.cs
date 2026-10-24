using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace EcoSortQuest.Tests
{
    public sealed class AreaJourneyTests
    {
        string player;
        [SetUp] public void Setup(){player="test-areas-"+Guid.NewGuid().ToString("N");}
        [TearDown] public void Cleanup(){PlayerPrefs.DeleteKey(AreaJourneyStore.Key(player));PlayerPrefs.Save();}
        [Test] public void NineAuthoredMissionsAreValidContextualAndHaveDistinctLearningGoals()
        {
            Assert.AreEqual(9,AreaMissions.All.Length);Assert.AreEqual(9,AreaMissions.All.Select(m=>m.Id).Distinct().Count());
            foreach(LearningContext context in Enum.GetValues(typeof(LearningContext)))Assert.AreEqual(3,AreaMissions.All.Count(m=>m.Context==context));
            foreach(var m in AreaMissions.All)
            {
                Assert.That(m.Items.Length,Is.InRange(4,12));Assert.AreEqual(m.Items.Length,m.Items.Distinct().Count());
                Assert.IsTrue(m.Items.All(id=>WasteCatalog.All.Any(w=>w.Id==id && w.AppearsIn(m.Context))),m.Id);
                if(m.Separate) {Assert.Contains("plastic-cup",m.Items);Assert.Contains("paper-bag",m.Items);}
                if(m.Context==LearningContext.School){Assert.AreEqual(3,m.Options.Length);Assert.That(m.Answer,Is.InRange(0,2));Assert.IsNotEmpty(m.Explanation);}
            }
            Assert.IsTrue(AreaMissions.All.Where(m=>m.Context==LearningContext.Home).All(m=>m.Items.Length==4));
            Assert.IsTrue(AreaMissions.All.Where(m=>m.Context==LearningContext.Park).All(m=>m.Items.Length>=8));
        }
        [TestCase(4,3,4,0,0,0)] [TestCase(4,4,4,0,0,3)]
        [TestCase(4,4,3,0,0,2)] [TestCase(4,4,4,1,0,2)]
        [TestCase(4,4,4,0,1,2)] [TestCase(4,4,4,0,3,1)]
        [TestCase(12,12,11,0,0,3)] [TestCase(12,12,8,0,0,1)]
        [TestCase(4,4,5,0,0,0)] [TestCase(0,0,0,0,0,0)]
        public void StarsRewardCompletionAccuracyAndIndependentDecisions(int total,int cleaned,int correct,int mistakes,int hints,int stars)
        {Assert.AreEqual(stars,AreaJourneyStore.Stars(total,cleaned,correct,mistakes,hints));}
        [Test] public void CompletionUnlocksInOrderAndIsIdempotentAcrossReloadAndWorseReplay()
        {
            var initial=AreaJourneyStore.Load(player);Assert.IsTrue(AreaJourneyStore.Unlocked(initial,AreaMissions.All[0]));
            Assert.IsFalse(AreaJourneyStore.Unlocked(initial,AreaMissions.All[1]));
            Assert.IsNull(AreaJourneyStore.Complete(player,AreaMissions.All[1],"skip",4,4,0,0,Array.Empty<string>()));
            Assert.IsNull(AreaJourneyStore.Complete(player,AreaMissions.All[0],"partial",3,3,0,0,Array.Empty<string>()));
            for(int i=0;i<9;i++)
            {
                var mission=AreaMissions.All[i];int n=mission.Items.Length;
                var record=AreaJourneyStore.Complete(player,mission,"session"+i,n,n,0,0,new[]{"Plastic","Metal","Paper","Organic"});
                Assert.AreEqual(3,record.bestStars);
                record=AreaJourneyStore.Complete(player,mission,"session"+i,n,n,0,0,Array.Empty<string>());Assert.AreEqual(1,record.completions);
                Assert.IsTrue(AreaJourneyStore.Unlocked(AreaJourneyStore.Load(player),mission));
            }
            var data=AreaJourneyStore.Load(player);Assert.AreEqual(27,data.missions.Sum(m=>m.bestStars));
            Assert.Contains("Nhận biết bốn vật liệu",data.badges);Assert.Contains("Người giữ góc xanh",data.badges);
            var worse=AreaJourneyStore.Complete(player,AreaMissions.All[0],"worse",4,1,2,4,Array.Empty<string>());
            Assert.AreEqual(3,worse.bestStars);Assert.AreEqual(2,worse.completions);Assert.AreEqual(3,AreaJourneyStore.Decorations(AreaJourneyStore.Load(player),LearningContext.Home));
        }
        [Test] public void DiscoveryBadgesImprovementReviewAndCosmeticSettingsPersist()
        {
            AreaJourneyStore.Discover(player,"water-bottle");AreaJourneyStore.Discover(player,"water-bottle");AreaJourneyStore.Discover(player,"unknown");
            Assert.AreEqual(1,AreaJourneyStore.Load(player).discovered.Count);
            AreaJourneyStore.Complete(player,AreaMissions.All[0],"first",4,2,0,0,new[]{"Plastic"});
            AreaJourneyStore.Complete(player,AreaMissions.All[0],"improved",4,4,0,0,new[]{"Paper"});
            Assert.Contains("Tiến bộ từng ngày",AreaJourneyStore.Load(player).badges);
            AreaJourneyStore.RecordReview(player,new LearningSession{mode="Review",total=1,answers=new[]{new LearningAnswer{correct=false}}});
            Assert.IsFalse(AreaJourneyStore.Load(player).badges.Contains("Chăm chỉ ôn tập"));
            AreaJourneyStore.RecordReview(player,new LearningSession{mode="Review",total=1,answers=new[]{new LearningAnswer{correct=true}}});
            var data=AreaJourneyStore.Load(player);Assert.Contains("Chăm chỉ ôn tập",data.badges);
            data.plants=false;data.flowers=true;data.benches=false;AreaJourneyStore.Save(player,data);
            var loaded=AreaJourneyStore.Load(player);Assert.IsFalse(loaded.plants);Assert.IsTrue(loaded.flowers);Assert.IsFalse(loaded.benches);
        }
    }
}

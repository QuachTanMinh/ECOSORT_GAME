using System;
using System.Linq;
using NUnit.Framework;

namespace EcoSortQuest.Tests
{
    public sealed class LearningTests
    {
        [Test] public void ContextDecksAreDistinctDeterministicAndReviewTargetsMistakes()
        {
            var home=LearningDeck.Build(LearningContext.Home,10,42);
            Assert.AreEqual(10,home.Select(d=>d.Id).Distinct().Count());
            CollectionAssert.AreEqual(home.Select(d=>d.Id),LearningDeck.Build(LearningContext.Home,10,42).Select(d=>d.Id));
            foreach(LearningContext context in Enum.GetValues(typeof(LearningContext)))
            {
                var deck=LearningDeck.Build(context,12,42);
                Assert.IsTrue(deck.All(d=>d.AppearsIn(context)));
                Assert.AreEqual(4,deck.Select(d=>d.Type).Distinct().Count());
                foreach(WasteType type in Enum.GetValues(typeof(WasteType)))Assert.AreEqual(3,deck.Count(d=>d.Type==type));
            }
            Assert.IsFalse(WasteCatalog.All.Single(d=>d.Id=="detergent-bottle").AppearsIn(LearningContext.Park));
            Assert.IsFalse(WasteCatalog.All.Single(d=>d.Id=="worksheet").AppearsIn(LearningContext.Park));
            var review=LearningDeck.Build(LearningContext.Home,10,42,new System.Collections.Generic.List<WasteDefinition>{WasteCatalog.Get(WasteType.Metal,1)});
            Assert.IsTrue(review.All(d=>d.Id=="food-can"));
        }
        [Test] public void ReviewResolvesLatestMistakeAndSummaryExcludesGuidedLearning()
        {
            var h=new LearningHistory();
            h.sessions.Add(new LearningSession{mode="Learn",total=10,answers=new[]{new LearningAnswer{item_id="water-bottle",expected="Plastic",choice="Metal",correct=false}}});
            Assert.AreEqual(1,LearningStore.Mistakes(h).Count);
            h.sessions.Add(new LearningSession{mode="Review",total=10,answers=new[]{new LearningAnswer{item_id="water-bottle",expected="Plastic",choice="Plastic",correct=true}}});
            Assert.AreEqual(0,LearningStore.Mistakes(h).Count);
            StringAssert.Contains("0 lượt",LearningStore.Summary(h));
            h.sessions.Add(new LearningSession{mode="Test",total=10,answers=new[]{new LearningAnswer{expected="Metal",choice="Metal",correct=true}}});
            StringAssert.Contains("10%",LearningStore.Summary(h));
        }
        [Test] public void DuplicateSessionDoesNotDuplicateHistoryOrOutbox()
        {
            var h=new LearningHistory();var session=new LearningSession();
            LearningStore.Add(h,session);LearningStore.Add(h,session);
            Assert.AreEqual(1,h.sessions.Count);Assert.AreEqual(1,h.pending.Count);
        }
        [Test] public void InsightsCompareSameContextAndIncludeUnansweredItems()
        {
            var h=new LearningHistory();
            h.sessions.Add(new LearningSession{mode="Test",context="Home",total=10,answers=Enumerable.Repeat(new LearningAnswer{correct=true},5).ToArray()});
            h.sessions.Add(new LearningSession{mode="Test",context="Park",total=10,answers=Enumerable.Repeat(new LearningAnswer{correct=true},10).ToArray()});
            h.sessions.Add(new LearningSession{mode="Learn",context="Home",total=10,answers=Enumerable.Repeat(new LearningAnswer{correct=true},10).ToArray()});
            h.sessions.Add(new LearningSession{mode="Test",context="Home",total=10,answers=Enumerable.Repeat(new LearningAnswer{correct=true},7).ToArray()});
            StringAssert.Contains("+20 điểm %",LearningInsights.ContextProgress(h,LearningContext.Home));
            Assert.AreEqual(2,LearningInsights.Tests(h,LearningContext.Home).Length);
            Assert.AreEqual(0,LearningInsights.Rate(new LearningSession{total=0}));
            Assert.AreEqual(LearningMode.Test,LearningInsights.Recommend(h,LearningContext.Home).Mode);
        }
        [Test] public void RecentMistakesChooseCompatibleContextAndResolvedItemsDisappear()
        {
            var h=new LearningHistory();
            h.sessions.Add(new LearningSession{mode="Test",answers=new[]{
                new LearningAnswer{item_id="water-bottle",expected="Plastic",choice="Metal"},
                new LearningAnswer{item_id="detergent-bottle",expected="Plastic",choice="Paper"}}});
            Assert.AreEqual("detergent-bottle",LearningStore.Mistakes(h)[0].Id);
            var next=LearningInsights.Recommend(h,LearningContext.Park);
            Assert.AreEqual(LearningMode.Review,next.Mode);Assert.AreEqual(LearningContext.Home,next.Context);
            h.sessions.Add(new LearningSession{mode="Review",answers=new[]{new LearningAnswer{item_id="detergent-bottle",correct=true}}});
            Assert.AreEqual("water-bottle",LearningStore.Mistakes(h).Single().Id);
            StringAssert.Contains("→",LearningInsights.Confusions(h));
        }
    }
}

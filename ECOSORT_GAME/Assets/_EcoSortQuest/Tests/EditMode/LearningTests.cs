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
    }
}

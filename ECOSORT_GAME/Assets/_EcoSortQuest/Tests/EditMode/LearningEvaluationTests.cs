using System;
using NUnit.Framework;
namespace EcoSortQuest.Tests
{
    public class LearningEvaluationTests
    {
        LearningSession Session(string id,string date,string context="Home",string mode="Test")=>new LearningSession{
            session_id=id,created_at=date,context=context,mode=mode,total=10,answers=new[]{new LearningAnswer{item_id="water-bottle",expected="Plastic",choice="Plastic",correct=true,seconds=2}}};
        [Test]public void RequiresDifferentLaterTestsInSameContext()
        {
            var a=Session("a","2026-10-30T01:00:00Z");var b=Session("b","2026-10-30T02:00:00Z");
            Assert.IsTrue(LearningEvaluation.CanPair(a,b));Assert.IsFalse(LearningEvaluation.CanPair(a,a));
            Assert.IsFalse(LearningEvaluation.CanPair(b,a));b.context="School";Assert.IsFalse(LearningEvaluation.CanPair(a,b));
            b.context="Home";b.mode="Learn";Assert.IsFalse(LearningEvaluation.CanPair(a,b));
        }
        [Test]public void ExportIncludesUnansweredQuestionsAndFixedDenominator()
        {
            var pair=new EvaluationPair{before=Session("a","2026-10-30T01:00:00Z"),after=Session("b","2026-10-30T02:00:00Z")};
            string csv=LearningEvaluation.Csv(pair);StringAssert.Contains("10.00",csv);StringAssert.Contains("unanswered",csv);
            Assert.AreEqual(21,csv.Split(new[]{'\n'},StringSplitOptions.RemoveEmptyEntries).Length);
        }
    }
}

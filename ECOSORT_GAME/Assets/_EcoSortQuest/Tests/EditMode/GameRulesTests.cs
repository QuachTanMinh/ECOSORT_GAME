using NUnit.Framework;

namespace EcoSortQuest.Tests
{
    public sealed class GameRulesTests
    {
        [Test] public void WrongAnswerAtZeroCannotMakeScoreNegative()
        { var round = new SortingRound(); round.Submit(WasteType.Paper, WasteType.Metal); Assert.AreEqual(0, round.Score); }
        [Test] public void CorrectThenWrongGivesFivePoints()
        { var round = new SortingRound(); round.Submit(WasteType.Paper, WasteType.Paper); round.Submit(WasteType.Metal, WasteType.Plastic); Assert.AreEqual(5, round.Score); }
        [TestCase(6, false)] [TestCase(7, true)] [TestCase(10, true)]
        public void PassThresholdAndFinishAreEnforced(int correct, bool passed)
        {
            var round = new SortingRound();
            for (int i = 0; i < 10; i++) round.Submit(WasteType.Plastic, i < correct ? WasteType.Plastic : WasteType.Metal);
            Assert.IsTrue(round.Finished); Assert.AreEqual(passed, round.Passed);
            int score = round.Score; round.Submit(WasteType.Plastic, WasteType.Plastic);
            Assert.AreEqual(10, round.Answered); Assert.AreEqual(score, round.Score);
        }
        [Test] public void TimeoutDoesNotAwardUnansweredItems()
        { var round = new SortingRound(); round.Submit(WasteType.Paper, WasteType.Paper); round.Expire(); Assert.IsFalse(round.Passed); Assert.AreEqual(1, round.Answered); }
        [Test] public void CampaignUnlockRequiresPassingThirdLevel()
        {
            var progress = new PlayerProgress(); var round = new SortingRound();
            for (int i = 0; i < 10; i++) round.Submit(WasteType.Metal, WasteType.Metal);
            progress.RecordRound(1, round); Assert.AreEqual(2, progress.unlocked_level); Assert.IsFalse(progress.ChallengeUnlocked);
            progress.RecordRound(2, round); Assert.AreEqual(3, progress.unlocked_level); Assert.IsFalse(progress.ChallengeUnlocked);
            progress.RecordRound(3, round); Assert.IsTrue(progress.ChallengeUnlocked);
            Assert.IsFalse(progress.HasBadge); progress.ar_completed++; Assert.IsTrue(progress.HasBadge);
        }
        [Test] public void CloudMergeNeverRegressesLocalProgress()
        {
            var progress = new PlayerProgress { unlocked_level = 3, best_score = 90, campaign_completed = true };
            progress.Merge(new PlayerProgress { unlocked_level = 1, best_score = 10 });
            Assert.AreEqual(3, progress.unlocked_level); Assert.AreEqual(90, progress.best_score); Assert.IsTrue(progress.campaign_completed);
        }
        [TestCase("Plastic", .92f, true, true)] [TestCase("Paper", .5f, true, false)]
        [TestCase("Metal", .95f, false, false)] [TestCase("Unknown", .99f, true, false)]
        [TestCase("99", .99f, true, false)] [TestCase("0", .99f, true, false)] [TestCase("Plastic", 1.1f, true, false)]
        public void PredictionsMustBeAcceptedConfidentAndKnown(string label, float confidence, bool accepted, bool valid)
        { Assert.AreEqual(valid, new Prediction { class_name = label, confidence = confidence, accepted = accepted }.TryAccept(.75f, out _)); }
        [Test] public void NonFiniteConfidenceIsRejected()
        { Assert.IsFalse(new Prediction { class_name = "Paper", accepted = true, confidence = float.NaN }.TryAccept(.75f, out _)); }
    }
}

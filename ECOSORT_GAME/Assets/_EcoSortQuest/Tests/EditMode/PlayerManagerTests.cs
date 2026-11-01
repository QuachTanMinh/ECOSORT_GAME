using NUnit.Framework;
using UnityEngine;

namespace EcoSortQuest.Tests
{
    public class PlayerManagerTests
    {
        string original;bool had;
        const string Id="test-player-name",Key="EcoSortQuest.LocalProgress.v1";
        [SetUp]public void Setup(){had=PlayerPrefs.HasKey(Key);original=PlayerPrefs.GetString(Key);LocalProgress.Save(new PlayerProgress{player_id=Id,best_score=80});}
        [TearDown]public void Cleanup(){PlayerPrefs.DeleteKey(PlayerManager.NameKey(Id));if(had)PlayerPrefs.SetString(Key,original);else PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();}
        [Test]public void VietnameseNicknameKeepsStableIdAndProgress()
        {
            Assert.IsTrue(PlayerManager.TrySetDisplayName("  Bạn   Mây  ",out _));Assert.AreEqual("Bạn Mây",PlayerManager.DisplayName);
            Assert.IsTrue(PlayerManager.TrySetDisplayName("Minh",out _));Assert.AreEqual(Id,PlayerManager.CurrentPlayerId);
            Assert.AreEqual(80,LocalProgress.Load().best_score);
        }
        [TestCase("")][TestCase("  ")][TestCase("<b>Minh</b>")][TestCase("abcdefghijklmnopqrstuvwxyz123456789")]
        public void RejectsEmptyMarkupAndLongNames(string value)=>Assert.IsFalse(PlayerManager.TrySetDisplayName(value,out _));
    }
}

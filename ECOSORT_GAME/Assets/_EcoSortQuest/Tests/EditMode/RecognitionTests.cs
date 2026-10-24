using NUnit.Framework;

namespace EcoSortQuest.Tests
{
    public sealed class RecognitionTests
    {
        [TestCase("Plastic",true)] [TestCase("Metal",true)] [TestCase("Paper",true)] [TestCase("Organic",true)]
        [TestCase("Cardboard",false)] [TestCase("GreenGlass",false)] [TestCase("BrownGlass",false)]
        [TestCase("WhiteGlass",false)] [TestCase("Clothes",false)] [TestCase("Shoes",false)]
        [TestCase("Batteries",false)] [TestCase("Trash",false)]
        public void RecognizeTwelveLabelsWithoutForcingExtendedWasteIntoExistingBins(string name,bool playable)
        {
            var prediction=new Prediction{class_name=name,confidence=.9f,accepted=true};
            Assert.IsTrue(prediction.TryRecognize(.75f,out var category));
            Assert.IsNotEmpty(category.display_name); Assert.IsNotEmpty(category.guidance);
            Assert.AreEqual(playable,prediction.TryAccept(.75f,out _));
        }
        [TestCase("Unknown",.9f,true)] [TestCase("4",.9f,true)] [TestCase("Batteries",.74f,true)]
        [TestCase("Batteries",.9f,false)] [TestCase("Plastic",1.1f,true)]
        public void RefusedOrInvalidResponsesCannotStartTasks(string name,float confidence,bool accepted)
        {
            var prediction=new Prediction{class_name=name,confidence=confidence,accepted=accepted};
            Assert.IsFalse(prediction.TryRecognize(.75f,out _));
            Assert.IsFalse(prediction.TryAccept(.75f,out _));
        }
        [Test] public void NonFiniteConfidenceOrThresholdIsRefused()
        {
            var prediction=new Prediction{class_name="Paper",confidence=float.NaN,accepted=true};
            Assert.IsFalse(prediction.TryRecognize(.75f,out _));
            prediction.confidence=.9f;Assert.IsFalse(prediction.TryRecognize(float.NaN,out _));
        }
    }
}

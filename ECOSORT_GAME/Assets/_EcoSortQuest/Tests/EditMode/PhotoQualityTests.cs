using NUnit.Framework;
using UnityEngine;
namespace EcoSortQuest.Tests
{
    public class PhotoQualityTests
    {
        [TestCase(5,"tối")][TestCase(250,"sáng")][TestCase(120,"ít chi tiết")]
        public void GivesSpecificCaptureAdvice(byte value,string expected)
        {
            var pixels=new Color32[64];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(value,value,value,255);
            StringAssert.Contains(expected,PhotoQuality.Advice(pixels,8,8));
        }
        [Test]public void InvalidShapeDoesNotThrow()=>StringAssert.Contains("quá nhỏ",PhotoQuality.Advice(new Color32[1],4,4));
    }
}

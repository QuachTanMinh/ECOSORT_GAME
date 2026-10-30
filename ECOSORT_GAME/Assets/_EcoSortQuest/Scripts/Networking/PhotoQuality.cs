using UnityEngine;

namespace EcoSortQuest
{
    // Advisory measurements only: this does not detect objects or guarantee recognition accuracy.
    public static class PhotoQuality
    {
        public static string Advice(Color32[] pixels,int width,int height)
        {
            if(pixels==null || width<2 || height<2 || pixels.Length!=width*height)return "Ảnh quá nhỏ. Hãy chụp lại rõ hơn.";
            int step=Mathf.Max(1,Mathf.Max(width,height)/128),count=0;float sum=0,edges=0;
            for(int y=step;y<height;y+=step)for(int x=step;x<width;x+=step)
            {
                float current=Luma(pixels[y*width+x]);sum+=current;
                edges+=Mathf.Abs(current-Luma(pixels[y*width+x-step]));count++;
            }
            if(count==0)return "Ảnh quá nhỏ. Hãy chụp lại rõ hơn.";
            if(sum/count<35)return "Ảnh khá tối. Bật thêm đèn hoặc đưa vật ra nơi sáng hơn.";
            if(sum/count>225)return "Ảnh khá sáng. Tránh đèn chiếu thẳng và phản sáng trên vật.";
            if(edges/count<3.5f)return "Ảnh ít chi tiết. Giữ máy yên, lấy nét và đưa một vật vào gần hơn.";
            return "Đã kiểm tra sáng và chi tiết. Chụp một vật rõ; vẫn cần tự kiểm tra vật liệu.";
        }
        static float Luma(Color32 c)=>(.2126f*c.r+.7152f*c.g+.0722f*c.b);
    }
}

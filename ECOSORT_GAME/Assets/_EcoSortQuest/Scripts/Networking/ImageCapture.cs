using UnityEngine;

namespace EcoSortQuest
{
    public static class ImageCapture
    {
        public static Texture2D Resize(Texture2D source, int maxEdge)
        {
            float scale = Mathf.Min(1, (float)maxEdge / Mathf.Max(source.width, source.height));
            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, target); RenderTexture.active = target;
                var result = new Texture2D(width, height, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0); result.Apply(); return result;
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }
        public static Texture2D Orient(Texture2D source, int clockwise, bool mirrorVertical)
        {
            int angle = ((clockwise % 360) + 360) % 360;
            int width = angle == 90 || angle == 270 ? source.height : source.width;
            int height = angle == 90 || angle == 270 ? source.width : source.height;
            var input = source.GetPixels32(); var output = new Color32[input.Length];
            for (int y = 0; y < source.height; y++) for (int x = 0; x < source.width; x++)
            {
                int sy = mirrorVertical ? source.height - 1 - y : y;
                int dx = x, dy = y;
                if (angle == 90) { dx = y; dy = source.width - 1 - x; }
                else if (angle == 180) { dx = source.width - 1 - x; dy = source.height - 1 - y; }
                else if (angle == 270) { dx = source.height - 1 - y; dy = x; }
                output[dy * width + dx] = input[sy * source.width + x];
            }
            var result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.SetPixels32(output); result.Apply(); return result;
        }
    }
}

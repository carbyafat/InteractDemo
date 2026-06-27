using UnityEngine;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 儲存一次 RGB 圖片分析產生的結果資料。
    /// </summary>
    [System.Serializable]
    public class RgbAnalysisResult
    {
        /// <summary>
        /// 被分析貼圖的寬度，單位為像素。
        /// </summary>
        public int Width;

        /// <summary>
        /// 被分析貼圖的高度，單位為像素。
        /// </summary>
        public int Height;

        /// <summary>
        /// 轉換成 Unity 0-1 Color 格式的平均顏色。
        /// </summary>
        public Color AverageColor;

        /// <summary>
        /// 紅色通道平均值，範圍 0-255。
        /// </summary>
        public float AverageR;

        /// <summary>
        /// 綠色通道平均值，範圍 0-255。
        /// </summary>
        public float AverageG;

        /// <summary>
        /// 藍色通道平均值，範圍 0-255。
        /// </summary>
        public float AverageB;

        /// <summary>
        /// 由平均 RGB 通道計算出的簡易亮度值。
        /// </summary>
        public float Brightness;

        /// <summary>
        /// 將結果格式化成 Console 或 UI 文字。
        /// </summary>
        /// <returns>方便閱讀的分析摘要。</returns>
        public override string ToString()
        {
            return $"Size: {Width}x{Height}\n" +
                   $"Average RGB: R={AverageR:F1}, G={AverageG:F1}, B={AverageB:F1}\n" +
                   $"Brightness: {Brightness:F1}";
        }
    }
}

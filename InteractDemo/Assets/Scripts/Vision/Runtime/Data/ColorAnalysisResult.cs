using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 儲存一次主色分析產生的結果資料。
    /// </summary>
    [System.Serializable]
    public class ColorAnalysisResult
    {
        /// <summary>
        /// 最終判定的主要顏色分類。
        /// </summary>
        public MainColorType MainColor;

        /// <summary>
        /// 所有效不透明像素的平均顏色。
        /// </summary>
        public Color AverageColor;

        /// <summary>
        /// 最終分類對應的代表顯示色。
        /// </summary>
        public Color MainColorPreview;

        /// <summary>
        /// 經過透明度過濾後，實際用於分析的像素數量。
        /// </summary>
        public int ValidPixelCount;

        /// <summary>
        /// 來源圖片的總像素數量。
        /// </summary>
        public int TotalPixelCount;

        /// <summary>
        /// 有效像素在來源圖片中的比例。
        /// </summary>
        public float ValidPixelRatio;

        /// <summary>
        /// 有效像素中，比例最高的顏色分類占比。
        /// </summary>
        public float DominantRatio;

        /// <summary>
        /// 有效像素中，比例第二高的顏色分類占比。
        /// </summary>
        public float SecondRatio;

        /// <summary>
        /// 第一名與第二名顏色比例之間的差距。
        /// </summary>
        public float DominanceGap;

        /// <summary>
        /// 當圖片沒有明確主色時為 true。
        /// </summary>
        public bool IsMixed;

        /// <summary>
        /// 已排序的顏色比例，用於 debug 顯示。
        /// </summary>
        public List<ColorRatio> Ratios = new List<ColorRatio>();

        /// <summary>
        /// 將結果格式化成 Console 或 UI 文字。
        /// </summary>
        /// <returns>方便閱讀的顏色分析摘要。</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Main Color: {MainColor}");
            builder.AppendLine($"Average RGB: R={AverageColor.r * 255f:F1}, G={AverageColor.g * 255f:F1}, B={AverageColor.b * 255f:F1}");
            builder.AppendLine($"Valid Pixels: {ValidPixelCount}/{TotalPixelCount} ({ValidPixelRatio:P1})");
            builder.AppendLine($"Dominant Ratio: {DominantRatio:P1}");
            builder.AppendLine($"Second Ratio: {SecondRatio:P1}");
            builder.AppendLine($"Mixed: {IsMixed}");

            int shownCount = Mathf.Min(5, Ratios.Count);
            for (int i = 0; i < shownCount; i++)
                builder.AppendLine($"{i + 1}. {Ratios[i].ColorType}: {Ratios[i].Ratio:P1}");

            return builder.ToString();
        }
    }
}

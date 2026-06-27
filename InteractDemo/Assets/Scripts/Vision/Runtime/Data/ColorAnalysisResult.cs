using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace InteractDemo.Vision
{
    /// <summary>
    /// Stores the result values produced by one main-color analysis pass.
    /// </summary>
    [System.Serializable]
    public class ColorAnalysisResult
    {
        /// <summary>
        /// Final dominant color category.
        /// </summary>
        public MainColorType MainColor;

        /// <summary>
        /// Average color of all valid non-transparent pixels.
        /// </summary>
        public Color AverageColor;

        /// <summary>
        /// Representative display color for the final category.
        /// </summary>
        public Color MainColorPreview;

        /// <summary>
        /// Number of pixels used by the analyzer after alpha filtering.
        /// </summary>
        public int ValidPixelCount;

        /// <summary>
        /// Total number of pixels in the source image.
        /// </summary>
        public int TotalPixelCount;

        /// <summary>
        /// Ratio of valid pixels in the source image.
        /// </summary>
        public float ValidPixelRatio;

        /// <summary>
        /// Ratio of the strongest color category among valid pixels.
        /// </summary>
        public float DominantRatio;

        /// <summary>
        /// Ratio of the second strongest color category among valid pixels.
        /// </summary>
        public float SecondRatio;

        /// <summary>
        /// Difference between dominant and second strongest ratios.
        /// </summary>
        public float DominanceGap;

        /// <summary>
        /// True when the image has no clearly dominant color.
        /// </summary>
        public bool IsMixed;

        /// <summary>
        /// Sorted color ratios for debug display.
        /// </summary>
        public List<ColorRatio> Ratios = new List<ColorRatio>();

        /// <summary>
        /// Formats the result for Console or UI text output.
        /// </summary>
        /// <returns>Human-readable color analysis summary.</returns>
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

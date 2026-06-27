using UnityEngine;

namespace InteractDemo.Vision
{
    /// <summary>
    /// Stores the result values produced by one RGB image analysis.
    /// </summary>
    [System.Serializable]
    public class RgbAnalysisResult
    {
        /// <summary>
        /// Width of the analyzed texture in pixels.
        /// </summary>
        public int Width;

        /// <summary>
        /// Height of the analyzed texture in pixels.
        /// </summary>
        public int Height;

        /// <summary>
        /// Average color converted to Unity's 0-1 Color format.
        /// </summary>
        public Color AverageColor;

        /// <summary>
        /// Average red channel value in 0-255 range.
        /// </summary>
        public float AverageR;

        /// <summary>
        /// Average green channel value in 0-255 range.
        /// </summary>
        public float AverageG;

        /// <summary>
        /// Average blue channel value in 0-255 range.
        /// </summary>
        public float AverageB;

        /// <summary>
        /// Simple brightness value calculated from the average RGB channels.
        /// </summary>
        public float Brightness;

        /// <summary>
        /// Formats the result for Console or UI text output.
        /// </summary>
        /// <returns>Human-readable analysis summary.</returns>
        public override string ToString()
        {
            return $"Size: {Width}x{Height}\n" +
                   $"Average RGB: R={AverageR:F1}, G={AverageG:F1}, B={AverageB:F1}\n" +
                   $"Brightness: {Brightness:F1}";
        }
    }
}

using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;

namespace InteractDemo.Vision
{
    /// <summary>
    /// Provides main-color classification methods for Texture2D images.
    /// </summary>
    public static class ColorImageAnalyzer
    {
        private const byte AlphaThreshold = 16;
        private const float BlackValueThreshold = 0.12f;
        private const float WhiteValueThreshold = 0.86f;
        private const float LowSaturationThreshold = 0.16f;
        private const float BeigeSaturationThreshold = 0.36f;
        private const float MixedDominantThreshold = 0.42f;
        private const float MixedGapThreshold = 0.12f;

        /// <summary>
        /// Analyzes a Texture2D and classifies its dominant color.
        /// </summary>
        /// <param name="source">Texture2D image to analyze.</param>
        /// <returns>Color analysis result, or null if the input is invalid.</returns>
        public static ColorAnalysisResult Analyze(Texture2D source)
        {
            if (source == null)
            {
                Debug.LogError("ColorImageAnalyzer.Analyze failed: source texture is null.");
                return null;
            }

            using (Mat rgbaMat = new Mat(source.height, source.width, CvType.CV_8UC4))
            {
                OpenCVMatUtils.Texture2DToMat(source, rgbaMat);

                byte[] pixels = new byte[rgbaMat.total() * rgbaMat.channels()];
                rgbaMat.get(0, 0, pixels);

                return AnalyzeRgbaPixels(pixels, source.width, source.height);
            }
        }

        /// <summary>
        /// Returns a representative Unity color for a color category.
        /// </summary>
        /// <param name="colorType">Color category to preview.</param>
        /// <returns>Unity Color for display in UI.</returns>
        public static Color GetPreviewColor(MainColorType colorType)
        {
            switch (colorType)
            {
                case MainColorType.Red: return new Color(0.95f, 0.08f, 0.08f);
                case MainColorType.Orange: return new Color(1f, 0.45f, 0.05f);
                case MainColorType.Yellow: return new Color(1f, 0.86f, 0.08f);
                case MainColorType.Lime: return new Color(0.62f, 0.95f, 0.08f);
                case MainColorType.Green: return new Color(0.12f, 0.72f, 0.22f);
                case MainColorType.Cyan: return new Color(0.05f, 0.85f, 0.85f);
                case MainColorType.SkyBlue: return new Color(0.2f, 0.62f, 1f);
                case MainColorType.Blue: return new Color(0.08f, 0.22f, 0.95f);
                case MainColorType.Purple: return new Color(0.45f, 0.18f, 0.85f);
                case MainColorType.Magenta: return new Color(0.9f, 0.1f, 0.78f);
                case MainColorType.Pink: return new Color(1f, 0.45f, 0.68f);
                case MainColorType.Brown: return new Color(0.42f, 0.24f, 0.1f);
                case MainColorType.Beige: return new Color(0.78f, 0.65f, 0.45f);
                case MainColorType.White: return Color.white;
                case MainColorType.LightGray: return new Color(0.75f, 0.75f, 0.75f);
                case MainColorType.Gray: return new Color(0.5f, 0.5f, 0.5f);
                case MainColorType.DarkGray: return new Color(0.24f, 0.24f, 0.24f);
                case MainColorType.Black: return Color.black;
                case MainColorType.Transparent: return new Color(1f, 1f, 1f, 0.15f);
                case MainColorType.Mixed: return new Color(0.75f, 0.75f, 0.75f);
                default: return Color.gray;
            }
        }

        /// <summary>
        /// Classifies RGBA pixel data into color ratios and a final main color.
        /// </summary>
        /// <param name="pixels">RGBA pixel byte array.</param>
        /// <param name="width">Source image width.</param>
        /// <param name="height">Source image height.</param>
        /// <returns>Color analysis result.</returns>
        private static ColorAnalysisResult AnalyzeRgbaPixels(byte[] pixels, int width, int height)
        {
            Dictionary<MainColorType, int> counts = CreateColorCountMap();
            int totalPixelCount = width * height;
            int validPixelCount = 0;
            double sumR = 0;
            double sumG = 0;
            double sumB = 0;

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte r = pixels[i];
                byte g = pixels[i + 1];
                byte b = pixels[i + 2];
                byte a = pixels[i + 3];

                if (a < AlphaThreshold)
                    continue;

                validPixelCount++;
                sumR += r;
                sumG += g;
                sumB += b;

                MainColorType colorType = ClassifyPixel(r, g, b);
                counts[colorType]++;
            }

            if (validPixelCount == 0)
                return CreateTransparentResult(totalPixelCount);

            List<ColorRatio> ratios = BuildSortedRatios(counts, validPixelCount);
            ColorRatio dominant = ratios[0];
            ColorRatio second = ratios.Count > 1 ? ratios[1] : new ColorRatio(MainColorType.Unknown, 0f);

            bool isMixed = dominant.Ratio < MixedDominantThreshold || dominant.Ratio - second.Ratio < MixedGapThreshold;
            MainColorType mainColor = isMixed ? MainColorType.Mixed : dominant.ColorType;

            return new ColorAnalysisResult
            {
                MainColor = mainColor,
                AverageColor = new Color((float)(sumR / validPixelCount) / 255f, (float)(sumG / validPixelCount) / 255f, (float)(sumB / validPixelCount) / 255f, 1f),
                MainColorPreview = GetPreviewColor(mainColor),
                ValidPixelCount = validPixelCount,
                TotalPixelCount = totalPixelCount,
                ValidPixelRatio = validPixelCount / (float)totalPixelCount,
                DominantRatio = dominant.Ratio,
                SecondRatio = second.Ratio,
                DominanceGap = dominant.Ratio - second.Ratio,
                IsMixed = isMixed,
                Ratios = ratios
            };
        }

        /// <summary>
        /// Classifies a single RGB pixel into one color category.
        /// </summary>
        /// <param name="r">Red channel in 0-255 range.</param>
        /// <param name="g">Green channel in 0-255 range.</param>
        /// <param name="b">Blue channel in 0-255 range.</param>
        /// <returns>Color category for the pixel.</returns>
        private static MainColorType ClassifyPixel(byte r, byte g, byte b)
        {
            Color.RGBToHSV(new Color(r / 255f, g / 255f, b / 255f), out float hue, out float saturation, out float value);
            float hueDegrees = hue * 360f;

            if (value <= BlackValueThreshold)
                return MainColorType.Black;

            if (saturation <= LowSaturationThreshold)
                return ClassifyNeutral(value);

            if (IsBrownOrBeige(hueDegrees, saturation, value, out MainColorType earthColor))
                return earthColor;

            return ClassifyHue(hueDegrees, saturation, value);
        }

        /// <summary>
        /// Classifies low-saturation pixels by brightness.
        /// </summary>
        /// <param name="value">HSV value channel in 0-1 range.</param>
        /// <returns>Neutral color category.</returns>
        private static MainColorType ClassifyNeutral(float value)
        {
            if (value >= WhiteValueThreshold)
                return MainColorType.White;
            if (value >= 0.62f)
                return MainColorType.LightGray;
            if (value >= 0.34f)
                return MainColorType.Gray;

            return MainColorType.DarkGray;
        }

        /// <summary>
        /// Detects brown and beige before general hue classification.
        /// </summary>
        /// <param name="hueDegrees">Hue in degrees.</param>
        /// <param name="saturation">HSV saturation in 0-1 range.</param>
        /// <param name="value">HSV value in 0-1 range.</param>
        /// <param name="earthColor">Detected brown or beige category.</param>
        /// <returns>True when the pixel is classified as brown or beige.</returns>
        private static bool IsBrownOrBeige(float hueDegrees, float saturation, float value, out MainColorType earthColor)
        {
            earthColor = MainColorType.Unknown;

            bool warmHue = hueDegrees >= 18f && hueDegrees < 55f;
            if (!warmHue)
                return false;

            if (value < 0.48f && saturation >= 0.28f)
            {
                earthColor = MainColorType.Brown;
                return true;
            }

            if (value >= 0.48f && saturation <= BeigeSaturationThreshold)
            {
                earthColor = MainColorType.Beige;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Classifies saturated pixels by hue angle.
        /// </summary>
        /// <param name="hueDegrees">Hue in degrees.</param>
        /// <param name="saturation">HSV saturation in 0-1 range.</param>
        /// <param name="value">HSV value in 0-1 range.</param>
        /// <returns>Hue-based color category.</returns>
        private static MainColorType ClassifyHue(float hueDegrees, float saturation, float value)
        {
            if (hueDegrees < 12f || hueDegrees >= 348f)
                return saturation < 0.55f && value > 0.55f ? MainColorType.Pink : MainColorType.Red;
            if (hueDegrees < 35f)
                return MainColorType.Orange;
            if (hueDegrees < 60f)
                return MainColorType.Yellow;
            if (hueDegrees < 88f)
                return MainColorType.Lime;
            if (hueDegrees < 150f)
                return MainColorType.Green;
            if (hueDegrees < 185f)
                return MainColorType.Cyan;
            if (hueDegrees < 215f)
                return MainColorType.SkyBlue;
            if (hueDegrees < 255f)
                return MainColorType.Blue;
            if (hueDegrees < 285f)
                return MainColorType.Purple;
            if (hueDegrees < 325f)
                return MainColorType.Magenta;

            return MainColorType.Pink;
        }

        /// <summary>
        /// Creates a count map with all color categories initialized.
        /// </summary>
        /// <returns>Dictionary from color category to pixel count.</returns>
        private static Dictionary<MainColorType, int> CreateColorCountMap()
        {
            Dictionary<MainColorType, int> counts = new Dictionary<MainColorType, int>();
            foreach (MainColorType colorType in Enum.GetValues(typeof(MainColorType)))
            {
                if (colorType == MainColorType.Transparent || colorType == MainColorType.Mixed || colorType == MainColorType.Unknown)
                    continue;

                counts[colorType] = 0;
            }

            return counts;
        }

        /// <summary>
        /// Builds sorted color ratios from raw pixel counts.
        /// </summary>
        /// <param name="counts">Pixel counts by color category.</param>
        /// <param name="validPixelCount">Number of valid pixels used by the analysis.</param>
        /// <returns>Descending list of color ratios.</returns>
        private static List<ColorRatio> BuildSortedRatios(Dictionary<MainColorType, int> counts, int validPixelCount)
        {
            List<ColorRatio> ratios = new List<ColorRatio>();
            foreach (KeyValuePair<MainColorType, int> pair in counts)
            {
                if (pair.Value <= 0)
                    continue;

                ratios.Add(new ColorRatio(pair.Key, pair.Value / (float)validPixelCount));
            }

            ratios.Sort((a, b) => b.Ratio.CompareTo(a.Ratio));
            return ratios;
        }

        /// <summary>
        /// Creates a result for images with no valid opaque pixels.
        /// </summary>
        /// <param name="totalPixelCount">Total number of source pixels.</param>
        /// <returns>Transparent color analysis result.</returns>
        private static ColorAnalysisResult CreateTransparentResult(int totalPixelCount)
        {
            return new ColorAnalysisResult
            {
                MainColor = MainColorType.Transparent,
                AverageColor = Color.clear,
                MainColorPreview = GetPreviewColor(MainColorType.Transparent),
                ValidPixelCount = 0,
                TotalPixelCount = totalPixelCount,
                ValidPixelRatio = 0f,
                DominantRatio = 0f,
                SecondRatio = 0f,
                DominanceGap = 0f,
                IsMixed = false,
                Ratios = new List<ColorRatio>()
            };
        }
    }
}

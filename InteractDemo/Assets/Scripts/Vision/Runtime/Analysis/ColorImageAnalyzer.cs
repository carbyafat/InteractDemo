using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 提供 Texture2D 圖片的主色分類方法。
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
        /// 分析一張 Texture2D，並分類它的主要顏色。
        /// </summary>
        /// <param name="source">要分析的 Texture2D 圖片。</param>
        /// <returns>顏色分析結果；如果輸入無效則回傳 null。</returns>
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

                byte[] pixels = new byte[(int)(rgbaMat.total() * rgbaMat.channels())];
                rgbaMat.get(0, 0, pixels);

                return AnalyzeRgbaPixels(pixels, source.width, source.height);
            }
        }

        /// <summary>
        /// 取得指定顏色分類對應的 Unity 預覽色。
        /// </summary>
        /// <param name="colorType">要預覽的顏色分類。</param>
        /// <returns>可用於 UI 顯示的 Unity Color。</returns>
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
        /// 將 RGBA 像素資料分類成顏色比例，並產生最終主色。
        /// </summary>
        /// <param name="pixels">RGBA 像素 byte 陣列。</param>
        /// <param name="width">來源圖片寬度。</param>
        /// <param name="height">來源圖片高度。</param>
        /// <returns>顏色分析結果。</returns>
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
        /// 將單一 RGB 像素分類到一種顏色類別。
        /// </summary>
        /// <param name="r">紅色通道，範圍 0-255。</param>
        /// <param name="g">綠色通道，範圍 0-255。</param>
        /// <param name="b">藍色通道，範圍 0-255。</param>
        /// <returns>此像素對應的顏色分類。</returns>
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
        /// 依照亮度分類低飽和度像素。
        /// </summary>
        /// <param name="value">HSV 明度通道，範圍 0-1。</param>
        /// <returns>中性色分類。</returns>
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
        /// 在一般色相分類前，先判斷棕色與米色。
        /// </summary>
        /// <param name="hueDegrees">色相角度。</param>
        /// <param name="saturation">HSV 飽和度，範圍 0-1。</param>
        /// <param name="value">HSV 明度，範圍 0-1。</param>
        /// <param name="earthColor">偵測到的棕色或米色分類。</param>
        /// <returns>如果像素被分類為棕色或米色，回傳 true。</returns>
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
        /// 依照色相角度分類高飽和度像素。
        /// </summary>
        /// <param name="hueDegrees">色相角度。</param>
        /// <param name="saturation">HSV 飽和度，範圍 0-1。</param>
        /// <param name="value">HSV 明度，範圍 0-1。</param>
        /// <returns>依照色相得到的顏色分類。</returns>
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
        /// 建立已初始化所有顏色分類的計數表。
        /// </summary>
        /// <returns>顏色分類到像素數量的對照表。</returns>
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
        /// 依照原始像素數量建立排序後的顏色比例。
        /// </summary>
        /// <param name="counts">各顏色分類的像素數量。</param>
        /// <param name="validPixelCount">本次分析使用的有效像素數量。</param>
        /// <returns>由高到低排序的顏色比例清單。</returns>
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
        /// 建立沒有有效不透明像素時的分析結果。
        /// </summary>
        /// <param name="totalPixelCount">來源圖片總像素數。</param>
        /// <returns>透明圖片的顏色分析結果。</returns>
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

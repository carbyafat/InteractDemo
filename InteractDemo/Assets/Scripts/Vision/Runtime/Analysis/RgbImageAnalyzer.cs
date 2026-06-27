using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 提供 Texture2D 圖片的 RGB 分析方法。
    /// </summary>
    public static class RgbImageAnalyzer
    {
        /// <summary>
        /// 分析一張 Texture2D，並計算平均 RGB 數值。
        /// </summary>
        /// <param name="source">要分析的 Texture2D 圖片。</param>
        /// <returns>RGB 分析結果；如果輸入無效則回傳 null。</returns>
        public static RgbAnalysisResult Analyze(Texture2D source)
        {
            if (source == null)
            {
                Debug.LogError("RgbImageAnalyzer.Analyze failed: source texture is null.");
                return null;
            }

            using (Mat rgbaMat = new Mat(source.height, source.width, CvType.CV_8UC4))
            {
                OpenCVMatUtils.Texture2DToMat(source, rgbaMat);

                Scalar sum = Core.sumElems(rgbaMat);

                double pixelCount = rgbaMat.rows() * rgbaMat.cols();

                float averageR = (float)(sum.val[0] / pixelCount);
                float averageG = (float)(sum.val[1] / pixelCount);
                float averageB = (float)(sum.val[2] / pixelCount);

                float brightness = (averageR + averageG + averageB) / 3f;

                return new RgbAnalysisResult
                {
                    Width = source.width,
                    Height = source.height,
                    AverageR = averageR,
                    AverageG = averageG,
                    AverageB = averageB,
                    Brightness = brightness,

                    AverageColor = new Color(averageR / 255f, averageG / 255f, averageB / 255f, 1f)
                };
            }
        }
    }
}

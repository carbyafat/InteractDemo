using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using Rect = UnityEngine.Rect;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 提供 Texture2D 圖片的灰階、二值化、邊緣與輪廓 debug 處理。
    /// </summary>
    public static class ShapeImageProcessor
    {
        /// <summary>
        /// 依照選擇的 debug 模式處理 Texture2D。
        /// </summary>
        /// <param name="source">要處理的 Texture2D 圖片。</param>
        /// <param name="debugMode">要產生的 debug 輸出模式。</param>
        /// <param name="threshold">二值化模式與輪廓前處理使用的門檻值。</param>
        /// <param name="cannyThreshold1">Canny 邊緣偵測的第一個遲滯門檻。</param>
        /// <param name="cannyThreshold2">Canny 邊緣偵測的第二個遲滯門檻。</param>
        /// <param name="blurSize">高斯模糊核心大小，必須為奇數；低於 3 時不套用模糊。</param>
        /// <param name="drawThickness">輪廓線條粗細，單位為像素。</param>
        /// <returns>包含 debug 貼圖的形狀分析結果；如果輸入無效則回傳 null。</returns>
        public static ShapeAnalysisResult Process(
            Texture2D source,
            VisionDebugMode debugMode,
            double threshold,
            double cannyThreshold1,
            double cannyThreshold2,
            int blurSize,
            int drawThickness)
        {
            if (source == null)
            {
                Debug.LogError("ShapeImageProcessor.Process failed: source texture is null.");
                return null;
            }

            using (Mat rgbaMat = new Mat(source.height, source.width, CvType.CV_8UC4))
            using (Mat grayMat = new Mat())
            using (Mat preparedGrayMat = new Mat())
            using (Mat thresholdMat = new Mat())
            using (Mat cannyMat = new Mat())
            {
                OpenCVMatUtils.Texture2DToMat(source, rgbaMat);

                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                PrepareGray(grayMat, preparedGrayMat, blurSize);
                Imgproc.threshold(preparedGrayMat, thresholdMat, threshold, 255, Imgproc.THRESH_BINARY);
                Imgproc.Canny(preparedGrayMat, cannyMat, cannyThreshold1, cannyThreshold2);

                switch (debugMode)
                {
                    case VisionDebugMode.Original:
                        return CreateResult(debugMode, rgbaMat, null, source.width, source.height);

                    case VisionDebugMode.Grayscale:
                        return CreateResult(debugMode, preparedGrayMat, null, source.width, source.height);

                    case VisionDebugMode.Threshold:
                        return CreateResult(debugMode, thresholdMat, FindContours(thresholdMat), source.width, source.height);

                    case VisionDebugMode.Canny:
                        return CreateResult(debugMode, cannyMat, FindContours(cannyMat), source.width, source.height);

                    case VisionDebugMode.AllContours:
                        return CreateContourDebugResult(debugMode, rgbaMat, cannyMat, false, source.width, source.height, drawThickness);

                    case VisionDebugMode.LargestContour:
                        return CreateContourDebugResult(debugMode, rgbaMat, cannyMat, true, source.width, source.height, drawThickness);

                    default:
                        Debug.LogError($"ShapeImageProcessor.Process failed: unsupported debug mode {debugMode}.");
                        return null;
                }
            }
        }

        /// <summary>
        /// 對灰階 Mat 套用可選的模糊處理。
        /// </summary>
        /// <param name="sourceGray">來源灰階 Mat。</param>
        /// <param name="outputGray">輸出灰階 Mat。</param>
        /// <param name="blurSize">高斯模糊核心大小，必須為奇數。</param>
        private static void PrepareGray(Mat sourceGray, Mat outputGray, int blurSize)
        {
            int normalizedBlurSize = Mathf.Max(0, blurSize);
            if (normalizedBlurSize >= 3)
            {
                if (normalizedBlurSize % 2 == 0)
                    normalizedBlurSize += 1;

                Imgproc.GaussianBlur(sourceGray, outputGray, new Size(normalizedBlurSize, normalizedBlurSize), 0);
            }
            else
            {
                sourceGray.copyTo(outputGray);
            }
        }

        /// <summary>
        /// 在單通道 debug Mat 中尋找外部輪廓。
        /// </summary>
        /// <param name="singleChannelMat">用於輪廓偵測的二值化或邊緣 Mat。</param>
        /// <returns>偵測到的輪廓。</returns>
        private static List<MatOfPoint> FindContours(Mat singleChannelMat)
        {
            List<MatOfPoint> contours = new List<MatOfPoint>();
            using (Mat contourSource = singleChannelMat.clone())
            using (Mat hierarchy = new Mat())
            {
                Imgproc.findContours(contourSource, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);
            }

            return contours;
        }

        /// <summary>
        /// 為直接顯示 Mat 的模式建立結果。
        /// </summary>
        /// <param name="debugMode">此輸出對應的 debug 模式。</param>
        /// <param name="outputMat">要轉換成 Texture2D 的 Mat。</param>
        /// <param name="contours">可選的輪廓資料，用於統計。</param>
        /// <param name="width">來源圖片寬度。</param>
        /// <param name="height">來源圖片高度。</param>
        /// <returns>形狀分析結果。</returns>
        private static ShapeAnalysisResult CreateResult(VisionDebugMode debugMode, Mat outputMat, List<MatOfPoint> contours, int width, int height)
        {
            ShapeAnalysisResult result = CreateStats(debugMode, contours, width, height);
            result.OutputTexture = MatToTexture(outputMat);
            DisposeContours(contours);
            return result;
        }

        /// <summary>
        /// 建立輪廓疊圖結果。
        /// </summary>
        /// <param name="debugMode">此輸出對應的 debug 模式。</param>
        /// <param name="sourceRgbaMat">作為繪製背景的來源 RGBA Mat。</param>
        /// <param name="contourSourceMat">用於偵測輪廓的單通道 Mat。</param>
        /// <param name="largestOnly">若為 true，只繪製最大輪廓。</param>
        /// <param name="width">來源圖片寬度。</param>
        /// <param name="height">來源圖片高度。</param>
        /// <param name="drawThickness">輪廓線條粗細，單位為像素。</param>
        /// <returns>形狀分析結果。</returns>
        private static ShapeAnalysisResult CreateContourDebugResult(
            VisionDebugMode debugMode,
            Mat sourceRgbaMat,
            Mat contourSourceMat,
            bool largestOnly,
            int width,
            int height,
            int drawThickness)
        {
            List<MatOfPoint> contours = FindContours(contourSourceMat);
            ShapeAnalysisResult result = CreateStats(debugMode, contours, width, height);

            using (Mat drawMat = sourceRgbaMat.clone())
            {
                if (largestOnly)
                {
                    int largestIndex = FindLargestContourIndex(contours);
                    if (largestIndex >= 0)
                        Imgproc.drawContours(drawMat, contours, largestIndex, new Scalar(255, 0, 0, 255), Mathf.Max(1, drawThickness));
                }
                else if (contours.Count > 0)
                {
                    Imgproc.drawContours(drawMat, contours, -1, new Scalar(0, 255, 0, 255), Mathf.Max(1, drawThickness));
                }

                result.OutputTexture = MatToTexture(drawMat);
            }

            DisposeContours(contours);
            return result;
        }

        /// <summary>
        /// 建立結果用的輪廓統計資料。
        /// </summary>
        /// <param name="debugMode">此結果對應的 debug 模式。</param>
        /// <param name="contours">偵測到的輪廓；如果該模式不偵測輪廓則可為 null。</param>
        /// <param name="width">來源圖片寬度。</param>
        /// <param name="height">來源圖片高度。</param>
        /// <returns>尚未包含輸出貼圖的形狀分析結果。</returns>
        private static ShapeAnalysisResult CreateStats(VisionDebugMode debugMode, List<MatOfPoint> contours, int width, int height)
        {
            ShapeAnalysisResult result = new ShapeAnalysisResult
            {
                DebugMode = debugMode,
                ContourCount = contours != null ? contours.Count : 0
            };

            int largestIndex = FindLargestContourIndex(contours);
            if (largestIndex >= 0)
            {
                MatOfPoint largestContour = contours[largestIndex];
                OpenCVForUnity.CoreModule.Rect bounds = Imgproc.boundingRect(largestContour);
                result.LargestContourArea = Imgproc.contourArea(largestContour);
                result.LargestContourAreaRatio = (float)(result.LargestContourArea / (width * height));
                result.LargestContourBounds = new Rect(bounds.x, bounds.y, bounds.width, bounds.height);
            }

            return result;
        }

        /// <summary>
        /// 依照面積找出最大輪廓的索引。
        /// </summary>
        /// <param name="contours">偵測到的輪廓。</param>
        /// <returns>最大輪廓的索引；如果沒有輪廓則回傳 -1。</returns>
        private static int FindLargestContourIndex(List<MatOfPoint> contours)
        {
            if (contours == null || contours.Count == 0)
                return -1;

            int largestIndex = 0;
            double largestArea = Imgproc.contourArea(contours[0]);
            for (int i = 1; i < contours.Count; i++)
            {
                double area = Imgproc.contourArea(contours[i]);
                if (area > largestArea)
                {
                    largestArea = area;
                    largestIndex = i;
                }
            }

            return largestIndex;
        }

        /// <summary>
        /// 將 Mat 轉換成 Texture2D，供 Unity UI 預覽使用。
        /// </summary>
        /// <param name="mat">要轉換的 Mat。</param>
        /// <returns>包含 Mat 圖像內容的 Texture2D。</returns>
        private static Texture2D MatToTexture(Mat mat)
        {
            Texture2D texture = new Texture2D(mat.cols(), mat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(mat, texture);
            return texture;
        }

        /// <summary>
        /// 釋放由 OpenCV 建立的輪廓 Mat。
        /// </summary>
        /// <param name="contours">要釋放的輪廓清單。</param>
        private static void DisposeContours(List<MatOfPoint> contours)
        {
            if (contours == null)
                return;

            for (int i = 0; i < contours.Count; i++)
                contours[i].Dispose();
        }
    }
}

using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 分析樸克牌角落資訊區，做紅黑、花色、點數與 Joker 的初步判斷。
    /// </summary>
    public static class PlayingCardIdentityAnalyzer
    {
        /// <summary>
        /// 分析一張角落資訊區圖片。
        /// </summary>
        /// <param name="cornerTexture">階段三裁出的角落資訊區。</param>
        /// <param name="redDominanceThreshold">紅色判定需要超過黑色的最小比例差。</param>
        /// <param name="minSymbolPixelRatio">判定有符號時需要達到的最小遮罩像素比例。</param>
        /// <param name="minContourAreaRatio">接受符號輪廓時的最小面積比例。</param>
        /// <param name="drawDebugContours">是否在 debug 圖上畫出候選輪廓。</param>
        /// <returns>樸克牌角落辨識結果；如果輸入無效則回傳 null。</returns>
        public static PlayingCardIdentityResult Analyze(
            Texture2D cornerTexture,
            float redDominanceThreshold,
            float minSymbolPixelRatio,
            float minContourAreaRatio,
            bool drawDebugContours)
        {
            if (cornerTexture == null)
            {
                Debug.LogError("PlayingCardIdentityAnalyzer.Analyze failed: corner texture is null.");
                return null;
            }

            using (Mat rgbaMat = new Mat(cornerTexture.height, cornerTexture.width, CvType.CV_8UC4))
            using (Mat redMask = new Mat())
            using (Mat blackMask = new Mat())
            using (Mat debugMat = new Mat())
            {
                OpenCVMatUtils.Texture2DToMat(cornerTexture, rgbaMat);
                rgbaMat.copyTo(debugMat);

                CreateRedMask(rgbaMat, redMask);
                CreateBlackMask(rgbaMat, blackMask);

                int pixelCount = Mathf.Max(1, cornerTexture.width * cornerTexture.height);
                float redPixelRatio = Core.countNonZero(redMask) / (float)pixelCount;
                float blackPixelRatio = Core.countNonZero(blackMask) / (float)pixelCount;

                List<MatOfPoint> redContours = FindContours(redMask);
                List<MatOfPoint> blackContours = FindContours(blackMask);
                SymbolStats redStats = BuildSymbolStats(redContours, pixelCount, minContourAreaRatio);
                SymbolStats blackStats = BuildSymbolStats(blackContours, pixelCount, minContourAreaRatio);
                PlayingCardColorGroup colorGroup = ClassifyColor(
                    redPixelRatio,
                    blackPixelRatio,
                    redStats,
                    blackStats,
                    redDominanceThreshold,
                    minSymbolPixelRatio);

                List<MatOfPoint> selectedContours = colorGroup == PlayingCardColorGroup.Black ? blackContours : redContours;
                SymbolStats selectedStats = colorGroup == PlayingCardColorGroup.Black ? blackStats : redStats;

                PlayingCardIdentityResult result = new PlayingCardIdentityResult
                {
                    ColorGroup = colorGroup,
                    RedPixelRatio = redPixelRatio,
                    BlackPixelRatio = blackPixelRatio,
                    SymbolContourCount = selectedStats.AcceptedContourCount,
                    LargestSymbolAreaRatio = selectedStats.LargestAreaRatio
                };

                ClassifyIdentity(result, selectedStats, minSymbolPixelRatio);

                if (drawDebugContours)
                    DrawDebug(debugMat, selectedContours, selectedStats.AcceptedContourIndexes, colorGroup);

                result.DebugTexture = MatToTexture(debugMat);
                DisposeContours(redContours);
                DisposeContours(blackContours);

                return result;
            }
        }

        /// <summary>
        /// 建立紅色符號遮罩。
        /// </summary>
        /// <param name="rgbaMat">來源 RGBA Mat。</param>
        /// <param name="redMask">輸出的紅色遮罩。</param>
        private static void CreateRedMask(Mat rgbaMat, Mat redMask)
        {
            using (Mat hsvMat = new Mat())
            using (Mat lowerRedMask = new Mat())
            using (Mat upperRedMask = new Mat())
            {
                Imgproc.cvtColor(rgbaMat, hsvMat, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(hsvMat, hsvMat, Imgproc.COLOR_RGB2HSV);

                Core.inRange(hsvMat, new Scalar(0, 70, 70), new Scalar(12, 255, 255), lowerRedMask);
                Core.inRange(hsvMat, new Scalar(170, 70, 70), new Scalar(180, 255, 255), upperRedMask);
                Core.bitwise_or(lowerRedMask, upperRedMask, redMask);
                CleanMask(redMask);
            }
        }

        /// <summary>
        /// 建立黑色符號遮罩。
        /// </summary>
        /// <param name="rgbaMat">來源 RGBA Mat。</param>
        /// <param name="blackMask">輸出的黑色遮罩。</param>
        private static void CreateBlackMask(Mat rgbaMat, Mat blackMask)
        {
            using (Mat rgbMat = new Mat())
            using (Mat grayMat = new Mat())
            {
                Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(rgbMat, grayMat, Imgproc.COLOR_RGB2GRAY);
                Imgproc.threshold(grayMat, blackMask, 55, 255, Imgproc.THRESH_BINARY_INV);
                CleanMask(blackMask);
            }
        }

        /// <summary>
        /// 清理遮罩中的小雜訊。
        /// </summary>
        /// <param name="mask">要清理的遮罩。</param>
        private static void CleanMask(Mat mask)
        {
            using (Mat kernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3)))
            {
                Imgproc.morphologyEx(mask, mask, Imgproc.MORPH_OPEN, kernel);
                Imgproc.morphologyEx(mask, mask, Imgproc.MORPH_CLOSE, kernel);
            }
        }

        /// <summary>
        /// 依照紅黑像素比例判斷符號顏色。
        /// </summary>
        /// <param name="redPixelRatio">紅色像素比例。</param>
        /// <param name="blackPixelRatio">黑色像素比例。</param>
        /// <param name="redDominanceThreshold">紅色需要超過黑色的比例差。</param>
        /// <param name="minSymbolPixelRatio">最小符號像素比例。</param>
        /// <returns>紅黑顏色分類。</returns>
        private static PlayingCardColorGroup ClassifyColor(
            float redPixelRatio,
            float blackPixelRatio,
            SymbolStats redStats,
            SymbolStats blackStats,
            float redDominanceThreshold,
            float minSymbolPixelRatio)
        {
            if (redPixelRatio >= minSymbolPixelRatio &&
                redStats.AcceptedContourCount > 0 &&
                redPixelRatio >= redDominanceThreshold)
            {
                return PlayingCardColorGroup.Red;
            }

            if (blackPixelRatio >= minSymbolPixelRatio &&
                blackPixelRatio < 0.45f &&
                blackStats.AcceptedContourCount > 0)
            {
                return PlayingCardColorGroup.Black;
            }

            return PlayingCardColorGroup.Unknown;
        }

        /// <summary>
        /// 從遮罩中尋找外部輪廓。
        /// </summary>
        /// <param name="mask">符號遮罩。</param>
        /// <returns>偵測到的輪廓。</returns>
        private static List<MatOfPoint> FindContours(Mat mask)
        {
            List<MatOfPoint> contours = new List<MatOfPoint>();
            using (Mat contourSource = mask.clone())
            using (Mat hierarchy = new Mat())
            {
                Imgproc.findContours(contourSource, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);
            }

            return contours;
        }

        /// <summary>
        /// 建立符號輪廓統計。
        /// </summary>
        /// <param name="contours">偵測到的輪廓。</param>
        /// <param name="pixelCount">角落圖片像素總數。</param>
        /// <param name="minContourAreaRatio">接受輪廓的最小面積比例。</param>
        /// <returns>符號統計。</returns>
        private static SymbolStats BuildSymbolStats(List<MatOfPoint> contours, int pixelCount, float minContourAreaRatio)
        {
            SymbolStats stats = new SymbolStats();
            double minArea = pixelCount * Mathf.Clamp01(minContourAreaRatio);

            for (int i = 0; i < contours.Count; i++)
            {
                double area = Imgproc.contourArea(contours[i]);
                if (area < minArea)
                    continue;

                Rect bounds = Imgproc.boundingRect(contours[i]);
                stats.AcceptedContourIndexes.Add(i);
                stats.AcceptedContourCount++;

                float areaRatio = (float)(area / pixelCount);
                if (areaRatio > stats.LargestAreaRatio)
                {
                    stats.LargestAreaRatio = areaRatio;
                    stats.LargestBounds = bounds;
                }
            }

            return stats;
        }

        /// <summary>
        /// 依照目前啟發式規則判斷花色與點數。
        /// </summary>
        /// <param name="result">要寫入的結果。</param>
        /// <param name="stats">符號統計。</param>
        /// <param name="minSymbolPixelRatio">最小符號像素比例。</param>
        private static void ClassifyIdentity(PlayingCardIdentityResult result, SymbolStats stats, float minSymbolPixelRatio)
        {
            if (result.ColorGroup == PlayingCardColorGroup.Red && stats.AcceptedContourCount >= 2)
            {
                result.Suit = PlayingCardSuit.Heart;
                result.Rank = "1";
                result.Confidence = Mathf.Clamp01(0.45f + result.RedPixelRatio * 4f + Mathf.Min(0.25f, stats.LargestAreaRatio * 5f));
                result.Reason = "Red symbols found. Heuristic result: Heart + 1.";
                return;
            }

            if (result.ColorGroup == PlayingCardColorGroup.Red && stats.AcceptedContourCount == 1)
            {
                result.Suit = PlayingCardSuit.Heart;
                result.Rank = "Unknown";
                result.Confidence = 0.35f;
                result.Reason = "One red symbol found. Suit may be Heart or Diamond.";
                return;
            }

            if (result.ColorGroup == PlayingCardColorGroup.Black && stats.AcceptedContourCount > 0)
            {
                result.Suit = PlayingCardSuit.Unknown;
                result.Rank = "Unknown";
                result.Confidence = 0.25f;
                result.Reason = "Black symbols found. Spade/Club templates are not implemented yet.";
                return;
            }

            result.IsJoker = false;
            result.Rank = "Unknown";
            result.Suit = PlayingCardSuit.Unknown;
            result.Confidence = result.RedPixelRatio >= minSymbolPixelRatio ? 0.2f : 0f;
            result.Reason = "No reliable symbol match. Joker rule is reserved.";
        }

        /// <summary>
        /// 在 debug 圖上繪製候選輪廓。
        /// </summary>
        /// <param name="debugMat">要繪製的 debug Mat。</param>
        /// <param name="contours">所有輪廓。</param>
        /// <param name="acceptedIndexes">被接受的輪廓索引。</param>
        /// <param name="colorGroup">目前判定的顏色分類。</param>
        private static void DrawDebug(Mat debugMat, List<MatOfPoint> contours, List<int> acceptedIndexes, PlayingCardColorGroup colorGroup)
        {
            Scalar drawColor = colorGroup == PlayingCardColorGroup.Red
                ? new Scalar(255, 255, 0, 255)
                : new Scalar(0, 255, 255, 255);

            for (int i = 0; i < acceptedIndexes.Count; i++)
            {
                int contourIndex = acceptedIndexes[i];
                Imgproc.drawContours(debugMat, contours, contourIndex, drawColor, 2);
                Rect bounds = Imgproc.boundingRect(contours[contourIndex]);
                Imgproc.rectangle(
                    debugMat,
                    new Point(bounds.x, bounds.y),
                    new Point(bounds.x + bounds.width, bounds.y + bounds.height),
                    drawColor,
                    1);
            }
        }

        /// <summary>
        /// 將 Mat 轉換成 Texture2D。
        /// </summary>
        /// <param name="mat">要轉換的 Mat。</param>
        /// <returns>Texture2D。</returns>
        private static Texture2D MatToTexture(Mat mat)
        {
            Texture2D texture = new Texture2D(mat.cols(), mat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(mat, texture);
            return texture;
        }

        /// <summary>
        /// 釋放 OpenCV 輪廓資料。
        /// </summary>
        /// <param name="contours">要釋放的輪廓。</param>
        private static void DisposeContours(List<MatOfPoint> contours)
        {
            for (int i = 0; i < contours.Count; i++)
                contours[i].Dispose();
        }

        private class SymbolStats
        {
            public readonly List<int> AcceptedContourIndexes = new List<int>();
            public int AcceptedContourCount;
            public float LargestAreaRatio;
            public Rect LargestBounds;
        }
    }
}

using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using Rect = UnityEngine.Rect;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 從 Texture2D 圖片中偵測單一類似樸克牌的四邊形。
    /// </summary>
    public static class PlayingCardDetector
    {
        /// <summary>
        /// 從來源貼圖中偵測最大的類樸克牌四邊形。
        /// </summary>
        /// <param name="source">要處理的 Texture2D 圖片。</param>
        /// <param name="cannyThreshold1">Canny 邊緣偵測的第一個遲滯門檻。</param>
        /// <param name="cannyThreshold2">Canny 邊緣偵測的第二個遲滯門檻。</param>
        /// <param name="blurSize">高斯模糊核心大小，必須為奇數；低於 3 時不套用模糊。</param>
        /// <param name="minAreaRatio">接受為牌面候選時需要達到的最小輪廓面積比例。</param>
        /// <param name="minAspectRatio">接受為牌面候選時允許的最小長寬比。</param>
        /// <param name="maxAspectRatio">接受為牌面候選時允許的最大長寬比。</param>
        /// <param name="normalizedLongSide">標準化牌面長邊輸出尺寸。</param>
        /// <param name="normalizedShortSide">標準化牌面短邊輸出尺寸。</param>
        /// <param name="cornerWidthRatio">角落資訊區寬度占標準化牌面的比例。</param>
        /// <param name="cornerHeightRatio">角落資訊區高度占標準化牌面的比例。</param>
        /// <param name="drawAllContours">若為 true，會用綠色畫出所有候選輪廓。</param>
        /// <returns>樸克牌偵測結果；如果輸入無效則回傳 null。</returns>
        public static PlayingCardDetectionResult Detect(
            Texture2D source,
            double cannyThreshold1,
            double cannyThreshold2,
            int blurSize,
            float minAreaRatio,
            float minAspectRatio,
            float maxAspectRatio,
            int normalizedLongSide,
            int normalizedShortSide,
            float cornerWidthRatio,
            float cornerHeightRatio,
            bool drawAllContours)
        {
            if (source == null)
            {
                Debug.LogError("PlayingCardDetector.Detect failed: source texture is null.");
                return null;
            }

            using (Mat rgbaMat = new Mat(source.height, source.width, CvType.CV_8UC4))
            using (Mat grayMat = new Mat())
            using (Mat preparedGrayMat = new Mat())
            using (Mat edgeMat = new Mat())
            using (Mat whiteCardMask = new Mat())
            using (Mat debugMat = new Mat())
            {
                OpenCVMatUtils.Texture2DToMat(source, rgbaMat);
                rgbaMat.copyTo(debugMat);

                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                PrepareGray(grayMat, preparedGrayMat, blurSize);
                Imgproc.Canny(preparedGrayMat, edgeMat, cannyThreshold1, cannyThreshold2);

                List<MatOfPoint> contours = FindContours(edgeMat);
                CardCandidate bestCandidate = FindBestCardCandidate(
                    contours,
                    source.width,
                    source.height,
                    minAreaRatio,
                    minAspectRatio,
                    maxAspectRatio,
                    out float largestContourAreaRatio,
                    out float largestContourAspectRatio,
                    out string rejectionReason);

                int contourCount = contours.Count;

                if (bestCandidate == null)
                {
                    CreateWhiteCardMask(rgbaMat, whiteCardMask);
                    List<MatOfPoint> whiteContours = FindContours(whiteCardMask);
                    CardCandidate whiteCandidate = FindBestCardCandidate(
                        whiteContours,
                        source.width,
                        source.height,
                        minAreaRatio,
                        minAspectRatio,
                        maxAspectRatio,
                        out float whiteLargestAreaRatio,
                        out float whiteLargestAspectRatio,
                        out string whiteRejectionReason);

                    contourCount += whiteContours.Count;
                    if (whiteCandidate != null)
                    {
                        bestCandidate = whiteCandidate;
                        largestContourAreaRatio = whiteLargestAreaRatio;
                        largestContourAspectRatio = whiteLargestAspectRatio;
                        rejectionReason = "Passed by white card fallback";
                    }
                    else
                    {
                        rejectionReason = $"{rejectionReason}; white fallback: {whiteRejectionReason}";
                    }

                    if (drawAllContours && whiteContours.Count > 0)
                        Imgproc.drawContours(debugMat, whiteContours, -1, new Scalar(255, 255, 0, 255), 1);

                    DisposeContours(whiteContours);
                }

                if (drawAllContours && contours.Count > 0)
                    Imgproc.drawContours(debugMat, contours, -1, new Scalar(0, 255, 0, 255), 1);

                PlayingCardDetectionResult result = CreateResult(
                    bestCandidate,
                    contourCount,
                    source.width,
                    source.height,
                    largestContourAreaRatio,
                    largestContourAspectRatio,
                    rejectionReason);

                if (bestCandidate != null)
                {
                    DrawDetectedCard(debugMat, bestCandidate);
                    CreateStageTwoTextures(rgbaMat, bestCandidate, result, normalizedLongSide, normalizedShortSide, cornerWidthRatio, cornerHeightRatio);
                }

                result.DebugTexture = MatToTexture(debugMat);
                DisposeContours(contours);

                return result;
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
        /// 建立白底牌面的遮罩；給真實相機畫面作為 Canny 失敗時的備援。
        /// </summary>
        /// <param name="rgbaMat">輸入的 RGBA Mat。</param>
        /// <param name="whiteCardMask">輸出的白色牌面遮罩。</param>
        private static void CreateWhiteCardMask(Mat rgbaMat, Mat whiteCardMask)
        {
            using (Mat rgbMat = new Mat())
            using (Mat hsvMat = new Mat())
            using (Mat kernel = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(9, 9)))
            {
                Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);

                Core.inRange(hsvMat, new Scalar(0, 0, 120), new Scalar(180, 85, 255), whiteCardMask);
                Imgproc.morphologyEx(whiteCardMask, whiteCardMask, Imgproc.MORPH_CLOSE, kernel);
                Imgproc.morphologyEx(whiteCardMask, whiteCardMask, Imgproc.MORPH_OPEN, kernel);
            }
        }

        /// <summary>
        /// 從邊緣圖片中尋找外部輪廓。
        /// </summary>
        /// <param name="edgeMat">單通道邊緣 Mat。</param>
        /// <returns>偵測到的輪廓。</returns>
        private static List<MatOfPoint> FindContours(Mat edgeMat)
        {
            List<MatOfPoint> contours = new List<MatOfPoint>();
            using (Mat contourSource = edgeMat.clone())
            using (Mat hierarchy = new Mat())
            {
                Imgproc.findContours(contourSource, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);
            }

            return contours;
        }

        /// <summary>
        /// 尋找最像樸克牌的四邊形候選輪廓。
        /// </summary>
        /// <param name="contours">偵測到的輪廓。</param>
        /// <param name="width">來源圖片寬度。</param>
        /// <param name="height">來源圖片高度。</param>
        /// <param name="minAreaRatio">接受為牌面時需要達到的最小面積比例。</param>
        /// <param name="minAspectRatio">接受為牌面時允許的最小長寬比。</param>
        /// <param name="maxAspectRatio">接受為牌面時允許的最大長寬比。</param>
        /// <param name="largestContourAreaRatio">輸出最大輪廓面積比例。</param>
        /// <param name="largestContourAspectRatio">輸出最大輪廓長寬比。</param>
        /// <param name="rejectionReason">輸出主要拒絕原因。</param>
        /// <returns>最佳牌面候選；如果沒有符合條件的輪廓則回傳 null。</returns>
        private static CardCandidate FindBestCardCandidate(
            List<MatOfPoint> contours,
            int width,
            int height,
            float minAreaRatio,
            float minAspectRatio,
            float maxAspectRatio,
            out float largestContourAreaRatio,
            out float largestContourAspectRatio,
            out string rejectionReason)
        {
            CardCandidate bestCandidate = null;
            double imageArea = width * height;
            double minArea = imageArea * Mathf.Clamp01(minAreaRatio);
            double largestContourArea = 0;
            largestContourAreaRatio = 0f;
            largestContourAspectRatio = 0f;
            rejectionReason = contours.Count == 0 ? "No contours" : "No accepted card contour";

            for (int i = 0; i < contours.Count; i++)
            {
                double area = Imgproc.contourArea(contours[i]);
                OpenCVForUnity.CoreModule.Rect contourBounds = Imgproc.boundingRect(contours[i]);
                float contourAspect = GetNormalizedAspect(contourBounds);

                if (area > largestContourArea)
                {
                    largestContourArea = area;
                    largestContourAreaRatio = (float)(area / imageArea);
                    largestContourAspectRatio = contourAspect;
                }

                if (area < minArea)
                {
                    rejectionReason = $"Area ratio {largestContourAreaRatio:P1} below min {minAreaRatio:P1}";
                    continue;
                }

                using (MatOfPoint2f contour2f = new MatOfPoint2f(contours[i].toArray()))
                using (MatOfPoint2f approx2f = new MatOfPoint2f())
                {
                    double perimeter = Imgproc.arcLength(contour2f, true);
                    Imgproc.approxPolyDP(contour2f, approx2f, perimeter * 0.02, true);

                    Point[] points = approx2f.toArray();
                    if (points.Length == 4)
                    {
                        using (MatOfPoint approxPoint = new MatOfPoint(points))
                        {
                            if (!Imgproc.isContourConvex(approxPoint))
                            {
                                rejectionReason = "Quad contour is not convex";
                                continue;
                            }

                            OpenCVForUnity.CoreModule.Rect bounds = Imgproc.boundingRect(approxPoint);
                            if (!IsCardLikeAspect(bounds, minAspectRatio, maxAspectRatio))
                            {
                                rejectionReason = $"Aspect {GetNormalizedAspect(bounds):F2} outside {minAspectRatio:F2}-{maxAspectRatio:F2}";
                                continue;
                            }

                            if (bestCandidate == null || area > bestCandidate.Area)
                            {
                                bestCandidate = new CardCandidate
                                {
                                    Area = area,
                                    Bounds = bounds,
                                    Points = points
                                };
                            }
                        }
                    }
                    else
                    {
                        OpenCVForUnity.CoreModule.Rect bounds = Imgproc.boundingRect(contours[i]);
                        if (!IsCardLikeAspect(bounds, minAspectRatio, maxAspectRatio))
                        {
                            rejectionReason = $"Aspect {GetNormalizedAspect(bounds):F2} outside {minAspectRatio:F2}-{maxAspectRatio:F2}";
                            continue;
                        }

                        if (bestCandidate == null || area > bestCandidate.Area)
                        {
                            bestCandidate = new CardCandidate
                            {
                                Area = area,
                                Bounds = bounds,
                                Points = CreateBoundsPoints(bounds)
                            };
                        }
                    }
                }
            }

            if (bestCandidate != null)
                rejectionReason = "Passed";

            return bestCandidate;
        }

        /// <summary>
        /// 判斷外接矩形比例是否接近一般樸克牌。
        /// </summary>
        /// <param name="bounds">要檢查的外接矩形。</param>
        /// <returns>比例合理時回傳 true。</returns>
        private static bool IsCardLikeAspect(OpenCVForUnity.CoreModule.Rect bounds, float minAspectRatio, float maxAspectRatio)
        {
            float normalizedAspect = GetNormalizedAspect(bounds);
            float safeMinAspectRatio = Mathf.Max(1f, minAspectRatio);
            float safeMaxAspectRatio = Mathf.Max(safeMinAspectRatio, maxAspectRatio);
            return normalizedAspect >= safeMinAspectRatio && normalizedAspect <= safeMaxAspectRatio;
        }

        /// <summary>
        /// 取得不分橫式或直式的長寬比。
        /// </summary>
        /// <param name="bounds">要計算的外接矩形。</param>
        /// <returns>大於等於 1 的長寬比。</returns>
        private static float GetNormalizedAspect(OpenCVForUnity.CoreModule.Rect bounds)
        {
            float aspect = bounds.width / (float)Mathf.Max(1, bounds.height);
            return aspect < 1f ? 1f / aspect : aspect;
        }

        /// <summary>
        /// 從外接矩形建立四個角點。
        /// </summary>
        /// <param name="bounds">來源外接矩形。</param>
        /// <returns>矩形四角點。</returns>
        private static Point[] CreateBoundsPoints(OpenCVForUnity.CoreModule.Rect bounds)
        {
            return new[]
            {
                new Point(bounds.x, bounds.y),
                new Point(bounds.x + bounds.width, bounds.y),
                new Point(bounds.x + bounds.width, bounds.y + bounds.height),
                new Point(bounds.x, bounds.y + bounds.height)
            };
        }

        /// <summary>
        /// 依照偵測到的牌面角點建立標準化牌面圖與四個角落資訊區。
        /// </summary>
        /// <param name="sourceRgbaMat">來源 RGBA Mat。</param>
        /// <param name="candidate">偵測到的牌面候選。</param>
        /// <param name="result">要寫入輸出貼圖的偵測結果。</param>
        /// <param name="normalizedLongSide">標準化牌面長邊輸出尺寸。</param>
        /// <param name="normalizedShortSide">標準化牌面短邊輸出尺寸。</param>
        /// <param name="cornerWidthRatio">角落資訊區寬度占標準化牌面的比例。</param>
        /// <param name="cornerHeightRatio">角落資訊區高度占標準化牌面的比例。</param>
        private static void CreateStageTwoTextures(
            Mat sourceRgbaMat,
            CardCandidate candidate,
            PlayingCardDetectionResult result,
            int normalizedLongSide,
            int normalizedShortSide,
            float cornerWidthRatio,
            float cornerHeightRatio)
        {
            int safeLongSide = Mathf.Max(32, normalizedLongSide);
            int safeShortSide = Mathf.Max(32, normalizedShortSide);
            bool isLandscape = candidate.Bounds.width >= candidate.Bounds.height;
            int outputWidth = isLandscape ? safeLongSide : safeShortSide;
            int outputHeight = isLandscape ? safeShortSide : safeLongSide;

            using (Mat warpedCardMat = WarpCard(sourceRgbaMat, candidate.Points, outputWidth, outputHeight))
            {
                result.NormalizedCardTexture = MatToTexture(warpedCardMat);
                CreateCornerTextures(warpedCardMat, result, cornerWidthRatio, cornerHeightRatio);
            }
        }

        /// <summary>
        /// 將偵測到的牌面透視校正成固定尺寸。
        /// </summary>
        /// <param name="sourceRgbaMat">來源 RGBA Mat。</param>
        /// <param name="points">牌面的四個角點。</param>
        /// <param name="outputWidth">輸出寬度。</param>
        /// <param name="outputHeight">輸出高度。</param>
        /// <returns>校正後的牌面 Mat。</returns>
        private static Mat WarpCard(Mat sourceRgbaMat, Point[] points, int outputWidth, int outputHeight)
        {
            Point[] orderedPoints = OrderPoints(points);
            Mat warpedMat = new Mat(outputHeight, outputWidth, CvType.CV_8UC4);

            using (MatOfPoint2f sourcePoints = new MatOfPoint2f(orderedPoints))
            using (MatOfPoint2f destinationPoints = new MatOfPoint2f(
                       new Point(0, 0),
                       new Point(outputWidth - 1, 0),
                       new Point(outputWidth - 1, outputHeight - 1),
                       new Point(0, outputHeight - 1)))
            using (Mat perspective = Imgproc.getPerspectiveTransform(sourcePoints, destinationPoints))
            {
                Imgproc.warpPerspective(sourceRgbaMat, warpedMat, perspective, new Size(outputWidth, outputHeight));
            }

            return warpedMat;
        }

        /// <summary>
        /// 將四個點排序為左上、右上、右下、左下。
        /// </summary>
        /// <param name="points">待排序的四個點。</param>
        /// <returns>排序後的四個點。</returns>
        private static Point[] OrderPoints(Point[] points)
        {
            Point topLeft = points[0];
            Point topRight = points[0];
            Point bottomRight = points[0];
            Point bottomLeft = points[0];

            double minSum = double.MaxValue;
            double maxSum = double.MinValue;
            double minDiff = double.MaxValue;
            double maxDiff = double.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                double sum = points[i].x + points[i].y;
                double diff = points[i].x - points[i].y;

                if (sum < minSum)
                {
                    minSum = sum;
                    topLeft = points[i];
                }

                if (sum > maxSum)
                {
                    maxSum = sum;
                    bottomRight = points[i];
                }

                if (diff < minDiff)
                {
                    minDiff = diff;
                    bottomLeft = points[i];
                }

                if (diff > maxDiff)
                {
                    maxDiff = diff;
                    topRight = points[i];
                }
            }

            return new[] { topLeft, topRight, bottomRight, bottomLeft };
        }

        /// <summary>
        /// 從標準化牌面切出四個角落資訊區。
        /// </summary>
        /// <param name="cardMat">標準化牌面 Mat。</param>
        /// <param name="result">要寫入角落貼圖的偵測結果。</param>
        /// <param name="cornerWidthRatio">角落資訊區寬度比例。</param>
        /// <param name="cornerHeightRatio">角落資訊區高度比例。</param>
        private static void CreateCornerTextures(Mat cardMat, PlayingCardDetectionResult result, float cornerWidthRatio, float cornerHeightRatio)
        {
            int cornerWidth = Mathf.Clamp(Mathf.RoundToInt(cardMat.cols() * cornerWidthRatio), 8, cardMat.cols());
            int cornerHeight = Mathf.Clamp(Mathf.RoundToInt(cardMat.rows() * cornerHeightRatio), 8, cardMat.rows());

            result.TopLeftCornerTexture = CropTexture(cardMat, 0, 0, cornerWidth, cornerHeight);
            result.TopRightCornerTexture = CropTexture(cardMat, cardMat.cols() - cornerWidth, 0, cornerWidth, cornerHeight);
            result.BottomLeftCornerTexture = CropTexture(cardMat, 0, cardMat.rows() - cornerHeight, cornerWidth, cornerHeight);
            result.BottomRightCornerTexture = CropTexture(cardMat, cardMat.cols() - cornerWidth, cardMat.rows() - cornerHeight, cornerWidth, cornerHeight);
        }

        /// <summary>
        /// 從 Mat 裁切指定區域並轉成 Texture2D。
        /// </summary>
        /// <param name="sourceMat">來源 Mat。</param>
        /// <param name="x">裁切起點 X。</param>
        /// <param name="y">裁切起點 Y。</param>
        /// <param name="width">裁切寬度。</param>
        /// <param name="height">裁切高度。</param>
        /// <returns>裁切結果貼圖。</returns>
        private static Texture2D CropTexture(Mat sourceMat, int x, int y, int width, int height)
        {
            using (Mat cropMat = new Mat(sourceMat, new OpenCVForUnity.CoreModule.Rect(x, y, width, height)))
            {
                return MatToTexture(cropMat);
            }
        }

        /// <summary>
        /// 由候選輪廓建立可序列化的偵測結果。
        /// </summary>
        /// <param name="candidate">選中的牌面候選。</param>
        /// <param name="contourCount">找到的輪廓數量。</param>
        /// <param name="width">來源圖片寬度。</param>
        /// <param name="height">來源圖片高度。</param>
        /// <returns>偵測結果。</returns>
        private static PlayingCardDetectionResult CreateResult(
            CardCandidate candidate,
            int contourCount,
            int width,
            int height,
            float largestContourAreaRatio,
            float largestContourAspectRatio,
            string rejectionReason)
        {
            PlayingCardDetectionResult result = new PlayingCardDetectionResult
            {
                IsDetected = candidate != null,
                ContourCount = contourCount,
                LargestContourAreaRatio = largestContourAreaRatio,
                LargestContourAspectRatio = largestContourAspectRatio,
                RejectionReason = rejectionReason
            };

            if (candidate == null)
                return result;

            result.CardArea = candidate.Area;
            result.CardAreaRatio = (float)(candidate.Area / (width * height));
            result.CardBounds = new Rect(candidate.Bounds.x, candidate.Bounds.y, candidate.Bounds.width, candidate.Bounds.height);
            result.Corners = new Vector2[candidate.Points.Length];

            for (int i = 0; i < candidate.Points.Length; i++)
                result.Corners[i] = new Vector2((float)candidate.Points[i].x, (float)candidate.Points[i].y);

            return result;
        }

        /// <summary>
        /// 將選中的牌面候選畫到 debug Mat 上。
        /// </summary>
        /// <param name="debugMat">要繪製內容的 debug Mat。</param>
        /// <param name="candidate">偵測到的牌面候選。</param>
        private static void DrawDetectedCard(Mat debugMat, CardCandidate candidate)
        {
            using (MatOfPoint cardContour = new MatOfPoint(candidate.Points))
            {
                Imgproc.drawContours(debugMat, new List<MatOfPoint> { cardContour }, -1, new Scalar(255, 0, 0, 255), 4);

                for (int i = 0; i < candidate.Points.Length; i++)
                    Imgproc.circle(debugMat, candidate.Points[i], 8, new Scalar(255, 255, 0, 255), -1);
            }
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

        private class CardCandidate
        {
            public double Area;
            public OpenCVForUnity.CoreModule.Rect Bounds;
            public Point[] Points;
        }
    }
}

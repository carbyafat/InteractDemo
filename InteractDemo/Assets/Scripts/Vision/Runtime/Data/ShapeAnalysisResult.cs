using UnityEngine;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 儲存形狀處理的 debug 輸出與基本輪廓統計。
    /// </summary>
    [System.Serializable]
    public class ShapeAnalysisResult
    {
        /// <summary>
        /// 產生此結果時使用的 debug 模式。
        /// </summary>
        public VisionDebugMode DebugMode;

        /// <summary>
        /// 用於視覺化 debug 顯示的輸出貼圖。
        /// </summary>
        public Texture2D OutputTexture;

        /// <summary>
        /// 在處理後圖片中偵測到的輪廓數量。
        /// </summary>
        public int ContourCount;

        /// <summary>
        /// 最大輪廓的面積，單位為像素。
        /// </summary>
        public double LargestContourArea;

        /// <summary>
        /// 最大輪廓面積相對於整張圖片的比例。
        /// </summary>
        public float LargestContourAreaRatio;

        /// <summary>
        /// 最大輪廓的外接矩形，使用 OpenCV 像素座標。
        /// </summary>
        public Rect LargestContourBounds;

        /// <summary>
        /// 將結果格式化成 Console 或 UI 文字。
        /// </summary>
        /// <returns>方便閱讀的形狀分析摘要。</returns>
        public override string ToString()
        {
            return $"Debug Mode: {DebugMode}\n" +
                   $"Contours: {ContourCount}\n" +
                   $"Largest Area: {LargestContourArea:F1}\n" +
                   $"Largest Area Ratio: {LargestContourAreaRatio:P1}\n" +
                   $"Largest Bounds: x={LargestContourBounds.x:F0}, y={LargestContourBounds.y:F0}, w={LargestContourBounds.width:F0}, h={LargestContourBounds.height:F0}";
        }
    }
}

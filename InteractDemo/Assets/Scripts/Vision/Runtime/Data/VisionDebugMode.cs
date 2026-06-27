namespace InteractDemo.Vision
{
    /// <summary>
    /// 形狀圖片處理器支援的 debug 顯示模式。
    /// </summary>
    public enum VisionDebugMode
    {
        /// <summary>顯示原始來源圖片。</summary>
        Original,

        /// <summary>顯示來源圖片的灰階版本。</summary>
        Grayscale,

        /// <summary>顯示二值化後的圖片。</summary>
        Threshold,

        /// <summary>顯示 Canny 邊緣偵測結果。</summary>
        Canny,

        /// <summary>在來源圖片上顯示所有偵測到的輪廓。</summary>
        AllContours,

        /// <summary>只在來源圖片上顯示最大的輪廓。</summary>
        LargestContour
    }
}

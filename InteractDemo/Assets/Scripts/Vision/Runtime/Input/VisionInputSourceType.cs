namespace InteractDemo.Vision
{
    /// <summary>
    /// 影像分析測試入口支援的輸入來源類型。
    /// </summary>
    public enum VisionInputSourceType
    {
        /// <summary>
        /// 分析直接在 Inspector 指定的 Texture2D。
        /// </summary>
        Texture2D,

        /// <summary>
        /// 分析 Unity UI Image 使用的 Sprite 貼圖。
        /// </summary>
        UIImage,

        /// <summary>
        /// 分析 Unity UI RawImage 顯示的貼圖。
        /// </summary>
        RawImage,

        /// <summary>
        /// 分析從 WebCamTexture 擷取的一幀畫面。
        /// </summary>
        WebCamTexture
    }
}

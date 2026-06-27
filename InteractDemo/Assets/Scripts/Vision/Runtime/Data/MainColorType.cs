namespace InteractDemo.Vision
{
    /// <summary>
    /// 圖片顏色分析會輸出的細分顏色類別。
    /// </summary>
    public enum MainColorType
    {
        /// <summary>明顯的紅色色相。</summary>
        Red,

        /// <summary>介於紅色與黃色之間的橘色色相。</summary>
        Orange,

        /// <summary>黃色色相。</summary>
        Yellow,

        /// <summary>黃綠色色相。</summary>
        Lime,

        /// <summary>綠色色相。</summary>
        Green,

        /// <summary>藍綠色色相。</summary>
        Cyan,

        /// <summary>淺藍色色相。</summary>
        SkyBlue,

        /// <summary>藍色色相。</summary>
        Blue,

        /// <summary>紫色色相。</summary>
        Purple,

        /// <summary>紅紫色色相。</summary>
        Magenta,

        /// <summary>粉紅色色相。</summary>
        Pink,

        /// <summary>偏暗、低飽和的橘色或黃色。</summary>
        Brown,

        /// <summary>偏亮、低飽和的橘色或黃色。</summary>
        Beige,

        /// <summary>非常明亮且低飽和的顏色。</summary>
        White,

        /// <summary>偏亮、低飽和的灰色。</summary>
        LightGray,

        /// <summary>中等亮度、低飽和的灰色。</summary>
        Gray,

        /// <summary>偏暗、低飽和的灰色。</summary>
        DarkGray,

        /// <summary>非常暗的顏色。</summary>
        Black,

        /// <summary>沒有找到有意義的不透明像素。</summary>
        Transparent,

        /// <summary>沒有任何單一顏色分類足夠突出。</summary>
        Mixed,

        /// <summary>無法分類輸入時使用的保底類別。</summary>
        Unknown
    }
}

namespace InteractDemo.Vision
{
    /// <summary>
    /// 儲存某個顏色分類在分析圖片中的比例。
    /// </summary>
    [System.Serializable]
    public class ColorRatio
    {
        /// <summary>
        /// 此比例代表的顏色分類。
        /// </summary>
        public MainColorType ColorType;

        /// <summary>
        /// 被分類為此顏色的有效像素比例，範圍 0-1。
        /// </summary>
        public float Ratio;

        /// <summary>
        /// 建立一筆顏色比例資料。
        /// </summary>
        /// <param name="colorType">此資料代表的顏色分類。</param>
        /// <param name="ratio">有效像素比例，範圍 0-1。</param>
        public ColorRatio(MainColorType colorType, float ratio)
        {
            ColorType = colorType;
            Ratio = ratio;
        }
    }
}

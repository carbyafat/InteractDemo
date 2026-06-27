namespace InteractDemo.Vision
{
    /// <summary>
    /// Stores the ratio of one color category in an analyzed image.
    /// </summary>
    [System.Serializable]
    public class ColorRatio
    {
        /// <summary>
        /// Color category represented by this ratio.
        /// </summary>
        public MainColorType ColorType;

        /// <summary>
        /// Ratio of valid pixels classified as this color, from 0 to 1.
        /// </summary>
        public float Ratio;

        /// <summary>
        /// Creates a color ratio entry.
        /// </summary>
        /// <param name="colorType">Color category represented by the entry.</param>
        /// <param name="ratio">Ratio of valid pixels in 0-1 range.</param>
        public ColorRatio(MainColorType colorType, float ratio)
        {
            ColorType = colorType;
            Ratio = ratio;
        }
    }
}

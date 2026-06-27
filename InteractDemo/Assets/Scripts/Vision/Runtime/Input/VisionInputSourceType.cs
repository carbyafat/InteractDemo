namespace InteractDemo.Vision
{
    /// <summary>
    /// Input source types supported by the RGB analysis test entry.
    /// </summary>
    public enum VisionInputSourceType
    {
        /// <summary>
        /// Analyze a Texture2D assigned directly in the Inspector.
        /// </summary>
        Texture2D,

        /// <summary>
        /// Analyze the Sprite texture used by a Unity UI Image.
        /// </summary>
        UIImage,

        /// <summary>
        /// Analyze the texture displayed by a Unity UI RawImage.
        /// </summary>
        RawImage,

        /// <summary>
        /// Analyze a frame captured from a WebCamTexture.
        /// </summary>
        WebCamTexture
    }
}

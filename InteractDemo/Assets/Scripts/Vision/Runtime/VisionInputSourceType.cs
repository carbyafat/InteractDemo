namespace InteractDemo.Vision
{
    // All supported first-stage input sources for image analysis.
    // The analyzer still receives Texture2D; this enum only describes where that Texture2D comes from.
    public enum VisionInputSourceType
    {
        Texture2D,
        UIImage,
        RawImage,
        WebCamTexture
    }
}

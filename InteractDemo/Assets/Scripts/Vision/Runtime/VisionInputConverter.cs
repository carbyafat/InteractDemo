using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    /// <summary>
    /// Converts Unity image source types into Texture2D for analysis.
    /// </summary>
    public static class VisionInputConverter
    {
        /// <summary>
        /// Converts a UI Image source into a Texture2D.
        /// </summary>
        /// <param name="image">UI Image whose Sprite should be analyzed.</param>
        /// <returns>Texture2D for the Sprite area, or null if conversion fails.</returns>
        public static Texture2D FromImage(Image image)
        {
            if (image == null || image.sprite == null)
            {
                Debug.LogError("VisionInputConverter.FromImage failed: Image or Sprite is null.");
                return null;
            }

            Texture2D sourceTexture = image.sprite.texture;
            Rect textureRect = image.sprite.textureRect;

            if (Mathf.Approximately(textureRect.x, 0f) &&
                Mathf.Approximately(textureRect.y, 0f) &&
                Mathf.Approximately(textureRect.width, sourceTexture.width) &&
                Mathf.Approximately(textureRect.height, sourceTexture.height))
            {
                return sourceTexture;
            }

            Color[] pixels = sourceTexture.GetPixels(
                Mathf.RoundToInt(textureRect.x),
                Mathf.RoundToInt(textureRect.y),
                Mathf.RoundToInt(textureRect.width),
                Mathf.RoundToInt(textureRect.height));

            Texture2D croppedTexture = new Texture2D(
                Mathf.RoundToInt(textureRect.width),
                Mathf.RoundToInt(textureRect.height),
                TextureFormat.RGBA32,
                false);

            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();
            return croppedTexture;
        }

        /// <summary>
        /// Converts a RawImage source into a Texture2D.
        /// </summary>
        /// <param name="rawImage">RawImage whose texture should be analyzed.</param>
        /// <returns>Texture2D converted from the RawImage texture, or null if conversion fails.</returns>
        public static Texture2D FromRawImage(RawImage rawImage)
        {
            if (rawImage == null || rawImage.texture == null)
            {
                Debug.LogError("VisionInputConverter.FromRawImage failed: RawImage or texture is null.");
                return null;
            }

            return FromTexture(rawImage.texture);
        }

        /// <summary>
        /// Captures the current frame of a WebCamTexture as a Texture2D.
        /// </summary>
        /// <param name="webCamTexture">Running WebCamTexture to capture.</param>
        /// <returns>Texture2D containing the current camera frame, or null if unavailable.</returns>
        public static Texture2D FromWebCamTexture(WebCamTexture webCamTexture)
        {
            if (webCamTexture == null)
            {
                Debug.LogError("VisionInputConverter.FromWebCamTexture failed: WebCamTexture is null.");
                return null;
            }

            if (!webCamTexture.isPlaying)
            {
                Debug.LogError("VisionInputConverter.FromWebCamTexture failed: WebCamTexture is not playing.");
                return null;
            }

            if (webCamTexture.width <= 16 || webCamTexture.height <= 16)
            {
                Debug.LogError("VisionInputConverter.FromWebCamTexture failed: WebCamTexture is not ready yet.");
                return null;
            }

            Texture2D texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            texture.SetPixels32(webCamTexture.GetPixels32());
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Converts a general Unity Texture into a Texture2D when the texture type is supported.
        /// </summary>
        /// <param name="texture">Source texture, such as Texture2D, WebCamTexture, or RenderTexture.</param>
        /// <returns>Texture2D converted from the source texture, or null if unsupported.</returns>
        public static Texture2D FromTexture(Texture texture)
        {
            if (texture == null)
            {
                Debug.LogError("VisionInputConverter.FromTexture failed: texture is null.");
                return null;
            }

            if (texture is Texture2D texture2D)
                return texture2D;

            if (texture is WebCamTexture webCamTexture)
                return FromWebCamTexture(webCamTexture);

            if (texture is RenderTexture renderTexture)
                return FromRenderTexture(renderTexture);

            Debug.LogError($"VisionInputConverter.FromTexture failed: unsupported texture type {texture.GetType().Name}.");
            return null;
        }

        /// <summary>
        /// Reads a RenderTexture into a Texture2D.
        /// </summary>
        /// <param name="renderTexture">RenderTexture to read from.</param>
        /// <returns>Texture2D containing the RenderTexture pixels, or null if input is invalid.</returns>
        public static Texture2D FromRenderTexture(RenderTexture renderTexture)
        {
            if (renderTexture == null)
            {
                Debug.LogError("VisionInputConverter.FromRenderTexture failed: RenderTexture is null.");
                return null;
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            RenderTexture.active = previous;
            return texture;
        }
    }
}

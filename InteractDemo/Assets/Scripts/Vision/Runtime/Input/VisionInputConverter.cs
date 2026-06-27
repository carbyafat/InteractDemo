using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 將 Unity 的各種影像來源轉換成可分析的 Texture2D。
    /// </summary>
    public static class VisionInputConverter
    {
        /// <summary>
        /// 將 UI Image 來源轉換成 Texture2D。
        /// </summary>
        /// <param name="image">要分析其 Sprite 的 UI Image。</param>
        /// <returns>Sprite 範圍對應的 Texture2D；如果轉換失敗則回傳 null。</returns>
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
        /// 將 RawImage 來源轉換成 Texture2D。
        /// </summary>
        /// <param name="rawImage">要分析其 texture 的 RawImage。</param>
        /// <returns>由 RawImage texture 轉換出的 Texture2D；如果轉換失敗則回傳 null。</returns>
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
        /// 將 WebCamTexture 目前畫面擷取成 Texture2D。
        /// </summary>
        /// <param name="webCamTexture">正在播放、準備擷取畫面的 WebCamTexture。</param>
        /// <returns>包含目前相機畫面的 Texture2D；如果不可用則回傳 null。</returns>
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
        /// 在類型支援時，將一般 Unity Texture 轉換成 Texture2D。
        /// </summary>
        /// <param name="texture">來源貼圖，例如 Texture2D、WebCamTexture 或 RenderTexture。</param>
        /// <returns>由來源貼圖轉換出的 Texture2D；如果不支援則回傳 null。</returns>
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
        /// 將 RenderTexture 讀取成 Texture2D。
        /// </summary>
        /// <param name="renderTexture">要讀取的 RenderTexture。</param>
        /// <returns>包含 RenderTexture 像素的 Texture2D；如果輸入無效則回傳 null。</returns>
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

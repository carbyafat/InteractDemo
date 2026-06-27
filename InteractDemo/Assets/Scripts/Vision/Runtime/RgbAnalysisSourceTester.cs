using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    // Unified test entry for different image source types.
    // This is the recommended playground script after the first Texture2D-only test works.
    public class RgbAnalysisSourceTester : MonoBehaviour
    {
        [Header("Input Source")]
        [SerializeField] private VisionInputSourceType sourceType = VisionInputSourceType.Texture2D;
        [SerializeField] private Texture2D textureSource;
        [SerializeField] private Image imageSource;
        [SerializeField] private RawImage rawImageSource;
        [SerializeField] private WebCamTexture webCamTextureSource;

        [Header("Optional Output")]
        [SerializeField] private RawImage sourcePreview;
        [SerializeField] private Image averageColorPreview;
        [SerializeField] private Text resultText;

        [Header("Run")]
        [SerializeField] private bool analyzeOnStart = true;

        private Texture2D runtimeTexture;

        private void Start()
        {
            if (analyzeOnStart)
                AnalyzeSelectedSource();
        }

        private void OnDestroy()
        {
            if (runtimeTexture != null)
            {
                Destroy(runtimeTexture);
                runtimeTexture = null;
            }
        }

        [ContextMenu("Analyze Selected Source")]
        public void AnalyzeSelectedSource()
        {
            Texture2D source = GetSelectedSourceTexture();
            if (source == null)
                return;

            if (sourcePreview != null)
                sourcePreview.texture = source;

            RgbAnalysisResult result = RgbImageAnalyzer.Analyze(source);
            if (result == null)
                return;

            Debug.Log(result.ToString());

            if (averageColorPreview != null)
                averageColorPreview.color = result.AverageColor;

            if (resultText != null)
                resultText.text = result.ToString();
        }

        private Texture2D GetSelectedSourceTexture()
        {
            if (runtimeTexture != null)
            {
                Destroy(runtimeTexture);
                runtimeTexture = null;
            }

            switch (sourceType)
            {
                case VisionInputSourceType.Texture2D:
                    return textureSource;

                case VisionInputSourceType.UIImage:
                    runtimeTexture = VisionInputConverter.FromImage(imageSource);
                    return runtimeTexture;

                case VisionInputSourceType.RawImage:
                    runtimeTexture = VisionInputConverter.FromRawImage(rawImageSource);
                    return runtimeTexture;

                case VisionInputSourceType.WebCamTexture:
                    runtimeTexture = VisionInputConverter.FromWebCamTexture(webCamTextureSource);
                    return runtimeTexture;

                default:
                    Debug.LogError($"Unsupported source type: {sourceType}");
                    return null;
            }
        }
    }
}

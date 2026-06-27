using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    /// <summary>
    /// Test component for analyzing one selected source type through a shared Texture2D pipeline.
    /// </summary>
    public class RgbAnalysisSourceTester : MonoBehaviour
    {
        [Header("Input Source")]
        [Tooltip("Type of image source to convert and analyze.")]
        [SerializeField] private VisionInputSourceType sourceType = VisionInputSourceType.Texture2D;

        [Tooltip("Texture2D used when Source Type is Texture2D.")]
        [SerializeField] private Texture2D textureSource;

        [Tooltip("UI Image used when Source Type is UIImage.")]
        [SerializeField] private Image imageSource;

        [Tooltip("RawImage used when Source Type is RawImage.")]
        [SerializeField] private RawImage rawImageSource;

        [Tooltip("WebCamTexture used when Source Type is WebCamTexture.")]
        [SerializeField] private WebCamTexture webCamTextureSource;

        [Header("Optional Output")]
        [Tooltip("Optional RawImage used to preview the converted Texture2D.")]
        [SerializeField] private RawImage sourcePreview;

        [Tooltip("Optional Image whose color will be set to the calculated average color.")]
        [SerializeField] private Image averageColorPreview;

        [Tooltip("Optional Text used to display the analysis result.")]
        [SerializeField] private Text resultText;

        [Header("Run")]
        [Tooltip("If true, analyzes the selected source when Play mode starts.")]
        [SerializeField] private bool analyzeOnStart = true;

        /// <summary>
        /// Temporary Texture2D created by source conversion and destroyed by this component.
        /// </summary>
        private Texture2D runtimeTexture;

        /// <summary>
        /// Unity lifecycle method that optionally runs the analysis at startup.
        /// </summary>
        private void Start()
        {
            if (analyzeOnStart)
                AnalyzeSelectedSource();
        }

        /// <summary>
        /// Unity lifecycle method used to release converted runtime textures.
        /// </summary>
        private void OnDestroy()
        {
            if (runtimeTexture != null)
            {
                Destroy(runtimeTexture);
                runtimeTexture = null;
            }
        }

        /// <summary>
        /// Converts the selected input source, analyzes it, and updates optional UI.
        /// </summary>
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

        /// <summary>
        /// Gets the Texture2D for the currently selected source type.
        /// </summary>
        /// <returns>Texture2D to analyze, or null if conversion fails.</returns>
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

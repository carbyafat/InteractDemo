using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    /// <summary>
    /// Test component for analyzing a Texture2D assigned in the Inspector.
    /// </summary>
    public class TextureRgbAnalysisTester : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Texture2D image to analyze.")]
        [SerializeField] private Texture2D sourceTexture;

        [Header("Optional Output")]
        [Tooltip("Optional RawImage used to preview the analyzed texture.")]
        [SerializeField] private RawImage sourcePreview;

        [Tooltip("Optional Image whose color will be set to the calculated average color.")]
        [SerializeField] private Image averageColorPreview;

        [Tooltip("Optional Text used to display the analysis result.")]
        [SerializeField] private Text resultText;

        [Header("Run")]
        [Tooltip("If true, analyzes the assigned texture when Play mode starts.")]
        [SerializeField] private bool analyzeOnStart = true;

        /// <summary>
        /// Unity lifecycle method that optionally runs the analysis at startup.
        /// </summary>
        private void Start()
        {
            if (analyzeOnStart)
                AnalyzeAssignedTexture();
        }

        /// <summary>
        /// Analyzes the assigned Texture2D and updates optional preview UI.
        /// </summary>
        [ContextMenu("Analyze Assigned Texture")]
        public void AnalyzeAssignedTexture()
        {
            if (sourcePreview != null)
                sourcePreview.texture = sourceTexture;

            RgbAnalysisResult result = RgbImageAnalyzer.Analyze(sourceTexture);
            if (result == null)
                return;

            Debug.Log(result.ToString());

            if (averageColorPreview != null)
                averageColorPreview.color = result.AverageColor;

            if (resultText != null)
                resultText.text = result.ToString();
        }
    }
}

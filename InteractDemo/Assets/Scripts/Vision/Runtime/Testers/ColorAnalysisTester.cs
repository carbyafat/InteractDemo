using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    /// <summary>
    /// Test component for classifying the main color of a Texture2D.
    /// </summary>
    public class ColorAnalysisTester : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Texture2D image to classify.")]
        [SerializeField] private Texture2D sourceTexture;

        [Header("Optional Output")]
        [Tooltip("Optional RawImage used to preview the analyzed texture.")]
        [SerializeField] private RawImage sourcePreview;

        [Tooltip("Optional Image whose color will be set to the final main color preview.")]
        [SerializeField] private Image mainColorPreview;

        [Tooltip("Optional Image whose color will be set to the average color of valid pixels.")]
        [SerializeField] private Image averageColorPreview;

        [Tooltip("Optional Text used to display the classification result.")]
        [SerializeField] private Text resultText;

        [Header("Run")]
        [Tooltip("If true, analyzes the assigned texture when Play mode starts.")]
        [SerializeField] private bool analyzeOnStart = true;

        /// <summary>
        /// Unity lifecycle method that optionally runs the color analysis at startup.
        /// </summary>
        private void Start()
        {
            if (analyzeOnStart)
                AnalyzeAssignedTexture();
        }

        /// <summary>
        /// Analyzes the assigned Texture2D and updates optional debug UI.
        /// </summary>
        [ContextMenu("Analyze Assigned Texture")]
        public void AnalyzeAssignedTexture()
        {
            if (sourcePreview != null)
                sourcePreview.texture = sourceTexture;

            ColorAnalysisResult result = ColorImageAnalyzer.Analyze(sourceTexture);
            if (result == null)
                return;

            Debug.Log(result.ToString());

            if (mainColorPreview != null)
                mainColorPreview.color = result.MainColorPreview;

            if (averageColorPreview != null)
                averageColorPreview.color = result.AverageColor;

            if (resultText != null)
                resultText.text = result.ToString();
        }
    }
}

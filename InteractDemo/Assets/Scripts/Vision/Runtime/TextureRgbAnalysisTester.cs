using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    // Scene-facing test component for the first OpenCV milestone.
    // Attach this to a GameObject, assign Source Texture in the Inspector,
    // then press Play to analyze that image.
    public class TextureRgbAnalysisTester : MonoBehaviour
    {
        [Header("Input")]
        // The image we want to analyze. For now this is assigned manually in Inspector.
        // Later this input can be replaced by camera capture, screenshots, or file import.
        [SerializeField] private Texture2D sourceTexture;

        [Header("Optional Output")]
        // Optional UI preview of the source image. The analyzer does not need this;
        // it is only for easier visual debugging.
        [SerializeField] private RawImage sourcePreview;

        // Optional UI block that displays the calculated average color.
        [SerializeField] private Image averageColorPreview;

        // Optional UI text output. If this is not assigned, the result still appears in Console.
        [SerializeField] private Text resultText;

        [Header("Run")]
        // If true, analysis runs automatically when Play mode starts.
        // If false, use the component context menu or call AnalyzeAssignedTexture from a button.
        [SerializeField] private bool analyzeOnStart = true;

        private void Start()
        {
            if (analyzeOnStart)
                AnalyzeAssignedTexture();
        }

        // Adds a right-click menu item on this component in the Inspector.
        // Useful for rerunning the test without adding a UI Button.
        [ContextMenu("Analyze Assigned Texture")]
        public void AnalyzeAssignedTexture()
        {
            // Show the source texture if a RawImage was assigned.
            if (sourcePreview != null)
                sourcePreview.texture = sourceTexture;

            // All OpenCV work is delegated to RgbImageAnalyzer.
            RgbAnalysisResult result = RgbImageAnalyzer.Analyze(sourceTexture);
            if (result == null)
                return;

            // Console output is the minimum debug feedback.
            Debug.Log(result.ToString());

            // Optional UI outputs for a friendlier test scene.
            if (averageColorPreview != null)
                averageColorPreview.color = result.AverageColor;

            if (resultText != null)
                resultText.text = result.ToString();
        }
    }
}

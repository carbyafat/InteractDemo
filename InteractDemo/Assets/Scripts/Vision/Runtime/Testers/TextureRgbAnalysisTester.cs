using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 用於測試 Inspector 指定 Texture2D 的 RGB 分析元件。
    /// </summary>
    public class TextureRgbAnalysisTester : MonoBehaviour
    {
        [Header("輸入")]
        [Tooltip("要分析的 Texture2D 圖片。")]
        [SerializeField] private Texture2D sourceTexture;

        [Header("可選輸出")]
        [Tooltip("可選的 RawImage，用來預覽被分析的貼圖。")]
        [SerializeField] private RawImage sourcePreview;

        [Tooltip("可選的 Image，顏色會設定為計算出的平均色。")]
        [SerializeField] private Image averageColorPreview;

        [Tooltip("可選的 TMP 文字，用來顯示分析結果。")]
        [SerializeField] private TMP_Text resultText;

        [Header("執行")]
        [Tooltip("若為 true，進入 Play 模式時會分析指定貼圖。")]
        [SerializeField] private bool analyzeOnStart = true;

        /// <summary>
        /// Unity 生命週期方法，可選擇在啟動時執行分析。
        /// </summary>
        private void Start()
        {
            if (analyzeOnStart)
                AnalyzeAssignedTexture();
        }

        /// <summary>
        /// 分析指定的 Texture2D，並更新可選的預覽 UI。
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

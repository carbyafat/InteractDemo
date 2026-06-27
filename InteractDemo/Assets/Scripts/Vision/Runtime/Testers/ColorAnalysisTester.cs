using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 用於測試 Texture2D 主色分類的元件。
    /// </summary>
    public class ColorAnalysisTester : MonoBehaviour
    {
        [Header("輸入")]
        [Tooltip("要分類主色的 Texture2D 圖片。")]
        [SerializeField] private Texture2D sourceTexture;

        [Header("可選輸出")]
        [Tooltip("可選的 RawImage，用來預覽被分析的貼圖。")]
        [SerializeField] private RawImage sourcePreview;

        [Tooltip("可選的 Image，顏色會設定為最終主色的預覽色。")]
        [SerializeField] private Image mainColorPreview;

        [Tooltip("可選的 Image，顏色會設定為有效像素的平均色。")]
        [SerializeField] private Image averageColorPreview;

        [Tooltip("可選的 TMP 文字，用來顯示分類結果。")]
        [SerializeField] private TMP_Text resultText;

        [Header("執行")]
        [Tooltip("若為 true，進入 Play 模式時會分析指定貼圖。")]
        [SerializeField] private bool analyzeOnStart = true;

        /// <summary>
        /// Unity 生命週期方法，可選擇在啟動時執行顏色分析。
        /// </summary>
        private void Start()
        {
            if (analyzeOnStart)
                AnalyzeAssignedTexture();
        }

        /// <summary>
        /// 分析指定的 Texture2D，並更新可選的 debug UI。
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

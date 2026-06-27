using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 用於測試不同輸入來源，並統一透過 Texture2D 流程進行 RGB 分析的元件。
    /// </summary>
    public class RgbAnalysisSourceTester : MonoBehaviour
    {
        [Header("輸入來源")]
        [Tooltip("要轉換並分析的影像來源類型。")]
        [SerializeField] private VisionInputSourceType sourceType = VisionInputSourceType.Texture2D;

        [Tooltip("Source Type 為 Texture2D 時使用的貼圖。")]
        [SerializeField] private Texture2D textureSource;

        [Tooltip("Source Type 為 UIImage 時使用的 UI Image。")]
        [SerializeField] private Image imageSource;

        [Tooltip("Source Type 為 RawImage 時使用的 RawImage。")]
        [SerializeField] private RawImage rawImageSource;

        [Tooltip("Source Type 為 WebCamTexture 時使用的 WebCamTexture。")]
        [SerializeField] private WebCamTexture webCamTextureSource;

        [Header("可選輸出")]
        [Tooltip("可選的 RawImage，用來預覽轉換後的 Texture2D。")]
        [SerializeField] private RawImage sourcePreview;

        [Tooltip("可選的 Image，顏色會設定為計算出的平均色。")]
        [SerializeField] private Image averageColorPreview;

        [Tooltip("可選的 TMP 文字，用來顯示分析結果。")]
        [SerializeField] private TMP_Text resultText;

        [Header("執行")]
        [Tooltip("若為 true，進入 Play 模式時會分析選擇的來源。")]
        [SerializeField] private bool analyzeOnStart = true;

        /// <summary>
        /// 由來源轉換產生的暫時 Texture2D，會由此元件負責銷毀。
        /// </summary>
        private Texture2D runtimeTexture;
        private bool ownsRuntimeTexture;

        /// <summary>
        /// Unity 生命週期方法，可選擇在啟動時執行分析。
        /// </summary>
        private void Start()
        {
            if (analyzeOnStart)
                AnalyzeSelectedSource();
        }

        /// <summary>
        /// Unity 生命週期方法，用來釋放執行時轉換出的貼圖。
        /// </summary>
        private void OnDestroy()
        {
            if (runtimeTexture != null)
            {
                if (ownsRuntimeTexture)
                    Destroy(runtimeTexture);

                runtimeTexture = null;
                ownsRuntimeTexture = false;
            }
        }

        /// <summary>
        /// 轉換選擇的輸入來源、執行分析，並更新可選 UI。
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
        /// 取得目前選擇來源類型對應的 Texture2D。
        /// </summary>
        /// <returns>要分析的 Texture2D；如果轉換失敗則回傳 null。</returns>
        private Texture2D GetSelectedSourceTexture()
        {
            if (runtimeTexture != null)
            {
                if (ownsRuntimeTexture)
                    Destroy(runtimeTexture);

                runtimeTexture = null;
                ownsRuntimeTexture = false;
            }

            switch (sourceType)
            {
                case VisionInputSourceType.Texture2D:
                    return textureSource;

                case VisionInputSourceType.UIImage:
                    runtimeTexture = VisionInputConverter.FromImage(imageSource);
                    ownsRuntimeTexture = imageSource != null &&
                                         imageSource.sprite != null &&
                                         runtimeTexture != imageSource.sprite.texture;
                    return runtimeTexture;

                case VisionInputSourceType.RawImage:
                    runtimeTexture = VisionInputConverter.FromRawImage(rawImageSource);
                    ownsRuntimeTexture = rawImageSource != null &&
                                         runtimeTexture != rawImageSource.texture;
                    return runtimeTexture;

                case VisionInputSourceType.WebCamTexture:
                    runtimeTexture = VisionInputConverter.FromWebCamTexture(webCamTextureSource);
                    ownsRuntimeTexture = runtimeTexture != null;
                    return runtimeTexture;

                default:
                    Debug.LogError($"Unsupported source type: {sourceType}");
                    return null;
            }
        }
    }
}

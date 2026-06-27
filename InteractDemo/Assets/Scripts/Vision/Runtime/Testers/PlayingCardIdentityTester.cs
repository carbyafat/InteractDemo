using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 場景測試用元件，用於分析樸克牌角落資訊區的花色、顏色與點數。
    /// </summary>
    public class PlayingCardIdentityTester : MonoBehaviour
    {
        [Header("輸入")]
        [Tooltip("要分析的角落資訊區貼圖。")]
        [SerializeField] private Texture2D cornerTexture;

        [Tooltip("若指定 RawImage，會優先分析 RawImage 目前顯示的 texture。")]
        [SerializeField] private RawImage cornerRawImage;

        [Header("分析設定")]
        [Tooltip("紅色像素比例需要比黑色高出多少，才會判定為紅色。")]
        [SerializeField] private float redDominanceThreshold = 0.002f;

        [Tooltip("判定有符號時需要達到的最小遮罩像素比例。")]
        [SerializeField] private float minSymbolPixelRatio = 0.002f;

        [Tooltip("接受為符號輪廓時需要達到的最小面積比例。")]
        [SerializeField] private float minContourAreaRatio = 0.0005f;

        [Tooltip("若為 true，會在 debug 圖上畫出候選符號輪廓。")]
        [SerializeField] private bool drawDebugContours = true;

        [Header("可選輸出")]
        [Tooltip("用來顯示角落辨識 debug 圖的 RawImage。")]
        [SerializeField] private RawImage debugPreview;

        [Tooltip("可選的 TMP 文字，用來顯示角落辨識結果。")]
        [SerializeField] private TMP_Text resultText;

        [Header("執行")]
        [Tooltip("若為 true，進入 Play 模式時會分析一次。")]
        [SerializeField] private bool analyzeOnStart = true;

        [Tooltip("若為 true，當 Corner Raw Image 的 texture 改變時會自動分析。")]
        [SerializeField] private bool autoAnalyzeWhenSourceChanges = true;

        [Tooltip("若為 true，Play 模式中每一幀都會重新分析。")]
        [SerializeField] private bool analyzeContinuously = false;

        private Texture2D runtimeSourceTexture;
        private Texture2D runtimeDebugTexture;
        private bool ownsRuntimeSourceTexture;
        private Texture lastAnalyzedRawTexture;

        /// <summary>
        /// Unity 生命週期方法，可選擇在啟動時分析角落資訊區。
        /// </summary>
        private void Start()
        {
            if (analyzeOnStart)
                AnalyzeSelectedCorner();
        }

        /// <summary>
        /// Unity 生命週期方法，可選擇每一幀重新分析。
        /// </summary>
        private void Update()
        {
            if (analyzeContinuously)
            {
                AnalyzeSelectedCorner();
                return;
            }

            if (autoAnalyzeWhenSourceChanges && HasCornerRawImageTextureChanged())
                AnalyzeSelectedCorner();
        }

        /// <summary>
        /// Unity 生命週期方法，用來釋放執行時產生的貼圖。
        /// </summary>
        private void OnDestroy()
        {
            DestroyRuntimeTextures();
        }

        /// <summary>
        /// 分析目前指定的角落資訊區，並更新可選 UI。
        /// </summary>
        [ContextMenu("Analyze Selected Corner")]
        public void AnalyzeSelectedCorner()
        {
            Texture2D source = GetSourceTexture();
            if (source == null)
            {
                if (resultText != null)
                    resultText.text = "No corner texture source.";

                return;
            }

            PlayingCardIdentityResult result = PlayingCardIdentityAnalyzer.Analyze(
                source,
                redDominanceThreshold,
                minSymbolPixelRatio,
                minContourAreaRatio,
                drawDebugContours);

            if (result == null)
                return;

            if (runtimeDebugTexture != null)
                Destroy(runtimeDebugTexture);

            runtimeDebugTexture = result.DebugTexture;

            if (debugPreview != null)
                debugPreview.texture = runtimeDebugTexture;

            if (resultText != null)
                resultText.text = result.ToString();

            lastAnalyzedRawTexture = cornerRawImage != null ? cornerRawImage.texture : null;
            Debug.Log(result.ToString());
        }

        /// <summary>
        /// 判斷 RawImage 來源貼圖是否已經更新。
        /// </summary>
        /// <returns>來源貼圖存在且和上次分析不同時回傳 true。</returns>
        private bool HasCornerRawImageTextureChanged()
        {
            if (cornerRawImage == null || cornerRawImage.texture == null)
                return false;

            return cornerRawImage.texture != lastAnalyzedRawTexture;
        }

        /// <summary>
        /// 取得要分析的 Texture2D。
        /// </summary>
        /// <returns>要分析的 Texture2D；如果轉換失敗則回傳 null。</returns>
        private Texture2D GetSourceTexture()
        {
            if (runtimeSourceTexture != null)
            {
                if (ownsRuntimeSourceTexture)
                    Destroy(runtimeSourceTexture);

                runtimeSourceTexture = null;
                ownsRuntimeSourceTexture = false;
            }

            if (cornerRawImage != null && cornerRawImage.texture != null)
            {
                runtimeSourceTexture = VisionInputConverter.FromRawImage(cornerRawImage);
                ownsRuntimeSourceTexture = runtimeSourceTexture != cornerRawImage.texture;
                return runtimeSourceTexture;
            }

            return cornerTexture;
        }

        /// <summary>
        /// 銷毀執行時產生的貼圖。
        /// </summary>
        private void DestroyRuntimeTextures()
        {
            if (runtimeSourceTexture != null)
            {
                if (ownsRuntimeSourceTexture)
                    Destroy(runtimeSourceTexture);

                runtimeSourceTexture = null;
                ownsRuntimeSourceTexture = false;
            }

            if (runtimeDebugTexture != null)
            {
                Destroy(runtimeDebugTexture);
                runtimeDebugTexture = null;
            }
        }
    }
}

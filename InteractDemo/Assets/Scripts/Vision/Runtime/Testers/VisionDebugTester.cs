using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 場景測試用元件，用於切換 OpenCV 形狀處理 debug 畫面。
    /// </summary>
    public class VisionDebugTester : MonoBehaviour
    {
        [Header("輸入來源")]
        [Tooltip("要轉換並處理的影像來源類型。")]
        [SerializeField] private VisionInputSourceType sourceType = VisionInputSourceType.Texture2D;

        [Tooltip("Source Type 為 Texture2D 時使用的貼圖。")]
        [SerializeField] private Texture2D textureSource;

        [Tooltip("Source Type 為 UIImage 時使用的 UI Image。")]
        [SerializeField] private Image imageSource;

        [Tooltip("Source Type 為 RawImage 時使用的 RawImage。")]
        [SerializeField] private RawImage rawImageSource;

        [Header("Debug 模式")]
        [Tooltip("要產生的 OpenCV debug 畫面。")]
        [SerializeField] private VisionDebugMode debugMode = VisionDebugMode.Grayscale;

        [Tooltip("Threshold 與輪廓前處理使用的二值化門檻。")]
        [Range(0, 255)]
        [SerializeField] private int threshold = 128;

        [Tooltip("Canny 邊緣偵測使用的第一個門檻。")]
        [SerializeField] private double cannyThreshold1 = 80;

        [Tooltip("Canny 邊緣偵測使用的第二個門檻。")]
        [SerializeField] private double cannyThreshold2 = 160;

        [Tooltip("高斯模糊大小，需為奇數；設為 0 或 1 可關閉模糊。")]
        [SerializeField] private int blurSize = 5;

        [Tooltip("輪廓線條粗細，單位為像素。")]
        [SerializeField] private int drawThickness = 2;

        [Header("可選輸出")]
        [Tooltip("用來顯示產生出的 debug 貼圖的 RawImage。")]
        [SerializeField] private RawImage debugPreview;

        [Tooltip("可選的 TMP 文字，用來顯示輪廓統計。")]
        [SerializeField] private TMP_Text resultText;

        [Header("執行")]
        [Tooltip("若為 true，進入 Play 模式時會處理選擇的來源。")]
        [SerializeField] private bool processOnStart = true;

        [Tooltip("若為 true，Play 模式中每一幀都會重新處理。")]
        [SerializeField] private bool processContinuously = false;

        private Texture2D runtimeSourceTexture;
        private Texture2D runtimeOutputTexture;
        private bool ownsRuntimeSourceTexture;

        /// <summary>
        /// Unity 生命週期方法，可選擇在啟動時處理來源。
        /// </summary>
        private void Start()
        {
            if (processOnStart)
                ProcessSelectedSource();
        }

        /// <summary>
        /// Unity 生命週期方法，可選擇每一幀刷新 debug 畫面。
        /// </summary>
        private void Update()
        {
            if (processContinuously)
                ProcessSelectedSource();
        }

        /// <summary>
        /// Unity 生命週期方法，用來釋放產生出的貼圖。
        /// </summary>
        private void OnDestroy()
        {
            DestroyRuntimeTextures();
        }

        /// <summary>
        /// 轉換選擇的來源、執行處理，並更新可選 UI。
        /// </summary>
        [ContextMenu("Process Selected Source")]
        public void ProcessSelectedSource()
        {
            Texture2D source = GetSelectedSourceTexture();
            if (source == null)
                return;

            ShapeAnalysisResult result = ShapeImageProcessor.Process(
                source,
                debugMode,
                threshold,
                cannyThreshold1,
                cannyThreshold2,
                blurSize,
                drawThickness);

            if (result == null)
                return;

            if (runtimeOutputTexture != null)
                Destroy(runtimeOutputTexture);

            runtimeOutputTexture = result.OutputTexture;

            if (debugPreview != null)
                debugPreview.texture = runtimeOutputTexture;

            Debug.Log(result.ToString());

            if (resultText != null)
                resultText.text = result.ToString();
        }

        /// <summary>
        /// 從目前選擇的來源類型取得 Texture2D。
        /// </summary>
        /// <returns>Texture2D 來源；如果轉換失敗則回傳 null。</returns>
        private Texture2D GetSelectedSourceTexture()
        {
            if (runtimeSourceTexture != null)
            {
                if (ownsRuntimeSourceTexture)
                    Destroy(runtimeSourceTexture);

                runtimeSourceTexture = null;
                ownsRuntimeSourceTexture = false;
            }

            switch (sourceType)
            {
                case VisionInputSourceType.Texture2D:
                    return textureSource;

                case VisionInputSourceType.UIImage:
                    runtimeSourceTexture = VisionInputConverter.FromImage(imageSource);
                    ownsRuntimeSourceTexture = imageSource != null &&
                                               imageSource.sprite != null &&
                                               runtimeSourceTexture != imageSource.sprite.texture;
                    return runtimeSourceTexture;

                case VisionInputSourceType.RawImage:
                    runtimeSourceTexture = VisionInputConverter.FromRawImage(rawImageSource);
                    ownsRuntimeSourceTexture = rawImageSource != null &&
                                               runtimeSourceTexture != rawImageSource.texture;
                    return runtimeSourceTexture;

                default:
                    Debug.LogError($"VisionDebugTester does not support source type {sourceType}. Use RawImage for camera preview input.");
                    return null;
            }
        }

        /// <summary>
        /// 銷毀此測試元件在執行時產生的貼圖。
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

            if (runtimeOutputTexture != null)
            {
                Destroy(runtimeOutputTexture);
                runtimeOutputTexture = null;
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 場景測試用元件，用於第一階段樸克牌外框偵測。
    /// </summary>
    public class PlayingCardDetectionTester : MonoBehaviour
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

        [Header("偵測設定")]
        [Tooltip("Canny 邊緣偵測使用的第一個門檻。")]
        [SerializeField] private double cannyThreshold1 = 80;

        [Tooltip("Canny 邊緣偵測使用的第二個門檻。")]
        [SerializeField] private double cannyThreshold2 = 160;

        [Tooltip("高斯模糊大小，需為奇數；設為 0 或 1 可關閉模糊。")]
        [SerializeField] private int blurSize = 5;

        [Tooltip("牌面輪廓相對於整張圖片的最小面積比例。")]
        [Range(0.001f, 0.8f)]
        [SerializeField] private float minAreaRatio = 0.01f;

        [Tooltip("接受為牌面時允許的最小長寬比。會自動把橫式與直式都轉成大於 1 的比例。")]
        [SerializeField] private float minAspectRatio = 1.1f;

        [Tooltip("接受為牌面時允許的最大長寬比。測試階段可放寬，避免圓角圖片被排除。")]
        [SerializeField] private float maxAspectRatio = 2.6f;

        [Tooltip("若為 true，除了選中的牌面，也會用綠色畫出所有輪廓。")]
        [SerializeField] private bool drawAllContours = true;

        [Header("階段二：牌面擷取")]
        [Tooltip("標準化牌面的長邊輸出尺寸。")]
        [SerializeField] private int normalizedLongSide = 600;

        [Tooltip("標準化牌面的短邊輸出尺寸。")]
        [SerializeField] private int normalizedShortSide = 420;

        [Tooltip("角落資訊區寬度占標準化牌面的比例。")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float cornerWidthRatio = 0.28f;

        [Tooltip("角落資訊區高度占標準化牌面的比例。")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float cornerHeightRatio = 0.38f;

        [Header("可選輸出")]
        [Tooltip("用來顯示樸克牌偵測 debug 貼圖的 RawImage。")]
        [SerializeField] private RawImage debugPreview;

        [Tooltip("用來顯示階段二標準化牌面圖的 RawImage。")]
        [SerializeField] private RawImage normalizedCardPreview;

        [Tooltip("用來顯示標準化牌面左上角資訊區的 RawImage。")]
        [SerializeField] private RawImage topLeftCornerPreview;

        [Tooltip("用來顯示標準化牌面右上角資訊區的 RawImage。")]
        [SerializeField] private RawImage topRightCornerPreview;

        [Tooltip("用來顯示標準化牌面左下角資訊區的 RawImage。")]
        [SerializeField] private RawImage bottomLeftCornerPreview;

        [Tooltip("用來顯示標準化牌面右下角資訊區的 RawImage。")]
        [SerializeField] private RawImage bottomRightCornerPreview;

        [Tooltip("可選的 TMP 文字，用來顯示樸克牌偵測統計。")]
        [SerializeField] private TMP_Text resultText;

        [Header("執行")]
        [Tooltip("若為 true，進入 Play 模式時會執行樸克牌偵測。")]
        [SerializeField] private bool detectOnStart = true;

        [Tooltip("若為 true，Play 模式中每一幀都會重新偵測。")]
        [SerializeField] private bool detectContinuously = false;

        [Tooltip("若為 true，會把偵測結果輸出到 Console。連續偵測時建議只在狀態變化時輸出。")]
        [SerializeField] private bool logToConsole = true;

        [Tooltip("若為 true，只有成功/失敗狀態改變時才輸出 Console，避免每幀洗版。")]
        [SerializeField] private bool logOnlyWhenStateChanges = true;

        private Texture2D runtimeSourceTexture;
        private Texture2D runtimeOutputTexture;
        private Texture2D runtimeNormalizedCardTexture;
        private Texture2D runtimeTopLeftCornerTexture;
        private Texture2D runtimeTopRightCornerTexture;
        private Texture2D runtimeBottomLeftCornerTexture;
        private Texture2D runtimeBottomRightCornerTexture;
        private bool ownsRuntimeSourceTexture;
        private bool hasLoggedResult;
        private bool lastLoggedDetected;

        /// <summary>
        /// Unity 生命週期方法，可選擇在啟動時執行樸克牌偵測。
        /// </summary>
        private void Start()
        {
            if (detectOnStart)
                DetectSelectedSource();
        }

        /// <summary>
        /// Unity 生命週期方法，可選擇每一幀刷新樸克牌偵測。
        /// </summary>
        private void Update()
        {
            if (detectContinuously)
                DetectSelectedSource();
        }

        /// <summary>
        /// Unity 生命週期方法，用來釋放產生出的貼圖。
        /// </summary>
        private void OnDestroy()
        {
            DestroyRuntimeTextures();
        }

        /// <summary>
        /// 轉換選擇的來源、偵測牌面矩形，並更新可選 UI。
        /// </summary>
        [ContextMenu("Detect Selected Source")]
        public void DetectSelectedSource()
        {
            Texture2D source = GetSelectedSourceTexture();
            if (source == null)
                return;

            PlayingCardDetectionResult result = PlayingCardDetector.Detect(
                source,
                cannyThreshold1,
                cannyThreshold2,
                blurSize,
                minAreaRatio,
                minAspectRatio,
                maxAspectRatio,
                normalizedLongSide,
                normalizedShortSide,
                cornerWidthRatio,
                cornerHeightRatio,
                drawAllContours);

            if (result == null)
                return;

            if (runtimeOutputTexture != null)
                Destroy(runtimeOutputTexture);

            runtimeOutputTexture = result.DebugTexture;
            StoreStageTwoTextures(result);

            if (debugPreview != null)
                debugPreview.texture = runtimeOutputTexture;

            if (normalizedCardPreview != null)
                normalizedCardPreview.texture = runtimeNormalizedCardTexture;

            if (topLeftCornerPreview != null)
                topLeftCornerPreview.texture = runtimeTopLeftCornerTexture;

            if (topRightCornerPreview != null)
                topRightCornerPreview.texture = runtimeTopRightCornerTexture;

            if (bottomLeftCornerPreview != null)
                bottomLeftCornerPreview.texture = runtimeBottomLeftCornerTexture;

            if (bottomRightCornerPreview != null)
                bottomRightCornerPreview.texture = runtimeBottomRightCornerTexture;

            if (ShouldLogResult(result))
            {
                Debug.Log(result.ToString());
                hasLoggedResult = true;
                lastLoggedDetected = result.IsDetected;
            }

            if (resultText != null)
                resultText.text = result.ToString();
        }

        /// <summary>
        /// 判斷這次偵測結果是否需要輸出到 Console。
        /// </summary>
        /// <param name="result">本次偵測結果。</param>
        /// <returns>需要輸出時回傳 true。</returns>
        private bool ShouldLogResult(PlayingCardDetectionResult result)
        {
            if (!logToConsole)
                return false;

            if (!logOnlyWhenStateChanges)
                return true;

            return !hasLoggedResult || result.IsDetected != lastLoggedDetected;
        }

        /// <summary>
        /// 接收並保存階段二產生的貼圖。
        /// </summary>
        /// <param name="result">本次偵測結果。</param>
        private void StoreStageTwoTextures(PlayingCardDetectionResult result)
        {
            DestroyStageTwoTextures();

            runtimeNormalizedCardTexture = result.NormalizedCardTexture;
            runtimeTopLeftCornerTexture = result.TopLeftCornerTexture;
            runtimeTopRightCornerTexture = result.TopRightCornerTexture;
            runtimeBottomLeftCornerTexture = result.BottomLeftCornerTexture;
            runtimeBottomRightCornerTexture = result.BottomRightCornerTexture;
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
                    Debug.LogError($"PlayingCardDetectionTester does not support source type {sourceType}. Use RawImage for camera preview input.");
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

            DestroyStageTwoTextures();
        }

        /// <summary>
        /// 銷毀階段二在執行時產生的貼圖。
        /// </summary>
        private void DestroyStageTwoTextures()
        {
            if (runtimeNormalizedCardTexture != null)
            {
                Destroy(runtimeNormalizedCardTexture);
                runtimeNormalizedCardTexture = null;
            }

            if (runtimeTopLeftCornerTexture != null)
            {
                Destroy(runtimeTopLeftCornerTexture);
                runtimeTopLeftCornerTexture = null;
            }

            if (runtimeTopRightCornerTexture != null)
            {
                Destroy(runtimeTopRightCornerTexture);
                runtimeTopRightCornerTexture = null;
            }

            if (runtimeBottomLeftCornerTexture != null)
            {
                Destroy(runtimeBottomLeftCornerTexture);
                runtimeBottomLeftCornerTexture = null;
            }

            if (runtimeBottomRightCornerTexture != null)
            {
                Destroy(runtimeBottomRightCornerTexture);
                runtimeBottomRightCornerTexture = null;
            }
        }
    }
}

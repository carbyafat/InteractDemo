using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 用於測試筆電或手機相機接到 RGB 分析流程的元件。
    /// </summary>
    public class WebCamRgbAnalysisTester : MonoBehaviour
    {
        [Header("相機")]
        [Tooltip("若為 true，進入 Play 模式時會啟動選擇的相機。")]
        [SerializeField] private bool playOnStart = true;

        [Tooltip("若為 true，會優先使用前鏡頭。")]
        [SerializeField] private bool preferFrontFacing = false;

        [Tooltip("期望的相機畫面寬度。")]
        [SerializeField] private int requestedWidth = 1280;

        [Tooltip("期望的相機畫面高度。")]
        [SerializeField] private int requestedHeight = 720;

        [Tooltip("期望的相機影格率。")]
        [SerializeField] private int requestedFps = 30;

        [Header("分析")]
        [Tooltip("若為 true，Play 模式中會重複分析相機畫面。")]
        [SerializeField] private bool analyzeContinuously = false;

        [Tooltip("連續分析之間的秒數間隔。")]
        [SerializeField] private float analysisInterval = 0.5f;

        [Header("可選輸出")]
        [Tooltip("若為 true，且未指定 Camera Preview 時，會自動建立簡易的螢幕相機預覽。")]
        [SerializeField] private bool createPreviewIfMissing = true;

        [Tooltip("自動建立的相機預覽尺寸，單位為螢幕像素。")]
        [SerializeField] private Vector2 autoPreviewSize = new Vector2(640f, 360f);

        [Tooltip("自動建立相機預覽的錨點位置。")]
        [SerializeField] private Vector2 autoPreviewPosition = new Vector2(24f, -24f);

        [Tooltip("可選的 RawImage，用來預覽即時相機畫面。")]
        [SerializeField] private RawImage cameraPreview;

        [Tooltip("可選的 Image，顏色會設定為計算出的平均色。")]
        [SerializeField] private Image averageColorPreview;

        [Tooltip("可選的 TMP 文字，用來顯示分析結果。")]
        [SerializeField] private TMP_Text resultText;

        /// <summary>
        /// 目前啟用中的 Unity 相機貼圖。
        /// </summary>
        private WebCamTexture webCamTexture;

        /// <summary>
        /// 從目前相機畫面擷取出的暫時 Texture2D。
        /// </summary>
        private Texture2D capturedFrame;

        /// <summary>
        /// 下一次允許執行連續分析的 Time.time 時間點。
        /// </summary>
        private float nextAnalysisTime;

        /// <summary>
        /// 未指定 RawImage 時，在執行時自動建立的預覽根物件。
        /// </summary>
        private GameObject autoPreviewRoot;

        /// <summary>
        /// Unity 生命週期方法，可選擇在啟動時開啟相機。
        /// </summary>
        /// <returns>相機啟動流程使用的 Coroutine enumerator。</returns>
        private IEnumerator Start()
        {
            if (playOnStart)
                yield return StartCamera();
        }

        /// <summary>
        /// Unity 生命週期方法，在啟用時執行連續相機分析。
        /// </summary>
        private void Update()
        {
            if (!analyzeContinuously || webCamTexture == null || !webCamTexture.isPlaying)
                return;

            if (Time.time < nextAnalysisTime)
                return;

            nextAnalysisTime = Time.time + Mathf.Max(0.05f, analysisInterval);
            AnalyzeCurrentFrame();
        }

        /// <summary>
        /// Unity 生命週期方法，用來停止相機並釋放擷取畫面。
        /// </summary>
        private void OnDestroy()
        {
            StopCamera();

            if (autoPreviewRoot != null)
            {
                Destroy(autoPreviewRoot);
                autoPreviewRoot = null;
            }
        }

        /// <summary>
        /// 從 Inspector 右鍵選單啟動相機。
        /// </summary>
        [ContextMenu("Start Camera")]
        public void StartCameraFromInspector()
        {
            StartCoroutine(StartCamera());
        }

        /// <summary>
        /// 從 Inspector 右鍵選單停止相機。
        /// </summary>
        [ContextMenu("Stop Camera")]
        public void StopCameraFromInspector()
        {
            StopCamera();
        }

        /// <summary>
        /// 擷取並分析目前的相機畫面。
        /// </summary>
        [ContextMenu("Analyze Current Frame")]
        public void AnalyzeCurrentFrame()
        {
            if (webCamTexture == null || !webCamTexture.isPlaying)
            {
                Debug.LogError("AnalyzeCurrentFrame failed: camera is not running.");
                return;
            }

            if (webCamTexture.width <= 16 || webCamTexture.height <= 16)
            {
                Debug.LogError("AnalyzeCurrentFrame failed: camera frame is not ready yet.");
                return;
            }

            if (capturedFrame != null)
                Destroy(capturedFrame);

            capturedFrame = VisionInputConverter.FromWebCamTexture(webCamTexture);
            if (capturedFrame == null)
                return;

            RgbAnalysisResult result = RgbImageAnalyzer.Analyze(capturedFrame);
            if (result == null)
                return;

            Debug.Log(result.ToString());

            if (averageColorPreview != null)
                averageColorPreview.color = result.AverageColor;

            if (resultText != null)
                resultText.text = result.ToString();
        }

        /// <summary>
        /// 請求權限、選擇相機裝置，並啟動 WebCamTexture。
        /// </summary>
        /// <returns>非同步相機啟動流程使用的 Coroutine enumerator。</returns>
        public IEnumerator StartCamera()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
                yield break;

            yield return RequestCameraPermission();

            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Debug.LogError("StartCamera failed: camera permission was not granted.");
                yield break;
            }

            WebCamDevice? selectedDevice = FindCamera(preferFrontFacing);
            if (!selectedDevice.HasValue)
            {
                Debug.LogError("StartCamera failed: no camera device found.");
                yield break;
            }

            webCamTexture = new WebCamTexture(
                selectedDevice.Value.name,
                requestedWidth,
                requestedHeight,
                requestedFps);

            webCamTexture.Play();

            // WebCamTexture 需要短暫預熱，width/height 才會變成真實數值。
            float timeoutAt = Time.realtimeSinceStartup + 3f;
            while ((webCamTexture.width <= 16 || webCamTexture.height <= 16) &&
                   Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            EnsureCameraPreview();

            Debug.Log($"Camera started: {selectedDevice.Value.name}, {webCamTexture.width}x{webCamTexture.height}");
        }

        /// <summary>
        /// 停止目前啟用的相機，並釋放擷取畫面資源。
        /// </summary>
        public void StopCamera()
        {
            if (webCamTexture != null)
            {
                if (webCamTexture.isPlaying)
                    webCamTexture.Stop();

                Destroy(webCamTexture);
                webCamTexture = null;
            }

            if (capturedFrame != null)
            {
                Destroy(capturedFrame);
                capturedFrame = null;
            }
        }

        /// <summary>
        /// 切換前鏡頭與後鏡頭偏好，並重新啟動相機。
        /// </summary>
        public void SwitchCamera()
        {
            preferFrontFacing = !preferFrontFacing;
            StopCamera();
            StartCoroutine(StartCamera());
        }

        /// <summary>
        /// 如果尚未取得權限，向 Unity 請求相機權限。
        /// </summary>
        /// <returns>權限請求流程使用的 Coroutine enumerator。</returns>
        private static IEnumerator RequestCameraPermission()
        {
            if (Application.HasUserAuthorization(UserAuthorization.WebCam))
                yield break;

            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        /// <summary>
        /// 盡可能尋找符合指定鏡頭方向的相機裝置。
        /// </summary>
        /// <param name="frontFacing">true 表示優先使用前鏡頭；false 表示優先使用後鏡頭。</param>
        /// <returns>符合方向的相機、第一個可用相機；如果沒有相機則回傳 null。</returns>
        private static WebCamDevice? FindCamera(bool frontFacing)
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
                return null;

            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].isFrontFacing == frontFacing)
                    return devices[i];
            }

            return devices[0];
        }

        /// <summary>
        /// 將相機貼圖指定給既有 RawImage，或建立簡易預覽 UI。
        /// </summary>
        private void EnsureCameraPreview()
        {
            if (cameraPreview == null && createPreviewIfMissing)
                cameraPreview = CreateAutoCameraPreview();

            if (cameraPreview == null)
                return;

            cameraPreview.texture = webCamTexture;

            ApplyPreviewSize(cameraPreview.rectTransform);
        }

        /// <summary>
        /// 建立 Canvas 與 RawImage，用於測試時顯示相機畫面。
        /// </summary>
        /// <returns>產生出的預覽物件上的 RawImage。</returns>
        private RawImage CreateAutoCameraPreview()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Vision Camera Preview Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
                autoPreviewRoot = canvasObject;
            }

            GameObject previewObject = new GameObject("Auto Camera Preview");
            previewObject.transform.SetParent(canvas.transform, false);

            RawImage preview = previewObject.AddComponent<RawImage>();
            preview.color = Color.white;

            RectTransform rectTransform = preview.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = autoPreviewPosition;
            ApplyPreviewSize(rectTransform);

            if (autoPreviewRoot == null)
                autoPreviewRoot = previewObject;

            return preview;
        }

        /// <summary>
        /// 在維持相機長寬比的前提下套用指定的預覽尺寸。
        /// </summary>
        /// <param name="rectTransform">要調整尺寸的 RectTransform。</param>
        private void ApplyPreviewSize(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            Vector2 maxSize = new Vector2(Mathf.Max(16f, autoPreviewSize.x), Mathf.Max(16f, autoPreviewSize.y));
            if (webCamTexture == null || webCamTexture.width <= 16 || webCamTexture.height <= 16)
            {
                rectTransform.sizeDelta = maxSize;
                return;
            }

            float cameraAspect = webCamTexture.width / (float)webCamTexture.height;
            float targetWidth = maxSize.x;
            float targetHeight = targetWidth / cameraAspect;

            if (targetHeight > maxSize.y)
            {
                targetHeight = maxSize.y;
                targetWidth = targetHeight * cameraAspect;
            }

            rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);
        }
    }
}

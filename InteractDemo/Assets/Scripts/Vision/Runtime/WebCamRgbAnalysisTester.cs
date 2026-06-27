using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    // Camera-facing test component for laptop webcams and mobile cameras.
    // This is still a first-stage bridge: WebCamTexture -> Texture2D -> RgbImageAnalyzer.
    // Later, realtime OpenCV should use OpenCVForUnity camera helpers to avoid repeated Texture2D allocation.
    public class WebCamRgbAnalysisTester : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool preferFrontFacing = false;
        [SerializeField] private int requestedWidth = 1280;
        [SerializeField] private int requestedHeight = 720;
        [SerializeField] private int requestedFps = 30;

        [Header("Analysis")]
        [SerializeField] private bool analyzeContinuously = false;
        [SerializeField] private float analysisInterval = 0.5f;

        [Header("Optional Output")]
        [SerializeField] private RawImage cameraPreview;
        [SerializeField] private Image averageColorPreview;
        [SerializeField] private Text resultText;

        private WebCamTexture webCamTexture;
        private Texture2D capturedFrame;
        private float nextAnalysisTime;

        private IEnumerator Start()
        {
            if (playOnStart)
                yield return StartCamera();
        }

        private void Update()
        {
            if (!analyzeContinuously || webCamTexture == null || !webCamTexture.isPlaying)
                return;

            if (Time.time < nextAnalysisTime)
                return;

            nextAnalysisTime = Time.time + Mathf.Max(0.05f, analysisInterval);
            AnalyzeCurrentFrame();
        }

        private void OnDestroy()
        {
            StopCamera();
        }

        [ContextMenu("Start Camera")]
        public void StartCameraFromInspector()
        {
            StartCoroutine(StartCamera());
        }

        [ContextMenu("Stop Camera")]
        public void StopCameraFromInspector()
        {
            StopCamera();
        }

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

            // WebCamTexture needs a short warm-up before width/height become real values.
            float timeoutAt = Time.realtimeSinceStartup + 3f;
            while ((webCamTexture.width <= 16 || webCamTexture.height <= 16) &&
                   Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            if (cameraPreview != null)
                cameraPreview.texture = webCamTexture;

            Debug.Log($"Camera started: {selectedDevice.Value.name}, {webCamTexture.width}x{webCamTexture.height}");
        }

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

        public void SwitchCamera()
        {
            preferFrontFacing = !preferFrontFacing;
            StopCamera();
            StartCoroutine(StartCamera());
        }

        private static IEnumerator RequestCameraPermission()
        {
            if (Application.HasUserAuthorization(UserAuthorization.WebCam))
                yield break;

            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

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
    }
}

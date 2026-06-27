using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InteractDemo.Vision
{
    /// <summary>
    /// Test component for connecting a laptop or mobile camera to RGB analysis.
    /// </summary>
    public class WebCamRgbAnalysisTester : MonoBehaviour
    {
        [Header("Camera")]
        [Tooltip("If true, starts the selected camera when Play mode starts.")]
        [SerializeField] private bool playOnStart = true;

        [Tooltip("If true, prefers a front-facing camera when one is available.")]
        [SerializeField] private bool preferFrontFacing = false;

        [Tooltip("Requested camera frame width.")]
        [SerializeField] private int requestedWidth = 1280;

        [Tooltip("Requested camera frame height.")]
        [SerializeField] private int requestedHeight = 720;

        [Tooltip("Requested camera frame rate.")]
        [SerializeField] private int requestedFps = 30;

        [Header("Analysis")]
        [Tooltip("If true, analyzes the camera frame repeatedly during Play mode.")]
        [SerializeField] private bool analyzeContinuously = false;

        [Tooltip("Seconds between continuous analysis passes.")]
        [SerializeField] private float analysisInterval = 0.5f;

        [Header("Optional Output")]
        [Tooltip("Optional RawImage used to preview the live camera feed.")]
        [SerializeField] private RawImage cameraPreview;

        [Tooltip("Optional Image whose color will be set to the calculated average color.")]
        [SerializeField] private Image averageColorPreview;

        [Tooltip("Optional Text used to display the analysis result.")]
        [SerializeField] private Text resultText;

        /// <summary>
        /// Active Unity camera texture.
        /// </summary>
        private WebCamTexture webCamTexture;

        /// <summary>
        /// Temporary Texture2D captured from the current camera frame.
        /// </summary>
        private Texture2D capturedFrame;

        /// <summary>
        /// Next Time.time value at which continuous analysis may run.
        /// </summary>
        private float nextAnalysisTime;

        /// <summary>
        /// Unity lifecycle method that optionally starts the camera.
        /// </summary>
        /// <returns>Coroutine enumerator for camera startup.</returns>
        private IEnumerator Start()
        {
            if (playOnStart)
                yield return StartCamera();
        }

        /// <summary>
        /// Unity lifecycle method that runs continuous camera analysis when enabled.
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
        /// Unity lifecycle method used to stop the camera and release captured frames.
        /// </summary>
        private void OnDestroy()
        {
            StopCamera();
        }

        /// <summary>
        /// Starts the camera from the Inspector context menu.
        /// </summary>
        [ContextMenu("Start Camera")]
        public void StartCameraFromInspector()
        {
            StartCoroutine(StartCamera());
        }

        /// <summary>
        /// Stops the camera from the Inspector context menu.
        /// </summary>
        [ContextMenu("Stop Camera")]
        public void StopCameraFromInspector()
        {
            StopCamera();
        }

        /// <summary>
        /// Captures and analyzes the current camera frame.
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
        /// Requests permission, selects a camera device, and starts the WebCamTexture.
        /// </summary>
        /// <returns>Coroutine enumerator for asynchronous camera startup.</returns>
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

        /// <summary>
        /// Stops the active camera and releases captured frame resources.
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
        /// Toggles between preferring front-facing and rear-facing cameras, then restarts the camera.
        /// </summary>
        public void SwitchCamera()
        {
            preferFrontFacing = !preferFrontFacing;
            StopCamera();
            StartCoroutine(StartCamera());
        }

        /// <summary>
        /// Requests webcam permission from Unity if it has not already been granted.
        /// </summary>
        /// <returns>Coroutine enumerator for permission request.</returns>
        private static IEnumerator RequestCameraPermission()
        {
            if (Application.HasUserAuthorization(UserAuthorization.WebCam))
                yield break;

            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        /// <summary>
        /// Finds a camera device matching the requested facing direction when possible.
        /// </summary>
        /// <param name="frontFacing">True to prefer a front-facing camera; false to prefer a rear-facing camera.</param>
        /// <returns>Matching camera device, first available device, or null when no device exists.</returns>
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

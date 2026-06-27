using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;

namespace InteractDemo.Vision
{
    // Pure analysis logic for RGB values.
    // This class does not know about scene objects, UI, or gameplay. It only receives
    // a Texture2D and returns numeric data.
    public static class RgbImageAnalyzer
    {
        // Entry point for the current first-stage test:
        // Unity Texture2D -> OpenCV Mat -> average RGB result.
        public static RgbAnalysisResult Analyze(Texture2D source)
        {
            if (source == null)
            {
                Debug.LogError("RgbImageAnalyzer.Analyze failed: source texture is null.");
                return null;
            }

            // OpenCV stores images in Mat objects.
            // CV_8UC4 means:
            // - 8U: each channel is an unsigned 8-bit value, so 0-255
            // - C4: four channels, matching RGBA
            using (Mat rgbaMat = new Mat(source.height, source.width, CvType.CV_8UC4))
            {
                // Convert Unity's Texture2D pixel data into OpenCV's Mat format.
                // After this call, OpenCV functions can read and process the image.
                OpenCVMatUtils.Texture2DToMat(source, rgbaMat);

                // sumElems adds every pixel channel across the whole image.
                // For RGBA Mat, sum.val[0], [1], [2], [3] are total R, G, B, A.
                Scalar sum = Core.sumElems(rgbaMat);

                // Average color is total channel value divided by number of pixels.
                double pixelCount = rgbaMat.rows() * rgbaMat.cols();

                float averageR = (float)(sum.val[0] / pixelCount);
                float averageG = (float)(sum.val[1] / pixelCount);
                float averageB = (float)(sum.val[2] / pixelCount);

                // First version uses the simplest brightness definition.
                // Later we can replace this with luminance: 0.2126R + 0.7152G + 0.0722B.
                float brightness = (averageR + averageG + averageB) / 3f;

                return new RgbAnalysisResult
                {
                    Width = source.width,
                    Height = source.height,
                    AverageR = averageR,
                    AverageG = averageG,
                    AverageB = averageB,
                    Brightness = brightness,

                    // Unity Color uses 0-1 floats, while image RGB values are 0-255.
                    AverageColor = new Color(averageR / 255f, averageG / 255f, averageB / 255f, 1f)
                };
            }
        }
    }
}

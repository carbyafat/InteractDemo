using UnityEngine;

namespace InteractDemo.Vision
{
    // A simple data container for the result of one RGB analysis pass.
    // Keeping this separate from the analyzer makes it easier to reuse the data in UI,
    // gameplay mapping, debug logs, or future save files.
    [System.Serializable]
    public class RgbAnalysisResult
    {
        // Original texture size. Useful for checking whether Unity imported the image
        // at the expected resolution.
        public int Width;
        public int Height;

        // Unity color normalized to 0-1. This can be assigned directly to UI Image.color,
        // materials, particles, or other Unity visual outputs.
        public Color AverageColor;

        // Average channel values in the familiar 0-255 image range.
        // Example: R=255, G=0, B=0 means pure red.
        public float AverageR;
        public float AverageG;
        public float AverageB;

        // Very simple brightness value. This is only the average of R/G/B for now,
        // not a perceptual luminance formula.
        public float Brightness;

        // Controls how the result appears in Debug.Log and UI text.
        public override string ToString()
        {
            return $"Size: {Width}x{Height}\n" +
                   $"Average RGB: R={AverageR:F1}, G={AverageG:F1}, B={AverageB:F1}\n" +
                   $"Brightness: {Brightness:F1}";
        }
    }
}

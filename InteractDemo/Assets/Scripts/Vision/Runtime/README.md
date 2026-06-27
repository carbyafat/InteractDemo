# Vision Runtime Script Layout

## Data

Result classes, enums, and small serializable data containers.

Examples:

- `RgbAnalysisResult`
- `ColorAnalysisResult`
- `MainColorType`
- `ShapeAnalysisResult`
- `PlayingCardDetectionResult`
- `PlayingCardIdentityResult`
- `PlayingCardColorGroup`
- `PlayingCardSuit`
- `VisionDebugMode`

## Analysis

Pure image analysis logic. These scripts should not depend on scene objects or UI.

Examples:

- `RgbImageAnalyzer`
- `ColorImageAnalyzer`
- `ShapeImageProcessor`
- `PlayingCardDetector`
- `PlayingCardIdentityAnalyzer`

## Input

Converters and source-type helpers. These scripts turn Unity input sources into analysis-ready textures.

Examples:

- `VisionInputConverter`
- `VisionInputSourceType`

## Testers

MonoBehaviour components that can be attached to GameObjects for testing in Unity scenes.

Examples:

- `TextureRgbAnalysisTester`
- `RgbAnalysisSourceTester`
- `ColorAnalysisTester`
- `WebCamRgbAnalysisTester`
- `VisionDebugTester`
- `PlayingCardDetectionTester`
- `PlayingCardIdentityTester`

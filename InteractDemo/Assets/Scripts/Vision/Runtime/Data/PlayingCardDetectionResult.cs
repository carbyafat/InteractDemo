using UnityEngine;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 儲存第一階段樸克牌外框偵測的結果資料。
    /// </summary>
    [System.Serializable]
    public class PlayingCardDetectionResult
    {
        /// <summary>
        /// 找到類似樸克牌的四邊形時為 true。
        /// </summary>
        public bool IsDetected;

        /// <summary>
        /// 帶有輪廓與選中牌面外框的 debug 貼圖。
        /// </summary>
        public Texture2D DebugTexture;

        /// <summary>
        /// 將偵測到的牌面裁切並校正後產生的標準化牌面圖。
        /// </summary>
        public Texture2D NormalizedCardTexture;

        /// <summary>
        /// 標準化牌面左上角資訊區。
        /// </summary>
        public Texture2D TopLeftCornerTexture;

        /// <summary>
        /// 標準化牌面右上角資訊區。
        /// </summary>
        public Texture2D TopRightCornerTexture;

        /// <summary>
        /// 標準化牌面左下角資訊區。
        /// </summary>
        public Texture2D BottomLeftCornerTexture;

        /// <summary>
        /// 標準化牌面右下角資訊區。
        /// </summary>
        public Texture2D BottomRightCornerTexture;

        /// <summary>
        /// 在篩選牌面前找到的外部輪廓數量。
        /// </summary>
        public int ContourCount;

        /// <summary>
        /// 選中牌面輪廓的面積，單位為像素。
        /// </summary>
        public double CardArea;

        /// <summary>
        /// 選中牌面面積相對於整張圖片的比例。
        /// </summary>
        public float CardAreaRatio;

        /// <summary>
        /// 本次找到的最大輪廓面積比例，用於判斷門檻是否太高。
        /// </summary>
        public float LargestContourAreaRatio;

        /// <summary>
        /// 本次找到的最大輪廓長寬比，用於判斷牌面比例範圍是否太窄。
        /// </summary>
        public float LargestContourAspectRatio;

        /// <summary>
        /// 沒有偵測成功時的主要原因。
        /// </summary>
        public string RejectionReason;

        /// <summary>
        /// 選中牌面的外接矩形，使用 OpenCV 像素座標。
        /// </summary>
        public Rect CardBounds;

        /// <summary>
        /// 選中牌面的四個角點，使用 OpenCV 像素座標。
        /// </summary>
        public Vector2[] Corners = new Vector2[0];

        /// <summary>
        /// 將偵測結果格式化成 Console 或 UI 文字。
        /// </summary>
        /// <returns>方便閱讀的樸克牌偵測摘要。</returns>
        public override string ToString()
        {
            return $"Card Detected: {IsDetected}\n" +
                   $"Contours: {ContourCount}\n" +
                   $"Card Area: {CardArea:F1}\n" +
                   $"Card Area Ratio: {CardAreaRatio:P1}\n" +
                   $"Largest Contour Area Ratio: {LargestContourAreaRatio:P1}\n" +
                   $"Largest Contour Aspect: {LargestContourAspectRatio:F2}\n" +
                   $"Reason: {RejectionReason}\n" +
                   $"Normalized Card: {NormalizedCardTexture != null}\n" +
                   $"Card Bounds: x={CardBounds.x:F0}, y={CardBounds.y:F0}, w={CardBounds.width:F0}, h={CardBounds.height:F0}\n" +
                   $"Corners: {Corners.Length}";
        }
    }
}

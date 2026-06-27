using UnityEngine;

namespace InteractDemo.Vision
{
    /// <summary>
    /// 儲存樸克牌角落資訊區的初步辨識結果。
    /// </summary>
    [System.Serializable]
    public class PlayingCardIdentityResult
    {
        /// <summary>
        /// 是否初步判定為 Joker。
        /// </summary>
        public bool IsJoker;

        /// <summary>
        /// 初步判定的點數文字，例如 A、1、2、J、Q、K。
        /// </summary>
        public string Rank = "Unknown";

        /// <summary>
        /// 初步判定的花色。
        /// </summary>
        public PlayingCardSuit Suit = PlayingCardSuit.Unknown;

        /// <summary>
        /// 初步判定的紅黑顏色。
        /// </summary>
        public PlayingCardColorGroup ColorGroup = PlayingCardColorGroup.Unknown;

        /// <summary>
        /// 目前這筆判斷的信心分數，範圍 0-1。
        /// </summary>
        public float Confidence;

        /// <summary>
        /// 偵測到的紅色像素比例。
        /// </summary>
        public float RedPixelRatio;

        /// <summary>
        /// 偵測到的黑色像素比例。
        /// </summary>
        public float BlackPixelRatio;

        /// <summary>
        /// 用於 debug 的遮罩或疊圖。
        /// </summary>
        public Texture2D DebugTexture;

        /// <summary>
        /// 找到的候選符號輪廓數。
        /// </summary>
        public int SymbolContourCount;

        /// <summary>
        /// 最大候選符號輪廓面積比例。
        /// </summary>
        public float LargestSymbolAreaRatio;

        /// <summary>
        /// 本次判斷的文字說明。
        /// </summary>
        public string Reason = "Not analyzed";

        /// <summary>
        /// 將結果格式化成 Console 或 UI 文字。
        /// </summary>
        /// <returns>方便閱讀的辨識摘要。</returns>
        public override string ToString()
        {
            return $"Rank: {Rank}\n" +
                   $"Suit: {Suit}\n" +
                   $"Color: {ColorGroup}\n" +
                   $"Joker: {IsJoker}\n" +
                   $"Confidence: {Confidence:P1}\n" +
                   $"Red Ratio: {RedPixelRatio:P1}\n" +
                   $"Black Ratio: {BlackPixelRatio:P1}\n" +
                   $"Symbol Contours: {SymbolContourCount}\n" +
                   $"Largest Symbol Area Ratio: {LargestSymbolAreaRatio:P1}\n" +
                   $"Reason: {Reason}";
        }
    }
}

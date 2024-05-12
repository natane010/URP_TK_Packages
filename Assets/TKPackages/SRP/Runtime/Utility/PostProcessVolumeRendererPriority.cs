using System;

namespace TKPackages.SRP.Runtime.Utility
{
    /// <summary>
    /// 優先度指標 (かぶったらダメ)
    /// 設定なしBeforeでコンパイル順
    /// 設定文字列あくまで指標（無視してOK）
    /// </summary>
    public class CustomPassPriority
    {
        public const int Before = 0;
        public const int EnvironmentEffect = 100;
        public const int Glitch = 200;
        public const int EdgeDetection = 300;
        public const int Pixel = 400;
        public const int Blur = 500;
        public const int ImageProcessing = 600;
        public const int ColorAdjustment = 700;
        public const int EndImage = 800;
        public const int Stay = 900;
        public const int Vignette = 1000;
        public const int AfterImage = 1100;
    }

    /// <summary>
    /// 属性タグ
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PostProcessRendererPriority : Attribute
    {
        public readonly int priority;

        public PostProcessRendererPriority(int priority)
        {
            this.priority = priority;
        }
    }

}
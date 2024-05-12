#if URP_SETTINGS_PJ
using System;

namespace TKPackages.SRP.Runtime.Utility
{
    /// <summary>
    /// Volume優先度
    /// </summary>
    public class VolumePriority
    {
        public const int Before = 0;
        public const int Environment = 100;
        public const int Glitch = 200;
        public const int EdgeDetection = 300;
        public const int Pixelate = 400;
        public const int Blur = 500;
        public const int ImageProcessing = 600;
        public const int ColorAdjustment = 700;
        public const int Skill = 800;
        public const int Story = 900;
        public const int Vignette = 1000;
        public const int After = 1100;
    }
    
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
#endif
using TKPackages.SRP.Runtime.Base;
using TKPackages.SRP.Runtime.Utility;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TKPackages.SRP.Runtime.Effect.BokehBlur
{
    [VolumeComponentMenu("SRP/(Bokeh Blur)")]
    public class BokehBlur : PostProcessSettingBase
    {
        public override bool IsActive() => BlurRadius.value > 0;
        public FloatParameter BlurRadius = new ClampedFloatParameter(0f, 0f, 3f);
        public IntParameter Iteration = new ClampedIntParameter(32, 8, 128);
        public FloatParameter RTDownScaling = new ClampedFloatParameter(2f, 1f, 10f);
    }

    [PostProcessRendererPriority(CustomPassPriority.Blur + 5)]
    public class BokehBlurRenderer : PostProcessRendererBase<BokehBlur>
    {
        public override string ProfilerTag => "BokehBlur";
        protected override string ShaderName => "Hidden/TKPackages/PostProcessing/Blur/BokehBlur";

        private RTHandle m_BufferRT1;
        private Vector4 m_GoldenRot;

        public override void Init()
        {
            base.Init();

            // Precompute rotations
            float c = Mathf.Cos(2.39996323f);
            float s = Mathf.Sin(2.39996323f);
            m_GoldenRot = new Vector4(c, s, -s, c);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_BufferRT1 != null)
            {
                RTHandles.Release(m_BufferRT1);
                m_BufferRT1 = null;
            }
        }

        static class ShaderIDs
        {
            internal static readonly string BufferRT1 = "_BufferRT1";
            internal static readonly int GoldenRot = Shader.PropertyToID("_GoldenRot");
            internal static readonly int Params = Shader.PropertyToID("_Params");
        }

        public override void Render(CommandBuffer cmd, RTHandle source, RTHandle target, ref RenderingData renderingData)
        {
            var desc = GetDefaultColorRTDescriptor(ref renderingData, (int)(Screen.width / m_Settings.RTDownScaling.value), (int)(Screen.height / m_Settings.RTDownScaling.value));
            desc.colorFormat = RenderTextureFormat.ARGB32;
            desc.sRGB = true;
            RenderingUtils.ReAllocateIfNeeded(ref m_BufferRT1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: ShaderIDs.BufferRT1);

            Blitter.BlitCameraTexture(cmd, source, m_BufferRT1);
            m_BlitMaterial.SetVector(ShaderIDs.GoldenRot, m_GoldenRot);
            m_BlitMaterial.SetVector(ShaderIDs.Params, new Vector4(m_Settings.Iteration.value, m_Settings.BlurRadius.value, 1f / Screen.width, 1f / Screen.height));
            Blitter.BlitCameraTexture(cmd, m_BufferRT1, target, m_BlitMaterial, 0);
        }

    }
}
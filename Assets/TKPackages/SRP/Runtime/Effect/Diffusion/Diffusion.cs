using TKPackages.SRP.Runtime.Base;
using TKPackages.SRP.Runtime.Utility;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TKPackages.SRP.Runtime.Diffusion
{
    [VolumeComponentMenu("TKPackages/SRP/Diffusion")]
    public class Diffusion : PostProcessSettingsBase
    {
        public override bool IsActive() => Intensity.value > 0;
        public FloatParameter BlurRadius = new ClampedFloatParameter(0f, 0f, 3f);
        public FloatParameter Contrast = new ClampedFloatParameter(0f, 0f, 3f);
        public FloatParameter Intensity = new ClampedFloatParameter(0f, 0f, 1f);
        public IntParameter Iteration = new ClampedIntParameter(32, 8, 128);
        public FloatParameter RTDownScaling = new ClampedFloatParameter(2f, 1f, 10f);
    }

    [PostProcessRendererPriority(VolumePriority.Blur + 50)]
    public class DiffusionRenderer : PostProcessRendererBase<Diffusion>
    {
        public override string ProfilerTag => "PostFX-Diffusion";
        protected override string ShaderName => "Hidden/TK/PostFX/Diffusion";

        private RTHandle m_BufferRT1;
        private RTHandle m_BufferRT2;

        public override void Init()
        {
            base.Init();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_BufferRT1 != null)
            {
                RTHandles.Release(m_BufferRT1);
                m_BufferRT1 = null;
            }
            if (m_BufferRT2 != null)
            {
                RTHandles.Release(m_BufferRT2);
                m_BufferRT2 = null;
            }
        }

        static class ShaderIDs
        {
            internal static readonly string BufferRT1 = "_BufferRT1";
            internal static readonly string BufferRT2 = "_BufferRT2";
            internal static readonly int BlurTexId = UnityEngine.Shader.PropertyToID("_BlurTex");
            internal static readonly int ContrastId = UnityEngine.Shader.PropertyToID("_Contrast");
            internal static readonly int IntensityId = UnityEngine.Shader.PropertyToID("_Intensity");
        }
        public override void Render(CommandBuffer cmd, RTHandle source, RTHandle target, ref RenderingData renderingData)
        {
            m_BlitMaterial.material.SetFloat(ShaderIDs.ContrastId, m_Settings.Contrast.value);
            m_BlitMaterial.material.SetFloat(ShaderIDs.IntensityId, m_Settings.Intensity.value);
            //
            // var desc = GetDefaultColorRTDescriptor(ref renderingData, 
            //     (int)(Screen.width / m_Settings.RTDownScaling.value), 
            //     (int)(Screen.height / m_Settings.RTDownScaling.value));
            // desc.colorFormat = RenderTextureFormat.ARGB32;
            // desc.sRGB = true;
            // RenderingUtils.ReAllocateIfNeeded(ref m_BufferRT1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: ShaderIDs.BufferRT1);
            //
            Blitter.BlitCameraTexture(cmd, source, target, m_BlitMaterial.material,4);
        }

    }
}


// Blitter.BlitCameraTexture(cmd, source, m_BufferRT1);
// Blitter.BlitCameraTexture(cmd, m_BufferRT1, m_BufferRT2, m_BlitMaterial.material, 0);
// Blitter.BlitCameraTexture(cmd, m_BufferRT2, m_BufferRT1, m_BlitMaterial.material, 1);
// Blitter.BlitCameraTexture(cmd, m_BufferRT1, m_BufferRT2, m_BlitMaterial.material, 2);
// m_BlitMaterial.material.SetTexture(ShaderIDs.BlurTexId, m_BufferRT2.rt);
// Blitter.BlitCameraTexture(cmd, source, m_BufferRT1);
// Blitter.BlitCameraTexture(cmd, m_BufferRT1, m_BufferRT2, m_BlitMaterial.material, 3);
// Blitter.BlitCameraTexture(cmd,m_BufferRT2,target);
//
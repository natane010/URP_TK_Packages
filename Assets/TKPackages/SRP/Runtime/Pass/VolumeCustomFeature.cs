#if URP_SETTINGS_PJ
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using TKPackages.SRP.Runtime.Base;

namespace TKPackages.SRP.Runtime.Pass
{
    [DisallowMultipleRendererFeature("TK Post Process")]
    public class VolumeCustomFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        [SerializeField]
        private Settings m_Settings;
        private PostProcessBasePass m_VolumePass;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.postProcessEnabled)
                renderer.EnqueuePass(m_VolumePass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            base.SetupRenderPasses(renderer, renderingData);

            m_VolumePass.renderPassEvent = m_Settings.renderPassEvent;
            m_VolumePass.Setup(renderer.cameraColorTargetHandle);
        }

        public override void Create()
        {
            m_VolumePass ??= new VolumeCustomPass();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            m_VolumePass?.Dispose();
            m_VolumePass = null;
        }
    }
}
#endif
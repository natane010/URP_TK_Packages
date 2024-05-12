using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TKPackages.SRP.Runtime.Base
{
    [DisallowMultipleRendererFeature("Custom Post Processing")]
    public class CustomVolumeFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        [SerializeField]
        private Settings m_Settings;
        private PostProcessPassBase _mPostProcessPass;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.postProcessEnabled)
                renderer.EnqueuePass(_mPostProcessPass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            base.SetupRenderPasses(renderer, renderingData);

            _mPostProcessPass.renderPassEvent = m_Settings.renderPassEvent;
            _mPostProcessPass.Setup(renderer.cameraColorTargetHandle);
        }

        public override void Create()
        {
            _mPostProcessPass ??= new CustomPostProcessPass();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _mPostProcessPass?.Dispose();
            _mPostProcessPass = null;
        }

    }
}

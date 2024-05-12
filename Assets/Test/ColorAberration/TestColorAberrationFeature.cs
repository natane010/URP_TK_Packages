using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Test.ColorAberration
{
    public class TestColorAberrationFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _shader;
        TestColorAberrationPass _renderPass = null;
    
        public override void Create()
        {
            if (_renderPass == null)
            {
                _renderPass = new TestColorAberrationPass(_shader);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_renderPass);
            // ConfigureInputはAddRenderPasses内で呼ぶこと
            _renderPass.ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            _renderPass.Setup(cameraTargetDescriptor);
        }

        protected override void Dispose(bool disposing)
        {
            _renderPass.Cleanup();
            _renderPass = null;
            base.Dispose(disposing);
        }
    }
}

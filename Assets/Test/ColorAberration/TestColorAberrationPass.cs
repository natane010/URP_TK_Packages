using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Test.ColorAberration
{
    public class TestColorAberrationPass : ScriptableRenderPass
    {
        private ProfilingSampler _profilingSampler = new ProfilingSampler("PostProcess");
    private MaterialLibrary m_Materials;
    private TestColorAberration m_ColorAberration;
    private static readonly int IntensityShaderId = Shader.PropertyToID("_AberrationIntensity");
    private RenderTextureDescriptor m_Descriptor;
    
    public TestColorAberrationPass(Shader uberPost)
    {
        m_Materials = new MaterialLibrary(uberPost);
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
    
    public void Cleanup() => m_Materials.Cleanup();

    public void Setup(in RenderTextureDescriptor baseDescriptor)
    {
        m_Descriptor = baseDescriptor;
        m_Descriptor.useMipMap = false;
        m_Descriptor.autoGenerateMips = false;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // ConfigureTargetはConfigure内で呼ぶこと
        // Configure内でSetRenderTargetによる任意のRenderTarget指定をしてはいけない
        // 今回は何もしない。
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (m_Materials == null)
        {
            Debug.LogError("PostProcessRenderPass.Execute: Material is null.");
            return;
        }

        if (renderingData.cameraData.cameraType == CameraType.SceneView || renderingData.cameraData.cameraType == CameraType.Preview)
        {
            return;
        }
        
        var stack = VolumeManager.instance.stack;
        m_ColorAberration = stack.GetComponent<TestColorAberration>();
        
        // ObjectPoolからCommandBufferを取り出す.
        CommandBuffer cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, _profilingSampler))
        {
            bool colorAberrationActive = m_ColorAberration.IsActive();
            if (colorAberrationActive)
            {
                Render(cmd, ref renderingData);
            }
        }
        
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }

    void Render(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // NOTE: URP標準PostProcessPassでのSwap処理について(PostProcessPass.Swap参照)
        // URP標準PostProcessPassでは、m_UseSwapBuffer(SwapBufferを使用するかどうかのフラグ)が常にtrueになっており、
        // 特に理由がない限り、RTHandleが確保しているFrontBufferとBackBufferのSwapで実現している。
        // SwapBufferを使用しない場合は、TempRTHandleを経由してBlitしている。
        // ScriptableRenderPass.Blit内でBlit処理とSwap処理をおこなっている。
        // また、Load、Storeの可否を明示的に指定することができる。URP標準PostProcessPassではLoad、Storeを明示的に指定している。
        // また、Load、Store処理が正常に動作するかはモバイル端末(Tile-Based Rendering)でのみ確認可能。
        // 今回は明示的な指定はしない。
        if (m_Materials.uber != null)
        {
            m_Materials.uber.SetFloat(IntensityShaderId, m_ColorAberration.intensity.value);
            Blit(cmd, ref renderingData, m_Materials.uber, 0);
        }
    }
    
    class MaterialLibrary
    {
        public readonly Material uber;
    
        public MaterialLibrary(Shader uberPost)
        {
            uber = Load(uberPost);
        }
        
        Material Load(Shader shader)
        {
            if (shader == null)
            {
                return null;
            }
            else if (!shader.isSupported)
            {
                return null;
            }

            return CoreUtils.CreateEngineMaterial(shader);
        }

        internal void Cleanup()
        {
            CoreUtils.Destroy(uber);
        }
    }
    }
}
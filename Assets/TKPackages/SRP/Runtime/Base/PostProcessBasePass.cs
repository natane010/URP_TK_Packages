#if URP_SETTINGS_PJ
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TKPackages.SRP.Runtime.Utility;

namespace TKPackages.SRP.Runtime.Base
{
    public abstract class PostProcessBasePass : ScriptableRenderPass
    {
        enum VolumeRendererState
        {
            WaitingToAdd,
            WaitingToRemove,
            ImmediatelyRemove,
            InExecution,
        }

        class PostProcessRendererMark
        {
            internal VolumeRendererState state;
            internal IPostProcessRenderer renderer;
        }

        protected abstract string PostProcessingTag { get; }
        private readonly ProfilingSampler m_PostProcessingProfiling;
        private readonly List<IPostProcessRenderer> m_VolumeRenderers;
        private readonly Dictionary<Type, PostProcessRendererMark> m_VolumeRendererMarkDict;
        private bool m_IsCheckMark;
        private RTHandle m_SourceRT;
        private RTHandle m_TempRT;
        private static int m_RTIndex = 0;
        private readonly string m_RTName = "_TempRT";

        public PostProcessBasePass()
        {
            m_RTName += m_RTIndex;
            m_RTIndex += 1;
            m_PostProcessingProfiling = new ProfilingSampler(PostProcessingTag);
            m_VolumeRenderers = new List<IPostProcessRenderer>();
            m_VolumeRendererMarkDict = new Dictionary<Type, PostProcessRendererMark>();
            OnInit();
        }

        protected abstract void OnInit();

        public void AddEffects(Assembly assembly)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (!type.IsDefined(typeof(PostProcessRendererPriority)))
                    continue;
                if (!typeof(IPostProcessRenderer).IsAssignableFrom(type))
                {
                    Debug.LogError($"Type is not IPostProcessRenderer: {type.FullName}");
                    continue;
                }
                if (m_VolumeRendererMarkDict.TryGetValue(type, out var mark))
                {
                    if (mark.state == VolumeRendererState.WaitingToRemove)
                    {
                        mark.state = VolumeRendererState.WaitingToAdd;
                        m_IsCheckMark = true;
                    }
                }
                else
                {
                    mark = new PostProcessRendererMark() { state = VolumeRendererState.WaitingToAdd, renderer = Activator.CreateInstance(type) as IPostProcessRenderer };
                    m_VolumeRendererMarkDict.Add(type, mark);
                    m_IsCheckMark = true;
                }
            }
        }

        public void AddEffect<T>() where T : IPostProcessRenderer
        {
            var type = typeof(T);
            if (!type.IsDefined(typeof(PostProcessRendererPriority)))
            {
                Debug.LogError($"Type do not have Priority: {type.FullName}");
                return;
            }
            if (m_VolumeRendererMarkDict.TryGetValue(type, out var mark))
            {
                if (mark.state == VolumeRendererState.WaitingToRemove)
                {
                    mark.state = VolumeRendererState.WaitingToAdd;
                    m_IsCheckMark = true;
                }
            }
            else
            {
                mark = new PostProcessRendererMark() { state = VolumeRendererState.WaitingToAdd, renderer = Activator.CreateInstance(type) as IPostProcessRenderer };
                m_VolumeRendererMarkDict.Add(type, mark);
                m_IsCheckMark = true;
            }
        }

        public void RemoveEffect<T>() where T : IPostProcessRenderer
        {
            var type = typeof(T);
            if (m_VolumeRendererMarkDict.TryGetValue(type, out var mark))
            {
                if (mark.state == VolumeRendererState.WaitingToAdd)
                {
                    mark.state = VolumeRendererState.ImmediatelyRemove;
                    m_VolumeRendererMarkDict.Remove(type);
                }
                else if (mark.state == VolumeRendererState.InExecution)
                {
                    mark.state = VolumeRendererState.WaitingToRemove;
                    m_IsCheckMark = true;
                }
            }
        }

        public void ResetAllEffects()
        {
            foreach (var renderer in m_VolumeRenderers)
            {
                renderer.Dispose();
                renderer.Init();
            }
        }

        public void Setup(RTHandle cameraColorTarget)
        {
            m_SourceRT = cameraColorTarget;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);

            var desc = cameraTextureDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: m_RTName);

            ConfigureTarget(m_SourceRT);
            ConfigureClear(ClearFlag.None, Color.white);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            cmd.Clear();

            using (new ProfilingScope(cmd, m_PostProcessingProfiling))
            {
                if (m_IsCheckMark)
                {
                    foreach (var mark in m_VolumeRendererMarkDict)
                    {
                        if (mark.Value.state == VolumeRendererState.WaitingToRemove)
                        {
                            mark.Value.state = VolumeRendererState.ImmediatelyRemove;
                            mark.Value.renderer.Dispose();
                            m_VolumeRenderers.Remove(mark.Value.renderer);
                            m_VolumeRendererMarkDict.Remove(mark.Key);
                        }
                        else if (mark.Value.state == VolumeRendererState.WaitingToAdd)
                        {
                            mark.Value.state = VolumeRendererState.InExecution;
                            mark.Value.renderer.Init();
                            m_VolumeRenderers.Add(mark.Value.renderer);
                        }
                    }
                    m_VolumeRenderers.Sort((a, b) =>
                    {
                        int aPriority = a.GetType().GetCustomAttribute<PostProcessRendererPriority>().priority;
                        int bPriority = b.GetType().GetCustomAttribute<PostProcessRendererPriority>().priority;
                        return aPriority <= bPriority ? -1 : 1;
                    });
                    m_IsCheckMark = false;
                }
                int count = 0;
                foreach (var renderer in m_VolumeRenderers)
                {
                    if (!renderer.IsActive(ref renderingData))
                        continue;

                    cmd.BeginSample(renderer.ProfilerTag);
                    renderer.Render(cmd, m_SourceRT, m_TempRT, ref renderingData);
                    CoreUtils.Swap(ref m_SourceRT, ref m_TempRT);
                    count++;
                    cmd.EndSample(renderer.ProfilerTag);
                }
                if (count > 0 && count % 2 != 0)
                {
                    cmd.BeginSample("FinalBlit");
                    Blitter.BlitCameraTexture(cmd, m_SourceRT, m_TempRT);
                    CoreUtils.Swap(ref m_SourceRT, ref m_TempRT);
                    cmd.EndSample("FinalBlit");
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            foreach (var renderer in m_VolumeRenderers)
            {
                renderer.Dispose();
            }
            m_VolumeRenderers.Clear();
            m_VolumeRendererMarkDict.Clear();
            RTHandles.Release(m_TempRT);
            m_TempRT = null;
            m_SourceRT = null;
        }
    }
}
#endif
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TKPackages.SRP.Runtime.Base
{
    public interface IPostProcessRenderer
    {
        string ProfilerTag { get; }
        bool IsActive(ref RenderingData renderingData);
        void Init();
        void Render(CommandBuffer cmd, RTHandle source, RTHandle target, ref RenderingData renderingData);
        void Dispose();
    }
}

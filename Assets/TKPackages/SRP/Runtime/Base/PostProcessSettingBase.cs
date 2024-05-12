using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TKPackages.SRP.Runtime.Base
{
    [Serializable]
    public abstract class PostProcessSettingBase : VolumeComponent, IPostProcessComponent
    {
        public abstract bool IsActive();
        public virtual bool IsTileCompatible() => false;
    }
}
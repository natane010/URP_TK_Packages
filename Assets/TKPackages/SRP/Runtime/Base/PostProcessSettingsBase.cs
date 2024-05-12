#if URP_SETTINGS_PJ
using System;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace TKPackages.SRP.Runtime.Base
{
    [Serializable]
    public abstract class PostProcessSettingsBase : VolumeComponent, IPostProcessComponent
    {
        public abstract bool IsActive();
        public virtual bool IsTileCompatible() => false;
    }
}
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Test.ColorAberration
{
    [Serializable, VolumeComponentMenuForRenderPipeline("InPro/ColorAberration", typeof(UniversalRenderPipeline))]
    public class TestColorAberration : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Strength of the color aberration.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, -1f, 1f);
    
        public bool IsActive() => intensity.value > 0f;

        public bool IsTileCompatible() => false;
    }
}

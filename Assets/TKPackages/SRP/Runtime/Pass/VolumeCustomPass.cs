#if URP_SETTINGS_PJ
using TKPackages.SRP.Runtime.Base;

namespace TKPackages.SRP.Runtime.Pass
{
    public class VolumeCustomPass : PostProcessBasePass
    {
        protected override string PostProcessingTag => "Render Original PostProcessing Effects";

        protected override void OnInit()
        {
            AddEffects(typeof(VolumeCustomPass).Assembly);
        }
    }
}
#endif
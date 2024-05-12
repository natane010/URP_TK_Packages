namespace TKPackages.SRP.Runtime.Base
{
    public class CustomPostProcessPass : PostProcessPassBase
    {
        protected override string PostProcessingTag => "Render Custom PostProcessing Effects";

        protected override void OnInit()
        {
            AddEffects(typeof(CustomPostProcessPass).Assembly);
        }

    }
}

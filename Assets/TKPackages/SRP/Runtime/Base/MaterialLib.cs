#if URP_SETTINGS_PJ
using UnityEngine;
using UnityEngine.Rendering;

namespace TKPackages.SRP.Runtime.Base
{
    [System.Serializable]
    public sealed class MaterialLib
    {
        public readonly Material material;
    
        public MaterialLib(Shader materialPost)
        {
            material = Load(materialPost);
        }
        
        Material Load(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogErrorFormat($"shaderが見つかりません");
                return null;
            }
            else if (!shader.isSupported)
            {
                Debug.LogErrorFormat($"shaderが対応していません");
                return null;
            }

            return CoreUtils.CreateEngineMaterial(shader);
        }

        internal void Cleanup()
        {
            CoreUtils.Destroy(material);
        }
    }
}
#endif

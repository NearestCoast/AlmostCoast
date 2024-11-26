using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _Project.Managers.Scripts._Core.VolumeManager
{
    public class RenderFeatureController : MonoBehaviour
    {
        public void EnableFeature(ScriptableRendererFeature feature)
        {
            Debug.Log(feature);
            feature.SetActive(true);
        }

        public void DisableFeature(ScriptableRendererFeature feature)
        {
            Debug.Log(feature);
            feature.SetActive(false);
        }
    }
}
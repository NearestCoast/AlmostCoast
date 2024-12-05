using System.Collections.Generic;
using FlatKit;
using UnityEngine;

namespace _Project.Managers.Scripts._Core.VolumeManager
{
    public class FogController : MonoBehaviour
    {
        [SerializeField] private FlatKitFog fogFeature;
        [SerializeField] private List<FogSettings> fogSettings;
        public void ChangeFogSetting(int chapterNumber)
        {
            fogFeature.settings = fogSettings[chapterNumber];
        }
    }
}
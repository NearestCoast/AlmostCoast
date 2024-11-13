using System;
using System.Collections.Generic;
using _Project._Core;
using _Project.Combat.HitObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class SpotLightSwitch : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private Level level;

        public Level Level
        {
            get => level;
            set => level = value;
        }
        
        [SerializeField] private string id;

        public string ID
        {
            get => id;
            set => id = value;
        }

        [SerializeField] private SpotLight containerLight;
        [SerializeField] private List<SpotLight> spotLights;

        public SpotLight ContainerLight
        {
            get => containerLight;
            set => containerLight = value;
        }

        public List<SpotLight> SpotLights
        {
            get => spotLights;
            set => spotLights = value;
        }

        [SerializeField] private Material spotlightOnMaterial;
        [SerializeField] private Material spotlightOffMaterial;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Resources 폴더 경로에서 Material 불러오기
            spotlightOnMaterial = Resources.Load<Material>("M.SpotLight.On");
            spotlightOffMaterial = Resources.Load<Material>("M.SpotLight.Off");
        }  
#endif

        // private bool IsDamageTaking { get; set; }
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("HitObject")) return;
            if (other.gameObject.GetComponent<HitObject>() is HitObjectExplosion hitObject)
            {
                TakeDamage(new HittingInfo(hitObject, Vector3.zero), 0);
            }
        }

        public void TakeDamage(HittingInfo hittingInfo, int damage, SideEffect sideEffect = SideEffect.None)
        {
            if (hittingInfo.hitObject is not HitObjectExplosion) return;
            
            foreach (var spotLight in spotLights)
            {
                if (spotLight.IsStatic) return; 
                spotLight.Toggle();
            }
            
            var isAllSpotLightOn = SetMaterial();
        }

        public bool SetMaterial()
        {
            var result = 0;
            foreach (var spotLight in spotLights)
            {
                if (spotLight.IsOn) result += 1;
            }

            var isAllSpotLightOn = result > 0; 
            GetComponent<MeshRenderer>().sharedMaterial = isAllSpotLightOn ? spotlightOnMaterial : spotlightOffMaterial;
            
            if (containerLight)
            {
                if (isAllSpotLightOn) containerLight.TurnOn(false);
                else containerLight.TurnOff(false);
            }

            return isAllSpotLightOn;
        }
    }
}
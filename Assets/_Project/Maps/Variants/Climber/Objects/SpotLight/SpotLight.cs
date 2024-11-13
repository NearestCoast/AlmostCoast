
using System;
using _Project.Characters.IngameCharacters.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Maps.Climber.Objects
{
    public class SpotLight : MonoBehaviour
    {
        [SerializeField] private string containerSwitchID;
        [SerializeField] private bool isStatic;
        [SerializeField] private Light lightCompo;
        [SerializeField] private SpotLightSwitch ownSwitch;
        [SerializeField] private SpotLightSwitch containerSwitch;
        [SerializeField] private bool isSwitchOnly;
        [SerializeField] private bool isOn;

        public bool IsOn
        {
            get => isOn;
            set => isOn = value;
        }

        [SerializeField] private Color staticColor;
        [SerializeField] private Color nonStaticColor;
        
        private bool IsPlayerInArea { get; set; }

        public string ContainerSwitchID
        {
            get => containerSwitchID;
            set => containerSwitchID = value;
        }

        public SpotLightSwitch OwnSwitch
        {
            get => ownSwitch;
            set => ownSwitch = value;
        }

        public SpotLightSwitch ContainerSwitch
        {
            get => containerSwitch;
            set => containerSwitch = value;
        }

        public bool IsStatic
        {
            get => isStatic;
            set
            {
                lightCompo.color = value ? staticColor : nonStaticColor;
                isStatic = value;
            }
        }

        public Light LightCompo
        {
            get => lightCompo;
            set => lightCompo = value;
        }

        public bool IsSwitchOnly
        {
            get => isSwitchOnly;
            set => isSwitchOnly = value;
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            lightCompo ??= GetComponent<Light>();
        }
#endif

        private LayerMask surfaceLayers;
        private void Start()
        {
            SetIntensity();

            void SetIntensity()
            {
                if (lightCompo.intensity == 0) return;
                surfaceLayers = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Wall");
                var ray = new Ray(transform.position, Vector3.down);
                var isHit = Physics.Raycast(ray, out var hitInfo, float.MaxValue, surfaceLayers);

                if (isHit)
                {
                    lightCompo.intensity = Mathf.Pow(hitInfo.distance, 2) * 1;
                }
            }
        }

        public void Toggle()
        {
            if (IsOn) TurnOff();
            else TurnOn();
        }

        public void TurnOn(bool setMaterial = true)
        {
            IsOn = true;
            if (!isSwitchOnly) lightCompo.enabled = true;
            
            if (setMaterial)
            {
                OwnSwitch.SetMaterial();
                if (containerSwitch) containerSwitch.SetMaterial();
            }
        }

        public void TurnOff(bool setMaterial = true)
        {
            IsOn = false;
            lightCompo.enabled = false;
            IsPlayerInArea = false;
            
            if (ingameCharacter) ingameCharacter.CurrentSpotLight = null;
            
            if (setMaterial)
            {
                OwnSwitch.SetMaterial();
                if (containerSwitch) containerSwitch.SetMaterial();
            }
        }       


        private IngameCharacter ingameCharacter;
        private void OnTriggerStay(Collider other)
        {
            if (!IsOn) return;
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                ingameCharacter = other.attachedRigidbody.GetComponent<IngameCharacter>();
                ingameCharacter.CurrentSpotLight = this;
                IsPlayerInArea = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayerInArea) return;
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                ingameCharacter = other.attachedRigidbody.GetComponent<IngameCharacter>();
                IsPlayerInArea = false;
                
                if (ingameCharacter.CurrentSpotLight == this) ingameCharacter.CurrentSpotLight = null;
            }
        }
    }
}
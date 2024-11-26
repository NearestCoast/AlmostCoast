using System;
using _Project.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

namespace _Project.Managers.Scripts._Core.VolumeManager
{
    public class VolumeTrigger : MonoBehaviour
    {
        private LayerMask targetLayers;
        private void Awake()
        {
            targetLayers = 1 << LayerMask.NameToLayer("Player");
        }

        [SerializeField] private UnityEvent onEnter;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer.IsInLayerMask(targetLayers))
            {
                Debug.Log(name);
                onEnter?.Invoke();
            }
        }
    }
}
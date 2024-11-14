using System;
using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Characters.IngameCharacters.Core;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Character.IngameCharacters.Enemies
{
    public class PlayerEnteringChecker : MonoBehaviour
    {
        [SerializeField] private Enemy master;
        [SerializeField] private SphereCollider trigger;
        public float TriggerRadius => trigger.radius;

#if UNITY_EDITOR
        private void OnValidate()
        {
            master = GetComponentInParent<Enemy>();
            trigger = GetComponent<SphereCollider>(); 
        }
#endif

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody)
            {
                if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    var pC = other.attachedRigidbody.GetComponent<PlayerCharacter>();
                    if (pC)
                    {
                        master.StartBehaviour();
                    }
                }
            }
        }
    }
}
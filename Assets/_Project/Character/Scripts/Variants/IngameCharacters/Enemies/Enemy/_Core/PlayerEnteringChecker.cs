using System;
using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Characters.IngameCharacters.Core;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Character.IngameCharacters.Enemies
{
    public class PlayerEnteringChecker : MonoBehaviour
    {
        private Enemy master;
        private SphereCollider trigger;
        
        private void Awake()
        {
            master = GetComponentInParent<Enemy>();
        }

        private void Start()
        {
            // trigger = GetComponent<SphereCollider>();
            // trigger.enabled = true;
            // trigger.radius /= master.CharacterControllerEnveloper.CurrentScale;
        }

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
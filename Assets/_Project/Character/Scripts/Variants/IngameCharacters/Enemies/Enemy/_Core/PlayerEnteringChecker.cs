using System;
using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Characters.IngameCharacters.Core;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Character.IngameCharacters.Enemies
{
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerEnteringChecker : MonoBehaviour
    {
        [SerializeField] private Enemy master;
        [SerializeField] private SphereCollider trigger;
        [SerializeField] private float radius = 60;
        public float TriggerRadius => trigger.radius;

#if UNITY_EDITOR
        private void OnValidate()
        {
            master = GetComponentInParent<Enemy>();
            trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
        }
#endif

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                var pC = other.gameObject.GetComponent<PlayerCharacter>();
                if (pC)
                {
                    master.StartBehaviour();
                }
            }
        }
    }
}
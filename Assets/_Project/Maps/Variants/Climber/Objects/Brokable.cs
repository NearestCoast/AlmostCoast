using System;
using _Project._Core;
using _Project.Combat.HitObjects;
using _Project.Utils;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class Brokable : MonoBehaviour, IDamageReceiver
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
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("HitObject")) return;
            if (other.gameObject.GetComponent<HitObject>() is HitObjectMeleeAttack hitObject)
            {
                TakeDamage(new HittingInfo(hitObject, Vector3.zero), 0);
            }
        }

        public void TakeDamage(HittingInfo hittingInfo, int damage, SideEffect sideEffect = SideEffect.None)
        {
            // Debug.Log("Come");
            Destroy(gameObject);
        }
    }
}
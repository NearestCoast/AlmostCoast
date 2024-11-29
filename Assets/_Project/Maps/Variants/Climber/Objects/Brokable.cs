using System;
using _Project._Core;
using _Project.Combat.HitObjects;
using _Project.Managers.Scripts._Core.SaveManager;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class Brokable : MonoBehaviour, IDamageReceiver, ISavable
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
        
        private bool IsBroken { get; set; }

        private void Start()
        {
            if (IsBroken) Destroy(gameObject);
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
            destroy();

            void destroy()
            {
                Destroy(gameObject);
                IsBroken = true;
            }
        }

        public bool EnrollToSaveManager => true;
        public bool Save(string saveFileName)
        {
            ISavable.EasySave($"Brokable_{id}", IsBroken, saveFileName);
            return true;
        }

        public bool Load(string saveFileName)
        {
            IsBroken = ISavable.EasyLoad<bool>($"Brokable_{id}", saveFileName);
            return true;
        }
    }
}
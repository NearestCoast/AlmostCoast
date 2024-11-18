using System;
using UnityEngine;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class Collectable : MonoBehaviour
    {
        private bool isWorked;
        
        private void OnTriggerEnter(Collider other)
        {
            if (isWorked) return;
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Work();
                isWorked = true;
            }
        }

        protected virtual void Work()
        {
            
        }
    }
}
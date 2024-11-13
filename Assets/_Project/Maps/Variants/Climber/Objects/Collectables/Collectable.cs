using System;
using UnityEngine;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class Collectable : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Work();
            }
        }

        protected virtual void Work()
        {
            
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core
{
    [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
    public class SpecificTrigger : MonoBehaviour
    {
        [SerializeField] private LayerMask surfaceLayers;
        // [ShowInInspector] public bool IsHit => hitColliders.Count > 0;
        [ShowInInspector] public bool IsHit
        {
            get
            {
                Debug.Log(name + " hitColliders.Count : " + hitColliders.Count);
                int errorCount = 0;
                foreach (var hitCollider in hitColliders)
                {
                    if (!hitCollider) errorCount += 1;
                }
                
                if (hitColliders.Count == errorCount) return false;
                
                if (hitColliders.Count > 0)
                {
                    Debug.Log(hitColliders.First());
                    return true;
                }

                

                

                return false;
            }
        }

        [ShowInInspector]private readonly HashSet<Collider> hitColliders = new HashSet<Collider>();
         
        private void OnTriggerStay(Collider other)
        {
            foreach (var hitCollider in hitColliders)
            {
                if (!hitCollider)
                {
                    Reset();
                    break;
                }
            }

            if (other.gameObject.layer.IsInLayerMask(surfaceLayers))
            {
                hitColliders.Add(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer.IsInLayerMask(surfaceLayers))
            {
                hitColliders.Remove(other);
            }
        }

        public void Reset()
        {
            hitColliders.Clear();
        }
    }
}
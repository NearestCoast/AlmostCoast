using System;
using System.Collections.Generic;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core
{
    [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
    public class SpecificTrigger : MonoBehaviour
    {
        [SerializeField] private LayerMask surfaceLayers;
        [ShowInInspector] public bool IsHit => hitColliders.Count > 0;

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
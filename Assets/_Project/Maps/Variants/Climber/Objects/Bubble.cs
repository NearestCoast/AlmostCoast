using System;
using _Project.Characters._Core;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class Bubble : MonoBehaviour
    {
        private Rigidbody Holdings { get; set; }
        private void OnTriggerEnter(Collider other)
        {
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody == Holdings) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Holdings = other.attachedRigidbody;
                
                ActionsStateParams.SetBubble(this);
                
                var playerCharacter = other.attachedRigidbody.GetComponent<IngameCharacter>();
                playerCharacter.PlayBubbleReady();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Holdings = null;
                
            ActionsStateParams.ReleaseBubble();
        }
    }
}
using System;
using _Project.Characters._Core;
using _Project.Characters.IngameCharacters.Core;
using _Project.Utils;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class SavePoint : MonoBehaviour
    {
        [SerializeField] private Level level;

        public Level Level
        {
            get => level;
            set => level = value;
        }

        private LayerMask targetLayers;

        private void Awake()
        {
            targetLayers = 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Character");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer.IsInLayerMask(targetLayers))
            {
                var character = other.attachedRigidbody.GetComponent<IngameCharacter>();
                
                if (character.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    if (character.CurrentLevel != level) level.StartLevel();
                }
                
                character.CurrentLevel = level;
                character.SavePoint = this;
            }
        }
    }
}
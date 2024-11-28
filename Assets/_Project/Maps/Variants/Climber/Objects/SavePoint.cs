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
            set
            {
                level = value;
                level.SavePoints.Add(this);
            }
        }

        private LayerMask targetLayers;

        private void Awake()
        {
            targetLayers = 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Character");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer.IsInLayerMask(targetLayers))
            {
                var character = other.gameObject.GetComponent<IngameCharacter>();
                character.SavePoint = this;
            }
        }
    }
}
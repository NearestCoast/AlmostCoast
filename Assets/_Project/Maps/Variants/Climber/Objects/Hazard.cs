using System;
using System.Collections.Generic;
using _Project.Characters._Core;
using _Project.Characters.IngameCharacters.Core;
using _Project.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class Hazard : MonoBehaviour
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
            characters = new List<IngameCharacter>();
        }

        private List<IngameCharacter> characters;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer.IsInLayerMask(targetLayers))
            {
                var character = other.attachedRigidbody.GetComponent<IngameCharacter>();
                if (characters.Contains(character)) return;
                if (character.IsDying || character.IsDead) return;
                character.MoveToSavePoint();
                characters.Add(character);
                RemoveCharacterAfterDelay(character);
            }
        }

        private async void RemoveCharacterAfterDelay(IngameCharacter character)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            characters.Remove(character);
        }
    }
}
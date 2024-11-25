using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Maps.Climber.Objects.Collectables;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class KeyDoor : LeverDoor
    {
        [SerializeField] private Key.Type keyType;

        public Key.Type KeyType
        {
            get => keyType;
            set => keyType = value;
        }

        private PlayerCharacter playerCharacter;

        protected override void Awake()
        {
            base.Awake();
            playerCharacter = FindAnyObjectByType<PlayerCharacter>();
        }

        public override void Open()
        {
            playerCharacter.TryUseKey(keyType, out var success);
            if (!success) return;
            base.Open();
        }
    }
}
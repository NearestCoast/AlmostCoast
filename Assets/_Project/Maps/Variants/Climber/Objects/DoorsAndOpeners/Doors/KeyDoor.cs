using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Inventories;
using _Project.Inventories.Items;
using _Project.Maps.Climber.Objects.Collectables;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class KeyDoor : LeverDoor
    {
        [SerializeField] private KeyData.KeyType keyType;

        public KeyData.KeyType KeyType
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
            playerCharacter.InventoryMaster.TryUse(new KeyData(IInventory.Type.Key, KeyType));
            base.Open();
        }
    }
}
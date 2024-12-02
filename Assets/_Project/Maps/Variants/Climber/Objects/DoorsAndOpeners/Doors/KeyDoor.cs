using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Inventories;
using _Project.Inventories.Items;
using _Project.Managers.Scripts._Core.SaveManager;
using _Project.Maps.Climber.Objects.Collectables;
using UnityEngine;

namespace _Project.Maps.Climber.Objects
{
    public class KeyDoor : LeverDoor, ISavable
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

        private bool IsOpened { get; set; }
        public override void Open()
        {
            playerCharacter.InventoryMaster.TryUse(new KeyData(IInventory.Type.Key, KeyType), out var success);
            
            if (!success) return;
            IsOpened = true;
            base.Open();

            var saveLoadManager = FindAnyObjectByType<SaveLoadManager>();
            saveLoadManager.SaveGame();
        }

        public bool EnrollToSaveManager => true;
        public bool Save(string saveFileName)
        {
            ISavable.EasySave($"{ID}", IsOpened, saveFileName);
            return true;
        }

        public bool Load(string saveFileName)
        {
            IsOpened = ISavable.EasyLoad<bool>($"{ID}", saveFileName);
            if (IsOpened) base.Open();
            return true;
        }
    }
}
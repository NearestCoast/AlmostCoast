using _Project.Inventories;
using UnityEngine;
using _Project.Inventories.Items;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class KeyCollectable : Collectable
    {

        [SerializeField] private KeyData.KeyType type;

        public KeyData.KeyType Type => type;
        
        protected override void Work()
        {
            base.Work();
            // playerCharacter.AddKey(type);
            var key = new KeyData(IInventory.Type.Key, KeyData.KeyType.Silver);
            playerCharacter.InventoryMaster.Add(key);
            Debug.Log("Player Collected " + Type + " Key");
        }
    }
}
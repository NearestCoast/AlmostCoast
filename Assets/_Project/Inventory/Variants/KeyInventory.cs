using _Project.Inventories.Items;
using _Project.Maps.Climber.Objects.Collectables;
using UnityEngine;

namespace _Project.Inventories
{
    public class KeyInventory : IInventory
    {
        public IInventory.Type InvType { get; }
        public void Add(ItemData itemData)
        {
            throw new System.NotImplementedException();
        }

        public void TryUse(ItemData itemData)
        {
            throw new System.NotImplementedException();
        }
    }
}
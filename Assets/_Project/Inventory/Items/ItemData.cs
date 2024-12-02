using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Inventories.Items
{
    [Serializable]
    public class ItemData
    {
        [SerializeField] private IInventory.Type invType;

        [SerializeField] private Sprite sprite;
        public Sprite Sprite => sprite;
        
        public ItemData(IInventory.Type invType)
        {
            this.invType = invType;
        }
    }
}
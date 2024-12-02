using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Inventories.Items
{
    [Serializable]
    public class ItemData
    {
        [SerializeField] private IInventory.Type invType;
        public ItemData(IInventory.Type invType)
        {
            this.invType = invType;
        }
    }
}
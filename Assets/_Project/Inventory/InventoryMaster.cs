using System;
using System.Collections.Generic;
using _Project.Inventories.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Inventories
{
    public interface IInventory
    {
        public enum Type
        {
            None,
            Key,
        }
        
        public Type InvType { get; }

        public void Add(ItemData itemData);
        public void TryUse(ItemData itemData);
    }
    
    [Serializable]
    public class ItemStack
    {
        [SerializeField] private List<ItemData> items;
        public List<ItemData> Items => items;
    }
    
    public class InventoryMaster : MonoBehaviour
    {
        
        [ShowInInspector] private Dictionary<IInventory.Type, IInventory> inventoriesDict;

        // 인덱서 추가
        public IInventory this[IInventory.Type type]
        {
            get
            { 
                if (inventoriesDict.TryGetValue(type, out var inventory))
                {
                    return inventory;
                }
                throw new KeyNotFoundException($"Inventory of type {type} not found.");
            }
        }

        private void Awake()
        {
            inventoriesDict = new Dictionary<IInventory.Type, IInventory>();
            var inventories = GetComponentsInChildren<IInventory>();
            foreach (var inventory in inventories)
            { 
                inventoriesDict.Add(inventory.InvType, inventory);
            }
        }

        public void Add(ItemData itemData)
        {
            switch (itemData)
            {
                case KeyData keyData:
                {
                    inventoriesDict[IInventory.Type.Key].Add(keyData);
                    break;
                }
            }
        }

        public void TryUse(ItemData itemData)
        {
            switch (itemData)
            {
                case KeyData keyData:
                {
                    inventoriesDict[IInventory.Type.Key].TryUse(keyData);
                    break;
                }
            }
        }
    }
}
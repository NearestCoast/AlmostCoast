using System;
using System.Collections.Generic;
using _Project.Inventories.Items;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

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
        
        public IEnumerable<ItemStack> ItemStacks { get; }
        
        public void Add(ItemData itemData);
        public void TryUse(ItemData itemData, out bool success);
    }
    
    [Serializable]
    public class ItemStack
    {
        [SerializeField] private int amount;
        public int Amount => amount;

        [SerializeField] private UnityEvent<int> onUpdate = new UnityEvent<int>();
        public UnityEvent<int> OnUpdate => onUpdate;

        [SerializeField] private Sprite sprite;
        public Sprite Sprite => sprite;

        public ItemStack(int amount, Sprite sprite)
        {
            this.amount = amount;
            this.sprite = sprite;
            onUpdate?.Invoke(this.amount);
        }

        public void Add()
        {
            amount += 1;
            Debug.Log("COmeeeeeeeeeeeeeeeeeeeeeeeeee");
            onUpdate?.Invoke(amount);
        }

        public void TryUse(out bool success)
        {
            success = false;
            if (amount > 0)
            {
                amount -= 1;
                success = true;
                onUpdate?.Invoke(amount);
            }
        }

        public void Load(int amount)
        {
            this.amount = amount;
            onUpdate?.Invoke(amount);
        }

        public void UpdateUI()
        {
            onUpdate?.Invoke(amount);
        }
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

        public void TryUse(ItemData itemData, out bool success)
        {
            success = false;
            switch (itemData)
            {
                case KeyData keyData:
                {
                    inventoriesDict[IInventory.Type.Key].TryUse(keyData, out success);
                    break;
                }
            }
        }
    }
}
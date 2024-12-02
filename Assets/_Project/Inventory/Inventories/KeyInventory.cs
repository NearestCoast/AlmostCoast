using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Inventories.Items;
using _Project.Managers.Scripts._Core.SaveManager;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Inventories
{
    [DefaultExecutionOrder(-10)]
    public class KeyInventory : MonoBehaviour, IInventory, ISavable
    {
        [SerializeField] private ItemDataMaster itemDataMaster;
        public IInventory.Type InvType => IInventory.Type.Key;
        [ShowInInspector] private Dictionary<KeyData.KeyType, ItemStack> ItemStackDict { get; set; }
        public IEnumerable<ItemStack> ItemStacks => ItemStackDict.Values;

        private void Awake()
        {
            ItemStackDict = new Dictionary<KeyData.KeyType, ItemStack>()
            {
                {KeyData.KeyType.Gold, new ItemStack(0, getSprite(KeyData.KeyType.Gold))},
                {KeyData.KeyType.Silver, new ItemStack(0, getSprite(KeyData.KeyType.Silver))},
            };

            return;
            Sprite getSprite(KeyData.KeyType keyType)
            {
                return itemDataMaster.KeyDatas.Find(x => x.Type == keyType).Sprite;
            }
        }

        public void Add(ItemData itemData)
        {
            if (itemData is KeyData keyData)
            {
                ItemStackDict[keyData.Type].Add();
            }
        }

        public void TryUse(ItemData itemData, out bool success)
        {
            success = false;
            if (itemData is KeyData keyData)
            {
                ItemStackDict[keyData.Type].TryUse(out success);
            }
        }

        public bool EnrollToSaveManager => true;
        public bool Save(string saveFileName)
        {
            foreach (var keyValuePair in ItemStackDict)
            {
                var keyType = keyValuePair.Key;
                var amount = keyValuePair.Value.Amount;
                ISavable.EasySave($"Player_KeyInventory_{keyType}", amount, saveFileName);
            }
            return true;
        }

        public bool Load(string saveFileName)
        {
            Debug.Log("################### Load ###############################");
            var keyTypes = ItemStackDict.Keys.ToList();
            foreach (var keyType in keyTypes)
            {
                var amount = ISavable.EasyLoad<int>($"Player_KeyInventory_{keyType}", saveFileName);
                ItemStackDict[keyType].Load(amount);
            }
            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Inventories.Items;
using _Project.Managers.Scripts._Core.SaveManager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Inventories
{
    public class KeyInventory : MonoBehaviour, IInventory, ISavable
    {
        public IInventory.Type InvType => IInventory.Type.Key;
        [ShowInInspector] private Dictionary<KeyData.KeyType, int> ItemStackDict { get; set; }

        private void Awake()
        {
            ItemStackDict = new Dictionary<KeyData.KeyType, int>()
            {
                {KeyData.KeyType.Gold, 0},
                {KeyData.KeyType.Silver, 0},
            };
        }

        public void Add(ItemData itemData)
        {
            if (itemData is KeyData keyData)
            {
                ItemStackDict[keyData.Type] += 1;
            }
        }

        public void TryUse(ItemData itemData, out bool success)
        {
            success = false;
            if (itemData is KeyData keyData)
            {
                if (ItemStackDict[keyData.Type] > 0)
                {
                    ItemStackDict[keyData.Type] -= 1;
                    success = true;
                }
            }
        }

        public bool EnrollToSaveManager => true;
        public bool Save(string saveFileName)
        {
            foreach (var keyValuePair in ItemStackDict)
            {
                var keyType = keyValuePair.Key;
                var amount = keyValuePair.Value;
                ISavable.EasySave($"Player_KeyInventory_{keyType}", amount, saveFileName);
            }
            return true;
        }

        public bool Load(string saveFileName)
        {
            var keyTypes = ItemStackDict.Keys.ToList();
            foreach (var keyType in keyTypes)
            {
                ItemStackDict[keyType] = ISavable.EasyLoad<int>($"Player_KeyInventory_{keyType}", saveFileName);
            }
            return true;
        }
    }
}
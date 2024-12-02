using System;
using UnityEngine;

namespace _Project.Inventories.Items
{
    [Serializable]
    public class KeyData : ItemData
    {
        public enum KeyType
        {
            Silver,
            Gold
        }

        [SerializeField] private KeyType keyType;
        public KeyType Type => keyType;

        public KeyData(IInventory.Type invType, KeyType keyType) : base(invType)
        {
            this.keyType = keyType;
        }
    }
}
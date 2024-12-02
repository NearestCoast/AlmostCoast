using System.Collections.Generic;
using System.Linq;
using _Project.Inventories;
using _Project.Inventories.Items;
using UnityEngine;

namespace _Project.UI.InGame
{
    public class InventoryUI : MonoBehaviour
    {
        private IInventory inventory;
        [SerializeField] private IInventory.Type targetInventoryType;
        [SerializeField] private ItemStackUI itemStackUIPrefab;

        private void Start()
        {
            var inventoryMaster = transform.GetComponentInParent<InventoryMaster>();
            inventory = inventoryMaster[targetInventoryType];
            foreach (var inventoryItemStack in inventory.ItemStacks)
            {
                var itemStackUI = Instantiate(itemStackUIPrefab.gameObject, transform).GetComponent<ItemStackUI>();
                inventoryItemStack.OnUpdate.AddListener(itemStackUI.UpdateUI);
                itemStackUI.Initialize(inventoryItemStack.Sprite);
                
                inventoryItemStack.UpdateUI();
            }
        }
    }
}
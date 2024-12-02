using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.UI.InGame
{
    public class ItemStackUI : MonoBehaviour
    {
        [SerializeField] private int initialAmount = 10;
        private ItemUI prefab;

        [SerializeField] private List<ItemUI> instances;

        public void Initialize(Sprite sprite)
        {
            prefab = transform.Find("Prefab").GetComponent<ItemUI>();
            prefab.SetImage(sprite);

            instances = new List<ItemUI>();
            for (var i = 0; i < initialAmount; i++)
            {
                var instance = Instantiate(prefab, transform);
                instance.name = "Instance";
                instance.gameObject.SetActive(true);
                instance.Activate();
                instances.Add(instance);
            }
        }
        
        public void UpdateUI(int amount)
        {
            Debug.Log("Update ItemStack UI");
            foreach (var instance in instances)
            {
                instance.Deactivate();
            }

            for (var i = 0; i < amount; i++)
            {
                instances[i].Activate();
            }
        }
    }
}
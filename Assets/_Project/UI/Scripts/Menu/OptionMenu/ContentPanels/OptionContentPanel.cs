using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.UI.OptionMenu
{
    public class OptionContentPanel : MonoBehaviour
    {
        public OptionContentItem[] ContentItems { get; private set; }

        private void Awake()
        {
            ContentItems = GetComponentsInChildren<OptionContentItem>();
        }

        public void OpenPanel()
        {
            gameObject.SetActive(true);
        }
        
        public void ClosePanel()
        {
            gameObject.SetActive(false);
            foreach (var optionContentItem in ContentItems)
            {
                optionContentItem.OnDeselect(null);
            }
        }
    }
}
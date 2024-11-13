using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.UI.OptionMenu
{
    [DefaultExecutionOrder(12)]
    public class OptionCanvas : MonoBehaviour
    {
        [SerializeField] private bool initialActiveState;

        [SerializeField] private UIButton firstSelected;
        [SerializeField] private UIButton escapeSelected;

        [SerializeField] private OptionContentPanel[] panels;

        private void Awake()
        {
            panels = GetComponentsInChildren<OptionContentPanel>();
        }

        private void Start()
        {
            gameObject.SetActive(initialActiveState);
        }

        private void OnEnable()
        {
            firstSelected.Select();
        }

        private void OnDisable()
        {
            if (EventSystem.current && EventSystem.current.currentSelectedGameObject)
            {
                var current = EventSystem.current.currentSelectedGameObject.GetComponent<UIButton>();
                if (current) current.OnDeselect(null);
            }
            if (escapeSelected) escapeSelected.Select();
        }

        public void OpenCanvas()
        {
            gameObject.SetActive(true);
        }

        public void CloseCanvas()
        {
            gameObject.SetActive(false);
        }

        public void ClosePanelsExceptCurrentSelected(OptionContentPanel currentPanel)
        {
            foreach (var optionContentPanel in panels)
            {
                if (optionContentPanel != currentPanel)
                {
                    optionContentPanel.ClosePanel();
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using _Project.UI.Menu._Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.UI.OptionMenu
{
    [DefaultExecutionOrder(12)]
    public class OptionCanvas : AdditionalCanvas
    {
        [SerializeField] private OptionContentPanel[] panels;

        protected override void Awake()
        {
            base.Awake();
            panels = GetComponentsInChildren<OptionContentPanel>();
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
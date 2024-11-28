using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _Project.UI.MainMenu
{
    public class MenuButton : UIButton
    {
        private Text buttonText;
        
        [SerializeField] private Color colorSelected = new Color(0, 0, 0, 1);
        [SerializeField] private Color colorDeselected = new Color(1, 1, 1, 1);
        
        protected override void Awake()
        {
            base.Awake();
            buttonText = GetComponentInChildren<Text>();
        }

        protected override void Start()
        {
            base.Start();
            buttonText.color = colorDeselected;
            buttonText.fontSize = (int)(Screen.height * 0.05f);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            buttonText.color = colorSelected;
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            buttonText.color = isSelected ? colorSelected : colorDeselected;
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            buttonText.color = colorSelected;
        }
        
        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            buttonText.color = colorDeselected;
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
        }

        protected void SetColorSelected()
        {
            buttonText.color = colorSelected;
        }
    }
}
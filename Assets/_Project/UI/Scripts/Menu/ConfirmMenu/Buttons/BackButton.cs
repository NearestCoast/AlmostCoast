using _Project.UI.MainMenu;
using _Project.UI.OptionMenu;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.UI.ConfirmMenu
{
    public class BackButton : MenuButton
    {
        private OptionCanvas optionCanvas;
        protected override void Awake()
        {
            base.Awake();
            optionCanvas = GetComponentInParent<OptionCanvas>();   
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            optionCanvas.CloseCanvas();
        }

        public override void OnCancel(BaseEventData eventData)
        {
            base.OnCancel(eventData);
            optionCanvas.CloseCanvas();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            OnSubmit(eventData);
        }
    }
}
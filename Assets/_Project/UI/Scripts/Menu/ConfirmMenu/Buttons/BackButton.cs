using _Project.UI.MainMenu;
using _Project.UI.Menu._Core;
using _Project.UI.OptionMenu;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.UI.ConfirmMenu
{
    public class BackButton : MenuButton
    {
        private AdditionalCanvas additionalCanvas;
        protected override void Awake()
        {
            base.Awake();
            additionalCanvas = GetComponentInParent<AdditionalCanvas>();   
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            additionalCanvas.CloseCanvas();
        }

        public override void OnCancel(BaseEventData eventData)
        {
            base.OnCancel(eventData);
            additionalCanvas.CloseCanvas();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            OnSubmit(eventData);
        }
    }
}
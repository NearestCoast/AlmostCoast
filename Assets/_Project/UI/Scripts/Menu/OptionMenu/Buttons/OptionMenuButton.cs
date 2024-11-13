using _Project.InputSystem;
using _Project.UI.MainMenu;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.UI.OptionMenu
{
    public class OptionMenuButton : MenuButton
    {
        [SerializeField] private OptionContentPanel contentPanel;
        
        private OptionCanvas optionCanvas;
        protected override void Awake()
        {
            base.Awake();
            optionCanvas = GetComponentInParent<OptionCanvas>();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            contentPanel.OpenPanel();
            optionCanvas.ClosePanelsExceptCurrentSelected(contentPanel);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
        }

        public override void OnCancel(BaseEventData eventData)
        {
            base.OnCancel(eventData);
            optionCanvas.CloseCanvas();
        }
    }
}
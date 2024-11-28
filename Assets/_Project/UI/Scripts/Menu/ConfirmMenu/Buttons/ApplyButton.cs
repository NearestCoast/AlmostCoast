using _Project.UI.MainMenu;
using _Project.UI.Menu._Core;
using _Project.UI.OptionMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.UI.ConfirmMenu
{
    public class ApplyButton : MenuButton
    {
        private AdditionalCanvas additionalCanvas;
        private OptionContentPanel[] optionContentPanels;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button backButton;
        
        protected override void Awake()
        {
            base.Awake();
            optionContentPanels = FindObjectsByType<OptionContentPanel>(FindObjectsSortMode.None);
            additionalCanvas = GetComponentInParent<AdditionalCanvas>();
        }

        public void SetUpperSelectable(Selectable selectable)
        {
            applyButton.navigation = new Navigation()
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = selectable,
                selectOnRight = backButton,
            };
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            foreach (var contentPanel in optionContentPanels)
            {
                foreach (var contentItem in contentPanel.ContentItems)
                {
                    contentItem.Apply();
                }
            }
            
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
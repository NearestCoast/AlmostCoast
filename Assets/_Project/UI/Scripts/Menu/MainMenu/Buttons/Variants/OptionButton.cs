using UnityEngine;
using UnityEngine.EventSystems;
using _Project.UI.OptionMenu;

namespace _Project.UI.MainMenu
{
    public class OptionButton : MainMenuButton
    {
        [SerializeField] private OptionCanvas optionCanvasObj;

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            optionCanvasObj.OpenCanvas();
        }
    }
}
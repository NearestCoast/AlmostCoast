using UnityEngine.EventSystems;

namespace _Project.UI.MainMenu
{
    public class QuitButton : MainMenuButton
    {
        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            gameManager.Quit();
        }
    }
}
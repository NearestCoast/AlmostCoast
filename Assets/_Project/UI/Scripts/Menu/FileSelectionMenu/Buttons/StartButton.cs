using _Project.Managers.Scripts._Core.SaveManager;
using _Project.UI.MainMenu;
using UnityEngine.EventSystems;

namespace _Project.UI.FileSelectionMenu
{
    public class StartButton : MenuButton
    {
        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            // LobbyManager에서 InGame 씬으로 전환
            LobbyManager.StartGame();
        }
    }
}
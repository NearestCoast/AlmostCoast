using UnityEngine;
using UnityEngine.EventSystems;
using _Project.UI.FileSelectionMenu;

namespace _Project.UI.MainMenu
{
    public class StartButton : MainMenuButton
    {
        [SerializeField] private FileSelectionCanvas fileSelectionCanvas;

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            // LobbyManager.StartGame();
            fileSelectionCanvas.OpenCanvas();
        }
    }
}
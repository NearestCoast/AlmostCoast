using _Project.Managers.Scripts._Core.SaveManager;
using _Project.UI.MainMenu;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.UI.FileSelectionMenu
{
    public class SaveFileStartButton : MenuButton
    {
        [SerializeField] private string saveFileName;
        private LobbyManager LobbyManager { get; set; }
        protected override void Awake()
        {
            base.Awake();
            LobbyManager = FindAnyObjectByType<LobbyManager>();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            
            SaveFileData.SelectedSaveFileName = saveFileName;
            
            LobbyManager.StartGame();
        }

        private FileSelectionCanvas fileSelectionCanvas;
        public override void OnCancel(BaseEventData eventData)
        {
            base.OnCancel(eventData);
            fileSelectionCanvas.CloseCanvas();
        }
    }
}
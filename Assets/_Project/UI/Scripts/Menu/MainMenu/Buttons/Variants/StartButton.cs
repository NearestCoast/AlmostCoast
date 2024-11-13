using UnityEngine.EventSystems;

namespace _Project.UI.MainMenu
{
    public class StartButton : MainMenuButton
    {
        private LobbyManager LobbyManager { get; set; }
        protected override void Awake()
        {
            base.Awake();
            LobbyManager = FindObjectOfType<LobbyManager>();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            LobbyManager.StartGame();
        }
    }
}
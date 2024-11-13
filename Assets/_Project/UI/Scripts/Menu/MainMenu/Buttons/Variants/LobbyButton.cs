using UnityEngine.EventSystems;

namespace _Project.UI.MainMenu
{
    public class LobbyButton : MainMenuButton 
    {
        private InGameManager inGameManager;
        
        protected override void Awake()
        { 
            base.Awake(); 
            inGameManager = FindObjectOfType<InGameManager>(); 
        }
        
        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            inGameManager.LoadLobbyScene(); 
        }
    }
}
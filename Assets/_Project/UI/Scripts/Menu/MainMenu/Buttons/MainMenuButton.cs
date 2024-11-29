using _Project.InputSystem;
using UnityEngine.EventSystems;

namespace _Project.UI.MainMenu
{
    public class MainMenuButton : MenuButton
    { 
        protected GameManager gameManager;
        
        protected override void Awake()
        {
            base.Awake();
            gameManager = FindAnyObjectByType<GameManager>();
        }

        public override void OnCancel(BaseEventData eventData)
        {
            base.OnCancel(eventData);
            if (gameManager is InGameManager inGameManager) inGameManager.ResumeGame();
        }
    }
}
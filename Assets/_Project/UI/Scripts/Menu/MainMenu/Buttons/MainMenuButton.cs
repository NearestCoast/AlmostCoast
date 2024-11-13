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
            gameManager = FindObjectOfType<GameManager>();
        }

        public override void OnCancel(BaseEventData eventData)
        {
            base.OnCancel(eventData);
            if (gameManager is InGameManager inGameManager) inGameManager.ResumeGame();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            
            if (isSelected)
            {
                OnSubmit(eventData);
            }
            else
            {
                
            }
        }
    }
}
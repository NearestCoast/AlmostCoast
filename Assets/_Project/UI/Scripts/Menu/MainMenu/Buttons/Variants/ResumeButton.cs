using UnityEngine.EventSystems;

namespace _Project.UI.MainMenu
{
    public class ResumeButton : MainMenuButton
    {
        private InGameManager inGameManager;
        protected override void Awake()
        {
            base.Awake();
            inGameManager = FindAnyObjectByType<InGameManager>();
        }
        
        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            inGameManager.ResumeGame();
        }
    }
}
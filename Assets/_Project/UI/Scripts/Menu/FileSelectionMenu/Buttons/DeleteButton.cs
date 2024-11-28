using _Project.Managers.Scripts._Core.SaveManager;
using _Project.UI.MainMenu;
using UnityEngine.EventSystems;

namespace _Project.UI.FileSelectionMenu
{
    public class DeleteButton : MenuButton
    {
        private SaveFileStartButton parent;

        protected override void Awake()
        {
            base.Awake();
            parent = GetComponentInParent<SaveFileStartButton>();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            var saveLoadManager = FindAnyObjectByType<SaveLoadManager>();
            saveLoadManager.DeleteSaveFile();
            parent.Load();
        }
    }
}
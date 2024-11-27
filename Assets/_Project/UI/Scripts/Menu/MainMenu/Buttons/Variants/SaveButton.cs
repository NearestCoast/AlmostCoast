using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace _Project.UI.MainMenu
{
    public class SaveButton : MainMenuButton
    {
        [SerializeField] private UnityEvent onSubmit;
        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            onSubmit?.Invoke();
        }
    }
}
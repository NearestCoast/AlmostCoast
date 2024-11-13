using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.UI.MainMenu
{
    [DefaultExecutionOrder(11)]
    public class MainMenuCanvas : MonoBehaviour
    {
        [SerializeField] private bool initialActiveState;
        [SerializeField] private UIButton firstSelected;

        private void Start()
        {
            gameObject.SetActive(initialActiveState);
        }

        private void OnEnable()
        {
            if (!initialActiveState) EventSystem.current.SetSelectedGameObject(null);
            firstSelected.Select();
        }

        public void OpenCanvas()
        {
            gameObject.SetActive(true);
        }

        public void CloseCanvas()
        {
            gameObject.SetActive(false);
        }
    }
}
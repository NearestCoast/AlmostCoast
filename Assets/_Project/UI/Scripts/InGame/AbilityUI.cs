using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.UI.InGame
{
    public class AbilityUI : MonoBehaviour
    {
        private void Start()
        {
            ToggleUI(default);
        }

        public void ToggleUI(InputAction.CallbackContext ctx)
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            else gameObject.SetActive(true);
        }
    }
}
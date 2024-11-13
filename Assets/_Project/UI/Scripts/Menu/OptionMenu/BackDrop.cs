using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.UI.OptionMenu
{
    public class BackDrop : MonoBehaviour, IPointerClickHandler
    {
        private OptionCanvas optionCanvas;
        private void Awake()
        {
            optionCanvas = GetComponentInParent<OptionCanvas>();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log(name + " PointerClick");
            optionCanvas.CloseCanvas();
        }
    }
}
using _Project.UI.Menu._Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.UI.OptionMenu
{
    public class BackDrop : MonoBehaviour, IPointerClickHandler
    {
        private AdditionalCanvas additionalCanvas;
        private void Awake()
        {
            additionalCanvas = GetComponentInParent<AdditionalCanvas>();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log(name + " PointerClick");
            additionalCanvas.CloseCanvas();
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.UI.OptionMenu
{
    public class OptionContentItemArrow : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private MoveDirection direction;
        private Image image;
        private OptionContentItem parentItem;

        private void Awake()
        {
            image = GetComponent<Image>();
            parentItem = GetComponentInParent<OptionContentItem>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log(parentItem.name + " " + name + " PointerClick");
            parentItem.Treat(direction);
        }

        public void ToggleImage(bool value)
        {
            image.enabled = value;
        }
    }
}
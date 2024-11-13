using System;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.UI
{
    public class UIButton : MonoBehaviour, 
        ISelectHandler, IDeselectHandler, ISubmitHandler, ICancelHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [ShowInInspector] protected Button Button { get; private set; }
        [ShowInInspector] protected bool isSelected;

        protected virtual void Awake()
        {
            Button = GetComponent<Button>();
        }

        protected virtual void Start()
        {
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            Debug.Log(name + " Selected");
            isSelected = true;
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            Debug.Log(name + " Deselected");
            isSelected = false;
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            Debug.Log(name + " Summit");
            isSelected = false;
        }

        public virtual void OnCancel(BaseEventData eventData)
        {
            Debug.Log(name + " Canceled");
            isSelected = false;
        }

        public virtual void Select()
        {
            if (!EventSystem.current || !Button) return;
            if (EventSystem.current.currentSelectedGameObject == Button.gameObject) return;
            Button.Select();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log(name + " PointerClick");
        }
    }
}
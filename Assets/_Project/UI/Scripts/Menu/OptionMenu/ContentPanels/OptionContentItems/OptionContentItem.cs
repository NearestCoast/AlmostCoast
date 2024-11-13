using System;
using _Project.UI.ConfirmMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.UI.OptionMenu
{
    public class OptionContentItem : MonoBehaviour, ISelectHandler, IDeselectHandler, IMoveHandler, ICancelHandler
    {
        
        [SerializeField] private Color colorSelected = new Color(0, 0, 0, 1);
        [SerializeField] private Color colorDeselected = new Color(1, 1, 1, 1);
        [SerializeField] private ApplyButton applyButton;
        private Text title;
        private Text description;
        private OptionCanvas optionCanvas;

        private int openIndex;
        protected int currentIndex = 0;
        protected int ItemLength { get; set; }

        // protected OptionContentItemArrow arrowLeft;
        // protected OptionContentItemArrow arrowRight;
        
        protected virtual void Awake()
        {
            title = transform.Find("Panel - Title/Text (Legacy) - Title").GetComponent<Text>();
            description = transform.Find("Panel - Selection/Panel/Text (Legacy) - Description").GetComponent<Text>();
            optionCanvas = GetComponentInParent<OptionCanvas>();
            applyButton = FindObjectOfType<ApplyButton>();
            // arrowLeft = transform.Find("Panel - Selection/Panel/ArrowLeft").GetComponent<OptionContentItemArrow>();
            // arrowRight = transform.Find("Panel - Selection/Panel/ArrowRight").GetComponent<OptionContentItemArrow>();
        }

        protected virtual void Start()
        {
            title.fontSize = (int)(Screen.height * 0.05f);
            description.fontSize = (int)(Screen.height * 0.05f);
            
            title.color = colorDeselected;
            description.color = colorDeselected;
            
            Treat(MoveDirection.Left);
        }

        private void OnEnable()
        {
            SetDescription();
            applyButton.SetUpperSelectable(GetComponent<Selectable>());
        }

        private void OnDisable()
        {
            // Debug.Log($"{currentIndex} <= {openIndex}");
            currentIndex = openIndex;
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            Debug.Log(name + " Selected");
            title.color = colorSelected;
            description.color = colorSelected;
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            Debug.Log(name + " Deselected");
            title.color = colorDeselected;
            description.color = colorDeselected;
        }

        public virtual void OnCancel(BaseEventData eventData)
        {
            Debug.Log(name + " Canceled");
            optionCanvas.CloseCanvas();
        }

        public virtual void OnMove(AxisEventData eventData)
        {
            Debug.Log(name + " Move " + eventData.moveDir);
            Treat(eventData.moveDir);
        }

        public virtual void Treat(MoveDirection direction)
        {
            Debug.Log(name + " Treat " + direction);
            switch (direction)
            {
                case MoveDirection.Left:
                {
                    currentIndex -= 1;
                    if (currentIndex < 0) currentIndex = 0;
                    break;
                }
                case MoveDirection.Right:
                {
                    currentIndex += 1;
                    if (currentIndex > ItemLength - 1) currentIndex = ItemLength - 1;
                    break;
                }
            }
        }

        public virtual void Apply()
        {
            Debug.Log(name + " Apply");
            openIndex = currentIndex;
        }

        protected virtual void SetDescription(string value = "")
        {
            description.text = value;
        }
    }
}
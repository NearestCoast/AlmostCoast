using UnityEngine;

namespace _Project.UI.Menu._Core
{
    public class AdditionalCanvas : MonoBehaviour
    {
        [SerializeField] private bool initialActiveState;

        [SerializeField] private UIButton firstSelected;
        [SerializeField] private UIButton escapeSelected;
        
        protected virtual void Awake()
        {
            
        }

        protected virtual void Start()
        {
            gameObject.SetActive(initialActiveState);
        }

        protected virtual  void OnEnable()
        {
            firstSelected.Select();
        }

        protected virtual  void OnDisable()
        {
            if (escapeSelected) escapeSelected.Select();
        }

        public virtual  void OpenCanvas()
        {
            gameObject.SetActive(true);
        }

        public virtual void CloseCanvas()
        {
            gameObject.SetActive(false);
        }
    }
}
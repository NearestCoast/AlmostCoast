using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.UI
{
    public class UIElementAspectRatio : MonoBehaviour
    {
        private Canvas parentCanvas;
        private RectTransform rectTransform;
        [SerializeField] private AspectRatio aspectRatio;
        
        public AspectRatio CurrentAspectRatio => aspectRatio;

        [Serializable]
        public class AspectRatio
        {
            public Vector2 positionRatio;
            public float widthRatio;
            public float heightRatio;
            public float padding;
        }

        protected virtual void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
        }

        protected virtual void OnEnable()
        {
            SetAspect();
        }

        [Button]
        public void SetAspect()
        {
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = getSize(); 
            
            
            rectTransform.anchoredPosition = new Vector2(
                parentCanvas.pixelRect.width * aspectRatio.positionRatio.x/2 + 
                rectTransform.sizeDelta.x * -aspectRatio.positionRatio.x / 2 + 
                aspectRatio.padding * Mathf.Sign(-aspectRatio.positionRatio.x),
                
                parentCanvas.pixelRect.height * aspectRatio.positionRatio.y/2 + 
                rectTransform.sizeDelta.y * -aspectRatio.positionRatio.y / 2 + 
                aspectRatio.padding * Mathf.Sign(-aspectRatio.positionRatio.y)
            );

            Vector2 getSize()
            {
                if (aspectRatio.widthRatio == 0)
                {
                    var width = parentCanvas.pixelRect.height * aspectRatio.heightRatio;
                    return new Vector2(width, parentCanvas.pixelRect.height * aspectRatio.heightRatio);
                }
                else if (aspectRatio.heightRatio == 0)
                {
                    var height = parentCanvas.pixelRect.width * aspectRatio.widthRatio;
                    return new Vector2(parentCanvas.pixelRect.width * aspectRatio.widthRatio, height);
                }
                return new Vector2(parentCanvas.pixelRect.width * aspectRatio.widthRatio, parentCanvas.pixelRect.height * aspectRatio.heightRatio);
            }
        }
    }
}
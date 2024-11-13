
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace _Project.InputSystem
{
    public class Input : MonoBehaviour
    {
        [SerializeField] protected InputActionReference inputActionRef;
        
        [SerializeField, FoldoutGroup("Context Events")] 
        protected UnityEvent<InputAction.CallbackContext> onStarted;
        
        [SerializeField, FoldoutGroup("Context Events")]   
        protected UnityEvent<InputAction.CallbackContext> onPerformed;
        
        [SerializeField, FoldoutGroup("Context Events")] 
        protected UnityEvent<InputAction.CallbackContext> onCanceled;

        protected virtual void OnEnable()
        {
            inputActionRef.action.Enable();
            inputActionRef.action.started += OnStarted;
            inputActionRef.action.performed += OnPerformed;
            inputActionRef.action.canceled += OnCanceled;
        }
        protected virtual void OnDisable()
        {
            inputActionRef.action.Disable();
            inputActionRef.action.started -= OnStarted;
            inputActionRef.action.performed -= OnPerformed;
            inputActionRef.action.canceled -= OnCanceled;
        }

        protected virtual void OnStarted(InputAction.CallbackContext ctx)
        {
            onStarted?.Invoke(ctx);
            
            // Debug.Log("OnStarted");
        }

        protected virtual void OnPerformed(InputAction.CallbackContext ctx)
        {
            onPerformed?.Invoke(ctx);
            
            // Debug.Log($"{gameObject}OnPerformed") ;
        }

        protected virtual void OnCanceled(InputAction.CallbackContext ctx)
        {
            onCanceled?.Invoke(ctx);
            
            // Debug.Log("OnCanceled");
        }
    }
}
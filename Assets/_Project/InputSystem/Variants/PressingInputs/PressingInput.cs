using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.InputSystem
{
    public class PressingInput<TValue> : Input  where TValue : struct
    {
        public TValue Value => inputActionRef.action.ReadValue<TValue>();
        
        [SerializeField, FoldoutGroup("Context Events")] 
        protected UnityEvent<TValue> onIsPressed;

        private void Update()
        {
            OnIsPressing<TValue>();
        }

        protected virtual void OnIsPressing<T>()
        {
            onIsPressed?.Invoke(Value);
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.InputSystem
{
    public class PressingOnlyInput : PressingInput<float>
    {
        public bool IsPressing => inputActionRef.action.IsPressed(); 
        public float PressingValue { get; set; }

        protected override void OnStarted(InputAction.CallbackContext ctx)
        {
            base.OnStarted(ctx);
            PressingValue = 0;
        }

        protected override void OnIsPressing<T>()
        {
            if (IsPressing)
            {
                onIsPressed?.Invoke(Value);
                PressingValue += Time.deltaTime;
            }
        }
    }
}
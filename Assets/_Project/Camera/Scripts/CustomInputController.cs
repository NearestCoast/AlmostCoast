using UnityEngine;
using Unity.Cinemachine;
using System;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

//The component that you will add to your CinemachineCamera.
public class CustomInputController : InputAxisControllerBase<CustomInputController.Reader>
{
    private void Update()
    {
        if (Application.isPlaying)
        {
            UpdateControllers();
        }
    }

    [Serializable]
    public class Reader : IInputAxisReader
    {
        [SerializeField] private InputActionReference inputRef;
        [SerializeField] private float gain = 1;

        public float GetValue(Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            var value = 0f;
            if (hint == IInputAxisOwner.AxisDescriptor.Hints.X)
            {
                value = inputRef.action.ReadValue<Vector2>().x * gain;
            }
            
            if (hint == IInputAxisOwner.AxisDescriptor.Hints.Y)
            {
                value =  inputRef.action.ReadValue<Vector2>().y * gain;
            }
            
            return value;
        }  
    }
}
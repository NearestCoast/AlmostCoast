using System;
using UnityEngine;
using UnityEngine.UI;

using Unity.Cinemachine;
using UnityEngine.EventSystems;

namespace _Project.UI.OptionMenu
{
    public class CameraSensitivity : OptionContentItem
    {
        public enum Axis
        {
            XAxis,
            YAxis,
        }

        [SerializeField] private Axis axis;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private float minSpeed = 10;
        [SerializeField] private float maxSpeed = 150;

        [SerializeField] private CinemachineInputAxisController ceCinemachineOrbitalFollow;
        
        private float defaultHorizontalSpeed;
        private float CurrentHorizontalSpeed
        {
            get
            {
                if (sensitivitySlider.value == 0) return minSpeed;
                if (sensitivitySlider.value >= 1) return maxSpeed;
                return (sensitivitySlider.value * (maxSpeed));
            }
        }

        private string Description => ((int)CurrentHorizontalSpeed).ToString();
        
        protected override void Start()
        {
            base.Start();
            switch (axis)
            {
                case Axis.XAxis:
                {
                    // defaultHorizontalSpeed = freeLookCamera.m_XAxis.m_MaxSpeed;
                    //
                    // sensitivitySlider.value = freeLookCamera.m_XAxis.m_MaxSpeed / maxSpeed;
                    break;
                }
                
                case Axis.YAxis:
                {
                    // defaultHorizontalSpeed = freeLookCamera.m_YAxis.m_MaxSpeed;
                    //
                    // sensitivitySlider.value = freeLookCamera.m_YAxis.m_MaxSpeed / maxSpeed;
                    break;
                }
            }
            
            
            SetDescription();

            sensitivitySlider.onValueChanged.AddListener(arg0 => SetDescription());
        }

        public override void Apply()
        {
            base.Apply();
            
            switch (axis)
            {
                case Axis.XAxis:
                {
                    // freeLookCamera.m_XAxis.m_MaxSpeed = CurrentHorizontalSpeed;
                    break;
                }
                
                case Axis.YAxis:
                {
                    // freeLookCamera.m_YAxis.m_MaxSpeed = CurrentHorizontalSpeed;
                    break;
                }
            }
            
        }

        private float LastChangedTime { get; set; }
        public override void Treat(MoveDirection direction)
        {
            Debug.Log(name + " Treat " + direction);
            var amount = Time.unscaledTime - LastChangedTime <= 0.2f ? 0.1f : 0.01f;  
            switch (direction)
            {
                case MoveDirection.Left:
                {
                    sensitivitySlider.value -= amount;
                    if (sensitivitySlider.value < 0) sensitivitySlider.value = 0;
                    LastChangedTime = Time.unscaledTime;
                    break;
                }
                case MoveDirection.Right:
                {
                    sensitivitySlider.value += amount;
                    if (sensitivitySlider.value > 1) sensitivitySlider.value = 1;
                    LastChangedTime = Time.unscaledTime;
                    break;
                }
            }
            
            SetDescription();
        }

        protected override void SetDescription(string value = "")
        {
            value = Description;
            base.SetDescription(value);
        }
    }
}
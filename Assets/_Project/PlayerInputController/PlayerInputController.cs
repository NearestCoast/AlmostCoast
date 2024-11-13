using System;
using _Project.UI.MainMenu;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace _Project.InputSystem
{
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actionAsset;
        
        private PlayerInput PlayerInput { get; set; }

        private void Awake()
        {
            PlayerInput = GetComponent<PlayerInput>();
        } 
        
        public void ToggleInputActionMap(GameManager.PlayMode mode)
        {
            switch (mode)
            {
                case GameManager.PlayMode.Play:
                {
                    PlayerInput.currentActionMap = actionAsset.FindActionMap("Play");
                    break;
                }
                case GameManager.PlayMode.Pause:
                {
                    PlayerInput.currentActionMap = actionAsset.FindActionMap("UI");
                    break;
                }
            }
        }
    }
}
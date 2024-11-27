using System;
using _Project.InputSystem;
using _Project.UI;
using _Project.UI.InGame;
using _Project.UI.MainMenu;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace _Project
{
    public class InGameManager : GameManager
    {
        [SerializeField] private InputActionReference escapeActionKey;
        private PlayerInputController playerInputController;
        private MainMenuCanvas mainMenuCanvas;
        private CurtainUI curtainUI;

        private void Awake()
        {
            playerInputController = FindAnyObjectByType<PlayerInputController>();
            mainMenuCanvas = FindAnyObjectByType<MainMenuCanvas>();
            curtainUI = FindAnyObjectByType<CurtainUI>();
        }

        private void Start()
        {
            ResumeGame();
        }

        private void OnEnable()
        {
            escapeActionKey.action.Enable();
            escapeActionKey.action.performed += PauseGame;
        }

        private void OnDisable()
        { 
            escapeActionKey.action.Disable();
            escapeActionKey.action.performed -= PauseGame;
        }

        private void PauseGame(InputAction.CallbackContext ctx)
        {
            if (!IsInGameScene) return;
            if (curtainUI.IsFadingIn) return;
            Time.timeScale = 0f; // 게임 시간 정지
            playerInputController.ToggleInputActionMap(playMode = GameManager.PlayMode.Pause);
            mainMenuCanvas.OpenCanvas();
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void ResumeGame()
        {
            if (!IsInGameScene) return;
            Time.timeScale = 1f; // 게임 시간 재개
            playerInputController.ToggleInputActionMap(playMode = GameManager.PlayMode.Play);
            mainMenuCanvas.CloseCanvas();
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        public void LoadLobbyScene()
        {
            SceneManager.LoadSceneAsync("Lobby");
        }
    }
}
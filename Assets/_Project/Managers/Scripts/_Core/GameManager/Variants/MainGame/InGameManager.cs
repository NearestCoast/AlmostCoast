using System;
using _Project.InputSystem;
using _Project.Managers.Scripts._Core.SaveManager;
using _Project.UI;
using _Project.UI.InGame;
using _Project.UI.MainMenu;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace _Project
{
    public class InGameManager : GameManager, ISavable
    {
        [SerializeField] private InputActionReference escapeActionKey;
        private PlayerInputController playerInputController;
        private MainMenuCanvas mainMenuCanvas;
        private CurtainUI curtainUI;

        private float playTime; // 누적 플레이 시간
        private float sessionStartTime; // 현재 세션 시작 시간

        private GUIStyle guiStyle = new GUIStyle(); // GUI 스타일 설정

        private void Awake()
        {
            playerInputController = FindAnyObjectByType<PlayerInputController>();
            mainMenuCanvas = FindAnyObjectByType<MainMenuCanvas>();
            curtainUI = FindAnyObjectByType<CurtainUI>();

            sessionStartTime = Time.realtimeSinceStartup; // 세션 시작 시간 기록
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

        
        // ISavable 인터페이스 구현
        public bool EnrollToSaveManager => true;

        public bool Save(string saveFileName)
        {
            try
            {
                // 현재 세션의 플레이 시간을 누적하고 저장
                playTime += Time.realtimeSinceStartup - sessionStartTime;
                sessionStartTime = Time.realtimeSinceStartup; // 세션 시간 초기화
                ES3.Save("PlayTime", playTime, saveFileName);

                Debug.Log($"PlayTime saved: {playTime} seconds");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save PlayTime: {e.Message}");
                return false;
            }
        }

        public bool Load(string saveFileName)
        {
            try
            {
                // 저장된 플레이타임 불러오기
                if (ES3.KeyExists("PlayTime", saveFileName))
                {
                    playTime = ES3.Load<float>("PlayTime", saveFileName);
                    sessionStartTime = Time.realtimeSinceStartup; // 세션 시작 시간 재설정
                    Debug.Log($"PlayTime loaded: {playTime} seconds");
                    return true;
                }

                Debug.LogWarning("No PlayTime found in save file.");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load PlayTime: {e.Message}");
                return false;
            }
        }

        // OnGUI에서 PlayTime 표시
        private void OnGUI()
        {
            // 스타일 설정
            guiStyle.fontSize = 20; // 글자 크기
            guiStyle.normal.textColor = Color.white; // 글자 색상

            // 총 플레이 시간 가져오기
            float totalPlayTime = GetTotalPlayTime();

            // 시간을 시:분:초 형식으로 변환
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalPlayTime);
            string formattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

            // 화면에 표시
            GUI.Label(new Rect(10, 10, 200, 40), $"PlayTime: {formattedTime}", guiStyle);
            
            return;
            
            // 누적 플레이 시간 반환
            float GetTotalPlayTime()
            {
                return playTime + (Time.realtimeSinceStartup - sessionStartTime);
            }
        }
    }
}

using _Project.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace _Project
{
    public class LobbyManager : GameManager
    {
        private PlayerInputController playerInputController;
        private PlayerInput PlayerInput => playerInputController.PlayerInput;

        private void Awake()
        {
            playerInputController = FindAnyObjectByType<PlayerInputController>();

            if (PlayerInput != null)
            {
                // 입력 장치 변경 감지 이벤트 연결
                PlayerInput.onControlsChanged += OnInputDeviceChange;
            }
        }

        private void Start()
        {
            Time.timeScale = 0f; // 게임 시간 정지
            playerInputController.ToggleInputActionMap(playMode = GameManager.PlayMode.Pause);

            // 입력 장치에 따른 초기 커서 상태 설정
            UpdateCursorVisibilityBasedOnInputDevice();
        }

        private void OnDestroy()
        {
            if (PlayerInput != null)
            {
                // 이벤트 연결 해제
                PlayerInput.onControlsChanged -= OnInputDeviceChange;
            }
        }

        private void OnInputDeviceChange(PlayerInput input)
        {
            // 입력 장치 변경 시 커서 상태 업데이트
            UpdateCursorVisibilityBasedOnInputDevice();
        }

        private void UpdateCursorVisibilityBasedOnInputDevice()
        {
            if (PlayerInput == null)
                return;

            // 현재 입력 장치 확인
            var lastDevice = PlayerInput.currentControlScheme;

            if (lastDevice == "Keyboard&Mouse")
            {
                // 키보드와 마우스 입력일 경우 커서를 보이게 설정
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                // 게임패드나 기타 입력 장치일 경우 커서를 숨김
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public static void StartGame()
        {
            SceneManager.LoadSceneAsync("InGame");
        }
    }
}

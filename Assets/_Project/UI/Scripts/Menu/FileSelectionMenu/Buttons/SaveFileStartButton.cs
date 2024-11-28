using _Project.Managers.Scripts._Core.SaveManager;
using _Project.UI.MainMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.UI.FileSelectionMenu
{
    public class SaveFileStartButton : MenuButton, ISavable
    {
        [SerializeField] private string saveFileName; // 저장 파일 이름
        
        private FileSelectionCanvas fileSelectionCanvas;

        [SerializeField] private Text textCompo_playTime;

        protected override void Awake()
        {
            base.Awake();
            fileSelectionCanvas = FindAnyObjectByType<FileSelectionCanvas>();

            // PlayTime 로드 시도
            if (!string.IsNullOrEmpty(saveFileName))
            {
                Load(saveFileName);
            }
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);

            // 선택된 저장 파일 설정
            SaveFileData.SelectedSaveFileName = saveFileName;

            // LobbyManager에서 InGame 씬으로 전환
            LobbyManager.StartGame();
        }

        public override void OnCancel(BaseEventData eventData)
        {
            base.OnCancel(eventData);
            fileSelectionCanvas.CloseCanvas();
        }

        // ISavable 인터페이스 구현
        public bool EnrollToSaveManager => true;

        public bool Save(string saveFileName)
        {
            return false; // SaveFileStartButton은 저장 로직이 필요하지 않음
        }
        
        private float loadedPlayTime; // 로드된 플레이 타임
        public bool Load(string saveFileName)
        {
            saveFileName = this.saveFileName;
            try
            {
                // EasySave에서 PlayTime 데이터를 로드
                if (ES3.KeyExists("PlayTime", saveFileName))
                {
                    loadedPlayTime = ES3.Load<float>("PlayTime", saveFileName);
                    Debug.Log($"PlayTime loaded for {saveFileName}: {loadedPlayTime} seconds");
                    textCompo_playTime.text = $"{GetFormattedPlayTime(loadedPlayTime)}";
                    
                    return true;
                }

                Debug.LogWarning($"No PlayTime found in save file: {saveFileName}");
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load PlayTime for {saveFileName}: {e.Message}");
                return false;
            }
        }

        // PlayTime 포맷팅 함수
        private string GetFormattedPlayTime(float playTime)
        {
            var timeSpan = System.TimeSpan.FromSeconds(playTime);
            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }
}

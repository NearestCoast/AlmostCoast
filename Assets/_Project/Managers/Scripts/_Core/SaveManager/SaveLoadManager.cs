using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Managers.Scripts._Core.SaveManager
{
    public interface ISavable
    {
        public bool EnrollToSaveManager { get; } // SaveManager에 등록 여부
        bool Save(string saveFileName);
        bool Load(string saveFileName);
    }
    
    public class SaveLoadManager : MonoBehaviour
    {
        [ShowInInspector] private static string SaveFileName => SaveFileData.SelectedSaveFileName;
        [ShowInInspector] private List<ISavable> savableObjects;

        private void Awake()
        {
            // Scene의 모든 오브젝트에서 ISavable 인터페이스를 구현한 오브젝트를 수집
            savableObjects = findAllSavableObjects();

            Debug.Log($"Found {savableObjects.Count} savable objects in the scene.");

            
            return;
            
            // 로컬 함수로 구현된 ISavable 오브젝트 검색
            List<ISavable> findAllSavableObjects()
            {
                var savableList = new List<ISavable>();

                // 현재 활성화된 씬 가져오기
                var activeScene = SceneManager.GetActiveScene();

                // 루트 오브젝트 가져오기
                var rootObjects = activeScene.GetRootGameObjects();

                foreach (var rootObject in rootObjects)
                {
                    // 루트와 자식 오브젝트에서 ISavable 컴포넌트 검색
                    var savablesInChildren = rootObject.GetComponentsInChildren<ISavable>(true);
                
                    // EnrollToSaveManager가 true인 오브젝트만 추가
                    foreach (var savable in savablesInChildren)
                    {
                        if (savable.EnrollToSaveManager)
                        {
                            savableList.Add(savable);
                        }
                    }
                }

                return savableList;
            }
        }

        private void Start()
        {
            var fileExists = ES3.FileExists(SaveFileName);
            if (fileExists)
            {
                LoadGame();
            }
        }

        private void OnApplicationQuit()
        {
            SaveFileData.SelectedSaveFileName = null;
        }

        // InGameUI - MainMenu - SaveButton
        public void SaveGame()
        {
            foreach (var savable in savableObjects)
            {
                savable.Save(SaveFileName);
            }
            Debug.Log($"All savable objects have been saved. File name: {SaveFileName}");
        }

        private void LoadGame()
        {
            foreach (var savable in savableObjects)
            {
                savable.Load(SaveFileName);
            }
            Debug.Log($"All savable objects have been loaded. File name: {SaveFileName}");
        }

        [Button("Delete Save File")]
        public void DeleteSaveFile()
        {
            if (ES3.FileExists(SaveFileName))
            {
                ES3.DeleteFile(SaveFileName);
                Debug.Log($"Save file deleted: {SaveFileName}");
            }
            else
            {
                Debug.LogWarning($"No save file found to delete: {SaveFileName}");
            }
        }
    }
}
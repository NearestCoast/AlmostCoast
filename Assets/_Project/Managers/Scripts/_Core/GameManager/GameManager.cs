using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Data;
using _Project.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace _Project
{
    [DefaultExecutionOrder(13)]
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private SO_GameSetting soGameSetting;
        public enum PlayMode
        {
            Play,
            Pause,
        }
        
        public PlayMode playMode = PlayMode.Play;
        
        private static string CurrentSceneName => SceneManager.GetActiveScene().name;
        public bool IsInGameScene => CurrentSceneName.Contains("InGame");
        
        public void Quit()
        {
            // 에디터에서 실행할 경우
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(); // 실제 애플리케이션에서 게임을 종료합니다.
#endif
        }

        public async UniTaskVoid DelayedOnApply()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            var aspectRatios = FindObjectsOfType<UIElementAspectRatio>();
            foreach (var uiElementAspectRatio in aspectRatios)
            {
                uiElementAspectRatio.SetAspect();
            }
        }
        
        private void OnGUI()
        {
            var fontsize = (int)(Screen.height * 0.1f);
            soGameSetting.GameStatusStyle.fontSize = fontsize;
            // GUI.Label(new Rect(0, fontsize * 0, 1000, fontsize), $"{name}:{Screen.currentResolution}", soGameSetting.GameStatusStyle);
            // GUI.Label(new Rect(0, fontsize * 1, 1000, fontsize), $"{name}:{Screen.fullScreen}", soGameSetting.GameStatusStyle);
        }
    }
}
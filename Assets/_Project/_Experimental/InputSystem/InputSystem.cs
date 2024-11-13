using System;
using System.IO;
using Sirenix.OdinInspector;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json.Linq;

namespace _Project.InputSystem
{
    public class InputSystem : MonoBehaviour
    {
        public InputActionAsset inputActions; // 인스펙터에서 할당
        private InputActionMap actionMap;
        public string actionMapName = "Play"; // 액션 맵 이름
        public string actionName = "Jump"; // 재바인딩할 액션 이름
        // [SerializeField, FolderPath] private string filePath;
        private InputAction inputAction;
        // public InputBinding binding;

        private void OnEnable()
        {
            LoadBindings();
            inputAction.Enable();
            inputAction.performed += Test;
        }

        private void OnDisable()
        {  
            inputAction.Disable();   
            inputAction.performed -= Test;
            // SaveBindings();
        }

        [SerializeField] private TMP_Text text;
        public void Test(InputAction.CallbackContext ctx)
        {
            text.text = text.text == "Jump-" ? "Jump+" : "Jump-";
        }

        private InputActionRebindingExtensions.RebindingOperation operation;
        [Button]
        public void StartRebinding() {
            inputAction.Disable();

            operation = inputAction.PerformInteractiveRebinding()
                    .WithControlsExcluding("<Mouse>/position")
                    .WithControlsExcluding("<Mouse>/delta") // 마우스 제외 예시
                    .WithControlsExcluding("<Gamepad>/Start") // 마우스 제외 예시
                    .WithControlsExcluding("<Keyboard>/p") // 마우스 제외 예시
                    .WithControlsExcluding("<keyboard>/escape") // 마우스 제외 예시
                    .OnMatchWaitForAnother(0.1f) // 바인딩을 확정하기 위한 대기 시간
                    .OnComplete(op => RebindComplete())
                ;
            
            operation.Start(); // 재바인딩 프로세스 시작
            // action.Enable();
        }

        private void RebindComplete()
        {
            Debug.Log("Complete");
            operation.action.Enable(); 
            

            operation.Dispose();
            

            // 바인딩을 JSON 형식으로 저장 
            // string rebinds = map.ToJson();
            // PlayerPrefs.SetString(actionName + "Rebinds", rebinds);
            // PlayerPrefs.Save();
            //
            // SaveBindings(map);
        }

        void OnApplicationQuit()
        {
            SaveBindings();
        }

        public void SaveBindings()
        {
            Debug.Log("SaveBindings");
            foreach (var map in inputActions.actionMaps)
            {
                string rebindings = map.SaveBindingOverridesAsJson();
                PlayerPrefs.SetString(map.name + "_bindings", rebindings);
            }
            PlayerPrefs.Save();
        }

        public void LoadBindings()
        {
            
            foreach (var map in inputActions.actionMaps)
            {
                string rebindings = PlayerPrefs.GetString(map.name + "_bindings", string.Empty);
                if (!string.IsNullOrEmpty(rebindings))
                {
                    map.LoadBindingOverridesFromJson(rebindings);
                }
            }

            inputAction = inputActions.FindActionMap(actionMapName).FindAction(actionName);
        }
    }
}
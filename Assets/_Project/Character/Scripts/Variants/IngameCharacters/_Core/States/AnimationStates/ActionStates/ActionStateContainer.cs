using System.Collections.Generic;
using _Project.Managers.Scripts._Core.SaveManager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.ActionStates
{
    public class ActionStateContainer : MonoBehaviour, ISavable
    {
        [SerializeField] private bool isPlayer = false;
        [ShowInInspector] public Dictionary<ActionState.StateType, ActionState> StateObjDict { get; set; }
        
        // 인덱서 추가
        public ActionState this[ActionState.StateType stateType] => StateObjDict[stateType]; 

        private void Awake()
        {
            UpdateDictionary();
        }

        // ContainsKey 메서드 추가
        public bool ContainsKey(ActionState.StateType stateType)
        {
            return StateObjDict.ContainsKey(stateType);
        }   

        public void UpdateDictionary()
        {
            StateObjDict = new Dictionary<ActionState.StateType, ActionState>();
            var children = GetComponentsInChildren<ActionState>(true);
            foreach (var actionState in children)
            {
                if (!actionState) continue;
                var type = actionState.Type;
                StateObjDict.Add(type, actionState);
            }
        }

        [SerializeField] private bool enrollToSaveManager;

        // ISave 구현
        public bool EnrollToSaveManager => enrollToSaveManager;

        public bool Save(string saveFileName)
        {
            if (saveFileName == string.Empty) return false;
            // 활성화된 상태를 StateObjDict에서 동적으로 검색
            var activeStateTypes = new List<ActionState.StateType>();
            foreach (var kvp in StateObjDict)
            {
                if (kvp.Value.gameObject.activeInHierarchy) // 활성화된 오브젝트만 저장
                {
                    activeStateTypes.Add(kvp.Key);
                }
            }

            ES3.Save("ActiveActionStates", activeStateTypes, saveFileName);
            Debug.Log("ActionStateContainer saved.");

            return true;
        }

        public bool Load(string saveFileName)
        {
            if (!ES3.KeyExists("ActiveActionStates", saveFileName)) return false;

            var loadedStateTypes = ES3.Load<List<ActionState.StateType>>("ActiveActionStates", saveFileName);

            foreach (var stateType in loadedStateTypes)
            {
                // Debug.Log(stateType);
                if (StateObjDict.ContainsKey(stateType))
                {
                    var state = StateObjDict[stateType];
                    // ActionState가 비활성화되어 있다면 활성화
                    if (!state.gameObject.activeInHierarchy)
                    {
                        state.gameObject.SetActive(true);
                    }
                }
            }

            Debug.Log("ActionStateContainer loaded.");

            // 사전 업데이트 (필요 시)
            UpdateDictionary();

            return true;
        }
    }
}
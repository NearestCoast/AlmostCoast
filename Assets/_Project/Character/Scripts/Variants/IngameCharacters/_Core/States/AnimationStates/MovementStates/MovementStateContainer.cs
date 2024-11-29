using System.Collections.Generic;
using _Project.Managers.Scripts._Core.SaveManager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public class MovementStateContainer : MonoBehaviour, ISavable
    {
        [SerializeField] private bool isPlayer = false;
        [ShowInInspector] private Dictionary<MovementState.StateType, MovementState> StateObjDict { get; set; }
        
        public MovementState this[MovementState.StateType stateType] => StateObjDict[stateType];
        
        private void Awake()
        {
            UpdateDictionary();
        }
        
        public bool ContainsKey(MovementState.StateType stateType)
        {
            return StateObjDict.ContainsKey(stateType);
        }

        public void UpdateDictionary()
        {
            StateObjDict = new Dictionary<MovementState.StateType, MovementState>();
            var children = GetComponentsInChildren<MovementState>(true);
            foreach (var movementState in children)
            {
                if (movementState != null)
                {
                    StateObjDict[movementState.Type] = movementState;
                    movementState.isPlayer = isPlayer;
                }
            }
        }

        [SerializeField] private bool enrollToSaveManager;

        // ISave 구현
        public bool EnrollToSaveManager => enrollToSaveManager;

        public bool Save(string saveFileName)
        {
            UpdateDictionary();
            if (saveFileName == string.Empty) return false;
            // 활성화된 상태를 StateObjDict에서 동적으로 검색
            var activeStateTypes = new List<MovementState.StateType>();
            foreach (var kvp in StateObjDict)
            {
                if (kvp.Value.gameObject.activeInHierarchy) // 활성화된 오브젝트만 저장
                {
                    activeStateTypes.Add(kvp.Key);
                }
            }

            ES3.Save("ActiveMovementStates", activeStateTypes, saveFileName);
            Debug.Log("MovementStateContainer saved.");

            return true;
        }

        public bool Load(string saveFileName)
        {
            if (!ES3.KeyExists("ActiveMovementStates", saveFileName)) return false;

            var loadedStateTypes = ES3.Load<List<MovementState.StateType>>("ActiveMovementStates", saveFileName);
            
            foreach (var stateType in loadedStateTypes)
            {
                if (StateObjDict.ContainsKey(stateType))
                {
                    var state = StateObjDict[stateType];
                    // MovementState가 비활성화되어 있다면 활성화
                    if (!state.gameObject.activeInHierarchy)
                    {
                        state.gameObject.SetActive(true);
                    }
                }
            }

            Debug.Log("MovementStateContainer loaded.");

            // 사전 업데이트 (필요 시)
            UpdateDictionary();

            return true;
        }
    }
}

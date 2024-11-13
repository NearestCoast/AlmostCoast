using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates
{
    public class LockStateContainer : MonoBehaviour
    {
        [SerializeField] private bool isPlayer;
        [ShowInInspector] public Dictionary<LockState.StateType, LockState> ActionStateObjDict { get; set; }
        
        // 인덱서 추가
        public LockState this[LockState.StateType stateType]
        {
            get => ActionStateObjDict[stateType];
            set => ActionStateObjDict[stateType] = value;
        }

        private void Awake()
        {
            ActionStateObjDict = new Dictionary<LockState.StateType, LockState>();
            for (var i = 0; i < transform.childCount; i++)
            {
                var lockState = transform.GetChild(i).GetComponent<LockState>();
                if (!lockState) continue;
                var type = lockState.Type;
                ActionStateObjDict.Add(type, lockState);
                lockState.IsPlayer = isPlayer;
            }
        }
    }
}
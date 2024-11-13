using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.ActionStates
{
    public class ActionStateContainer : MonoBehaviour
    {
        [ShowInInspector] public Dictionary<ActionState.StateType, ActionState> StateObjDict { get; set; }
        
        // 인덱서 추가
        public ActionState this[ActionState.StateType stateType]
        {
            get => StateObjDict[stateType]; 
            set => StateObjDict[stateType] = value;
        }

        private void Awake()
        {
            StateObjDict = new Dictionary<ActionState.StateType, ActionState>();
            var children = GetComponentsInChildren<ActionState>();
            foreach (var actionState in children)
            {
                if (!actionState) continue;
                var type = actionState.Type;
                StateObjDict.Add(type, actionState);
            }
        }

        // ContainsKey 메서드 추가
        public bool ContainsKey(ActionState.StateType stateType)
        {
            return StateObjDict.ContainsKey(stateType);
        }
    }
}
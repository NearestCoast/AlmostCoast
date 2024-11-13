using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.States.CommonStates.BrightnessState
{
    public class BrightnessStateContainer : MonoBehaviour
    {
        [ShowInInspector] public Dictionary<BrightnessState.StateType, BrightnessState> ActionStateObjDict { get; set; }
        
        // 인덱서 추가
        public BrightnessState this[BrightnessState.StateType stateType]
        {
            get => ActionStateObjDict[stateType];
            set => ActionStateObjDict[stateType] = value;
        }

        private void Awake()
        {
            ActionStateObjDict = new Dictionary<BrightnessState.StateType, BrightnessState>();
            for (var i = 0; i < transform.childCount; i++)
            {
                var actionState = transform.GetChild(i).GetComponent<BrightnessState>();
                if (!actionState) continue;
                var type = actionState.Type;
                ActionStateObjDict.Add(type, actionState);
            }
        }
    }
}
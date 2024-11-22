using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public class MovementStateContainer : MonoBehaviour
    {
        [SerializeField] private bool isPlayer = false;
        [ShowInInspector] public Dictionary<MovementState.StateType, MovementState> ActionStateObjDict { get; set; }
        
        // 인덱서 추가
        public MovementState this[MovementState.StateType stateType]
        {
            get => ActionStateObjDict[stateType];
            set => ActionStateObjDict[stateType] = value;
        }

        private void Awake()
        {
            UpdateDictionary();
        }

        public void UpdateDictionary()
        {
            ActionStateObjDict = new Dictionary<MovementState.StateType, MovementState>();
            var children = GetComponentsInChildren<MovementState>();
            foreach (var movementState in children)
            {
                if (!movementState) continue;
                var type = movementState.Type;
                ActionStateObjDict.Add(type, movementState);

                movementState.isPlayer = isPlayer;
            }
        }
    }
}
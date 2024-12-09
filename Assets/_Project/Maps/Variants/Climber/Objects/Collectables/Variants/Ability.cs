using System.Collections.Generic;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.InputSystem;
using UnityEngine;
using AnimationState = _Project.Characters._Core.States.AnimationStates.AnimationState;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class Ability : Collectable
    {
        [SerializeField] private List<AnimationState> targetStates;

        public List<AnimationState> TargetStates
        {
            get => targetStates;
            set => targetStates = value;
        }

        protected override void Work()
        {
            base.Work();
            
            foreach (var targetState in TargetStates)
            {
                Debug.Log("Player Collected " + targetState.name);
                targetState.gameObject.SetActive(true);

                if (targetState is MovementState)
                {
                    playerCharacter.GetComponentInChildren<MovementStateContainer>().UpdateDictionary();
                }
                else if (targetState is ActionState)
                {
                    playerCharacter.GetComponentInChildren<ActionStateContainer>().UpdateDictionary();
                }
            }

            var attackInputBuffers = FindObjectsByType<AttackInputBuffer>(FindObjectsSortMode.None);
            foreach (var attackInputBuffer in attackInputBuffers)
            {
                attackInputBuffer.CheckStateEnabled();
            }
        }
    }
}
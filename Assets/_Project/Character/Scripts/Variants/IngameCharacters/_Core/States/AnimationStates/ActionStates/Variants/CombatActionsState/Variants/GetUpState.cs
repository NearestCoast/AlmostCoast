using UnityEngine;
using UnityEngine.Events;

namespace _Project.Characters.IngameCharacters.Core.ActionStates.CombatActionsState
{
    public class GetUpState : ActionLayerClipActionState
    {
        public override StateType Type => StateType.GetUp;

        public override bool CanExitState => IsAnimEnded;
        public override void OnEnterState()
        {
            base.OnEnterState();    
            MoveParams.SetCrowdControlled();
            characterControllerEnveloper.OnCrouchStart();
        }

        [SerializeField] private UnityEvent onExitState;
        public override void OnExitState()
        {
            base.OnExitState();
            MoveParams.ResetCrowdControlled();
            characterControllerEnveloper.ResetCharacterController();
            onExitState?.Invoke();
        }
    }
}
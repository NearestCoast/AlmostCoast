using UnityEngine;
using UnityEngine.Events;

namespace _Project.Characters.IngameCharacters.Core.ActionStates.CombatActionsState
{
    public class KnockBackedState : ActionLayerClipActionState
    {
        public override StateType Type => StateType.KnockBacked;
        
        public override bool CanEnterState => true;

        public override bool CanExitState => NextState.Type == StateType.GetUp && IsAnimEnded;

        [SerializeField] private UnityEvent onExitState;
        [SerializeField] private UnityEvent onAnimEnd;
        public override void OnEnterState()
        { 
            base.OnEnterState();    
            MoveParams.SetCrowdControlled();
            // characterControllerEnveloper.OnCrouchStart();
            
            var dir = masterCharacter.HittingInfo.GetHitDirectionFromCenter();
            Acc = dir * 10;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            MoveParams.ResetCrowdControlled();
            // characterControllerEnveloper.OnCrouchEnd();
            onExitState?.Invoke();
            Acc = Vector3.zero;
        }

        public override Vector3 GetVelocity()
        {
            if (AnimNormalizedTime >= 1) onAnimEnd?.Invoke();
            return base.GetVelocity();
        }
    }
}
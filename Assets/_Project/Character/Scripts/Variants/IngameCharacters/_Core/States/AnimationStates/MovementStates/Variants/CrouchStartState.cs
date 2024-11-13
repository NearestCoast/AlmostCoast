using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public class CrouchStartState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.CrouchStart;

        public override bool CanEnterState => (GroundParams.IsGrounded && !MoveParams.IsUnderCrowdControl && !MoveParams.IsGroundPoundingEnded);

        public override bool CanExitState
        {
            get 
            { 
                if (MoveParams.IsUnderCrowdControl) return true;
                var nextStateType = NextState.Type;
                
                var value = nextStateType switch
                {
                    StateType.Landing => true,
                    
                    StateType.Jump => true,
                    StateType.CrouchEnd => true,
                    StateType.Roll => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                
                return value; 
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            characterControllerEnveloper.OnCrouchStart();
        }

        public override void OnExitState()
        {
            base.OnExitState();
            characterControllerEnveloper.ResetCharacterController();
        }
    }
}
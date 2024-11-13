using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public class CrouchEndState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.CrouchEnd;

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                return true;
                // return inputChecker.Direction2 == Vector2.zero;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                var value = NextState.Type switch 
                {
                    StateType.Landing => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                
                return value || IsAnimEnded; 
            }
        }
    }
}
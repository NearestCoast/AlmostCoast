using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public class SurprisedState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.Surprised;

        public override bool CanEnterState => !MoveParams.IsUnderCrowdControl;
        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                var value = NextState.Type switch
                {
                    StateType.Landing => true,
                    
                    StateType.Jump => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                
                return IsAnimEnded || value;
            }
        }
    }
}
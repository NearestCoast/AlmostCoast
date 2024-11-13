using _Project.Maps.Climber.Objects;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class BubbleReady : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.BubbleReady;
        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                return ActionsStateParams.CurrentBubble;
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
                    
                    StateType.BubbleBlueExit => true,
                    
                    StateType.Die => true,
                    _ => false,
                };

                return value;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            
            MoveParams.ResetJumpCount();
            MoveParams.ResetKickCount();
            MoveParams.ResetClimbStamina();
            MoveParams.ResetAcceleration();
            MoveParams.ResetWallJumpCount();
            MoveParams.GravityTime = 0;
        }
    }
    
    public partial class BubbleReady : BaseLayerClipMovementState
    {
        protected override Vector3 GetVelocity()
        {
            var dir = ActionsStateParams.CurrentBubble.transform.position - transform.position;
            return dir;
        }
    }
}
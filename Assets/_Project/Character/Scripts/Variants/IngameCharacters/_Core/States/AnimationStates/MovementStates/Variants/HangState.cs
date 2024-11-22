using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public class HangState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.Hang;
        [SerializeField] private VerticalSurfaceChecker checker;

        public override bool CanEnterState
        {
            get
            {
                // VerticalParams.IsEdgeOfPlatform = checker.GetIsEdgeOfPlatform();
                if (masterCharacter.CurrentMovingPlatform) return false;
                return MoveParams.IsClimbable && !MoveParams.IsClimbButtonPressed && !GroundParams.IsGrounded && VerticalParams.IsEdgeOfPlatformFromTop;
                // return !GroundParams.IsGrounded && VerticalParams.IsEdgeOfPlatform;
            }
        }

        public override bool CanExitState
        {
            get
            {
                // if (!MoveParams.IsClimbable) return true;

                switch (NextState.Type)
                {
                    case StateType.HangJump : return StateTime > 0.2f;
                    case StateType.ClimbOverLedge : return StateTime > 0.2f;
                }
                
                var value = NextState.Type switch
                {
                    StateType.Climb => true,
                    StateType.Die => true,
                    _ => false,
                };

                return value;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            MoveParams.StartClimbing();
            
            MoveParams.ResetWallJumping();
            MoveParams.ResetHeadJumping();
            MoveParams.ResetAcceleration();
            
            IsJustEntered = true;
            
            WallNormalSnap = VerticalParams.WallNormal is null ? -transform.forward : VerticalParams.WallNormal.Value;
            EdgeHeightSnap = VerticalParams.EdgeHeight;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            MoveParams.EndClimbing();
        }

        // [SerializeField] private float maxTime = 0.1f;
        [SerializeField] private float distanceOffset = 0.2f;
        private bool IsJustEntered { get; set; }
        private Vector3 WallNormalSnap { get; set; }
        private float EdgeHeightSnap { get; set; }
        protected override Vector3 GetVelocity()
        {
            MoveParams.DecreaseClimbStaminaPerFrame();
            
            // if (IsJustEntered)
            // {
            //     IsJustEntered = false;
            //     
            //     // return Vector3.up * (VerticalParams.DistanceToTopEdge - characterControllerEnveloper.Height + distanceOffset);
                var value = (EdgeHeightSnap - (transform.position.y + characterControllerEnveloper.Height) + distanceOffset);
                
                if (float.IsNaN(value))
                {
                    Debug.Log(EdgeHeightSnap);
                    Debug.Log(transform.position.y);
                    Debug.Log(characterControllerEnveloper.Height);
                    Debug.Log(distanceOffset);
                    Debug.Log(value);
                    return Vector3.zero;
                }
                return Vector3.up * value;
            // }
            
            return Vector3.zero;
        }

        protected override Quaternion GetRotation()
        {
            Quaternion targetRotation = Quaternion.LookRotation(-WallNormalSnap, Vector3.up);
            return targetRotation;
        }

    }
}
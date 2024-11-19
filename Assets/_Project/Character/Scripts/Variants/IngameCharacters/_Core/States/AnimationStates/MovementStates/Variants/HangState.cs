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
                VerticalParams.IsEdgeOfPlatform = checker.GetIsEdgeOfPlatform();
                return MoveParams.IsClimbable && !MoveParams.IsClimbButtonPressed && !GroundParams.IsGrounded && VerticalParams.IsEdgeOfPlatform;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (!MoveParams.IsClimbable) return true;

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
        }

        public override void OnExitState()
        {
            base.OnExitState();
            MoveParams.EndClimbing();
        }

        // [SerializeField] private float maxTime = 0.1f;
        [SerializeField] private float distanceOffset = 0.2f;
        private bool IsJustEntered { get; set; }
        protected override Vector3 GetVelocity()
        {
            MoveParams.DecreaseClimbStaminaPerFrame();
            
            if (IsJustEntered)
            {
                IsJustEntered = false;
                return Vector3.up * (VerticalParams.DistanceToTopEdge - characterControllerEnveloper.Height + distanceOffset);
            }
            // if (StateTime < maxTime)
            // {
            //     var value = Vector3.up * (VerticalParams.DistanceToTopEdge + distanceOffset) / maxTime;
            //     return value * Time.deltaTime;
            // }
            // else
            // {
            //     
            // }
            
            return Vector3.zero;
        }

        protected override Quaternion GetRotation()
        {
            // WallNormal이 유효하지 않을 경우 현재 회전 유지
            if (VerticalParams.WallNormal == null)
            {
                return transform.rotation;
            }

            // WallNormal을 기반으로 회전 생성
            Vector3 wallNormal = VerticalParams.WallNormal.Value; // nullable 처리
            Quaternion targetRotation = Quaternion.LookRotation(-wallNormal, Vector3.up);

            return targetRotation;
        }

    }
}
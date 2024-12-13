using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public class LandingState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.Landing;

        public override bool CanExitState
        {
            get
            {
                var nextStateType = NextState.Type;
                var value = nextStateType switch
                {
                    StateType.Idle => IsLandingEnded,
                    StateType.Move => IsLandingEnded,
                    StateType.CautiousMove => IsLandingEnded,
                    StateType.WanderingMove => IsLandingEnded,
                    
                    // StateType.Flinched => true,
                    StateType.Die => true,
                    _ => false,
                };

                return value;
            }
        }

        private MovementState prevState { get; set; }

        [SerializeField] private UnityEvent onEnter;
        [SerializeField] private UnityEvent onEnd;

        public override void OnEnterState()
        {
            base.OnEnterState();
            onEnter?.Invoke();
            
            prevState = PrevState;
            MoveParams.ResetClimbStamina();
            MoveParams.EndClimbing();
            MoveParams.ResetWallJumping();
            MoveParams.ResetHeadJumping();
            
            MoveParams.ResetAcceleration();

            MoveParams.ResetJumpCount();
            MoveParams.ResetWallJumpCount();
            MoveParams.ResetClimbJumpCount();
            MoveParams.ResetKickCount();
            MoveParams.ResetSkillJumpCount();
        }

        private bool IsLandingEnded => true;
        // private bool IsLandingEnded => transform.position.y - GroundParams.GroundPoint.y <= characterControllerEnveloper.SkinWidth + 0.0001f;

        protected override Vector3 GetVelocity()
        {
            if (IsLandingEnded)
            {
                if (!masterCharacter.IsMovingToSavePoint) onEnd?.Invoke();
            }
            
            if (transform.position.y - GroundParams.GroundPoint.y - characterControllerEnveloper.SkinWidth > 0)
            {
                MoveParams.Gravity = Vector3.down * (transform.position.y - GroundParams.GroundPoint.y - characterControllerEnveloper.SkinWidth);
                // if (MoveParams.HasMovingPlatform) MoveParams.Gravity = Vector3.down * (transform.position.y - GroundParams.GroundPoint.y);
            }
            else
            {
                MoveParams.Gravity = Vector3.zero;
            }

            return MoveParams.Gravity;
        }

        public override Vector3 CameraTargetUpdate()
        {
           return prevState.CameraTargetUpdate();
        //      
        //     var moveDirection = MoveParams.GetGroundProjectedDirection(HorizontalDirection3);
        //     if (moveDirection.y > 0) moveDirection.y = 0;
        //
        //     var inputDirSpeed = InputDirection.magnitude > 0.75f ? 1 : 0;
        //     var inputSpeed = InputDirection == Vector2.zero ? camTargetGoBackRate : InputDirection.magnitude;
        //     var dir = (transform.position + moveDirection * (camTargetMoveAmount * inputDirSpeed) - moveCameraTarget.transform.position).XYZ3toX0Z3();
        //     moveCameraTarget.Move(dir * (camTargetMoveSpeed * inputSpeed * Time.deltaTime));
        //     // if (!IsBlocked)
        //     // {
        //     //     var dir = (transform.position + moveDirection * (camTargetMoveAmount * inputDirSpeed) - moveCameraTarget.transform.position).XYZ3toX0Z3();
        //     //     moveCameraTarget.Move(dir * (camTargetMoveSpeed * inputSpeed * Time.deltaTime));
        //     // }
        //     // else
        //     // {
        //     //     var dir = transform.position - moveCameraTarget.transform.position + Vector3.up * 1.5f;
        //     //     moveCameraTarget.Move(dir * (camTargetMoveSpeed * camTargetGoBackRate * inputSpeed * Time.deltaTime));
        //     // }
        }
    }
}
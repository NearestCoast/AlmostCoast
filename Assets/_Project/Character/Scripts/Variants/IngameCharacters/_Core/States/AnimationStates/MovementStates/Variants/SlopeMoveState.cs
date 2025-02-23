using _Project.Utils;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class SlopeMoveState : MovementState
    {
        public override StateType Type => StateType.SlopeMove;
        [SerializeField, TitleGroup("Animation")] private LinearMixerTransition anims;

        private float OriginalAnimSpeed { get; set; }
        public override void PlayAnimation()
        {
            base.PlayAnimation();
            AnimancerState = AnimationStateConductor.BaseLayer.Play(anims);
            AnimancerState.NormalizedTime = animCutStartNormalizedTime;
            OriginalAnimSpeed = AnimancerState.Speed;
        }

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                if (LockParams.IsLockingOn) return false;
                if (inputChecker.HorizontalDirection3 == Vector3.zero) return false;
                
                return true;
                // return GroundParams.SlopeAngleDeg <= characterControllerEnveloper.SlopeLimit;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                if (LockParams.IsLockingOn) return true;
                var nextStateType = NextState.Type;
                
                var value = nextStateType switch
                {
                    StateType.Idle => true,
                    StateType.Landing => true,
                    StateType.CrouchStart => true,
                    StateType.Jump => true,
                    StateType.AirDash => true,
                    StateType.SkillJumpUp => true,
                    StateType.GroundPounding => true,
                    StateType.Hang => true,
                    StateType.Climb => true,
                    StateType.SlideDash => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                return value;
            }
        }
    }

    public partial class SlopeMoveState : MovementState
    {
        [SerializeField, TitleGroup("Velocity")] private bool useInputMagnitude = false;
        [SerializeField, TitleGroup("Velocity")] private float maxLength = 8;
        [SerializeField, TitleGroup("Velocity")] private float maxTime = 1;
        // [SerializeField, TitleGroup("Velocity"),Range(0,1)] private float angleGravityRate = 0.5f;
        [SerializeField, TitleGroup("Fx")] private float audioTick = 1;
        private bool IsBlocked { get; set; }
        private float AudioTickTimer { get; set; }
        private Vector3 lastMoveValue;
        
        protected override Vector3 GetVelocity()
        {
            movementStateValues.SetGravity();
            
            var inputMagnitudeAmplified = useInputMagnitude ? InputDirection.magnitude : 1; // 최대 1
            
            Vector3 moveValue;
            // if (GroundParams.SlopeAngleDeg == 0)
            // {
            //     moveValue = HorizontalDirection3 * (inputMagnitudeAmplified * maxLength / maxTime);
            // }
            // else
            // {
            //     var dir = Vector3.ProjectOnPlane(HorizontalDirection3, GroundParams.GroundNormal).normalized;
            //     moveValue = dir * (inputMagnitudeAmplified * maxLength / maxTime);
            // }

            var dir = Vector3.ProjectOnPlane(transform.forward, GroundParams.GroundNormal).normalized;
            moveValue = dir * ((GroundParams.SlopeAngleDeg + lastMoveValue.magnitude));
            moveValue = Vector3.ClampMagnitude(moveValue, maxLength / maxTime);
            
            lastMoveValue = moveValue;
            if (inputMagnitudeAmplified < 0.65f) MoveParams.SetStealthMove();
            else MoveParams.ResetStealthMove();

            if (MoveParams.IsStealthMove)
            {
                anims.State.Parameter = 0;
            }
            else
            {
                anims.State.Parameter = 1;
            }
            
            var ray = new Ray(transform.position + Vector3.up * 1.5f, moveValue);
            IsBlocked = Physics.Raycast(ray,  out var hitInfo, camTargetMoveAmount,  surfaceLayers);
            // if (IsBlocked) Debug.Log(IsBlocked);

            // if (anims.State is not null) anims.State.Parameter = (!GroundParams.IsGrounded && VerticalParams.IsWalled) ? 1 : 0;
            
            if (AudioTickTimer >= audioTick)
            {
                // if(GroundParams.IsGrounded)audioSource.Play();
                AudioTickTimer = 0;
            }

            AudioTickTimer += Time.deltaTime;
            
            return MoveParams.Gravity + (moveValue * Time.deltaTime) ;
        }

        public override Vector3 CameraTargetUpdate()
        {
            var moveDirection = MoveParams.GetGroundProjectedDirection(HorizontalDirection3);
            // var moveDirection = MoveParams.GetGroundProjectedDirection(HorizontalDirection3VerticalNegative);
            if (moveDirection.y > 0) moveDirection.y = 0;
        
            var inputDirSpeed = InputDirection.magnitude > 0.75f ? 1 : 0;
            var inputSpeed = InputDirection == Vector2.zero ? camTargetGoBackRate : InputDirection.magnitude;
        
            if (InitialPosition.y > transform.position.y + 1.5f)
            {
                var dir = transform.position - moveCameraTarget.transform.position + Vector3.up * 1.5f;
                return dir * (camTargetMoveSpeed * camTargetGoBackRate * inputSpeed * Time.deltaTime);
            }
            
            if (!IsBlocked)
            {
                var camToPlayer = (transform.position - Camera.main.transform.position).XYZ3toX0Z3();
                var rightOfCamToPlayer = Vector3.Cross(Vector3.up, camToPlayer).normalized;
                
                var dotMag = Mathf.Abs(Vector3.Dot(moveDirection, rightOfCamToPlayer));
                var dir = (transform.position + Vector3.up * 1.5f + moveDirection * (camTargetMoveAmount * inputDirSpeed * dotMag) - moveCameraTarget.transform.position);
                
                var value = dir * (camTargetMoveSpeed * inputSpeed);
                // var value = (dir.XYZ3toX0Z3().normalized + dir.XYZ3to0Y03().normalized) * (camTargetMoveSpeed * inputSpeed * Time.deltaTime);
                // if (value.magnitude > 0.25f) value = value.normalized * 0.25f;
                // Debug.Log(value + ", " + value.magnitude);
                return value * Time.deltaTime;
            }
            else
            {
                // return base.CameraTargetUpdate();
                var dir = transform.position - moveCameraTarget.transform.position + Vector3.up * 1.5f;
                return dir * (camTargetMoveSpeed * camTargetGoBackRate * inputSpeed * Time.deltaTime);
            }
            
            
            //
            // Vector3 CalculateMoveDirectionVerticalNegative(Vector3 forward, Vector2 inputDirection)
            // {
            //     var right = Quaternion.Euler(0, -90, 0) * forward;
            //     forward.y = 0;  
            //     right.y = 0;
            //     return (forward * inputDirection.y + right * inputDirection.x).normalized;
            // }
        }

        protected override Quaternion GetRotation()
        {
            return base.GetRotation();
        }
    }
}
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class RollState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.Roll;

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                return inputChecker.Direction2 != Vector2.zero && !MoveParams.IsJustRollEnded;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                if (NextState.Type == StateType.CrouchEnd) return false;
                
                var value = NextState.Type switch
                {
                    StateType.Landing => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                
                return value || IsAnimEnded;
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
            MoveParams.SetRollJustEnded();
            characterControllerEnveloper.ResetCharacterController();
        }
    }
    public partial class RollState : BaseLayerClipMovementState
    {
        [SerializeField, TitleGroup("Velocity")] private float maxLength = 4;
        [SerializeField, TitleGroup("Velocity")] private float maxTime = 1;
        [SerializeField, TitleGroup("Velocity"),Range(0,1)] private float angleGravityRate = 0.5f;
        [SerializeField, TitleGroup("Fx")] private float audioTick = 1;
        private bool IsBlocked { get; set; }
        private float AudioTickTimer { get; set; }
        
        protected override Vector3 GetVelocity()
        {
            if (GroundParams.IsGrounded)
            {
                MoveParams.GravityTime = 0f;
                
                if (GroundParams.GroundNormal == Vector3.up)
                // if (GroundParams.GroundNormalDotWithGroundPlane > 0.98f)
                {
                    if (transform.position.y - GroundParams.GroundPoint.y - characterControllerEnveloper.SkinWidth > 0.00001f)
                    {
                        MoveParams.Gravity = Vector3.down * (transform.position.y - GroundParams.GroundPoint.y - characterControllerEnveloper.SkinWidth);
                        if (MoveParams.HasMovingPlatform) MoveParams.Gravity = Vector3.zero;
                    }
                    else
                    {
                        MoveParams.Gravity = Vector3.zero;
                    }
                    
                }
                else
                {
                    MoveParams.Gravity = Vector3.ProjectOnPlane(Vector3.down, GroundParams.GroundNormal).normalized * GroundParams.SlopeAngleRad * angleGravityRate;
                    if (!characterControllerEnveloper.IsGrounded)
                    {
                        var sphereCastHit = Physics.SphereCast(transform.position, characterControllerEnveloper.Radius, Vector3.down, out var sphereCastHitInfo, characterControllerEnveloper.Height);
                        if (sphereCastHit)
                        {
                            MoveParams.Gravity += Vector3.down * sphereCastHitInfo.distance * 0.1f;
                            // Debug.Log(sphereCastHitInfo.distance);
                        }
                    }
                }
            }
            else
            {
                if (GroundParams.GroundNormalDotWithGroundPlane > 0.99f)
                {
                    MoveParams.Gravity = movementStateValues.GetGravity();
                    MoveParams.GravityTime += Time.deltaTime;
                }
                else
                {
                    MoveParams.Gravity = Vector3.ProjectOnPlane(Vector3.down, GroundParams.GroundNormal).normalized * GroundParams.SlopeAngleRad * angleGravityRate + movementStateValues.GetGravity();
                }
            }
            
            Vector3 moveValue;
            if (GroundParams.SlopeAngleDeg == 0)
            {
                moveValue = HorizontalDirection3 * (maxLength / maxTime);
            }
            else
            {
                var dir = Vector3.ProjectOnPlane(HorizontalDirection3, GroundParams.GroundNormal);
                moveValue = dir * (maxLength / maxTime);
            }
            
            var ray = new Ray(characterControllerEnveloper.transform.position, moveValue);
            IsBlocked = Physics.Raycast(ray,  out var hitInfo, camTargetMoveAmount,  surfaceLayers);
            
            if (AudioTickTimer >= audioTick)
            {
                // if(GroundParams.IsGrounded)audioSource.Play();
                AudioTickTimer = 0;
            }
            
            if (LockParams.LockOnTarget)
            {
                if (InputDirection.y > 0 && (transform.position + moveValue * Time.deltaTime - LockParams.LockOnTarget.transform.position).magnitude < 2f)
                {
                    moveValue = Vector3.zero;
                }
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
                var dotMag = Mathf.Abs(Vector3.Dot(moveDirection, Vector3.right));
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
            
            
        
            // Vector3 CalculateMoveDirectionVerticalNegative(Vector3 forward, Vector2 inputDirection)
            // {
            //     var right = Quaternion.Euler(0, -90, 0) * forward;
            //     forward.y = 0;  
            //     right.y = 0;
            //     return (forward * inputDirection.y + right * inputDirection.x).normalized;
            // }
        }
    }
}
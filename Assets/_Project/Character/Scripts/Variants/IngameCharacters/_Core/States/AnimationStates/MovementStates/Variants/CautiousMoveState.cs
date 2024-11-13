using _Project.Utils;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class CautiousMoveState : MovementState
    {
        public override StateType Type => StateType.CautiousMove;
        [SerializeField, TitleGroup("Animation")] private LinearMixerTransition anims;

        private float OriginalAnimSpeed { get; set; }
        public override void PlayAnimation()
        {
            base.PlayAnimation();
            AnimancerState = AnimationStateConductor.Animancer.Play(anims);
            OriginalAnimSpeed = AnimancerState.Speed;
        }

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                if (!LockParams.IsLockingOn) return false;
                if (inputChecker.HorizontalDirection3 == Vector3.zero) return false;
                if (!GroundParams.IsGrounded) return false;
                
                return true;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                if (!LockParams.IsLockingOn) return true;
                
                var value = NextState.Type switch
                {
                    StateType.Idle => true,
                    StateType.Landing => true,
                    StateType.CrouchStart => true,
                    StateType.Jump => true,
                    StateType.AirDash => true,
                    StateType.SkillJumpUp => true,
                    StateType.GroundPounding => true,
                    StateType.Climb => true,
                    StateType.SlideDash => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                return value;
            }
        }
    }

    public partial class CautiousMoveState : MovementState
    {
        [SerializeField, TitleGroup("Velocity")] private bool useInputMagnitude = false;
        [SerializeField, TitleGroup("Velocity")] private float maxLength = 8;
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

            

            Vector3 forwardMove = Vector3.zero;
            Vector3 lateralMove = Vector3.zero;
            
            if (LockParams.LockOnTarget)
            {

                // LockOnTarget까지의 방향 및 현재 거리 계산
                Vector3 toTarget = LockParams.LockOnTarget.transform.position - characterControllerEnveloper.transform.position;
                float currentDistance = toTarget.magnitude;
                if (currentDistance == 0f) return Vector3.zero; // 거리가 0이면 이동 없음
                toTarget.Normalize();

                float inputMagnitudeAmplified = useInputMagnitude ? InputDirection.magnitude : 1f;
                float speed = maxLength / maxTime; // 이동 속도 (단위: m/s)

                // 전후 이동 - LockOnTarget과의 거리 조정
                if (InputDirection.y != 0)
                {
                    forwardMove = toTarget * InputDirection.y * inputMagnitudeAmplified * speed * Time.deltaTime; // 전후 이동

                    // LockOnTarget과 2f 거리 이상에서만 이동 가능 
                    if (InputDirection.y > 0 && (transform.position + forwardMove - LockParams.LockOnTarget.transform.position).magnitude < 2f)
                    {
                        forwardMove = Vector3.zero;
                    }
                }

                // 좌우 이동 - LockOnTarget을 중심으로 원형 경로를 따라 이동
                if (InputDirection.x != 0)
                {
                    // 각속도 (degrees per second) 계산
                    float omegaDeg = (speed / currentDistance) * (180f / Mathf.PI); // 각속도 (deg/s)
                    // 현재 프레임에서의 회전 각도
                    float rotationAngle = -InputDirection.x * omegaDeg * Time.deltaTime * inputMagnitudeAmplified;

                    // LockOnTarget을 중심으로 현재 위치를 회전
                    Vector3 offset = transform.position - LockParams.LockOnTarget.transform.position;
                    Quaternion rotation = Quaternion.AngleAxis(rotationAngle, Vector3.up);
                    Vector3 rotatedOffset = rotation * offset;
                    Vector3 newPosition = LockParams.LockOnTarget.transform.position + rotatedOffset;

                    lateralMove = newPosition - transform.position;
                }
            
                if (inputMagnitudeAmplified < 0.5f) MoveParams.SetStealthMove();
                else MoveParams.ResetStealthMove();

                if (MoveParams.IsStealthMove)
                {
                    AnimancerState.Speed = OriginalAnimSpeed * 0.5f;
                }
                else
                {
                    AnimancerState.Speed = OriginalAnimSpeed;
                }
            }

            var moveValue = (forwardMove + lateralMove).XYZ3toX0Z3();
            
            var result = MoveParams.Gravity * Time.deltaTime + moveValue;

            if (InputDirection.x == 0)
            {
                if (InputDirection.y == 0)
                {
                    anims.State.Parameter = 0;
                }
                else if (InputDirection.y > 0)
                {
                    anims.State.Parameter = 0;
                }
                else
                {
                    anims.State.Parameter = 1;
                }
                
            }
            else if (InputDirection.x > 0)
            {
                if (InputDirection.y == 0)
                {
                    anims.State.Parameter = 2;
                }
                else if (InputDirection.y > 0)
                {
                    anims.State.Parameter = 3;
                }
                else
                {
                    anims.State.Parameter = 4;
                }
            }
            else
            {
                if (InputDirection.y == 0)
                {
                    anims.State.Parameter = 5;
                }
                else if (InputDirection.y > 0)
                {
                    anims.State.Parameter = 6;
                }
                else
                {
                    anims.State.Parameter = 7;
                }
            }

            // 최종 속도 반환
            return result; 
        }

        protected override Quaternion GetRotation()
        {
            if (LockParams.LockOnTarget != null)
            {
                // LockOnTarget의 방향을 계산
                var directionToTarget = (LockParams.LockOnTarget.transform.position - characterControllerEnveloper.transform.position).XYZ3toX0Z3();
        
                if (directionToTarget != Vector3.zero)
                {
                    // LockOnTarget의 방향을 바라보는 회전값을 생성
                    var lookRotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
                    return Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
                }
            }

            // LockOnTarget이 없거나 방향이 없을 경우 기존 로직을 유지
            if (isPlayer && inputChecker.Direction2 == Vector2.zero)
            {
                var lookRotation = Quaternion.LookRotation(transform.forward.XYZ3toX0Z3(), Vector3.up);
                return Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }

            if (HorizontalDirection3 == Vector3.zero) return transform.rotation;
            var value = Quaternion.LookRotation(HorizontalDirection3, Vector3.up);
            return Quaternion.Slerp(transform.rotation, value, rotationSpeed * Time.deltaTime);
        }

    }
}
using _Project.InputSystem;
using _Project.Utils;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;
using _Project.Combat.HitObjects;
using _Project.Effect;
using UnityEngine.VFX;
using UnityEvent = UnityEngine.Events.UnityEvent;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    [InfoBox("오를 수 있는 높이 최대 : maxJumpHeight, 최소점프높이 : maxJumpHeight * 0.5, Horizontal : maxSpeed")]
    public partial class JumpState : MovementState
    {
        public override StateType Type => StateType.Jump;
        
        [SerializeField, TitleGroup("Animation")] private LinearMixerTransition anims;
        
        [SerializeField, TitleGroup("CollisionEffects")] private VisualEffect collisionVFX;
        [SerializeField, TitleGroup("CollisionEffects")] private AudioSource collisionSFX;
        [SerializeField, TitleGroup("CollisionEffects")] private HitObject collisionHitObject;
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            var collisionEffectHolder = GetComponentInChildren<CollisionEffectHolder>(true);
            collisionVFX = collisionEffectHolder.GetComponentInChildren<VisualEffect>(true);
            collisionSFX = collisionEffectHolder.GetComponentInChildren<AudioSource>(true);
            collisionHitObject = collisionEffectHolder.GetComponentInChildren<HitObject>(true);
        }
#endif
        protected override void Awake()
        {
            base.Awake();
            MoveParams.ResetJumpCount();
            MoveParams.ResetWallJumpCount();
            MaxHeightTime = CalculateTimeToMaxHeight();

            return;
            float CalculateTimeToMaxHeight()
            {
                var a = InitialPosition.y; // 초기 높이
                var b = defaultJumpHeight; // 최대 높이
                var n = 10000; // 적분할 구간의 수 (정확도를 위해 큰 값 사용)
            
                var  integral = TrapezoidalIntegral(a, b, n);

                // 시간 계산 (적분값을 속도로 나눔)
                var  time = integral / leapSpeed;
                return time;
        
                // 적분할 함수 (로그 값을 사용)
                float Integrand(float y)
                {
                    var  diff = defaultJumpHeight - y + 1 + stayHeightParameter;
                    return 1f / Mathf.Log(diff);
                }

                // 트랩제도법을 이용한 적분 계산
                float TrapezoidalIntegral(float a, float b, int n)
                {
                    var  h = (b - a) / n;
                    var  sum = 0.5f * (Integrand(a) + Integrand(b)); // 첫 점과 마지막 점
                    for (var  i = 1; i < n; i++)
                    {
                        var  x = a + i * h;
                        sum += Integrand(x);
                    }
                    return sum * h;
                }
            }
        }

        public override void PlayAnimation()
        {
            base.PlayAnimation();
            AnimancerState = AnimationStateConductor.BaseLayer.Play(anims);
            AnimancerState.NormalizedTime = 0f;
        }

        private bool IsJumpPossible
        {
            get
            {
                if (isPlayer) return !MoveParams.IsJumpButtonPerforming;
                else return true;
            }
        }

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                
                return (GroundParams.IsGrounded || VerticalParams.IsWalled) &&
                       (MoveParams.IsJumpable || MoveParams.IsWallJumpable) &&
                       IsJumpPossible;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;

                switch (NextState.Type)
                {
                    case StateType.Jump:
                    {
                        return isPlayer ? StateTime > 0.2f : IsLeapEnd;
                    }
                    case StateType.Idle:
                    {
                        return JumpEnd;
                    }
                    case StateType.Move:
                    {
                        if (IsClimbJumping) return IsLeapEnd;
                        return JumpEnd;
                    }
                    
                    case StateType.CautiousMove:
                    {
                        if (IsClimbJumping) return IsLeapEnd;
                        return JumpEnd;
                    }
                    
                    case StateType.GroundPounding:
                    {
                        return StateTime > 0.2f;
                    }
                    
                    case StateType.Hang:
                    {
                        return IsLeapEnd;
                    }
                    
                    case StateType.Climb:
                    {
                        return StateTime > 0.2f;
                    }
                }
                
                var value = NextState.Type switch
                {
                    StateType.SkillJumpUp => true,
                    StateType.AirDash => true,
                    StateType.Landing => true,
                    StateType.BubbleReady => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                
                return value;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            
            if (MoveParams.IsWallJumpable) MoveParams.SetIsWallJumping();
            
            WallNormalSnap = (MoveParams.IsWallJumpable) ? VerticalParams.WallNormal.Value : Vector3.zero;
            
            if (MoveParams.IsGroundPoundingEnded)
            {
                MaxJumpHeight = defaultJumpHeight * 2f;
                FallingTime = minFallingTime * 1.5f;
            }
            else
            {
                if (MoveParams.IsWallJumping)
                {
                    MaxJumpHeight = defaultJumpHeight * 1.2f;
                    FallingTime = minFallingTime * 1.2f;
                }
                else
                {
                    MaxJumpHeight = defaultJumpHeight;
                    FallingTime = minFallingTime;
                }
                 
            }
            
            MaxLength = maxLength;
            
            if (MoveParams.IsWallJumping)
            {
                HorizontalDirSnap = (HorizontalDirection3).XYZ3toX0Z3();
                var dot = Vector3.Dot(HorizontalDirSnap, WallNormalSnap);
                
                IsClimbJumping = PrevState.Type == StateType.Climb && dot < 0;
            }
            else
            {
                IsClimbJumping = false;
            }
            
            if (!GroundParams.IsGrounded)
            {
                InitialHeightSnap = InitialPosition.y;
            }
            else
            {
                InitialHeightSnap = (MoveParams.IsWallJumpable) ? VerticalParams.WallPoint.Value.y : GroundParams.GroundPoint.y;
            }
            
            MoveParams.GravityTime = 0f;
            IsLeapEnd = false;
            JumpEnd = false;

            // TargetHeight = maxJumpHeight;
            movementStateValues.CurrentHeight = MaxJumpHeight;

            if (MoveParams.IsWallJumpable)
            {
                MoveParams.DecreaseWallJumpCount();
            }
            else
            {
                MoveParams.DecreaseJumpCount();
            }
            
            MoveParams.StartJumping();
            MoveParams.EndClimbing();
            MoveParams.IsJumpButtonPerforming = true;   
            
            if (MoveParams.IsHeadJumping)
            {
                collisionVFX.gameObject.SetActive(true);
                collisionVFX.Play();
                collisionSFX.Play();
                collisionHitObject.Invoke();
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();
            MoveParams.EndJumping();
            MoveParams.ResetWallJumping();
            MoveParams.ResetHeadJumping();
            MoveParams.Gravity = Vector3.zero;
        }

        public void ResetJumpButton()
        {
            if (gameObject.activeSelf) MoveParams.IsJumpButtonPerforming = false; 
        }
    }


    public partial class JumpState : MovementState
    {
        [SerializeField, TitleGroup("Velocity")]
        private float maxLength = 6;

        [SerializeField, TitleGroup("Velocity")]
        private PressingOnlyInput connectedInput;

        [SerializeField, TitleGroup("Velocity")]
        private float defaultJumpHeight = 4;

        [SerializeField, TitleGroup("Velocity")]
        private float leapSpeed = 10;

        [SerializeField, TitleGroup("Velocity")]
        private float minFallingTime = 0.4f;

        [SerializeField, TitleGroup("Velocity"), Range(0, 1)]
        private float stayHeightParameter = .1f;

        private bool IsBlocked { get; set; } = false;
        private bool isLeapEnd;

        private bool IsLeapEnd
        {
            get => isLeapEnd; 
            set
            {
                isLeapEnd = value;
                
                if (isLeapEnd)
                {
                    MoveParams.EndLeaping();
                    GroundParams.IsGrounded = false;
                }
            }
        }

        private bool JumpEnd { get; set; }
        private float InitialHeightSnap { get; set; }
        private float MaxHeightTime { get; set; }
        public float MaxJumpHeight { get; set; }
        public float MaxLength { get; set; }
        public float FallingTime { get; set; }

        private Vector3 HorizontalDirSnap { get; set; }
        private Vector3 WallNormalSnap { get; set; }
        private bool IsClimbJumping { get; set; }

        [SerializeField] private UnityEvent onLockOffDistance;
        
        protected override Vector3 GetVelocity()
        {
            if (LockParams.LockOnTarget)
            {
                if ((transform.position - LockParams.LockOnTarget.transform.position).XYZ3to0Y03().magnitude < 2f)
                {
                    onLockOffDistance?.Invoke();
                }
            }
            
            if (connectedInput && !connectedInput.IsPressing && transform.position.y - InitialHeightSnap> MaxJumpHeight * 0.5f)
            {
                IsLeapEnd = true;
            }

            var inputMagnitudeAmplified = isPlayer ? Mathf.Pow(InputDirection.magnitude, 2) : 1; // 최대 1
            Vector3 moveValue;
            if (MoveParams.IsWallJumping)
            {
                if (IsClimbJumping)
                {
                    moveValue = Vector3.zero;
                }
                else moveValue = HorizontalDirection3 * (inputMagnitudeAmplified * MaxLength / (MaxHeightTime + FallingTime));
            }
            else moveValue = HorizontalDirection3 * (inputMagnitudeAmplified * MaxLength / (MaxHeightTime + FallingTime));

            var value = (moveValue) * (MoveParams.IsWallJumping ? 1.5f : 1);
            value *= Time.deltaTime;


            Vector3 verticalVelocity;
            if (!IsLeapEnd)
            {
                var rayHead = new Ray(transform.position + Vector3.up * characterControllerEnveloper.Height / 2, Vector3.up);
                var isHeadHit = Physics.Raycast(rayHead.origin, rayHead.direction, characterControllerEnveloper.SkinWidth,
                    surfaceLayers);

                if (!isHeadHit)
                {
                    var diff = InitialHeightSnap + MaxJumpHeight - transform.position.y;
                    var log = Mathf.Log(diff + 1 + stayHeightParameter);
                    var yRevision = (log * leapSpeed) * Time.deltaTime;
                    if (transform.position.y + yRevision > InitialHeightSnap + MaxJumpHeight || diff + 1 < 0)
                    {
                        yRevision = MaxJumpHeight + InitialHeightSnap - transform.position.y;
                        IsLeapEnd = true;

                        if (yRevision < 0) Debug.Log("FUck");
                    }

                    verticalVelocity = Vector3.up * yRevision;
                }
                else
                {
                    verticalVelocity = Vector3.zero;
                    IsLeapEnd = true;
                }
            }
            else
            {
                IsLeapEnd = true;
                MoveParams.GravityTime += Time.deltaTime;
                MoveParams.Gravity = movementStateValues.GetGravity(MaxJumpHeight, FallingTime, out var isFinished);
                
                if (MoveParams.Gravity.y > 0) Debug.Log("FFFFFFF");
                
                JumpEnd = isFinished;
                

                if (GroundParams.IsGrounded && transform.position.y + MoveParams.Gravity.y < GroundParams.GroundPoint.y)
                {
                    JumpEnd = true;
                    MoveParams.Gravity = (GroundParams.GroundPoint - transform.position).XYZ3to0Y03();
                }

                verticalVelocity = MoveParams.Gravity;
            }


            if (anims.State is not null)
            {
                anims.State.Parameter = verticalVelocity.y < -0.05f
                        ? 0 // falling loop
                        : verticalVelocity.y > 0.5f
                            ? 2 // air loop
                            : 1 // start
                    ;
            }

            return verticalVelocity + value.XYZ3toX0Z3();
        }

    }


    public partial class JumpState : MovementState
    {
        public override Vector3 CameraTargetUpdate()
        {
            if (MoveParams.IsWallJumping || MoveParams.IsHeadJumping || 
                Mathf.Abs(transform.position.y - InitialHeightSnap) > defaultJumpHeight)
            {
                var d = (transform.position + Vector3.up * 0.5f) - moveCameraTarget.transform.position;
                return d * camTargetMoveSpeed * 3 * Time.deltaTime;
            }
            
            var moveDirection = MoveParams.GetGroundProjectedDirection(HorizontalDirection3);
            if (moveDirection.y > 0) moveDirection.y = 0;
            
            var inputDirSpeed = InputDirection.magnitude > 0.75f ? 1 : 0;
            var inputSpeed = InputDirection == Vector2.zero ? camTargetGoBackRate : InputDirection.magnitude;
            
            if (!IsBlocked)
            {
                var yRevision = 0f;
                if (moveDirection != Vector3.zero)
                {
                    var verticalRay = new Ray(InitialPosition + Vector3.up * MaxJumpHeight + moveDirection * MaxLength / (MaxHeightTime + FallingTime), Vector3.down * MaxJumpHeight);
                    var isHit = Physics.Raycast(verticalRay, out var hitInfo, MaxJumpHeight, surfaceLayers);
                    // Debug.DrawRay(verticalRay.origin, verticalRay.direction.normalized * MaxJumpHeight, Color.red);
                    
                    if (isHit)
                    {
                        yRevision = hitInfo.point.y + 1.5f - moveCameraTarget.transform.position.y;
                        if (yRevision < 0) yRevision = 0;
                    }
                }
            
                var dotMag = Mathf.Abs(Vector3.Dot(moveDirection, Vector3.right));
                var dir = (transform.position + moveDirection * (camTargetMoveAmount * inputDirSpeed * dotMag) - moveCameraTarget.transform.position).XYZ3toX0Z3() + Vector3.up * yRevision;
                var value = dir * (camTargetMoveSpeed * inputSpeed);
                return value * Time.deltaTime;
            }
            else
            {
                var dir = (transform.position - moveCameraTarget.transform.position).XYZ3toX0Z3() ;
                return dir * (camTargetMoveSpeed * camTargetGoBackRate * inputSpeed * Time.deltaTime);
            }
        }
    }


    public partial class JumpState : MovementState
    {
        public float MinJumpHeight => defaultJumpHeight * 0.5f;    
    }

}
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.ActionStates.MeleeAttacks;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Character.Scripts.Enemies.PlatformingEnemies.AttackStates
{
    public class Attack_01_Follower : Attack_01
    {
        protected override void Awake()
        {
            base.Awake();
            MoveParams.ResetJumpCount();
            MoveParams.ResetWallJumpCount();

            MaxHeightTime = CalculateTimeToMaxHeight();
            // Debug.Log(MaxHeightTime);
            
            // return;

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

        public override bool CanExitState => base.CanExitState && JumpEnd;

        public override void OnEnterState()
        {
            base.OnEnterState();
            MaxJumpHeight = defaultJumpHeight * characterControllerEnveloper.CurrentScale;
            FallingTime = minFallingTime;
            
            MaxLength = maxLength * characterControllerEnveloper.CurrentScale;
            
            InitialHeightSnap = GroundParams.GroundPoint.y;
            
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
        }

        public override void OnExitState()
        {
            base.OnExitState();
            MoveParams.EndJumping();
            MoveParams.Gravity = Vector3.zero;
        }
        
        [SerializeField, TitleGroup("Velocity")]
        private float maxLength = 6;

        [SerializeField, TitleGroup("Velocity")]
        private float defaultJumpHeight = 4;

        [SerializeField, TitleGroup("Velocity")]
        private float leapSpeed = 10;

        [SerializeField, TitleGroup("Velocity")]
        private float minFallingTime = 0.4f;

        // [SerializeField, TitleGroup("Velocity")]
        // private float wallForce = 10;

        [SerializeField, TitleGroup("Velocity"), Range(0, 1)]
        private float stayHeightParameter = .1f;

        private bool IsBlocked { get; set; }
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
        
        public override Vector3 GetVelocity()
        {
            if (transform.position.y - InitialHeightSnap> MaxJumpHeight * 0.5f)
            {
                IsLeapEnd = true;
            }

            var moveValue = transform.forward * (MaxLength / (MaxHeightTime + FallingTime));
            moveValue *= Time.deltaTime;

            Vector3 verticalVelocity;
            if (!IsLeapEnd)
            {
                var rayHead = new Ray(transform.position + Vector3.up * characterControllerEnveloper.Height / 2, Vector3.up);
                var isHeadHit = Physics.Raycast(rayHead.origin, rayHead.direction, characterControllerEnveloper.SkinWidth, surfaceLayers);

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
            
            return verticalVelocity + moveValue.XYZ3toX0Z3();
        }
    }
}

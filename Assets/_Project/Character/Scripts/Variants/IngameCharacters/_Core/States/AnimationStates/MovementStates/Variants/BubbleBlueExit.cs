using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class BubbleBlueExit : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.BubbleBlueExit;

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                return PrevState.Type == StateType.BubbleReady;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                if (NextState.Type == StateType.Jump) return IsLeapEnd;
                
                var value = NextState.Type switch
                {
                    StateType.Landing => true,
                    StateType.Climb => true,
                    StateType.AirDash => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                
                return value || IsJumpEnd;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            TargetDirSnap = transform.forward;
            InitialHeightSnap = InitialPosition.y;
            IsLeapEnd = false;
            IsJumpEnd = false;
            movementStateValues.CurrentHeight = maxJumpHeight;
            
            MoveParams.IsJumpButtonPerforming = true;   
        }

        public override void OnExitState()
        {
            base.OnExitState();
            
            MoveParams.Acceleration = TargetDirSnap * ((maxLength / maxTime) * afterAccelerationRate);

            MoveParams.GravityTime *= 0.5f;
        }
    }
    public partial class BubbleBlueExit : BaseLayerClipMovementState
    {
        [SerializeField, TitleGroup("Velocity")] private float maxJumpHeight = 5;
        [SerializeField, TitleGroup("Velocity")] private float leapTime = 1;
        [SerializeField, TitleGroup("Velocity")] private float maxLength = 10;
        [SerializeField, TitleGroup("Velocity")] private float maxTime = 1;
        [SerializeField, TitleGroup("Velocity")] private float fallingTime = 1;
        [SerializeField, TitleGroup("Velocity")] private float afterAccelerationRate = 1;
        [SerializeField, TitleGroup("Velocity"), Range(0, 1)] private float stayHeightParameter = .1f;
        
        private Vector3 TargetDirSnap { get; set; }
        private float InitialHeightSnap { get; set; }
        
        private bool IsLeapEnd { get;set; }
        private bool IsJumpEnd { get;set; }
        
        private float MaxSpeed => maxLength / maxTime;
        private float leapSpeed => maxJumpHeight / leapTime;
        private Vector3 CurVelocity { get; set; }
        private Vector3 PrevVelocity { get; set; }
        
        protected override Vector3 GetVelocity()
        {
            PrevVelocity = CurVelocity;
            var inputMagnitudeAmplified = Mathf.Pow(InputDirection.magnitude, 2);
            var moveValue = TargetDirSnap * (inputMagnitudeAmplified * MaxSpeed);
            var value = (moveValue);
            value *= Time.deltaTime;
            
            var height = (maxJumpHeight);
            Vector3 inputVelocity = Vector3.zero;
            if (!IsLeapEnd && transform.position.y < InitialPosition.y + height)
            {
                var diff = InitialPosition.y + maxJumpHeight - transform.position.y;
                var log = Mathf.Log(diff + 1 + stayHeightParameter);
                var yRevision = (log * leapSpeed ) * Time.deltaTime;
                if (transform.position.y + yRevision > InitialHeightSnap + maxJumpHeight)
                {
                    // yRevision = maxJumpHeight - transform.position.y;
                    IsLeapEnd = true;
                }
                
                inputVelocity = Vector3.up * yRevision + value.XYZ3toX0Z3(); 
            }
            else
            { 
                IsLeapEnd = true;
                MoveParams.GravityTime += Time.deltaTime;   
                MoveParams.Gravity = movementStateValues.GetGravity(maxJumpHeight, fallingTime, out var isFinished);
                if (MoveParams.Gravity.y > 0) Debug.Log("FFFFFFF");
                
                IsJumpEnd = isFinished;
                
                if (GroundParams.IsGrounded && transform.position.y + MoveParams.Gravity.y < GroundParams.GroundPoint.y)
                {
                    IsJumpEnd = true;
                    MoveParams.Gravity = (transform.position - GroundParams.GroundPoint).XYZ3to0Y03();
                }
                
                inputVelocity = MoveParams.Gravity + value.XYZ3toX0Z3();
            }

            return CurVelocity = inputVelocity;
        }
    }
}
using _Project.InputSystem;
using _Project.Utils;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    [InfoBox("오를 수 있는 높이 최대 : maxJumpHeight - 1")]
    public partial class ClimbJumpState : MovementState
    {
        public override StateType Type => StateType.ClimbJump;
        
        [SerializeField, TitleGroup("Animation")] private LinearMixerTransition anims;

        protected override void Awake()
        {
            base.Awake();
            MoveParams.ResetJumpCount();
        }

        public override void PlayAnimation()
        {
            base.PlayAnimation();
            AnimancerState = AnimationStateConductor.Animancer.Play(anims);
            AnimancerState.NormalizedTime = animCutStartNormalizedTime;
        }

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                
                return MoveParams.IsClimbing && MoveParams.IsJumpable && MoveParams.IsClimbable;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                if (!VerticalParams.IsWalled && IsLeapEnd) return true;
                
                switch (NextState.Type)
                {
                    case StateType.Climb :
                    { 
                        if (IsLeapEnd) return true;
                        break;
                    }
                }
                
                var value = NextState.Type switch
                {
                    StateType.Landing => true,
                    StateType.SlideDash => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                
                return value;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            
            MoveParams.GravityTime = 0f;
            IsLeapEnd = false;
            DownTime = 0f;
            
            MoveParams.DecreaseJumpCount();
            MoveParams.StartJumping();
            MoveParams.DecreaseClimbStaminaPerJump();
        }

        public override void OnExitState()
        {
            base.OnExitState();
            MoveParams.EndJumping();
        }
    }
    public partial class ClimbJumpState : MovementState
    {
        // [SerializeField, TitleGroup("Velocity")] private float maxSpeed = 6;
        private bool IsBlocked { get; set; }
        
        [SerializeField, TitleGroup("Velocity")] private PressingOnlyInput connectedInput;
        [SerializeField, TitleGroup("Velocity")] private float maxJumpHeight = 1f;
        [SerializeField, TitleGroup("Velocity")] private float leapSpeed = 10;
        [SerializeField, TitleGroup("Velocity")] private float gravitySpeed = 200;
        private bool IsLeapEnd { get; set; }
        private float DownTime { get; set; }
        
        protected override Vector3 GetVelocity()
        {
            var pressingRevision = connectedInput.PressingValue < .5f ? .5f : connectedInput.PressingValue;
            var height = pressingRevision * (maxJumpHeight);
            Vector3 inputVelocity;
            if (!IsLeapEnd && transform.position.y < InitialPosition.y + height)
            {
                var diff = InitialPosition.y + maxJumpHeight - transform.position.y;
                var log = Mathf.Log(diff + 1.1f);
                inputVelocity =  Vector3.up * (log * leapSpeed * pressingRevision * Time.deltaTime); 
            }
            else
            {
                IsLeapEnd = true;
                inputVelocity = Vector3.down * (Mathf.Pow(DownTime, 2) * gravitySpeed * pressingRevision * Time.deltaTime);
                DownTime += Time.deltaTime;
            }
            
            
            anims.State.Parameter = inputVelocity.y < -0.05f 
                ? 0 // falling loop
                : inputVelocity.y > 0.5f 
                    ? 2 // air loop
                    : 1 // start
                ;
            
            return inputVelocity;
        }

        protected override Quaternion GetRotation()
        {
            return transform.rotation;
        }
    }
}
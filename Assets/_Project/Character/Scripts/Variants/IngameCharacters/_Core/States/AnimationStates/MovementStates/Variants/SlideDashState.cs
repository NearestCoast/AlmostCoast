using _Project.InputSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class SlideDashState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.SlideDash;
        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                return GroundParams.IsGrounded;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                var value = NextState.Type switch
                {
                    StateType.Landing => true,
                    
                    StateType.Jump => true,
                    
                    StateType.Die => true,
                    _ => false,
                };
                
                var isTimeOver = StateTime >= maxTime;
                return isTimeOver || value;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            TargetDirSnap = transform.forward;
            characterControllerEnveloper.OnSlideStart();
        }

        public override void OnExitState()
        {
            base.OnExitState();
            if (NextState.Type == StateType.Jump)
            {
                MoveParams.Acceleration = TargetDirSnap * ((maxDashLength / maxTime) * afterAccelerationRate);
            }
            characterControllerEnveloper.ResetCharacterController();
        }
    }

    public partial class SlideDashState : BaseLayerClipMovementState
    {
        [SerializeField, TitleGroup("Velocity")] private float maxDashLength = 16;
        [SerializeField, TitleGroup("Velocity")] private float maxTime = 0.25f;
        [SerializeField, TitleGroup("Velocity")] private float afterAccelerationRate = 0.5f;
        
        private Vector3 TargetDirSnap { get; set; }
        
        protected override Vector3 GetVelocity()
        {
            var projection = Vector3.ProjectOnPlane(TargetDirSnap, GroundParams.GroundNormal).normalized;
            return (projection * (maxDashLength / (maxTime))) * Time.deltaTime;
        }

        protected override Quaternion GetRotation()
        {
            return transform.rotation;
        }
    }
}
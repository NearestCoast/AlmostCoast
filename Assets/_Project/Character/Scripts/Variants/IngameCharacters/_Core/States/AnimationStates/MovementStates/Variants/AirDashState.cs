using _Project.InputSystem;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class AirDashState : BaseLayerClipMovementState
    {
        public override StateType Type => StateType.AirDash;
        
        protected override void Awake()
        {
            base.Awake();
            MoveParams.ResetKickCount();
        }

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return false;
                return !GroundParams.IsGrounded && MoveParams.KickCount > 0;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                switch (NextState.Type)
                {
                    case StateType.AirDash:
                    {   
                        return MoveParams.KickCount > 0;
                    }
                }
                
                var value = NextState.Type switch
                {
                    StateType.Landing => true,
                    StateType.Climb => true,
                    
                    StateType.Die => true,
                    _ => false,
                }; 
                
                return value || StateTime > maxTime || !connectedInput.IsPressing;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            MoveParams.ResetAcceleration();
            MoveParams.DecreaseKickCount();
            DirSnap = transform.forward;
        }
    }

    public partial class AirDashState : BaseLayerClipMovementState
    {
        [SerializeField, TitleGroup("Velocity")] private PressingOnlyInput connectedInput;
        [SerializeField,TitleGroup("Velocity")] private float maxLength = 8;
        [SerializeField,TitleGroup("Velocity")] private float maxTime = 0.5f;
        
        private Vector3 DirSnap { get; set; }

        protected override Vector3 GetVelocity()
        {
            return DirSnap * (maxLength / (maxTime)) * Time.deltaTime;
        }

        protected override Quaternion GetRotation()
        {
            return transform.rotation;
        }
    }

    public partial class AirDashState : BaseLayerClipMovementState
    {
        public float MaxLength => maxLength;
    }
}
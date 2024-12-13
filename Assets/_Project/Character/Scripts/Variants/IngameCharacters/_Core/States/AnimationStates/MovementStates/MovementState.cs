using System;
using _Project.Cameras;
using _Project.Characters._Core;
using _Project.Characters._Core.Input;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Maps.Climber.Objects;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using AnimationState = _Project.Characters._Core.States.AnimationStates.AnimationState;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class MovementState : AnimationState
    {
        public enum StateType
        {
            None = 0,
            Idle,
            Move,
            WanderingMove,
            CautiousMove,
            Landing,
            BubbleReady,
            BubbleBlueExit,
            
            CrouchStart,
            CrouchEnd,
            Roll,
            
            Hang,
            HangJump,
            Climb,
            ClimbJump,
            Jump,
            JumpDirectionChange,
            
            AirDash,
            
            ClimbOverLedge, 
            ClimbRightLedge,
            ClimbLeftLedge,
            ClimbLeftTransition,
            ClimbRightTransition,
            GroundPounding,
            HeadKick,
            SlideDash,
            SkillJumpDrop,
            SkillJumpUp,
            WallRide,
            
            Surprised,
            
            Die,
        }

        [ShowInInspector] public virtual StateType Type { get; }
        protected MovementState NextState => AnimationStateConductor.MovementStateMachine.NextState as MovementState;
        protected MovementState PrevState => AnimationStateConductor.MovementStateMachine.PreviousState as MovementState;
    }

    public partial class MovementState : AnimationState
    {
       


        public float StateTime { get; private set; }
        protected Vector3 InitialPosition { get; private set; }
        
        protected override void OnEnable()
        {
            StateTime = 0f;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            
            PlayAnimation();
            
            InitialPosition = transform.position;
        }

        public virtual void ProgressStateTime()
        {
            StateTime += Time.deltaTime;
        }
    }

    public partial class MovementState : AnimationState
    {
        public virtual void PlayAnimation()
        {
            
        }
    }

    public partial class MovementState : AnimationState
    {
        public Vector3 Velocity => GetVelocity();
        protected virtual Vector3 GetVelocity()
        {
            return Vector3.zero;
        }
    }

    public partial class MovementState : AnimationState
    {
        [SerializeField, TitleGroup("Rotation")] public bool isPlayer = false;
        [SerializeField, TitleGroup("Rotation")] protected float rotationSpeed = 60;
        public Quaternion Rotation => GetRotation();
        protected virtual Quaternion GetRotation()
        {
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

    public partial class MovementState : AnimationState
    {
        [SerializeField, TitleGroup("CameraTarget")] protected float camTargetMoveAmount = 4;
        [SerializeField, TitleGroup("CameraTarget")] protected float camTargetMoveSpeed = 4;
        [SerializeField, TitleGroup("CameraTarget"), Tooltip("This Value is Multiplied with camTargetMoveDirSpeed")] protected float camTargetGoBackRate = 1;
        [SerializeField, TitleGroup("CameraTarget")] protected bool hitCheck = true;
        [SerializeField, TitleGroup("CameraTarget")] protected bool hitCheckByCharacter = false;
        
        public virtual Vector3 CameraTargetUpdate()
        {
            var dir = (transform.position + Vector3.up * 0.5f) - moveCameraTarget.transform.position;
            return dir * (camTargetMoveSpeed * camTargetGoBackRate * Time.deltaTime);
        }
    }
}
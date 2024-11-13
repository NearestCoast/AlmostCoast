using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using Sirenix.OdinInspector;
using UnityEngine;
using AnimationState = _Project.Characters._Core.States.AnimationStates.AnimationState;

namespace _Project.Characters.IngameCharacters.Core.ActionStates
{
    public partial class ActionState : AnimationState
    {
        public enum StateType
        {
            None = 0,
            ActionIdle,
            Attack_01,
            Attack_02,
            Attack_03,
            SpecialAttack_01,
            RangeAttack_01,
            RangeAttack_02,
            
            Flinched,
            KnockBacked,
            GetUp,
            
            Dying
        }

        [ShowInInspector] public virtual StateType Type { get; }
        
        protected ActionState NextState => AnimationStateConductor.ActionStateMachine.NextState as ActionState;
        protected ActionState PrevState => AnimationStateConductor.ActionStateMachine.PreviousState as ActionState;
        
        [SerializeField, TitleGroup("Animation")] protected float animCutStartNormalizedTime = 0;
        [SerializeField, TitleGroup("Animation")] protected float animCutEndNormalizedTime = 1;

        protected override bool IsAnimEnded => AnimancerState.NormalizedTime >= animCutEndNormalizedTime;
        public override bool CanExitState => IsAnimEnded;
    }

    public partial class ActionState : AnimationState
    {
        [SerializeField, TitleGroup("Velocity")] private bool holdActionVelocity;
        public bool HoldActionVelocity => holdActionVelocity;
        
        [SerializeField, TitleGroup("ActionParams")] private float actionRange = 2;
        public float ActionRange => actionRange * characterControllerEnveloper.CurrentScale;
        
        [SerializeField, TitleGroup("ActionParams")] private float afterActionDelayTime = 1;
        public float AfterActionDelayTime => afterActionDelayTime;

        [SerializeField, TitleGroup("ActionParams")] private float weight = 1;
        public float Weight => weight;

        [SerializeField, TitleGroup("ActionParams")] private int damage = 0;
        public int Damage => damage;

        public float AnimNormalizedTime => AnimancerState.NormalizedTime;   
        public float RemainingDuration => AnimancerState.RemainingDuration;
        
        public float StateTime { get; private set; }
        protected Vector3 InitialPosition { get; private set; }
        public Vector3 Acc { get; protected set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            StateTime = 0f;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            
            InitialPosition = transform.position;
        }

        public void ProgressStateTime()
        {
            StateTime += Time.deltaTime;
        }

        [SerializeField, TitleGroup("Velocity"),Range(0,1)] private float angleGravityRate = 0.5f;
        public virtual Vector3 GetVelocity()
        {
            if (masterCharacter is PlayerCharacter) return Vector3.zero;
            if (GroundParams.IsGrounded)
            {
                MoveParams.GravityTime = 0f;
                
                if (GroundParams.GroundNormal == Vector3.up)
                // if (GroundParams.GroundNormalDotWithGroundPlane > 0.98f) 
                {
                    if (transform.position.y - GroundParams.GroundPoint.y - characterControllerEnveloper.SkinWidth > 0.00001f)
                    {
                        MoveParams.Gravity = Vector3.down * (transform.position.y - GroundParams.GroundPoint.y - characterControllerEnveloper.SkinWidth);
                        if (MoveParams.HasMovingPlatform)
                        {
                            MoveParams.Gravity = Vector3.zero;
                        }
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
                            // Debug.Log(hitInfo.distance);
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

            return MoveParams.Gravity;
        }
    }
}
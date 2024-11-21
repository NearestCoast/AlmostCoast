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
            movementStateValues.SetGravity();

            return MoveParams.Gravity;
        }
    }
}
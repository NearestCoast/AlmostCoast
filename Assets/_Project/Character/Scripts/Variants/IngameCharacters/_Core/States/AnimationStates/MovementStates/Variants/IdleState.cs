using System.Collections.Generic;
using _Project.Characters._Core;
using _Project.Utils;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public partial class IdleState : MovementState
    {
        public override StateType Type => StateType.Idle;
        
        [SerializeField, TitleGroup("Animation")] private List<ClipTransition> anims;

        public override void PlayAnimation()
        {
            base.PlayAnimation();
            AnimancerState = AnimationStateConductor.BaseLayer.Play(anims[0]);
            AnimancerState.NormalizedTime = animCutStartNormalizedTime;
        }

        public override bool CanEnterState
        {
            get
            {
                if (MoveParams.IsUnderCrowdControl) return true;
                return inputChecker.Direction2 == Vector2.zero;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (NextState.Type == StateType.AirDash) return false;
                return NextState.Type != StateType.Idle;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            MoveParams.SetStealthMove();
        }

        // [SerializeField, TitleGroup("Velocity"),Range(0,1)] private float angleGravityRate = 0.5f;
        // [SerializeField, TitleGroup("Velocity")] private float exceptionalMove = 10;

        protected override Vector3 GetVelocity()
        {
            movementStateValues.SetGravity();
            return (MoveParams.Gravity);
        }

        protected override Quaternion GetRotation()
        {
            if (LockParams.IsLockingOn)
            {
                return Quaternion.LookRotation((LockParams.LockOnTarget.position - transform.position).XYZ3toX0Z3());
            }
            return transform.rotation;
        }
    }
}
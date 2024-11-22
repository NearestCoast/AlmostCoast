using _Project.Characters._Core.States.AnimationStates;
using _Project.Characters._Core.States.CommonStates;
using _Project.Characters._Core.Input;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks
{
    [TaskCategory("Custom")]
    public class CustomTask : Action
    {
        protected Enemy master;
        protected InputChecker inputChecker;
        protected AnimationStateConductor animationStateConductor;
        protected CommonStateConductor commonStateConductor;

        protected MoveParams moveParams;
        protected VerticalParams verticalParams;
        protected GroundParams groundParams;
        protected LockParams lockParams;

        protected LayerMask surfaceLayers;

        protected Pathfinder pathfinder;
        
        public override void OnAwake()
        {
            base.OnAwake();
            master = transform.GetComponentInParent<Enemy>();
            moveParams = transform.GetComponentInParent<MoveParams>();
            verticalParams = transform.GetComponentInParent<VerticalParams>();
            groundParams = transform.GetComponentInParent<GroundParams>();
            lockParams = transform.GetComponentInParent<LockParams>();
            inputChecker = transform.GetComponentInParent<InputChecker>();
            animationStateConductor = transform.GetComponentInParent<AnimationStateConductor>();
            commonStateConductor = transform.GetComponentInParent<CommonStateConductor>();
            pathfinder = transform.GetComponentInChildren<Pathfinder>();
            
            surfaceLayers = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("GroundUnlit") | 1 << LayerMask.NameToLayer("Wall") |  1 << LayerMask.NameToLayer("Character") ;
        }

        protected void SetBothIdle()
        {
            SetMovementIdle();
            SetActionIdle();
        }

        protected void SetMovementIdle()
        {
            animationStateConductor.TrySetMovementState(master.MovementStateContainer[MovementState.StateType.Idle]);
        }

        protected void SetActionIdle()
        {
            animationStateConductor.TrySetActionState(master.ActionStateContainer[ActionState.StateType.ActionIdle]);
        }
    }
}
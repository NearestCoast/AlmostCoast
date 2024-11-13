using _Project.Characters._Core.States.AnimationStates;
using _Project.Characters._Core.Input;
using _Project.Characters.IngameCharacters.Core;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals
{
    [TaskCategory("Custom")]
    public class CustomConditional : Conditional
    {
        protected Enemy master;
        protected InputChecker inputChecker;
        protected AnimationStateConductor animationStateConductor;

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
            pathfinder = transform.GetComponentInChildren<Pathfinder>();
            
            surfaceLayers = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("GroundUnlit") | 1 << LayerMask.NameToLayer("Wall");
        }
    }
}
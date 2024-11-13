using _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates;
using _Project.Utils;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks
{
    public class Chase : CustomTask
    {
        public override void OnStart()
        {
            base.OnStart();
            // Debug.Log("Chase");
            
            commonStateConductor.TrySetLockState(master.LockStateContainer[LockState.StateType.LockOff]);
            lockParams.LockOnTarget = null;
            
            pathfinder.SetTargetToPlayer();
        }

        public override TaskStatus OnUpdate()
        {
            // SetBothIdle();
            animationStateConductor.TrySetActionState(master.ActionStateContainer[ActionState.StateType.ActionIdle]);
            if (master.IsInAttackPhase)
            {
                inputChecker.HorizontalDirection3 = Vector3.zero;
                SetMovementState();
                return TaskStatus.Running;
            }
            
            pathfinder.CalculatePath();
            SetDirection();
            SetMovementState();
            return TaskStatus.Running;
        }

        private void SetDirection()
        {
            var toTarget = (pathfinder.TargetCharacter.transform.position - transform.position).XYZ3toX0Z3();
            var ledgeRay = new Ray(transform.position + toTarget.normalized * 8 + Vector3.down, -toTarget.normalized);
            var isHit = Physics.Raycast(ledgeRay, out var hitInfo, 8, surfaceLayers); 
            if (isHit)
            {
                inputChecker.HorizontalDirection3 = toTarget.normalized;
            }
            else
            {
                if (!moveParams.IsJumping)
                {
                    if (verticalParams.IsWalled)
                    {
                        inputChecker.HorizontalDirection3 = toTarget.normalized;
                    }
                    else
                    {
                        var toNextPos = (pathfinder.NextNodePoint - transform.position).XYZ3toX0Z3();
                        inputChecker.HorizontalDirection3 = toNextPos.XYZ3toX0Z3().normalized;
                    }
                }
            }
        }

        private void SetMovementState()
        {
            if (inputChecker.HorizontalDirection3 != Vector3.zero)
            {
                if (!moveParams.IsLeaping)
                {
                    animationStateConductor.TrySetMovementState(master.MovementStateContainer[MovementState.StateType.Move]);
                    
                    if (verticalParams.IsWalled)
                    {
                        var ray = new Ray(transform.position + Vector3.up, inputChecker.HorizontalDirection3);
                        if (Physics.Raycast(ray, out var hitInfo, 2, surfaceLayers))
                        {
                            animationStateConductor.TrySetMovementState(master.MovementStateContainer[MovementState.StateType.Jump]);
                        }
                    }
                    else
                    {
                        if (groundParams.IsGrounded)
                        {
                            var ray = new Ray(transform.position + inputChecker.HorizontalDirection3, Vector3.down);
                            var forwardBellowHit = Physics.Raycast(ray, out var hitInfo, 2, surfaceLayers);
                            
                            if (!forwardBellowHit)
                            {
                                animationStateConductor.TrySetMovementState(master.MovementStateContainer[MovementState.StateType.Jump]);
                            }
                        }
                    }
                }
            }
            else
            {
                animationStateConductor.TrySetMovementState(master.MovementStateContainer[MovementState.StateType.Idle]);
            }
            
            // Debug.Log(master.CurrentActionState);
        }
    }
}
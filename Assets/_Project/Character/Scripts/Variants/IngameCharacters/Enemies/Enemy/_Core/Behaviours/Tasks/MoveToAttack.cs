using System.Linq;
using _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates.RangeAttacks;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates;
using _Project.Utils;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks
{
    public class MoveToAttack : CustomTask
    {
        public override void OnStart()
        {
            base.OnStart();
            
            commonStateConductor.TrySetLockState(master.LockStateContainer[LockState.StateType.LockOff]);
            lockParams.LockOnTarget = null;
            if (master.PredictedAttackState is RangeAttack rangeAttack)
            {
                pathfinder.SetTargetRandomly(pathfinder.TargetCharacter.transform.position, rangeAttack.ActionRange);
            }
            else
            {
                pathfinder.SetTargetToPlayer();
            }
        }

        public override TaskStatus OnUpdate()
        {
            SetBothIdle();
            if (master.IsInAttackPhase)
            {
                inputChecker.HorizontalDirection3 = Vector3.zero;
                SetAction();
                return TaskStatus.Running;
            }
            pathfinder.CalculatePath();
            SetDirection();
            SetAction();

            

            if (master.PredictedAttackState is RangeAttack rangeAttack)
            {
                if (pathfinder.DistanceToTargetCharacter <= master.AlternateAttackState.ActionRange)
                {
                    master.IsFightMoveEnd = true;
                    master.PredictedAttackState = master.AlternateAttackState;
                }
                else if (pathfinder.IsReachedToTarget)
                {
                    master.IsFightMoveEnd = true;
                }
            }
            else
            {
                if (pathfinder.FlattenedDistanceToTarget <= master.PredictedAttackState.ActionRange)
                {
                    master.IsFightMoveEnd = true;
                }
            }
            
            return TaskStatus.Running;
        }

        private void SetDirection()
        {
            Vector3 toTarget;
            if (pathfinder.IsPathComplete)
            {
                toTarget = (pathfinder.NextNodePoint - transform.position).XYZ3toX0Z3();
            }
            else
            {
                toTarget = (pathfinder.Target.position - transform.position).XYZ3toX0Z3();
            }
            
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
                        inputChecker.HorizontalDirection3 = toTarget.normalized;
                    }
                }
            }
        }

        private void SetAction()
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
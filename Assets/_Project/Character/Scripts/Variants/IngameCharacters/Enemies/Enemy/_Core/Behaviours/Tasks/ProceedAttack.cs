using System;
using _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates.Variants;
using _Project.Utils;
using Animancer.FSM;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks; // UniTask 사용을 위해 추가
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks
{
    public class ProceedAttack : CustomTask
    {
        public override void OnStart()
        {
            base.OnStart();
            
            if (commonStateConductor.CurrentLockState is LockOffState)
            {
                commonStateConductor.TrySetLockState(master.LockStateContainer[LockState.StateType.LockOn]);
                lockParams.LockOnTarget = pathfinder.TargetCharacter.transform;
            }
            
            master.IsFightMoveEnd = false;
        }

        public override TaskStatus OnUpdate() 
        {
            // SetBothIdle();
            if (!master.IsInAttackPhase)
            {
                inputChecker.Direction2 = Vector2.zero;
                animationStateConductor.TrySetMovementState(master.MovementStateContainer[MovementState.StateType.Idle]);
            
                var toTarget = (pathfinder.TargetCharacter.transform.position - transform.position).XYZ3toX0Z3();
                inputChecker.HorizontalDirection3 = toTarget.normalized;
                animationStateConductor.TrySetActionState(master.PredictedAttackState);
            
                if (master.CurrentActionState.Type == master.PredictedAttackState.Type)
                {
                    WaitAndResetNextAttackState().Forget();
                }
            }
            else
            {
                animationStateConductor.TrySetActionState(master.ActionStateContainer[ActionState.StateType.ActionIdle]);
            }
            return TaskStatus.Running;
        }
        
        private async UniTask WaitAndResetNextAttackState()
        {
            if (master.PredictedAttackState != null)
            {
                master.IsInAttackPhase = true;
                var currentAttackState = master.PredictedAttackState;

                await UniTask.WaitUntil(() => master.CurrentActionState.Type != currentAttackState.Type);
                animationStateConductor.SetActionMaskFullBody();
                transform.rotation = Quaternion.LookRotation(pathfinder.TargetCharacter.transform.position);
                
                await UniTask.Delay(TimeSpan.FromSeconds(master.PredictedAttackState.AfterActionDelayTime));
                master.PredictedAttackState = null;
                master.IsAttackEnd = true;
                master.IsInAttackPhase = false;
            }
        }
    }
}
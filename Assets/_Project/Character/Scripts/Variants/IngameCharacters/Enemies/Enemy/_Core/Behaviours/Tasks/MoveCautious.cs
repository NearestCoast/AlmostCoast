using System;
using System.Threading;
using _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates;
using _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates.Variants;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks
{
    public class MoveCautious : CustomTask
    {
        private CancellationTokenSource cancellationTokenSource;

        private int[] xDirections = { -1, 1 };
        private int[] yDirections = { 0, -1 };

        public override void OnStart()
        {
            base.OnStart();
            // Debug.Log("MoveCautious");
            
            cancellationTokenSource = new CancellationTokenSource();
            WaitAndProceed().Forget();
            
            if (master.IsInAttackPhase)
            {
                inputChecker.HorizontalDirection3 = Vector3.zero;
                return;
            }
            
            if (commonStateConductor.CurrentLockState is LockOffState)
            {
                commonStateConductor.TrySetLockState(master.LockStateContainer[LockState.StateType.LockOn]);
                lockParams.LockOnTarget = pathfinder.TargetCharacter.transform;
            }

            var randomX = xDirections[Random.Range(0, xDirections.Length)];
            var randomY = yDirections[Random.Range(0, yDirections.Length)];
            inputChecker.Direction2 = new Vector2(randomX, randomY);
            animationStateConductor.TrySetMovementState(master.MovementStateContainer[MovementState.StateType.CautiousMove]);
        }

        public override TaskStatus OnUpdate()
        {
            SetBothIdle();
            return TaskStatus.Running;
        }

        private async UniTask WaitAndProceed()
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Random.Range(0.5f, 2.5f)), cancellationToken: cancellationTokenSource.Token);
                master.IsFightMoveEnd = true;
                master.IsAttackEnd = true; // 얘도 꼭 있어야함.
            }
            catch (OperationCanceledException)
            {
                // 조건이 변경되면 대기 중이던 작업을 취소합니다.
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }
    }
}
using System;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks; // UniTask 사용을 위해 추가
using System.Threading;
using _Project.Characters.IngameCharacters.Core.ActionStates;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks
{
    public class ProceedStop : CustomTask
    {
        private CancellationTokenSource cancellationTokenSource;

        public override void OnStart()
        {
            base.OnStart();
            cancellationTokenSource = new CancellationTokenSource();
            WaitAndProceed().Forget();
            master.IsAwarePlayer = false;
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
                var waitingTime = master.Stat.MaxWaitingTime;
                await UniTask.Delay(TimeSpan.FromSeconds(waitingTime), cancellationToken: cancellationTokenSource.Token);
                master.ReturnToStartPosition();
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
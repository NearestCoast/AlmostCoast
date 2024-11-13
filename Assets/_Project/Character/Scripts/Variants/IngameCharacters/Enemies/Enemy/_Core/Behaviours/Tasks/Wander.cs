using System;
using _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Utils;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks; // UniTask 사용을 위해 추가
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks
{
    public class Wander : CustomTask
    {
        public override void OnStart()
        {
            base.OnStart();
            
            // Debug.Log("Wander");
            pathfinder.IsWandering = true;
            
            inputChecker.HorizontalDirection3 = Vector3.zero;
                
            pathfinder.SetTargetToWayPoint();
            pathfinder.CalculatePath();
        }

        private bool IsWaiting { get; set; }
        public override TaskStatus OnUpdate()
        {
            SetBothIdle();
            if (IsWaiting) return TaskStatus.Running;
            
            if (pathfinder.IsReachedToTarget)
            {
                IsWaiting = true;
                WaitAndResume().Forget(); // 3초 후 IsWaiting을 false로 설정하는 함수 호출
                
                inputChecker.HorizontalDirection3 = Vector3.zero;
                
                pathfinder.SetTargetToWayPoint();
                pathfinder.CalculatePath();
            }
            else
            {
                var dir = (pathfinder.NextNodePoint - transform.position);
                inputChecker.HorizontalDirection3 = dir.XYZ3toX0Z3().normalized;
            }    
                
            if (pathfinder.IsReachedToNextNode) pathfinder.SetNextNode();
            
            if (inputChecker.HorizontalDirection3 != Vector3.zero)
            {
                animationStateConductor.TrySetMovementState(master.MovementStateContainer[MovementState.StateType.WanderingMove]);
            }
            else
            {
                animationStateConductor.TrySetMovementState(master.MovementStateContainer[MovementState.StateType.Idle]);
            }
            
            return TaskStatus.Running;
        }

        private async UniTask WaitAndResume()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3)); // 3초 대기
            IsWaiting = false;
        }
    }
}
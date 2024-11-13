using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Utils;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks
{
    public class BeingSurprised : CustomTask
    {
        public override void OnStart()
        {
            base.OnStart();
            var toTarget = (pathfinder.TargetCharacter.transform.position - transform.position).XYZ3toX0Z3();
            inputChecker.HorizontalDirection3 = toTarget.normalized;
            animationStateConductor.ForceSetMovementState(master.MovementStateContainer[MovementState.StateType.Surprised]);
            Wait().Forget();
        }

        public override TaskStatus OnUpdate()
        {
            SetBothIdle();
            return TaskStatus.Running;
        }

        private async UniTask Wait()
        {
            await UniTask.WaitUntil(() => master.CurrentMovementState.Type != MovementState.StateType.Surprised);
            master.IsJustEncountered = false;
        }
    }
}
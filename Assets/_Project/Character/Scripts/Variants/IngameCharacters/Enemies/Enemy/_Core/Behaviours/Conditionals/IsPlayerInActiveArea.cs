using _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals;
using _Project.Utils;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals
{
    public class IsPlayerInActiveArea : CustomConditional
    {
        public override TaskStatus OnUpdate()
        {
            if (pathfinder.TargetCharacter.CurrentLevel.ID == master.CurrentLevel.ID) return TaskStatus.Success;
            else return TaskStatus.Failure;
            // var directionToPlayer = pathfinder.TargetCharacter.transform.position - transform.position;
            // if (directionToPlayer.XYZ3toX0Z3().magnitude <= master.ActiveAreaRadius)
            // {
            //     return TaskStatus.Success;
            // }
            //
            // return TaskStatus.Failure;
        }
    }
}
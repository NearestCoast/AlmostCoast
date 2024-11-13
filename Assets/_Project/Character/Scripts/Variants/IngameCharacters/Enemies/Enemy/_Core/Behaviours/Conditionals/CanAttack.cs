using _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals;
using _Project.Characters.IngameCharacters.Core.ActionStates.RangeAttacks;
using _Project.Utils;
using BehaviorDesigner.Runtime.Tasks;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals
{
    public class CanAttack : CustomConditional
    {
        public override TaskStatus OnUpdate()
        {
            if (master.PredictedAttackState is RangeAttack rangeAttack)
            {
                // if (pathfinder.IsReachedToTarget)
                // {
                //     var directionToPlayer = pathfinder.TargetCharacter.transform.position - transform.position;
                //     if (directionToPlayer.XYZ3toX0Z3().magnitude <= master.PredictedAttackState.ActionRange)
                //     {
                //         return TaskStatus.Success;
                //     }
                // }

                if (master.IsFightMoveEnd)
                {
                    var directionToPlayer = pathfinder.TargetCharacter.transform.position - transform.position;
                    if (directionToPlayer.XYZ3toX0Z3().magnitude <= master.PredictedAttackState.ActionRange)
                    {
                        return TaskStatus.Success;
                    }
                }
            }
            else
            {
                var directionToPlayer = pathfinder.TargetCharacter.transform.position - transform.position;
                if (directionToPlayer.XYZ3toX0Z3().magnitude <= master.PredictedAttackState.ActionRange)
                {
                    return TaskStatus.Success;
                }   
            }

            return TaskStatus.Failure;
        }
    }
}
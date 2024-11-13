using _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals;
using _Project.Utils;
using BehaviorDesigner.Runtime.Tasks;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals
{
    public class IsPlayerInActiveArea : CustomConditional
    {
        public override TaskStatus OnUpdate()
        {
            var directionToPlayer = pathfinder.TargetCharacter.transform.position - transform.position;
            if (directionToPlayer.XYZ3toX0Z3().magnitude <= master.Stat.AwareAreaRadius)
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
}
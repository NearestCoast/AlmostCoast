using BehaviorDesigner.Runtime.Tasks;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals
{
    public class HasNextAttackState : CustomConditional
    {
        public override TaskStatus OnUpdate()
        {
            return master.PredictedAttackState ? TaskStatus.Success : TaskStatus.Failure;    
        }
    }
}
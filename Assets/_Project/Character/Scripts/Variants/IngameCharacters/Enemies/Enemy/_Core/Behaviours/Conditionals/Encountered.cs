using BehaviorDesigner.Runtime.Tasks;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals
{
    public class Encountered : CustomConditional
    {
        public override TaskStatus OnUpdate()
        {
            var result =  master.IsJustEncountered ? TaskStatus.Success : TaskStatus.Failure;
            
            return result;
        }
    }
}
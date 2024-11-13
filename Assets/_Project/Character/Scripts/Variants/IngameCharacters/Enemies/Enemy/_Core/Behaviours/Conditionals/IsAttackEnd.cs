using BehaviorDesigner.Runtime.Tasks;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals
{
    public class IsAttackEnd : CustomConditional
    {
        public override void OnStart()
        {
            base.OnStart();
            master.IsAttackEnd = false;
        }

        public override TaskStatus OnUpdate()
        {
            return master.IsAttackEnd ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
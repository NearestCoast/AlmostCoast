
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals
{
    public class IsFightMoveEnd : CustomConditional
    {
        public override void OnStart()
        {
            base.OnStart();
            master.IsFightMoveEnd = false;
        }

        public override TaskStatus OnUpdate()
        {
            return master.IsFightMoveEnd ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
using BehaviorDesigner.Runtime.Tasks;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Composites
{
    [TaskCategory("Custom")]
    public class WeightedChild : Composite
    {
        // 각 행동의 가중치를 설정하는 변수
        public float weight = 1f;

        // 하위 노드의 상태 추적
        private TaskStatus childStatus = TaskStatus.Inactive;

        public override void OnStart()
        {
            // 시작할 때 하위 노드를 실행할 준비를 합니다.
            childStatus = TaskStatus.Inactive;
        }

        public override bool CanExecute()
        {
            // 첫 번째 자식 노드가 존재하고, 실행 중이거나 완료되지 않은 경우 실행 가능
            return childStatus == TaskStatus.Inactive || childStatus == TaskStatus.Running;
        }

        public override TaskStatus OnUpdate()
        {
            // 자식 노드가 없을 경우 바로 실패로 반환
            if (children.Count == 0)
            {
                return TaskStatus.Failure;
            }

            // 첫 번째 자식 노드를 실행하고 상태를 추적
            childStatus = children[0].OnUpdate();

            // 자식 노드가 완료되면 그 상태를 반환
            if (childStatus == TaskStatus.Success || childStatus == TaskStatus.Failure)
            {
                return childStatus;
            }

            // 완료되지 않았다면 Running 상태 유지
            return TaskStatus.Running;
        }

        public override void OnChildExecuted(TaskStatus childStatus)
        {
            // 자식 노드가 실행을 마쳤을 때 상태를 업데이트합니다.
            this.childStatus = childStatus;
        }

        public override void OnEnd()
        {
            // 상태 초기화
            childStatus = TaskStatus.Inactive;
        }
    }
}
using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections.Generic;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Composites
{
    [TaskCategory("Custom")]
    public class WeightedSelector : Composite
    {
        private int selectedIndex = -1;
        private TaskStatus childStatus = TaskStatus.Inactive;

        public override void OnStart()
        {
            // OnStart에서만 가중치 기반 선택을 한 번 수행
            if (selectedIndex == -1)
            {
                selectedIndex = SelectWeightedIndex();
            }
        }

        public override int CurrentChildIndex()
        {
            return selectedIndex;
        }

        public override bool CanExecute()
        {
            // 선택된 자식이 유효하고, 실행 중이거나 완료되지 않았으면 true 반환
            return selectedIndex != -1 && (childStatus == TaskStatus.Inactive || childStatus == TaskStatus.Running);
        }

        public override TaskStatus OnUpdate()
        {
            // 선택된 자식 노드를 실행하고 상태를 반환합니다.
            if (selectedIndex != -1 && selectedIndex < children.Count)
            {
                // 선택된 자식 노드의 상태를 업데이트
                childStatus = children[selectedIndex].OnUpdate();

                // 자식이 완료되면 Running을 반환하여 Repeater가 다시 실행할 수 있도록 설정
                if (childStatus == TaskStatus.Success || childStatus == TaskStatus.Failure)
                {
                    // 다음 반복에서 새로운 가중치 기반 선택을 위해 초기화
                    selectedIndex = -1;
                    childStatus = TaskStatus.Inactive;
                    return TaskStatus.Running;
                }

                return TaskStatus.Running;
            }

            // 선택된 자식이 없거나 오류가 있는 경우 Failure 반환
            return TaskStatus.Failure;
        }

        public override void OnChildExecuted(TaskStatus childStatus)
        {
            // 자식 노드가 실행을 마쳤을 때 상태를 업데이트합니다.
            this.childStatus = childStatus;
        }

        public override void OnEnd()
        {
            // 상태 초기화
            selectedIndex = -1;
            childStatus = TaskStatus.Inactive;
        }

        private int SelectWeightedIndex()
        {
            float totalWeight = 0;
            List<float> childWeights = new List<float>();

            // 자식 노드를 순회하며 각 가중치를 가져옵니다.
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i] as WeightedChild;
                if (child != null)
                {
                    float weight = child.weight;
                    childWeights.Add(weight);
                    totalWeight += weight;
                }
                else
                {
                    childWeights.Add(0); // 기본 가중치를 0으로 설정
                }
            }

            // 가중치 기반 무작위 선택
            float randomValue = Random.Range(0, totalWeight);
            float cumulativeWeight = 0;

            for (int i = 0; i < childWeights.Count; i++)
            {
                cumulativeWeight += childWeights[i];
                if (randomValue < cumulativeWeight)
                {
                    return i;
                }
            }

            // 기본적으로 마지막 인덱스 선택
            return childWeights.Count - 1;
        }
    }

}
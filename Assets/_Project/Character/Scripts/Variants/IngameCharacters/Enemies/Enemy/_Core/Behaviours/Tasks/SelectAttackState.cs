using System.Collections.Generic;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.ActionStates.MeleeAttacks;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Tasks
{
    public class SelectAttackState : CustomTask
    {
        public override void OnStart()
        {
            base.OnStart();

            var attackStateCandidates = new List<AttackState>();
            foreach (var actionState in master.ActionStateContainer.StateObjDict.Values)
            {
                if (!actionState.gameObject.activeSelf) continue;
                if (actionState is AttackState attackState)
                {
                    attackStateCandidates.Add(attackState);
                }
            }

            // 공격 상태 선택을 위한 가중치 합계 계산
            float totalWeight = 0f;
            foreach (var attackStateCandidate in attackStateCandidates)
            {
                totalWeight += attackStateCandidate.Weight;
            }

            // 첫 번째 공격 상태 선택
            float randomPoint1 = Random.Range(0, totalWeight);
            float currentWeight = 0f;
            AttackState selectedActionState = null;

            foreach (var attackStateCandidate in attackStateCandidates)
            {
                currentWeight += attackStateCandidate.Weight;
                if (randomPoint1 <= currentWeight)
                {
                    selectedActionState = attackStateCandidate;
                    break;
                }
            }

            // 첫 번째 선택된 공격 상태를 후보에서 제거
            attackStateCandidates.Remove(selectedActionState);

            // 남은 후보 중 MeleeAttack 타입만으로 가중치 계산
            float meleeTotalWeight = 0f;
            var meleeAttackCandidates = new List<AttackState>();
            foreach (var attackStateCandidate in attackStateCandidates)
            {
                if (attackStateCandidate is MeleeAttack)
                {
                    meleeTotalWeight += attackStateCandidate.Weight;
                    meleeAttackCandidates.Add(attackStateCandidate);
                }
            }

            // 두 번째 공격 상태 선택 (MeleeAttack 중에서 가중치 기반)
            float randomPoint2 = Random.Range(0, meleeTotalWeight);
            currentWeight = 0f;
            AttackState alternateActionState = null;

            foreach (var meleeAttackCandidate in meleeAttackCandidates)
            {
                currentWeight += meleeAttackCandidate.Weight;
                if (randomPoint2 <= currentWeight)
                {
                    alternateActionState = meleeAttackCandidate;
                    break;
                }
            }

            // 첫 번째 및 두 번째 선택된 공격 상태 설정
            if (selectedActionState != null)
            {
                master.PredictedAttackState = selectedActionState;
            }

            if (alternateActionState != null)
            {
                master.AlternateAttackState = alternateActionState;
            }
        }

        public override TaskStatus OnUpdate()
        {
            SetBothIdle();
            return TaskStatus.Running;
        }
    }
}

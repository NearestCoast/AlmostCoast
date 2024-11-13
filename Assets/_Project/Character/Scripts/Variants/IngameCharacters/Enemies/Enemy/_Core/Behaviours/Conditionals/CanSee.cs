using _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals;
using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Characters.IngameCharacters.Core;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace _Project.Character.IngameCharacters.Enemies.Behaviours.Conditionals
{
    public class CanSee : CustomConditional
    {
        private float fieldOfViewAngle = 45f; // AI의 시야각
        private float viewDistance = 10f; // AI의 시야 거리
        private float soundDistance = 10f; // AI의 시야 거리
        private PlayerCharacter targetCharacter;

        public override void OnAwake()
        {
            base.OnAwake();
            fieldOfViewAngle = master.Stat.SightAngle;
            viewDistance = master.Stat.ViewDistance;
            soundDistance = master.Stat.SoundDistance;
            targetCharacter = pathfinder.TargetCharacter;
            
            // Debug.Log("FOV: " + fieldOfViewAngle + ", ViewDistance: " + viewDistance);
        }

        public override TaskStatus OnUpdate()
        {
            if (master.IsAwarePlayer)
            {
                pathfinder.IsWandering = false;
                return TaskStatus.Success;
            }
            // 플레이어와의 거리 계산
            Vector3 directionToPlayer = targetCharacter.transform.position - transform.position;
            float distanceToPlayer = directionToPlayer.magnitude;

            // 플레이어가 시야 거리 내에 있는지 확인
            if (distanceToPlayer <= viewDistance)
            {
                // 시야각 내에 있는지 확인
                float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
                if (angleToPlayer <= fieldOfViewAngle / 2f)
                {
                    // 시야 내에 장애물 없이 플레이어가 보이는지 Raycast로 확인
                    if (!Physics.Raycast(transform.position, directionToPlayer.normalized, distanceToPlayer, surfaceLayers))
                    {
                        // 플레이어가 시야에 있는 경우 성공 반환
                        pathfinder.IsWandering = false;
                        master.IsAwarePlayer = true;
                        pathfinder.SetTargetToPlayer();
                        return TaskStatus.Success;
                    }
                }
                
                if (distanceToPlayer <= soundDistance)
                {
                    if (!targetCharacter.IsStealthMove)
                    {
                        pathfinder.IsWandering = false;
                        master.IsAwarePlayer = true;
                        pathfinder.SetTargetToPlayer();
                        return TaskStatus.Success;
                    }
                }
            }
           

            // 플레이어가 시야에 없는 경우 실패 반환
            return TaskStatus.Failure;
        }
    }
}
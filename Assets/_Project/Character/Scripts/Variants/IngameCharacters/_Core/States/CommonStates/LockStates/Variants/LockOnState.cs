using System.Collections.Generic;
using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Characters.IngameCharacters.Core.States.CommonStates.LockOnStates.Variants
{
    public class LockOnState : LockState
    {
        public override StateType Type => StateType.LockOn;
        public override bool CanEnterState
        {
            get
            {
                return !LockParams.IsLockingOn;
            }
        }

        private static Camera MainCamera => Camera.main;
        [SerializeField] private float lockOnRadius = 60f;
        [SerializeField] private float closeRangeThreshold = 10f; // 가까운 거리와 먼 거리의 기준
        [SerializeField] private UnityEvent onEscape;
        
        private LayerMask enemyLayer;
        
        private Transform CurrentLockOnTargetT => LockParams.LockOnTarget;
        private HashSet<Transform> selectedTargets = new HashSet<Transform>(); // 선택된 적들 저장
        private List<Transform> nearbyTargets = new List<Transform>(); // 가까운 적들 리스트
        private Transform PlayerCharacterT { get; set; }

        protected override void Awake()
        {
            base.Awake();

            PlayerCharacterT = FindAnyObjectByType<PlayerCharacter>().transform;
            enemyLayer = 1 << LayerMask.NameToLayer("Character");
        }

        private Collider[] targetColliders;
        public override void OnEnterState()
        {
            // Debug.Log("Success Lock On");
            base.OnEnterState();
            
            if (CurrentLockOnTargetT == null)
            {
                var lockOnTarget = FindBestTarget();
                LockParams.LockOn(lockOnTarget);
                if (!lockOnTarget)
                {
                    onEscape?.Invoke();
                    return;
                }
                
                if (isPlayer) moveCameraTarget.RecenterImmediate();
                
                
            }
            Transform FindBestTarget()
            {
                if (masterCharacter is not PlayerCharacter) return PlayerCharacterT;
                
                Physics.OverlapSphereNonAlloc(transform.position, lockOnRadius, targetColliders, enemyLayer);
                Transform bestTarget = null;
                float bestScore = Mathf.Infinity;

                foreach (var col in targetColliders)
                {
                    Transform enemy = col.transform;

                    Vector3 screenPoint = MainCamera.WorldToScreenPoint(enemy.position);
                    Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                    float screenDistance = Vector2.Distance(screenCenter, new Vector2(screenPoint.x, screenPoint.y));
                    float worldDistance = Vector3.Distance(transform.position, enemy.position);
                    float score = screenDistance + worldDistance;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = enemy;
                    }
                }

                return bestTarget;
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();
            nearbyTargets.Clear();
            selectedTargets.Clear();
            if (isPlayer) moveCameraTarget.CancelRecenter();
        }


        public override void SwitchTarget()
        {
            // 가까운 적 리스트를 갱신하여 새로운 적이 포함되도록 보장
            UpdateNearbyTargets();

            Transform FindNextTarget()
            {
                // 선택되지 않은 가까운 적이 있으면 우선 선택
                foreach (Transform target in nearbyTargets)
                {
                    if (!selectedTargets.Contains(target))
                    {
                        return target;
                    }
                }

                // 모든 가까운 적이 선택된 경우, 순환해서 가장 처음 순서로 돌아가 타겟팅
                return nearbyTargets.Count > 0 ? nearbyTargets[0] : FindClosestScreenTarget();
            }

            Transform FindClosestScreenTarget()
            {
                Transform closestScreenTarget = null;
                float bestScreenDistance = Mathf.Infinity;
                Physics.OverlapSphereNonAlloc(transform.position, lockOnRadius, targetColliders, enemyLayer);
                foreach (Collider col in targetColliders)
                {
                    Transform enemy = col.transform;
                    if (nearbyTargets.Contains(enemy)) continue;

                    Vector3 screenPoint = MainCamera.WorldToScreenPoint(enemy.position);
                    Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                    float screenDistance = Vector2.Distance(screenCenter, new Vector2(screenPoint.x, screenPoint.y));

                    if (screenDistance < bestScreenDistance)
                    {
                        bestScreenDistance = screenDistance;
                        closestScreenTarget = enemy;
                    }
                }

                return closestScreenTarget;
            }

            Transform newTarget = FindNextTarget();
            if (newTarget != null)
            {
                LockParams.LockOn(newTarget);
                selectedTargets.Add(newTarget);
            }

            void UpdateNearbyTargets()
            {
                
                Physics.OverlapSphereNonAlloc(transform.position, lockOnRadius, targetColliders, enemyLayer);
                
                HashSet<Transform> currentNearbyTargets = new HashSet<Transform>();
                float screenWidth75 = Screen.width * 0.75f;

                foreach (Collider col in targetColliders)
                {
                    Transform enemy = col.transform;
                    float worldDistance = Vector3.Distance(transform.position, enemy.position);

                    Vector3 screenPoint = MainCamera.WorldToScreenPoint(enemy.position);
                    float horizontalDistanceFromCenter = Mathf.Abs(screenPoint.x - (Screen.width / 2f));

                    // 가까운 거리 내에 있으며 화면 중심에서 가로로 75% 이내에 있을 때만 추가
                    if (worldDistance <= closeRangeThreshold && horizontalDistanceFromCenter <= screenWidth75 / 2f)
                    {
                        currentNearbyTargets.Add(enemy);

                        // 새로 범위 내에 들어온 적을 nearbyTargets에 추가
                        if (!nearbyTargets.Contains(enemy))
                        {
                            nearbyTargets.Add(enemy);
                            SortNearbyTargets();
                        }
                    }
                }

                // 기존 목록에서 범위를 벗어난 적 제거
                nearbyTargets.RemoveAll(enemy => !currentNearbyTargets.Contains(enemy));

                void SortNearbyTargets()
                {
                    nearbyTargets.Sort((a, b) =>
                    {
                        Vector3 aScreenPoint = MainCamera.WorldToScreenPoint(a.position);
                        Vector3 bScreenPoint = MainCamera.WorldToScreenPoint(b.position);
                        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                        float aDistance = Vector2.Distance(screenCenter, new Vector2(aScreenPoint.x, aScreenPoint.y));
                        float bDistance = Vector2.Distance(screenCenter, new Vector2(bScreenPoint.x, bScreenPoint.y));
                        return aDistance.CompareTo(bDistance);
                    });
                }
            }
        }

        
        [SerializeField, TitleGroup("CameraTargetUpdate")] private float camTargetMoveSpeed = 30;
        
        public override Vector3 CameraTargetUpdate()
        {
            if (LockParams.LockOnTarget)
            {
                var targetC = LockParams.LockOnTarget.GetComponent<_Core.Character>();
                if (targetC && targetC.IsDead)
                {
                    onEscape?.Invoke();
                    return Vector3.zero;
                }
            }
            
            if (isPlayer)
            {
                moveCameraTarget.ProvideLookAtDirection((LockParams.LockOnTarget.position - transform.position).XYZ3toX0Z3().normalized);
                moveCameraTarget.RecenterImmediate();
            }
            
            // 선택된 타겟이 없을 경우 이동 벡터를 0으로 반환
            if (CurrentLockOnTargetT == null)
            {
                return Vector3.zero;
            }

            // 캐릭터와 적 사이의 거리 계산
            float distanceToEnemy = Vector3.Distance(transform.position, CurrentLockOnTargetT.position);

            // 최대 거리 조건 설정
            float maxDistanceThreshold = 3f;
            Vector3 targetPosition;

            // 적이 멀리 있을 때와 가까이 있을 때 목표 지점 계산
            if (distanceToEnemy > maxDistanceThreshold)
            {
                // 적이 3f보다 멀리 있을 때는 캐릭터로부터 적 방향으로 1.5f 떨어진 지점
                targetPosition = transform.position + (CurrentLockOnTargetT.position - transform.position).normalized * 1.5f;
            }
            else
            {
                // 적이 3f 이내에 있을 때는 캐릭터와 적 사이의 30% 지점
                targetPosition = Vector3.Lerp(transform.position, CurrentLockOnTargetT.position, 0.3f);
            }

            // 목표 지점의 Y 좌표를 바닥에서 1.5f 위로 설정
            targetPosition.y = transform.position.y + 1.5f;

            // 현재 위치와 목표 위치 간의 방향 벡터 및 거리 계산
            Vector3 directionToTarget = targetPosition - moveCameraTarget.transform.position;
            float distanceToTarget = directionToTarget.magnitude;

            // 이동 속도 설정 및 감속 적용
            
            float smoothingFactor = Mathf.Clamp01(distanceToTarget / maxDistanceThreshold);
            
            moveCameraTarget.SetRotation(LockParams.LockOnTarget.position - transform.position);

            // 최종 이동 벡터 계산 및 반환, 목표 위치에 가까워질수록 감속하여 흔들림 방지
            return directionToTarget.normalized * (camTargetMoveSpeed * smoothingFactor * Time.deltaTime);
        }

        public override void UpdateLockOnMarker()
        {
            // Transform target = CurrentLockOnTargetT;
            //
            // if (target != null)
            // {
            //     Vector3 screenPoint = MainCamera.WorldToScreenPoint(target.position + Vector3.up * LockParams.MarkerHeight);
            //
            //     if (screenPoint.z > 0)
            //     {
            //         lockOnMarker.transform.position = screenPoint;
            //         lockOnMarker.enabled = true;
            //     }
            //     else
            //     {
            //         lockOnMarker.enabled = false; // 대상이 보이지 않으면 비활성화
            //     }
            // }
            // else
            // {
            //     lockOnMarker.enabled = false; // LockOn 대상이 없으면 비활성화
            // }    
        }
    }
}

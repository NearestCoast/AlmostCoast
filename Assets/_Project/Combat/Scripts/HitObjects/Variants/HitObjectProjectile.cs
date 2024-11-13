using System;
using _Project.Characters._Core.States;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Effect;
using Animancer;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace _Project.Combat.HitObjects
{
    public class HitObjectProjectile : HitObject
    {
        private enum TargetingType
        {
            NonTarget,
            Target,
        }
        private enum TrackingType
        {
            Straight, // 직선 경로
            Homing, // 타겟을 추적
        }

        private enum TrajectoryType
        {
            Linear, // 직선
            Parabolic, // 포물선
            Spiral, // 나선형
        }

        private enum CollisionType
        {
            None, // 목표까지 충돌 없이 이동
            Collidable // 중간에 충돌 감지 가능
        }
        [SerializeField, TitleGroup("Projectile")] private LayerMask targetLayer; // 타겟이 될 레이어
        [SerializeField, TitleGroup("Projectile")] private TargetingType targetingType; // 추적 옵션
        [SerializeField, TitleGroup("Projectile")] private TrackingType trackingType; // 추적 옵션
        [SerializeField, TitleGroup("Projectile")] private TrajectoryType trajectoryType; // Trajectory 옵션
        [SerializeField, TitleGroup("Projectile")] private CollisionType collisionType; // 충돌 여부 옵션

        [SerializeField, TitleGroup("Projectile")] private float speed = 30; // 투사체의 속도
        [SerializeField, TitleGroup("Projectile")] private int projectileCount = 1; // 발사할 투사체의 수
        [SerializeField, TitleGroup("Projectile")] private float spreadAngle = 30f; // 퍼짐 각도 (전체 각도)
        [SerializeField, TitleGroup("Projectile")] private float collisionRadius = 0.5f; // 충돌 감지 반경
        [SerializeField, TitleGroup("Projectile")] private GameObject spawnObj;

        [SerializeField, TitleGroup("Effects")] private VisualEffect collisionVFX;
        [SerializeField, TitleGroup("Effects")] private AudioSource collisionSFX;
        [SerializeField, TitleGroup("Effects")] private HitObject collisionHitObject;

        [SerializeField, TitleGroup("Parabolic Setting")] private float verticalHeight = 10f; // 수직 포물선의 최대 높이
        [SerializeField, TitleGroup("Parabolic Setting")] private float horizontalHeight = 10f; // 수평 포물선의 최대 높이
        [SerializeField, TitleGroup("Parabolic Setting")] private float flightDurationRate = 2f; // 목표 지점까지의 총 이동 시간

        public float FlightDuration { get; set; }

        public float VerticalHeight { get; set; }
        public float HorizontalHeight { get; set; }


        private ActionState actionState;
        [SerializeField, TitleGroup("StateEvent"), HideLabel, InlineProperty, PropertyOrder(10), PropertySpace(SpaceAfter = 20)]
        private StateEvent stateEvent = new StateEvent();

        [ShowInInspector] private AnimancerComponent animancer;

        [SerializeField, TitleGroup("Animation")]
        private ClipTransition anim;

        private AnimancerState AnimancerState { get; set; }
        private AnimancerEvent.Sequence Events;

#if UNITY_EDITOR
        private void OnValidate()
        {
            stateEvent.Initialize(gameObject);
            var collisionEffectHolder = GetComponentInChildren<CollisionEffectHolder>(true);
            collisionVFX = collisionEffectHolder.GetComponentInChildren<VisualEffect>(true);
            collisionSFX = collisionEffectHolder.GetComponentInChildren<AudioSource>(true);
            collisionHitObject = collisionEffectHolder.GetComponentInChildren<HitObject>(true);
        }
#endif

        private void Awake()
        {
            actionState = GetComponentInParent<ActionState>();
            
            animancer = GetComponentInChildren<AnimancerComponent>();
            Events = stateEvent.InitializeEvents();
        }

        private void Update()
        {
            if (IsInvoked)
            {
                FlyTrajectory();
            }
        }

        private float ActionRange => actionState.ActionRange;
        public override void Invoke()
        {
            InvokeMultipleProjectiles();

            void InvokeMultipleProjectiles()
            {
                for (int i = 0; i < projectileCount; i++)
                {
                    // 발사 각도 계산 (중앙 기준으로 균등하게 배치)
                    float angleOffset = projectileCount > 1
                        ? -spreadAngle / 2 + (spreadAngle / (projectileCount - 1)) * i
                        : 0; // 프로젝타일이 1개면 중앙에 위치

                    Vector3 rotatedDirection = Quaternion.Euler(0, angleOffset, 0) * Direction;

                    var projectileInstance = Instantiate(this, transform.position, Quaternion.LookRotation(rotatedDirection));
                    projectileInstance.gameObject.SetActive(true);

                    switch (targetingType)
                    {
                        case TargetingType.NonTarget:
                        {
                            projectileInstance.Initialize(StartPosition, transform.position + rotatedDirection * 10);
                            break;
                        }

                        case TargetingType.Target:
                        {
                            if (TargetCharacter)
                            {
                                projectileInstance.Initialize(StartPosition, TargetCharacter);
                            }
                            else
                            {
                                projectileInstance.Initialize(StartPosition, TargetPosition + rotatedDirection * 10);
                            }
                            break;
                        }
                    }
                    

                    
                    projectileInstance.Direction = rotatedDirection.normalized;
                    projectileInstance.IsInvoked = true;
                    projectileInstance.TravelTime = 0;
                    projectileInstance.PlayAnimation();
                }
            }
        }

        public void PlayAnimation()
        {
            if (animancer != null)
            {
                AnimancerState = animancer.Play(anim);

                if (AnimancerState is not null && AnimancerState.Events(this, out var events))
                {
                    events.AddRange(Events);
                }
            }
        }

        private Vector3 StartPosition { get; set; }
        private IngameCharacter TargetCharacter { get; set; }
        private Vector3 TargetPosition { get; set; }
        private Vector3 fixedTargetPosition;
        private Vector3 Direction { get; set; }
        private float DistanceFromStartToEnd => Vector3.Distance(TargetPosition, StartPosition);
        private float TravelTime { get; set; }
        private bool IsInvoked { get; set; }

        public void Initialize(Vector3 startPosition, Vector3 targetPosition)
        {
            StartPosition = startPosition;
            transform.position = StartPosition; // 위치 설정
            
            fixedTargetPosition = targetPosition;
            TargetPosition = fixedTargetPosition;
            
            Initialize();
        }

        public void Initialize(Vector3 startPosition, IngameCharacter targetT)
        {
            StartPosition = startPosition;
            transform.position = StartPosition; // 이미 올바르게 설정됨
            
            TargetCharacter = targetT;
            TargetPosition = TargetCharacter.transform.position + TargetCharacter.CharacterControllerEnveloper.Center;
            
            Initialize();
        }

        private void Initialize()
        {
            Direction = (TargetPosition - transform.position).normalized;
            FlightDuration = DistanceFromStartToEnd * flightDurationRate / 10;   
            
            VerticalHeight =  verticalHeight * DistanceFromStartToEnd / 10;
            HorizontalHeight = GetHorizontalHeight();
            
            return;
            float GetHorizontalHeight()
            {
                var rnd = Random.Range(0, 2);
                var value = horizontalHeight * DistanceFromStartToEnd / 10;
                return value * (rnd == 0 ? 1 : -1);
            }
        }

        private Vector3 lastPosition;
        private Vector3 lastDirection;

        protected virtual void FlyTrajectory()
        {
            switch (trackingType)
            {
                case TrackingType.Straight:
                    break;
                case TrackingType.Homing:
                    if (TargetCharacter)
                    {
                        Direction = (TargetCharacter.transform.position + TargetCharacter.CharacterControllerEnveloper.Center - transform.position).normalized;
                    }

                    break;
            }

            switch (trajectoryType)
            {
                case TrajectoryType.Linear:
                    MoveLinear();
                    break;
                case TrajectoryType.Parabolic:
                    FlyCombinedParabolic();
                    break;
                case TrajectoryType.Spiral:
                    SpiralMove();
                    break;
            }

            
            return;
            void MoveLinear()
            {
                // 남은 거리 계산
                float remainingDistance = Vector3.Distance(transform.position, TargetPosition);

                // 목표 지점에 충분히 근접했는지 확인
                if (remainingDistance <= 0.05f)
                {
                    transform.position = TargetPosition;
                    OnCollision();
                    return;
                }

                // 다음 위치 계산
                Vector3 nextPosition = Vector3.MoveTowards(transform.position, TargetPosition, speed * Time.deltaTime);

                // 충돌 검사 및 위치 업데이트
                if (collisionType == CollisionType.Collidable &&
                    CheckCollision(nextPosition, out Vector3 collisionPoint))
                {
                    transform.position = collisionPoint;
                    OnCollision();
                    return;
                }

                // 위치 업데이트
                transform.position = nextPosition;
            }


            void FlyCombinedParabolic()
            {
                TravelTime += Time.deltaTime;

                // 목표 지점까지의 비율 계산
                float t = TravelTime / FlightDuration;

                // 목표 지점에 도달했을 때
                if (t >= 1f)
                {
                    transform.position = TargetPosition;
                    OnCollision();
                    return;
                }

                // 수평 궤적 보간 (시작점에서 목표점까지 선형으로 이동)
                Vector3 horizontalPosition = Vector3.Lerp(StartPosition, TargetPosition, t);

                // 수직 및 수평 포물선 궤적 계산
                float verticalOffset = 4 * VerticalHeight * t * (1 - t);      // 수직 포물선 (위아래 곡선)
                float horizontalOffset = 4 * HorizontalHeight * t * (1 - t);   // 수평 포물선 (좌우 곡선)

                // 수평 오프셋을 기준으로 좌우 곡선을 적용하기 위해 Direction 벡터의 수직 방향을 기준으로 회전 적용
                Vector3 horizontalDirection = Vector3.Cross(Direction, Vector3.up).normalized;

                // 최종 위치 계산
                Vector3 nextPosition = new Vector3(
                    horizontalPosition.x + horizontalOffset * horizontalDirection.x,
                    Mathf.Lerp(StartPosition.y, TargetPosition.y, t) + verticalOffset,
                    horizontalPosition.z + horizontalOffset * horizontalDirection.z
                );

                // 목표 지점에 충분히 근접했는지 확인
                if (Vector3.Distance(nextPosition, TargetPosition) < 0.05f)
                {
                    transform.position = TargetPosition;
                    OnCollision();
                    return;
                }

                // 위치 업데이트
                transform.position = nextPosition;
            }


            void SpiralMove()
            {
                // 스파이럴 이동을 위한 각도 증가 속도 (속도와 타임스케일을 곱하여 회전 속도 조절)
                float angle = TravelTime * speed * 10; 

                // 스파이럴 중심 축의 오프셋 계산
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.5f;

                // 다음 위치 계산: 나선형 오프셋을 Direction과 합산하여 이동
                Vector3 nextPosition = transform.position + (Direction + offset) * (speed * Time.deltaTime);

                // 목표에 도달했는지 확인
                float remainingDistance = Vector3.Distance(transform.position, TargetPosition);
                if (remainingDistance <= 0.05f)
                {
                    transform.position = TargetPosition;
                    OnCollision();
                    return;
                }

                // 충돌이 가능하도록 설정된 경우 충돌 체크
                if (collisionType == CollisionType.Collidable && CheckCollision(nextPosition, out Vector3 collisionPoint))
                {
                    transform.position = collisionPoint; // 충돌 지점에서 정지
                    OnCollision();
                    return;
                }

                // 위치 업데이트
                transform.position = nextPosition;
            }




            bool CheckCollision(Vector3 nextPosition, out Vector3 collisionPoint)
            {
                RaycastHit hit;
                Vector3 direction = (nextPosition - transform.position).normalized;

                if (Physics.SphereCast(transform.position, collisionRadius, direction, out hit,
                        (nextPosition - transform.position).magnitude, targetLayer))
                {
                    collisionPoint = hit.point;
                    return true;
                }

                collisionPoint = Vector3.zero;
                return false;
            }

        }

        private bool IsCollided { get; set; }

        private void OnCollision()
        {
            if (IsCollided) return;
            if (collisionVFX) collisionVFX.gameObject.SetActive(true);
            collisionSFX?.Play();
            collisionHitObject?.Invoke();
            IsCollided = true;
            AnimancerState.Stop();
            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var skinnedMeshRenderer in renderers)
            {
                skinnedMeshRenderer.enabled = false;
            }
            

            if (spawnObj)
            {
                var spawnObjInstance = Instantiate(spawnObj, transform.position, transform.rotation);
            }
            
            Wait().Forget();

            async UniTaskVoid Wait()
            {
                await UniTask.Delay(TimeSpan.FromSeconds(10));
                Destroy(gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            for (int i = 0; i < projectileCount; i++)
            {
                // 발사 각도를 중앙 기준으로 균등하게 계산
                float angleOffset;
                if (spreadAngle == 360f && projectileCount > 1)
                {
                    angleOffset = (360f / projectileCount) * i; // 360도일 경우 겹치지 않도록 projectileCount로 나눔
                }
                else
                {
                    angleOffset = projectileCount > 1
                        ? -spreadAngle / 2 + (spreadAngle / (projectileCount - 1)) * i
                        : 0; // 프로젝타일이 1개면 중앙에 위치
                }

                // Direction이 초기화되지 않은 경우 transform.forward를 기본값으로 사용
                Vector3 direction = Direction == Vector3.zero ? transform.forward : Direction;
                Vector3 rotatedDirection = Quaternion.Euler(0, angleOffset, 0) * direction;
                Vector3 currentPosition = transform.position;

                // 예상 경로에 구를 일정 간격으로 배치
                for (int j = 0; j < 10; j++) // 경로 상의 구 개수
                {
                    Gizmos.DrawSphere(currentPosition, 0.1f); // 구의 위치와 크기
                    currentPosition += rotatedDirection * 0.5f; // 구의 간격
                }
            }
        }
    }
}
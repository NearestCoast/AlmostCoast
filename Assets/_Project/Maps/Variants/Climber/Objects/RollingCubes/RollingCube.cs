using System;
using System.Threading;
using _Project.Characters.IngameCharacters.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Maps.Climber.Objects
{
    public partial class RollingCube : MonoBehaviour
    {
        [SerializeField] private Level level;

        public Level Level
        {
            get => level;
            set => level = value;
        }
        
        [SerializeField] private string levelID;

        public string LevelID
        {
            get => levelID;
            set => levelID = value;
        }
        
        [SerializeField] private string id;

        public string ID
        {
            get => id;
            set => id = value;
        }
    }

    public partial class RollingCube : MonoBehaviour
    {

        protected bool IsPlayerOnPlatform { get; private set; }
        private async void OnTriggerStay(Collider other)
        {
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                // if (IsPlayerOnPlatform) return;
        
                // 1 프레임 대기
                // await UniTask.Yield();

                var playerCharacter = other.attachedRigidbody.GetComponent<IngameCharacter>();
                playerCharacter.CurrentRollingCube = this;
                IsPlayerOnPlatform = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                var playerCharacter = other.attachedRigidbody.GetComponent<IngameCharacter>();
                playerCharacter.CurrentRollingCube = null;
                IsPlayerOnPlatform = false;
            }
        }

        // private void OnDestroy()
        // {
        //     ResetCube();
        // }
    }

    public partial class RollingCube : MonoBehaviour
    {
        [SerializeField] protected bool inverse;
        [SerializeField] protected float rotationDuration = 3; // 회전 완료 시간 (초)
        [SerializeField] protected  float delayBetweenRotations = 1; // 회전 사이의 딜레이 (초)
        protected  float rotationAmount = 90f; // 회전 각도 (90도씩)

        public float RotationDuration
        {
            get => rotationDuration;
            set => rotationDuration = value;
        }

        public bool Inverse
        {
            get => inverse;
            set => inverse = value;
        }

        // 회전을 시작하는 비동기 함수
        protected Vector3[] possibleRotations = new Vector3[]
        {
            // Vector3.zero,
            new Vector3(0f, 0f, 1), // Z축 방향 회전
            new Vector3(1, 0f, 0f), // X축 방향 회전
            // new Vector3(-1, 0f, 0f),  // X축 반대 방향 회전
            // new Vector3(0f, 1, 0f), // Y축 방향 회전
            // new Vector3(0f, -1, 0f), // Y축 반대 방향 회전
            
            // new Vector3(0f, 0f, -1),  // Z축 반대 방향 회전
        };

        protected Quaternion StartRotation { get; set; }
        protected Quaternion PrevRotation { get; set; }
        protected Quaternion TargetRotation { get; set; }

        protected Quaternion initialRotation; // 처음 회전 상태 저장

        protected bool IsFirstTime { get; set; }


        protected float elapsedTime;
        protected float waitingTime;
        private int currentIndex;
        private bool PrevIsPlayerOnPlatform { get; set; }
        
        public Vector3 Velocity { get; protected set; }
        public Quaternion RotationChange { get; protected set; }

        protected virtual void Awake()
        {
            initialRotation = transform.rotation;
            StartRotation = transform.rotation;
            // elapsedTime = rotationDuration;
            currentIndex = 0;
            TargetRotation = Quaternion.AngleAxis(rotationAmount, possibleRotations[currentIndex] * (inverse ? -1 : 1)) * StartRotation;
        }
        
        public virtual void StartWorking()
        {
            IsFirstTime = true;

            Velocity = Vector3.zero;
        }


        public virtual void ResetCube()
        {
            // playerCharacter.CurrentRollingCube = null;
            IsPlayerOnPlatform = false;
            
            
            transform.rotation = initialRotation;

            elapsedTime = 0;
            // waitingTime = delayBetweenRotations;
            currentIndex = 0;
            StartRotation = transform.rotation;
            TargetRotation = Quaternion.AngleAxis(rotationAmount, possibleRotations[currentIndex] * (inverse ? -1 : 1)) * StartRotation;
        }

        public virtual void Work(Transform playerT)
        {
            Velocity = Vector3.zero;

            if (waitingTime >= delayBetweenRotations)
            {
                if (elapsedTime < rotationDuration)
                {
                    // 이전 회전값 저장
                    var prevRotation = transform.rotation;

                    // 회전 시간 갱신
                    elapsedTime += Time.deltaTime;
                    var t = Mathf.SmoothStep(0f, 1f, elapsedTime / rotationDuration);

                    // 현재 회전 상태 계산 (부드러운 회전)
                    Quaternion currentRotation = Quaternion.Slerp(StartRotation, TargetRotation, t);

                    // 플레이어의 현재 상대적 위치 (회전 시작 전)
                    Vector3 initialPlayerOffset = playerT.position - transform.position;

                    // 큐브 회전 적용 (글로벌 회전 축 적용)
                    transform.rotation = currentRotation;

                    // 이전 프레임과 비교하여 플레이어 위치 변화 계산
                    Vector3 newPlayerPosition = transform.position +
                                                (transform.rotation * Quaternion.Inverse(prevRotation) *
                                                 initialPlayerOffset);
                    Vector3 playerMovement = newPlayerPosition - playerT.position;
                    
                    // Debug.Log(currentIndex);

                    // 이동할 Velocity (플레이어의 위치 변화량)
                    Velocity = playerMovement;

                    RotationChange = currentRotation * Quaternion.Inverse(prevRotation);
                }
                else
                {
                    // 회전 종료 후 새로운 회전 설정
                    transform.rotation = TargetRotation;
                    StartRotation = transform.rotation;

                    currentIndex = currentIndex == 0 ? 1 : 0;
                    // 글로벌 좌표계에서의 회전 적용
                    Vector3 randomAxis = possibleRotations[currentIndex] * (inverse ? -1 : 1);

                    // Debug.Log(currentIndex);

                    // 글로벌 회전 기준으로 회전 (글로벌 좌표계 축 사용)
                    TargetRotation = Quaternion.AngleAxis(rotationAmount, randomAxis) * StartRotation;

                    elapsedTime = 0;
                    // waitingTime = IsFirstTime ? delayBetweenRotations : 0;
                }
            }
            else
            {
                waitingTime += Time.deltaTime;
            }
        }
    }
}
using System.Collections.Generic;
using _Project.Characters._Core.States.AnimationStates;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace _Project.InputSystem
{
    public class AttackInputBuffer : MonoBehaviour
    {
        [SerializeField] private InputActionReference attackActionRef; // 공격 액션 참조
        [SerializeField] private List<AttackState> attackStates; // 공격 상태 목록
        [SerializeField] private float bufferPossibleRemainingTime = 1f; // 예약 가능한 남은 시간 임계값
        [SerializeField] private float attackTerm = 1f; // 공격 텀
        [SerializeField] private UnityEvent onIdleState; // Idle 상태 이벤트
        [SerializeField] private float inputIdleTimeout = 0.5f; // 입력이 없을 경우 리셋되는 시간

        private AnimationStateConductor AnimationStateConductor { get; set; }
        [ShowInInspector] private int currentAttackIndex = 0; // 현재 공격 상태 인덱스
        private float inputIdleTimer = 0f; // 입력이 없을 때의 시간 경과
        private float attackHoldTime = 0f; // 공격 버튼 홀드 시간

        private void Awake()
        {
            AnimationStateConductor = GetComponentInParent<AnimationStateConductor>();
            onUpdate?.Invoke(0);
            onPerformed?.Invoke();
        }

        private void OnEnable()
        {
            if (attackActionRef != null)
            {
                attackActionRef.action.Enable();
                attackActionRef.action.started += OnAttackStarted; // 공격 입력 시작 이벤트 연결
                attackActionRef.action.performed += OnAttackPerformed;
            }
        }

        private void OnDisable()
        {
            if (attackActionRef != null)
            {
                attackActionRef.action.started -= OnAttackStarted; // 공격 입력 시작 이벤트 해제
                attackActionRef.action.performed -= OnAttackPerformed;
                attackActionRef.action.Disable();
            }
        }

        [SerializeField] private UnityEvent onStarted; // 공격 입력 시작 이벤트
        [SerializeField] private UnityEvent<float> onUpdate; // 공격 진행 비율 이벤트
        [SerializeField] private UnityEvent onPerformed; // 공격 입력 수행 이벤트

        private void Update()
        {
            if (IsBuffered) ExecuteAttack();
            else onIdleState?.Invoke();

            inputIdleTimer += Time.deltaTime;
            if (inputIdleTimer >= inputIdleTimeout)
            {
                ResetBuffer();
            }

            attackTermTime += Time.deltaTime;

            // 버튼이 눌려 있는 동안 duration 값을 지속적으로 전달
            if (attackActionRef.action.IsPressed())
            {
                attackHoldTime += Time.deltaTime; // 홀드 시간 증가
                onUpdate?.Invoke(Mathf.Clamp01(attackHoldTime / demandedDuration)); // 비율 전달
            }
            else
            {
                attackHoldTime = 0f; // 버튼을 놓으면 초기화
            }
        }

        [SerializeField] private float demandedDuration = 0;

        // 공격 입력이 시작될 때 호출되는 함수
        private void OnAttackStarted(InputAction.CallbackContext context)
        {
            onStarted?.Invoke(); // onStarted 이벤트 호출
        }

        // 공격 입력이 발생했을 때 호출되는 함수
        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            onPerformed?.Invoke();
            if (attackTermTime < attackTerm) return;  
            if (context.duration < demandedDuration) return;
            
            var currentActionState = AnimationStateConductor.CurrentActionState as ActionState;

            // 입력이 발생했으므로 타이머 리셋
            inputIdleTimer = 0f;

            // 공격이 진행 중이 아니면 첫 번째 공격 실행
            if (currentActionState is ActionIdleState)
            {
                QueueNextAttack();
            }
            // 현재 공격이 진행 중일 때만 예약 가능 여부 확인
            else if (currentActionState is AttackState && currentActionState.RemainingDuration <= bufferPossibleRemainingTime)
            {
                QueueNextAttack();
            }
        }

        [ShowInInspector] private bool IsBuffered { get; set; }

        private void QueueNextAttack()
        {
            IsBuffered = true;
        }

        // 공격 실행
        private void ExecuteAttack()
        {
            if (attackTermTime < attackTerm) return;
            if (attackStates.Count == 0) return;

            var attackState = attackStates[currentAttackIndex % attackStates.Count];
            AnimationStateConductor.TrySetActionState(attackState);

            var currentAttackState = AnimationStateConductor.CurrentActionState as AttackState;
            if (currentAttackState == attackState)
            {
                if (currentAttackIndex % attackStates.Count == attackStates.Count - 1)
                {
                    attackTermTime = 0;
                }
                else currentAttackIndex++;

                IsBuffered = false;
            }

            onUpdate?.Invoke(0);
        }

        private float attackTermTime;

        // 버퍼 및 공격 상태 초기화
        public void ResetBuffer()
        {
            currentAttackIndex = 0;
            IsBuffered = false;
        }
    }
}
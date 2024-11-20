using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.ActionStates
{
    public class AttackState : ActionLayerClipActionState
    {
        protected override void Awake()
        {
            base.Awake();
            if (masterCharacter is PlayerCharacter) enemyLayer = 1 << LayerMask.NameToLayer("Character");
            else enemyLayer = 1 << LayerMask.NameToLayer("Player");
            PlayerCharacterT = FindAnyObjectByType<PlayerCharacter>().transform;
        }

        public override bool CanEnterState
        {
            get 
            {
                var value = AnimationStateConductor.CurrentMovementState switch
                {
                    IdleState => true,
                    MoveState => true,
                    CautiousMoveState => true,
                    WanderingMoveState => true,
                    JumpState => true,
                    SlideDashState => true,
                    AirDashState => true,
                    _ => false,
                };

                return value;
            }
        }

        public override bool CanExitState
        {
            get
            {
                if (NextState.Type == StateType.Flinched) return true;
                if (NextState.Type == StateType.KnockBacked) return true;
                
                return base.CanExitState;
            }   
        }

        [SerializeField, TitleGroup("SearchEnemy")] private float searchAngle = 45f; // 탐색 각도
        private LayerMask enemyLayer; // 적 레이어

        protected IngameCharacter ClosestEnemy { get; set; }
        private Transform PlayerCharacterT { get; set; }
        
        public override void OnEnterState()
        {
            base.OnEnterState();
            MoveParams.ResetWallJumpCount();
            SearchEnemies();
            
            
            return;
            void SearchEnemies()
            {
                var inputDirection = inputChecker.HorizontalDirection3;
                Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, ActionRange, enemyLayer);
                
                IngameCharacter closestEnemy = null;
                float closestDistance = float.MaxValue;

                foreach (var enemyCollider in enemiesInRange)
                {
                    IngameCharacter targetCharacter;
                    if (enemyCollider.attachedRigidbody)
                    {
                        targetCharacter = enemyCollider.attachedRigidbody.GetComponent<IngameCharacter>();
                    }
                    else targetCharacter = enemyCollider.GetComponent<IngameCharacter>();
                    
                    // Enemy 컴포넌트가 있는지 확인
                    if (targetCharacter == null) continue;

                    Vector3 directionToEnemy = (targetCharacter.transform.position - transform.position).normalized;
                    float angleToEnemy = Vector3.Angle(inputDirection, directionToEnemy);

                    // 각도와 거리 제한을 만족하는 적만 선택
                    float distanceToEnemy = Vector3.Distance(transform.position, targetCharacter.transform.position);
                    if (angleToEnemy <= searchAngle && distanceToEnemy <= ActionRange)
                    {
                        // 가장 가까운 적 찾기
                        if (distanceToEnemy < closestDistance)
                        {
                            closestDistance = distanceToEnemy;
                            closestEnemy = targetCharacter;
                        }
                    }
                }

                ClosestEnemy = closestEnemy;
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();
            ClosestEnemy = null;
        }
    }
}

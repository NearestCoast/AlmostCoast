using System;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using BehaviorDesigner.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using _Project.UI.InGame;
using Cysharp.Threading.Tasks;
using Renge.PPB;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace _Project.Character.IngameCharacters.Enemies
{
    public partial class Enemy : IngameCharacter
    {
        // [SerializeField] private string specificID;
        [SerializeField, TitleGroup("BehaviourParams")] private float sightAngle = 120;
        [SerializeField, TitleGroup("BehaviourParams")] private float viewDistance = 60;
        [SerializeField, TitleGroup("BehaviourParams")] private float soundDistance = 10;
        
        [SerializeField, TitleGroup("BehaviourParams")] private float isInFightDistance = 8;
        
        [SerializeField, TitleGroup("BehaviourParams")] private float maxWaitingTime = 60;
         
        public float SightAngle => sightAngle;
        public float ViewDistance => viewDistance;
        public float SoundDistance => soundDistance;
        
        public float IsInFightDistance => isInFightDistance;
        public float MaxWaitingTime => maxWaitingTime;
    }

    public partial class Enemy : IngameCharacter
    {
        private Behavior behavior;

        private EnemyWorldHealthBar enemyWorldHealthBar;
        
        private Vector3 StartPosition { get; set; }
        
        public bool IsAwarePlayer { get; set; }
        public bool IsFightMoveEnd { get; set; }
        public bool IsAttackEnd { get; set; }
        public bool IsInAttackPhase { get; set; }
        public bool IsJustEncountered { get; set; } = true;
        public AttackState PredictedAttackState { get; set; }
        public AttackState AlternateAttackState { get; set; }

        // [SerializeField, TitleGroup("Velocity")] private float exceptionalMove = 20;
        protected override Vector3 Velocity
        {
            get
            { 
                var velocity = CurrentActionState.Type == ActionState.StateType.ActionIdle
                    ? CurrentMovementState.Velocity
                    : CurrentActionState.GetVelocity();
                velocity += CurrentActionState.Acc * Time.deltaTime;
            
                velocity += CurrentMovingPlatform ? CurrentMovingPlatform.Velocity : Vector3.zero;
                velocity += CurrentRollingCube ? CurrentRollingCube.Velocity : Vector3.zero;
                velocity += MoveParams.Acceleration * Time.deltaTime;
                
                return velocity;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            behavior = GetComponentInChildren<Behavior>(true);
            behavior.StartWhenEnabled = false;
            enemyWorldHealthBar = GetComponentInChildren<EnemyWorldHealthBar>();
        }

        protected override void Update()
        {
            base.Update();
            enemyWorldHealthBar?.UpdateHealthBarPosition(transform.position + Vector3.up * (characterControllerEnveloper.Height + 1), IsAwarePlayer);
        }

        public override void MoveToSavePoint()
        {
            if (IsDead) return;
            CharacterControllerEnveloper.OnSpawn();
            
            gameObject.SetActive(false);
            transform.position = SavePoint ? SavePoint.transform.position : StartPosition;
            gameObject.SetActive(true);
            
            base.MoveToSavePoint();
        }

        protected override void SetDying()
        {
            base.SetDying();
            behavior.DisableBehavior();
            enemyWorldHealthBar?.Close();
        }

        [SerializeField] private GameObject lootObjPrefab;

        public GameObject LootObj
        {
            get => lootObjPrefab;
            set => lootObjPrefab = value;
        }
        
        public override void Die()
        {
            base.Die();
            
            WaitAndRespawn().Forget();
            
            return;
            async UniTaskVoid WaitAndRespawn()
            {
                if (lootObjPrefab)
                {
                    var lootObj = Instantiate(lootObjPrefab, StartPosition, quaternion.identity);
                    lootObj.SetActive(true);
                }
                
                await UniTask.Delay(TimeSpan.FromSeconds(4));
                enabled = false;
            }
        }

        private bool IsEnabled { get; set; }
        public void StartBehaviour()
        {
            // Debug.Log(transform.parent.name + " " + IsEnabled);
            if (IsEnabled) return;
            behavior.EnableBehavior();
            StartPosition = transform.position;
            IsEnabled = true;
        }

        public void ReturnToStartPosition()
        {
            // Debug.Log(transform.parent.name + "Disable ");
            IsEnabled = false;
            behavior.DisableBehavior();
            
            base.MoveToSavePoint();
            
            transform.position = StartPosition;

            IsJustEncountered = true;
        }
    }
}
using System;
using _Project.Characters.IngameCharacters.Core;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using BehaviorDesigner.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.UI.InGame;
using Cysharp.Threading.Tasks;
using Renge.PPB;

namespace _Project.Character.IngameCharacters.Enemies
{
    public class Enemy : IngameCharacter
    {
        private Behavior behavior;
        
        private WorldHealthBar worldHealthBar;
        private ProceduralProgressBar progressBar;
        
        private Vector3 StartPosition { get; set; }
        
        public bool IsAwarePlayer { get; set; }
        public bool IsFightMoveEnd { get; set; }
        public bool IsAttackEnd { get; set; }
        public bool IsInAttackPhase { get; set; }
        public bool IsJustEncountered { get; set; } = true;
        public AttackState PredictedAttackState { get; set; }
        public AttackState AlternateAttackState { get; set; }

        [SerializeField, TitleGroup("Velocity")] private float exceptionalMove = 20;
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
            worldHealthBar = GetComponentInChildren<WorldHealthBar>();
            progressBar = GetComponentInChildren<ProceduralProgressBar>(true);
            
            Stat.OnHealthChange?.AddListener((value)=> progressBar.Value = value);
        }

        protected override void Update()
        {
            base.Update();
            worldHealthBar?.UpdateHealthBarPosition(transform.position + Vector3.up * (characterControllerEnveloper.Height + 1), IsAwarePlayer);
        }

        public override void MoveToSavePoint()
        {
            if (IsDead) return;
            transform.position = SavePoint ? SavePoint.transform.position : StartPosition;
            base.MoveToSavePoint();
        }

        public override void ProgressDying()
        {
            base.ProgressDying();
            behavior.DisableBehavior();
            worldHealthBar?.Close();
        }

        public override void Die()
        {
            base.Die();
            
            WaitAndRespawn().Forget();
            
            return;
            async UniTaskVoid WaitAndRespawn()
            {
                await UniTask.Delay(TimeSpan.FromSeconds(4));
                enabled = false;
            }
        }

        private bool IsEnabled { get; set; }
        public void StartBehaviour()
        {
            if (IsEnabled) return;
            behavior.EnableBehavior();
            StartPosition = transform.position;
            IsEnabled = true;
        }

        public void ReturnToStartPosition()
        {
            IsEnabled = false;
            behavior.DisableBehavior();
            
            base.MoveToSavePoint();
            
            transform.position = StartPosition;

            IsJustEncountered = true;
        }
    }
}
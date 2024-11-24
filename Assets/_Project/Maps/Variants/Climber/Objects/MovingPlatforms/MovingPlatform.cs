using System;
using _Project.Characters.IngameCharacters.Core;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Maps.Climber.Objects
{
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Level level;

        public Level Level
        {
            get => level;
            set => level = value;
        }
        
        [SerializeField] private string id;
        [SerializeField] private Vector3 targetPosition;

        public string ID
        {
            get => id;
            set => id = value;
        }

        public Vector3 TargetPosition
        {
            get => targetPosition;
            set => targetPosition = value;
        }

        protected Vector3 StartPosition { get; set; }
        protected float Length { get; set; }
        
        private void Awake()
        {
            StartPosition = transform.position;
            Length = Vector3.Distance(StartPosition, TargetPosition);
            isGoDir = true;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            finishSoundSource = GetComponent<AudioSource>();
        }
#endif
        
        [SerializeField] protected AudioSource finishSoundSource;
        [SerializeField] protected AudioSource progressingSoundSource;

        public AudioSource FinishAudioSource
        {
            get => finishSoundSource;
            set => finishSoundSource = value;
        }

        public AudioSource ProgressingSoundSource
        {
            get => progressingSoundSource;
            set => progressingSoundSource = value;
        }

        
        public Vector3 Velocity { get; protected set; }
        public Vector3 Acc { get; protected set; }
        
        protected bool isGoDir = true;
        protected bool isWorking = false;
        protected float GoStartTime { get; set; }
        protected float GoEndTime { get; set; }

        public virtual void MoveStart()
        {
            GoStartTime = 0;
        }
        
        public virtual void ResetMovingPlatform()
        {
            GoStartTime = 0;
            GoEndTime = 0;
            isGoDir = true;
            isWorking = false;
            
            gameObject.SetActive(false);
            transform.position = StartPosition;
            gameObject.SetActive(true);
        }

        public virtual void Move()
        {   
            
        }
        
        
        protected bool IsPlayerOnPlatform { get; private set; }
        private void OnTriggerStay(Collider other)
        {
            if (IsPlayerOnPlatform) return;
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                var playerCharacter = other.attachedRigidbody.GetComponent<IngameCharacter>();
                playerCharacter.CurrentMovingPlatform = this;
                isWorking = true;
                IsPlayerOnPlatform = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                var playerCharacter = other.attachedRigidbody.GetComponent<IngameCharacter>();
                playerCharacter.CurrentMovingPlatform = null;
                playerCharacter.MoveParams.Acceleration = Acc;
                IsPlayerOnPlatform = false;
            }
        }
    }
}
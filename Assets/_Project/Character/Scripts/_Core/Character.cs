using System;
using _Project._Core;
using _Project.Characters._Core.Input;
using _Project.Combat.HitObjects;
using Sirenix.OdinInspector;
using UnityEngine;

using UnityEngine.Events;
using UnityEngine.Serialization;

namespace _Project.Characters._Core
{
    [Serializable] 
    public class Statistic
    {
        [SerializeField, TitleGroup("HealthParams")] private int maxHealth = 100;
        [SerializeField, TitleGroup("HealthParams")] private int currentHealth = 100;
        public int MaxHealth => maxHealth;
        public int CurrentHealth
        {
            get => currentHealth;
            set
            {
                currentHealth = value;
                onHealthChange?.Invoke(HealthPerMax);
            }
        }

        public float HealthPerMax => (float)currentHealth / maxHealth;

        private UnityEvent<float> onHealthChange = new UnityEvent<float>();
        private UnityEvent onDie = new UnityEvent();

        public UnityEvent<float> OnHealthChange => onHealthChange;
        public UnityEvent OnDie => onDie;

        public void OnStart()
        {
            onHealthChange?.Invoke(HealthPerMax);
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth -= damage;
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                onDie?.Invoke();
            }
        }

        [SerializeField, TitleGroup("BehaviourParams")] private float awareAreaRadius = 60;
        
        [SerializeField, TitleGroup("BehaviourParams")] private float sightAngle = 120;
        [SerializeField, TitleGroup("BehaviourParams")] private float viewDistance = 60;
        [SerializeField, TitleGroup("BehaviourParams")] private float soundDistance = 10;
        
        [SerializeField, TitleGroup("BehaviourParams")] private float isInFightDistance = 8;
        
        [SerializeField, TitleGroup("BehaviourParams")] private float maxWaitingTime = 60;

        public float AwareAreaRadius => awareAreaRadius;
        
        public float SightAngle => sightAngle;
        public float ViewDistance => viewDistance;
        public float SoundDistance => soundDistance;
        
        public float IsInFightDistance => isInFightDistance;
        
        public float MaxWaitingTime => maxWaitingTime;
    }
    
    public class Character : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private Statistic stat;
        public Statistic Stat => stat;
        public bool IsDying { get; protected set; }
        public bool IsDead { get; protected set; }
        
        protected virtual void Awake()
        {
            Stat.OnDie.AddListener(ProgressDying);
        }

        protected virtual void Start()
        {
            stat.OnStart();
        }

        public virtual void ProgressDying()
        {
            
        }

        public virtual void TakeDamage(HittingInfo hittingInfo, int damage, SideEffect sideEffect = SideEffect.None)
        {
            stat.TakeDamage(damage);
        }

        public virtual void Die()
        {
            IsDead = true;
        }
    }
}
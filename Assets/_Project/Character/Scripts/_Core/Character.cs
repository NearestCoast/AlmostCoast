using System;
using _Project._Core;
using _Project.Characters._Core.Input;
using _Project.Combat.HitObjects;
using _Project.UI.InGame;
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
    }
    
    public class Character : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private Statistic stat;
        protected Statistic Stat => stat;
        
        private HealthBar healthBar;
        
        public bool IsDying { get; protected set; }
        public bool IsDead { get; protected set; }
        
        protected virtual void Awake()
        {
            healthBar = GetComponentInChildren<HealthBar>(true);
        }

        protected virtual void Start()
        {
            Stat.OnDie.AddListener(SetDying);
            Stat.OnHealthChange?.AddListener((value)=> healthBar.ProceduralProgressBar.Value = value);
            Stat.OnStart();
        }

        public virtual void SetDying()
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
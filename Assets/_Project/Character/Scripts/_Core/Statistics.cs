using System;
using _Project.Managers.Scripts._Core.SaveManager;
using _Project.UI.InGame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Characters._Core
{
    public class Statistics : MonoBehaviour, ISavable
    {
        [SerializeField] private bool isPlayer;
        private HealthBar healthBar;

        private void Awake()
        {
            healthBar = GetComponentInChildren<HealthBar>(true);
        }

        private void Start()
        {
            onHealthChange?.AddListener((value)=> healthBar.ProceduralProgressBar.Value = value);
            onHealthChange?.Invoke(HealthPerMax);
        }

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
        
        public UnityEvent OnDie => onDie;

        public void TakeDamage(int damage)
        {
            CurrentHealth -= damage;
            if (CurrentHealth <= 0)
            {
                currentHealth = maxHealth;
                onDie?.Invoke();
            }
        }

        public bool EnrollToSaveManager => isPlayer;
        public bool Save(string saveFileName)
        {
            ISavable.EasySave("Player_Statistics_CurrentHealth", currentHealth, saveFileName);
            ISavable.EasySave("Player_Statistics_MaxHealth", maxHealth, saveFileName);
            return true;
        }

        public bool Load(string saveFileName)
        {
            currentHealth = ISavable.EasyLoad<int>("Player_Statistics_CurrentHealth", saveFileName);
            maxHealth = ISavable.EasyLoad<int>("Player_Statistics_MaxHealth", saveFileName);
            return true;
        }
    }
}
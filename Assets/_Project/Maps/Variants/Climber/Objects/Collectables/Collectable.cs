using System;
using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Managers.Scripts._Core.AudioManager;
using _Project.Managers.Scripts._Core.SaveManager;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class Collectable : MonoBehaviour, ISavable
    {
        [SerializeField] private string id;

        public string ID
        {
            get => id;
            set => id = value;
        }
        
        private bool isWorked;
        
        protected PlayerCharacter playerCharacter;
        private void Awake()
        {
            playerCharacter = FindAnyObjectByType<PlayerCharacter>();
        }

        private void OnEnable()
        {
            if (isWorked) gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isWorked) return;
            if (!other.attachedRigidbody) return;
            if (other.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Work();
            }
        }

        protected virtual void Work()
        {
            isWorked = true;
            
            Time.timeScale = 0;
            FindAnyObjectByType<AudioManager>().SetClip(audioSource.clip);
            FindAnyObjectByType<AudioManager>().Play();
            DeactivateAfterDelay().Forget();
        }
        
        protected async UniTaskVoid DeactivateAfterDelay()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), ignoreTimeScale: true);
            
            var saveLoadManager = FindAnyObjectByType<SaveLoadManager>();
            saveLoadManager.SaveGame();
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), ignoreTimeScale: true);
            
            Time.timeScale = 1;
            gameObject.SetActive(false);
        }
        
        [SerializeField] private AudioSource audioSource;

        public AudioSource AudioSource
        {
            get => audioSource;
            set => audioSource = value;
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            audioSource = GetComponent<AudioSource>();
        }
#endif

        public bool EnrollToSaveManager => true;
        
        public bool Save(string saveFileName)
        {
            if (ID != "") ISavable.EasySave($"{ID} isWorked", isWorked, saveFileName);
            
            return true;
        }

        public bool Load(string saveFileName)
        {
            if (ID != "") isWorked = ISavable.EasyLoad<bool>($"{ID} isWorked", saveFileName);
            return true;
        }
    }
}
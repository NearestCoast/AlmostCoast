using System;
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
        }
        
        protected async UniTaskVoid DeactivateAfterDelay()
        {
            var saveLoadManager = FindAnyObjectByType<SaveLoadManager>();
            saveLoadManager.SaveGame();
            
            await UniTask.Delay(1000, ignoreTimeScale: true);
            
            Time.timeScale = 1;
            gameObject.SetActive(false);
        }

        public bool EnrollToSaveManager => true;
        
        public bool Save(string saveFileName)
        {
            ISavable.EasySave($"{ID}isWorked", isWorked, saveFileName);
            
            return true;
        }

        public bool Load(string saveFileName)
        {
            isWorked = ISavable.EasyLoad<bool>($"{ID}isWorked", saveFileName);
            return true;
        }
    }
}
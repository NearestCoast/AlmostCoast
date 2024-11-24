using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Managers.Scripts._Core.AudioManager;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class Key : Collectable
    {
        public enum Type
        {
            Silver,
            Gold
        }

        [SerializeField] private Type type;

        public Type KeyType => type;
        
        private PlayerCharacter playerCharacter;
        private void Awake()
        {
            playerCharacter = FindAnyObjectByType<PlayerCharacter>();
        }
        
        protected override void Work()
        {
            base.Work();
            playerCharacter.AddKey(type);
            Debug.Log("Player Collected " + KeyType + " Key");
            
            FindAnyObjectByType<AudioManager>().SetClip(audioSource.clip);
            FindAnyObjectByType<AudioManager>().Play();
            Time.timeScale = 0;
            
            DestroyAfterDelay().Forget();
        }
        
        private async UniTaskVoid DestroyAfterDelay()
        {
            await UniTask.Delay(1000, ignoreTimeScale: true); // real-time으로 3초 대기
            Time.timeScale = 1;
            Destroy(gameObject);
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
    }
}
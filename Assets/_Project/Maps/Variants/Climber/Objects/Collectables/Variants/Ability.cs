using System;
using _Project.Managers.Scripts._Core.AudioManager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using AnimationState = _Project.Characters._Core.States.AnimationStates.AnimationState;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class Ability : Collectable
    {
        [SerializeField] private AnimationState targetState;

        public AnimationState TargetState
        {
            get => targetState;
            set => targetState = value;
        }
        
        protected override void Work()
        {
            base.Work();
            Debug.Log("Player Collected " + TargetState.name);
            TargetState.gameObject.SetActive(true);
            FindObjectOfType<AudioManager>().SetClip(audioSource.clip);
            FindObjectOfType<AudioManager>().Play();
            Time.timeScale = 0;
            
            DestroyAfterDelay().Forget();
        }
        
        private async UniTaskVoid DestroyAfterDelay()
        {
            await UniTask.Delay(3000, ignoreTimeScale: true); // real-time으로 3초 대기
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
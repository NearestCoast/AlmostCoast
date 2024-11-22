using System;
using System.Collections.Generic;
using _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter;
using _Project.Characters.IngameCharacters.Core.ActionStates;
using _Project.Characters.IngameCharacters.Core.MovementStates;
using _Project.Managers.Scripts._Core.AudioManager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using AnimationState = _Project.Characters._Core.States.AnimationStates.AnimationState;

namespace _Project.Maps.Climber.Objects.Collectables
{
    public class Ability : Collectable
    {
        private PlayerCharacter playerCharacter;
        private void Awake()
        {
            playerCharacter = FindAnyObjectByType<PlayerCharacter>();
        }

        [SerializeField] private List<AnimationState> targetStates;

        public List<AnimationState> TargetStates
        {
            get => targetStates;
            set => targetStates = value;
        }

        protected override void Work()
        {
            base.Work();
            
            foreach (var targetState in TargetStates)
            {
                Debug.Log("Player Collected " + targetState.name);
                targetState.gameObject.SetActive(true);

                if (targetState is MovementState)
                {
                    playerCharacter.GetComponentInChildren<MovementStateContainer>().UpdateDictionary();
                }
                else if (targetState is ActionState)
                {
                    playerCharacter.GetComponentInChildren<ActionStateContainer>().UpdateDictionary();
                }
            }
            
            
            FindAnyObjectByType<AudioManager>().SetClip(audioSource.clip);
            FindAnyObjectByType<AudioManager>().Play();
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
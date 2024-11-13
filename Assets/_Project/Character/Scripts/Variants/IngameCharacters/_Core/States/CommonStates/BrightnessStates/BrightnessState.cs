using _Project.Characters._Core.States.CommonStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace _Project.Characters.IngameCharacters.Core.States.CommonStates.BrightnessState
{
    public partial class BrightnessState : CommonState
    {
        public enum StateType
        {
            Normal,
            Dark,
        }

        [ShowInInspector] public virtual StateType Type { get; }
        protected BrightnessState NextState => CommonStateConductor.BrightnessStateMachine.NextState as BrightnessState;
        protected BrightnessState PrevState => CommonStateConductor.BrightnessStateMachine.PreviousState as BrightnessState;
    }

    public partial class BrightnessState : CommonState
    {
        [SerializeField, TitleGroup("Fx")] protected VisualEffect visualEffect;
        [SerializeField, TitleGroup("Fx")] protected AudioSource audioSource;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            audioSource = gameObject.GetComponent<AudioSource>();
            visualEffect = gameObject.GetComponentInChildren<VisualEffect>();
        }
#endif
        
        public override void OnEnterState()
        {
            base.OnEnterState();
            PlaySound();
            PlayVisualEffect();
        }

        protected virtual void PlaySound()
        {
            if (audioSource) audioSource.Play();
        }

        protected virtual void PlayVisualEffect()
        {
            if (visualEffect) visualEffect.Play();
        }
    }
}
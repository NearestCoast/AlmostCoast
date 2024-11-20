using Animancer;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public class BaseLayerClipMovementState : MovementState
    {
        [SerializeField] protected ClipTransition anim;

        public override void PlayAnimation()
        {
            base.PlayAnimation();
            AnimancerState = AnimationStateConductor.BaseLayer.Play(anim);
            AnimancerState.NormalizedTime = animCutStartNormalizedTime;
        }
    }
}
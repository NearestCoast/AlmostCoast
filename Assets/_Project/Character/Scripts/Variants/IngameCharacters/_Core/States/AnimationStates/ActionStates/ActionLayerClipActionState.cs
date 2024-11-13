using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.ActionStates
{
    public class ActionLayerClipActionState : ActionState
    {
        [SerializeField, TitleGroup("Animation")] private ClipTransition anim;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            AnimancerState = AnimationStateConductor.AttackLayer.Play(anim);
            AnimancerState.NormalizedTime = animCutStartNormalizedTime;
        }
    }
}